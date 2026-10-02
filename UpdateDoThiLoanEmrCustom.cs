using System;
using System.IO;
using System.Text;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = new UTF8Encoding(false);

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
        {
            var requestedName = new AssemblyName(resolveArgs.Name).Name;
            string[] searchPaths = new string[]
            {
                Path.Combine(baseDir, requestedName + ".dll"),
                Path.Combine(baseDir, "ReferencedAssemblies", requestedName + ".dll"),
                Path.Combine(baseDir, "Integrate", "EMR", requestedName + ".dll")
            };
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    try { return Assembly.LoadFrom(path); } catch { }
                }
            }
            return null;
        };

        return RunUpdate();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunUpdate()
    {
        long maQuanLy = 7345919;
        string connStr = "User Id=EMR_FINAL;Password=EMR_FINAL;Data Source=192.168.7.248:1521/orclstb;";

        string lyDoVaoVien = "Khối u mặt trước cổ tay trái đau tức và tê bì bàn ngón tay";

        string quaTrinhBenhLy = 
@"- Cách nhập viện khoảng 6 tháng, bệnh nhân tự sờ thấy khối gồ nhỏ tại mặt trước cổ tay trái, tăng dần kích thước, ấn đau tức và tê bì dị cảm lan xuống các ngón 1, 2, 3 khi tì đè hoặc cầm nắm.
- Ngày 24/09/2026: Khám ngoại trú tại Viện Cơ xương khớp - BV Bạch Mai, chụp MRI cổ tay trái phát hiện khối u bao dây thần kinh giữa kích thước 21x17 mm liên tục với dây thần kinh giữa ngang mức gân gấp nông.
- Ngày 29/09/2026 (08:45): Nhập viện Khoa Chấn thương Chỉnh hình và Cột sống (Khoa 57 - P712A), hoàn thiện các xét nghiệm bilan trước mổ.
- Ngày 30/09/2026: Hội chẩn chuyên khoa, giải thích chỉ định phẫu thuật bóc u thần kinh giữa cho người bệnh và gia đình.
- Ngày 01/10/2026: Phẫu thuật bóc tách cắt u bao dây thần kinh giữa cổ tay trái gửi xét nghiệm giải phẫu bệnh (GPB); hậu phẫu ổn định tại phòng hồi tỉnh và chuyển về buồng bệnh theo dõi.
- Ngày 02/10/2026 (12:00): Bệnh nhân tỉnh táo, vết mổ khô sạch, giảm tê bì, đầu chi ấm hồng, vận động cảm giác các ngón hồi phục tốt -> Làm tổng kết ra khoa, duyệt xuất viện.";

        string tienSuBanThan = "Khỏe mạnh, chưa ghi nhận bệnh lý mạn tính trước đây. Không có tiền sử dị ứng thuốc hay thức ăn.";
        string tienSuGiaDinh = "Chưa phát hiện bệnh lý di truyền hoặc liên quan.";

        string toanThan = 
@"Bệnh nhân tỉnh, tiếp xúc tốt. Thể trạng trung bình.
Da niêm mạc hồng hào, không phù, không xuất huyết dưới da.
Tuyến giáp không to, hạch ngoại vi không sờ thấy.
Dấu hiệu sinh tồn: Mạch 78 ck/phút, Huyết áp 120/80 mmHg, Nhiệt độ 36.5°C, Nhịp thở 18 ck/phút, SpO2 98%.";

        string benhNgoaiKhoa = 
@"Khám chuyên khoa Cổ - Bàn tay trái:
- Nhìn: Khối gồ mặt trước cổ tay trái dọc theo đường đi giải phẫu dây thần kinh giữa, kích thước khoảng 2x2 cm. Da trên bề mặt khối bình thường, không sưng nóng đỏ.
- Sờ & Thăm khám thực thể: Khối mật độ chắc, ranh giới rõ, bề mặt nhẵn, di động ngang tốt hơn dọc, ấn đau tức tại chỗ.
- Dấu hiệu thần kinh ngoại vi (+): Dấu hiệu Tinel (+) rõ tại vị trí khối u gây cảm giác tê buốt giật điện lan xuống ngón 1, 2, 3 và nửa ngoài ngón 4 bàn tay trái. Dấu hiệu Phalen (+).
- Vận động & Dinh dưỡng ngọn chi: Cơ mô cái bảo tồn, cơ lực ngón cái 5/5, biên độ vận động khớp cổ tay và các ngón bình thường. Mạch quay, mạch trụ bắt rõ, thời gian hồi lưu mao mạch (CRT) < 2s.";

        string coXuongKhop = benhNgoaiKhoa;

        string tuanHoan = "Tim nhịp đều, T1 T2 rõ, chưa nghe thấy tiếng tim bệnh lý.";
        string hoHap = "Phổi thông khí đều 2 bên rõ, không rale.";
        string tieuHoa = "Bụng mềm, không chướng, không điểm đau khu trú.";
        string thanTietNieu = "Chạm thận (-), bập bềnh thận (-), tiểu tiện tự chủ.";
        string thanKinh = "Tỉnh táo G15đ, không có dấu hiệu thần kinh khu trú ngoài chèn ép thần kinh giữa cổ tay trái.";

        string canLamSang = 
@"- Chẩn đoán hình ảnh:
  + MRI cổ tay trái có tiêm Gadovist: Khối ngấm thuốc không đồng nhất liên tục với dây thần kinh giữa ngang mức gân gấp nông kích thước 21x17 mm -> Theo dõi Schwannoma (U bao dây thần kinh giữa); Kèm nang bao hoạt dịch gan tay 8x5 mm.
  + X-quang cẳng tay: Dày mô mềm mặt trước cổ tay trái.
  + X-quang ngực thẳng: Dải xơ mờ 1/3 dưới phổi trái, bóng tim không to.
- Xét nghiệm Bilan trước mổ:
  + CTM: Hb 127 g/L, WBC 4.9 G/L, PLT 344 G/L.
  + Đông máu: PT-INR 0.99, Fibrinogen 3.48 g/L, APTT 1.02.
  + Sinh hóa: Glucose 4.8 mmol/L, Ure 4.1 mmol/L, Creatinin 60 µmol/L, AST 24 U/L, ALT 28 U/L.
  + Nhóm máu: O.";

        string tomTatBenhAn = 
@"Bệnh nhân nữ, 59 tuổi, tiền sử khỏe mạnh, vào viện vì khối u mặt trước cổ tay trái kèm tê bì bàn ngón tay.
Qua hỏi bệnh, khai thác tiền sử và thăm khám lâm sàng ngoại khoa phát hiện các hội chứng, triệu chứng chính sau:
1. Triệu chứng khối u thực thể thần kinh giữa: Khối mặt trước cổ tay trái ~2x2 cm dọc trục dây thần kinh giữa, mật độ chắc, ranh giới rõ, di động ngang; Dấu hiệu Tinel (+) và Phalen (+) rõ vùng cổ tay trái, tê dị cảm vùng chi phối ngón 1, 2, 3 và nửa ngoài ngón 4.
2. Chức năng vận động & mạch máu ngọn chi: Cơ mô cái bảo tồn, cơ lực ngón cái 5/5, tầm vận động khớp cổ bàn ngón tay bình thường; Mạch quay và mạch trụ bắt rõ, tưới máu đầu chi tốt (CRT < 2s).
3. Cận lâm sàng giá trị: MRI cổ tay trái xác định khối u bao dây thần kinh giữa kích thước 21x17 mm (Schwannoma) phía trước gân gấp nông. Bilan tiền phẫu trong giới hạn bình thường.
4. Toàn trạng: Bệnh nhân tỉnh táo, tiếp xúc tốt, dấu hiệu sinh tồn ổn định (Mạch 78 ck/p, HA 120/80 mmHg, SpO2 98%), không có hội chứng nhiễm trùng.";

        string phanBiet = "Phân biệt Nang bao hoạt dịch gân gấp (Ganglion cyst), U tế bào khổng lồ bao gân (GCTTS), U mỡ (Lipoma).";
        string tienLuong = "Khá";
        string huongDieuTri = "Phẫu thuật bóc u thần kinh giữa gửi giải phẫu bệnh, dùng kháng sinh dự phòng/điều trị, giảm đau, chăm sóc vết mổ và tập phục hồi chức năng ngón tay.";

        // THONGTINDIEUTRI
        string qtBenhLyVaDienBien = 
@"- Ngày 24/09/2026: Bệnh nhân khám ngoại trú tại Viện CXK - BV Bạch Mai, chụp MRI cổ tay trái phát hiện khối u bao dây thần kinh giữa 21x17 mm.
- Ngày 29/09/2026 (08:45): Nhập viện Khoa Chấn thương Chỉnh hình và Cột sống (Khoa 57) với chẩn đoán [D48.2] U bao dây thần kinh giữa cổ tay trái (Schwannoma). Hoàn thiện bilan trước mổ.
- Ngày 30/09/2026: Hội chẩn chuyên khoa, giải thích chỉ định phẫu thuật cho người bệnh và gia đình.
- Ngày 01/10/2026: Phẫu thuật bóc tách cắt u bao dây thần kinh giữa cổ tay trái gửi xét nghiệm GPB; hậu phẫu tại phòng hồi tỉnh ổn định, sau đó chuyển về buồng bệnh P712A tiếp tục điều trị.
- Ngày 02/10/2026 (12:00): Bệnh nhân tỉnh táo, sinh hiệu ổn định, vết mổ khô sạch, đầu chi ấm hồng, giảm tê bì, vận động cảm giác các ngón tốt -> Đủ điều kiện ra viện.";

        string tomTatKqXn = 
@"- Chẩn đoán hình ảnh:
  + MRI cổ tay trái: Khối u bao dây thần kinh giữa (Schwannoma) kích thước 21x17 mm phía trước gân gấp nông; nang bao hoạt dịch gan tay 8x5 mm.
  + X-quang cẳng tay: Dày mô mềm mặt trước cổ tay trái.
- Xét nghiệm huyết học, sinh hóa, đông máu: CTM (Hb 127, WBC 4.9, PLT 344), Đông máu (PT-INR 0.99, Fib 3.48), Sinh hóa (Glucose 4.8, Ure 4.1, Cre 60, AST/ALT 24/28), Nhóm máu O.";

        string ppDieuTri = "Phẫu thuật bóc u dây thần kinh giữa cổ tay trái gửi xét nghiệm giải phẫu bệnh; Điều trị nội khoa chu phẫu: Kháng sinh, giảm đau chống phù nề, thay băng chăm sóc vết mổ và hướng dẫn tập phục hồi chức năng.";
        string tinhTrangRaVien = "Bệnh nhân tỉnh táo, tiếp xúc tốt, da niêm mạc hồng hào, không sốt. Vết mổ khô sạch, không sưng nề chảy dịch. Vận động và cảm giác các ngón tay hồi phục tốt, đầu chi hồng ấm, đại tiểu tiện tự chủ.";
        string huongDieuTriTiep = "- Kê đơn thuốc điều trị ngoại trú dùng tại nhà theo hướng dẫn.\n- Giữ vệ sinh vết mổ khô sạch, thay băng định kỳ.\n- Vận động nhẹ nhàng các ngón tay, tránh tì đè cổ tay.\n- Tái khám cắt chỉ sau 10-14 ngày và nhận kết quả mô bệnh học giải phẫu bệnh.";
        string loiDan = "Uống thuốc đúng liều lượng và thời gian theo đơn; giữ vết mổ sạch; tái khám đúng hẹn hoặc khám lại ngay nếu vết mổ sưng đau, chảy dịch bất thường.";

        try
        {
            var asm = Assembly.Load("Oracle.ManagedDataAccess");
            var connType = asm.GetType("Oracle.ManagedDataAccess.Client.OracleConnection");
            var cmdType = asm.GetType("Oracle.ManagedDataAccess.Client.OracleCommand");
            var paramType = asm.GetType("Oracle.ManagedDataAccess.Client.OracleParameter");

            using (var conn = (IDbConnection)Activator.CreateInstance(connType, connStr))
            {
                conn.Open();
                Console.WriteLine("✓ Kết nối Oracle EMR thành công!");

                // 1. Cập nhật BENHANNGOAIKHOA
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE BENHANNGOAIKHOA SET
                            LyDoVaoVien = :lyDo,
                            QuaTrinhBenhLy = :qtBenhLy,
                            TienSuBanThan = :tsBanThan,
                            TienSuGiaDinh = :tsGiaDinh,
                            ToanThan = :toanThan,
                            BenhNgoaiKhoa = :benhNgoaiKhoa,
                            CoXuongKhop = :coXuongKhop,
                            TuanHoan = :tuanHoan,
                            HoHap = :hoHap,
                            TieuHoa = :tieuHoa,
                            ThanTietNieu = :thanTietNieu,
                            ThanKinh = :thanKinh,
                            CanLamSang = :canLamSang,
                            TomTatBenhAn = :tomTat,
                            PhanBiet = :phanBiet,
                            TienLuong = :tienLuong,
                            HuongDieuTri = :huongDieuTri,
                            PhauThuat = 1,
                            BacSyLamBenhAn = '034727',
                            TenBacSyLamBenhAn = 'ThS.BS Nguyễn Hữu Sâm'
                        WHERE MaQuanLy = :mql";

                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "lyDo", lyDoVaoVien));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "qtBenhLy", quaTrinhBenhLy));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tsBanThan", tienSuBanThan));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tsGiaDinh", tienSuGiaDinh));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "toanThan", toanThan));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "benhNgoaiKhoa", benhNgoaiKhoa));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "coXuongKhop", coXuongKhop));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tuanHoan", tuanHoan));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "hoHap", hoHap));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tieuHoa", tieuHoa));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "thanTietNieu", thanTietNieu));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "thanKinh", thanKinh));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "canLamSang", canLamSang));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tomTat", tomTatBenhAn));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "phanBiet", phanBiet));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tienLuong", tienLuong));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "huongDieuTri", huongDieuTri));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "mql", maQuanLy));

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(string.Format("✓ Đã cập nhật BENHANNGOAIKHOA (MaQuanLy={0}, Rows affected={1})", maQuanLy, rows));
                }

                // 2. Cập nhật THONGTINDIEUTRI
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        UPDATE THONGTINDIEUTRI SET
                            QuaTrinhBenhLyVaDienBien = :qtDienBien,
                            TomTatKetQuaXetNghiem = :tomTatXn,
                            PhuongPhapDieuTri = :ppDieuTri,
                            TinhTrangNguoiBenhRaVien = :ttRaVien,
                            HuongDieuTriTiepTheo = :huongDieuTriTiep,
                            LoiDanBacSi = :loiDan,
                            NgayTongKet = '02/10/2026',
                            BacSyDieuTri = '034727',
                            TenBacSyDieuTri = 'ThS.BS Nguyễn Hữu Sâm'
                        WHERE MaQuanLy = :mql";

                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "qtDienBien", qtBenhLyVaDienBien));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "tomTatXn", tomTatKqXn));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "ppDieuTri", ppDieuTri));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "ttRaVien", tinhTrangRaVien));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "huongDieuTriTiep", huongDieuTriTiep));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "loiDan", loiDan));
                    cmd.Parameters.Add(Activator.CreateInstance(paramType, "mql", maQuanLy));

                    int rows = cmd.ExecuteNonQuery();
                    Console.WriteLine(string.Format("✓ Đã cập nhật THONGTINDIEUTRI (MaQuanLy={0}, Rows affected={1})", maQuanLy, rows));
                }
            }

            Console.WriteLine("\n🎉 HOÀN TẤT NÂNG CẤP BỆNH ÁN NGOẠI KHOA CHO BỆNH NHÂN ĐỖ THỊ LOAN!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Lỗi: " + ex.Message + "\n" + ex.StackTrace);
            return 1;
        }
    }
}
