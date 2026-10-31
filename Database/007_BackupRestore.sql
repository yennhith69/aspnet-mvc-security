-- 007_BackupRestore.sql (Mục 7: Bảo mật sao lưu và khôi phục CSDL)
-- Chạy bằng tài khoản quản trị (Windows Authentication), SAU 006_TDE.sql.
-- Thay CHANGE_ME_* trong SSMS, KHÔNG lưu mật khẩu thật vào file này.
-- Chạy MỘT LẦN: nếu chạy lại, xóa HrmBackupCert.cer/.pvk cũ trong D:\SQLKeys trước.

USE master;
GO
-- 1. Chứng chỉ RIÊNG để mã hóa tệp sao lưu (tách khỏi chứng chỉ TDE), sao lưu khóa ngay
IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE name = N'HrmBackupCert')
BEGIN
    CREATE CERTIFICATE HrmBackupCert WITH SUBJECT = N'HrmSecure backup encryption', EXPIRY_DATE = '2030-12-31';

    BACKUP CERTIFICATE HrmBackupCert TO FILE = N'D:\SQLKeys\HrmBackupCert.cer'
        WITH PRIVATE KEY (FILE = N'D:\SQLKeys\HrmBackupCert.pvk',
                          ENCRYPTION BY PASSWORD = N'CHANGE_ME_BACKUPCERT');
END
GO

-- 2. Recovery FULL để có chuỗi sao lưu đầy đủ
ALTER DATABASE HrmSecure SET RECOVERY FULL;
GO

-- 3. Sao lưu toàn phần, mã hóa AES-256, kiểm tra checksum
BACKUP DATABASE HrmSecure
TO DISK = N'D:\SQLBackup\HrmSecure_Full.bak'
WITH FORMAT, INIT, CHECKSUM, STATS = 10,
     NAME = N'HrmSecure-Full (encrypted)',
     ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);
GO

-- 4. Sao lưu vi sai
BACKUP DATABASE HrmSecure
TO DISK = N'D:\SQLBackup\HrmSecure_Diff.bak'
WITH DIFFERENTIAL, FORMAT, INIT, CHECKSUM, STATS = 10,
     NAME = N'HrmSecure-Differential (encrypted)',
     ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);
GO

-- 5. Sao lưu nhật ký giao dịch
BACKUP LOG HrmSecure
TO DISK = N'D:\SQLBackup\HrmSecure_Log.trn'
WITH FORMAT, INIT, CHECKSUM, STATS = 10,
     NAME = N'HrmSecure-Log (encrypted)',
     ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);
GO

-- 6. Kiểm tra tệp sao lưu đọc được và không hỏng
RESTORE VERIFYONLY FROM DISK = N'D:\SQLBackup\HrmSecure_Full.bak' WITH CHECKSUM;
GO

-- 7. Khôi phục thử sang CSDL tạm để chứng minh bản sao lưu dùng được
DECLARE @dataDir NVARCHAR(500) = CONVERT(NVARCHAR(500), SERVERPROPERTY('InstanceDefaultDataPath'));
DECLARE @logDir  NVARCHAR(500) = CONVERT(NVARCHAR(500), SERVERPROPERTY('InstanceDefaultLogPath'));
DECLARE @dataName SYSNAME = (SELECT name FROM sys.master_files WHERE database_id = DB_ID(N'HrmSecure') AND type = 0);
DECLARE @logName  SYSNAME = (SELECT name FROM sys.master_files WHERE database_id = DB_ID(N'HrmSecure') AND type = 1);
DECLARE @sql NVARCHAR(MAX) =
    N'RESTORE DATABASE HrmSecure_RestoreTest FROM DISK = N''D:\SQLBackup\HrmSecure_Full.bak'' ' +
    N'WITH MOVE N''' + @dataName + N''' TO N''' + @dataDir + N'HrmSecure_RestoreTest.mdf'', ' +
    N'MOVE N''' + @logName + N''' TO N''' + @logDir + N'HrmSecure_RestoreTest_log.ldf'', ' +
    N'RECOVERY, STATS = 10;';
EXEC sys.sp_executesql @sql;
GO

-- 8. So sánh số hồ sơ của bản gốc và bản khôi phục
SELECT (SELECT COUNT(*) FROM HrmSecure.dbo.NhanSus) AS HoSoGoc,
       (SELECT COUNT(*) FROM HrmSecure_RestoreTest.dbo.NhanSus) AS HoSoBanKhoiPhuc;
GO

-- 9. Dọn dẹp CSDL tạm
DROP DATABASE HrmSecure_RestoreTest;
GO

-- 10. Lịch sử sao lưu: cột ThuatToanMaHoa phải có AES_256
USE msdb;
GO
SELECT bs.database_name AS CSDL, bs.backup_finish_date AS ThoiGianHoanThanh,
       CASE bs.type WHEN 'D' THEN 'Full' WHEN 'I' THEN 'Differential' WHEN 'L' THEN 'Log' END AS LoaiBackup,
       bs.key_algorithm AS ThuatToanMaHoa, bmf.physical_device_name AS DuongDanTep
FROM dbo.backupset bs
JOIN dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
WHERE bs.database_name = N'HrmSecure'
ORDER BY bs.backup_finish_date DESC;
GO