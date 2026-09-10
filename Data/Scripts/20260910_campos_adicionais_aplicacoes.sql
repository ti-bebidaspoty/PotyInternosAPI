IF OBJECT_ID(N'[Global].[UsuariosAplicacoesCamposAdicionaisValores]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[Global].[AplicacoesCamposAdicionais]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Global].[AplicacoesCamposAdicionais]
        (
            [CampoAdicionalID] VARCHAR(50) NOT NULL,
            [AplicacaoID] VARCHAR(50) NOT NULL,
            [Campo] VARCHAR(200) NOT NULL,
            [Tipo] VARCHAR(20) NOT NULL,
            [Ordem] INT NOT NULL CONSTRAINT [DF_AplicacoesCamposAdicionais_Ordem] DEFAULT (0),
            CONSTRAINT [PK_AplicacoesCamposAdicionais] PRIMARY KEY ([CampoAdicionalID]),
            CONSTRAINT [FK_AplicacoesCamposAdicionais_Aplicacoes]
                FOREIGN KEY ([AplicacaoID]) REFERENCES [Global].[Aplicacoes] ([AplicacaoID]),
            CONSTRAINT [CK_AplicacoesCamposAdicionais_Tipo]
                CHECK ([Tipo] IN ('Int', 'String', 'Bool', 'Float'))
        );

        CREATE UNIQUE INDEX [UX_AplicacoesCamposAdicionais_Aplicacao_Campo]
            ON [Global].[AplicacoesCamposAdicionais] ([AplicacaoID], [Campo]);
    END;

    CREATE TABLE [Global].[UsuariosAplicacoesCamposAdicionaisValores]
    (
        [UsuarioID] VARCHAR(50) NOT NULL,
        [AplicacaoID] VARCHAR(50) NOT NULL,
        [CampoAdicionalID] VARCHAR(50) NOT NULL,
        [Valor] VARCHAR(500) NOT NULL,
        CONSTRAINT [PK_UsuariosAplicacoesCamposAdicionaisValores]
            PRIMARY KEY ([UsuarioID], [AplicacaoID], [CampoAdicionalID]),
        CONSTRAINT [FK_UsuariosAplicacoesCamposValores_UsuariosAplicacoes]
            FOREIGN KEY ([UsuarioID], [AplicacaoID])
            REFERENCES [Global].[UsuariosAplicacoes] ([UsuarioID], [AplicacaoID]),
        CONSTRAINT [FK_UsuariosAplicacoesCamposValores_Campos]
            FOREIGN KEY ([CampoAdicionalID])
            REFERENCES [Global].[AplicacoesCamposAdicionais] ([CampoAdicionalID])
    );
END;
