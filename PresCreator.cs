using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Inventec.Core;
using Inventec.Common.WebApiClient;
using MOS.SDO;
using MOS.EFMODEL.DataModels;

namespace PresCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = @"e:\his-x64-28-11fix GDYK\his-x64\ReferencedAssemblies";
            string pluginDir = @"e:\his-x64-28-11fix GDYK\his-x64\Plugins\Module";
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
                string shortName = e.Name.Split(',')[0];
                string p1 = Path.Combine(baseDir, shortName + ".dll");
                if (File.Exists(p1)) return Assembly.LoadFrom(p1);
                string p2 = Path.Combine(pluginDir, shortName + ".dll");
                if (File.Exists(p2)) return Assembly.LoadFrom(p2);
                string p3 = Path.Combine(@"e:\his-x64-28-11fix GDYK\his-x64", shortName + ".dll");
                if (File.Exists(p3)) return Assembly.LoadFrom(p3);
                return null;
            };

            Execute();
        }

        class PatientPrescriptionConfig
        {
            public int Index { get; set; }
            public string PatientCode { get; set; }
            public string PatientName { get; set; }
            public string RoomBed { get; set; }
            public long TreatmentId { get; set; }
            public string IcdCode { get; set; }
            public string IcdName { get; set; }
            public string IcdSubCode { get; set; }
            public string IcdText { get; set; }
            public List<PresMedicineSDO> Medicines { get; set; }
        }

        static void Execute()
        {
            try
            {
                // 1. Read token from logs
                string logFile = @"E:\his-x64-28-11fix GDYK\his-x64\Logs\LogSystem.txt";
                string token = "";
                if (File.Exists(logFile))
                {
                    using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            int idx = line.IndexOf("TokenCode|");
                            if (idx >= 0 && line.Length >= idx + 10 + 64)
                            {
                                token = line.Substring(idx + 10, 64);
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Could not find TokenCode in log file.");
                    return;
                }

                Console.WriteLine("Using active token: " + token);
                var mosConsumer = new ApiConsumer("http://192.168.7.236:1608/", token, "HIS");
                var commonParam = new CommonParam();

                // Build config for 8 patients
                var patients = new List<PatientPrescriptionConfig>
                {
                    // 1. HỒ HỮU MÔN - 000007092259
                    new PatientPrescriptionConfig
                    {
                        Index = 1,
                        PatientCode = "000007092259",
                        PatientName = "HỒ HỮU MÔN",
                        RoomBed = "P.712 - G.24",
                        TreatmentId = 7092075,
                        IcdCode = "S91.0",
                        IcdName = "Đứt gân Achilles chân trái",
                        IcdSubCode = "M10.00",
                        IcdText = "Gout",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 23646, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/truyền TM Sáng 1 lọ, Chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    },

                    // 2. TRẦN VĂN HẢI - 000007071532
                    new PatientPrescriptionConfig
                    {
                        Index = 2,
                        PatientCode = "000007071532",
                        PatientName = "TRẦN VĂN HẢI",
                        RoomBed = "P.712 - G.22",
                        TreatmentId = 7071348,
                        IcdCode = "D36.1",
                        IcdName = "TD Schwannoma khuỷu phải",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 25006, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 27427, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha tiêm 2 ống/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 18907, MediStockId = 4209, PatientTypeId = 1, Amount = 1.0m, Morning = "1", Tutorial = "Tiêm/truyền theo y lệnh 1 lọ/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    },

                    // 3. LÊ DOÃN NGUYÊN - 000007084847
                    new PatientPrescriptionConfig
                    {
                        Index = 3,
                        PatientCode = "000007084847",
                        PatientName = "LÊ DOÃN NGUYÊN",
                        RoomBed = "P.712 - G.21",
                        TreatmentId = 7084663,
                        IcdCode = "S62.30",
                        IcdName = "Gãy kín xương bàn ngón IV, V tay phải.",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 17176, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    },

                    // 4. NGUYỄN TRỌNG TỀ - 000007068288
                    new PatientPrescriptionConfig
                    {
                        Index = 4,
                        PatientCode = "000007068288",
                        PatientName = "NGUYỄN TRỌNG TỀ",
                        RoomBed = "P.724 - G.53",
                        TreatmentId = 7068104,
                        IcdCode = "S30.0",
                        IcdName = "Trượt thân đốt sống L3-Xẹp đốt sống D12/Đái tháo đường typ 2, THA",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 17175, MediStockId = 4210, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Uống sáng 1v, chiều 1v sau ăn", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 23554, MediStockId = 4210, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Evening = "1",   Tutorial = "Uống sáng 1v, tối 1v sau ăn", NumOfDays = 1 }
                        }
                    },

                    // 5. PHÙNG VĂN NẦNG - 000007087595
                    new PatientPrescriptionConfig
                    {
                        Index = 5,
                        PatientCode = "000007087595",
                        PatientName = "PHÙNG VĂN NẦNG",
                        RoomBed = "P.724 - G.50",
                        TreatmentId = 7087411,
                        IcdCode = "M86.48",
                        IcdName = "Áp xe- viêm xương tuỷ xương ngón II tay trái/ Tắc ĐM gốc quay tay trái- THA",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 26044, MediStockId = 4209, PatientTypeId = 1, Amount = 1.0m, Morning = "1", Tutorial = "Tiêm dưới da 1 bơm/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    },

                    // 6. NGUYỄN THỊ NGUYỆT - 000007082247
                    new PatientPrescriptionConfig
                    {
                        Index = 6,
                        PatientCode = "000007082247",
                        PatientName = "NGUYỄN THỊ NGUYỆT",
                        RoomBed = "P.724 - G.50",
                        TreatmentId = 7082063,
                        IcdCode = "M75.1",
                        IcdName = "Theo dõi nhiễm trùng sau mô nội soi khâu chóp xoay vai P",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25223, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    },

                    // 7. LÊ VĂN CHIẾN - 000007060449
                    new PatientPrescriptionConfig
                    {
                        Index = 7,
                        PatientCode = "000007060449",
                        PatientName = "LÊ VĂN CHIẾN",
                        RoomBed = "P.724 - G.49",
                        TreatmentId = 7060265,
                        IcdCode = "M47.00†",
                        IcdName = "Viêm đốt sống đĩa đệm T4, T5 áp xe ngoài màng cứng, AIHB/ THA, ĐTĐ typ 2",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 18952, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25223, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền TM chậm 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 15474, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Tiêm/Truyền TM sáng 1 lọ, chiều 1 lọ", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25107, MediStockId = 804,  PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Pha truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 26573, MediStockId = 4210, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Uống sáng 1v, chiều 1v sau ăn", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    },

                    // 8. MAI THỊ THÙY LINH - 000007092253
                    new PatientPrescriptionConfig
                    {
                        Index = 8,
                        PatientCode = "000007092253",
                        PatientName = "MAI THỊ THÙY LINH",
                        RoomBed = "P.712A - G.20A",
                        TreatmentId = 7092069,
                        IcdCode = "S42.30",
                        IcdName = "Gãy 1/3 giữa xương cánh tay phải",
                        Medicines = new List<PresMedicineSDO>
                        {
                            new PresMedicineSDO { MedicineTypeId = 18184, MediStockId = 4208, PatientTypeId = 1, Amount = 1.0m, Evening = "1",   Tutorial = "Uống tối 20h 1 viên", NumOfDays = 1 },
                            new PresMedicineSDO { MedicineTypeId = 25683, MediStockId = 4209, PatientTypeId = 1, Amount = 2.0m, Morning = "1", Afternoon = "1", Tutorial = "Truyền tĩnh mạch 2 chai/ngày", NumOfDays = 1 }
                        }
                    }
                };

                long[] instructionDates = new long[] { 20260824080000, 20260825080000 };

                Console.WriteLine("\n================ BẮT ĐẦU TẠO ĐƠN THUỐC NỘI TRÚ (KHO DƯỢC VIỆN) ================\n");

                foreach (var p in patients)
                {
                    Console.WriteLine(string.Format(">>> [{0}/8] {1} ({2}) - {3} - TreatmentId: {4}", p.Index, p.PatientName, p.PatientCode, p.RoomBed, p.TreatmentId));

                    foreach (var instTime in instructionDates)
                    {
                        string dateStr = instTime.ToString().Substring(6, 2) + "/" + instTime.ToString().Substring(4, 2) + "/" + instTime.ToString().Substring(0, 4);

                        var sdo = new InPatientPresSDO
                        {
                            TreatmentId = p.TreatmentId,
                            RequestRoomId = 5252, // Khoa CTCH & CS
                            RequestLoginName = "034727",
                            RequestUserName = "NGUYỄN HỮU SÂM",
                            IcdCode = p.IcdCode,
                            IcdName = p.IcdName,
                            IcdSubCode = p.IcdSubCode ?? "",
                            IcdText = p.IcdText ?? "",
                            PrescriptionTypeId = (PrescriptionType)1,
                            InstructionTimes = new List<long> { instTime },
                            Medicines = p.Medicines
                        };

                        try
                        {
                            var result = mosConsumer.Post<InPatientPresResultSDO>("api/HisServiceReq/InPatientPresCreate", commonParam, sdo, new object[0]);

                            if (result != null && result.ServiceReqs != null && result.ServiceReqs.Count > 0)
                            {
                                var req = result.ServiceReqs[0];
                                string expCodes = result.ExpMests != null ? string.Join(", ", result.ExpMests.Select(e => e.EXP_MEST_CODE)) : "";
                                Console.WriteLine(string.Format("  [THÀNH CÔNG] Ngày {0}: Mã Y lệnh = {1} (ID: {2}) | Phiếu xuất = [{3}] | Số thuốc = {4}", 
                                    dateStr, req.SERVICE_REQ_CODE, req.ID, expCodes, result.Medicines != null ? result.Medicines.Count : 0));
                            }
                            else
                            {
                                Console.WriteLine(string.Format("  [THẤT BẠI] Ngày {0}: Không tạo được đơn. Lỗi: {1} (Bug: {2})", dateStr, commonParam.GetMessage(), commonParam.GetBugCode()));
                            }
                        }
                        catch (Exception exReq)
                        {
                            Console.WriteLine(string.Format("  [LỖI EXCEPTION] Ngày {0}: {1}", dateStr, exReq.Message));
                        }
                    }
                    Console.WriteLine();
                }

                Console.WriteLine("================ HOÀN TẤT TẠO ĐƠN THUỐC CHO 8 BỆNH NHÂN ================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal Error: " + ex);
            }
        }
    }
}
