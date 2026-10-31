-- 006_TDE.sql (Mục 6: Mã hóa cơ sở dữ liệu - Transparent Data Encryption)
-- Chạy bằng tài khoản quản trị (Windows Authentication) trên instance Developer/Enterprise.
-- Thay CHANGE_ME_* trong SSMS, KHÔNG lưu mật khẩu thật vào file này.
-- Chạy MỘT LẦN: nếu chạy lại, xóa các tệp .cer/.pvk cũ trong D:\SQLKeys trước.

USE master;
GO
-- 1. Khóa chủ của database master (bảo vệ khóa riêng của chứng chỉ)
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = N'CHANGE_ME_MASTERKEY';
GO

-- 2. Chứng chỉ bảo vệ khóa mã hóa dữ liệu, rồi SAO LƯU NGAY chứng chỉ và khóa riêng
IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE name = N'HrmTdeCert')
BEGIN
    CREATE CERTIFICATE HrmTdeCert WITH SUBJECT = N'HrmSecure TDE certificate', EXPIRY_DATE = '2030-12-31';

    BACKUP CERTIFICATE HrmTdeCert TO FILE = N'D:\SQLKeys\HrmTdeCert.cer'
        WITH PRIVATE KEY (FILE = N'D:\SQLKeys\HrmTdeCert.pvk',
                          ENCRYPTION BY PASSWORD = N'CHANGE_ME_TDECERT');
END
GO

-- 3. Khóa mã hóa database (AES-256) và bật mã hóa
USE HrmSecure;
GO
IF NOT EXISTS (SELECT 1 FROM sys.dm_database_encryption_keys WHERE database_id = DB_ID(N'HrmSecure'))
    CREATE DATABASE ENCRYPTION KEY
        WITH ALGORITHM = AES_256
        ENCRYPTION BY SERVER CERTIFICATE HrmTdeCert;
GO
ALTER DATABASE HrmSecure SET ENCRYPTION ON;
GO

-- 4. Kiểm tra trạng thái (3 = đã mã hóa; 2 = đang mã hóa, chạy lại sau vài giây)
SELECT DB_NAME(database_id) AS CSDL,
       encryption_state,
       CASE encryption_state
            WHEN 1 THEN N'Chưa mã hóa' WHEN 2 THEN N'Đang mã hóa' WHEN 3 THEN N'Đã mã hóa'
            WHEN 4 THEN N'Đang đổi khóa' WHEN 5 THEN N'Đang giải mã' ELSE N'Khác' END AS TrangThai,
       percent_complete AS PhanTram, key_algorithm AS ThuatToan, key_length AS DoDaiKhoa
FROM sys.dm_database_encryption_keys;
GO
SELECT name AS CSDL, is_encrypted AS DaMaHoa FROM sys.databases WHERE name IN (N'HrmSecure', N'tempdb');
GO