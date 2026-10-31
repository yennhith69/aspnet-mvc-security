BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031121640_AddKyLuat'
)
BEGIN
    CREATE TABLE [KyLuats] (
        [Id] int NOT NULL IDENTITY,
        [NhanSuId] int NOT NULL,
        [Loai] nvarchar(max) NOT NULL,
        [NoiDung] nvarchar(max) NOT NULL,
        [Ngay] datetime2 NOT NULL,
        CONSTRAINT [PK_KyLuats] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KyLuats_NhanSus_NhanSuId] FOREIGN KEY ([NhanSuId]) REFERENCES [NhanSus] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031121640_AddKyLuat'
)
BEGIN
    CREATE INDEX [IX_KyLuats_NhanSuId] ON [KyLuats] ([NhanSuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031121640_AddKyLuat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031121640_AddKyLuat', N'10.0.14');
END;

COMMIT;
GO
