using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Aspose.Words;

public class GeneratePt01AndCancelRationsOct02
{
    public class PatientTarget
    {
        public int Stt;
        public string RoomMng;
        public string PatientCode;
        public string PatientName;
        public string Dob;
        public string Gender;
        public string Address;
        public string InTime;
        public string Diagnosis;
        public string History;
        public string Course;
        public string HoiChanTime;
        public string ExamSummary;
        public string FullCls;
        public string BloodGroup;
        public string BloodReserve;
        public string SurgeryMethod;
        public string Anesthesia;
        public string Surgeon;
        public string SurgeryTime;
        public string Risks;
        public string RoomBed;
    }

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        Run(args);
    }

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mau pt01.docx");
        if (!File.Exists(templatePath)) templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "mau pt01.docx");

        if (!File.Exists(templatePath))
        {
            Console.WriteLine("❌ LỖI: Không tìm thấy file mẫu mau pt01.docx!");
            return;
        }

        string outDirDated = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01", "PT01_20261002_HN");
        string outDirStd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "BienBanHoiChan_PT01");
        if (!Directory.Exists(outDirDated)) Directory.CreateDirectory(outDirDated);
        if (!Directory.Exists(outDirStd)) Directory.CreateDirectory(outDirStd);

        var list = BuildProfiles();

        Console.WriteLine(string.Format("=== ĐANG XUẤT {0} BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) LỊCH MỔ THỨ SÁU 02/10/2026 — KHOA 57 HÀ NỘI ===", list.Count));

        int successCount = 0;
        foreach (var p in list)
        {
            Console.WriteLine(string.Format("\n[{0:D2}/{1:D2}] Đang tạo PT-01 cho BN: {2} ({3}) | {4} | PTV: {5}...", 
                p.Stt, list.Count, p.PatientName, p.PatientCode, p.RoomBed, p.Surgeon));

            Document doc = new Document(templatePath);

            // 1. Thay thế thông tin hành chính
            doc.Range.Replace("PHẠM VĂN BỒNG", p.PatientName.ToUpper(), false, false);
            doc.Range.Replace("22/07/1947", p.Dob, false, false);
            doc.Range.Replace("Giới tính:  Nam", "Giới tính:  " + p.Gender, false, false);
            doc.Range.Replace("Tổ 6, Phường  Minh Xuân, Tuyên Quang", p.Address, false, false);
            doc.Range.Replace("15/09/2026 21:25", p.InTime, false, false);
            doc.Range.Replace("Gãy liên mấu chuyển xương đùi Trái/ Stent mạch vành - Suy tim - Tăng huyết áp", p.Diagnosis, false, false);
            doc.Range.Replace("Stent đmv, suy tim, THA", p.History, false, false);

            string bloodStr = string.Format("Nhóm máu: {0}                 Dự trù máu: {1} (ml)", p.BloodGroup, p.BloodReserve);
            doc.Range.Replace("Nhóm máu:................ Dự trù máu........................................ (ml)", bloodStr, false, false);

            // 2. Điền chuẩn xác 9 thẻ <thay> theo từng đoạn ngữ cảnh
            foreach (Paragraph para in doc.GetChildNodes(NodeType.Paragraph, true))
            {
                string t = para.GetText();
                if (!t.Contains("<thay>")) continue;

                var prev = para.PreviousSibling as Paragraph;
                string prevT = prev != null ? prev.GetText().Trim() : "";

                if (t.StartsWith("Bệnh sử:"))
                {
                    para.Range.Replace("<thay>", p.Course, false, false);
                }
                else if (t.StartsWith("Thời gian hội chẩn:"))
                {
                    para.Range.Replace("..<thay>", " " + p.HoiChanTime, false, false);
                }
                else if (t.StartsWith("Tóm tắt tình trạng bệnh:"))
                {
                    para.Range.Replace("<thay>", p.ExamSummary, false, false);
                }
                else if (prevT.Contains("Các xét nghiệm, chẩn đoán hình ảnh"))
                {
                    para.Range.Replace("<thay>", p.FullCls, false, false);
                }
                else if (prevT.Contains("Phương pháp phẫu thuật"))
                {
                    para.Range.Replace("<thay>", p.SurgeryMethod, false, false);
                }
                else if (t.Contains("Phương pháp vô cảm dự kiến:"))
                {
                    para.Range.Replace("<thay>", p.Anesthesia, false, false);
                }
                else if (t.Contains("Phẫu  thuật  viên  chính:"))
                {
                    para.Range.Replace("<thay>", p.Surgeon, false, false);
                }
                else if (t.Contains("Ngày, giờ phẫu thuật dự kiến:"))
                {
                    para.Range.Replace(".....<thay>", " " + p.SurgeryTime, false, false);
                }
                else if (prevT.Contains("Các biến chứng, nguy cơ"))
                {
                    para.Range.Replace("<thay>", p.Risks, false, false);
                }
            }

            string safeName = p.PatientName.Trim().Replace(" ", "_");
            string fileNameDated = string.Format("{0:D2}_PT01_BienBanThongQuaMo_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);
            string fileNameStd = string.Format("PT01_{0:D2}_{1}_{2}.docx", p.Stt, safeName, p.PatientCode);

            string pathDated = Path.Combine(outDirDated, fileNameDated);
            string pathStd = Path.Combine(outDirStd, fileNameStd);

            doc.Save(pathDated);
            File.Copy(pathDated, pathStd, true);
            Console.WriteLine("  ✔ Đã xuất: " + fileNameDated);
            successCount++;
        }

        Console.WriteLine(string.Format("\n🎉 XUẤT THÀNH CÔNG 100% {0}/15 BIÊN BẢN HỘI CHẨN PT-01 TẠI:\n📂 {1}", successCount, outDirDated));
    }

    public static List<PatientTarget> BuildProfiles()
    {
        var list = new List<PatientTarget>();

        // =========================================================================
        // PHÒNG MỔ 5 (CỘT SỐNG) - 7 CA
        // =========================================================================

        // 1. NGUYỄN THANH HẢI (39t) - P740/G72 | PTV: BS Lê Đăng Tân
        list.Add(new PatientTarget {
            Stt = 1,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 740 - Giường số 72",
            PatientCode = "0004063457",
            PatientName = "NGUYỄN THANH HẢI",
            Dob = "18/06/1987",
            Gender = "Nam",
            Address = "Thành phố Hà Nội",
            InTime = "14:15 ngày 25/09/2026",
            Diagnosis = "CTCS: Vỡ L1, L4 HOS - Bí tiểu, vỡ ổ cối trái, vỡ chỏm xương đốt bàn ngón V chân trái / Giai đoạn trầm cảm nặng kèm triệu chứng loạn thần có hành vi tự sát",
            History = "Trầm cảm nặng kèm triệu chứng loạn thần, tự sát nhảy từ tầng cao.",
            Course = "Bệnh nhân nam 39 tuổi, tiền sử rối loạn trầm cảm nặng. Ngày vào viện do hành vi tự sát nhảy từ tầng cao xuống đất, đập lưng và mông xuống nền cứng. Sau ngã đau dữ dội vùng thắt lưng, mất vận động và tê bì hai chân, bí tiểu, đau sưng nề háng trái và bàn chân trái. Được cấp cứu đưa vào BV Bạch Mai. Chụp CT và MRI cột sống thắt lưng phát hiện vỡ phức tạp thân đốt sống L1 và L4 mảnh vỡ chèn ép ống sống gây bí tiểu; chụp X-quang/CT khung chậu vỡ ổ cối trái, gãy vỡ chỏm xương đốt bàn ngón V chân trái. Đã hội chẩn Viện Sức khỏe Tâm thần điều chỉnh thuốc an thần, hội chẩn thông qua mổ chỉ định phẫu thuật nắn chỉnh, cố định cột sống lối sau bằng nẹp vít qua cuống L1, L4 và giải ép thần kinh.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 82 ck/p, HA 120/75 mmHg, T° 36.8°C, SpO2 98%. BN tỉnh, tiếp xúc chậm, khí sắc trầm cảm. Khám cột sống thắt lưng: Biến dạng gù nhẹ vùng thắt lưng, co cứng cơ cạnh sống, ấn đau chói gai sau L1, L4; cơ lực chi dưới: chân phải 3/5, chân trái 2/5; giảm cảm giác nông sâu từ L1 trở xuống hai bên; bí tiểu, đang đặt sonde tiểu ra nước tiểu vàng trong; khớp háng trái sưng nề, hạn chế vận động; bàn chân trái sưng nề ngón V. Tim đều, phổi thông khí tốt, bụng mềm.",
            FullCls = "- Công thức máu: WBC 8.4 G/L, RBC 4.12 T/L, HGB 126 g/L, PLT 245 G/L.\n- Đông máu cơ bản: PT-INR 1.02, APTT 0.94, Fibrinogen 3.25 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 5.2 mmol/L, Creatinin 74 µmol/L, AST 28 U/L, ALT 24 U/L, Điện giải đồ: Na 139, K 4.1, Cl 102 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: 10 thông số trong giới hạn bình thường.\n- Chẩn đoán hình ảnh: CT/MRI cột sống thắt lưng: Vỡ lún mất vững thân đốt sống L1 và L4, mảnh xương vỡ đẩy lồi ra sau làm hẹp 50% thiết diện ống sống chèn ép nón tủy và chùm đuôi ngựa; X-quang/CT khung chậu: Vỡ ổ cối trái không di lệch nhiều; X-quang bàn chân trái: Gãy vỡ chỏm đốt bàn ngón V.",
            BloodGroup = "B Rh(+)",
            BloodReserve = "700",
            SurgeryMethod = "Phẫu thuật nắn chỉnh, cố định cột sống lối sau bằng nẹp vít qua cuống L1-L4 và mở cung sau giải ép thần kinh",
            Anesthesia = "Gây mê nội khí quản",
            Surgeon = "BS Lê Đăng Tân",
            SurgeryTime = "08 giờ 00 phút, ngày 02/10/2026",
            Risks = "Chảy máu trong mổ do tổn thương đám rối tĩnh mạch ngoài màng cứng, rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh/chùm đuôi ngựa gây liệt hoặc rối loạn cơ tròn nặng hơn, tụ máu chèn ép sau mổ, nhiễm trùng vết mổ sâu, nguy cơ kích động hoặc tự sát trong giai đoạn hậu phẫu."
        });

        // 2. NGUYỄN THỊ VINH (73t) - P723/G41 | PTV: TS. Nguyễn Văn Trung
        list.Add(new PatientTarget {
            Stt = 2,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 723 - Giường số 41",
            PatientCode = "0004054272",
            PatientName = "NGUYỄN THỊ VINH",
            Dob = "12/03/1953",
            Gender = "Nữ",
            Address = "Tỉnh Phú Thọ",
            InTime = "10:30 ngày 22/09/2026",
            Diagnosis = "Trượt đốt sống L4-L5 độ I - Hẹp ống sống thắt lưng / Vỡ xẹp cũ L1 - Suy thượng thận do thuốc - Theo dõi bệnh tim thiếu máu cục bộ",
            History = "Tăng huyết áp, đau khớp dùng thuốc nam nhiều năm gây suy thượng thận do thuốc; Đau thắt ngực (đã HC Tim mạch theo dõi Troponin T).",
            Course = "Bệnh nhân nữ 73 tuổi, tiền sử đau lưng mạn tính nhiều năm, tự dùng thuốc giảm đau/thuốc nam kéo dài. Khoảng 6 tháng nay đau thắt lưng tăng dữ dội lan xuống mặt sau đùi và cẳng chân hai bên (chân phải > chân trái), tê bì châm chích bàn chân, đi bộ ngắt quãng dưới 50m phải ngồi nghỉ do đau buốt tê bì hai chân. Vào viện Bạch Mai khám, MRI và CT cột sống thắt lưng cho thấy trượt L4 ra trước độ I trên nền thoái hóa nặng, phì đại dây chằng vàng và diện khớp gây hẹp nặng ống sống tầng L4-L5, kèm loãng xương nặng và xẹp lún cũ L1. Đã hội chẩn Tim mạch, Nội tiết điều chỉnh Cortisol nền -> Chỉ định phẫu thuật nẹp vít qua cuống nhồi xi măng sinh học (cement-augmented pedicle screw) để tăng độ vững trên nền loãng xương kết hợp giải ép ống sống hàn khớp liên thân đốt.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 78 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. Hội chứng Cushing do thuốc (mặt tròn đỏ, da mỏng). Cột sống thắt lưng: Gù vẹo cột sống thắt lưng, ấn đau gai sau và cạnh sống L4-L5; Lasègue (+) 45 độ bên phải, 60 độ bên trái; giảm cảm giác vùng chi phối rễ L5 hai bên; cơ lực mu bàn chân phải 4/5, chân trái 4/5. Phản xạ gân gót hai bên giảm. Tim T1 T2 rõ, không tiếng thổi; phổi không rale; bụng mềm.",
            FullCls = "- Công thức máu: WBC 7.1 G/L, RBC 3.95 T/L, HGB 118 g/L, PLT 210 G/L.\n- Đông máu cơ bản: PT-INR 1.01, APTT 0.92, Fibrinogen 3.10 g/L.\n- Sinh hóa máu: Glucose 5.8 mmol/L, Ure 5.4 mmol/L, Creatinin 76 µmol/L, AST 25 U/L, ALT 20 U/L, Cortisol máu (8h) 85 nmol/L (suy vỏ thượng thận thứ phát), Troponin T-hs 11.2 ng/L, Điện giải đồ: Na 137, K 3.9, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: 10 thông số bình thường.\n- Chẩn đoán hình ảnh: MRI/CT cột sống thắt lưng: Trượt đốt sống L4 ra trước độ I so với L5, phì đại diện khớp và dây chằng vàng dày 5mm gây hẹp nặng ống sống tầng L4-L5 chèn ép rễ thần kinh L5 hai bên; Loãng xương nặng, xẹp cũ ổn định thân đốt L1.",
            BloodGroup = "O Rh(+)",
            BloodReserve = "350",
            SurgeryMethod = "Phẫu thuật nẹp vít qua cuống L4-L5 nhồi xi măng sinh học, cắt cung sau giải ép ống sống và hàn xương liên thân đốt (TLIF L4-L5)",
            Anesthesia = "Gây mê nội khí quản",
            Surgeon = "TS. Nguyễn Văn Trung",
            SurgeryTime = "09 giờ 30 phút, ngày 02/10/2026",
            Risks = "Rò dịch não tủy do rách màng tủy dính ở người già, tràn xi măng vào ống sống hoặc tĩnh mạch gây thuyên tắc phổi, chảy máu vết mổ, suy thượng thận cấp sau mổ do stress phẫu thuật, biến cố tim mạch thiếu máu cơ tim cấp, nhiễm trùng vết mổ trên cơ địa suy giảm miễn dịch do corticoid."
        });

        // 3. NGUYỄN THỊ LIÊN (58t) - P723/G44 | PTV: TS. Nguyễn Văn Trung
        list.Add(new PatientTarget {
            Stt = 3,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 723 - Giường số 44",
            PatientCode = "0004078976",
            PatientName = "NGUYỄN THỊ LIÊN",
            Dob = "05/08/1968",
            Gender = "Nữ",
            Address = "Tỉnh Hải Dương",
            InTime = "09:00 ngày 26/09/2026",
            Diagnosis = "Xẹp đốt sống T12, L1 mới / Đã bơm xi măng L3 - Viêm khớp dạng thấp huyết thanh dương tính - Tăng huyết áp - Loãng xương nặng - Suy giáp sau PT cắt bán phần tuyến giáp do bướu giáp",
            History = "Viêm khớp dạng thấp điều trị Medrol dài ngày; Đã phẫu thuật cắt bán phần tuyến giáp do bướu giáp, đang bù Levothyrox; Tăng huyết áp; Tiền sử đã BXM L3 cách 1 năm.",
            Course = "Bệnh nhân nữ 58 tuổi, tiền sử viêm khớp dạng thấp dùng corticoid kéo dài gây loãng xương thứ phát nặng, đã từng được phẫu thuật bơm xi măng thân đốt L3 cách 1 năm. Đợt này sau ngã ngồi nhẹ cách 1 tuần xuất hiện đau chói dữ dội vùng lưng - thắt lưng, đau tăng khi thay đổi tư thế, ngồi dậy và đi lại rất khó khăn, không tự xoay trở được trên giường. Nhập Khoa 57, chụp MRI cột sống ngực - thắt lưng phát hiện tổn thương phù tủy xương xẹp cấp tính thân đốt sống T12 và L1 (tăng tín hiệu trên STIR, giảm trên T1W), đốt L3 có hình ảnh xi măng cũ ổn định. Bệnh nhân đã được hội chẩn Cơ xương khớp điều chỉnh thuốc nền, chỉ định phẫu thuật tạo hình thân đốt sống T12 và L1 bằng bơm xi măng sinh học qua da (Vertebroplasty).",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.5°C, SpO2 99%. Thể trạng trung bình. Cột sống ngực - thắt lưng: Co cứng cơ cạnh sống ngực 12 và thắt lưng 1; gõ dồn đau chói tại vị trí gai sau T12 và L1; không có dấu hiệu chèn ép rễ thần kinh hay tủy sống (Lasègue (-), cơ lực hai chân 5/5, cảm giác chi dưới bình thường, phản xạ gân xương bình thường, đại tiểu tiện tự chủ). Khám các khớp bàn ngón tay biến dạng nhẹ do VKDT, không sưng nóng đỏ. Tim phổi bình thường, bụng mềm.",
            FullCls = "- Công thức máu: WBC 6.5 G/L, RBC 4.02 T/L, HGB 122 g/L, PLT 230 G/L.\n- Đông máu cơ bản: PT-INR 1.00, APTT 0.93, Fibrinogen 3.05 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 4.8 mmol/L, Creatinin 68 µmol/L, AST 22 U/L, ALT 19 U/L, FT4 14.5 pmol/L, TSH 2.1 mIU/L, Điện giải đồ: Na 138, K 4.0, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính, RF (+), Anti-CCP (+).\n- Nhóm máu: A Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: MRI cột sống ngực - thắt lưng: Tổn thương phù tủy xương xẹp cấp tính thân đốt sống T12 và L1 (giảm tín hiệu trên T1W, tăng tín hiệu mạnh trên STIR), thành sau thân đốt còn nguyên vẹn, không chèn ép ống sống; Đốt L3 có hình ảnh xi măng sinh học cũ phân bố tốt.",
            BloodGroup = "A Rh(+)",
            BloodReserve = "Không (Dự trù phương tiện)",
            SurgeryMethod = "Tạo hình thân đốt sống T12, L1 bằng bơm xi măng sinh học có bóng/không bóng qua da (Vertebroplasty T12, L1)",
            Anesthesia = "Tiền mê + Gây tê tại chỗ",
            Surgeon = "TS. Nguyễn Văn Trung",
            SurgeryTime = "11 giờ 00 phút, ngày 02/10/2026",
            Risks = "Rò xi măng sinh học vào khoang ngoài màng cứng hoặc lỗ liên hợp gây chèn ép tủy/rễ thần kinh, rò xi măng vào hệ thống tĩnh mạch cạnh sống dẫn đến thuyên tắc mạch phổi, tụ máu tại vị trí chọc kim, gãy xẹp đốt sống lân cận sau can thiệp, nhiễm trùng thân đốt - đĩa đệm."
        });

        // 4. NGUYỄN THỊ XÔ (72t) - P722/G35 | PTV: BS Nguyễn Đức Hoàng
        list.Add(new PatientTarget {
            Stt = 4,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 722 - Giường số 35",
            PatientCode = "0004083361",
            PatientName = "NGUYỄN THỊ XÔ",
            Dob = "20/09/1954",
            Gender = "Nữ",
            Address = "Tỉnh Nam Định",
            InTime = "11:20 ngày 27/09/2026",
            Diagnosis = "Xẹp cấp thân đốt sống L1 do loãng xương / Thoái hóa cột sống thắt lưng",
            History = "Thoái hóa cột sống nhiều năm, loãng xương người già.",
            Course = "Bệnh nhân nữ 72 tuổi, tiền sử loãng xương và thoái hóa cột sống thắt lưng. Cách vào viện 5 ngày bệnh nhân bê chậu nước nhẹ thì nghe tiếng 'khục' ở lưng, sau đó đau nhói vùng thắt lưng dữ dội, đau tăng khi đứng, ngồi và khi ho/hắt hơi, chỉ nằm ngửa bất động trên giường mới đỡ đau. Vào BV Bạch Mai, chụp MRI cột sống thắt lưng cho hình ảnh xẹp cấp tính đốt sống L1 giảm chiều cao thân đốt khoảng 30%, phù tủy xương rõ rệt trên chuỗi xung STIR, không có mảnh xương lồi vào ống sống. Bệnh nhân được chỉ định phẫu thuật tạo hình thân đốt sống L1 bằng bơm xi măng sinh học qua da để giảm đau nhanh và làm vững cột sống.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 74 ck/p, HA 130/75 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng già yếu. Cột sống thắt lưng: Đau chói khu trú tại gai sau L1, gõ dồn đau tăng; co cứng nhóm cơ dựng gai lưng; vận động cột sống hạn chế nhiều do đau; khám thần kinh chi dưới: cơ lực 5/5, cảm giác bình thường, không có dấu hiệu chèn ép rễ hay tủy; đại tiểu tiện bình thường. Tim mạch, hô hấp không phát hiện bệnh lý cấp tính.",
            FullCls = "- Công thức máu: WBC 6.8 G/L, RBC 4.10 T/L, HGB 124 g/L, PLT 225 G/L.\n- Đông máu cơ bản: PT-INR 1.01, APTT 0.95, Fibrinogen 3.12 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 5.0 mmol/L, Creatinin 70 µmol/L, AST 24 U/L, ALT 21 U/L, Điện giải đồ: Na 139, K 4.1, Cl 102 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Xẹp cấp tính thân đốt sống L1 giảm 30% chiều cao, phù nề tủy xương lan tỏa trên STIR; thoái hóa nhẹ các tầng lân cận, không có hẹp ống sống.",
            BloodGroup = "O Rh(+)",
            BloodReserve = "Không",
            SurgeryMethod = "Tạo hình thân đốt sống L1 bằng bơm xi măng sinh học qua da (Vertebroplasty L1)",
            Anesthesia = "Tiền mê + Gây tê tại chỗ",
            Surgeon = "BS Nguyễn Đức Hoàng",
            SurgeryTime = "12 giờ 30 phút, ngày 02/10/2026",
            Risks = "Tràn xi măng ra ngoài thân đốt sống gây chèn ép tủy/rễ, rò xi măng vào tĩnh mạch cạnh sống gây tắc mạch phổi, tụ máu mô mềm vị trí chọc kim, xẹp thứ phát các đốt sống kế cận, phản ứng dị ứng hoặc tụt huyết áp thoáng qua khi bơm xi măng monomer."
        });

        // 5. TRẦN THỊ VÒNG (60t) - P722/G36 | PTV: BS Nguyễn Đức Hoàng
        list.Add(new PatientTarget {
            Stt = 5,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 722 - Giường số 36",
            PatientCode = "0003386201",
            PatientName = "TRẦN THỊ VÒNG",
            Dob = "14/11/1966",
            Gender = "Nữ",
            Address = "Tỉnh Bắc Giang",
            InTime = "08:45 ngày 28/09/2026",
            Diagnosis = "Xẹp cấp thân đốt sống T12 do loãng xương / Xẹp cũ L1 - Viêm da nấm thân mình",
            History = "Loãng xương, xẹp cũ L1 điều trị bảo tồn ổn định.",
            Course = "Bệnh nhân nữ 60 tuổi, tiền sử xẹp cũ đốt sống L1. Khoảng 4 ngày trước vào viện bệnh nhân hắt hơi mạnh sau đó xuất hiện đau nhói vùng lưng ngực - thắt lưng, đau nhiều khi xoay trở người và ngồi dậy, dùng thuốc giảm đau đường uống đáp ứng kém. Vào BV Bạch Mai khám, chụp MRI cột sống phát hiện xẹp cấp tính thân đốt sống T12 phù nề tủy xương mới, đốt L1 xẹp cũ xơ hóa không có tín hiệu phù cấp. Được chỉ định phẫu thuật tạo hình thân đốt sống T12 bằng bơm xi măng sinh học qua da (Vertebroplasty T12).",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 80 ck/p, HA 120/80 mmHg, T° 36.7°C, SpO2 99%. BN tỉnh táo. Khám cột sống: Co rút cơ cạnh sống vùng ngực 11-12; ấn đau chói gai sau T12, gõ dồn dọc cột sống đau tăng tại T12; không có triệu chứng thần kinh khu trú chi dưới (cơ lực hai chân 5/5, cảm giác bình thường, phản xạ gân xương bình thường, đại tiểu tiện tự chủ). Da vùng lưng vị trí dự kiến chọc kim nguyên vẹn, không có tổn thương nhiễm trùng tại chỗ. Tim phổi bình thường.",
            FullCls = "- Công thức máu: WBC 6.2 G/L, RBC 4.15 T/L, HGB 128 g/L, PLT 240 G/L.\n- Đông máu cơ bản: PT-INR 1.00, APTT 0.91, Fibrinogen 3.00 g/L.\n- Sinh hóa máu: Glucose 5.2 mmol/L, Ure 4.9 mmol/L, Creatinin 69 µmol/L, AST 23 U/L, ALT 20 U/L, Điện giải đồ: Na 140, K 4.0, Cl 102 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: MRI cột sống: Tổn thương phù tủy xương xẹp cấp tính thân đốt sống T12 trên chuỗi xung STIR, giảm trên T1W, thành sau thân đốt nguyên vẹn; đốt L1 xẹp cũ giảm chiều cao ổn định, không có phù tủy cấp.",
            BloodGroup = "B Rh(+)",
            BloodReserve = "Không",
            SurgeryMethod = "Tạo hình thân đốt sống T12 bằng bơm xi măng sinh học qua da (Vertebroplasty T12)",
            Anesthesia = "Tiền mê + Gây tê tại chỗ",
            Surgeon = "BS Nguyễn Đức Hoàng",
            SurgeryTime = "14 giờ 00 phút, ngày 02/10/2026",
            Risks = "Tràn xi măng vào ống sống gây tổn thương nón tủy/rễ thần kinh, tắc mạch phổi do xi măng đi vào tĩnh mạch, tụ máu vết chọc, nhiễm trùng mô mềm hoặc đĩa đệm, tổn thương màng phổi khi tiếp cận đốt ngực T12."
        });

        // 6. TRƯƠNG THỊ THI (68t) - P735/G81 | PTV: BS Lê Đăng Tân
        list.Add(new PatientTarget {
            Stt = 6,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 735 - Giường số 81",
            PatientCode = "0003362121",
            PatientName = "TRƯƠNG THỊ THI",
            Dob = "08/10/1958",
            Gender = "Nữ",
            Address = "Tỉnh Thái Bình",
            InTime = "14:00 ngày 27/09/2026",
            Diagnosis = "Xẹp cấp đốt sống T4, T10 do loãng xương nặng / Đã bơm xi măng T6, T10, T12 - Đái tháo đường típ 2 - Tăng huyết áp - Loãng xương",
            History = "Đái tháo đường típ 2 điều trị thuốc viên/Insulin; Tăng huyết áp; Loãng xương nặng đã từng BXM các đốt T6, T10, T12 trước đây.",
            Course = "Bệnh nhân nữ 68 tuổi, tiền sử đái tháo đường típ 2, tăng huyết áp, loãng xương nặng đã từng can thiệp bơm xi măng nhiều đốt sống ngực trước đây. Đợt này xuất hiện đau lưng và ngực nhiều, đau buốt dữ dội khi hít thở sâu và khi xoay người, hạn chế vận động nhiều. Chụp MRI cột sống ngực phát hiện xẹp cấp mới phù tủy xương đốt T4 và xẹp lún tiến triển đốt T10 bên cạnh các đốt T6, T12 đã có xi măng cũ ổn định. Đã được kiểm soát đường huyết và huyết áp ổn định tại nội trú, hội chẩn chỉ định phẫu thuật tạo hình thân đốt sống T4, T10 bằng bơm xi măng sinh học qua da.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 78 ck/p, HA 135/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng trung bình. Cột sống ngực: Ấn đau chói tại gai sau T4 và T10; cơ cạnh sống co cứng; không có dấu hiệu chèn ép tủy ngực (cơ lực hai chân 5/5, cảm giác hai chân bình thường, Babinski (-), không rối loạn cơ tròn). Tim T1 T2 rõ, phổi hai bên thông khí đều, bụng mềm.",
            FullCls = "- Công thức máu: WBC 7.2 G/L, RBC 4.05 T/L, HGB 120 g/L, PLT 235 G/L.\n- Đông máu cơ bản: PT-INR 1.02, APTT 0.94, Fibrinogen 3.20 g/L.\n- Sinh hóa máu: Glucose 6.8 mmol/L, HbA1c 7.1%, Ure 5.6 mmol/L, Creatinin 75 µmol/L, AST 26 U/L, ALT 24 U/L, Điện giải đồ: Na 138, K 4.1, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: MRI cột sống ngực: Xẹp cấp mới phù tủy xương thân đốt sống T4 và xẹp lún tiến triển đốt T10 trên nền loãng xương; các đốt T6, T10, T12 có bóng mờ xi măng cũ ổn định.",
            BloodGroup = "O Rh(+)",
            BloodReserve = "Không",
            SurgeryMethod = "Tạo hình thân đốt sống T4, T10 bằng bơm xi măng sinh học qua da (Vertebroplasty T4, T10)",
            Anesthesia = "Tiền mê + Gây tê tại chỗ",
            Surgeon = "BS Lê Đăng Tân",
            SurgeryTime = "15 giờ 30 phút, ngày 02/10/2026",
            Risks = "Tràn xi măng vào ống sống gây tổn thương tủy ngực, tràn xi măng vào tĩnh mạch gây thuyên tắc phổi, tràn khí màng phổi khi chọc đốt sống ngực cao (T4), tụ máu vết mổ, nhiễm trùng trên nền ĐTĐ, xẹp các đốt sống kế cận do loãng xương tiến triển."
        });

        // 7. LỤC ANH TUẤN (40t) - P740.1/G78 | PTV: BS Lê Đăng Tân · phụ: BS Đặng Nhật Quang
        list.Add(new PatientTarget {
            Stt = 7,
            RoomMng = "Phòng mổ 5 (CS)",
            RoomBed = "Phòng 740.1 - Giường số 78",
            PatientCode = "0004095785",
            PatientName = "LỤC ANH TUẤN",
            Dob = "10/05/1986",
            Gender = "Nam",
            Address = "Tỉnh Lạng Sơn",
            InTime = "16:30 ngày 29/09/2026",
            Diagnosis = "Đa chấn thương: Chấn thương cột sống vỡ xẹp D12, L3 - Gãy kín đầu dưới xương quay trái, gãy kín đài quay phải - Chấn thương ngực kín: Gãy cung sau xương sườn 1, 2 bên phải",
            History = "Khỏe mạnh, không tiền sử bệnh mạn tính.",
            Course = "Bệnh nhân nam 40 tuổi, bị tai nạn lao động ngã cao đập lưng và chống hai tay xuống đất. Sau ngã đau dữ dội vùng cột sống ngực thắt lưng, hạn chế vận động hai chân do đau, biến dạng đau sưng khuỷu tay phải và cổ tay trái, đau tức ngực phải khi hít thở. Cấp cứu BV Bạch Mai, chụp CT/MRI cột sống phát hiện vỡ lún đốt sống D12 mất vững cột sống và xẹp L3; chụp X-quang/CT khuỷu tay và cổ tay phát hiện gãy vỡ phức tạp đài quay phải độ III Mason, gãy đầu dưới xương quay trái; chụp CT ngực phát hiện gãy cung sau xương sườn 1, 2 phải không tràn khí/tràn máu màng phổi. Đã bất động bột hai tay, điều trị ổn định chấn thương ngực kín. Chỉ định phẫu thuật cố định cột sống lối sau bằng nẹp vít qua cuống D12-L3 kết hợp phẫu thuật lấy bỏ chỏm xương quay phải.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 84 ck/p, HA 125/80 mmHg, T° 36.8°C, SpO2 99%. BN tỉnh táo, thể trạng tốt. Cột sống: Biến dạng gù nhẹ vùng ngực-thắt lưng, ấn đau chói D12 và L3; cơ lực hai chi dưới 4+/5 do đau; cảm giác nông sâu bình thường; phản xạ gân xương bình thường; tiểu tiện bình thường. Tay phải: Nẹp bột cẳng bàn tay, sưng nề đau chói vùng chỏm quay, hạn chế sấp ngửa; tay trái: bột cẳng bàn tay trái gãy đầu dưới xương quay; ngực phải: đau tức nhẹ cung sau sườn 1-2, rì rào phế nang 2 phổi rõ. Bụng mềm.",
            FullCls = "- Công thức máu: WBC 9.1 G/L, RBC 4.35 T/L, HGB 134 g/L, PLT 255 G/L.\n- Đông máu cơ bản: PT-INR 1.01, APTT 0.95, Fibrinogen 3.30 g/L.\n- Sinh hóa máu: Glucose 5.6 mmol/L, Ure 5.4 mmol/L, Creatinin 76 µmol/L, AST 32 U/L, ALT 28 U/L, Điện giải đồ: Na 139, K 4.2, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: CT/MRI cột sống: Vỡ lún thân đốt sống D12 mất vững và xẹp vỡ L3; CT khuỷu tay phải: Gãy phức tạp nhiều mảnh chỏm xương quay phải độ III Mason; X-quang cổ tay trái: Gãy đầu dưới xương quay trái di lệch ít; CT lồng ngực: Gãy cung sau xương sườn 1, 2 phải, không có tràn máu/tràn khí màng phổi.",
            BloodGroup = "B Rh(+)",
            BloodReserve = "700",
            SurgeryMethod = "Phẫu thuật cố định cột sống lối sau bằng nẹp vít qua cuống D12-L3 giải ép + Phẫu thuật lấy bỏ chỏm xương quay phải",
            Anesthesia = "Gây mê nội khí quản",
            Surgeon = "BS Lê Đăng Tân · phụ: BS Đặng Nhật Quang",
            SurgeryTime = "17 giờ 00 phút, ngày 02/10/2026",
            Risks = "Chảy máu trong mổ, rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh, tổn thương dây thần kinh quay (nhánh sâu thần kinh gian cốt sau) khi phẫu thuật lấy chỏm quay, mất vững khớp khuỷu/quay trụ trên, tụ máu vết mổ, biến chứng hô hấp do gãy xương sườn."
        });

        // =========================================================================
        // PHÒNG MỔ 2 (CHẤN THƯƠNG CHỈNH HÌNH) - 8 CA
        // =========================================================================

        // 8. ĐINH THỊ VẺ (62t) - P723/G47 | PTV: BS Đặng Hoàng Giang
        list.Add(new PatientTarget {
            Stt = 8,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 723 - Giường số 47",
            PatientCode = "0002048483",
            PatientName = "ĐINH THỊ VẺ",
            Dob = "15/04/1964",
            Gender = "Nữ",
            Address = "Tỉnh Ninh Bình",
            InTime = "14:20 ngày 25/09/2026",
            Diagnosis = "Hoại tử vô mạch chỏm xương đùi hai bên giai đoạn IV (Bên phải > Bên trái) / Tăng tiểu cầu - Tăng huyết áp - Đái tháo đường típ 2",
            History = "Tăng huyết áp, Đái tháo đường típ 2, Tăng tiểu cầu nguyên phát (đã dừng Aspirin cách 2 tháng, đã HC Huyết học & Tim mạch).",
            Course = "Bệnh nhân nữ 62 tuổi, tiền sử đái tháo đường, tăng huyết áp, tăng tiểu cầu. Đau khớp háng hai bên tăng dần 2 năm nay, bên phải nặng hơn bên trái. Khoảng 3 tháng nay khớp háng phải đau buốt nhiều khi đi lại và tì đè, hạn chế gấp duỗi và xoay khớp háng, đi khập khiễng, teo nhẹ cơ đùi phải. Chụp X-quang và MRI khớp háng hai bên chẩn đoán hoại tử vô khuẩn chỏm xương đùi hai bên độ IV theo Ficat, chỏm xương đùi phải xẹp biến dạng nặng, mất khe khớp háng phải. Bệnh nhân đã dừng Aspirin đủ thời gian, được hội chẩn Huyết học, Tim mạch, Nội tiết chuẩn bị kỹ lưỡng -> Chỉ định phẫu thuật thay toàn bộ khớp háng phải.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 78 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng tốt. Khám khớp háng phải: Ấn đau điểm khớp háng trước và sau; nghiệm pháp Patrick (+); biên độ vận động khớp háng phải hạn chế: gấp 80°, duỗi 0°, khép 15°, dạng 20°, xoay trong/xoay ngoài hạn chế nhiều do đau; teo cơ tứ đầu đùi phải 1.5 cm so với bên trái; chiều dài chi dưới phải ngắn hơn chân trái 0.5 cm; tuần hoàn và cảm giác mu bàn chân tốt. Tim đều, phổi sáng, bụng mềm.",
            FullCls = "- Công thức máu: WBC 7.8 G/L, RBC 4.20 T/L, HGB 128 g/L, PLT 520 G/L (Tăng tiểu cầu - Đã HC Huyết học cho phép mổ).\n- Đông máu cơ bản: PT-INR 1.02, APTT 0.95, Fibrinogen 3.40 g/L.\n- Sinh hóa máu: Glucose 6.5 mmol/L, HbA1c 6.9%, Ure 5.2 mmol/L, Creatinin 72 µmol/L, AST 24 U/L, ALT 22 U/L, Điện giải đồ: Na 138, K 4.1, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: X-quang và MRI khớp háng hai bên: Hình ảnh hoại tử vô mạch chỏm xương đùi hai bên độ IV (Ficat), chỏm xương đùi phải xẹp biến dạng, mất khe khớp háng phải, xơ đặc và khuyết xương dưới sụn ổ cối.",
            BloodGroup = "O Rh(+)",
            BloodReserve = "350",
            SurgeryMethod = "Phẫu thuật thay toàn bộ khớp háng phải bằng khớp háng nhân tạo không xi măng (Total Hip Arthroplasty - THA)",
            Anesthesia = "Gây tê tủy sống kết hợp gây tê ngoài màng cứng giảm đau (hoặc Mê NKQ)",
            Surgeon = "BS Đặng Hoàng Giang",
            SurgeryTime = "08 giờ 00 phút, ngày 02/10/2026",
            Risks = "Chảy máu trong và sau mổ, trật khớp háng nhân tạo, gãy xương đùi/ổ cối quanh chuôi khớp nhân tạo, tổn thương thần kinh ngồi (thần kinh hông to), biến cố tắc mạch huyết khối tĩnh mạch sâu do tăng tiểu cầu, nhiễm trùng khớp háng nhân tạo trên nền ĐTĐ."
        });

        // 9. HOÀNG MẠNH THƯỜNG (28t) - P730/G92 | PTV: BS. Hà Đức Cường
        list.Add(new PatientTarget {
            Stt = 9,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 730 - Giường số 92",
            PatientCode = "0002254037",
            PatientName = "HOÀNG MẠNH THƯỜNG",
            Dob = "22/10/1997",
            Gender = "Nam",
            Address = "Tỉnh Hưng Yên",
            InTime = "09:15 ngày 29/09/2026",
            Diagnosis = "Đứt hoàn toàn dây chằng chéo trước khớp gối trái / Thoái hóa nhẹ sụn khớp gối trái",
            History = "Khỏe mạnh, tiền sử chấn thương thể thao đá bóng cách 2 tháng.",
            Course = "Bệnh nhân nam 28 tuổi, tiền sử chấn thương gối trái khi đá bóng cách đây 2 tháng (nghe tiếng 'rắc', sưng nề đau khớp gối nhiều). Sau đó gối đỡ nề nhưng bệnh nhân cảm giác lỏng khớp gối trái, bước hụt chân khi đi cầu thang và chạy nhanh, thường xuyên có cảm giác kẹt khớp nhẹ. Đến khám tại BV Bạch Mai, chụp MRI khớp gối trái phát hiện đứt hoàn toàn dây chằng chéo trước (ACL), rách nhẹ sụn chêm trong độ II, tràn dịch khớp gối mức độ nhẹ. Bệnh nhân có nguyện vọng chơi thể thao trở lại, hội chẩn chỉ định phẫu thuật nội soi tái tạo dây chằng chéo trước gối trái bằng gân tự thân (Hamstring).",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.5°C, SpO2 99%. BN thể trạng tốt. Khám gối trái: Gối không sưng đỏ, dấu hiệu chạm xương bánh chè (-); nghiệm pháp Ngăn kéo trước (+) 2+; Lachman (+) 2+ không có điểm dừng chắc; Pivot shift (+); nghiệm pháp McMurray (-) rách sụn chêm; biên độ vận động gối trái gấp 130°, duỗi 0°; cơ lực chi dưới trái 5/5; mạch mu chân và chày sau bắt rõ.",
            FullCls = "- Công thức máu: WBC 6.4 G/L, RBC 4.45 T/L, HGB 138 g/L, PLT 230 G/L.\n- Đông máu cơ bản: PT-INR 1.00, APTT 0.92, Fibrinogen 2.95 g/L.\n- Sinh hóa máu: Glucose 5.1 mmol/L, Ure 4.7 mmol/L, Creatinin 72 µmol/L, AST 21 U/L, ALT 18 U/L, Điện giải đồ: Na 140, K 4.1, Cl 102 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: MRI khớp gối trái: Mất liên tục hoàn toàn dây chằng chéo trước (ACL), rách sừng sau sụn chêm trong độ II, tràn dịch khoang bao hoạt dịch khớp gối mức độ nhẹ.",
            BloodGroup = "B Rh(+)",
            BloodReserve = "Không",
            SurgeryMethod = "Phẫu thuật nội soi tái tạo dây chằng chéo trước khớp gối trái bằng gân tự thân (Gân bán gân và gân thon - Hamstring)",
            Anesthesia = "Gây tê tủy sống",
            Surgeon = "BS. Hà Đức Cường",
            SurgeryTime = "09 giờ 30 phút, ngày 02/10/2026",
            Risks = "Chảy máu tụ dịch khớp gối sau mổ, tổn thương sụn khớp trong quá trình khoan đường hầm, lỏng hoặc đứt lại mảnh ghép sau mổ, cứng khớp gối do xơ dính, nhiễm trùng khớp gối nội soi, tổn thương thần kinh hiển khi lấy gân Hamstring."
        });

        // 10. NGUYỄN VĂN PHƯƠNG (58t) - P712/G23 | PTV: BS Đặng Hoàng Giang
        list.Add(new PatientTarget {
            Stt = 10,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 712 - Giường số 23",
            PatientCode = "0002740977",
            PatientName = "NGUYỄN VĂN PHƯƠNG",
            Dob = "10/06/1967",
            Gender = "Nam",
            Address = "Tỉnh Bắc Ninh",
            InTime = "08:15 ngày 25/09/2026",
            Diagnosis = "Thoái hóa khớp gối phải nặng giai đoạn IV - Gai xương chèn ép / Nốt đặc thùy trên phổi trái Lung-RADS 4X",
            History = "Thoái hóa khớp gối phải hơn 5 năm; Nốt đặc thùy trên phổi trái theo dõi Lung-RADS 4X (đã HC Hô hấp/Lồng ngực cho phép mổ gối trước).",
            Course = "Bệnh nhân nam 58 tuổi, đau khớp gối phải kéo dài nhiều năm, điều trị nội khoa tiêm khớp nhiều đợt không hiệu quả. Khoảng 6 tháng nay đau dữ dội khi đi lại và tì đè, khớp gối biến dạng vẹo trong (varus deformity), phát ra tiếng lục khục khi vận động, đi bộ dưới 100m. Chụp X-quang và MRI gối phải cho thấy hẹp khe khớp hoàn toàn khoang trong, gai xương lớn bờ khớp, xơ đặc xương dưới sụn giai đoạn IV theo Kellgren-Lawrence. CT lồng ngực phát hiện nốt đặc thùy trên phổi trái Lung-RADS 4X đã được hội chẩn chuyên khoa Hô hấp đánh giá chức năng hô hấp bình thường, cho phép tiến hành phẫu thuật thay khớp gối -> Chỉ định phẫu thuật thay toàn bộ khớp gối phải.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh táo. Khám gối phải: Khớp gối biến dạng vẹo trong khoảng 10 độ; ấn đau chói khe khớp trong; dấu hiệu bào khớp (+); biên độ vận động: gấp 90°, duỗi thiếu 5° (co rút gập nhẹ); lỏng khớp nhẹ dây chằng bên ngoài thứ phát; tuần hoàn và thần kinh mu chân nguyên vẹn. Khám phổi thông khí đều 2 bên, không rale. Tim mạch bình thường.",
            FullCls = "- Công thức máu: WBC 6.9 G/L, RBC 4.30 T/L, HGB 130 g/L, PLT 240 G/L.\n- Đông máu cơ bản: PT-INR 1.01, APTT 0.94, Fibrinogen 3.15 g/L.\n- Sinh hóa máu: Glucose 5.5 mmol/L, Ure 5.1 mmol/L, Creatinin 74 µmol/L, AST 25 U/L, ALT 22 U/L, Điện giải đồ: Na 139, K 4.1, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: A Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: X-quang gối thẳng - nghiêng và MRI khớp gối: Thoái hóa khớp gối độ IV (Kellgren-Lawrence), hẹp hoàn toàn khe khớp khoang trong, gai xương mâm chày và lồi cầu đùi lớn, xơ đặc xương dưới sụn; biến dạng trục chi vẹo trong; CT ngực: Nốt đặc thùy trên phổi trái đường kính 8mm Lung-RADS 4X (HC Hô hấp theo dõi định kỳ sau mổ).",
            BloodGroup = "A Rh(+)",
            BloodReserve = "350",
            SurgeryMethod = "Phẫu thuật thay toàn bộ khớp gối phải bằng khớp nhân tạo (Total Knee Arthroplasty - TKA)",
            Anesthesia = "Gây tê tủy sống + Gây tê mặt phẳng cơ vuông thắt lưng/khoang cơ khép giảm đau sau mổ",
            Surgeon = "BS Đặng Hoàng Giang",
            SurgeryTime = "11 giờ 00 phút, ngày 02/10/2026",
            Risks = "Chảy máu vết mổ, tụ máu khớp gối, tắc mạch huyết khối tĩnh mạch sâu chi dưới, tổn thương thần kinh mác chung gây yếu bàn chân rủ, cứng khớp gối do xơ dính sau mổ, trật hoặc mất vững khớp gối nhân tạo, nhiễm trùng khớp gối nhân tạo."
        });

        // 11. NGUYỄN THỊ TÍNH (93t) - P740.2/G65 | PTV: BS Đặng Nhật Quang
        list.Add(new PatientTarget {
            Stt = 11,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 740.2 - Giường số 65",
            PatientCode = "0004094236",
            PatientName = "NGUYỄN THỊ TÍNH",
            Dob = "10/01/1933",
            Gender = "Nữ",
            Address = "Thành phố Hà Nội",
            InTime = "15:00 ngày 28/09/2026",
            Diagnosis = "Gãy kín phức tạp liên mấu chuyển xương đùi trái di lệch / Đái tháo đường típ 2 - Tăng huyết áp - Loãng xương người già nặng",
            History = "Tăng huyết áp, Đái tháo đường típ 2 điều trị thường xuyên, thể trạng người cao tuổi 93 tuổi.",
            Course = "Bệnh nhân nữ 93 tuổi, trượt chân ngã đập mông và hông trái xuống nền cứng tại nhà. Sau ngã đau chói vùng háng đùi trái, bất lực vận động hoàn toàn chân trái, không thể ngồi dậy. Được người nhà đưa vào BV Bạch Mai cấp cứu. Chụp X-quang khớp háng và đùi trái chẩn đoán gãy kín liên mấu chuyển xương đùi trái loại mất vững theo phân loại AO/OTA 31-A2. Bệnh nhân cao tuổi có bệnh nền tăng huyết áp, đái tháo đường, nguy cơ biến chứng do nằm lâu (viêm phổi ứ đọng, loét tì đè, huyết khối mạch). Đã được hội chẩn Tim mạch, Gây mê hồi sức đánh giá nguy cơ phẫu thuật -> Chỉ định phẫu thuật kết hợp xương bằng đinh nội tủy chốt đầu trên xương đùi (PFNA/Gamma nail) ít xâm lấn để phục hồi vận động sớm.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 80 ck/p, HA 140/85 mmHg, T° 36.7°C, SpO2 97%. BN tỉnh, tiếp xúc được, già yếu. Khám háng - đùi trái: Chi dưới trái ngắn hơn bên phải 2 cm, bàn chân đổ ngoài sát mặt giường; tam giác Scarpa đầy, ấn đau chói vùng mấu chuyển lớn đùi trái; cử động chân trái gây đau dữ dội, bất lực vận động hoàn toàn; mạch mu chân và chày sau trái bắt rõ; cảm giác mu chân bình thường. Tim đều T1 T2 rõ, phổi đáy có ít rale ẩm ứ đọng do nằm, bụng mềm.",
            FullCls = "- Công thức máu: WBC 8.1 G/L, RBC 3.80 T/L, HGB 112 g/L, PLT 215 G/L.\n- Đông máu cơ bản: PT-INR 1.03, APTT 0.96, Fibrinogen 3.25 g/L.\n- Sinh hóa máu: Glucose 6.9 mmol/L, HbA1c 7.2%, Ure 6.2 mmol/L, Creatinin 82 µmol/L, AST 28 U/L, ALT 25 U/L, Điện giải đồ: Na 137, K 4.0, Cl 100 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: X-quang khớp háng và đùi trái: Gãy kín liên mấu chuyển xương đùi trái nhiều mảnh di lệch (loại mất vững AO 31-A2), góc cổ thân xương đùi giảm, loãng xương nặng.",
            BloodGroup = "O Rh(+)",
            BloodReserve = "350",
            SurgeryMethod = "Phẫu thuật kết hợp xương đùi trái bằng đinh nội tủy có chốt Gamma/PFNA dưới màn tăng sáng C-arm",
            Anesthesia = "Gây tê tủy sống liều thấp (hoặc Gây mê nội khí quản)",
            Surgeon = "BS Đặng Nhật Quang",
            SurgeryTime = "12 giờ 30 phút, ngày 02/10/2026",
            Risks = "Nguy cơ tim mạch và hô hấp chu phẫu cao ở bệnh nhân 93 tuổi, chảy máu trong mổ, tụt huyết áp khi gây tê, gãy thêm mảnh xương đùi khi đóng đinh, cut-out đinh vít chốt qua chỏm do loãng xương nặng, huyết khối tĩnh mạch sâu thuyên tắc phổi, loét tì đè và nhiễm trùng vết mổ."
        });

        // 12. PHẠM THỊ LAN (56t) - P735/G81 | PTV: BS Ngô Đăng Quang
        list.Add(new PatientTarget {
            Stt = 12,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 735 - Giường số 81",
            PatientCode = "0004095977",
            PatientName = "PHẠM THỊ LAN",
            Dob = "18/03/1970",
            Gender = "Nữ",
            Address = "Tỉnh Vĩnh Phúc",
            InTime = "10:00 ngày 29/09/2026",
            Diagnosis = "Hội chứng ống cổ tay hai bên mức độ nặng (Bên phải > Bên trái)",
            History = "Khỏe mạnh, làm công việc nội trợ/thủ công sử dụng cổ tay nhiều năm.",
            Course = "Bệnh nhân nữ 56 tuổi, tê bì dị cảm các ngón 1, 2, 3 và nửa ngón 4 bàn tay hai bên kéo dài hơn 1 năm, bên phải tê nhiều hơn bên trái. Tê bì tăng nhiều về đêm làm bệnh nhân thức giấc, phải lắc vẫy bàn tay mới đỡ, gần đây xuất hiện teo nhẹ cơ mô cái bàn tay phải, cầm nắm đồ vật hay bị rơi. Đi khám tại BV Bạch Mai, được làm điện cơ (EMG) ghi nhận dẫn truyền thần kinh giữa qua cổ tay hai bên chậm nặng (Hội chứng ống cổ tay mức độ nặng). Điều trị nội khoa nẹp cổ tay và dùng vitamin B không đỡ -> Chỉ định phẫu thuật cắt dây chằng vòng cổ tay ngang giải phóng thần kinh giữa hai bên.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 75 ck/p, HA 120/75 mmHg, T° 36.5°C, SpO2 99%. Thể trạng tốt. Khám bàn tay hai bên: Teo nhẹ cơ mô cái bàn tay phải; nghiệm pháp Tinel (+) cổ tay hai bên; nghiệm pháp Phalen (+) xuất hiện tê bì sau 20 giây; giảm cảm giác nông ngón 1, 2, 3 và nửa ngoài ngón 4 bàn tay phải; cơ lực đối chiếu ngón cái bàn tay phải 4/5; tuần hoàn đầu ngón tay hai bên hồng ấm, mao mạch hồi lưu < 2s. Tim phổi bình thường.",
            FullCls = "- Công thức máu: WBC 6.1 G/L, RBC 4.10 T/L, HGB 124 g/L, PLT 225 G/L.\n- Đông máu cơ bản: PT-INR 1.00, APTT 0.92, Fibrinogen 2.90 g/L.\n- Sinh hóa máu: Glucose 5.2 mmol/L, Ure 4.8 mmol/L, Creatinin 68 µmol/L, AST 20 U/L, ALT 18 U/L, Điện giải đồ: Na 139, K 4.0, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: AB Rh(+).\n- TPT nước tiểu: Bình thường.\n- Thăm dò chức năng: Điện cơ (EMG) thần kinh chi trên: Tổn thương sợi trục và myelin đoạn qua cổ tay của dây thần kinh giữa hai bên mức độ nặng (Bên phải > Bên trái), kéo dài thời gian tiềm vận động và cảm giác.",
            BloodGroup = "AB Rh(+)",
            BloodReserve = "Không",
            SurgeryMethod = "Phẫu thuật giải ép thần kinh giữa - Cắt dây chằng vòng cổ tay ngang (Cắt mạc hãm gân gấp) cổ tay hai bên",
            Anesthesia = "Gây tê tại chỗ (hoặc Tê đám rối thần kinh cánh tay)",
            Surgeon = "BS Ngô Đăng Quang",
            SurgeryTime = "14 giờ 00 phút, ngày 02/10/2026",
            Risks = "Tổn thương nhánh vận động quặt ngược cơ mô cái của thần kinh giữa, tổn thương cung mạch gan tay nông, chảy máu tụ máu vết mổ, sẹo đau phì đại vùng gan bàn tay, đau phức hợp khu vực (CRPS), tê bì kéo dài sau mổ do tổn thương sợi thần kinh mạn tính."
        });

        // 13. NGUYỄN THỊ DỤNG (68t) - P712/G12 | PTV: BS. Hà Đức Cường
        list.Add(new PatientTarget {
            Stt = 13,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 712 - Giường số 12",
            PatientCode = "0004099240",
            PatientName = "NGUYỄN THỊ DỤNG",
            Dob = "14/07/1958",
            Gender = "Nữ",
            Address = "Tỉnh Hà Nam",
            InTime = "11:00 ngày 30/09/2026",
            Diagnosis = "Thoái hóa khớp gối hai bên giai đoạn IV - Hẹp nặng khe khớp gối trái / Loãng xương - Tăng huyết áp",
            History = "Tăng huyết áp, loãng xương, thoái hóa khớp gối 2 bên nhiều năm.",
            Course = "Bệnh nhân nữ 68 tuổi, đau khớp gối hai bên kéo dài nhiều năm, gối trái nặng hơn gối phải. Bệnh nhân đau nhiều khi đi lại, khó khăn khi lên xuống cầu thang và ngồi xổm, gối trái biến dạng cong vẹo trục chi. Đã điều trị nội khoa nhiều đợt, uống thuốc giảm đau và tiêm chất nhờn nội khớp đáp ứng kém dần. Chụp X-quang và MRI gối trái cho thấy thoái hóa khớp gối độ IV theo Kellgren-Lawrence, mất khe khớp khoang trong, gai xương mâm chày và xương đùi lớn, xơ đặc xương dưới sụn. Hội chẩn chuyên khoa chỉ định phẫu thuật thay toàn bộ khớp gối trái.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 76 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh táo. Khám gối trái: Trục chi dưới trái vẹo trong (Varus) 8 độ; ấn đau chói dọc khe khớp gối trong; dấu hiệu bào khớp (+); biên độ vận động gối trái: gấp 95°, duỗi thiếu 5°; lỏng khớp nhẹ khi làm nghiệm pháp dạng ép; cơ lực chi dưới trái 5/5; mạch mu chân và chày sau bắt rõ. Khám tim phổi bình thường.",
            FullCls = "- Công thức máu: WBC 6.7 G/L, RBC 4.12 T/L, HGB 126 g/L, PLT 235 G/L.\n- Đông máu cơ bản: PT-INR 1.01, APTT 0.93, Fibrinogen 3.10 g/L.\n- Sinh hóa máu: Glucose 5.4 mmol/L, Ure 5.0 mmol/L, Creatinin 72 µmol/L, AST 24 U/L, ALT 21 U/L, Điện giải đồ: Na 139, K 4.1, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: O Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: X-quang gối thẳng - nghiêng và MRI khớp gối: Thoái hóa khớp gối độ IV (Kellgren-Lawrence), hẹp hoàn toàn khe khớp khoang trong, gai xương mâm chày và lồi cầu đùi lớn, xơ đặc xương dưới sụn; biến dạng trục chi vẹo trong.",
            BloodGroup = "O Rh(+)",
            BloodReserve = "350",
            SurgeryMethod = "Phẫu thuật thay toàn bộ khớp gối trái bằng khớp gối nhân tạo (TKA Trái)",
            Anesthesia = "Gây tê tủy sống + Gây tê vùng giảm đau sau mổ",
            Surgeon = "BS. Hà Đức Cường",
            SurgeryTime = "15 giờ 00 phút, ngày 02/10/2026",
            Risks = "Chảy máu trong và sau mổ, tụ máu khớp gối, tắc mạch huyết khối tĩnh mạch sâu chi dưới, tổn thương thần kinh mác chung, cứng khớp gối do dính sau mổ, nhiễm trùng khớp gối nhân tạo, lỏng hoặc mòn khớp nhân tạo về sau."
        });

        // 14. NGUYỄN THỊ TỊNH (76t) - P717/G10 | PTV: BS. Hà Đức Cường
        list.Add(new PatientTarget {
            Stt = 14,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 717 - Giường số 10",
            PatientCode = "0004099512",
            PatientName = "NGUYỄN THỊ TỊNH",
            Dob = "02/09/1950",
            Gender = "Nữ",
            Address = "Tỉnh Thanh Hóa",
            InTime = "14:30 ngày 30/09/2026",
            Diagnosis = "Hoại tử vô mạch chỏm xương đùi hai bên (Phải > Trái) giai đoạn IV / Sau phẫu thuật cố định cột sống thắt lưng 5 tháng - Loãng xương",
            History = "Đã phẫu thuật mổ nẹp vít cố định cột sống thắt lưng cách 5 tháng tại Khoa 57; Loãng xương.",
            Course = "Bệnh nhân nữ 76 tuổi, tiền sử mổ cố định nẹp vít cột sống thắt lưng cách 5 tháng ổn định. Bệnh nhân đau khớp háng hai bên tăng dần, bên phải đau buốt dữ dội lan xuống mặt trước đùi, hạn chế vận động nhiều, không tự đi lại được phải có người dìu. Chụp X-quang và MRI khớp háng hai bên phát hiện hoại tử vô mạch chỏm xương đùi hai bên độ IV, chỏm xương đùi phải xẹp dẹt hoàn toàn, khuyết xương ổ cối, viêm dính khớp háng. Bệnh nhân được hội chẩn thông qua mổ chỉ định phẫu thuật thay toàn bộ khớp háng phải bằng khớp nhân tạo.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 74 ck/p, HA 125/75 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh táo, thể trạng già. Vết mổ cũ cột sống thắt lưng liền sẹo tốt. Khám khớp háng phải: Ấn đau chói vùng khớp háng trước; Patrick (+); biên độ khớp háng phải: gấp 75°, duỗi 0°, khép 10°, dạng 15°, hạn chế xoay; ngắn chi dưới phải 1 cm; cơ lực chi dưới phải 4/5 do đau; mạch mu chân bắt rõ. Tim phổi không phát hiện bất thường.",
            FullCls = "- Công thức máu: WBC 6.5 G/L, RBC 3.90 T/L, HGB 118 g/L, PLT 210 G/L.\n- Đông máu cơ bản: PT-INR 1.02, APTT 0.94, Fibrinogen 3.15 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 5.2 mmol/L, Creatinin 75 µmol/L, AST 23 U/L, ALT 20 U/L, Điện giải đồ: Na 138, K 4.0, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: B Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: X-quang và MRI khớp háng hai bên: Hình ảnh hoại tử vô mạch chỏm xương đùi hai bên độ IV (Ficat), chỏm xương đùi phải xẹp biến dạng nặng, mất khe khớp háng phải, xơ đặc và khuyết xương dưới sụn ổ cối.",
            BloodGroup = "B Rh(+)",
            BloodReserve = "350",
            SurgeryMethod = "Phẫu thuật thay toàn bộ khớp háng phải bằng khớp háng nhân tạo không xi măng (THA Phải)",
            Anesthesia = "Gây tê tủy sống (hoặc Gây mê nội khí quản)",
            Surgeon = "BS. Hà Đức Cường",
            SurgeryTime = "16 giờ 30 phút, ngày 02/10/2026",
            Risks = "Chảy máu trong mổ, trật khớp háng nhân tạo sau mổ, gãy xương đùi hoặc vỡ ổ cối khi doa đóng chuôi khớp trên nền loãng xương, tổn thương thần kinh ngồi, tắc mạch huyết khối chi dưới, nhiễm trùng khớp nhân tạo."
        });

        // 15. BÙI THỊ TUYẾT (64t) - P717/G10 | PTV: BS Đặng Nhật Quang
        list.Add(new PatientTarget {
            Stt = 15,
            RoomMng = "Phòng mổ 2 (CTCH)",
            RoomBed = "Phòng 717 - Giường số 10",
            PatientCode = "0001934566",
            PatientName = "BÙI THỊ TUYẾT",
            Dob = "25/12/1962",
            Gender = "Nữ",
            Address = "Tỉnh Hòa Bình",
            InTime = "08:30 ngày 30/09/2026",
            Diagnosis = "Viêm màng hoạt dịch thể lông nốt sắc tố (PVNS) khớp vai trái / Thoái hóa khớp vai trái",
            History = "Khỏe mạnh, sưng đau khớp vai trái tái diễn hơn 1 năm.",
            Course = "Bệnh nhân nữ 64 tuổi, sưng nề và đau tức âm ỉ vùng khớp vai trái hơn 1 năm nay, đau tăng khi vận động đưa tay lên cao hoặc ra sau, cảm giác kẹt vướng trong khớp vai, đã điều trị nội khoa nhiều đợt không đỡ. Đi khám tại BV Bạch Mai, chụp MRI khớp vai trái phát hiện hình ảnh dày màng hoạt dịch lan tỏa nhiều nốt giảm tín hiệu trên cả T1W và T2W (lắng đọng hemosiderin), tràn dịch khớp vai mức độ vừa, hình ảnh điển hình của u màng hoạt dịch thể lông nốt sắc tố (PVNS) khớp vai trái. Hội chẩn chỉ định phẫu thuật mở bóc u, cắt bỏ toàn bộ màng hoạt dịch viêm khớp vai trái gửi làm xét nghiệm giải phẫu bệnh.",
            HoiChanTime = "14 giờ 00 phút, ngày 01 tháng 10 năm 2026",
            ExamSummary = "DHST: Mạch 76 ck/p, HA 120/80 mmHg, T° 36.6°C, SpO2 99%. Thể trạng tốt. Khám vai trái: Khớp vai trái sưng nề nhẹ hơn vai phải, không nóng đỏ; ấn đau tức rãnh nhị đầu và khe khớp vai trước sau; biên độ vận động khớp vai trái: dang 90°, gấp 100°, xoay ngoài 30°, xoay trong 40° (hạn chế do đau và kẹt cơ học); cơ lực đai vai trái 5/5; cảm giác và mạch đập đầu chi trên trái bình thường. Tim phổi bình thường, bụng mềm.",
            FullCls = "- Công thức máu: WBC 6.3 G/L, RBC 4.18 T/L, HGB 126 g/L, PLT 230 G/L.\n- Đông máu cơ bản: PT-INR 1.00, APTT 0.93, Fibrinogen 3.05 g/L.\n- Sinh hóa máu: Glucose 5.3 mmol/L, Ure 4.9 mmol/L, Creatinin 70 µmol/L, AST 22 U/L, ALT 19 U/L, Điện giải đồ: Na 139, K 4.1, Cl 101 mmol/L.\n- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.\n- Nhóm máu: A Rh(+).\n- TPT nước tiểu: Bình thường.\n- Chẩn đoán hình ảnh: MRI khớp vai trái: Dày không đều bao màng hoạt dịch khớp vai tạo thành nhiều nốt và khối giảm tín hiệu trên T1W và T2W (lắng đọng hemosiderin), tràn dịch bao hoạt dịch dưới mỏm cùng vai và ổ chảo cánh tay - Điển hình viêm màng hoạt dịch thể lông nốt sắc tố (PVNS).",
            BloodGroup = "A Rh(+)",
            BloodReserve = "Không",
            SurgeryMethod = "Phẫu thuật bóc u, cắt toàn bộ bao màng hoạt dịch viêm khớp vai trái làm giải phẫu bệnh",
            Anesthesia = "Gây mê nội khí quản + Tê đám rối thần kinh cánh tay giảm đau",
            Surgeon = "BS Đặng Nhật Quang",
            SurgeryTime = "17 giờ 30 phút, ngày 02/10/2026",
            Risks = "Chảy máu tụ máu khoang khớp vai, tổn thương thần kinh nách hoặc thần kinh trên vai, cứng khớp vai do dính sau mổ, tái phát khối u màng hoạt dịch sau phẫu thuật, nhiễm trùng vết mổ."
        });

        return list;
    }
}
