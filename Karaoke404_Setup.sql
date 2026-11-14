IF OBJECT_ID(N'tempdb..#Karaoke404SetupOptions') IS NOT NULL
    DROP TABLE #Karaoke404SetupOptions;
CREATE TABLE #Karaoke404SetupOptions (TaoDuLieuMau bit NOT NULL);
-- Đổi 1 thành 0 để chỉ tạo cấu trúc, không nạp dữ liệu demo.
INSERT INTO #Karaoke404SetupOptions (TaoDuLieuMau) VALUES (1);
GO

-- ==================== CẤU TRÚC VÀ PHÂN QUYỀN ====================
-- Bao gồm các cột bảo mật mà code đăng nhập hiện tại yêu cầu.
-- Giữ nguyên bảng nhật ký tài khoản nếu có.
-- Chạy lại không đổi mật khẩu hoặc trạng thái Admin hiện có.
SET QUOTED_IDENTIFIER ON;
GO

-- PHẦN 1: BẢNG NGHIỆP VỤ
IF DB_ID(N'QuanLyKaraoke') IS NULL
BEGIN
    CREATE DATABASE QuanLyKaraoke;
END;
GO
USE QuanLyKaraoke;
GO

IF OBJECT_ID(N'dbo.NHANVIEN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NHANVIEN (
        MaNV VARCHAR(20) NOT NULL CONSTRAINT PK_NHANVIEN PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        NgaySinh DATE NULL,
        SDT VARCHAR(15) NULL,
        ChucVu NVARCHAR(50) NOT NULL,
        TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_NHANVIEN_TRANGTHAI DEFAULT N'Đang làm việc'
    );
END;
GO

IF OBJECT_ID(N'dbo.TAIKHOAN', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TAIKHOAN (
        MaTK INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TAIKHOAN PRIMARY KEY,
        MaNV VARCHAR(20) NOT NULL CONSTRAINT UQ_TAIKHOAN_MANV UNIQUE,
        TenDangNhap VARCHAR(50) NOT NULL CONSTRAINT UQ_TAIKHOAN_TENDANGNHAP UNIQUE,
        MatKhauHash VARCHAR(255) NOT NULL,
        VaiTro VARCHAR(20) NOT NULL,
        TrangThai BIT NOT NULL CONSTRAINT DF_TAIKHOAN_TRANGTHAI DEFAULT (1),
        CONSTRAINT FK_TAIKHOAN_NHANVIEN FOREIGN KEY (MaNV) REFERENCES dbo.NHANVIEN(MaNV),
        CONSTRAINT CK_TAIKHOAN_VAITRO CHECK (VaiTro IN ('Admin','NhanVien'))
    );
END;
GO

IF OBJECT_ID(N'dbo.LOAIPHONG', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LOAIPHONG (
        MaLoaiPhong INT NOT NULL CONSTRAINT PK_LOAIPHONG PRIMARY KEY,
        TenLoaiPhong NVARCHAR(50) NOT NULL,
        DonGiaGio DECIMAL(18,2) NOT NULL,
        CONSTRAINT CK_LOAIPHONG_DONGIA CHECK (DonGiaGio >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.PHONG', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PHONG (
        MaPhong VARCHAR(20) NOT NULL CONSTRAINT PK_PHONG PRIMARY KEY,
        TenPhong NVARCHAR(50) NOT NULL,
        MaLoaiPhong INT NOT NULL,
        SucChua INT NOT NULL,
        TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_PHONG_TRANGTHAI DEFAULT N'Trống',
        CONSTRAINT FK_PHONG_LOAIPHONG FOREIGN KEY (MaLoaiPhong) REFERENCES dbo.LOAIPHONG(MaLoaiPhong),
        CONSTRAINT CK_PHONG_SUCCHUA CHECK (SucChua > 0),
        CONSTRAINT CK_PHONG_TRANGTHAI CHECK (TrangThai IN (N'Trống',N'Đang sử dụng',N'Bảo trì'))
    );
END;
GO

IF OBJECT_ID(N'dbo.DICHVU', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DICHVU (
        MaDV VARCHAR(20) NOT NULL CONSTRAINT PK_DICHVU PRIMARY KEY,
        TenDV NVARCHAR(100) NOT NULL,
        DonViTinh NVARCHAR(30) NOT NULL,
        DonGia DECIMAL(18,2) NOT NULL,
        TrangThai BIT NOT NULL CONSTRAINT DF_DICHVU_TRANGTHAI DEFAULT(1),
        TonKho INT NOT NULL CONSTRAINT DF_DICHVU_TONKHO DEFAULT(0),
        CONSTRAINT CK_DICHVU_TONKHO CHECK (TonKho >= 0),
        CONSTRAINT CK_DICHVU_DONGIA CHECK (DonGia >= 0)
    );
END;
GO

-- Bổ sung tồn kho khi nâng cấp database cũ; chạy lại không đặt lại số lượng.
IF COL_LENGTH('dbo.DICHVU', 'TonKho') IS NULL
    EXEC(N'ALTER TABLE dbo.DICHVU ADD TonKho INT NOT NULL CONSTRAINT DF_DICHVU_TONKHO DEFAULT(0) WITH VALUES;');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.DICHVU') AND name = N'CK_DICHVU_TONKHO')
    EXEC(N'ALTER TABLE dbo.DICHVU WITH CHECK ADD CONSTRAINT CK_DICHVU_TONKHO CHECK (TonKho >= 0);');
GO

IF OBJECT_ID(N'dbo.PHIENHAT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PHIENHAT (
        MaPhien INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PHIENHAT PRIMARY KEY,
        MaPhong VARCHAR(20) NOT NULL,
        MaNV VARCHAR(20) NOT NULL,
        GioBatDau DATETIME2 NOT NULL CONSTRAINT DF_PHIENHAT_GIOBATDAU DEFAULT SYSDATETIME(),
        GioKetThuc DATETIME2 NULL,
        DonGiaGio DECIMAL(18,2) NOT NULL,
        TrangThai VARCHAR(20) NOT NULL CONSTRAINT DF_PHIENHAT_TRANGTHAI DEFAULT 'DangHat',
        CONSTRAINT FK_PHIENHAT_PHONG FOREIGN KEY (MaPhong) REFERENCES dbo.PHONG(MaPhong),
        CONSTRAINT FK_PHIENHAT_NHANVIEN FOREIGN KEY (MaNV) REFERENCES dbo.NHANVIEN(MaNV),
        CONSTRAINT CK_PHIENHAT_DONGIA CHECK (DonGiaGio >= 0),
        CONSTRAINT CK_PHIENHAT_TRANGTHAI CHECK (TrangThai IN ('DangHat','DaKetThuc')),
        CONSTRAINT CK_PHIENHAT_THOIGIAN CHECK (GioKetThuc IS NULL OR GioKetThuc >= GioBatDau)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PHIENHAT') AND name = N'UX_PHIENHAT_PHONG_DANGHAT'
)
BEGIN
    CREATE UNIQUE INDEX UX_PHIENHAT_PHONG_DANGHAT
    ON dbo.PHIENHAT(MaPhong)
    WHERE TrangThai = 'DangHat';
END;
GO

IF OBJECT_ID(N'dbo.CHITIETDICHVU', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CHITIETDICHVU (
        MaCTDV INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CHITIETDICHVU PRIMARY KEY,
        MaPhien INT NOT NULL,
        MaDV VARCHAR(20) NOT NULL,
        SoLuong INT NOT NULL,
        DonGia DECIMAL(18,2) NOT NULL,
        ThoiGianGoi DATETIME2 NOT NULL CONSTRAINT DF_CTDV_THOIGIANGOI DEFAULT SYSDATETIME(),
        CONSTRAINT FK_CTDV_PHIENHAT FOREIGN KEY (MaPhien) REFERENCES dbo.PHIENHAT(MaPhien),
        CONSTRAINT FK_CTDV_DICHVU FOREIGN KEY (MaDV) REFERENCES dbo.DICHVU(MaDV),
        CONSTRAINT CK_CTDV_SOLUONG CHECK (SoLuong > 0),
        CONSTRAINT CK_CTDV_DONGIA CHECK (DonGia >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.HOADON', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HOADON (
        MaHD INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HOADON PRIMARY KEY,
        MaPhien INT NOT NULL CONSTRAINT UQ_HOADON_MAPHIEN UNIQUE,
        MaNV VARCHAR(20) NOT NULL,
        NgayThanhToan DATETIME2 NOT NULL CONSTRAINT DF_HOADON_NGAYTT DEFAULT SYSDATETIME(),
        TienPhong DECIMAL(18,2) NOT NULL,
        TienDichVu DECIMAL(18,2) NOT NULL,
        GiamGia DECIMAL(18,2) NOT NULL CONSTRAINT DF_HOADON_GIAMGIA DEFAULT(0),
        ThanhTien DECIMAL(18,2) NOT NULL,
        PhuongThucTT NVARCHAR(30) NOT NULL,
        CONSTRAINT FK_HOADON_PHIENHAT FOREIGN KEY (MaPhien) REFERENCES dbo.PHIENHAT(MaPhien),
        CONSTRAINT FK_HOADON_NHANVIEN FOREIGN KEY (MaNV) REFERENCES dbo.NHANVIEN(MaNV),
        CONSTRAINT CK_HOADON_TIENPHONG CHECK (TienPhong >= 0),
        CONSTRAINT CK_HOADON_TIENDICHVU CHECK (TienDichVu >= 0),
        CONSTRAINT CK_HOADON_GIAMGIA CHECK (GiamGia >= 0),
        CONSTRAINT CK_HOADON_THANHTIEN CHECK (ThanhTien >= 0),
        CONSTRAINT CK_HOADON_TIEN CHECK (ThanhTien = TienPhong + TienDichVu - GiamGia)
    );
END;
GO
PRINT N'Da kiem tra/tao 8 bang cho QuanLyKaraoke.';
GO

-- PHẦN 2: ĐẶT PHÒNG
USE QuanLyKaraoke;
GO
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.DATPHONG') IS NULL
BEGIN
 CREATE TABLE dbo.DATPHONG (
 MaDatPhong int IDENTITY PRIMARY KEY, MaPhong varchar(20) NOT NULL REFERENCES dbo.PHONG(MaPhong),
 TenKhach nvarchar(100) NOT NULL, SDT varchar(15) NOT NULL, SoKhach int NOT NULL CHECK(SoKhach>0),
 GioBatDauDuKien datetime2 NOT NULL, GioKetThucDuKien datetime2 NOT NULL,
 DonGiaGioDaChot decimal(18,2) NOT NULL CHECK(DonGiaGioDaChot>=0),
 TrangThai varchar(20) NOT NULL CHECK(TrangThai IN ('ChoNhan','DaNhan','DaHuy')),
 MaNV varchar(20) NOT NULL REFERENCES dbo.NHANVIEN(MaNV), GhiChu nvarchar(500) NULL,
 CHECK(GioKetThucDuKien>GioBatDauDuKien));
 CREATE INDEX IX_DATPHONG_Lich ON dbo.DATPHONG(MaPhong,TrangThai,GioBatDauDuKien,GioKetThucDuKien);
END;
IF COL_LENGTH('dbo.PHIENHAT','MaDatPhong') IS NULL
 ALTER TABLE dbo.PHIENHAT ADD MaDatPhong int NULL REFERENCES dbo.DATPHONG(MaDatPhong);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_PHIENHAT_DATPHONG' AND object_id=OBJECT_ID('dbo.PHIENHAT'))
 EXEC('CREATE UNIQUE INDEX UX_PHIENHAT_DATPHONG ON dbo.PHIENHAT(MaDatPhong) WHERE MaDatPhong IS NOT NULL');
COMMIT;
GO

-- PHẦN 3: TÀI KHOẢN NỘI BỘ
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.TAIKHOAN','PhienBanBaoMat') IS NULL
 EXEC('ALTER TABLE dbo.TAIKHOAN ADD PhienBanBaoMat int NOT NULL CONSTRAINT DF_TK_SECURITY_VERSION DEFAULT(0)');
IF COL_LENGTH('dbo.TAIKHOAN','SoLanDangNhapSai') IS NULL
 EXEC('ALTER TABLE dbo.TAIKHOAN ADD SoLanDangNhapSai int NOT NULL CONSTRAINT DF_TK_FAILURES DEFAULT(0)');
IF COL_LENGTH('dbo.TAIKHOAN','KhoaDen') IS NULL
 ALTER TABLE dbo.TAIKHOAN ADD KhoaDen datetime2 NULL;
-- Giữ nguyên nhật ký tài khoản cũ, kể cả khi ứng dụng không còn sử dụng.
COMMIT;
GO

-- PHẦN 4: KHỞI TẠO ADMIN
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @TenDangNhap varchar(50)='admin';
 -- Mật khẩu khởi tạo: AdminKaraoke2026!; chỉ dùng khi chưa có tài khoản admin.
 -- Đổi mật khẩu ngay sau lần đăng nhập đầu tiên.
 DECLARE @Hash varchar(255)='$2a$11$t.titrtcM0Han36ubpOabevEhk3bQG6ofAO5itSe0I6dii7MILWue';
 IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WITH(UPDLOCK,HOLDLOCK) WHERE TenDangNhap=@TenDangNhap)
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV
                WHERE t.TenDangNhap=@TenDangNhap AND t.VaiTro='Admin')
   THROW 50002,N'Tên admin đã thuộc tài khoản khác. Đổi @TenDangNhap ở phần tạo Admin.',1;
  PRINT N'Admin đã tồn tại. Giữ nguyên mật khẩu và trạng thái hiện có.';
 END
 ELSE
 BEGIN
  DECLARE @MaNV varchar(20)='ADM'+LEFT(REPLACE(CONVERT(varchar(36),NEWID()),'-',''),17);
  INSERT dbo.NHANVIEN(MaNV,HoTen,NgaySinh,SDT,ChucVu,TrangThai)
  VALUES(@MaNV,N'Quản trị hệ thống',NULL,NULL,N'Admin',N'Đang làm việc');
  INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,VaiTro,TrangThai)
  VALUES(@MaNV,@TenDangNhap,@Hash,'Admin',1);
  PRINT N'Đã tạo tài khoản Admin.';
 END;
 COMMIT;
 SELECT MaTK,TenDangNhap,VaiTro,TrangThai FROM dbo.TAIKHOAN WHERE TenDangNhap=@TenDangNhap;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
GO

-- PHẦN 5: PHÂN QUYỀN
-- Nâng cấp phân quyền cho CSDL QuanLyKaraoke hiện có. Chạy trên đúng database.
-- Không xóa hay sửa tài khoản, mật khẩu, lịch đặt, hóa đơn hiện có.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.TAIKHOAN', N'U') IS NULL
        THROW 51000, N'Chưa có bảng TAIKHOAN. Hãy chọn CSDL Karaoke đã cài đặt.', 1;

    DECLARE @firstInstall bit = CASE WHEN OBJECT_ID(N'dbo.NHOMQUYEN', N'U') IS NULL THEN 1 ELSE 0 END;
    IF OBJECT_ID(N'dbo.NHOMQUYEN', N'U') IS NULL
        CREATE TABLE dbo.NHOMQUYEN (
            MaNhom int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NHOMQUYEN PRIMARY KEY,
            TenNhom nvarchar(100) NOT NULL CONSTRAINT UQ_NHOMQUYEN_TEN UNIQUE,
            LaHeThong bit NOT NULL CONSTRAINT DF_NHOMQUYEN_LAHETHONG DEFAULT (0)
        );
    IF OBJECT_ID(N'dbo.QUYEN', N'U') IS NULL
        CREATE TABLE dbo.QUYEN (
            MaQuyen varchar(50) NOT NULL CONSTRAINT PK_QUYEN PRIMARY KEY,
            TenQuyen nvarchar(100) NOT NULL
        );
    IF OBJECT_ID(N'dbo.NHOMQUYEN_CHITIET', N'U') IS NULL
        CREATE TABLE dbo.NHOMQUYEN_CHITIET (
            MaNhom int NOT NULL CONSTRAINT FK_NQCT_NHOM REFERENCES dbo.NHOMQUYEN(MaNhom),
            MaQuyen varchar(50) NOT NULL CONSTRAINT FK_NQCT_QUYEN REFERENCES dbo.QUYEN(MaQuyen),
            CONSTRAINT PK_NHOMQUYEN_CHITIET PRIMARY KEY (MaNhom, MaQuyen)
        );
    IF OBJECT_ID(N'dbo.TAIKHOAN_NHOMQUYEN', N'U') IS NULL
        CREATE TABLE dbo.TAIKHOAN_NHOMQUYEN (
            MaTK int NOT NULL CONSTRAINT PK_TAIKHOAN_NHOMQUYEN PRIMARY KEY CONSTRAINT FK_TKNQ_TK REFERENCES dbo.TAIKHOAN(MaTK),
            MaNhom int NOT NULL CONSTRAINT FK_TKNQ_NHOM REFERENCES dbo.NHOMQUYEN(MaNhom)
        );
    DECLARE @permissions TABLE (MaQuyen varchar(50) PRIMARY KEY, TenQuyen nvarchar(100));
    INSERT @permissions VALUES
      ('PHONG_XEM', N'Xem phòng'), ('PHONG_SUA', N'Quản lý phòng'),
      ('DICHVU_XEM', N'Xem dịch vụ'), ('DICHVU_SUA', N'Quản lý dịch vụ'),
      ('DATPHONG', N'Đặt và nhận phòng'), ('GOIDICHVU', N'Gọi dịch vụ'),
      ('THANHTOAN', N'Thanh toán'), ('THONGKE', N'Xem doanh thu'),
      ('NHANVIEN', N'Quản lý nhân viên'), ('TAIKHOAN', N'Quản lý tài khoản'),
      ('NHOMQUYEN', N'Quản lý nhóm quyền'), ('LOAIPHONG', N'Quản lý loại phòng');
    INSERT dbo.QUYEN(MaQuyen, TenQuyen)
      SELECT p.MaQuyen, p.TenQuyen FROM @permissions p WHERE NOT EXISTS (SELECT 1 FROM dbo.QUYEN q WHERE q.MaQuyen=p.MaQuyen);
    IF @firstInstall = 1
    BEGIN
        INSERT dbo.NHOMQUYEN(TenNhom, LaHeThong) VALUES (N'Admin', 1), (N'Nhân viên', 1);
        INSERT dbo.NHOMQUYEN_CHITIET(MaNhom, MaQuyen)
          SELECT n.MaNhom, q.MaQuyen FROM dbo.NHOMQUYEN n CROSS JOIN dbo.QUYEN q WHERE n.TenNhom=N'Admin';
        INSERT dbo.NHOMQUYEN_CHITIET(MaNhom, MaQuyen)
          SELECT n.MaNhom, q.MaQuyen FROM dbo.NHOMQUYEN n CROSS JOIN dbo.QUYEN q
          WHERE n.TenNhom=N'Nhân viên' AND q.MaQuyen IN ('PHONG_XEM','DICHVU_XEM','DATPHONG','GOIDICHVU','THANHTOAN');
        INSERT dbo.TAIKHOAN_NHOMQUYEN(MaTK, MaNhom)
          SELECT t.MaTK, n.MaNhom FROM dbo.TAIKHOAN t JOIN dbo.NHOMQUYEN n
            ON n.TenNhom=CASE WHEN t.VaiTro='Admin' THEN N'Admin' ELSE N'Nhân viên' END;
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO


-- KARAOKE404_SCHEMA_END
-- ==================== DỮ LIỆU DEMO TÙY CHỌN ====================
-- Chỉ nạp khi chưa có dữ liệu nghiệp vụ; chạy lại không thêm hóa đơn/ngày mới.
IF EXISTS (SELECT 1 FROM #Karaoke404SetupOptions WHERE TaoDuLieuMau = 1)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.LOAIPHONG)
       OR EXISTS (SELECT 1 FROM dbo.PHONG)
       OR EXISTS (SELECT 1 FROM dbo.DICHVU)
       OR EXISTS (SELECT 1 FROM dbo.DATPHONG)
       OR EXISTS (SELECT 1 FROM dbo.PHIENHAT)
       OR EXISTS (SELECT 1 FROM dbo.HOADON)
       OR EXISTS (SELECT 1 FROM dbo.NHANVIEN n WHERE NOT EXISTS (
           SELECT 1 FROM dbo.TAIKHOAN t WHERE t.MaNV = n.MaNV AND t.VaiTro = 'Admin'))
    BEGIN
        PRINT N'Bỏ qua demo: database đã có dữ liệu. Giữ nguyên dữ liệu hiện có.';
    END
    ELSE
    BEGIN
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.LOAIPHONG WHERE MaLoaiPhong = 1)
        INSERT INTO dbo.LOAIPHONG(MaLoaiPhong,TenLoaiPhong,DonGiaGio)
        VALUES(1,N'Phòng thường',80000);
    IF NOT EXISTS (SELECT 1 FROM dbo.LOAIPHONG WHERE MaLoaiPhong = 2)
        INSERT INTO dbo.LOAIPHONG(MaLoaiPhong,TenLoaiPhong,DonGiaGio)
        VALUES(2,N'Phòng VIP',150000);

    IF NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MaNV = 'NV001')
        INSERT INTO dbo.NHANVIEN(MaNV,HoTen,NgaySinh,SDT,ChucVu,TrangThai)
        VALUES('NV001',N'Nhân viên mẫu',NULL,NULL,N'Quản lý',N'Đang làm việc');

    DECLARE @i INT = 1;
    DECLARE @ma VARCHAR(20);
    WHILE @i <= 20
    BEGIN
        SET @ma = 'P' + RIGHT('000' + CAST(@i AS VARCHAR(3)),3);
        IF NOT EXISTS (SELECT 1 FROM dbo.PHONG WHERE MaPhong=@ma)
        BEGIN
            INSERT INTO dbo.PHONG(MaPhong,TenPhong,MaLoaiPhong,SucChua,TrangThai)
            VALUES (
                @ma,
                N'Phòng ' + CAST(100+@i AS NVARCHAR(10)),
                CASE WHEN @i % 4 = 0 THEN 2 ELSE 1 END,
                CASE WHEN @i % 4 = 0 THEN 12 ELSE 6 END,
                CASE WHEN @i IN (2,5,9) THEN N'Đang sử dụng' ELSE N'Trống' END
            );
        END;
        SET @i += 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.DICHVU WHERE MaDV='DV001')
        INSERT INTO dbo.DICHVU(MaDV,TenDV,DonViTinh,DonGia,TrangThai,TonKho)
        VALUES ('DV001',N'Nước suối',N'Chai',10000,1,100);
    IF NOT EXISTS (SELECT 1 FROM dbo.DICHVU WHERE MaDV='DV002')
        INSERT INTO dbo.DICHVU(MaDV,TenDV,DonViTinh,DonGia,TrangThai,TonKho)
        VALUES ('DV002',N'Coca-Cola',N'Lon',15000,1,100);
    IF NOT EXISTS (SELECT 1 FROM dbo.DICHVU WHERE MaDV='DV003')
        INSERT INTO dbo.DICHVU(MaDV,TenDV,DonViTinh,DonGia,TrangThai,TonKho)
        VALUES ('DV003',N'Trái cây',N'Đĩa',50000,1,100);
    IF NOT EXISTS (SELECT 1 FROM dbo.DICHVU WHERE MaDV='DV004')
        INSERT INTO dbo.DICHVU(MaDV,TenDV,DonViTinh,DonGia,TrangThai,TonKho)
        VALUES ('DV004',N'Khô bò',N'Gói',35000,1,100);
    IF NOT EXISTS (SELECT 1 FROM dbo.DICHVU WHERE MaDV='DV005')
        INSERT INTO dbo.DICHVU(MaDV,TenDV,DonViTinh,DonGia,TrangThai,TonKho)
        VALUES ('DV005',N'Bia',N'Lon',25000,1,100);

    -- 3 phien dang hat theo 3 phong da dat TrangThai = 'Dang su dung'.
    DECLARE @now DATETIME2 = SYSDATETIME();
    DECLARE @activeRoom VARCHAR(20);
    DECLARE active_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT MaPhong FROM (VALUES('P002'),('P005'),('P009')) A(MaPhong);
    OPEN active_cursor;
    FETCH NEXT FROM active_cursor INTO @activeRoom;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.PHONG WHERE MaPhong = @activeRoom AND TrangThai = N'Đang sử dụng')
           AND NOT EXISTS (SELECT 1 FROM dbo.PHIENHAT WHERE MaPhong = @activeRoom AND TrangThai = 'DangHat')
        BEGIN
            INSERT INTO dbo.PHIENHAT (MaPhong,MaNV,GioBatDau,GioKetThuc,DonGiaGio,TrangThai)
            SELECT @activeRoom,'NV001',DATEADD(MINUTE,-45,@now),NULL,lp.DonGiaGio,'DangHat'
            FROM dbo.PHONG p INNER JOIN dbo.LOAIPHONG lp ON p.MaLoaiPhong=lp.MaLoaiPhong
            WHERE p.MaPhong=@activeRoom;
        END;
        FETCH NEXT FROM active_cursor INTO @activeRoom;
    END;
    CLOSE active_cursor;
    DEALLOCATE active_cursor;

    -- 6 hoa don thuoc 6 thang truoc; ngay 15 moi thang. Khong tao ngay tuong lai.
    DECLARE @offset INT = 6;
    DECLARE @start DATETIME2;
    DECLARE @end DATETIME2;
    DECLARE @maPhien INT;
    DECLARE @dichVu DECIMAL(18,2);
    WHILE @offset >= 1
    BEGIN
        SET @start = DATEADD(HOUR,18,DATEADD(DAY,14,DATEADD(MONTH,-@offset,DATEADD(MONTH,DATEDIFF(MONTH,0,@now),0))));
        SET @end = DATEADD(HOUR,2,@start);
        SET @dichVu = CAST((7-@offset)*50000 AS DECIMAL(18,2));
        SET @maPhien = NULL;
        SELECT @maPhien = MaPhien FROM dbo.PHIENHAT WHERE MaPhong='P001' AND GioBatDau=@start;
        IF @maPhien IS NULL
        BEGIN
            INSERT INTO dbo.PHIENHAT(MaPhong,MaNV,GioBatDau,GioKetThuc,DonGiaGio,TrangThai)
            VALUES('P001','NV001',@start,@end,80000,'DaKetThuc');
            SET @maPhien = CONVERT(INT,SCOPE_IDENTITY());
            INSERT INTO dbo.CHITIETDICHVU(MaPhien,MaDV,SoLuong,DonGia,ThoiGianGoi)
            VALUES(@maPhien,'DV003',7-@offset,50000,@start);
            INSERT INTO dbo.HOADON(MaPhien,MaNV,NgayThanhToan,TienPhong,TienDichVu,GiamGia,ThanhTien,PhuongThucTT)
            VALUES(@maPhien,'NV001',@end,160000,@dichVu,0,160000+@dichVu,N'Tiền mặt');
        END;
        SET @offset -= 1;
    END;

    -- Hoa don hom nay: moc dau ngay co dinh de chay lai khong bi trung.
    -- Ket thuc tai thoi diem chay, khong tao thoi gian trong tuong lai.
    DECLARE @todayStart DATETIME2 = CAST(CAST(@now AS DATE) AS DATETIME2);
    DECLARE @todayEnd DATETIME2 = @now;
    DECLARE @todayHours DECIMAL(18,2) = ROUND(DATEDIFF(SECOND, @todayStart, @todayEnd) / 3600.0, 2);
    IF @todayHours < 0.25 SET @todayHours = 0.25;
    DECLARE @todayRoom DECIMAL(18,2) = ROUND(@todayHours * 80000, 0);
    SET @maPhien = NULL;
    SELECT @maPhien = MaPhien FROM dbo.PHIENHAT
    WHERE MaPhong='P003' AND GioBatDau=@todayStart;
    IF @maPhien IS NULL
    BEGIN
        INSERT INTO dbo.PHIENHAT(MaPhong,MaNV,GioBatDau,GioKetThuc,DonGiaGio,TrangThai)
        VALUES('P003','NV001',@todayStart,@todayEnd,80000,'DaKetThuc');
        SET @maPhien = CONVERT(INT,SCOPE_IDENTITY());
        INSERT INTO dbo.CHITIETDICHVU(MaPhien,MaDV,SoLuong,DonGia,ThoiGianGoi)
        VALUES(@maPhien,'DV003',13,50000,@todayStart), (@maPhien,'DV001',4,10000,@todayStart);
        INSERT INTO dbo.HOADON(MaPhien,MaNV,NgayThanhToan,TienPhong,TienDichVu,GiamGia,ThanhTien,PhuongThucTT)
        VALUES(@maPhien,'NV001',@now,@todayRoom,690000,0,@todayRoom+690000,N'Tiền mặt');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
        PRINT N'Đã tạo dữ liệu demo: 20 phòng, 5 dịch vụ có tồn kho và các phiên/hóa đơn mẫu.';
    END;
END
ELSE
    PRINT N'Chỉ khởi tạo cấu trúc; không nạp dữ liệu demo.';
GO
DROP TABLE #Karaoke404SetupOptions;
PRINT N'Hoàn tất thiết lập Karaoke404. Admin đã tồn tại được giữ nguyên mật khẩu.';
SELECT COUNT(*) AS TongPhong FROM dbo.PHONG;
SELECT COUNT(*) AS TongDichVu FROM dbo.DICHVU;
GO
