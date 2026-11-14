# Karaoke404

Ứng dụng quản lý karaoke trên Windows, hỗ trợ quản lý phòng, dịch vụ, đặt phòng, thanh toán và báo cáo doanh thu.

## Tải ứng dụng

👉 [Tải KaraokeSetup.exe](https://github.com/NualCadd/Karaoke/releases/latest)

## Yêu cầu hệ thống

- Windows 10 hoặc Windows 11
- .NET Framework 4.7.2
- SQL Server Express LocalDB
- Windows Authentication

## Cài đặt

1. Tải file `KaraokeSetup.exe`.
2. Nhấp chuột phải vào file và chọn **Run as administrator**.
3. Làm theo hướng dẫn trên màn hình.
4. Bộ cài đặt sẽ cài ứng dụng và khởi tạo cơ sở dữ liệu `QuanLyKaraoke`.
5. Sau khi cài đặt hoàn tất, mở ứng dụng từ shortcut trên Desktop hoặc Start Menu.

## Tài khoản demo

> [!IMPORTANT]
> Đây là tài khoản dùng cho mục đích kiểm thử. Không sử dụng thông tin này trong môi trường thực tế.

| Thông tin | Giá trị |
|---|---|
| Tài khoản | `admin` |
| Mật khẩu | `AdminKaraoke2026!` |
| Vai trò | `Admin` |

Sau khi đăng nhập, nên đổi mật khẩu để bảo đảm an toàn.

## Chức năng chính

- Đăng nhập tài khoản Admin và Nhân viên.
- Quản lý phòng và loại phòng.
- Quản lý dịch vụ.
- Đặt phòng và nhận phòng.
- Gọi dịch vụ.
- Thanh toán và lập hóa đơn.
- Xem báo cáo, thống kê doanh thu.
- Quản lý tài khoản nhân viên.

## Lưu ý

Ứng dụng sử dụng SQL Server Express LocalDB nên dữ liệu được lưu cục bộ trên từng máy.

Phiên bản hiện tại chưa hỗ trợ:

- Đặt phòng trực tuyến.
- Kết nối nhiều máy trong mạng.
- Quản lý nhiều chi nhánh.
- Ứng dụng di động.
- Tài khoản đăng nhập cho khách hàng.

Chỉ tài khoản Admin và Nhân viên được phép đăng nhập. Thông tin khách hàng được nhân viên ghi nhận khi đặt phòng hoặc sử dụng dịch vụ.

## Xử lý lỗi thường gặp

Nếu không đăng nhập được, hãy kiểm tra:

- SQL Server LocalDB đã được cài đặt.
- Instance `MSSQLLocalDB` đang hoạt động.
- Cơ sở dữ liệu `QuanLyKaraoke` đã được khởi tạo.
- File cài đặt đã được chạy bằng quyền Administrator.

## Mã nguồn

Mã nguồn, file SQL và script khởi tạo cơ sở dữ liệu được lưu trong repository này.
