-- M27: đo lường truy vấn danh sách hồ sơ và kiểm tra index.
-- Chạy trên bản sao/staging trước khi áp dụng production.
USE HrmSecure;
GO
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT n.Id, n.MaHrm, n.HoTen, n.DonViId, n.TinhTrangCongTac, n.TrangThaiHoSo
FROM dbo.NhanSus AS n
WHERE n.DonViId = 1 AND n.TinhTrangCongTac = N'DangCongTac'
ORDER BY n.MaHrm;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- So sánh execution plan trước/sau (bật Include Actual Execution Plan trong SSMS).
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

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
SELECT n.Id, n.MaHrm, n.HoTen, n.DonViId, n.TinhTrangCongTac, n.TrangThaiHoSo
FROM dbo.NhanSus AS n
WHERE n.DonViId = 1 AND n.TinhTrangCongTac = N'DangCongTac'
ORDER BY n.MaHrm;
SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO
