using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using karaoke.Services;

namespace karaoke
{
    public partial class FrmGoiDichVu : Form
    {
        private readonly ServiceOrderService orderService = new ServiceOrderService();
        private readonly BindingList<CartItemInput> unconfirmedCart = new BindingList<CartItemInput>();
        private int currentMaPhien = 0;
        private readonly RoomService roomService = new RoomService();
        private List<RoomRow> rooms = new List<RoomRow>();
        private List<ActiveSessionRow> sessions = new List<ActiveSessionRow>();

        public FrmGoiDichVu()
        {
            InitializeComponent();
            this.Load += FrmGoiDichVu_Load;
        }

        private void txtTimPhong_TextChanged(object sender, EventArgs e) { RenderRoomCards(); }
        private void btnLamMoiPhong_Click(object sender, EventArgs e) { LoadRooms(); }
        private void btnChonPhongKhac_Click(object sender, EventArgs e) { ReturnToRooms(); }

        private void ShowRoomPicker()
        {
            panelTopBar.Visible = false;
            splitContainerMain.Visible = false;
            panelChonPhong.Visible = true;
            panelChonPhong.BringToFront();
            LoadRooms();
        }

        private void LoadRooms()
        {
            try
            {
                rooms = roomService.ListForServiceOrder();
                sessions = orderService.GetActiveSessions();
                RenderRoomCards();
            }
            catch (Exception ex) { MessageBox.Show("Không thể tải danh sách phòng: " + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void RenderRoomCards()
        {
            string keyword = txtTimPhong.Text.Trim();
            var compare = System.Globalization.CultureInfo.GetCultureInfo("vi-VN").CompareInfo;
            var visible = rooms.Where(r => keyword.Length == 0 || compare.IndexOf(r.MaPhong ?? "", keyword, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0 || compare.IndexOf(r.TenPhong ?? "", keyword, System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0).ToList();
            lblSoPhong.Text = string.Format("Hiển thị {0} / {1} phòng • Chọn phòng đang sử dụng", visible.Count, rooms.Count);
            danhSachPhong.SuspendLayout();
            var old = danhSachPhong.Controls.Cast<Control>().ToArray();
            danhSachPhong.Controls.Clear();
            foreach (var control in old) control.Dispose();
            foreach (var room in visible)
            {
                int activeCount = sessions.Count(s => s.MaPhong == room.MaPhong);
                bool canOrder = room.TrangThai == "Đang sử dụng" && activeCount == 1;
                Color color = room.TrangThai == "Trống" ? Color.LightGreen : room.TrangThai == "Đang sử dụng" ? Color.LightCoral : room.TrangThai == "Bảo trì" ? Color.LightYellow : Color.LightGray;
                var button = new Button { Width = 140, Height = 100, Text = room.TenPhong + "\n" + room.LoaiPhong + "\n" + room.TrangThai, BackColor = color, UseVisualStyleBackColor = false, Cursor = canOrder ? Cursors.Hand : Cursors.Default, Tag = room.MaPhong };
                button.Click += (s, e) => OpenRoom(room.MaPhong);
                danhSachPhong.Controls.Add(button);
            }
            danhSachPhong.ResumeLayout();
        }

        private void OpenRoom(string maPhong)
        {
            try
            {
                var room = roomService.ListForServiceOrder().FirstOrDefault(r => r.MaPhong == maPhong);
                var active = orderService.GetActiveSessions().Where(s => s.MaPhong == maPhong).ToList();
                if (room == null || room.TrangThai != "Đang sử dụng" || active.Count != 1)
                {
                    MessageBox.Show("Phòng không có phiên hát đang hoạt động hợp lệ. Hãy nhận phòng trước khi gọi dịch vụ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadRooms();
                    return;
                }
                currentMaPhien = active[0].MaPhien;
                panelChonPhong.Visible = false;
                panelTopBar.Visible = true;
                splitContainerMain.Visible = true;
                LoadCatalog();
                LoadSessionDetails(currentMaPhien);
            }
            catch (Exception ex) { MessageBox.Show("Không thể mở phòng: " + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ReturnToRooms()
        {
            if (unconfirmedCart.Count > 0 && MessageBox.Show("Giỏ hàng chưa xác nhận sẽ bị xóa khi trở về danh sách phòng. Tiếp tục?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            unconfirmedCart.Clear();
            currentMaPhien = 0;
            ShowRoomPicker();
        }

        public bool ConfirmLeave()
        {
            return unconfirmedCart.Count == 0 || MessageBox.Show("Giỏ dịch vụ chưa xác nhận sẽ bị xóa khi rời màn hình. Tiếp tục?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void FrmGoiDichVu_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            try
            {
                AuthService.RequireCurrentUser();
                btnXacNhanGoi.Enabled = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); Close(); return; }

            InitGrids();
            dgvGioHang.DataSource = unconfirmedCart;
            unconfirmedCart.ListChanged += (s, ev) => UpdateCartTotal();

            ShowRoomPicker();
        }

        private void InitGrids()
        {
            
            dgvMenu.AutoGenerateColumns = false;
            dgvMenu.Columns.Add(new DataGridViewTextBoxColumn { Name = "TonKho", DataPropertyName = "TonKho", HeaderText = "Tồn kho", Width = 70 });

            FormatGridStyle(dgvMenu);

            
            dgvGioHang.AutoGenerateColumns = false;

            FormatGridStyle(dgvGioHang);

            
            dgvDaGoi.AutoGenerateColumns = false;

            FormatGridStyle(dgvDaGoi);
        }

        private void FormatGridStyle(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 101, 52);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(16, 185, 129);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        private void LoadSessionDetails(int maPhien)
        {
            if (maPhien <= 0) return;

            btnXacNhanGoi.Enabled = false;
            btnThemVaoGio.Enabled = false;
            dgvDaGoi.DataSource = null;
            lblSessionDetail.Text = "Đang tải thông tin phòng...";
            lblTongTienDichVu.Text = "Tiền dịch vụ đã gọi: —";
            lblTienPhongTamTinh.Text = "Tiền giờ hát tạm tính: —";
            lblTongCongTamTinh.Text = "TỔNG CỘNG TẠM TÍNH: —";
            try
            {
                var orders = orderService.GetSessionOrders(maPhien);
                var summary = orderService.GetSessionTotal(maPhien);
                dgvDaGoi.DataSource = orders;
                lblSessionDetail.Text = string.Format(
                    "Phòng: {0} | Bắt đầu: {1:HH:mm dd/MM} | Đã hát: {2}h{3:D2}p | Giá phòng: {4:N0} đ/h",
                    summary.TenPhong,
                    summary.GioBatDau,
                    (int)summary.ThoiGianHat.TotalHours,
                    summary.ThoiGianHat.Minutes,
                    summary.DonGiaGio
                );

                lblTongTienDichVu.Text = string.Format("Tiền dịch vụ đã gọi: {0:N0} đ", summary.TienDichVu);
                lblTienPhongTamTinh.Text = string.Format("Tiền giờ hát tạm tính: {0:N0} đ", summary.TienPhongTamTinh);
                lblTongCongTamTinh.Text = string.Format("TỔNG CỘNG TẠM TÍNH: {0:N0} đ", summary.TongCongTamTinh);
                btnThemVaoGio.Enabled = true;
                btnXacNhanGoi.Enabled = true;
            }
            catch (Exception ex)
            {
                lblSessionDetail.Text = "Không tải được thông tin phòng. Hãy chọn Làm mới hoặc trở về danh sách phòng.";
                MessageBox.Show("Lỗi tải chi tiết phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCatalog()
        {
            try
            {
                string search = txtTimDichVu.Text.Trim();
                var items = orderService.GetActiveServices(search);
                dgvMenu.DataSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải menu dịch vụ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtTimDichVu_TextChanged(object sender, EventArgs e)
        {
            LoadCatalog();
        }

        private void btnThemVaoGio_Click(object sender, EventArgs e)
        {
            if (currentMaPhien <= 0)
            {
                MessageBox.Show("Vui lòng chọn một phòng đang sử dụng trước khi gọi dịch vụ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvMenu.CurrentRow == null || !(dgvMenu.CurrentRow.DataBoundItem is ServiceItemRow item))
            {
                MessageBox.Show("Vui lòng chọn dịch vụ muốn thêm từ danh mục.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int qty = (int)numSoLuong.Value;
            if (qty <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = unconfirmedCart.FirstOrDefault(x => x.MaDV == item.MaDV);
            if ((long)(existing == null ? 0 : existing.SoLuong) + qty > item.TonKho)
            {
                MessageBox.Show(string.Format("Dịch vụ '{0}' chỉ còn {1} trong kho.", item.TenDV, item.TonKho), "Không đủ tồn kho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (existing != null)
            {
                existing.SoLuong += qty;
                unconfirmedCart.ResetBindings();
            }
            else
            {
                unconfirmedCart.Add(new CartItemInput
                {
                    MaDV = item.MaDV,
                    TenDV = item.TenDV,
                    DonViTinh = item.DonViTinh,
                    DonGia = item.DonGia,
                    SoLuong = qty
                });
            }

            numSoLuong.Value = 1;
            UpdateCartTotal();
        }

        private void dgvMenu_DoubleClick(object sender, EventArgs e)
        {
            btnThemVaoGio_Click(sender, e);
        }

        private void btnXoaMonGio_Click(object sender, EventArgs e)
        {
            if (dgvGioHang.CurrentRow != null && dgvGioHang.CurrentRow.DataBoundItem is CartItemInput item)
            {
                unconfirmedCart.Remove(item);
                UpdateCartTotal();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn món cần xóa khỏi giỏ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnXoaHetGio_Click(object sender, EventArgs e)
        {
            if (unconfirmedCart.Count == 0) return;

            if (MessageBox.Show("Xóa toàn bộ các món trong giỏ hàng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                unconfirmedCart.Clear();
                UpdateCartTotal();
            }
        }

        private void UpdateCartTotal()
        {
            decimal total = unconfirmedCart.Sum(x => x.ThanhTien);
            lblTongTienGio.Text = string.Format("Tổng giỏ: {0:N0} đ", total);
        }

        private void btnXacNhanGoi_Click(object sender, EventArgs e)
        {
            if (currentMaPhien <= 0)
            {
                MessageBox.Show("Vui lòng chọn phòng cần phục vụ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (unconfirmedCart.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống. Vui lòng thêm dịch vụ vào giỏ trước khi xác nhận.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                string.Format("Xác nhận gửi {0} món vào phòng này?\nTổng tiền giỏ: {1:N0} đ", unconfirmedCart.Count, unconfirmedCart.Sum(x => x.ThanhTien)),
                "Xác nhận gọi dịch vụ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes) return;

            try
            {
                orderService.OrderServices(currentMaPhien, unconfirmedCart.ToList());
                MessageBox.Show("Đã xác nhận gọi dịch vụ thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                unconfirmedCart.Clear();
                UpdateCartTotal();
                LoadSessionDetails(currentMaPhien);
                LoadCatalog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể đặt dịch vụ: " + ex.GetBaseException().Message, "Lỗi nghiệp vụ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            try
            {
                if (!orderService.GetActiveSessions().Any(s => s.MaPhien == currentMaPhien))
                {
                    unconfirmedCart.Clear();
                    currentMaPhien = 0;
                    MessageBox.Show("Phiên hát không còn hoạt động. Giỏ chưa xác nhận đã được xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ShowRoomPicker();
                    return;
                }
                LoadSessionDetails(currentMaPhien);
                LoadCatalog();
            }
            catch (Exception ex) { MessageBox.Show("Không thể làm mới: " + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
