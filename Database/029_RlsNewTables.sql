-- 029_RlsNewTables.sql (RLS và masking cho các bảng nghiệp vụ mới)
-- Chạy bằng tài khoản quản trị sau 004_RowLevelSecurity.sql.
USE HrmSecure;
GO

ALTER SECURITY POLICY dbo.pol_BangCon
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.LichSuSucKhoes,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.LichSuSucKhoes AFTER INSERT,
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.ThanhVienGiaDinhs,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.ThanhVienGiaDinhs AFTER INSERT,
    ADD FILTER PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.KyLuats,
    ADD BLOCK PREDICATE dbo.fn_Rls_TheoNhanSu(NhanSuId) ON dbo.KyLuats AFTER INSERT;
GO

ALTER TABLE dbo.LichSuSucKhoes
    ALTER COLUMN TinhTrang ADD MASKED WITH (FUNCTION = 'default()');
ALTER TABLE dbo.LichSuSucKhoes
    ALTER COLUMN GhiChu ADD MASKED WITH (FUNCTION = 'default()');
ALTER TABLE dbo.ThanhVienGiaDinhs
    ALTER COLUMN HoTen ADD MASKED WITH (FUNCTION = 'default()');
GO

-- Kiểm tra nhanh các đối tượng đã được bảo vệ.
SELECT sp.name AS SecurityPolicy, so.name AS TableName, p.predicate_type_desc
FROM sys.security_predicates p
JOIN sys.security_policies sp ON sp.object_id = p.object_id
JOIN sys.objects so ON so.object_id = p.target_object_id
WHERE sp.name = N'pol_BangCon'
  AND so.name IN (N'LichSuSucKhoes', N'ThanhVienGiaDinhs', N'KyLuats');
GO
