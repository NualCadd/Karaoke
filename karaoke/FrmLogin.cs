
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading.Tasks;
using karaoke.Services;
namespace karaoke
{
    public partial class FrmLogin : Form
    {
        private readonly AuthService authService =
            new AuthService();
        private Bitmap eyeIcon;
        private Bitmap eyeHiddenIcon;

        public FrmLogin()
        {
            InitializeComponent();
            eyeIcon = CreateEyeIcon(false);
            eyeHiddenIcon = CreateEyeIcon(true);
            brandIcon.Image = LoadLoginImage("login-karaoke-logo.png");
            forgotIcon.Image = LoadLoginImage("login-forgot-icon.png");
            txtMatKhau.IconRight = eyeIcon;
            passwordToolTip.SetToolTip(txtMatKhau, "Nhấn biểu tượng con mắt để hiện hoặc ẩn mật khẩu");
            this.AcceptButton = btnDangNhap;
            this.Shown += FrmLogin_Shown;
            txtTenDangNhap.MaxLength = 50; txtMatKhau.MaxLength = 72;
        }

        private async void FrmLogin_Shown(object sender, EventArgs e)
        {
            btnDangNhap.Enabled = false;
            lblThongBao.Text = "Đang kiểm tra cơ sở dữ liệu...";
            try
            {
                bool created = await Task.Run(() => LocalDatabaseSetup.EnsureCreated());
                if (IsDisposed || Disposing) return;
                lblThongBao.Text = "";
                if (created)
                    MessageBox.Show("Đã tạo cơ sở dữ liệu. Đăng nhập lần đầu bằng admin / AdminKaraoke2026!, sau đó đổi mật khẩu.",
                        "Thiết lập lần đầu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                lblThongBao.Text = "Không thể chuẩn bị cơ sở dữ liệu.";
                MessageBox.Show("Không thể chuẩn bị LocalDB. Hãy kiểm tra SQL Server Express LocalDB và kết nối.\n\n" + ex.GetBaseException().Message,
                    "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!IsDisposed && !Disposing) btnDangNhap.Enabled = true; }
        }

        private static Image LoadLoginImage(string fileName)
        {
            using (var stream = typeof(FrmLogin).Assembly.GetManifestResourceStream("karaoke.Images." + fileName))
            {
                if (stream == null) return null;
                using (var original = Image.FromStream(stream)) return new Bitmap(original);
            }
        }

        private void linkQuenMatKhau_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Nhân viên quên mật khẩu vui lòng liên hệ Admin để xác minh và đặt lại. Nếu quên mật khẩu Admin, hãy liên hệ người quản trị cơ sở dữ liệu.",
                "Quên mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static Bitmap CreateEyeIcon(bool crossed)
        {
            var bitmap = new Bitmap(28, 28);
            using (var graphics = Graphics.FromImage(bitmap))
            using (var pen = new Pen(Color.FromArgb(90, 104, 97), 2f))
            using (var path = new GraphicsPath())
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                path.AddBezier(3, 14, 8, 6, 20, 6, 25, 14);
                path.AddBezier(25, 14, 20, 22, 8, 22, 3, 14);
                graphics.DrawPath(pen, path);
                graphics.DrawEllipse(pen, 11, 11, 6, 6);
                if (crossed) graphics.DrawLine(pen, 5, 23, 23, 5);
            }
            return bitmap;
        }

        private void txtMatKhau_IconRightClick(object sender, EventArgs e)
        {
            bool show = txtMatKhau.PasswordChar != '\0';
            txtMatKhau.PasswordChar = show ? '\0' : '*';
            txtMatKhau.IconRight = show ? eyeHiddenIcon : eyeIcon;
            passwordToolTip.SetToolTip(txtMatKhau, show ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        }

        private void userIcon_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(22, 38, 28), 1.7f))
            {
                e.Graphics.DrawEllipse(pen, 8, 2, 8, 8);
                e.Graphics.DrawArc(pen, 4, 12, 16, 13, 190, 160);
                e.Graphics.DrawLine(pen, 5, 21, 19, 21);
            }
        }

        private void lockIcon_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(22, 38, 28), 1.7f))
            {
                e.Graphics.DrawArc(pen, 7, 2, 10, 13, 180, 180);
                e.Graphics.DrawRectangle(pen, 5, 10, 14, 11);
                e.Graphics.DrawLine(pen, 12, 14, 12, 17);
            }
        }

        private void brandPanel_Paint(object sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            var green = Color.FromArgb(0, 145, 62);
            using (var brush = new SolidBrush(green))
            using (var dark = new SolidBrush(Color.FromArgb(5, 49, 30)))
            using (var pen = new Pen(green, 3f))
            using (var titleFont = new Font("Segoe UI", 30f, FontStyle.Bold | FontStyle.Italic))
            {
                var format = StringFormat.GenericTypographic;
                float karaokeWidth = graphics.MeasureString("KARAOKE", titleFont, PointF.Empty, format).Width;
                float numberWidth = graphics.MeasureString("404", titleFont, PointF.Empty, format).Width;
                float start = (brandPanel.ClientSize.Width - karaokeWidth - numberWidth) / 2f;
                graphics.DrawString("KARAOKE", titleFont, dark, new PointF(start, 75), format);
                graphics.DrawString("404", titleFont, brush, new PointF(start + karaokeWidth, 75), format);
                graphics.DrawArc(pen, start + karaokeWidth + 7, 119, numberWidth - 12, 14, 185, 155);
            }
        }
        
        private async void btnDangNhap_Click(
            object sender, EventArgs e)
        {
            string tenDangNhap = txtTenDangNhap.Text.Trim();
            string matKhau = txtMatKhau.Text;

            lblThongBao.Text = "";

            if (tenDangNhap == "" || matKhau == "")
            {
                lblThongBao.Text =
                    "Vui lòng nhập tài khoản và mật khẩu!";
                return;
            }

            try
            {
                Enabled = false;
                btnDangNhap.Enabled = false;
                lblThongBao.Text = "Đang kiểm tra tài khoản...";

                
                LoginAttemptResult loginResult = await Task.Run(
                    () => authService.TryDangNhap(tenDangNhap, matKhau));
                if (IsDisposed || Disposing)
                {
                    karaoke.Helpers.UserSession.Clear();
                    return;
                }
                lblThongBao.Text = "";

                if (loginResult.Error != null)
                {
                    karaoke.Helpers.UserSession.Clear();
                    var databaseError = loginResult.Error.GetBaseException() as System.Data.SqlClient.SqlException;
                    string setupHint = databaseError != null && databaseError.Number == 4060
                        ? "Không mở được database QuanLyKaraoke. Với máy mới, chạy SetupLocalDb.ps1 trong thư mục project, sau đó mở lại ứng dụng. Nếu database đã có, kiểm tra quyền truy cập và kết nối LocalDB.\n\n"
                        : "Không thể kiểm tra đăng nhập. Hãy kiểm tra kết nối cơ sở dữ liệu rồi thử lại.\n\n";
                    MessageBox.Show(
                        setupHint +
                        loginResult.Error.GetBaseException().Message,
                        "Lỗi đăng nhập",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                if (!loginResult.Success)
                {
                    lblThongBao.Text =
                        "Không thể đăng nhập. Kiểm tra thông tin; nếu nhập sai nhiều lần, hãy chờ 15 phút hoặc liên hệ Admin.";
                    return;
                }

                
                txtMatKhau.Clear();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                karaoke.Helpers.UserSession.Clear();
                if (IsDisposed || Disposing) return;
                lblThongBao.Text = "";
                this.Show();
                MessageBox.Show(
                    "Không thể đăng nhập: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                if (!IsDisposed) { Enabled = true; btnDangNhap.Enabled = true; }
            }
        }
    }
}
