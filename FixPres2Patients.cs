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
using MOS.EFMODEL.DataModels;
using MOS.SDO;

class FixPres2Patients
{
    public class MyAdapter : AdapterBase
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

    static void Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
            string shortName = e.Name.Split(',')[0];
            string p1 = Path.Combine("ReferencedAssemblies", shortName + ".dll");
            if (File.Exists(p1)) return Assembly.LoadFrom(p1);
            return null;
        };

        Run();
    }

    static void Run()
    {
        string token = "";
        using (var fs = new FileStream(@"Logs\LogSystem.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fs))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int idx = line.IndexOf("TokenCode|");
                if (idx >= 0 && line.Length >= idx + 10 + 64)
                    token = line.Substring(idx + 10, 64);
            }
        }

        var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
        MyAdapter adapter = new MyAdapter();
        CommonParam cp = new CommonParam();

        // Bind token to room 5257 (Buồng 724)
        var workInfo = new WorkInfoSDO
        {
            Rooms = new List<RoomSDO> { new RoomSDO { RoomId = 5257 }, new RoomSDO { RoomId = 5248 } }
        };
        adapter.PostData<List<WorkPlaceSDO>>("api/Token/UpdateWorkInfo", mosConsumer, workInfo, cp);

        long targetInstructionTime = 20260827080000;

        // 1. PHAM VAN THEM (7023543)
        Console.WriteLine("\n--- KÊ ĐƠN CHO BN PHẠM VĂN THÊM (7023543) ---");
        var sdoThem = new InPatientPresSDO
        {
            TreatmentId = 7023543,
            RequestRoomId = 5257,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "L98.4",
            IcdName = "Nhiễm trùng hoại tử cổ bàn chân 2 bên viêm xương tủy xương",
            IcdText = "",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { targetInstructionTime },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = 22583, // Meropenem/ Anfarm 1g
                    MediStockId = 4209, // Kho thuốc ống
                    PatientTypeId = 1,
                    Amount = 1,
                    Afternoon = "01",
                    Tutorial = "Hoàn nguyên 10ml nước cất, pha với 100ml NaCl 0.9% truyền tĩnh mạch 33ml/h lúc 15h",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 17182, // Zyvox 600mg/300ml
                    MediStockId = 4209, // Kho thuốc ống
                    PatientTypeId = 1,
                    Amount = 1,
                    Afternoon = "01",
                    Tutorial = "Truyền tĩnh mạch 30 giọt/phút lúc 15h",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 25107, // Sodium Chloride 0,9% 100ml
                    MediStockId = 804, // Kho Dịch truyền
                    PatientTypeId = 1,
                    Amount = 1,
                    Afternoon = "01",
                    Tutorial = "Dung môi pha Meropenem truyền TM lúc 15h",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 25110, // Cordaflex 20 mg
                    MediStockId = 4210, // Kho thuốc viên
                    PatientTypeId = 1,
                    Amount = 1,
                    Morning = "01",
                    Tutorial = "Ngày uống 1 viên buổi sáng lúc 6h00",
                    NumOfDays = 1
                }
            }
        };

        CommonParam cp1 = new CommonParam();
        var res1 = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, sdoThem, cp1);
        if (res1 != null && res1.ExpMests != null && res1.ExpMests.Count > 0)
        {
            Console.WriteLine("✅ KÊ ĐƠN THÀNH CÔNG CHO PHẠM VĂN THÊM: " + string.Join(", ", res1.ExpMests.Select(x => x.EXP_MEST_CODE + " (Kho " + x.MEDI_STOCK_ID + ")")));
        }
        else
        {
            Console.WriteLine("❌ THẤT BẠI: " + cp1.GetBugCode() + " | " + cp1.GetMessage());
            if (cp1.Messages != null) foreach (var m in cp1.Messages) Console.WriteLine("  - " + m);
        }

        // 2. NGUYEN TRONG TE (7068104)
        Console.WriteLine("\n--- KÊ ĐƠN CHO BN NGUYỄN TRỌNG TỀ (7068104) ---");
        var sdoTe = new InPatientPresSDO
        {
            TreatmentId = 7068104,
            RequestRoomId = 5257,
            RequestLoginName = "034727",
            RequestUserName = "NGUYỄN HỮU SÂM",
            IcdCode = "S30.0",
            IcdName = "Trượt thân đốt sống L3-Xẹp đốt sống D12",
            IcdText = "",
            PrescriptionTypeId = (PrescriptionType)1,
            InstructionTimes = new List<long> { targetInstructionTime },
            Medicines = new List<PresMedicineSDO>
            {
                new PresMedicineSDO
                {
                    MedicineTypeId = 18918, // Partamol Tab. 500mg
                    MediStockId = 4210, // Kho thuốc viên
                    PatientTypeId = 1,
                    Amount = 2,
                    Morning = "01",
                    Evening = "01",
                    Tutorial = "Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 17155, // Celebrex 200mg
                    MediStockId = 4210, // Kho thuốc viên
                    PatientTypeId = 1,
                    Amount = 2,
                    Morning = "01",
                    Evening = "01",
                    Tutorial = "Ngày uống 2 viên chia 2 lần, sáng: 1 viên, tối: 1 viên sau ăn no",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 15040, // Lyrica 75mg
                    MediStockId = 4210, // Kho thuốc viên
                    PatientTypeId = 1,
                    Amount = 1,
                    Evening = "01",
                    Tutorial = "Ngày uống 1 viên buổi tối",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 18239, // Briozcal (500mg + 125IU)
                    MediStockId = 4210, // Kho thuốc viên
                    PatientTypeId = 1,
                    Amount = 1,
                    Morning = "01",
                    Tutorial = "Ngày uống 1 viên buổi sáng",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 25222, // Glucose 5% 500ml
                    MediStockId = 804, // Kho Dịch truyền
                    PatientTypeId = 1,
                    Amount = 1,
                    Morning = "01",
                    Tutorial = "Thuốc pha tiêm phong bế",
                    NumOfDays = 1
                },
                new PresMedicineSDO
                {
                    MedicineTypeId = 17385, // Povidone 10% 125ml
                    MediStockId = 810, // Tủ trực khoa CTCH & Cột sống (Povidone là dung dịch sát khuẩn dùng tại buồng bệnh tủ trực)
                    PatientTypeId = 1,
                    Amount = 1,
                    Morning = "01",
                    Tutorial = "Ngày dùng ngoài 1 chai buổi sáng",
                    NumOfDays = 1
                }
            }
        };

        CommonParam cp2 = new CommonParam();
        var res2 = adapter.PostData<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", mosConsumer, sdoTe, cp2);
        if (res2 != null && res2.ExpMests != null && res2.ExpMests.Count > 0)
        {
            Console.WriteLine("✅ KÊ ĐƠN THÀNH CÔNG CHO NGUYỄN TRỌNG TỀ: " + string.Join(", ", res2.ExpMests.Select(x => x.EXP_MEST_CODE + " (Kho " + x.MEDI_STOCK_ID + ")")));
        }
        else
        {
            Console.WriteLine("❌ THẤT BẠI: " + cp2.GetBugCode() + " | " + cp2.GetMessage());
            if (cp2.Messages != null) foreach (var m in cp2.Messages) Console.WriteLine("  - " + m);
        }
    }
}
