// ==============================================================================
// HisPt01ApiUploader.cs — (SKELETON — CHỜ ENDPOINT)
// Tự động nạp nội dung Biên bản Hội chẩn Thông qua Mổ (PT-01) vào HIS
// qua REST API trực tiếp — Không cần điều khiển UI.
//
// TRẠNG THÁI: Đang chờ capture HTTP traffic để xác nhận endpoint chính xác.
//             Xem hướng dẫn tại: autopt01/README.md
// ==============================================================================
//
// KHI ĐÃ CÓ ENDPOINT → Agent sẽ điền vào TODO bên dưới và compile thành .exe
//
// Cú pháp sử dụng (sau khi hoàn thiện):
//   HisPt01ApiUploader.exe <MãBN1,MãBN2,...>
//   HisPt01ApiUploader.exe --batch      (xử lý toàn bộ file trong BienBanHoiChan_PT01\)
//
// ==============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Inventec.Common.Adapter;
using Inventec.Common.WebApiClient;
using Inventec.Core;
using MOS.EFMODEL.DataModels;
using MOS.Filter;

class HisPt01ApiUploader
{
    // ──────────────────────────────────────────────────────────────────────
    // CẤU HÌNH CỐ ĐỊNH
    // ──────────────────────────────────────────────────────────────────────
    const string MOS_BASE    = "http://192.168.7.236:1608/";
    const string NB_MOS_BASE = "http://192.168.7.239:1608/"; // TODO: xác nhận IP NB
    const string LOG_PATH    = "LogSystem.txt";

    // TODO: Điền sau khi capture traffic
    // Endpoint dự kiến (1 trong các phương án):
    // const string FORM_OTHER_API = "api/HisMedicalFormOther/Create";
    // const string FORM_OTHER_API = "api/HisFormOther/SaveContent";
    // const string FORM_OTHER_API = "api/HisOtherMedicalRecord/Create";
    const string FORM_OTHER_API = "TODO_ENDPOINT_AFTER_CAPTURE";

    // ID mẫu PT-01 trong danh mục biểu mẫu HIS
    // TODO: Xác nhận sau capture (tên gần đúng: "Biên bản hội chẩn thông qua mổ")
    const long PT01_FORM_TEMPLATE_ID = 0; // TODO

    const string DEFAULT_DOCTOR_CODE = "034727";

    // ──────────────────────────────────────────────────────────────────────
    // ENTRY POINT
    // ──────────────────────────────────────────────────────────────────────
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   ⚡ HIS PT-01 API UPLOADER — AUTOPT01 CODENAME             ║");
        Console.WriteLine("║   Nạp Biên Bản Mổ PT-01 qua API (Không cần UI)             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

        if (FORM_OTHER_API == "TODO_ENDPOINT_AFTER_CAPTURE")
        {
            Console.WriteLine("❌ CHƯA SẴN SÀNG: Cần capture HTTP traffic trước!");
            Console.WriteLine("   Xem hướng dẫn: autopt01\\README.md");
            Console.WriteLine("   Sau khi có captured_request.txt, commit lên git → Agent sẽ build tiếp.");
            return;
        }

        // TODO: Phần còn lại sẽ được triển khai sau khi xác nhận endpoint
        // Luồng dự kiến:
        // 1. Đọc token từ LogSystem.txt
        // 2. Tìm TreatmentId của BN qua api/HisTreatment/Get
        // 3. Convert file .docx → RTF/Base64 (dùng Aspose.Words)
        // 4. POST lên FORM_OTHER_API với payload chứa content + TreatmentId + TemplateId
        // 5. In ra FormId / ServiceReqCode để bác sĩ tra cứu
    }
}
