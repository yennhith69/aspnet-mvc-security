-- 008_AccountMonitoring.sql (Mục 8: Giám sát tài khoản người dùng và hệ thống)
-- Chạy bằng tài khoản quản trị (Windows Authentication).

USE master;
GO

-- 1. Giám sát trạng thái các tài khoản SQL: chính sách mật khẩu, hạn dùng, trạng thái bị vô hiệu hóa
SELECT 
    name AS TenLogin,
    is_disabled AS DaBiVoHieuHoa,
    is_policy_checked AS BatChinhSachMatKhau,
    is_expiration_checked AS BatHetHanMatKhau,
    create_date AS NgayTao,
    modify_date AS NgaySua
FROM sys.sql_logins
WHERE name IN ('sa', 'hrm_app', 'hrm_report', 'hrm_auditor');
GO

-- 2. Giám sát thành viên của role quản trị cao nhất mức Server (sysadmin)
SELECT 
    r.name AS ServerRole,
    m.name AS ThanhVien
FROM sys.server_role_members rm
JOIN sys.server_principals r ON rm.role_principal_id = r.principal_id
JOIN sys.server_principals m ON rm.member_principal_id = m.principal_id
WHERE r.name = 'sysadmin';
GO