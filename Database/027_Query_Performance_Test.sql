USE HrmSecure;
GO
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Chạy truy vấn này trước khi tạo index và lưu output Messages/Actual Plan.
SELECT n.Id, n.MaHrm, n.HoTen, n.DonViId, n.TinhTrangCongTac, n.TrangThaiHoSo
FROM dbo.NhanSus AS n
WHERE n.DonViId = 1 AND n.TinhTrangCongTac = N'DangCongTac'
ORDER BY n.MaHrm;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.NhanSus')
      AND name = N'IX_NhanSus_DonVi_TinhTrang_MaHrm')
BEGIN
    CREATE NONCLUSTERED INDEX IX_NhanSus_DonVi_TinhTrang_MaHrm
        ON dbo.NhanSus (DonViId, TinhTrangCongTac, MaHrm)
        INCLUDE (HoTen, TrangThaiHoSo);
END
GO

-- Chạy lại, rồi so sánh logical reads/CPU/elapsed time và Actual Execution Plan.
SELECT n.Id, n.MaHrm, n.HoTen, n.DonViId, n.TinhTrangCongTac, n.TrangThaiHoSo
FROM dbo.NhanSus AS n
WHERE n.DonViId = 1 AND n.TinhTrangCongTac = N'DangCongTac'
ORDER BY n.MaHrm;
GO
SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
