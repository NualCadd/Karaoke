using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using karaoke.Models;
using karaoke.Services;
using karaoke.UI;

namespace karaoke
{
    public partial class FrmDatPhong : Form
    {
        private readonly BookingService bookingService = new BookingService();
        private readonly int? editingMaDatPhong;
        private string selectedMaPhong;
        private decimal selectedDonGia;

        private class LoaiPhongItem
        {
            public int? MaLoaiPhong { get; set; }
            public string TenLoaiPhong { get; set; }
            public override string ToString() { return TenLoaiPhong; }
        }

        public FrmDatPhong() : this(null)
        {
        }

        public FrmDatPhong(int? maDatPhong)
        {
            InitializeComponent();
            foreach (var button in new[] { btnTimPhong, btnHuy, btnLuu })
            {
                button.Resize += (sender, args) => ButtonActive.RoundButton((Button)sender);
                ButtonActive.RoundButton(button);
            }
            this.editingMaDatPhong = maDatPhong;
            this.Load += FrmDatPhong_Load;
        }

        private void FrmDatPhong_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            InitGrid();
            LoadLoaiPhong();

            if (editingMaDatPhong.HasValue)
            {
                lblTitle.Text = "CHỈNH SỬA PHIẾU ĐẶT PHÒNG #" + editingMaDatPhong.Value;
                btnLuu.Text = "Cập nhật";
                LoadExistingBooking(editingMaDatPhong.Value);
            }
            else
            {
                lblTitle.Text = "TẠO PHIẾU ĐẶT PHÒNG MỚI";
                btnLuu.Text = "Lưu đặt phòng";
    
                DateTime now = DateTime.Now;
                DateTime defaultStart = now.AddMinutes(20);
                defaultStart = new DateTime(defaultStart.Year, defaultStart.Month, defaultStart.Day, defaultStart.Hour, (defaultStart.Minute / 15) * 15, 0);
                dtpBatDau.Value = defaultStart;
                dtpKetThuc.Value = defaultStart.AddHours(2);
                numSoKhachTim.Value = 4;
                numSoKhach.Value = 4;

                SearchRooms();
            }
        }

        private void InitGrid()
        {
            dgvPhongTrong.AutoGenerateColumns = false;

            dgvPhongTrong.EnableHeadersVisualStyles = false;
            dgvPhongTrong.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(22, 101, 52);
            dgvPhongTrong.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPhongTrong.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvPhongTrong.DefaultCellStyle.SelectionBackColor = Color.FromArgb(16, 185, 129);
            dgvPhongTrong.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        private void LoadLoaiPhong()
        {
            cbLoaiPhong.Items.Clear();
            cbLoaiPhong.Items.Add(new LoaiPhongItem { MaLoaiPhong = null, TenLoaiPhong = "Tất cả loại phòng" });

            try
            {
                var types = bookingService.ListRoomTypes();
                foreach (var t in types)
                {
                    cbLoaiPhong.Items.Add(new LoaiPhongItem { MaLoaiPhong = t.MaLoaiPhong, TenLoaiPhong = t.TenLoaiPhong });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải danh mục loại phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            cbLoaiPhong.SelectedIndex = 0;
        }

        private void LoadExistingBooking(int maDatPhong)
        {
            try
            {
                var booking = bookingService.GetBooking(maDatPhong);
                if (booking == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin phiếu đặt phòng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                if (booking.TrangThai != "ChoNhan")
                {
                    MessageBox.Show("Phiếu đặt đã ở trạng thái '" + booking.TrangThai + "'. Chỉ cho phép chỉnh sửa phiếu 'ChoNhan'.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnLuu.Enabled = false;
                }

                dtpBatDau.Value = booking.GioBatDauDuKien;
                dtpKetThuc.Value = booking.GioKetThucDuKien;
                numSoKhachTim.Value = Math.Max(1, booking.SoKhach);
                numSoKhach.Value = Math.Max(1, booking.SoKhach);

                selectedMaPhong = booking.MaPhong;
                selectedDonGia = booking.DonGiaGioDaChot;
                txtTenPhong.Text = string.Format("{0} ({1})", booking.TenPhong, booking.MaPhong);
                lblDonGiaChot.Text = booking.DonGiaGioDaChot.ToString("N0") + " đ/giờ";

                txtTenKhach.Text = booking.TenKhach;
                txtSDT.Text = booking.SDT;
                txtGhiChu.Text = booking.GhiChu;

                SearchRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thông tin phiếu đặt: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTimPhong_Click(object sender, EventArgs e)
        {
            SearchRooms();
        }

        private void SearchRooms()
        {
            try
            {
                DateTime start = dtpBatDau.Value;
                DateTime end = dtpKetThuc.Value;
                int guestCount = (int)numSoKhachTim.Value;

                var selectedType = cbLoaiPhong.SelectedItem as LoaiPhongItem;
                int? maLoaiPhong = selectedType != null ? selectedType.MaLoaiPhong : null;

                var available = bookingService.FindAvailableRooms(start, end, guestCount, maLoaiPhong, editingMaDatPhong);

                dgvPhongTrong.DataSource = available;

                if (!string.IsNullOrEmpty(selectedMaPhong))
                {
                    foreach (DataGridViewRow row in dgvPhongTrong.Rows)
                    {
                        var room = row.DataBoundItem as AvailableRoomRow;
                        if (room != null && room.MaPhong == selectedMaPhong)
                        {
                            row.Selected = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm phòng: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void dgvPhongTrong_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPhongTrong.CurrentRow != null && dgvPhongTrong.CurrentRow.DataBoundItem is AvailableRoomRow room)
            {
                selectedMaPhong = room.MaPhong;
                selectedDonGia = room.DonGiaGio;
                txtTenPhong.Text = string.Format("{0} ({1}) - Sức chứa: {2}", room.TenPhong, room.MaPhong, room.SucChua);
                lblDonGiaChot.Text = room.DonGiaGio.ToString("N0") + " đ/giờ";
                if (numSoKhach.Value < numSoKhachTim.Value)
                {
                    numSoKhach.Value = numSoKhachTim.Value;
                }
            }
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedMaPhong))
                {
                    MessageBox.Show("Vui lòng chọn một phòng khả dụng từ danh sách phòng trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string tenKhach = txtTenKhach.Text.Trim();
                string sdt = txtSDT.Text.Trim();
                int soKhach = (int)numSoKhach.Value;
                DateTime start = dtpBatDau.Value;
                DateTime end = dtpKetThuc.Value;
                string ghiChu = txtGhiChu.Text.Trim();

                if (editingMaDatPhong.HasValue)
                {
                    bookingService.EditBooking(editingMaDatPhong.Value, selectedMaPhong, tenKhach, sdt, soKhach, start, end, ghiChu);
                    MessageBox.Show("Cập nhật phiếu đặt phòng thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    int newId = bookingService.CreateBooking(selectedMaPhong, tenKhach, sdt, soKhach, start, end, ghiChu);
                    MessageBox.Show("Tạo phiếu đặt phòng thành công! Mã đặt phòng: #" + newId, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu đặt phòng: " + ex.GetBaseException().Message, "Lỗi nghiệp vụ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnHuy_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
