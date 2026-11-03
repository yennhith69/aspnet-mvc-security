BEGIN TRANSACTION;
ALTER TABLE [NhanSus] ADD [CongKhaiChucVu] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [NhanSus] ADD [CongKhaiDonVi] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [NhanSus] ADD [CongKhaiHoTen] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [NhanSus] ADD [CongKhaiNckh] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [NhanSus] ADD [CongKhaiTrinhDo] bit NOT NULL DEFAULT CAST(0 AS bit);

CREATE TABLE [ChungChis] (
    [Id] int NOT NULL IDENTITY,
    [NhanSuId] int NOT NULL,
    [Ten] nvarchar(200) NOT NULL,
    [DonViCap] nvarchar(200) NULL,
    [NgayCap] datetime2 NULL,
    [SoHieu] nvarchar(100) NULL,
    [TepMinhChungId] int NULL,
    CONSTRAINT [PK_ChungChis] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChungChis_NhanSus_NhanSuId] FOREIGN KEY ([NhanSuId]) REFERENCES [NhanSus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChungChis_TepMinhChungs_TepMinhChungId] FOREIGN KEY ([TepMinhChungId]) REFERENCES [TepMinhChungs] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LichSuSucKhoes] (
    [Id] int NOT NULL IDENTITY,
    [NhanSuId] int NOT NULL,
    [NgayGhiNhan] datetime2 NOT NULL,
    [TinhTrang] nvarchar(500) NOT NULL,
    [GhiChu] nvarchar(1000) NULL,
    CONSTRAINT [PK_LichSuSucKhoes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LichSuSucKhoes_NhanSus_NhanSuId] FOREIGN KEY ([NhanSuId]) REFERENCES [NhanSus] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_ChungChis_NhanSuId] ON [ChungChis] ([NhanSuId]);

CREATE INDEX [IX_ChungChis_TepMinhChungId] ON [ChungChis] ([TepMinhChungId]);

CREATE INDEX [IX_LichSuSucKhoes_NhanSuId] ON [LichSuSucKhoes] ([NhanSuId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261103123419_AddEducationHealthPublicControls', N'10.0.14');

COMMIT;
GO

