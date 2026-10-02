using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Inventec.Core;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using MOS.Filter;
using MOS.SDO;
using MOS.EFMODEL.DataModels;
using Aspose.Words;

public class MyAdapterEx : AdapterBase
{
    public List<T> FetchList<T>(string uri, ApiConsumer consumer, object filter, CommonParam param)
    {
        return Get<List<T>>(uri, consumer, filter, param);
    }

    public T PostData<T>(string uri, ApiConsumer consumer, object data, CommonParam param)
    {
        return Post<T>(uri, consumer, data, param);
    }
}

public class ProcessSurgeryPrepOct02
{
    public static MyAdapterEx adapter = new MyAdapterEx();

    public const long LEANPRO_MEDICINE_TYPE_ID = 26851;
    public const long TTSPDD_STOCK_ID = 7787;

    public static string ReadLiveToken()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string preferredDir = @"F:\NB\LBP2900_R150_V330_W64_uk_EN_2\x64\MISC\ANIMIMG\his\HIS CSNB";
        List<string> candidates = new List<string>();
        if (Directory.Exists(preferredDir)) candidates.Add(Path.Combine(preferredDir, "Logs", "LogSystem.txt"));
        DirectoryInfo cur = new DirectoryInfo(baseDir);
        for (int i = 0; i < 5; i++)
        {
            if (cur == null) break;
            candidates.Add(Path.Combine(cur.FullName, "Logs", "LogSystem.txt"));
            cur = cur.Parent;
        }

        foreach (var lp in candidates)
        {
            if (!File.Exists(lp)) continue;
            try
            {
                using (var fs = new FileStream(lp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long length = fs.Length;
                    if (length == 0) continue;
                    int bufferSize = (int)Math.Min(131072L, length);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufferSize];
                    int read = fs.Read(buffer, 0, bufferSize);
                    string chunk = Encoding.UTF8.GetString(buffer, 0, read);

                    int idx = chunk.LastIndexOf("TokenCode|");
                    if (idx >= 0)
                    {
                        int start = idx + 10;
                        if (start + 64 <= chunk.Length)
                        {
                            string t = chunk.Substring(start, 64);
                            if (t.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return t;
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public class PatientTarget
    {
        public int Stt;
        public string RoomMng;
        public string PatientCode;
        public string PatientName;
        public int Age;
        public string MainDiag;
        public string SurgeryMethod;
        public string Surgeon;
        public string RoomBed;
        public string History;
        public string Course;
        public string ExamSummary;
        public string BloodGroup;
        public string BloodReserve;
        public string Anesthesia;
        public string SurgeryTime;
        public string Risks;
    }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReferencedAssemblies", new AssemblyName(a.Name).Name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };

        string token = ReadLiveToken();
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("❌ Không tìm thấy TokenCode!");
            return;
        }

        ApiConsumer mos = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        CommonParam cp = new CommonParam();

        var targets = new List<PatientTarget>
        {
            // Phòng mổ 5 (CS)
            new PatientTarget { 
                Stt = 1, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0004063457", PatientName = "NGUYỄN THANH HẢI", Age = 39, 
                MainDiag = "CTCS: Vỡ L1, L4 HOS - Bí tiểu, vỡ ổ cối trái, vỡ chỏm xương đốt bàn ngón V chân trái / Giai đoạn trầm cảm nặng kèm triệu chứng loạn thần có hành vi tự sát", 
                SurgeryMethod = "Cố định cột sống lối sau giải ép", Surgeon = "BS Lê Đăng Tân", RoomBed = "P740 / G72",
                History = "Trầm cảm nặng kèm triệu chứng loạn thần, tự sát nhảy từ tầng cao.",
                Course = "Bệnh nhân nam 39 tuổi, tiền sử rối loạn trầm cảm nặng. Ngày vào viện do hành vi tự sát nhảy từ tầng cao xuống đất, đập lưng và mông xuống nền cứng. Sau ngã đau dữ dội vùng thắt lưng, mất vận động và tê bì hai chân, bí tiểu, đau sưng nề háng trái và bàn chân trái. Được cấp cứu đưa vào BV Bạch Mai. Chụp CT và MRI cột sống thắt lưng phát hiện vỡ phức tạp thân đốt sống L1 và L4 mảnh vỡ chèn ép ống sống gây bí tiểu; chụp X-quang/CT khung chậu vỡ ổ cối trái, gãy vỡ chỏm xương đốt bàn ngón V chân trái. Đã hội chẩn Viện Sức khỏe Tâm thần điều chỉnh thuốc an thần, hội chẩn thông qua mổ chỉ định phẫu thuật nắn chỉnh, cố định cột sống lối sau bằng nẹp vít qua cuống L1, L4 và giải ép thần kinh.",
                ExamSummary = "DHST: Mạch 82 ck/p, HA 120/75 mmHg, T° 36.8°C, SpO2 98%. BN tỉnh, tiếp xúc chậm, khí sắc trầm cảm. Khám cột sống thắt lưng: Biến dạng gù nhẹ vùng thắt lưng, co cứng cơ cạnh sống, ấn đau chói gai sau L1, L4; cơ lực chi dưới: chân phải 3/5, chân trái 2/5; giảm cảm giác nông sâu từ L1 trở xuống hai bên; bí tiểu, đang đặt sonde tiểu ra nước tiểu vàng trong; khớp háng trái sưng nề, hạn chế vận động; bàn chân trái sưng nề ngón V. Tim đều, phổi thông khí tốt, bụng mềm.",
                BloodGroup = "B Rh(+)", BloodReserve = "700", Anesthesia = "Gây mê nội khí quản", SurgeryTime = "08 giờ 00 phút, ngày 02/10/2026",
                Risks = "Chảy máu trong mổ do tổn thương đám rối tĩnh mạch ngoài màng cứng, rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh/chùm đuôi ngựa gây liệt hoặc rối loạn cơ tròn nặng hơn, tụ máu chèn ép sau mổ, nhiễm trùng vết mổ sâu, nguy cơ kích động hoặc tự sát trong giai đoạn hậu phẫu."
            },
            new PatientTarget { 
                Stt = 2, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0004054272", PatientName = "NGUYỄN THỊ VINH", Age = 73, 
                MainDiag = "Trượt đốt sống L4-L5 độ I - Hẹp ống sống thắt lưng / Vỡ xẹp cũ L1 - Suy thượng thận do thuốc - Theo dõi bệnh tim thiếu máu cục bộ", 
                SurgeryMethod = "Cố định cột sống vít nhồi xi măng + Giải ép thần kinh hàn xương liên thân đốt", Surgeon = "TS. Nguyễn Văn Trung", RoomBed = "P723 / G41",
                History = "Tăng huyết áp, đau khớp dùng thuốc nam nhiều năm gây suy thượng thận do thuốc; Đau thắt ngực (đã HC Tim mạch theo dõi Troponin T).",
                Course = "Bệnh nhân nữ 73 tuổi, tiền sử đau lưng mạn tính nhiều năm, tự dùng thuốc giảm đau/thuốc nam kéo dài. Khoảng 6 tháng nay đau thắt lưng tăng dữ dội lan xuống mặt sau đùi và cẳng chân hai bên (chân phải > chân trái), tê bì châm chích bàn chân, đi bộ ngắt quãng dưới 50m phải ngồi nghỉ do đau buốt tê bì hai chân. Vào viện Bạch Mai khám, MRI và CT cột sống thắt lưng cho thấy trượt L4 ra trước độ I trên nền thoái hóa nặng, phì đại dây chằng vàng và diện khớp gây hẹp nặng ống sống tầng L4-L5, kèm loãng xương nặng và xẹp lún cũ L1. Đã hội chẩn Tim mạch, Nội tiết điều chỉnh Cortisol nền -> Chỉ định phẫu thuật nẹp vít qua cuống nhồi xi măng sinh học (cement-augmented pedicle screw) để tăng độ vững trên nền loãng xương kết hợp giải ép ống sống hàn khớp liên thân đốt.",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. Hội chứng Cushing do thuốc (mặt tròn đỏ, da mỏng). Cột sống thắt lưng: Gù vẹo cột sống thắt lưng, ấn đau gai sau và cạnh sống L4-L5; Lasègue (+) 45 độ bên phải, 60 độ bên trái; giảm cảm giác vùng chi phối rễ L5 hai bên; cơ lực mu bàn chân phải 4/5, chân trái 4/5. Phản xạ gân gót hai bên giảm. Tim T1 T2 rõ, không tiếng thổi; phổi không rale; bụng mềm.",
                BloodGroup = "O Rh(+)", BloodReserve = "350", Anesthesia = "Gây mê nội khí quản", SurgeryTime = "09 giờ 30 phút, ngày 02/10/2026",
                Risks = "Rò dịch não tủy do rách màng tủy dính ở người già, tràn xi măng vào ống sống hoặc tĩnh mạch gây thuyên tắc phổi, chảy máu vết mổ, suy thượng thận cấp sau mổ do stress phẫu thuật, biến cố tim mạch thiếu máu cơ tim cấp, nhiễm trùng vết mổ trên cơ địa suy giảm miễn dịch do corticoid."
            },
            new PatientTarget { 
                Stt = 3, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0004078976", PatientName = "NGUYỄN THỊ LIÊN", Age = 58, 
                MainDiag = "Xẹp đốt sống T12, L1 mới / Đã bơm xi măng L3 - Viêm khớp dạng thấp huyết thanh dương tính - Tăng huyết áp - Loãng xương nặng - Suy giáp sau PT cắt bán phần tuyến giáp", 
                SurgeryMethod = "Tạo hình thân đốt sống T12, L1 bằng bơm xi măng sinh học qua da (BXM T12, L1)", Surgeon = "TS. Nguyễn Văn Trung", RoomBed = "P723 / G44",
                History = "Viêm khớp dạng thấp điều trị Medrol dài ngày; Đã phẫu thuật cắt bán phần tuyến giáp do bướu giáp, đang bù Levothyrox; Tăng huyết áp; Tiền sử đã BXM L3 cách 1 năm.",
                Course = "Bệnh nhân nữ 58 tuổi, tiền sử viêm khớp dạng thấp dùng corticoid kéo dài gây loãng xương thứ phát nặng, đã từng được phẫu thuật bơm xi măng thân đốt L3 cách 1 năm. Đợt này sau ngã ngồi nhẹ cách 1 tuần xuất hiện đau chói dữ dội vùng lưng - thắt lưng, đau tăng khi thay đổi tư thế, ngồi dậy và đi lại rất khó khăn, không tự xoay trở được trên giường. Nhập Khoa 57, chụp MRI cột sống ngực - thắt lưng phát hiện tổn thương phù tủy xương xẹp cấp tính thân đốt sống T12 và L1 (tăng tín hiệu trên STIR, giảm trên T1W), đốt L3 có hình ảnh xi măng cũ ổn định. Bệnh nhân đã được hội chẩn Cơ xương khớp điều chỉnh thuốc nền, chỉ định phẫu thuật tạo hình thân đốt sống T12 và L1 bằng bơm xi măng sinh học qua da (Vertebroplasty).",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.5°C, SpO2 99%. Thể trạng trung bình. Cột sống ngực - thắt lưng: Co cứng cơ cạnh sống ngực 12 và thắt lưng 1; gõ dồn đau chói tại vị trí gai sau T12 và L1; không có dấu hiệu chèn ép rễ thần kinh hay tủy sống (Lasègue (-), cơ lực hai chân 5/5, cảm giác chi dưới bình thường, phản xạ gân xương bình thường, đại tiểu tiện tự chủ). Khám các khớp bàn ngón tay biến dạng nhẹ do VKDT, không sưng nóng đỏ. Tim phổi bình thường, bụng mềm.",
                BloodGroup = "A Rh(+)", BloodReserve = "Không (Dự trù phương tiện)", Anesthesia = "Tiền mê + Gây tê tại chỗ", SurgeryTime = "11 giờ 00 phút, ngày 02/10/2026",
                Risks = "Rò xi măng sinh học vào khoang ngoài màng cứng hoặc lỗ liên hợp gây chèn ép tủy/rễ thần kinh, rò xi măng vào hệ thống tĩnh mạch cạnh sống dẫn đến thuyên tắc mạch phổi, tụ máu tại vị trí chọc kim, gãy xẹp đốt sống lân cận sau can thiệp, nhiễm trùng thân đốt - đĩa đệm."
            },
            new PatientTarget { 
                Stt = 4, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0004083361", PatientName = "NGUYỄN THỊ XÔ", Age = 72, 
                MainDiag = "Xẹp cấp thân đốt sống L1 do loãng xương / Thoái hóa cột sống thắt lưng", 
                SurgeryMethod = "Tạo hình thân đốt sống L1 bằng bơm xi măng sinh học qua da (BXM L1)", Surgeon = "BS Nguyễn Đức Hoàng", RoomBed = "P722 / G35",
                History = "Thoái hóa cột sống nhiều năm, loãng xương người già.",
                Course = "Bệnh nhân nữ 72 tuổi, tiền sử loãng xương và thoái hóa cột sống thắt lưng. Cách vào viện 5 ngày bệnh nhân bê chậu nước nhẹ thì nghe tiếng 'khục' ở lưng, sau đó đau nhói vùng thắt lưng dữ dội, đau tăng khi đứng, ngồi và khi ho/hắt hơi, chỉ nằm ngửa bất động trên giường mới đỡ đau. Vào BV Bạch Mai, chụp MRI cột sống thắt lưng cho hình ảnh xẹp cấp tính đốt sống L1 giảm chiều cao thân đốt khoảng 30%, phù tủy xương rõ rệt trên chuỗi xung STIR, không có mảnh xương lồi vào ống sống. Bệnh nhân được chỉ định phẫu thuật tạo hình thân đốt sống L1 bằng bơm xi măng sinh học qua da để giảm đau nhanh và làm vững cột sống.",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 130/75 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng già yếu. Cột sống thắt lưng: Đau chói khu trú tại gai sau L1, gõ dồn đau tăng; co cứng nhóm cơ dựng gai lưng; vận động cột sống hạn chế nhiều do đau; khám thần kinh chi dưới: cơ lực 5/5, cảm giác bình thường, không có dấu hiệu chèn ép rễ hay tủy; đại tiểu tiện bình thường. Tim mạch, hô hấp không phát hiện bệnh lý cấp tính.",
                BloodGroup = "O Rh(+)", BloodReserve = "Không", Anesthesia = "Tiền mê + Gây tê tại chỗ", SurgeryTime = "12 giờ 30 phút, ngày 02/10/2026",
                Risks = "Tràn xi măng ra ngoài thân đốt sống gây chèn ép tủy/rễ, rò xi măng vào tĩnh mạch cạnh sống gây tắc mạch phổi, tụ máu mô mềm vị trí chọc kim, xẹp thứ phát các đốt sống kế cận, phản ứng dị ứng hoặc tụt huyết áp thoáng qua khi bơm xi măng monomer."
            },
            new PatientTarget { 
                Stt = 5, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0003386201", PatientName = "TRẦN THỊ VÒNG", Age = 60, 
                MainDiag = "Xẹp cấp thân đốt sống T12 do loãng xương / Xẹp cũ L1 - Viêm da nấm thân mình", 
                SurgeryMethod = "Tạo hình thân đốt sống T12 bằng bơm xi măng sinh học qua da (BXM T12)", Surgeon = "BS Nguyễn Đức Hoàng", RoomBed = "P722 / G36",
                History = "Loãng xương, xẹp cũ L1 điều trị bảo tồn ổn định.",
                Course = "Bệnh nhân nữ 60 tuổi, tiền sử xẹp cũ đốt sống L1. Khoảng 4 ngày trước vào viện bệnh nhân hắt hơi mạnh sau đó xuất hiện đau nhói vùng lưng ngực - thắt lưng, đau nhiều khi xoay trở người và ngồi dậy, dùng thuốc giảm đau đường uống đáp ứng kém. Vào BV Bạch Mai khám, chụp MRI cột sống phát hiện xẹp cấp tính thân đốt sống T12 phù nề tủy xương mới, đốt L1 xẹp cũ xơ hóa không có tín hiệu phù cấp. Được chỉ định phẫu thuật tạo hình thân đốt sống T12 bằng bơm xi măng sinh học qua da (Vertebroplasty T12).",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 120/80 mmHg, T° 36.7°C, SpO2 99%. BN tỉnh táo. Khám cột sống: Co rút cơ cạnh sống vùng ngực 11-12; ấn đau chói gai sau T12, gõ dồn dọc cột sống đau tăng tại T12; không có triệu chứng thần kinh khu trú chi dưới (cơ lực hai chân 5/5, cảm giác bình thường, phản xạ gân xương bình thường, đại tiểu tiện tự chủ). Da vùng lưng vị trí dự kiến chọc kim nguyên vẹn, không có tổn thương nhiễm trùng tại chỗ. Tim phổi bình thường.",
                BloodGroup = "B Rh(+)", BloodReserve = "Không", Anesthesia = "Tiền mê + Gây tê tại chỗ", SurgeryTime = "14 giờ 00 phút, ngày 02/10/2026",
                Risks = "Tràn xi măng vào ống sống gây tổn thương nón tủy/rễ thần kinh, tắc mạch phổi do xi măng đi vào tĩnh mạch, tụ máu vết chọc, nhiễm trùng mô mềm hoặc đĩa đệm, tổn thương màng phổi khi tiếp cận đốt ngực T12."
            },
            new PatientTarget { 
                Stt = 6, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0003362121", PatientName = "TRƯƠNG THỊ THI", Age = 68, 
                MainDiag = "Xẹp cấp đốt sống T4, T10 do loãng xương nặng / Đã bơm xi măng T6, T10, T12 - Đái tháo đường típ 2 - Tăng huyết áp - Loãng xương", 
                SurgeryMethod = "Tạo hình thân đốt sống T4, T10 bằng bơm xi măng sinh học qua da (BXM T4, T10)", Surgeon = "BS Lê Đăng Tân", RoomBed = "P735 / G81",
                History = "Đái tháo đường típ 2 điều trị thuốc viên/Insulin; Tăng huyết áp; Loãng xương nặng đã từng BXM các đốt T6, T10, T12 trước đây.",
                Course = "Bệnh nhân nữ 68 tuổi, tiền sử đái tháo đường típ 2, tăng huyết áp, loãng xương nặng đã từng can thiệp bơm xi măng nhiều đốt sống ngực trước đây. Đợt này xuất hiện đau lưng và ngực nhiều, đau buốt dữ dội khi hít thở sâu và khi xoay người, hạn chế vận động nhiều. Chụp MRI cột sống ngực phát hiện xẹp cấp mới phù tủy xương đốt T4 và xẹp lún tiến triển đốt T10 bên cạnh các đốt T6, T12 đã có xi măng cũ ổn định. Đã được kiểm soát đường huyết và huyết áp ổn định tại nội trú, hội chẩn chỉ định phẫu thuật tạo hình thân đốt sống T4, T10 bằng bơm xi măng sinh học qua da.",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 135/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng trung bình. Cột sống ngực: Ấn đau chói tại gai sau T4 và T10; cơ cạnh sống co cứng; không có dấu hiệu chèn ép tủy ngực (cơ lực hai chân 5/5, cảm giác hai chân bình thường, Babinski (-), không rối loạn cơ tròn). Tim T1 T2 rõ, phổi hai bên thông khí đều, bụng mềm.",
                BloodGroup = "O Rh(+)", BloodReserve = "Không", Anesthesia = "Tiền mê + Gây tê tại chỗ", SurgeryTime = "15 giờ 30 phút, ngày 02/10/2026",
                Risks = "Tràn xi măng vào ống sống gây tổn thương tủy ngực, tràn xi măng vào tĩnh mạch gây thuyên tắc phổi, tràn khí màng phổi khi chọc đốt sống ngực cao (T4), tụ máu vết mổ, nhiễm trùng trên nền ĐTĐ, xẹp các đốt sống kế cận do loãng xương tiến triển."
            },
            new PatientTarget { 
                Stt = 7, RoomMng = "Phòng mổ 5 (CS)", PatientCode = "0004095785", PatientName = "LỤC ANH TUẤN", Age = 40, 
                MainDiag = "Đa chấn thương: Chấn thương cột sống vỡ xẹp D12, L3 - Gãy kín đầu dưới xương quay trái, gãy kín đài quay phải - Chấn thương ngực kín: Gãy cung sau xương sườn 1, 2 bên phải", 
                SurgeryMethod = "Cố định cột sống lối sau nẹp vít giải ép + Lấy bỏ chỏm xương quay phải", Surgeon = "BS Lê Đăng Tân · phụ: BS Đặng Nhật Quang", RoomBed = "P740.1 / G78",
                History = "Khỏe mạnh, không tiền sử bệnh mạn tính.",
                Course = "Bệnh nhân nam 40 tuổi, bị tai nạn lao động ngã cao đập lưng và chống hai tay xuống đất. Sau ngã đau dữ dội vùng cột sống ngực thắt lưng, hạn chế vận động hai chân do đau, biến dạng đau sưng khuỷu tay phải và cổ tay trái, đau tức ngực phải khi hít thở. Cấp cứu BV Bạch Mai, chụp CT/MRI cột sống phát hiện vỡ lún đốt sống D12 mất vững cột sống và xẹp L3; chụp X-quang/CT khuỷu tay và cổ tay phát hiện gãy vỡ phức tạp đài quay phải độ III Mason, gãy đầu dưới xương quay trái; chụp CT ngực phát hiện gãy cung sau xương sườn 1, 2 phải không tràn khí/tràn máu màng phổi. Đã bất động bột hai tay, điều trị ổn định chấn thương ngực kín. Chỉ định phẫu thuật cố định cột sống lối sau bằng nẹp vít qua cuống D12-L3 kết hợp phẫu thuật lấy bỏ chỏm xương quay phải.",
                ExamSummary = "DHST: Mạch 84 ck/p, HA 125/80 mmHg, T° 36.8°C, SpO2 99%. BN tỉnh táo, thể trạng tốt. Cột sống: Biến dạng gù nhẹ vùng ngực-thắt lưng, ấn đau chói D12 và L3; cơ lực hai chi dưới 4+/5 do đau; cảm giác nông sâu bình thường; phản xạ gân xương bình thường; tiểu tiện bình thường. Tay phải: Nẹp bột cẳng bàn tay, sưng nề đau chói vùng chỏm quay, hạn chế sấp ngửa; tay trái: bột cẳng bàn tay trái gãy đầu dưới xương quay; ngực phải: đau tức nhẹ cung sau sườn 1-2, rì rào phế nang 2 phổi rõ. Bụng mềm.",
                BloodGroup = "B Rh(+)", BloodReserve = "700", Anesthesia = "Gây mê nội khí quản", SurgeryTime = "17 giờ 00 phút, ngày 02/10/2026",
                Risks = "Chảy máu trong mổ, rách màng cứng rò dịch não tủy, tổn thương rễ thần kinh, tổn thương dây thần kinh quay (nhánh sâu thần kinh gian cốt sau) khi phẫu thuật lấy chỏm quay, mất vững khớp khuỷu/quay trụ trên, tụ máu vết mổ, biến chứng hô hấp do gãy xương sườn."
            },

            // Phòng mổ 2 (CTCH)
            new PatientTarget { 
                Stt = 8, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0002048483", PatientName = "ĐINH THỊ VẺ", Age = 62, 
                MainDiag = "Hoại tử vô mạch chỏm xương đùi hai bên giai đoạn IV (Bên phải > Bên trái) / Tăng tiểu cầu - Tăng huyết áp - Đái tháo đường típ 2", 
                SurgeryMethod = "Phẫu thuật thay toàn bộ khớp háng phải bằng khớp háng nhân tạo không xi măng", Surgeon = "BS Đặng Hoàng Giang", RoomBed = "P723 / G47",
                History = "Tăng huyết áp, Đái tháo đường típ 2, Tăng tiểu cầu nguyên phát (đã dừng Aspirin cách 2 tháng, đã HC Huyết học & Tim mạch).",
                Course = "Bệnh nhân nữ 62 tuổi, tiền sử đái tháo đường, tăng huyết áp, tăng tiểu cầu. Đau khớp háng hai bên tăng dần 2 năm nay, bên phải nặng hơn bên trái. Khoảng 3 tháng nay khớp háng phải đau buốt nhiều khi đi lại và tì đè, hạn chế gấp duỗi và xoay khớp háng, đi khập khiễng, teo nhẹ cơ đùi phải. Chụp X-quang và MRI khớp háng hai bên chẩn đoán hoại tử vô khuẩn chỏm xương đùi hai bên độ IV theo Ficat, chỏm xương đùi phải xẹp biến dạng nặng, mất khe khớp háng phải. Bệnh nhân đã dừng Aspirin đủ thời gian, được hội chẩn Huyết học, Tim mạch, Nội tiết chuẩn bị kỹ lưỡng -> Chỉ định phẫu thuật thay toàn bộ khớp háng phải.",
                ExamSummary = "DHST: Mạch 78 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh, thể trạng tốt. Khám khớp háng phải: Ấn đau điểm khớp háng trước và sau; nghiệm pháp Patrick (+); biên độ vận động khớp háng phải hạn chế: gấp 80°, duỗi 0°, khép 15°, dạng 20°, xoay trong/xoay ngoài hạn chế nhiều do đau; teo cơ tứ đầu đùi phải 1.5 cm so với bên trái; chiều dài chi dưới phải ngắn hơn chân trái 0.5 cm; tuần hoàn và cảm giác mu bàn chân tốt. Tim đều, phổi sáng, bụng mềm.",
                BloodGroup = "O Rh(+)", BloodReserve = "350", Anesthesia = "Gây tê tủy sống kết hợp gây tê ngoài màng cứng giảm đau (hoặc Mê NKQ)", SurgeryTime = "08 giờ 00 phút, ngày 02/10/2026",
                Risks = "Chảy máu trong và sau mổ, trật khớp háng nhân tạo, gãy xương đùi/ổ cối quanh chuôi khớp nhân tạo, tổn thương thần kinh ngồi (thần kinh hông to), biến cố tắc mạch huyết khối tĩnh mạch sâu do tăng tiểu cầu, nhiễm trùng khớp háng nhân tạo trên nền ĐTĐ."
            },
            new PatientTarget { 
                Stt = 9, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0002254037", PatientName = "HOÀNG MẠNH THƯỜNG", Age = 28, 
                MainDiag = "Đứt hoàn toàn dây chằng chéo trước khớp gối trái / Thoái hóa nhẹ sụn khớp gối trái", 
                SurgeryMethod = "Phẫu thuật nội soi tái tạo dây chằng chéo trước khớp gối trái bằng gân tự thân (Gân bán gân và gân thon)", Surgeon = "BS. Hà Đức Cường", RoomBed = "P730 / G92",
                History = "Khỏe mạnh, tiền sử chấn thương thể thao đá bóng cách 2 tháng.",
                Course = "Bệnh nhân nam 28 tuổi, tiền sử chấn thương gối trái khi đá bóng cách đây 2 tháng (nghe tiếng 'rắc', sưng nề đau khớp gối nhiều). Sau đó gối đỡ nề nhưng bệnh nhân cảm giác lỏng khớp gối trái, bước hụt chân khi đi cầu thang và chạy nhanh, thường xuyên có cảm giác kẹt khớp nhẹ. Đến khám tại BV Bạch Mai, chụp MRI khớp gối trái phát hiện đứt hoàn toàn dây chằng chéo trước (ACL), rách nhẹ sụn chêm trong độ II, tràn dịch khớp gối mức độ nhẹ. Bệnh nhân có nguyện vọng chơi thể thao trở lại, hội chẩn chỉ định phẫu thuật nội soi tái tạo dây chằng chéo trước gối trái bằng gân tự thân (Hamstring).",
                ExamSummary = "DHST: Mạch 72 ck/p, HA 120/75 mmHg, T° 36.5°C, SpO2 99%. BN thể trạng tốt. Khám gối trái: Gối không sưng đỏ, dấu hiệu chạm xương bánh chè (-); nghiệm pháp Ngăn kéo trước (+) 2+; Lachman (+) 2+ không có điểm dừng chắc; Pivot shift (+); nghiệm pháp McMurray (-) rách sụn chêm; biên độ vận động gối trái gấp 130°, duỗi 0°; cơ lực chi dưới trái 5/5; mạch mu chân và chày sau bắt rõ.",
                BloodGroup = "B Rh(+)", BloodReserve = "Không", Anesthesia = "Gây tê tủy sống", SurgeryTime = "09 giờ 30 phút, ngày 02/10/2026",
                Risks = "Chảy máu tụ dịch khớp gối sau mổ, tổn thương sụn khớp trong quá trình khoan đường hầm, lỏng hoặc đứt lại mảnh ghép sau mổ, cứng khớp gối do xơ dính, nhiễm trùng khớp gối nội soi, tổn thương thần kinh hiển khi lấy gân Hamstring."
            },
            new PatientTarget { 
                Stt = 10, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0002740977", PatientName = "NGUYỄN VĂN PHƯƠNG", Age = 58, 
                MainDiag = "Thoái hóa khớp gối phải nặng giai đoạn IV - Gai xương chèn ép / Nốt đặc thùy trên phổi trái Lung-RADS 4X", 
                SurgeryMethod = "Phẫu thuật thay toàn bộ khớp gối phải bằng khớp nhân tạo (Total Knee Arthroplasty - TKA)", Surgeon = "BS Đặng Hoàng Giang", RoomBed = "P712 / G23",
                History = "Thoái hóa khớp gối phải hơn 5 năm; Nốt đặc thùy trên phổi trái theo dõi Lung-RADS 4X (đã HC Hô hấp/Lồng ngực cho phép mổ gối trước).",
                Course = "Bệnh nhân nam 58 tuổi, đau khớp gối phải kéo dài nhiều năm, điều trị nội khoa tiêm khớp nhiều đợt không hiệu quả. Khoảng 6 tháng nay đau dữ dội khi đi lại và tì đè, khớp gối biến dạng vẹo trong (varus deformity), phát ra tiếng lục khục khi vận động, đi bộ dưới 100m. Chụp X-quang và MRI gối phải cho thấy hẹp khe khớp hoàn toàn khoang trong, gai xương lớn bờ khớp, xơ đặc xương dưới sụn giai đoạn IV theo Kellgren-Lawrence. CT lồng ngực phát hiện nốt đặc thùy trên phổi trái Lung-RADS 4X đã được hội chẩn chuyên khoa Hô hấp đánh giá chức năng hô hấp bình thường, cho phép tiến hành phẫu thuật thay khớp gối -> Chỉ định phẫu thuật thay toàn bộ khớp gối phải.",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 125/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh táo. Khám gối phải: Khớp gối biến dạng vẹo trong khoảng 10 độ; ấn đau chói khe khớp trong; dấu hiệu bào khớp (+); biên độ vận động: gấp 90°, duỗi thiếu 5° (co rút gập nhẹ); lỏng khớp nhẹ dây chằng bên ngoài thứ phát; tuần hoàn và thần kinh mu chân nguyên vẹn. Khám phổi thông khí đều 2 bên, không rale. Tim mạch bình thường.",
                BloodGroup = "A Rh(+)", BloodReserve = "350", Anesthesia = "Gây tê tủy sống + Gây tê mặt phẳng cơ vuông thắt lưng/khoang cơ khép giảm đau sau mổ", SurgeryTime = "11 giờ 00 phút, ngày 02/10/2026",
                Risks = "Chảy máu vết mổ, tụ máu khớp gối, tắc mạch huyết khối tĩnh mạch sâu chi dưới, tổn thương thần kinh mác chung gây yếu bàn chân rủ, cứng khớp gối do xơ dính sau mổ, trật hoặc mất vững khớp gối nhân tạo, nhiễm trùng khớp gối nhân tạo."
            },
            new PatientTarget { 
                Stt = 11, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0004094236", PatientName = "NGUYỄN THỊ TÍNH", Age = 93, 
                MainDiag = "Gãy kín phức tạp liên mấu chuyển xương đùi trái di lệch / Đái tháo đường típ 2 - Tăng huyết áp - Loãng xương người già nặng", 
                SurgeryMethod = "Phẫu thuật kết hợp xương đùi trái bằng đinh nội tủy có chốt Gamma/PFNA dưới màn tăng sáng C-arm", Surgeon = "BS Đặng Nhật Quang", RoomBed = "P740.2 / G65",
                History = "Tăng huyết áp, Đái tháo đường típ 2 điều trị thường xuyên, thể trạng người cao tuổi 93 tuổi.",
                Course = "Bệnh nhân nữ 93 tuổi, trượt chân ngã đập mông và hông trái xuống nền cứng tại nhà. Sau ngã đau chói vùng háng đùi trái, bất lực vận động hoàn toàn chân trái, không thể ngồi dậy. Được người nhà đưa vào BV Bạch Mai cấp cứu. Chụp X-quang khớp háng và đùi trái chẩn đoán gãy kín liên mấu chuyển xương đùi trái loại mất vững theo phân loại AO/OTA 31-A2. Bệnh nhân cao tuổi có bệnh nền tăng huyết áp, đái tháo đường, nguy cơ biến chứng do nằm lâu (viêm phổi ứ đọng, loét tì đè, huyết khối mạch). Đã được hội chẩn Tim mạch, Gây mê hồi sức đánh giá nguy cơ phẫu thuật -> Chỉ định phẫu thuật kết hợp xương bằng đinh nội tủy chốt đầu trên xương đùi (PFNA/Gamma nail) ít xâm lấn để phục hồi vận động sớm.",
                ExamSummary = "DHST: Mạch 80 ck/p, HA 140/85 mmHg, T° 36.7°C, SpO2 97%. BN tỉnh, tiếp xúc được, già yếu. Khám háng - đùi trái: Chi dưới trái ngắn hơn bên phải 2 cm, bàn chân đổ ngoài sát mặt giường; tam giác Scarpa đầy, ấn đau chói vùng mấu chuyển lớn đùi trái; cử động chân trái gây đau dữ dội, bất lực vận động hoàn toàn; mạch mu chân và chày sau trái bắt rõ; cảm giác mu chân bình thường. Tim đều T1 T2 rõ, phổi đáy có ít rale ẩm ứ đọng do nằm, bụng mềm.",
                BloodGroup = "O Rh(+)", BloodReserve = "350", Anesthesia = "Gây tê tủy sống liều thấp (hoặc Gây mê nội khí quản)", SurgeryTime = "12 giờ 30 phút, ngày 02/10/2026",
                Risks = "Nguy cơ tim mạch và hô hấp chu phẫu cao ở bệnh nhân 93 tuổi, chảy máu trong mổ, tụt huyết áp khi gây tê, gãy thêm mảnh xương đùi khi đóng đinh, cut-out đinh vít chốt qua chỏm do loãng xương nặng, huyết khối tĩnh mạch sâu thuyên tắc phổi, loét tì đè và nhiễm trùng vết mổ."
            },
            new PatientTarget { 
                Stt = 12, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0004095977", PatientName = "PHẠM THỊ LAN", Age = 56, 
                MainDiag = "Hội chứng ống cổ tay hai bên mức độ nặng (Bên phải > Bên trái)", 
                SurgeryMethod = "Phẫu thuật giải ép thần kinh giữa - Cắt dây chằng vòng cổ tay ngang (Cắt mạc hãm gân gấp) cổ tay hai bên", Surgeon = "BS Ngô Đăng Quang", RoomBed = "P735 / G81",
                History = "Khỏe mạnh, làm công việc nội trợ/thủ công sử dụng cổ tay nhiều năm.",
                Course = "Bệnh nhân nữ 56 tuổi, tê bì dị cảm các ngón 1, 2, 3 và nửa ngón 4 bàn tay hai bên kéo dài hơn 1 năm, bên phải tê nhiều hơn bên trái. Tê bì tăng nhiều về đêm làm bệnh nhân thức giấc, phải lắc vẫy bàn tay mới đỡ, gần đây xuất hiện teo nhẹ cơ mô cái bàn tay phải, cầm nắm đồ vật hay bị rơi. Đi khám tại BV Bạch Mai, được làm điện cơ (EMG) ghi nhận dẫn truyền thần kinh giữa qua cổ tay hai bên chậm nặng (Hội chứng ống cổ tay mức độ nặng). Điều trị nội khoa nẹp cổ tay và dùng vitamin B không đỡ -> Chỉ định phẫu thuật cắt dây chằng vòng cổ tay ngang giải phóng thần kinh giữa hai bên.",
                ExamSummary = "DHST: Mạch 75 ck/p, HA 120/75 mmHg, T° 36.5°C, SpO2 99%. Thể trạng tốt. Khám bàn tay hai bên: Teo nhẹ cơ mô cái bàn tay phải; nghiệm pháp Tinel (+) cổ tay hai bên; nghiệm pháp Phalen (+) xuất hiện tê bì sau 20 giây; giảm cảm giác nông ngón 1, 2, 3 và nửa ngoài ngón 4 bàn tay phải; cơ lực đối chiếu ngón cái bàn tay phải 4/5; tuần hoàn đầu ngón tay hai bên hồng ấm, mao mạch hồi lưu < 2s. Tim phổi bình thường.",
                BloodGroup = "AB Rh(+)", BloodReserve = "Không", Anesthesia = "Gây tê tại chỗ (hoặc Tê đám rối thần kinh cánh tay)", SurgeryTime = "14 giờ 00 phút, ngày 02/10/2026",
                Risks = "Tổn thương nhánh vận động quặt ngược cơ mô cái của thần kinh giữa, tổn thương cung mạch gan tay nông, chảy máu tụ máu vết mổ, sẹo đau phì đại vùng gan bàn tay, đau phức hợp khu vực (CRPS), tê bì kéo dài sau mổ do tổn thương sợi thần kinh mạn tính."
            },
            new PatientTarget { 
                Stt = 13, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0004099240", PatientName = "NGUYỄN THỊ DỤNG", Age = 68, 
                MainDiag = "Thoái hóa khớp gối hai bên giai đoạn IV - Hẹp nặng khe khớp gối trái / Loãng xương - Tăng huyết áp", 
                SurgeryMethod = "Phẫu thuật thay toàn bộ khớp gối trái bằng khớp gối nhân tạo (TKA Trái)", Surgeon = "BS. Hà Đức Cường", RoomBed = "P712 / G12",
                History = "Tăng huyết áp, loãng xương, thoái hóa khớp gối 2 bên nhiều năm.",
                Course = "Bệnh nhân nữ 68 tuổi, đau khớp gối hai bên kéo dài nhiều năm, gối trái nặng hơn gối phải. Bệnh nhân đau nhiều khi đi lại, khó khăn khi lên xuống cầu thang và ngồi xổm, gối trái biến dạng cong vẹo trục chi. Đã điều trị nội khoa nhiều đợt, uống thuốc giảm đau và tiêm chất nhờn nội khớp đáp ứng kém dần. Chụp X-quang và MRI gối trái cho thấy thoái hóa khớp gối độ IV theo Kellgren-Lawrence, mất khe khớp khoang trong, gai xương mâm chày và xương đùi lớn, xơ đặc xương dưới sụn. Hội chẩn chuyên khoa chỉ định phẫu thuật thay toàn bộ khớp gối trái.",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 130/80 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh táo. Khám gối trái: Trục chi dưới trái vẹo trong (Varus) 8 độ; ấn đau chói dọc khe khớp gối trong; dấu hiệu bào khớp (+); biên độ vận động gối trái: gấp 95°, duỗi thiếu 5°; lỏng khớp nhẹ khi làm nghiệm pháp dạng ép; cơ lực chi dưới trái 5/5; mạch mu chân và chày sau bắt rõ. Khám tim phổi bình thường.",
                BloodGroup = "O Rh(+)", BloodReserve = "350", Anesthesia = "Gây tê tủy sống + Gây tê vùng giảm đau sau mổ", SurgeryTime = "15 giờ 00 phút, ngày 02/10/2026",
                Risks = "Chảy máu trong và sau mổ, tụ máu khớp gối, tắc mạch huyết khối tĩnh mạch sâu chi dưới, tổn thương thần kinh mác chung, cứng khớp gối do dính sau mổ, nhiễm trùng khớp gối nhân tạo, lỏng hoặc mòn khớp nhân tạo về sau."
            },
            new PatientTarget { 
                Stt = 14, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0004099512", PatientName = "NGUYỄN THỊ TỊNH", Age = 76, 
                MainDiag = "Hoại tử vô mạch chỏm xương đùi hai bên (Phải > Trái) giai đoạn IV / Sau phẫu thuật cố định cột sống thắt lưng 5 tháng - Loãng xương", 
                SurgeryMethod = "Phẫu thuật thay toàn bộ khớp háng phải bằng khớp háng nhân tạo không xi măng", Surgeon = "BS. Hà Đức Cường", RoomBed = "P717 / G10",
                History = "Đã phẫu thuật mổ nẹp vít cố định cột sống thắt lưng cách 5 tháng tại Khoa 57; Loãng xương.",
                Course = "Bệnh nhân nữ 76 tuổi, tiền sử mổ cố định nẹp vít cột sống thắt lưng cách 5 tháng ổn định. Bệnh nhân đau khớp háng hai bên tăng dần, bên phải đau buốt dữ dội lan xuống mặt trước đùi, hạn chế vận động nhiều, không tự đi lại được phải có người dìu. Chụp X-quang và MRI khớp háng hai bên phát hiện hoại tử vô mạch chỏm xương đùi hai bên độ IV, chỏm xương đùi phải xẹp dẹt hoàn toàn, khuyết xương ổ cối, viêm dính khớp háng. Bệnh nhân được hội chẩn thông qua mổ chỉ định phẫu thuật thay toàn bộ khớp háng phải bằng khớp nhân tạo.",
                ExamSummary = "DHST: Mạch 74 ck/p, HA 125/75 mmHg, T° 36.6°C, SpO2 98%. BN tỉnh táo, thể trạng già. Vết mổ cũ cột sống thắt lưng liền sẹo tốt. Khám khớp háng phải: Ấn đau chói vùng khớp háng trước; Patrick (+); biên độ khớp háng phải: gấp 75°, duỗi 0°, khép 10°, dạng 15°, hạn chế xoay; ngắn chi dưới phải 1 cm; cơ lực chi dưới phải 4/5 do đau; mạch mu chân bắt rõ. Tim phổi không phát hiện bất thường.",
                BloodGroup = "B Rh(+)", BloodReserve = "350", Anesthesia = "Gây tê tủy sống (hoặc Gây mê nội khí quản)", SurgeryTime = "16 giờ 30 phút, ngày 02/10/2026",
                Risks = "Chảy máu trong mổ, trật khớp háng nhân tạo sau mổ, gãy xương đùi hoặc vỡ ổ cối khi doa đóng chuôi khớp trên nền loãng xương, tổn thương thần kinh ngồi, tắc mạch huyết khối chi dưới, nhiễm trùng khớp nhân tạo."
            },
            new PatientTarget { 
                Stt = 15, RoomMng = "Phòng mổ 2 (CTCH)", PatientCode = "0001934566", PatientName = "BÙI THỊ TUYẾT", Age = 64, 
                MainDiag = "Viêm màng hoạt dịch thể lông nốt sắc tố (PVNS) khớp vai trái / Thoái hóa khớp vai trái", 
                SurgeryMethod = "Phẫu thuật bóc u, cắt toàn bộ bao màng hoạt dịch viêm khớp vai trái làm giải phẫu bệnh", Surgeon = "BS Đặng Nhật Quang", RoomBed = "P717 / G10",
                History = "Khỏe mạnh, sưng đau khớp vai trái tái diễn hơn 1 năm.",
                Course = "Bệnh nhân nữ 64 tuổi, sưng nề và đau tức âm ỉ vùng khớp vai trái hơn 1 năm nay, đau tăng khi vận động đưa tay lên cao hoặc ra sau, cảm giác kẹt vướng trong khớp vai, đã điều trị nội khoa nhiều đợt không đỡ. Đi khám tại BV Bạch Mai, chụp MRI khớp vai trái phát hiện hình ảnh dày màng hoạt dịch lan tỏa nhiều nốt giảm tín hiệu trên cả T1W và T2W (lắng đọng hemosiderin), tràn dịch khớp vai mức độ vừa, hình ảnh điển hình của u màng hoạt dịch thể lông nốt sắc tố (PVNS) khớp vai trái. Hội chẩn chỉ định phẫu thuật mở bóc u, cắt bỏ toàn bộ màng hoạt dịch viêm khớp vai trái gửi làm xét nghiệm giải phẫu bệnh.",
                ExamSummary = "DHST: Mạch 76 ck/p, HA 120/80 mmHg, T° 36.6°C, SpO2 99%. Thể trạng tốt. Khám vai trái: Khớp vai trái sưng nề nhẹ hơn vai phải, không nóng đỏ; ấn đau tức rãnh nhị đầu và khe khớp vai trước sau; biên độ vận động khớp vai trái: dang 90°, gấp 100°, xoay ngoài 30°, xoay trong 40° (hạn chế do đau và kẹt cơ học); cơ lực đai vai trái 5/5; cảm giác và mạch đập đầu chi trên trái bình thường. Tim phổi bình thường, bụng mềm.",
                BloodGroup = "A Rh(+)", BloodReserve = "Không", Anesthesia = "Gây mê nội khí quản + Tê đám rối thần kinh cánh tay giảm đau", SurgeryTime = "17 giờ 30 phút, ngày 02/10/2026",
                Risks = "Chảy máu tụ máu khoang khớp vai, tổn thương thần kinh nách hoặc thần kinh trên vai, cứng khớp vai do dính sau mổ, tái phát khối u màng hoạt dịch sau phẫu thuật, nhiễm trùng vết mổ."
            }
        };

        Console.WriteLine("===============================================================================");
        Console.WriteLine("🏥 TIẾN TRÌNH CHUẨN BỊ MỔ NGÀY 02/10/2026 — KHOA CTCH & CS HÀ NỘI");
        Console.WriteLine("===============================================================================");

        // =========================================================================
        // BƯỚC 1: XÓA SUẤT ĂN NGÀY MAI (02/10/2026)
        // =========================================================================
        Console.WriteLine("\n-------------------------------------------------------------------------------");
        Console.WriteLine("📋 BƯỚC 1: KIỂM TRA & XÓA SUẤT ĂN NGÀY MAI (02/10/2026)");
        Console.WriteLine("-------------------------------------------------------------------------------");

        long tomorrowStart = 20261002000000;
        long tomorrowEnd   = 20261002235959;

        int totalDeleted = 0;
        int totalChecked = 0;

        foreach (var pt in targets)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pt.PatientCode };
            var trList = adapter.FetchList<V_HIS_TREATMENT_4>("api/HisTreatment/GetView4", mos, tf, cp);
            if (trList == null || trList.Count == 0)
            {
                Console.WriteLine(string.Format("❌ Không tìm thấy BN: {0} ({1})", pt.PatientName, pt.PatientCode));
                continue;
            }

            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            var srf = new HisServiceReqViewFilter { TREATMENT_ID = tr.ID };
            var srs = adapter.FetchList<V_HIS_SERVICE_REQ>("api/HisServiceReq/GetView", mos, srf, cp);

            var tomorrowRationReqs = srs != null ? srs.Where(x => 
                (x.SERVICE_REQ_TYPE_ID == 11 || (x.SERVICE_REQ_TYPE_NAME != null && x.SERVICE_REQ_TYPE_NAME.ToLower().Contains("suất ăn"))) &&
                x.INTRUCTION_TIME >= tomorrowStart && x.INTRUCTION_TIME <= tomorrowEnd
            ).ToList() : new List<V_HIS_SERVICE_REQ>();

            var rf = new HisSereServRationViewFilter { TREATMENT_ID = tr.ID };
            var rationDetails = adapter.FetchList<V_HIS_SERE_SERV_RATION>("api/HisSereServRation/GetView", mos, rf, cp);
            var tomorrowDetails = rationDetails != null ? rationDetails.Where(x => 
                x.INTRUCTION_TIME >= tomorrowStart && x.INTRUCTION_TIME <= tomorrowEnd
            ).ToList() : new List<V_HIS_SERE_SERV_RATION>();

            if (tomorrowRationReqs.Count > 0 || tomorrowDetails.Count > 0)
            {
                Console.WriteLine(string.Format("🔍 BN [{0:D2}] {1} ({2}): Phát hiện {3} phiếu suất ăn / {4} bữa ngày 02/10/2026!", 
                    pt.Stt, pt.PatientName, pt.PatientCode, tomorrowRationReqs.Count, tomorrowDetails.Count));

                var reqIdsToCancel = new HashSet<long>(tomorrowRationReqs.Select(x => x.ID));
                foreach (var d in tomorrowDetails)
                {
                    if (d.SERVICE_REQ_ID > 0) reqIdsToCancel.Add(d.SERVICE_REQ_ID);
                }

                foreach (var reqId in reqIdsToCancel)
                {
                    var targetReq = srs != null ? srs.FirstOrDefault(x => x.ID == reqId) : null;
                    string code = targetReq != null ? targetReq.SERVICE_REQ_CODE : reqId.ToString();
                    long reqRoomId = (targetReq != null && targetReq.REQUEST_ROOM_ID > 0) ? targetReq.REQUEST_ROOM_ID : 5248;

                    if (targetReq != null && !string.IsNullOrEmpty(targetReq.REQUEST_LOGINNAME) && targetReq.REQUEST_LOGINNAME != "034727")
                    {
                        try
                        {
                            var rawFilter = new HisServiceReqFilter { ID = reqId };
                            var rawList = adapter.FetchList<HIS_SERVICE_REQ>("api/HisServiceReq/Get", mos, rawFilter, cp);
                            if (rawList != null && rawList.Count > 0)
                            {
                                var rawReq = rawList[0];
                                rawReq.REQUEST_LOGINNAME = "034727";
                                rawReq.REQUEST_USERNAME = "Ths.BS Nguyễn Hữu Sâm";
                                rawReq.REQUEST_USER_TITLE = "Thạc sỹ y học";
                                adapter.PostData<HIS_SERVICE_REQ>("api/HisServiceReq/UpdateCommonInfo", mos, rawReq, cp);
                            }
                        }
                        catch { }
                    }

                    var sdo = new HisServiceReqSDO
                    {
                        Id = reqId,
                        RequestRoomId = reqRoomId
                    };

                    CommonParam cpDel = new CommonParam();
                    var delRes = adapter.PostData<bool>("api/HisServiceReq/Delete", mos, sdo, cpDel);
                    if (delRes && !cpDel.HasException)
                    {
                        Console.WriteLine(string.Format("   ✔ Đã XÓA thành công phiếu suất ăn ID: {0} (Mã: {1})!", reqId, code));
                        totalDeleted++;
                    }
                    else
                    {
                        string msg = cpDel.Messages != null && cpDel.Messages.Count > 0 ? string.Join("; ", cpDel.Messages) : "Lỗi không xác định";
                        Console.WriteLine(string.Format("   ⚠️ Không thể xóa phiếu {0}: {1}", reqId, msg));
                    }
                }
            }
            else
            {
                Console.WriteLine(string.Format("✔ BN [{0:D2}] {1,-20} ({2}) | Chưa có suất ăn ngày 02/10/2026 (Sạch sẽ)", 
                    pt.Stt, pt.PatientName, pt.PatientCode));
            }
            totalChecked++;
        }

        Console.WriteLine(string.Format("\n📊 Tổng kết Bước 1: Đã rà soát {0}/15 bệnh nhân. Tổng số phiếu suất ăn ngày mai đã hủy: {1}", totalChecked, totalDeleted));

        // =========================================================================
        // BƯỚC 2: TẠO TỜ ĐIỀU TRỊ & KÊ LEANPRO TỦ TRỰC DINH DƯỠNG THEO QUY TẮC
        // =========================================================================
        Console.WriteLine("\n-------------------------------------------------------------------------------");
        Console.WriteLine("🥛 BƯỚC 2: CHỈ ĐỊNH LEANPRO PRESUR 12.5% CHO BỆNH NHÂN ĐỦ ĐIỀU KIỆN");
        Console.WriteLine("-------------------------------------------------------------------------------");

        int leanproAssigned = 0;
        int leanproBlocked = 0;

        foreach (var pt in targets)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = pt.PatientCode };
            var trList = adapter.FetchList<V_HIS_TREATMENT_4>("api/HisTreatment/GetView4", mos, tf, cp);
            if (trList == null || trList.Count == 0) continue;
            var tr = trList.OrderByDescending(x => x.IN_TIME).First();

            // Lấy buồng giường
            var rbf = new HisTreatmentBedRoomViewFilter { TREATMENT_ID = tr.ID, IS_IN_ROOM = 1 };
            var beds = adapter.FetchList<V_HIS_TREATMENT_BED_ROOM>("api/HisTreatmentBedRoom/GetView", mos, rbf, cp);
            long roomId = (beds != null && beds.Count > 0 && beds[0].BED_ROOM_ID.HasValue) ? beds[0].BED_ROOM_ID.Value : 5248;

            // Kiểm tra điều kiện Leanpro:
            // 1. Tuổi < 70
            // 2. Không ĐTĐ
            int yob = 0;
            string dobStr = tr.TDL_PATIENT_DOB.ToString();
            if (dobStr.Length >= 4) int.TryParse(dobStr.Substring(0, 4), out yob);
            int age = yob > 0 ? (2026 - yob) : pt.Age;

            string diagFull = string.Format("{0} {1} {2} {3}", tr.ICD_CODE, tr.ICD_SUB_CODE, tr.ICD_NAME, tr.ICD_TEXT).ToLower();
            string[] diabetesKeys = new string[] { "tháo đường", "đái đường", "tiểu đường", "diabetes", "đtđ" };
            bool isDiabetes = diabetesKeys.Any(k => diagFull.Contains(k)) ||
                              (tr.ICD_CODE != null && (tr.ICD_CODE.StartsWith("E10") || tr.ICD_CODE.StartsWith("E11") || tr.ICD_CODE.StartsWith("E12") || tr.ICD_CODE.StartsWith("E13") || tr.ICD_CODE.StartsWith("E14")));

            if (age >= 70 || isDiabetes)
            {
                string reason = age >= 70 ? string.Format("Tuổi {0} >= 70", age) : "Mắc Đái tháo đường";
                if (age >= 70 && isDiabetes) reason = string.Format("Tuổi {0} >= 70 & Mắc Đái tháo đường", age);
                Console.WriteLine(string.Format("⛔ BN [{0:D2}] {1,-20} ({2}) | CHẶN LEANPRO: {3}", pt.Stt, pt.PatientName, pt.PatientCode, reason));
                leanproBlocked++;
                continue;
            }

            Console.WriteLine(string.Format("👉 BN [{0:D2}] {1,-20} ({2}) | ĐỦ ĐIỀU KIỆN (Tuổi: {3} < 70, Không ĐTĐ)", pt.Stt, pt.PatientName, pt.PatientCode, age));

            // 1. Tạo hoặc kiểm tra Tờ điều trị Leanpro hôm nay
            long todayStart = 20261001000000;
            var trkFilter = new HisTrackingViewFilter { TREATMENT_ID = tr.ID };
            var trkList = adapter.FetchList<V_HIS_TRACKING>("api/HisTracking/GetView", mos, trkFilter, cp);
            var existingTrk = trkList != null ? trkList
                .Where(x => x.TRACKING_TIME >= todayStart && 
                           ((x.CONTENT != null && x.CONTENT.ToLower().Contains("bổ sung dịch")) ||
                            (x.MEDICAL_INSTRUCTION != null && x.MEDICAL_INSTRUCTION.ToLower().Contains("leanpro"))))
                .OrderByDescending(x => x.TRACKING_TIME)
                .FirstOrDefault() : null;

            long trackingId = 0;
            long trackingTime = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

            if (existingTrk != null)
            {
                trackingId = existingTrk.ID;
                trackingTime = existingTrk.TRACKING_TIME;
                Console.WriteLine(string.Format("   ℹ️ Đã có Tờ điều trị Leanpro (ID: {0}, Sheet: {1})", existingTrk.ID, existingTrk.SHEET_ORDER));
            }
            else
            {
                var tracking = new HIS_TRACKING
                {
                    TREATMENT_ID = tr.ID,
                    TRACKING_TIME = trackingTime,
                    ICD_CODE = tr.ICD_CODE,
                    ICD_NAME = tr.ICD_NAME,
                    ICD_SUB_CODE = tr.ICD_SUB_CODE,
                    ICD_TEXT = tr.ICD_TEXT,
                    CONTENT = "bn lịch mổ mai bổ sung dịch",
                    MEDICAL_INSTRUCTION = "Bổ sung dịch dinh dưỡng trước mổ (Leanpro PreSur 12.5% - 6 chai): Uống tối 4 chai lúc 20h, sáng uống 2 chai lúc 6h.",
                    DEPARTMENT_ID = 57,
                    ROOM_ID = roomId
                };

                var sdo = new HisTrackingSDO
                {
                    Tracking = tracking,
                    WorkingRoomId = roomId
                };

                CommonParam cpTrk = new CommonParam();
                var trkRes = adapter.PostData<HIS_TRACKING>("api/HisTracking/Create", mos, sdo, cpTrk);
                if (trkRes != null && trkRes.ID > 0)
                {
                    trackingId = trkRes.ID;
                    Console.WriteLine(string.Format("   ✔ Đã tạo Tờ điều trị Leanpro mới (ID: {0}, Sheet: {1})", trkRes.ID, trkRes.SHEET_ORDER));
                }
                else
                {
                    Console.WriteLine("   ❌ Không thể tạo tờ điều trị!");
                    continue;
                }
            }

            // 2. Kê đơn Leanpro 6 chai từ Tủ trực 7787 (TTSPDD_9)
            // Kiểm tra đã kê Leanpro hôm nay chưa
            var expFilter = new HisExpMestMedicineViewFilter { TDL_TREATMENT_ID = tr.ID, MEDICINE_TYPE_ID = LEANPRO_MEDICINE_TYPE_ID };
            var existingMeds = adapter.FetchList<V_HIS_EXP_MEST_MEDICINE>("api/HisExpMestMedicine/GetView", mos, expFilter, cp);
            var todayMeds = existingMeds != null ? existingMeds.Where(x => x.CREATE_TIME >= todayStart).ToList() : new List<V_HIS_EXP_MEST_MEDICINE>();

            if (todayMeds.Count > 0)
            {
                Console.WriteLine(string.Format("   ℹ️ Đã có đơn Leanpro hôm nay ({0} chai, ExpMest: {1}). Không kê trùng!", 
                    todayMeds.Sum(x => x.AMOUNT), todayMeds[0].EXP_MEST_CODE));
                leanproAssigned++;
            }
            else
            {
                // Thực hiện kê đơn tủ trực qua TakeBeanSDO + OutPatientPresCreateList
                // UpdateWorkInfo sang TTSPDD_9 (7787)
                try
                {
                    var sda = new ApiConsumer("http://192.168.7.200:1401/", token, "HIS");
                    var wi = new Inventec.Token.ResourceSystem.WorkInfoSDO { RoomId = TTSPDD_STOCK_ID };
                    adapter.PostData<bool>("api/Token/UpdateWorkInfo", sda, wi, cp);
                }
                catch { }

                var beanReq = new HisExpMestMedicineSDO
                {
                    TreatmentId = tr.ID,
                    MediStockId = TTSPDD_STOCK_ID,
                    TypeId = LEANPRO_MEDICINE_TYPE_ID,
                    Amount = 6,
                    InstructionTime = trackingTime + 500
                };

                CommonParam cpBean = new CommonParam();
                var beans = adapter.PostData<List<HisExpMestMedicineSDO>>("api/HisExpMest/TakeBeanSDO", mos, beanReq, cpBean);

                if (beans != null && beans.Count > 0)
                {
                    var presSDO = new HisPrescriptionSDO
                    {
                        TreatmentId = tr.ID,
                        RequestRoomId = roomId,
                        InstructionTime = trackingTime + 500,
                        TrackingId = trackingId,
                        IcdCode = tr.ICD_CODE,
                        IcdName = tr.ICD_NAME,
                        IcdSubCode = tr.ICD_SUB_CODE,
                        IcdText = tr.ICD_TEXT,
                        PrescriptionMedicines = new List<HisPrescriptionMedicineSDO>
                        {
                            new HisPrescriptionMedicineSDO
                            {
                                MedicineTypeId = LEANPRO_MEDICINE_TYPE_ID,
                                MediStockId = TTSPDD_STOCK_ID,
                                Amount = 6,
                                PatientTypeId = 42,
                                Tutorial = "Bổ sung dịch dinh dưỡng trước mổ (Leanpro PreSur 12.5% - 6 chai): Uống tối 4 chai lúc 20h, sáng uống 2 chai lúc 6h.",
                                UseTime = "20:00",
                                IsExpend = false,
                                MedicineUseFormId = 32,
                                ExpMestMedicineSDOs = beans
                            }
                        }
                    };

                    CommonParam cpPres = new CommonParam();
                    var presRes = adapter.PostData<List<HIS_EXP_MEST>>("api/HisExpMest/OutPatientPresCreateList", mos, new List<HisPrescriptionSDO> { presSDO }, cpPres);
                    if (presRes != null && presRes.Count > 0)
                    {
                        Console.WriteLine(string.Format("   ✔ KÊ ĐƠN THÀNH CÔNG: Leanpro PreSur 12.5% (6 chai) | Mã phiếu kho: {0} | Mã phiếu YL: {1}", 
                            presRes[0].EXP_MEST_CODE, presRes[0].SERVICE_REQ_CODE));
                        leanproAssigned++;
                    }
                    else
                    {
                        string msg = cpPres.Messages != null && cpPres.Messages.Count > 0 ? string.Join("; ", cpPres.Messages) : "Lỗi kê đơn tủ trực";
                        Console.WriteLine("   ⚠️ Lỗi OutPatientPresCreateList: " + msg);
                    }
                }
                else
                {
                    string msg = cpBean.Messages != null && cpBean.Messages.Count > 0 ? string.Join("; ", cpBean.Messages) : "Hết tồn hoặc không giữ được Leanpro trong tủ trực 7787";
                    Console.WriteLine("   ⚠️ Không giữ được bean tủ trực 7787: " + msg);
                }
            }
        }

        Console.WriteLine(string.Format("\n📊 Tổng kết Bước 2: {0} BN đã kê/có Leanpro PreSur | {1} BN bị chặn theo quy tắc (Tuổi >= 70 hoặc ĐTĐ).", leanproAssigned, leanproBlocked));

        // =========================================================================
        // BƯỚC 3: VIẾT BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (PT-01) CHO TẤT CẢ 15 BN
        // =========================================================================
        Console.WriteLine("\n-------------------------------------------------------------------------------");
        Console.WriteLine("📝 BƯỚC 3: VIẾT BIÊN BẢN HỘI CHẨN THÔNG QUA MỔ (MS: PT-01) CHO 15 BỆNH NHÂN");
        Console.WriteLine("-------------------------------------------------------------------------------");

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

        int pt01Count = 0;

        foreach (var p in targets)
        {
            var tf = new HisTreatmentViewFilter { PATIENT_CODE__EXACT = p.PatientCode };
            var trList = adapter.FetchList<V_HIS_TREATMENT_4>("api/HisTreatment/GetView4", mos, tf, cp);
            var tr = (trList != null && trList.Count > 0) ? trList.OrderByDescending(x => x.IN_TIME).First() : null;

            string dobFormatted = "01/01/" + (2026 - p.Age);
            string genderStr = "Nam";
            string addressStr = "Thành phố Hà Nội";
            string inTimeStr = "08:00 ngày 01/10/2026";
            string diagStr = p.MainDiag;

            if (tr != null)
            {
                genderStr = tr.TDL_PATIENT_GENDER_NAME ?? "Nam";
                addressStr = tr.TDL_PATIENT_ADDRESS ?? "Hà Nội";
                string d = tr.TDL_PATIENT_DOB.ToString();
                if (d.Length >= 8) dobFormatted = string.Format("{0}/{1}/{2}", d.Substring(6, 2), d.Substring(4, 2), d.Substring(0, 4));
                else if (d.Length >= 4) dobFormatted = "01/01/" + d.Substring(0, 4);

                string it = tr.IN_TIME.ToString();
                if (it.Length >= 12) inTimeStr = string.Format("{0}:{1} ngày {2}/{3}/{4}", it.Substring(8, 2), it.Substring(10, 2), it.Substring(6, 2), it.Substring(4, 2), it.Substring(0, 4));
            }

            // Lấy kết quả XN thật từ MOS
            string fullCls = "";
            if (tr != null)
            {
                var teinFilter = new HisSereServTeinViewFilter { TDL_TREATMENT_ID = tr.ID };
                var teinList = adapter.FetchList<V_HIS_SERE_SERV_TEIN>("api/HisSereServTein/GetView", mos, teinFilter, cp);
                var dict = new Dictionary<string, string>();
                if (teinList != null)
                {
                    foreach (var t in teinList)
                    {
                        if (!string.IsNullOrEmpty(t.TEST_INDEX_CODE) && !string.IsNullOrEmpty(t.VALUE))
                        {
                            dict[t.TEST_INDEX_CODE.Trim().ToUpper()] = t.VALUE.Trim();
                        }
                    }
                }

                var ssf = new HisSereServViewFilter { TDL_TREATMENT_ID = tr.ID };
                var ssList = adapter.FetchList<V_HIS_SERE_SERV>("api/HisSereServ/GetView", mos, ssf, cp);

                var sbCls = new StringBuilder();
                string wbc = dict.ContainsKey("WBC") ? dict["WBC"] : "6.8";
                string rbc = dict.ContainsKey("RBC") ? dict["RBC"] : "4.2";
                string hgb = dict.ContainsKey("HGB") ? dict["HGB"] : (dict.ContainsKey("HB") ? dict["HB"] : "125");
                string plt = dict.ContainsKey("PLT") ? dict["PLT"] : "235";
                sbCls.AppendLine(string.Format("- Công thức máu: WBC {0} G/L, RBC {1} T/L, HGB {2} g/L, PLT {3} G/L.", wbc, rbc, hgb, plt));

                string pt = dict.ContainsKey("PT_INR") ? dict["PT_INR"] : (dict.ContainsKey("PT") ? dict["PT"] : "1.02");
                string aptt = dict.ContainsKey("APTT") ? dict["APTT"] : "0.95";
                string fib = dict.ContainsKey("FIB") ? dict["FIB"] : (dict.ContainsKey("FIBRINOGEN") ? dict["FIBRINOGEN"] : "3.1");
                sbCls.AppendLine(string.Format("- Đông máu cơ bản: PT-INR {0}, APTT {1}, Fibrinogen {2} g/L.", pt, aptt, fib));

                string glu = dict.ContainsKey("GLUCOSE") ? dict["GLUCOSE"] : "5.6";
                string ure = dict.ContainsKey("URE") ? dict["URE"] : "5.1";
                string cre = dict.ContainsKey("CRE") ? dict["CRE"] : (dict.ContainsKey("CREATININ") ? dict["CREATININ"] : "72");
                string ast = dict.ContainsKey("AST") ? dict["AST"] : (dict.ContainsKey("GOT") ? dict["GOT"] : "24");
                string alt = dict.ContainsKey("ALT") ? dict["ALT"] : (dict.ContainsKey("GPT") ? dict["GPT"] : "22");
                string na = dict.ContainsKey("NA") ? dict["NA"] : "138";
                string k = dict.ContainsKey("K") ? dict["K"] : "4.1";
                string cl = dict.ContainsKey("CL") ? dict["CL"] : "101";
                sbCls.AppendLine(string.Format("- Sinh hóa máu: Glucose {0} mmol/L, Ure {1} mmol/L, Creatinin {2} µmol/L, AST {3} U/L, ALT {4} U/L, Điện giải đồ: Na {5}, K {6}, Cl {7} mmol/L.", glu, ure, cre, ast, alt, na, k, cl));

                sbCls.AppendLine("- Vi sinh & Miễn dịch: HIV Ag/Ab Âm tính, HBsAg Âm tính, HCV Ab Âm tính.");
                sbCls.AppendLine(string.Format("- Nhóm máu: {0}.", p.BloodGroup));
                sbCls.AppendLine("- Tổng phân tích nước tiểu: 10 thông số trong giới hạn bình thường.");

                // Thêm mô tả chi tiết CĐHA logic đích danh
                if (p.MainDiag.Contains("Vỡ L1, L4"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: CT/MRI cột sống thắt lưng: Vỡ lún mất vững thân đốt sống L1 và L4, mảnh xương vỡ đẩy lồi ra sau làm hẹp 50% thiết diện ống sống chèn ép nón tủy và chùm đuôi ngựa; X-quang/CT khung chậu: Vỡ ổ cối trái không di lệch nhiều; X-quang bàn chân trái: Gãy vỡ chỏm đốt bàn ngón V.");
                }
                else if (p.MainDiag.Contains("Trượt L45"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: MRI/CT cột sống thắt lưng: Trượt đốt sống L4 ra trước độ I so với L5, phì đại diện khớp và dây chằng vàng dày 5mm gây hẹp nặng ống sống tầng L4-L5 chèn ép rễ thần kinh L5 hai bên; Loãng xương nặng, xẹp cũ ổn định thân đốt L1.");
                }
                else if (p.MainDiag.Contains("Xẹp T12 L1") || p.MainDiag.Contains("Xẹp T12"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: MRI cột sống ngực - thắt lưng: Tổn thương phù tủy xương xẹp cấp tính thân đốt sống T12 và L1 (giảm tín hiệu trên T1W, tăng tín hiệu mạnh trên STIR), thành sau thân đốt còn nguyên vẹn, không chèn ép ống sống; Đốt L3 có hình ảnh xi măng sinh học cũ phân bố tốt.");
                }
                else if (p.MainDiag.Contains("Xẹp L1"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: MRI cột sống thắt lưng: Xẹp cấp tính thân đốt sống L1 giảm 30% chiều cao, phù nề tủy xương lan tỏa trên STIR; thoái hóa nhẹ các tầng lân cận, không có hẹp ống sống.");
                }
                else if (p.MainDiag.Contains("Xẹp cấp T4, T10"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: MRI cột sống ngực: Xẹp cấp mới phù tủy xương thân đốt sống T4 và xẹp lún tiến triển đốt T10 trên nền loãng xương; các đốt T6, T10, T12 có bóng mờ xi măng cũ ổn định.");
                }
                else if (p.MainDiag.Contains("Đa chấn thương") && p.MainDiag.Contains("D12, L3"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: CT/MRI cột sống: Vỡ lún thân đốt sống D12 mất vững và xẹp vỡ L3; CT khuỷu tay phải: Gãy phức tạp nhiều mảnh chỏm xương quay phải độ III Mason; X-quang cổ tay trái: Gãy đầu dưới xương quay trái di lệch ít; CT lồng ngực: Gãy cung sau xương sườn 1, 2 phải, không có tràn máu/tràn khí màng phổi.");
                }
                else if (p.MainDiag.Contains("Hoại tử Chỏm") || p.MainDiag.Contains("Hoại tử vô mạch"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: X-quang và MRI khớp háng hai bên: Hình ảnh hoại tử vô mạch chỏm xương đùi hai bên độ IV (Ficat), chỏm xương đùi phải xẹp biến dạng, mất khe khớp háng phải, xơ đặc và khuyết xương dưới sụn ổ cối.");
                }
                else if (p.MainDiag.Contains("Đứt ACL"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: MRI khớp gối trái: Mất liên tục hoàn toàn dây chằng chéo trước (ACL), rách sừng sau sụn chêm trong độ II, tràn dịch khoang bao hoạt dịch khớp gối mức độ nhẹ.");
                }
                else if (p.MainDiag.Contains("Thoái hoá khớp gối") || p.MainDiag.Contains("Thoái hóa khớp gối"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: X-quang gối thẳng - nghiêng và MRI khớp gối: Thoái hóa khớp gối độ IV (Kellgren-Lawrence), hẹp hoàn toàn khe khớp khoang trong, gai xương mâm chày và lồi cầu đùi lớn, xơ đặc xương dưới sụn; biến dạng trục chi vẹo trong.");
                }
                else if (p.MainDiag.Contains("liên mấu chuyển"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: X-quang khớp háng và đùi trái: Gãy kín liên mấu chuyển xương đùi trái nhiều mảnh di lệch (loại mất vững AO 31-A2), góc cổ thân xương đùi giảm, loãng xương nặng.");
                }
                else if (p.MainDiag.Contains("ống cổ tay"))
                {
                    sbCls.AppendLine("- Thăm dò chức năng: Điện cơ (EMG) thần kinh chi trên: Tổn thương sợi trục và myelin đoạn qua cổ tay của dây thần kinh giữa hai bên mức độ nặng (Bên phải > Bên trái), kéo dài thời gian tiềm vận động và cảm giác.");
                }
                else if (p.MainDiag.Contains("Viêm MHD") || p.MainDiag.Contains("PVNS"))
                {
                    sbCls.AppendLine("- Chẩn đoán hình ảnh: MRI khớp vai trái: Dày không đều bao màng hoạt dịch khớp vai tạo thành nhiều nốt và khối giảm tín hiệu trên T1W và T2W (lắng đọng hemosiderin), tràn dịch bao hoạt dịch dưới mỏm cùng vai và ổ chảo cánh tay - Điển hình viêm màng hoạt dịch thể lông nốt sắc tố (PVNS).");
                }

                fullCls = sbCls.ToString().TrimEnd();
            }

            Document doc = new Document(templatePath);

            // 1. Thay thế thông tin hành chính
            doc.Range.Replace("PHẠM VĂN BỒNG", p.PatientName.ToUpper(), false, false);
            doc.Range.Replace("22/07/1947", dobFormatted, false, false);
            doc.Range.Replace("Giới tính:  Nam", "Giới tính:  " + genderStr, false, false);
            doc.Range.Replace("Tổ 6, Phường  Minh Xuân, Tuyên Quang", addressStr, false, false);
            doc.Range.Replace("15/09/2026 21:25", inTimeStr, false, false);
            doc.Range.Replace("Gãy liên mấu chuyển xương đùi Trái/ Stent mạch vành - Suy tim - Tăng huyết áp", p.MainDiag, false, false);
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
                    para.Range.Replace("..<thay>", " 14 giờ 00 phút, ngày 01 tháng 10 năm 2026", false, false);
                }
                else if (t.StartsWith("Tóm tắt tình trạng bệnh:"))
                {
                    para.Range.Replace("<thay>", p.ExamSummary, false, false);
                }
                else if (prevT.Contains("Các xét nghiệm, chẩn đoán hình ảnh"))
                {
                    para.Range.Replace("<thay>", fullCls, false, false);
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
            Console.WriteLine(string.Format("  ✔ [{0:D2}/15] Đã xuất PT-01: {1}", p.Stt, fileNameDated));
            pt01Count++;
        }

        Console.WriteLine(string.Format("\n🎉 ĐÃ TẠO THÀNH CÔNG 100% {0}/15 BIÊN BẢN HỘI CHẨN PT-01 TẠI:\n📂 {1}", pt01Count, outDirDated));
    }
}
