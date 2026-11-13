# Karaoke404

Ứng dụng quản lý karaoke trên Windows, hỗ trợ quản lý phòng, dịch vụ, đặt phòng, thanh toán và báo cáo doanh thu.

## Tải ứng dụng

Tải bộ cài đặt tại:

[Download KaraokeSetup.exe](https://github.com/NualCadd/Karaoke/releases/latest)

## Yêu cầu hệ thống

- .NET Framework 4.7.2
- SQL Server Express LocalDB
- Kết nối Windows Authentication

## Cài đặt

1. Tải file `KaraokeSetup.exe`.
2. Nhấp chuột phải vào file và chọn **Run as administrator**.
3. Làm theo hướng dẫn trên màn hình.
4. Bộ cài đặt sẽ cài ứng dụng và khởi tạo cơ sở dữ liệu `QuanLyKaraoke`.
5. Sau khi hoàn tất, ứng dụng sẽ tự động mở kèm **tài khoản admin**.

## Đăng nhập mặc định

Thông tin tài khoản mặc định được tạo trong quá trình khởi tạo cơ sở dữ liệu.

> Nếu tài khoản mặc định không đăng nhập được, hãy kiểm tra quá trình chạy file SQL và kết nối đến SQL Server LocalDB.

## Lưu ý

Ứng dụng hiện sử dụng SQL Server Express LocalDB nên dữ liệu được lưu cục bộ trên từng máy. Ứng dụng chưa hỗ trợ đặt phòng trực tuyến, kết nối nhiều máy hoặc quản lý nhiều chi nhánh.

Chỉ tài khoản Admin và Nhân viên được phép đăng nhập. Khách hàng không cần tạo tài khoản.

## Mã nguồn

Mã nguồn và các file SQL được lưu trong repository này.
