-- 010_LabAccidentalDeleteRestore.sql (Mục 24: xóa nhầm, khôi phục, ứng dụng mất kết nối)
-- Chạy bằng tài khoản quản trị. Chạy TỪNG KHỐI, giữa các khối kiểm tra ứng dụng như hướng dẫn.
-- Lưu ý: tài khoản ứng dụng hrm_app bị DENY DELETE nên "xóa nhầm" phải do quản trị DB thực hiện.

-- ========== KHỐI 0: dữ liệu mẫu và số liệu ban đầu (ảnh A) ==========
USE HrmSecure;
GO
INSERT INTO dbo.HopDongs (NhanSuId, Loai, TuNgay, DenNgay, TrangThai) VALUES
 (1, N'XacDinhThoiHan', '2026-01-01', '2027-01-01', N'HieuLuc'),
 (2, N'GiangThu',       '2026-06-01', '2026-12-01', N'HieuLuc'),
 (3, N'TapSu',          '2026-09-01', '2026-12-31', N'HieuLuc');
GO
SELECT (SELECT COUNT(*) FROM dbo.NhanSus) AS NhanSu, (SELECT COUNT(*) FROM dbo.GiayToDinhDanhs) AS GiayTo,
       (SELECT COUNT(*) FROM dbo.HopDongs) AS HopDong, (SELECT COUNT(*) FROM dbo.AspNetUsers) AS TaiKhoan,
       (SELECT COUNT(*) FROM dbo.Quyens) AS Quyen;
GO

-- ========== KHỐI 1: sao lưu Full (ứng dụng vẫn đang chạy, thử bấm vài trang trong lúc sao lưu) ==========
USE master;
GO
BACKUP DATABASE HrmSecure TO DISK = N'D:\SQLBackup\Lab_Full.bak'
WITH FORMAT, INIT, CHECKSUM, STATS = 10,
     ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);
GO

-- ========== KHỐI 2: một thay đổi HỢP LỆ sau Full, rồi sao lưu Log lần 1 ==========
USE HrmSecure;
GO
INSERT INTO dbo.HopDongs (NhanSuId, Loai, TuNgay, DenNgay, TrangThai)
VALUES (1, N'TapSu', '2026-11-01', '2027-02-01', N'HieuLuc');   -- hợp đồng thứ 4
GO
USE master;
GO
BACKUP LOG HrmSecure TO DISK = N'D:\SQLBackup\Lab_Log1.trn'
WITH FORMAT, INIT, CHECKSUM, STATS = 10,
     ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);
GO

-- ========== KHỐI 3: "XÓA NHẦM" toàn bộ hợp đồng (giao dịch có đánh dấu), rồi sao lưu Log lần 2 ==========
USE HrmSecure;
GO
BEGIN TRAN XoaNham WITH MARK N'Xoa nham toan bo hop dong';
DELETE FROM dbo.HopDongs;
COMMIT;
GO
SELECT (SELECT COUNT(*) FROM dbo.HopDongs) AS HopDong_SauKhiXoa;   -- phải là 0 (ảnh B)
GO
USE master;
GO
BACKUP LOG HrmSecure TO DISK = N'D:\SQLBackup\Lab_Log2.trn'
WITH FORMAT, INIT, CHECKSUM, STATS = 10,
     ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = HrmBackupCert);
GO
-- DỪNG: vào ứng dụng, mở "Hợp đồng sắp hết hạn": danh sách trống vì dữ liệu đã bị xóa.

-- ========== KHỐI 4: bắt đầu khôi phục, database ở trạng thái RESTORING ==========
USE master;
GO
ALTER DATABASE HrmSecure SET SINGLE_USER WITH ROLLBACK IMMEDIATE;   -- ngắt kết nối của ứng dụng
GO
RESTORE DATABASE HrmSecure FROM DISK = N'D:\SQLBackup\Lab_Full.bak'
WITH REPLACE, NORECOVERY, STATS = 10;
GO
-- DỪNG và KIỂM TRA ỨNG DỤNG (đây là bằng chứng "mất kết nối trong lúc restore"):
--   tải lại một trang trong ứng dụng => trang 503 "Hệ thống tạm thời chưa sẵn sàng"
--   mở https://localhost:7236/health => Unhealthy

-- ========== KHỐI 5: áp dụng Log, dừng TRƯỚC thời điểm xóa nhầm ==========
RESTORE LOG HrmSecure FROM DISK = N'D:\SQLBackup\Lab_Log1.trn' WITH NORECOVERY, STATS = 10;
GO
RESTORE LOG HrmSecure FROM DISK = N'D:\SQLBackup\Lab_Log2.trn'
WITH STOPBEFOREMARK = N'XoaNham', RECOVERY, STATS = 10;
GO
ALTER DATABASE HrmSecure SET MULTI_USER;
GO

-- ========== KHỐI 6: kiểm tra dữ liệu nhất quán (ảnh C) ==========
USE HrmSecure;
GO
SELECT (SELECT COUNT(*) FROM dbo.NhanSus) AS NhanSu, (SELECT COUNT(*) FROM dbo.GiayToDinhDanhs) AS GiayTo,
       (SELECT COUNT(*) FROM dbo.HopDongs) AS HopDong, (SELECT COUNT(*) FROM dbo.AspNetUsers) AS TaiKhoan,
       (SELECT COUNT(*) FROM dbo.Quyens) AS Quyen;     -- HopDong phải là 4, các số khác bằng ảnh A
GO
DBCC CHECKDB (HrmSecure) WITH NO_INFOMSGS;              -- phải hoàn thành, không có lỗi
GO
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;             -- phải trả về 0 dòng (không lệch khóa ngoại)
GO
SELECT name, is_rls_enabled = (SELECT COUNT(*) FROM sys.security_policies WHERE is_enabled = 1),
       is_encrypted FROM sys.databases WHERE name = N'HrmSecure';   -- RLS và TDE vẫn còn nguyên
GO