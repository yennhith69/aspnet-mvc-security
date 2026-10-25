-- 003_Masking.sql (mục 4). Chạy bằng tài khoản quản trị (Windows Authentication).
USE HrmSecure;
GO

ALTER TABLE dbo.NhanSus ALTER COLUMN Email ADD MASKED WITH (FUNCTION = 'email()');
ALTER TABLE dbo.NhanSus ALTER COLUMN DienThoai ADD MASKED WITH (FUNCTION = 'partial(2, "XXXXXX", 2)');
ALTER TABLE dbo.NhanSus ALTER COLUMN DiaChiCuTru ADD MASKED WITH (FUNCTION = 'default()');
ALTER TABLE dbo.GiayToDinhDanhs ALTER COLUMN So ADD MASKED WITH (FUNCTION = 'partial(0, "XXXXXXXX", 3)');
ALTER TABLE dbo.NangLuongs ALTER COLUMN MucLuong ADD MASKED WITH (FUNCTION = 'default()');
ALTER TABLE dbo.ThongTinNhayCams ALTER COLUMN GiaDinh ADD MASKED WITH (FUNCTION = 'default()');
ALTER TABLE dbo.ThongTinNhayCams ALTER COLUMN SucKhoe ADD MASKED WITH (FUNCTION = 'default()');
GO

-- Vai trò được xem bản rõ (UNMASK). Ứng dụng cần đọc dữ liệu thật để HR sửa hồ sơ.
IF DATABASE_PRINCIPAL_ID('role_hrm_unmask') IS NULL CREATE ROLE role_hrm_unmask;
GRANT UNMASK TO role_hrm_unmask;
ALTER ROLE role_hrm_unmask ADD MEMBER hrm_app;
GO

-- Người dùng thử nghiệm KHÔNG có UNMASK và không có login (dùng EXECUTE AS để so sánh)
IF DATABASE_PRINCIPAL_ID('hrm_masked_demo') IS NULL CREATE USER hrm_masked_demo WITHOUT LOGIN;
GRANT SELECT ON dbo.NhanSus TO hrm_masked_demo;
GRANT SELECT ON dbo.GiayToDinhDanhs TO hrm_masked_demo;
GRANT SELECT ON dbo.NangLuongs TO hrm_masked_demo;
GRANT SELECT ON dbo.ThongTinNhayCams TO hrm_masked_demo;
GO