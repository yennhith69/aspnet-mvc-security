BEGIN TRANSACTION;
CREATE TABLE [ThanhVienGiaDinhs] (
    [Id] int NOT NULL IDENTITY,
    [NhanSuId] int NOT NULL,
    [HoTen] nvarchar(200) NOT NULL,
    [QuanHe] nvarchar(100) NOT NULL,
    [NamSinh] int NULL,
    [NgheNghiep] nvarchar(200) NULL,
    CONSTRAINT [PK_ThanhVienGiaDinhs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ThanhVienGiaDinhs_NhanSus_NhanSuId] FOREIGN KEY ([NhanSuId]) REFERENCES [NhanSus] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_ThanhVienGiaDinhs_NhanSuId] ON [ThanhVienGiaDinhs] ([NhanSuId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261103114922_AddThanhVienGiaDinh', N'10.0.14');

COMMIT;
GO

