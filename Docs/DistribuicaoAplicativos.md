# Distribuição e versionamento de aplicativos

## Instalação

1. A migração `Data/Scripts/20261109_aplicacoes_versoes.sql` foi aplicada ao banco `PotyInternos` em 09/11/2026 e reexecutada para verificar a idempotência. Em outros ambientes, execute-a somente no banco correspondente. O script é transacional; adiciona o slug em `Global.Aplicacoes` e cria `Global.AplicacoesVersoes`. Não acessa `Global.Usuarios`.
2. Configure `Distribution:StoragePath` (variável de ambiente `Distribution__StoragePath`) como pasta persistente **fora do diretório publicado pelo IIS**, com escrita para a identidade da API. O padrão local é a pasta irmã `PotyInternosReleases`. Preserve e faça backup dessa pasta junto ao banco; os caminhos dos artefatos são referenciados nas versões.
3. Configure `Distribution:PublicBaseUrl` (variável `Distribution__PublicBaseUrl`) com a URL HTTPS pública da API. A configuração de produção atual aponta para `https://potyinternosapi.bebidaspoty.com.br`. O `web.config` permite até 3 GiB por request no IIS; confira também limites do proxy/CDN.

## Contrato

- `GET /api/aplicacoes/{id}/versoes` (Bearer Admin): histórico, mais recente primeiro.
- `POST /api/aplicacoes/{id}/versoes` (Bearer Admin, multipart): `versao`, `descricao`, `android`, `ios`, `apk`, `ipa`, `androidVersionCode`, `iosBundleId`, `iosBuildNumber`. A data UTC é gerada pelo servidor. Cada plataforma marcada exige seu arquivo e metadados. Uma plataforma desmarcada não aceita arquivo. O servidor verifica extensão e estrutura ZIP básica de APK/IPA e calcula SHA-256, mas **não autentica a assinatura nem confere os números internos do pacote**; devem corresponder aos valores preenchidos. A nova versão se torna ativa, preservando as anteriores.
- `PUT /api/aplicacoes/{id}/versoes/{versaoId}/ativar` (Bearer Admin): seleciona versão histórica como ativa; os arquivos precisam existir.
- `GET /api/apps/{aplicacaoId}/atual` (público): retorna `aplicacaoId`, `versaoId`, `versaoAtual`, `descricao`, `dataPublicacao`, `android`, `ios`, `androidVersionCode`, `iosBuildNumber`, `androidUrl`, `iosUrl` e `iosManifestUrl`. Use o `aplicacaoId` do cadastro de cada aplicação; o slug antigo continua aceito apenas como alias de compatibilidade.
- `GET /api/apps/{aplicacaoId}/download/android`: APK da versão ativa. `GET /api/apps/{aplicacaoId}/download/ios`: redireciona para instalação OTA. O `iosUrl` no JSON já é o link `itms-services://...` para abrir pelo Safari mediante toque do usuário.

No banco PotyInternos atual, o cadastro **Poty Pessoas** tem `AplicacaoID` `dc068cb7-b88b-4555-95be-344ab3b5e9c7`; seu link público é `https://potyinternos.bebidaspoty.com.br/#/app/dc068cb7-b88b-4555-95be-344ab3b5e9c7`. Cada outro aplicativo usa o próprio ID, exibido na coluna **Identificador** da tela de aplicações.

O app deve comparar **versão e número de build instalados** com `versaoAtual` e o código/build da plataforma; qualquer diferença indica mudança do catálogo, inclusive seleção de versão histórica. `versaoId` identifica o lançamento no servidor e serve para histórico, mas não está embutido no pacote instalado.

### Integração do PotyPessoas e dos demais apps Flutter

O PotyPessoas está em `1.0.3+103` e ainda precisa consumir este contrato. Em `app/pubspec.yaml`, mantenha `version` sincronizada com os dados publicados. Adicione `package_info_plus` para ler `PackageInfo.version` e `PackageInfo.buildNumber` do pacote instalado. Use um cliente HTTP separado da API do PotyPessoas, sem token de sessão, para consultar `GET https://potyinternosapi.bebidaspoty.com.br/api/apps/{aplicacaoId}/atual`, substituindo pelo ID do cadastro PotyPessoas. O campo `versaoAtual` corresponde a `PackageInfo.version`; no Android compare `androidVersionCode` com `int.parse(PackageInfo.buildNumber)` e no iOS compare `iosBuildNumber` com `PackageInfo.buildNumber`.

Faça a consulta no início do app, antes de abrir o conteúdo principal, e novamente quando o ciclo de vida voltar para `AppLifecycleState.resumed`. Compare por **desigualdade** (`!=`) para detectar tanto uma publicação nova quanto a seleção de uma versão histórica. Se houver diferença, mostre um aviso com botão que abra `https://potyinternos.bebidaspoty.com.br/#/app/{aplicacaoId}` no navegador externo. Ao voltar do navegador, atualize o aviso sem abrir o link automaticamente de novo, evitando um ciclo. Use timeout e opção de repetir a consulta se a rede falhar; não bloqueie indefinidamente o uso do app. Se `android` ou `ios` estiver desmarcado na versão ativa, informe indisponibilidade dessa plataforma.

## iOS

O fluxo IPA + manifesto HTTPS é adequado para aplicativos internos assinados para distribuição **Apple Developer Enterprise Program**, para empregados autorizados. O IPA precisa estar corretamente assinado, com bundle ID, versão e número de build coerentes com o cadastro. O manifesto gerado aponta ao IPA da **mesma publicação**, mesmo se outra versão for ativada durante a instalação. A Apple exige HTTPS confiável e o dispositivo precisa confiar no desenvolvedor empresarial. APK/IPA e manifesto têm endpoints públicos para que o instalador do sistema operacional consiga buscá-los sem o token Bearer; proteja a distribuição na rede/infraestrutura quando necessário.

Para disponibilizar iOS diretamente pelo PotyInternos:

1. Confirme que a empresa é elegível e está inscrita no Apple Developer Enterprise Program. Esse canal é para aplicativos próprios usados por empregados, com distribuição interna segura.
2. No portal Apple Developer, configure o App ID/bundle ID, certificado de distribuição empresarial e perfil de provisionamento *in-house*. No Xcode, arquive o app e exporte um `.ipa` Enterprise assinado. Confira se `CFBundleIdentifier`, `CFBundleShortVersionString` e `CFBundleVersion` correspondem ao cadastro.
3. No PotyInternos, publique a versão com iOS marcado, selecione o `.ipa`, informe o bundle ID e o build iOS. A API gera o manifesto `plist` e o link `itms-services` automaticamente. Mantenha `Distribution:PublicBaseUrl` e os endpoints de manifesto/IPA acessíveis por HTTPS confiável aos aparelhos autorizados.
4. No iPhone, abra `https://potyinternos.bebidaspoty.com.br/#/app/<aplicacaoId>` no Safari e toque em **Instalar no iPhone**. Se o sistema pedir, siga a política da empresa para confiar no desenvolvedor empresarial. Teste a instalação em um aparelho antes de liberar a atualização.

Se a empresa não usa o programa Enterprise, use **Custom Apps** no Apple Business: envie o build ao App Store Connect, escolha distribuição privada para o Organization ID, passe pela revisão da Apple e distribua pelo Apple Business/MDM. Nesse modo, a instalação ocorre pela Apple; o fluxo de upload IPA/manifesto deste projeto não substitui essa publicação. TestFlight serve a testes beta, com builds que expiram em 90 dias.

Referências: [Apple — distribuição in-house por website](https://support.apple.com/en-gb/guide/deployment/depce7cefc4d/1/web/1.0), [Apple — Enterprise Program](https://developer.apple.com/programs/enterprise/), [Apple — distribuição privada pelo Apple Business](https://developer.apple.com/help/app-store-connect/manage-your-apps-availability/set-distribution-methods), [Apple — TestFlight](https://developer.apple.com/help/app-store-connect/test-a-beta-version/testflight-overview/).

Ativar uma versão antiga muda a versão indicada pela API, mas **não garante downgrade** no aparelho: Android pode rejeitar APK com `versionCode` menor; iOS pode recusar um IPA/build antigo. A recuperação confiável é publicar novamente o código desejado com números de build maiores e assinatura compatível. App Store, Custom Apps e TestFlight usam fluxos de distribuição diferentes e não instalam por este manifesto Enterprise.

Referências: [Android — versionamento e proteção contra downgrade](https://developer.android.com/studio/publish/versioning), [Apple — criar nova versão no App Store Connect](https://developer.apple.com/help/app-store-connect/update-your-app/create-a-new-version).
