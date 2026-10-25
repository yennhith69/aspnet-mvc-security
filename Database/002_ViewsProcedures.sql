-- 002_ViewsProcedures.sql (mục 2). Chạy bằng tài khoản quản trị (Windows Authentication).
USE HrmSecure;
GO

-- View báo cáo: KHÔNG có email, điện thoại, địa chỉ, giấy tờ, lương
CREATE OR ALTER VIEW dbo.vw_BaoCao_NhanSu AS
SELECT n.Id, n.MaHrm, n.HoTen, n.GioiTinh, d.Ten AS DonVi, c.Ten AS ChucVu,
       n.TinhTrangCongTac, n.TrangThaiHoSo, n.NgayTao
FROM dbo.NhanSus n
JOIN dbo.DonVis d ON d.Id = n.DonViId
LEFT JOIN dbo.ChucVus c ON c.Id = n.ChucVuId;
GO

-- View công khai: chỉ hồ sơ đã xác nhận, được phép công khai, còn công tác
CREATE OR ALTER VIEW dbo.vw_NhanSu_CongKhai AS
SELECT n.MaHrm, n.HoTen, d.Ten AS DonVi, c.Ten AS ChucVu
FROM dbo.NhanSus n
JOIN dbo.DonVis d ON d.Id = n.DonViId
LEFT JOIN dbo.ChucVus c ON c.Id = n.ChucVuId
WHERE n.CongKhai = 1
  AND n.TrangThaiHoSo = N'DaXacNhan'
  AND n.TinhTrangCongTac <> N'NghiViec';
GO

-- Procedure thống kê: người gọi chỉ cần EXECUTE, không cần quyền đọc bảng
CREATE OR ALTER PROCEDURE dbo.sp_ThongKeNhanSuTheoDonVi
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.Ten AS DonVi,
           COUNT(n.Id) AS SoNhanSu,
           SUM(CASE WHEN n.TrangThaiHoSo = N'DaXacNhan' THEN 1 ELSE 0 END) AS DaXacNhan
    FROM dbo.DonVis d
    LEFT JOIN dbo.NhanSus n ON n.DonViId = d.Id
    GROUP BY d.Ten
    ORDER BY d.Ten;
END
GO

-- Procedure xác nhận hồ sơ: kiểm tra trạng thái, cập nhật và ghi nhật ký trong một giao dịch
CREATE OR ALTER PROCEDURE dbo.sp_XacNhanHoSo
    @NhanSuId INT,
    @UserId NVARCHAR(450) = NULL,
    @UserName NVARCHAR(256),
    @Ip NVARCHAR(45) = NULL,
    @ThanhCong BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ThanhCong = 0;
    DECLARE @ma NVARCHAR(20);

    BEGIN TRY
        BEGIN TRAN;

        UPDATE dbo.NhanSus
        SET TrangThaiHoSo = N'DaXacNhan', @ma = MaHrm
        WHERE Id = @NhanSuId AND TrangThaiHoSo = N'DaCapMa';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRAN;
            RETURN;
        END

        INSERT INTO dbo.AuditLogs (UserId, UserName, HanhDong, DoiTuong, DoiTuongId, Ip, ChiTiet, ThoiGian)
        VALUES (@UserId, @UserName, N'XacNhanHoSo', N'NhanSu', CAST(@NhanSuId AS NVARCHAR(20)),
                @Ip, N'Mã ' + @ma, SYSUTCDATETIME());

        COMMIT TRAN;
        SET @ThanhCong = 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

-- Phân quyền: report chỉ đọc view và gọi procedure thống kê, KHÔNG có quyền trên bảng gốc
GRANT SELECT ON dbo.vw_BaoCao_NhanSu TO role_hrm_report;
GRANT SELECT ON dbo.vw_NhanSu_CongKhai TO role_hrm_report;
GRANT EXECUTE ON dbo.sp_ThongKeNhanSuTheoDonVi TO role_hrm_report;

-- Ứng dụng gọi procedure xác nhận hồ sơ
GRANT EXECUTE ON dbo.sp_XacNhanHoSo TO role_hrm_app;
GO