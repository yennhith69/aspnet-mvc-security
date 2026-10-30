-- 005_SqlServerAudit.sql (Mục 5: SQL Server Audit & Mục 8: Giám sát đăng nhập thất bại)
-- Chạy bằng tài khoản quản trị (Windows Authentication).

USE master;
GO

-- 1. Tạo Server Audit ghi ra thư mục D:\SQLAudit\
IF EXISTS (SELECT 1 FROM sys.server_audits WHERE name = 'HrmServerAudit')
BEGIN
    ALTER SERVER AUDIT HrmServerAudit WITH (STATE = OFF);
    DROP SERVER AUDIT HrmServerAudit;
END
GO

CREATE SERVER AUDIT HrmServerAudit
TO FILE (
    FILEPATH = 'D:\SQLAudit\',
    MAXSIZE = 50 MB,
    MAX_ROLLOVER_FILES = 10,
    RESERVE_DISK_SPACE = OFF
)
WITH (
    QUEUE_DELAY = 1000,
    ON_FAILURE = CONTINUE
);
GO

-- Bật Server Audit
ALTER SERVER AUDIT HrmServerAudit WITH (STATE = ON);
GO

-- 2. Ghi nhận đăng nhập thất bại ở mức Server (Mục 8)
IF EXISTS (SELECT 1 FROM sys.server_audit_specifications WHERE name = 'HrmServerAuditSpec')
BEGIN
    ALTER SERVER AUDIT SPECIFICATION HrmServerAuditSpec WITH (STATE = OFF);
    DROP SERVER AUDIT SPECIFICATION HrmServerAuditSpec;
END
GO

CREATE SERVER AUDIT SPECIFICATION HrmServerAuditSpec
FOR SERVER AUDIT HrmServerAudit
    ADD (FAILED_LOGIN_GROUP)
WITH (STATE = ON);
GO

-- 3. Tạo Database Audit Specification trên database HrmSecure (Mục 5)
USE HrmSecure;
GO

IF EXISTS (SELECT 1 FROM sys.database_audit_specifications WHERE name = 'HrmDbAuditSpec')
BEGIN
    ALTER DATABASE AUDIT SPECIFICATION HrmDbAuditSpec WITH (STATE = OFF);
    DROP DATABASE AUDIT SPECIFICATION HrmDbAuditSpec;
END
GO

CREATE DATABASE AUDIT SPECIFICATION HrmDbAuditSpec
FOR SERVER AUDIT HrmServerAudit
    -- Giám sát thao tác đọc/ghi trên các bảng nhạy cảm
    ADD (SELECT, INSERT, UPDATE, DELETE ON dbo.GiayToDinhDanhs BY public),
    ADD (SELECT, INSERT, UPDATE, DELETE ON dbo.NangLuongs BY public),
    ADD (SELECT, INSERT, UPDATE, DELETE ON dbo.ThongTinNhayCams BY public),
    -- Giám sát thay đổi cấu trúc bảng/view/procedure (lệnh DDL)
    ADD (SCHEMA_OBJECT_CHANGE_GROUP)
WITH (STATE = ON);
GO

-- 4. hrm_auditor KHÔNG được cấp quyền mức server (không dùng CONTROL SERVER, mục 1).
--    hrm_auditor chỉ đọc bảng LoginLogs, AuditLogs (GRANT SELECT ở 001_LoginsRoles.sql).
--    File audit (.sqlaudit) do tài khoản quản trị Windows đọc bằng sys.fn_get_audit_file.
USE master;
GO
REVOKE CONTROL SERVER FROM hrm_auditor;   -- an toàn khi chạy lại nhiều lần
GO