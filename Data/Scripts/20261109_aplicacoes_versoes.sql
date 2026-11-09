-- Execute somente no banco PotyInternos. Não altera Global.Usuarios.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'Global.Aplicacoes', N'Slug') IS NULL
    ALTER TABLE [Global].[Aplicacoes] ADD [Slug] VARCHAR(200) NULL;

-- SQL Server compila referências a Slug por batch; o separador preserva a transação
-- na mesma conexão e permite compilar o backfill após a criação da coluna.
GO

-- Backfill determinístico. PotyPessoas torna-se potypessoas.
DECLARE @id VARCHAR(50), @name VARCHAR(200), @slug VARCHAR(200), @char CHAR(1), @i INT;
DECLARE app_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT [AplicacaoID], [Aplicacao] FROM [Global].[Aplicacoes] WHERE [Slug] IS NULL ORDER BY [AplicacaoID];
OPEN app_cursor;
FETCH NEXT FROM app_cursor INTO @id, @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @slug = '';
    SET @i = 1;
    WHILE @i <= LEN(@name)
    BEGIN
        SET @char = LOWER(SUBSTRING(@name, @i, 1));
        IF @char COLLATE Latin1_General_100_BIN2 LIKE '[a-z0-9]'
            SET @slug = @slug + @char;
        SET @i += 1;
    END;
    IF @slug = '' SET @slug = 'app';
    IF EXISTS (SELECT 1 FROM [Global].[Aplicacoes] WHERE [Slug] = @slug)
        SET @slug = LEFT(@slug, 165) + '-' + LOWER(REPLACE(@id, '-', ''));
    UPDATE [Global].[Aplicacoes] SET [Slug] = @slug WHERE [AplicacaoID] = @id;
    FETCH NEXT FROM app_cursor INTO @id, @name;
END;
CLOSE app_cursor;
DEALLOCATE app_cursor;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Global].[Aplicacoes]') AND name = 'Slug' AND is_nullable = 1)
    ALTER TABLE [Global].[Aplicacoes] ALTER COLUMN [Slug] VARCHAR(200) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Global].[Aplicacoes]') AND name = N'UX_Aplicacoes_Slug')
    CREATE UNIQUE INDEX [UX_Aplicacoes_Slug] ON [Global].[Aplicacoes] ([Slug]);

IF OBJECT_ID(N'[Global].[AplicacoesVersoes]', N'U') IS NULL
BEGIN
    CREATE TABLE [Global].[AplicacoesVersoes]
    (
        [VersaoID] VARCHAR(50) NOT NULL,
        [AplicacaoID] VARCHAR(50) NOT NULL,
        [Versao] VARCHAR(100) NOT NULL,
        [Descricao] NVARCHAR(2000) NOT NULL,
        [DataPublicacao] DATETIME2(3) NOT NULL,
        [Android] BIT NOT NULL,
        [Ios] BIT NOT NULL,
        [Ativa] BIT NOT NULL,
        [ApkArquivo] VARCHAR(100) NULL,
        [IpaArquivo] VARCHAR(100) NULL,
        [ApkSha256] VARCHAR(64) NULL,
        [IpaSha256] VARCHAR(64) NULL,
        [IosBundleId] VARCHAR(200) NULL,
        [AndroidVersionCode] INT NULL,
        [IosBuildNumber] VARCHAR(100) NULL,
        CONSTRAINT [PK_AplicacoesVersoes] PRIMARY KEY ([VersaoID]),
        CONSTRAINT [FK_AplicacoesVersoes_Aplicacoes] FOREIGN KEY ([AplicacaoID]) REFERENCES [Global].[Aplicacoes] ([AplicacaoID]),
        CONSTRAINT [CK_AplicacoesVersoes_Plataformas] CHECK (
            ([Android] = 1 AND [ApkArquivo] IS NOT NULL AND [AndroidVersionCode] > 0 OR [Android] = 0 AND [ApkArquivo] IS NULL AND [AndroidVersionCode] IS NULL)
            AND ([Ios] = 1 AND [IpaArquivo] IS NOT NULL AND [IosBundleId] IS NOT NULL AND [IosBuildNumber] IS NOT NULL OR [Ios] = 0 AND [IpaArquivo] IS NULL AND [IosBundleId] IS NULL AND [IosBuildNumber] IS NULL)
            AND ([Android] = 1 OR [Ios] = 1))
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Global].[AplicacoesVersoes]') AND name = N'UX_AplicacoesVersoes_Aplicacao_Versao')
    CREATE UNIQUE INDEX [UX_AplicacoesVersoes_Aplicacao_Versao] ON [Global].[AplicacoesVersoes] ([AplicacaoID], [Versao]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Global].[AplicacoesVersoes]') AND name = N'UX_AplicacoesVersoes_Ativa')
    CREATE UNIQUE INDEX [UX_AplicacoesVersoes_Ativa] ON [Global].[AplicacoesVersoes] ([AplicacaoID]) WHERE [Ativa] = 1;

COMMIT TRANSACTION;
