#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
parse_glucose_image.py
======================
Nhận ảnh báo cáo đường huyết điều dưỡng → Trích xuất dữ liệu có cấu trúc JSON
Ưu tiên sử dụng OpenRouter Free Tier (stealth/ox-alpha) hoặc Gemini Vision API.

Cú pháp:
    python parse_glucose_image.py <đường_dẫn_ảnh> [--output glucose_data.json]
    python parse_glucose_image.py report.jpg --output glucose_data.json
    python parse_glucose_image.py report.jpg --preview  # Xem bảng xác nhận trước
    python parse_glucose_image.py report.jpg --provider openrouter --model stealth/ox-alpha

Cấu hình API Keys:
    - OpenRouter: Biến môi trường OPENROUTER_API_KEY (hoặc tham số --openrouter-key)
    - Gemini: Biến môi trường GEMINI_API_KEY (hoặc tham số --api-key)
"""

import sys
import os
import json
import base64
import argparse
import re
from datetime import datetime, timedelta
from pathlib import Path

# Đảm bảo stdout hiển thị tiếng Việt UTF-8 chuẩn trên console Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

# ==============================================================================
# PROMPT TRÍCH XUẤT Y LỆNH & ĐƯỜNG HUYẾT
# ==============================================================================

OCR_PROMPT = """
Bạn là trợ lý y tế chuyên trích xuất dữ liệu bảng từ ảnh báo cáo theo dõi đường huyết của điều dưỡng bệnh viện.

Hãy phân tích ảnh và trích xuất CHÍNH XÁC theo định dạng JSON sau.

Quy tắc quan trọng:
1. Mã bệnh nhân (patient_code) thường có dạng 10 chữ số (VD: 0003969449). Nếu chỉ thấy tên, ghi tên vào patient_name và để patient_code rỗng.
2. Kết quả đường huyết tính theo mmol/L (nếu đơn vị là mg/dL thì chia 18 để quy đổi).
3. **KÝ HIỆU VIẾT TẮT INSULIN CỦA ĐIỀU DƯỠNG (BẮT BUỘC QUY ĐỔI CHUẨN XÁC):**
   - **`R`** (VD: `6R`, `8R`, `4R`, `R6`, `R8`): là **Actrapid** (Insulin Regular tác dụng nhanh). Ví dụ `6R` -> `insulin_name: "Actrapid"`, `insulin_dose_ui: 6`.
   - **`L`** (VD: `10L`, `12L`, `14L`, `L10`, `L12`): là **Lantus** (Insulin Glargine nền kéo dài). Ví dụ `10L` -> `insulin_name: "Lantus"`, `insulin_dose_ui: 10`.
   - **`M`** (VD: `8M`, `10M`, `12M`, `M8`, `M10`): là **Mixtard** (Insulin hỗn hợp / Mix 30/70). Ví dụ `8M` -> `insulin_name: "Mixtard"`, `insulin_dose_ui: 8`.
4. Nếu cột không có kết quả (chưa đo, bỏ trống) thì glucose_mmol = null.
5. Nếu không tiêm insulin (đường huyết thấp hoặc ghi "0" hoặc bỏ trống hoặc gạch ngang) thì inject = false, insulin_dose_ui = 0.
6. Xác định ngày báo cáo (report_date) từ ảnh. Mốc 06:00 thuộc NGÀY HÔM SAU (thêm 1 ngày).
7. Nếu không đọc được mã BN, ghi chú vào trường "note" để bác sĩ xác nhận thủ công.

Trả về JSON hợp lệ theo cấu trúc sau (KHÔNG có text ngoài JSON):

{
  "report_date": "YYYY-MM-DD",
  "parse_note": "Ghi chú tổng quan nếu có vấn đề nhận dạng",
  "patients": [
    {
      "patient_code": "0003969449",
      "patient_name": "Nguyễn Văn A",
      "bed": "Phòng 711 - Giường 02",
      "note": "",
      "sessions": [
        {
          "time": "17:00",
          "date": "YYYY-MM-DD",
          "glucose_mmol": 12.5,
          "insulin_name": "Actrapid",
          "insulin_dose_ui": 8,
          "inject": true,
          "raw_text": "Đường huyết 17h: 12.5 mmol/l - Actrapid 8 đv"
        },
        {
          "time": "21:00",
          "date": "YYYY-MM-DD",
          "glucose_mmol": 9.2,
          "insulin_name": "Actrapid",
          "insulin_dose_ui": 6,
          "inject": true,
          "raw_text": "ĐH 21h: 9.2 - 6 UI Actrapid"
        },
        {
          "time": "06:00",
          "date": "YYYY-MM-DD+1",
          "glucose_mmol": null,
          "insulin_name": "Lantus",
          "insulin_dose_ui": 10,
          "inject": true,
          "raw_text": "6h sáng: chưa đo - Lantus 10 đv"
        }
      ]
    }
  ]
}

Lưu ý: Nếu ảnh chỉ có 1 thời điểm (VD chỉ 17h), chỉ trả về sessions với thời điểm đó.
"""


# ==============================================================================
# HÀM TIỆN ÍCH
# ==============================================================================

def get_api_keys() -> tuple[str, str]:
    """Tìm API Key từ biến môi trường hoặc Windows Registry"""
    openrouter_key = os.environ.get("OPENROUTER_API_KEY", "")
    gemini_key = os.environ.get("GEMINI_API_KEY", "")

    if sys.platform == "win32" and (not openrouter_key or not gemini_key):
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as key:
                if not openrouter_key:
                    try:
                        openrouter_key, _ = winreg.QueryValueEx(key, "OPENROUTER_API_KEY")
                    except FileNotFoundError:
                        pass
                if not gemini_key:
                    try:
                        gemini_key, _ = winreg.QueryValueEx(key, "GEMINI_API_KEY")
                    except FileNotFoundError:
                        pass
        except Exception:
            pass

    return openrouter_key.strip(), gemini_key.strip()


def load_image_base64(image_path: str) -> tuple[str, str]:
    """Đọc ảnh và trả về (base64_data, mime_type)"""
    path = Path(image_path)
    if not path.exists():
        raise FileNotFoundError(f"Không tìm thấy file ảnh: {image_path}")

    ext = path.suffix.lower()
    mime_map = {
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".png": "image/png",
        ".gif": "image/gif",
        ".webp": "image/webp",
        ".bmp": "image/bmp",
        ".pdf": "application/pdf",
    }
    mime_type = mime_map.get(ext, "image/jpeg")

    with open(image_path, "rb") as f:
        data = base64.b64encode(f.read()).decode("utf-8")

    return data, mime_type


def call_openrouter_vision(image_path: str, api_key: str, model: str = "stealth/ox-alpha") -> dict:
    """Gọi OpenRouter API (Ưu tiên mô hình Free Tier stealth/ox-alpha) để phân tích ảnh"""
    import urllib.request

    image_data, mime_type = load_image_base64(image_path)

    url = "https://openrouter.ai/api/v1/chat/completions"
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json; charset=utf-8",
        "HTTP-Referer": "https://github.com/onthibacsinoitru9999-coder/hisx64-crossover",
        "X-Title": "HIS AI Integration Suite"
    }

    payload = {
        "model": model,
        "messages": [
            {
                "role": "user",
                "content": [
                    {"type": "text", "text": OCR_PROMPT},
                    {
                        "type": "image_url",
                        "image_url": {
                            "url": f"data:{mime_type};base64,{image_data}"
                        }
                    }
                ]
            }
        ],
        "response_format": {"type": "json_object"},
        "max_tokens": 16384
    }

    req = urllib.request.Request(url, data=json.dumps(payload).encode("utf-8"), headers=headers)
    with urllib.request.urlopen(req, timeout=90) as resp:
        res_json = json.loads(resp.read().decode("utf-8"))

    choices = res_json.get("choices", [])
    if not choices:
        raise ValueError(f"OpenRouter không trả về choices hợp lệ: {res_json}")

    message = choices[0].get("message", {})
    raw_text = message.get("content", "") or ""

    # Làm sạch markdown code block nếu có
    raw_text = re.sub(r"^```json\s*", "", raw_text.strip(), flags=re.IGNORECASE)
    raw_text = re.sub(r"\s*```$", "", raw_text.strip())
    raw_text = raw_text.strip()

    return json.loads(raw_text)


def call_gemini_vision(image_path: str, api_key: str) -> dict:
    """Gọi Gemini Vision API để phân tích ảnh"""
    try:
        import google.generativeai as genai
        genai.configure(api_key=api_key)

        model = genai.GenerativeModel("gemini-2.5-flash-preview-05-20")

        # Tải ảnh
        image_data, mime_type = load_image_base64(image_path)

        # Gọi API
        response = model.generate_content([
            {"mime_type": mime_type, "data": image_data},
            OCR_PROMPT
        ])

        raw_text = response.text.strip()

        # Làm sạch markdown code block nếu có
        raw_text = re.sub(r"^```json\s*", "", raw_text)
        raw_text = re.sub(r"\s*```$", "", raw_text)
        raw_text = raw_text.strip()

        return json.loads(raw_text)

    except ImportError:
        raise ImportError("Thiếu thư viện google-generativeai: pip install google-generativeai")
    except json.JSONDecodeError as e:
        raise ValueError(f"Gemini trả về không phải JSON hợp lệ: {e}\nRaw: {raw_text[:500]}")


def fix_dates(data: dict, report_date_str: str = None) -> dict:
    """Tự động chỉnh ngày: mốc 06:00 → ngày hôm sau"""
    if report_date_str:
        base_date = datetime.strptime(report_date_str, "%Y-%m-%d")
    else:
        base_date = datetime.today()

    report_date = base_date.strftime("%Y-%m-%d")
    next_date = (base_date + timedelta(days=1)).strftime("%Y-%m-%d")

    for patient in data.get("patients", []):
        for session in patient.get("sessions", []):
            t = session.get("time", "")
            # Mốc 06:00 thuộc sáng hôm sau
            if t.startswith("06"):
                session["date"] = next_date
            else:
                session["date"] = report_date

    data["report_date"] = report_date
    return data


def print_preview_table(data: dict):
    """In bảng xác nhận dữ liệu trước khi thực thi"""
    print("\n" + "=" * 90)
    print("  📋 BẢNG XÁC NHẬN DỮ LIỆU ĐƯỜNG HUYẾT TRÍCH XUẤT TỪ ẢNH")
    print("=" * 90)
    print(f"  Ngày báo cáo: {data.get('report_date', '?')}")
    if data.get("parse_note"):
        print(f"  ⚠️  Lưu ý OCR: {data['parse_note']}")
    print()

    header = f"  {'STT':<4} {'Mã BN':<14} {'Tên BN':<22} {'Giờ':<8} {'ĐH(mmol)':<12} {'Insulin':<14} {'Liều(UI)':<10} {'Tiêm':<6}"
    print(header)
    print("  " + "-" * 88)

    idx = 1
    for patient in data.get("patients", []):
        code = patient.get("patient_code", "?????")
        name = patient.get("patient_name", "???")[:20]
        note = patient.get("note", "")

        for session in patient.get("sessions", []):
            glucose = session.get("glucose_mmol")
            glucose_str = f"{glucose:.1f}" if glucose is not None else "Chưa đo"
            insulin = session.get("insulin_name", "-")
            dose = session.get("insulin_dose_ui", 0)
            inject = "✅" if session.get("inject") else "❌"
            time_str = f"{session.get('time','')} ({session.get('date','')})"

            print(f"  {idx:<4} {code:<14} {name:<22} {time_str:<20} {glucose_str:<12} {insulin:<14} {dose:<10} {inject}")
            if note:
                print(f"       ⚠️  Ghi chú: {note}")
            idx += 1

    print("=" * 90)
    print(f"  Tổng: {idx-1} dòng y lệnh | {len(data.get('patients', []))} bệnh nhân")
    print("=" * 90)


def save_output(data: dict, output_path: str):
    """Lưu JSON ra file"""
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    print(f"\n✅ Đã lưu dữ liệu → {output_path}")


# ==============================================================================
# MAIN
# ==============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Phân tích ảnh báo cáo đường huyết điều dưỡng → JSON cấu trúc",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
VÍ DỤ SỬ DỤNG:
  python parse_glucose_image.py report.jpg
  python parse_glucose_image.py report.jpg --output glucose_data.json
  python parse_glucose_image.py report.jpg --preview
  python parse_glucose_image.py report.jpg --provider openrouter --model stealth/ox-alpha
  python parse_glucose_image.py report.jpg --date 2026-08-25 --output glucose_data.json
"""
    )
    parser.add_argument("image", help="Đường dẫn file ảnh (JPG/PNG/PDF)")
    parser.add_argument("--output", "-o", default="glucose_data.json",
                        help="File JSON đầu ra (mặc định: glucose_data.json)")
    parser.add_argument("--date", "-d", default="",
                        help="Ngày báo cáo (YYYY-MM-DD), mặc định là hôm nay")
    parser.add_argument("--preview", "-p", action="store_true",
                        help="Hiển thị bảng xác nhận và hỏi trước khi lưu")
    parser.add_argument("--provider", choices=["auto", "openrouter", "gemini"], default="auto",
                        help="Nhà cung cấp AI (auto = ưu tiên openrouter stealth/ox-alpha, fallback gemini)")
    parser.add_argument("--model", "-m", default="stealth/ox-alpha",
                        help="Tên mô hình OpenRouter (mặc định: stealth/ox-alpha - Free Tier 1M context)")
    parser.add_argument("--openrouter-key", default="",
                        help="OpenRouter API Key (hoặc set biến OPENROUTER_API_KEY)")
    parser.add_argument("--api-key", "-k", default="",
                        help="Gemini API Key (hoặc set biến GEMINI_API_KEY)")
    args = parser.parse_args()

    env_openrouter, env_gemini = get_api_keys()
    openrouter_key = args.openrouter_key or env_openrouter
    gemini_key = args.api_key or env_gemini

    print(f"\n🔍 Đang phân tích ảnh: {args.image}")

    data = None
    last_err = None

    # Xác định provider ưu tiên
    provider = args.provider
    if provider == "auto":
        if openrouter_key:
            provider = "openrouter"
        elif gemini_key:
            provider = "gemini"
        else:
            print("❌ Lỗi: Cần OPENROUTER_API_KEY hoặc GEMINI_API_KEY!")
            print("   Ưu tiên OpenRouter Free Tier (stealth/ox-alpha): set OPENROUTER_API_KEY=sk-or-...")
            print("   Hoặc Gemini: set GEMINI_API_KEY=AIzaSy...")
            sys.exit(1)

    # 1. Thử gọi OpenRouter stealth/ox-alpha trước
    if provider == "openrouter" or (args.provider == "auto" and openrouter_key):
        if not openrouter_key:
            print("❌ Lỗi: Chưa cung cấp OpenRouter API Key!")
            sys.exit(1)
        print(f"   🤖 Gọi OpenRouter API (Mô hình: {args.model} - Free Tier 1M Context)...")
        try:
            data = call_openrouter_vision(args.image, openrouter_key, model=args.model)
        except Exception as ex:
            last_err = ex
            print(f"   ⚠️  OpenRouter gặp sự cố: {ex}")
            if gemini_key and args.provider == "auto":
                print("   🔄 Chuyển sang fallback Gemini Vision API...")
                try:
                    data = call_gemini_vision(args.image, gemini_key)
                except Exception as g_ex:
                    last_err = g_ex

    # 2. Hoặc gọi Gemini
    elif provider == "gemini":
        if not gemini_key:
            print("❌ Lỗi: Chưa cung cấp Gemini API Key!")
            sys.exit(1)
        print("   🤖 Gọi Gemini Vision API...")
        try:
            data = call_gemini_vision(args.image, gemini_key)
        except Exception as ex:
            last_err = ex

    if not data:
        print(f"❌ Không thể phân tích ảnh: {last_err}")
        sys.exit(1)

    try:
        # Chỉnh ngày tự động
        report_date = args.date or data.get("report_date", datetime.today().strftime("%Y-%m-%d"))
        data = fix_dates(data, report_date)

        # Hiển thị bảng xác nhận
        print_preview_table(data)

        # Hỏi xác nhận nếu --preview
        if args.preview:
            answer = input("\n❓ Dữ liệu trông đúng chưa? Tiếp tục lưu? [y/N]: ").strip().lower()
            if answer not in ("y", "yes", "có", "co"):
                print("⛔ Hủy bỏ. Vui lòng chỉnh sửa ảnh hoặc nhập lại thủ công.")
                sys.exit(0)

        # Lưu JSON
        save_output(data, args.output)
        print(f"✅ Hoàn tất! Dữ liệu sẵn sàng để HisDiabetesOrchestrator.ps1 xử lý.")

    except Exception as e:
        print(f"❌ Lỗi xử lý dữ liệu: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
