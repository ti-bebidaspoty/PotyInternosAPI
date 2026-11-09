-- Execute somente no banco PotyInternos. Mantém as publicações existentes.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF DB_NAME() <> N'PotyInternos'
    THROW 50001, 'Esta migração deve ser executada somente no banco PotyInternos.', 1;

IF OBJECT_ID(N'[Global].[AplicacoesVersoes]', N'U') IS NULL
    THROW 50002, 'A tabela Global.AplicacoesVersoes não existe. Aplique a migração inicial primeiro.', 1;

IF COL_LENGTH(N'Global.AplicacoesVersoes', N'AndroidExternalUrl') IS NULL
    ALTER TABLE [Global].[AplicacoesVersoes] ADD [AndroidExternalUrl] VARCHAR(2048) NULL;

IF COL_LENGTH(N'Global.AplicacoesVersoes', N'IosExternalUrl') IS NULL
    ALTER TABLE [Global].[AplicacoesVersoes] ADD [IosExternalUrl] VARCHAR(2048) NULL;

EXEC(N'
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N''CK_AplicacoesVersoes_Plataformas'' AND parent_object_id = OBJECT_ID(N''[Global].[AplicacoesVersoes]''))
    ALTER TABLE [Global].[AplicacoesVersoes] DROP CONSTRAINT [CK_AplicacoesVersoes_Plataformas];

ALTER TABLE [Global].[AplicacoesVersoes] WITH CHECK ADD CONSTRAINT [CK_AplicacoesVersoes_Plataformas] CHECK (
    ([Android] = 1 AND [AndroidVersionCode] > 0 AND
        (([ApkArquivo] IS NOT NULL AND [AndroidExternalUrl] IS NULL) OR
         ([ApkArquivo] IS NULL AND [AndroidExternalUrl] IS NOT NULL))
     OR [Android] = 0 AND [AndroidVersionCode] IS NULL AND [ApkArquivo] IS NULL AND [AndroidExternalUrl] IS NULL)
    AND
    ([Ios] = 1 AND [IosBuildNumber] IS NOT NULL AND
        (([IpaArquivo] IS NOT NULL AND [IosBundleId] IS NOT NULL AND [IosExternalUrl] IS NULL) OR
         ([IpaArquivo] IS NULL AND [IosBundleId] IS NULL AND [IosExternalUrl] IS NOT NULL))
     OR [Ios] = 0 AND [IosBuildNumber] IS NULL AND [IpaArquivo] IS NULL AND [IosBundleId] IS NULL AND [IosExternalUrl] IS NULL)
    AND ([Android] = 1 OR [Ios] = 1)
);
');

COMMIT TRANSACTION;
