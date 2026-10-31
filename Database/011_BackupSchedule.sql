-- 011_BackupSchedule.sql (Mục 24: lịch sao lưu Full và Log). Chạy bằng tài khoản quản trị SAU bài lab 010.
USE msdb;
GO
IF EXISTS (SELECT 1 FROM dbo.sysjobs WHERE name = N'HrmSecure_Full_Daily') EXEC dbo.sp_delete_job @job_name = N'HrmSecure_Full_Daily';
IF EXISTS (SELECT 1 FROM dbo.sysjobs WHERE name = N'HrmSecure_Log_15min') EXEC dbo.sp_delete_job @job_name = N'HrmSecure_Log_15min';
GO

-- Job 1: Full mỗi ngày 02:00, tên tệp có dấu thời gian, mã hóa AES-256
EXEC dbo.sp_add_job @job_name = N'HrmSecure_Full_Daily', @description = N'Sao luu Full HrmSecure hang ngay, ma hoa AES-256';
EXEC dbo.sp_add_jobstep @job_name = N'HrmSecure_Full_Daily', @step_name = N'Full backup', @subsystem = N'TSQL',
     @database_name = N'master',
     @command = N'DECLARE @f NVARCHAR(260) = N''D:\SQLBackup\HrmSecure_Full_'' + FORMAT(SYSDATETIME(), ''yyyyMMdd_HHmmss'') + N''.bak'';
BACKUP DATABASE HrmSecure TO DISK = @f WITH INIT, CHECKSUM,
      ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);';
EXEC dbo.sp_add_jobschedule @job_name = N'HrmSecure_Full_Daily', @name = N'Hang ngay 02:00',
     @freq_type = 4, @freq_interval = 1, @active_start_time = 20000;
EXEC dbo.sp_add_jobserver @job_name = N'HrmSecure_Full_Daily', @server_name = N'(local)';
GO

-- Job 2: Log mỗi 15 phút
EXEC dbo.sp_add_job @job_name = N'HrmSecure_Log_15min', @description = N'Sao luu nhat ky giao dich HrmSecure moi 15 phut';
EXEC dbo.sp_add_jobstep @job_name = N'HrmSecure_Log_15min', @step_name = N'Log backup', @subsystem = N'TSQL',
     @database_name = N'master',
     @command = N'DECLARE @f NVARCHAR(260) = N''D:\SQLBackup\HrmSecure_Log_'' + FORMAT(SYSDATETIME(), ''yyyyMMdd_HHmmss'') + N''.trn'';
BACKUP LOG HrmSecure TO DISK = @f WITH INIT, CHECKSUM,
      ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);';
EXEC dbo.sp_add_jobschedule @job_name = N'HrmSecure_Log_15min', @name = N'Moi 15 phut',
     @freq_type = 4, @freq_interval = 1, @freq_subday_type = 4, @freq_subday_interval = 15, @active_start_time = 0;
EXEC dbo.sp_add_jobserver @job_name = N'HrmSecure_Log_15min', @server_name = N'(local)';
GO