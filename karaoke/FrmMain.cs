
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using karaoke.Models;
using karaoke.Services;
using karaoke.UI;

namespace karaoke
{
    public partial class FrmMain : Form
    {
        private Form currentChildForm = null;
        private bool staffDashboard;

        public FrmMain()
        {
            InitializeComponent();
            using (var iconStream = typeof(FrmMain).Assembly.GetManifestResourceStream("karaoke.Images.Karaoke404.ico"))
            {
                if (iconStream != null)
                {
                    using (var icon = new Icon(iconStream)) Icon = (Icon)icon.Clone();
                }
            }
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
            FormClosing += FrmMain_FormClosing;
            this.Load += FrmMain_Load;
            this.btnThanhToan.Click += btnThanhToan_Click;
            this.btnThongKe.Click += btnThongKe_Click;
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            var orderForm = currentChildForm as FrmGoiDichVu;
            if (e.CloseReason == CloseReason.UserClosing && orderForm != null && !orderForm.ConfirmLeave())
                e.Cancel = true;
        }

        private void personalAccount_Click(object sender, EventArgs e)
        {
            var profile = new FrmTaiKhoanCaNhan();
            profile.LogoutRequested += (s, args) => { DialogResult = DialogResult.Retry; Close(); };
            profile.ProfileRefreshed += (s, args) => ApplyAccountMode();
            if (OpenChildForm(profile)) SetActiveSidebar(personalAccount);
        }
        private void accountManagement_Click(object sender, EventArgs e)
        { OpenAdminPage("TAIKHOAN", "Quản lý tài khoản", () => new FrmQuanLyTaiKhoan(), accountManagement); }
        private void staffManagement_Click(object sender, EventArgs e)
        { OpenAdminPage("NHANVIEN", "Nhân viên", () => new FrmQuanLyNhanVien(), staffManagement); }
        private void roomTypes_Click(object sender, EventArgs e)
        { OpenAdminPage("LOAIPHONG", "Loại phòng", () => new FrmQuanLyLoaiPhong(), roomTypes); }
        private void roleManagement_Click(object sender, EventArgs e)
        { OpenAdminPage("NHOMQUYEN", "Nhóm quyền", () => new FrmQuanLyNhomQuyen(), roleManagement); }
        private void OpenAdminPage(string permission, string title, Func<Form> create, Guna.UI2.WinForms.Guna2Button button)
        {
            try
            {
                using (var db = new QuanLyKaraokeEntities()) AuthService.RequirePermission(db, permission);
                if (OpenChildForm(create())) SetActiveSidebar(button);
            }
            catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, title); }
        }

        

        private void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                if (AuthService.IsAdmin(AuthService.RequireCurrentUser())) new RoleService().EnsureManagerRole();
                ApplyAccountMode();
                LoadDashboard();
                LoadChartDoanhThu();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi tải Dashboard: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        

        private void ApplyAccountMode()
        {
            bool canManageStaff;
            using (var db = new QuanLyKaraokeEntities())
                canManageStaff = AuthService.CanManageStaff(db, AuthService.RequireCurrentUser(db));
            staffDashboard = !canManageStaff;
            accountManagement.Visible = canManageStaff && AuthService.HasPermission("TAIKHOAN");
            staffManagement.Visible = canManageStaff && AuthService.HasPermission("NHANVIEN");
            roomTypes.Visible = AuthService.HasPermission("LOAIPHONG");
            roleManagement.Visible = AuthService.HasPermission("NHOMQUYEN");
            btnPhong.Enabled = AuthService.HasPermission("PHONG_XEM");
            btnDichVu.Enabled = AuthService.HasPermission("DICHVU_XEM");
            btnDatPhong.Enabled = AuthService.HasPermission("DATPHONG");
            btnGoiDichVu.Enabled = AuthService.HasPermission("GOIDICHVU");
            btnThanhToan.Enabled = AuthService.HasPermission("THANHTOAN");
            bool canViewStatistics = AuthService.HasPermission("THONGKE");
            bool showStatistics = !staffDashboard && canViewStatistics;
            btnThongKe.Visible = btnThongKe.Enabled = showStatistics;
            personalAccount.Top = showStatistics ? 495 : 440;
            chart1.Visible = lbl4.Visible = label11.Visible = staffDashboard || canViewStatistics;
            label11.Text = staffDashboard ? "Doanh thu hôm nay (" + DateTime.Today.ToString("dd/MM/yyyy") + ")" : "Doanh thu theo tháng";
            if (!staffDashboard && !canViewStatistics) { lblDoanhThu.Text = ""; chart1.Series.Clear(); }
        }
        private bool OpenInternal(Func<Form> factory, bool adminOnly = false)
        {
            try {
                using (var db = new QuanLyKaraokeEntities())
                { if (adminOnly) AuthService.RequireAdmin(db); else AuthService.RequireCurrentUser(db); }
                return OpenChildForm(factory());
            }
            catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Tài khoản nội bộ"); return false; }
        }
        private void LoadDashboard()
        {
            using (var db = new QuanLyKaraokeEntities())
            {
                var account = AuthService.RequireCurrentUser(db);
                lblTongPhong.Text = lblDangSuDung.Text = lblPhongTrong.Text = "—";
                lblDoanhThu.Text = "";
                if (AuthService.HasPermission("PHONG_XEM"))
                {
                    lblTongPhong.Text = db.PHONGs.Count().ToString();
                    lblDangSuDung.Text = db.PHONGs.Count(x => x.TrangThai == "Đang sử dụng").ToString();
                    lblPhongTrong.Text = db.PHONGs.Count(x => x.TrangThai == "Trống").ToString();
                }
                if (staffDashboard || AuthService.HasPermission("THONGKE"))
                {
                    DateTime start = DateTime.Today, end = start.AddDays(1);
                    decimal revenue = db.HOADONs.Where(x => x.NgayThanhToan >= start && x.NgayThanhToan < end).Select(x => (decimal?)x.ThanhTien).Sum() ?? 0m;
                    lblDoanhThu.Text = revenue.ToString("N0") + " đ";
                }
            }
        }

        

        private void LoadChartDoanhThu()
        {
            chart1.Series.Clear();
            if (!staffDashboard && !AuthService.HasPermission("THONGKE")) return;
            bool daily = staffDashboard;
            int nam = DateTime.Today.Year;

            DateTime tuNgay = daily ? DateTime.Today : new DateTime(nam, 1, 1);
            DateTime denNgay = daily ? tuNgay.AddDays(1) : tuNgay.AddYears(1);

            decimal[] doanhThu = new decimal[daily ? 24 : 13];

            using (var db = new QuanLyKaraokeEntities())
            {
                AuthService.RequireCurrentUser(db);
                var duLieu = db.HOADONs
                    .Where(h =>
                        h.NgayThanhToan >= tuNgay &&
                        h.NgayThanhToan < denNgay
                    )
                    .GroupBy(h => daily ? h.NgayThanhToan.Hour : h.NgayThanhToan.Month)
                    .Select(g => new
                    {
                        Moc = g.Key,
                        DoanhThu = g.Sum(h => h.ThanhTien)
                    })
                    .ToList();

                foreach (var item in duLieu)
                {
                    doanhThu[item.Moc] = item.DoanhThu;
                }
            }

            
            chart1.Series.Clear();

            Series series = new Series("DoanhThu");

            series.ChartType = SeriesChartType.Column;
            series.IsVisibleInLegend = false;

            for (int moc = daily ? 0 : 1; moc <= (daily ? 23 : 12); moc++)
            {
                
                double giaTri =
                    (double)(doanhThu[moc] / 1000000m);

                series.Points.AddXY(moc, giaTri);
            }

            chart1.Series.Add(series);
            chart1.Series["DoanhThu"].Color = Color.FromArgb(16, 185, 129);
            ChartArea area = chart1.ChartAreas[0];

           
            area.AxisX.Minimum = daily ? 0 : 1;
            area.AxisX.Maximum = daily ? 23 : 12;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Format = "0";
            area.AxisX.Title = daily ? "Giờ trong ngày" : "Tháng";
            area.AxisX.MajorGrid.Enabled = false;

           
            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = double.NaN;
            area.AxisY.Interval = 0;
            area.AxisY.LabelStyle.Format = "0.##";
            area.AxisY.Title = "Doanh thu (triệu đồng)";
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;

            chart1.Invalidate();
        }

       

        private bool OpenChildForm(Form childForm)
        {
            var orderForm = currentChildForm as FrmGoiDichVu;
            if (orderForm != null && !orderForm.ConfirmLeave()) { childForm.Dispose(); return false; }
            if (currentChildForm != null)
            {
                var previousForm = currentChildForm;
                currentChildForm = null;
                previousForm.Close();
                previousForm.Dispose();
            }

            panelDashboard.Visible = false;
            panelTrangChuHeader.Visible = false;

            currentChildForm = childForm;
            childForm.FormClosed += (sender, e) =>
            {
                if (currentChildForm == childForm)
                {
                    currentChildForm = null;
                    btnTrangChu_Click(this, EventArgs.Empty);
                }
            };

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            panelContent.Controls.Add(childForm);

            childForm.BringToFront();
            childForm.Show();
            return true;
        }

        

        private void btnPhong_Click(object sender, EventArgs e)
        {
            if (OpenInternal(() => new FrmQuanLyPhong())) SetActiveSidebar(btnPhong);
        }

        

        private void btnTrangChu_Click(object sender, EventArgs e)
        {
            var orderForm = currentChildForm as FrmGoiDichVu;
            if (orderForm != null && !orderForm.ConfirmLeave()) return;
            SetActiveSidebar(btnTrangChu);
            if (currentChildForm != null)
            {
                var previousForm = currentChildForm;
                currentChildForm = null;
                previousForm.Close();
                previousForm.Dispose();
                currentChildForm = null;
            }

            panelTrangChuHeader.Visible = true;
            panelDashboard.Visible = true;
            panelDashboard.BringToFront();
            
            try
            {
                ApplyAccountMode();
                LoadDashboard();
                LoadChartDoanhThu();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi cập nhật Dashboard: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnDichVu_Click(object sender, EventArgs e)
        {
            if (OpenInternal(() => new FrmQuanLyDichVu())) SetActiveSidebar(btnDichVu);
        }

        private void btnDatPhong_Click(object sender, EventArgs e)
        {
            if (OpenInternal(() => new FrmDanhSachDatPhong())) SetActiveSidebar(btnDatPhong);
        }

        private void btnGoiDichVu_Click(object sender, EventArgs e)
        {
            if (OpenInternal(() => new FrmGoiDichVu())) SetActiveSidebar(btnGoiDichVu);
        }

        private void btnThanhToan_Click(object sender, EventArgs e)
        {
            if (OpenChildForm(new FrmThanhToan())) SetActiveSidebar(btnThanhToan);
        }

        private void btnThongKe_Click(object sender, EventArgs e)
        {
            if (OpenChildForm(new FrmThongKe())) SetActiveSidebar(btnThongKe);
        }

        private void SetActiveSidebar(Guna.UI2.WinForms.Guna2Button button)
        {
            ButtonActive.SetActiveButton(
                button,
                btnTrangChu,
                btnPhong,
                btnDichVu,
                btnDatPhong,
                btnGoiDichVu,
                btnThanhToan,
                btnThongKe,
                personalAccount,
                accountManagement,
                staffManagement,
                roomTypes,
                roleManagement
            );
        }
    }
}
