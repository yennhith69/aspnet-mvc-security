-- 004_RowLevelSecurity.sql (mục 3). Chạy bằng tài khoản quản trị SAU 003_Masking.sql.
-- Trước khi chạy "dotnet ef database update" sửa cấu trúc các bảng này, tắt policy:
--   ALTER SECURITY POLICY dbo.pol_NhanSu WITH (STATE = OFF);
--   ALTER SECURITY POLICY dbo.pol_BangCon WITH (STATE = OFF);
USE HrmSecure;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'pol_NhanSu') DROP SECURITY POLICY dbo.pol_NhanSu;
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'pol_BangCon') DROP SECURITY POLICY dbo.pol_BangCon;
GO

-- Quy tắc cho bảng NhanSus. Chỉ áp dụng cho tài khoản ứng dụng hrm_app,
-- các tài khoản khác (quản trị, báo cáo) không bị lọc theo dòng.
CREATE OR ALTER FUNCTION dbo.fn_Rls_NhanSu
    (@Id INT, @DonViId INT, @CongKhai BIT, @TrangThai NVARCHAR(MAX))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS Allowed
    WHERE USER_NAME() <> N'hrm_app'
       OR CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) IN (N'System', N'Administrator', N'HR')
       OR (CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) = N'Manager'
           AND @DonViId = TRY_CAST(CAST(SESSION_CONTEXT(N'DonViId') AS NVARCHAR(20)) AS INT))
       OR (CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) = N'Lecturer'
           AND @Id = TRY_CAST(CAST(SESSION_CONTEXT(N'NhanSuId') AS NVARCHAR(20)) AS INT))
       OR (CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) = N'Public'
           AND @CongKhai = 1 AND @TrangThai = N'DaXacNhan');
GO

-- Quy tắc cho các bảng con (giấy tờ, lương, thông tin nhạy cảm, hợp đồng):
-- chỉ HR được xem tất cả; Manager trong đơn vị; Lecturer hồ sơ của mình; khách và Admin không xem
CREATE OR ALTER FUNCTION dbo.fn_Rls_TheoNhanSu (@NhanSuId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS Allowed
    WHERE USER_NAME() <> N'hrm_app'
       OR CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) IN (N'System', N'HR')
       OR EXISTS (
            SELECT 1 FROM dbo.NhanSus n
            WHERE n.Id = @NhanSuId
              AND (
                   (CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) = N'Manager'
                    AND n.DonViId = TRY_CAST(CAST(SESSION_CONTEXT(N'DonViId') AS NVARCHAR(20)) AS INT))
                OR (CAST(SESSION_CONTEXT(N'Role') AS NVARCHAR(30)) = N'Lecturer'
                    AND n.Id = TRY_CAST(CAST(SESSION_CONTEXT(N'NhanSuId') AS NVARCHAR(20)) AS INT))
              ));
GO

CREATE SECURITY POLICY dbo.pol_NhanSu
    ADD FILTER PREDICATE dbo.fn_Rls_NhanSu(Id, DonViId, CongKhai, TrangThaiHoSo) ON dbo.NhanSus,
    ADD BLOCK PREDICATE dbo.fn_Rls_NhanSu(Id, DonViId, CongKhai, TrangThaiHoSo) ON dbo.NhanSus AFTER INSERT,
    ADD BLOCK PREDICATE dbo.fn_Rls_NhanSu(Id, DonViId, CongKhai, TrangThaiHoSo) ON dbo.NhanSus AFTER UPDATE
WITH (STATE = ON, SCHEMABINDING = ON);
GO

CREATE SECURITY POLICY dbo.pol_BangCon
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.GiayToDinhDanhs,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.GiayToDinhDanhs AFTER INSERT,
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.NangLuongs,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.NangLuongs AFTER INSERT,
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.ThongTinNhayCams,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.ThongTinNhayCams AFTER INSERT,
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.HopDongs,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.HopDongs AFTER INSERT
WITH (STATE = ON, SCHEMABINDING = ON);
GO

