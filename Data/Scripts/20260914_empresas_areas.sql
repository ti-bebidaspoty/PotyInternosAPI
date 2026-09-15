IF OBJECT_ID(N'[Global].[Areas]', N'U') IS NULL
BEGIN
    CREATE TABLE [Global].[Areas]
    (
        [AreaID] VARCHAR(50) NOT NULL,
        [Area] VARCHAR(200) NOT NULL,
        [Status] BIT NOT NULL CONSTRAINT [DF_Areas_Status] DEFAULT (1),
        CONSTRAINT [PK_Areas] PRIMARY KEY ([AreaID])
    );

    CREATE UNIQUE INDEX [UX_Areas_Area] ON [Global].[Areas] ([Area]);
END;

IF OBJECT_ID(N'[Global].[Empresas]', N'U') IS NULL
BEGIN
    CREATE TABLE [Global].[Empresas]
    (
        [EmpresaID] VARCHAR(50) NOT NULL,
        [Empresa] VARCHAR(200) NOT NULL,
        [CodigoAlternativo] VARCHAR(50) NOT NULL,
        [Status] BIT NOT NULL CONSTRAINT [DF_Empresas_Status] DEFAULT (1),
        CONSTRAINT [PK_Empresas] PRIMARY KEY ([EmpresaID])
    );

    CREATE UNIQUE INDEX [UX_Empresas_Empresa] ON [Global].[Empresas] ([Empresa]);
    CREATE UNIQUE INDEX [UX_Empresas_CodigoAlternativo] ON [Global].[Empresas] ([CodigoAlternativo]);
END;

IF COL_LENGTH(N'[Global].[Departamentos]', 'AreaID') IS NULL
BEGIN
    -- Cria a coluna aceitando nulo para popular os registros existentes antes de tornar obrigatória.
    ALTER TABLE [Global].[Departamentos] ADD [AreaID] VARCHAR(50) NULL;

    IF NOT EXISTS (SELECT 1 FROM [Global].[Areas] WHERE [Area] = N'Geral')
    BEGIN
        INSERT INTO [Global].[Areas] ([AreaID], [Area], [Status])
        VALUES (LOWER(CONVERT(VARCHAR(36), NEWID())), N'Geral', 1);
    END;

    UPDATE d
    SET d.[AreaID] = (SELECT TOP 1 a.[AreaID] FROM [Global].[Areas] a WHERE a.[Area] = N'Geral')
    FROM [Global].[Departamentos] d
    WHERE d.[AreaID] IS NULL;

    ALTER TABLE [Global].[Departamentos] ALTER COLUMN [AreaID] VARCHAR(50) NOT NULL;

    ALTER TABLE [Global].[Departamentos]
        ADD CONSTRAINT [FK_Departamentos_Areas] FOREIGN KEY ([AreaID]) REFERENCES [Global].[Areas] ([AreaID]);
END;

IF COL_LENGTH(N'[Global].[Usuarios]', 'EmpresaID') IS NULL
BEGIN
    -- Cria a coluna aceitando nulo para popular os registros existentes antes de tornar obrigatória.
    ALTER TABLE [Global].[Usuarios] ADD [EmpresaID] VARCHAR(50) NULL;

    IF NOT EXISTS (SELECT 1 FROM [Global].[Empresas] WHERE [CodigoAlternativo] = N'DEFAULT')
    BEGIN
        INSERT INTO [Global].[Empresas] ([EmpresaID], [Empresa], [CodigoAlternativo], [Status])
        VALUES (LOWER(CONVERT(VARCHAR(36), NEWID())), N'Empresa Padrão', N'DEFAULT', 1);
    END;

    UPDATE u
    SET u.[EmpresaID] = (SELECT TOP 1 e.[EmpresaID] FROM [Global].[Empresas] e WHERE e.[CodigoAlternativo] = N'DEFAULT')
    FROM [Global].[Usuarios] u
    WHERE u.[EmpresaID] IS NULL;

    ALTER TABLE [Global].[Usuarios] ALTER COLUMN [EmpresaID] VARCHAR(50) NOT NULL;

    ALTER TABLE [Global].[Usuarios]
        ADD CONSTRAINT [FK_Usuarios_Empresas] FOREIGN KEY ([EmpresaID]) REFERENCES [Global].[Empresas] ([EmpresaID]);
END;
