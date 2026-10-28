-- 007_BackupRestore.sql (Mục 7: Bảo mật sao lưu và khôi phục CSDL)
-- Chạy bằng tài khoản quản trị (Windows Authentication).

USE master;
GO

-- 1. Chuyển database sang Recovery Model FULL để hỗ trợ chuỗi sao lưu đầy đủ
ALTER DATABASE HrmSecure SET RECOVERY FULL;
GO

-- 2. Sao lưu toàn phần (Full Backup)
BACKUP DATABASE HrmSecure
TO DISK = N'D:\SQLBackup\HrmSecure_Full.bak'
WITH FORMAT, INIT,
     NAME = N'HrmSecure-Full Database Backup',
     STATS = 10;
GO

-- 3. Sao lưu vi sai (Differential Backup)
BACKUP DATABASE HrmSecure
TO DISK = N'D:\SQLBackup\HrmSecure_Diff.bak'
WITH DIFFERENTIAL, FORMAT, INIT,
     NAME = N'HrmSecure-Differential Database Backup',
     STATS = 10;
GO

-- 4. Sao lưu nhật ký giao dịch (Transaction Log Backup)
BACKUP LOG HrmSecure
TO DISK = N'D:\SQLBackup\HrmSecure_Log.trn'
WITH FORMAT, INIT,
     NAME = N'HrmSecure-Transaction Log Backup',
     STATS = 10;
GO

-- 5. Xem lịch sử sao lưu đã ghi nhận trong msdb
USE msdb;
GO
SELECT 
    database_name AS CSDL,
    backup_start_date AS ThoiGianBatDau,
    backup_finish_date AS ThoiGianHoanThanh,
    CASE type 
        WHEN 'D' THEN 'Full'
        WHEN 'I' THEN 'Differential'
        WHEN 'L' THEN 'Transaction Log'
    END AS LoaiBackup,
    physical_device_name AS DuongDanTep
FROM backupset bs
JOIN backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
WHERE database_name = 'HrmSecure'
ORDER BY backup_finish_date DESC;
GO