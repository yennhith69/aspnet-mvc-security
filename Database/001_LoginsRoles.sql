-- 001_LoginsRoles.sql  (mục 1: đặc quyền tối thiểu)
-- Thay CHANGE_ME_* bằng mật khẩu thật trong SSMS, KHÔNG lưu mật khẩu thật vào file này.

USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hrm_app')
    CREATE LOGIN hrm_app WITH PASSWORD = N'CHANGE_ME_APP',
        DEFAULT_DATABASE = HrmSecure, CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hrm_report')
    CREATE LOGIN hrm_report WITH PASSWORD = N'CHANGE_ME_REPORT',
        DEFAULT_DATABASE = HrmSecure, CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'hrm_auditor')
    CREATE LOGIN hrm_auditor WITH PASSWORD = N'CHANGE_ME_AUDITOR',
        DEFAULT_DATABASE = HrmSecure, CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
GO

USE HrmSecure;
GO
IF DATABASE_PRINCIPAL_ID('hrm_app') IS NULL CREATE USER hrm_app FOR LOGIN hrm_app;
IF DATABASE_PRINCIPAL_ID('hrm_report') IS NULL CREATE USER hrm_report FOR LOGIN hrm_report;
IF DATABASE_PRINCIPAL_ID('hrm_auditor') IS NULL CREATE USER hrm_auditor FOR LOGIN hrm_auditor;

IF DATABASE_PRINCIPAL_ID('role_hrm_app') IS NULL CREATE ROLE role_hrm_app;
IF DATABASE_PRINCIPAL_ID('role_hrm_report') IS NULL CREATE ROLE role_hrm_report;
IF DATABASE_PRINCIPAL_ID('role_hrm_auditor') IS NULL CREATE ROLE role_hrm_auditor;

-- Gán user vào role, không cấp quyền trực tiếp cho user
ALTER ROLE role_hrm_app ADD MEMBER hrm_app;
ALTER ROLE role_hrm_report ADD MEMBER hrm_report;
ALTER ROLE role_hrm_auditor ADD MEMBER hrm_auditor;
GO

-- Ứng dụng: đọc, thêm, sửa; KHÔNG xóa
GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO role_hrm_app;
DENY DELETE ON SCHEMA::dbo TO role_hrm_app;

-- Nhật ký chỉ ghi thêm, ứng dụng không sửa hoặc xóa được
DENY UPDATE, DELETE ON dbo.LoginLogs TO role_hrm_app;
DENY UPDATE, DELETE ON dbo.AuditLogs TO role_hrm_app;
DENY UPDATE, DELETE ON dbo.LichSuMatKhaus TO role_hrm_app;

-- Kiểm toán viên: chỉ đọc nhật ký
GRANT SELECT ON dbo.LoginLogs TO role_hrm_auditor;
GRANT SELECT ON dbo.AuditLogs TO role_hrm_auditor;
GO