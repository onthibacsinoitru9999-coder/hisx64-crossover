#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
===============================================================================
HIS EMERGENCY SURGERY REGISTRATION AUTOMATION (MỔ CẤP CỨU HÀ NỘI & NINH BÌNH)
===============================================================================
Tự động trích xuất thông tin bệnh nhân từ HIS và đăng ký lịch mổ cấp cứu
lên Google Forms theo đúng phân luồng cơ sở:
  - Hà Nội:      Form CTCH & Cột sống Khoa 57
  - Ninh Bình:   Form Ngoại tổng hợp Tầng 3 Nhà E (Khoa 915)
Bác sĩ chỉ định mặc định: Ths.BS Nguyễn Hữu Sâm (034727)
===============================================================================
"""

import sys
import os
import re
import json
import argparse
import subprocess
import urllib.parse
import urllib.request
import webbrowser

# Ensure UTF-8 output on Windows console
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

# Form definitions and field mappings
FORMS_CONFIG = {
    "NB": {
        "facility_name": "BỆNH VIỆN BẠCH MAI CƠ SỞ 2 - NINH BÌNH",
        "department_name": "Khoa Ngoại tổng hợp - Tầng 3 Nhà E (Khoa 915)",
        "view_url": "https://docs.google.com/forms/d/e/1FAIpQLScn9LfQxqVPL0A-uVcLRDFwTah6GpgKNDabhcONXycLJ8ALkQ/viewform",
        "post_url": "https://docs.google.com/forms/d/e/1FAIpQLScn9LfQxqVPL0A-uVcLRDFwTah6GpgKNDabhcONXycLJ8ALkQ/formResponse",
        "entries": {
            "name": "entry.744010330",
            "age": "entry.1326381732",
            "gender": "entry.1146288352",
            "patient_code": "entry.938074737",
            "room_bed": "entry.300014506",
            "diagnosis": "entry.732172205",
            "surgery": "entry.437609881",
            "doctor": "entry.961181856",
            "note": "entry.1922640834"
        }
    },
    "HN": {
        "facility_name": "BỆNH VIỆN BẠCH MAI - HÀ NỘI",
        "department_name": "Khoa Chấn thương Chỉnh hình & Cột sống (Khoa 57)",
        "view_url": "https://docs.google.com/forms/d/e/1FAIpQLScq1EcSA7Ff5mwU1GKQrC2h9jfFu-bObdeUKJNpeZIRrDoUEA/viewform",
        "post_url": "https://docs.google.com/forms/d/e/1FAIpQLScq1EcSA7Ff5mwU1GKQrC2h9jfFu-bObdeUKJNpeZIRrDoUEA/formResponse",
        "entries": {
            "position": "entry.1771260210",
            "department": "entry.1225676642",
            "name": "entry.1876536986",
            "age": "entry.2076581334",
            "gender": "entry.688804617",
            "patient_code": "entry.1034142534",
            "treatment_code": "entry.1407489499",
            "room_bed": "entry.1681626113",
            "diagnosis": "entry.1982187638",
            "surgery": "entry.1121377833",
            "urgency": "entry.777442342",
            "doctor": "entry.716253062",
            "surgeon": "entry.1993991632",
            "assistant": "entry.1769648167",
            "note": "entry.1348503941"
        }
    }
}

DEFAULT_DOCTOR = "Ths.BS Nguyễn Hữu Sâm (034727)"
DEFAULT_HN_POSITION = "Nội trú tại khoa CTCH & CS"
DEFAULT_HN_DEPT = "Khoa 57 - CTCH & Cột sống"
DEFAULT_HN_URGENCY = "Cấp cứu"

def detect_facility(cli_facility=None):
    """Detect current facility: CLI arg > env var > git branch > default HN."""
    if cli_facility:
        f = cli_facility.strip().upper()
        if f in ("NB", "NINH-BINH", "NINH_BINH", "CS2"):
            return "NB"
        return "HN"

    env_f = os.environ.get("HIS_FACILITY", "").strip().upper()
    if env_f in ("NB", "NINH-BINH", "NINH_BINH", "CS2"):
        return "NB"
    if env_f in ("HN", "HA-NOI", "HA_NOI", "CS1"):
        return "HN"

    # Try detecting git branch
    try:
        branch = subprocess.check_output(
            ["git", "branch", "--show-current"],
            stderr=subprocess.DEVNULL,
            universal_newlines=True
        ).strip().lower()
        if "ninh-binh" in branch or "nb" in branch:
            return "NB"
    except Exception:
        pass

    return "HN"

def lookup_patient_his(patient_code, facility="HN", script_dir=None):
    """Query patient demographics and diagnosis via HisClinicalCli.exe lookup."""
    info = {
        "name": "",
        "age": "",
        "gender": "Nam",
        "patient_code": patient_code,
        "treatment_code": "",
        "department": "",
        "room_bed": "",
        "diagnosis": ""
    }

    # Find HisClinicalCli.exe
    base_dirs = []
    if script_dir:
        base_dirs.append(os.path.abspath(os.path.join(script_dir, "..", "..", "..")))
    base_dirs.extend([os.getcwd(), "."])

    cli_exe = None
    for b in base_dirs:
        candidate = os.path.join(b, "HisClinicalCli.exe")
        if os.path.exists(candidate):
            cli_exe = candidate
            break

    if not cli_exe:
        return info

    env = os.environ.copy()
    env["HIS_FACILITY"] = facility
    root_dir = os.path.dirname(cli_exe)
    token_file = os.path.join(root_dir, "doctor_nb.token" if facility == "NB" else "doctor_hn.token")
    env["HIS_TOKEN_FILE"] = token_file
    if "HIS_DOCTOR_LOGIN" not in env:
        env["HIS_DOCTOR_LOGIN"] = "034727"
    if "HIS_PASSWORD" not in env:
        env["HIS_PASSWORD"] = "981"

    try:
        proc = subprocess.run(
            [cli_exe, "lookup", patient_code],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            universal_newlines=True,
            encoding="utf-8",
            errors="replace",
            env=env,
            timeout=15,
            cwd=root_dir
        )
        output = proc.stdout

        # Parse Name & Age & Gender
        # Pattern: BN: TRẦN HẢI DƯƠNG  (30 tuổi (Sinh năm: 1996) - Nam)
        m_name = re.search(r"THÔNG TIN BỆNH NHÂN:\s*([^\(]+)\s*\((?:(\d+)\s*tuổi)?(?:\s*\(Sinh năm:\s*(\d{4})\))?\s*-\s*(Nam|Nữ|Nu)", output, re.IGNORECASE)
        if m_name:
            info["name"] = m_name.group(1).strip()
            if m_name.group(2):
                info["age"] = m_name.group(2).strip()
            elif m_name.group(3):
                # Calculate age from birth year
                import datetime
                info["age"] = str(datetime.datetime.now().year - int(m_name.group(3)))
            g = m_name.group(4).strip()
            info["gender"] = "Nữ" if g.lower() in ("nữ", "nu") else "Nam"

        # Parse Mã BN & Mã ĐT
        m_bn = re.search(r"Mã BN:\s*(\d+)", output)
        if m_bn:
            info["patient_code"] = m_bn.group(1).strip()

        m_dt = re.search(r"Mã ĐT:\s*(\d+)", output)
        if m_dt:
            info["treatment_code"] = m_dt.group(1).strip()

        # Parse Khoa & Buồng/Giường
        m_kb = re.search(r"Khoa:\s*([^\|\r\n]+)\s*\|\s*Buồng/Giường:\s*([^\r\n]+)", output)
        if m_kb:
            info["department"] = m_kb.group(1).strip()
            info["room_bed"] = m_kb.group(2).strip()

        # Parse Chẩn đoán ICD
        m_cd = re.search(r"Chẩn đoán ICD:\s*([^\r\n]+)", output)
        if m_cd:
            diag = m_cd.group(1).strip()
            # Clean up trailing (Chi tiết: )
            diag = re.sub(r"\s*\(Chi tiết:\s*\)\s*$", "", diag)
            info["diagnosis"] = diag.strip()

    except Exception as ex:
        # Fallback gracefully
        pass

    return info

def build_payload(facility, data):
    """Build URL-encoded payload matching Google Form entry IDs."""
    cfg = FORMS_CONFIG[facility]
    entries = cfg["entries"]
    payload = {}

    if facility == "NB":
        payload[entries["name"]] = data.get("name", "")
        payload[entries["age"]] = str(data.get("age", ""))
        payload[entries["gender"]] = data.get("gender", "Nam")
        payload[entries["patient_code"]] = data.get("patient_code", "")
        payload[entries["room_bed"]] = data.get("room_bed", "")
        payload[entries["diagnosis"]] = data.get("diagnosis", "")
        payload[entries["surgery"]] = data.get("surgery", "")
        payload[entries["doctor"]] = data.get("doctor", DEFAULT_DOCTOR)
        if data.get("note"):
            payload[entries["note"]] = data.get("note")

    elif facility == "HN":
        payload[entries["position"]] = data.get("position", DEFAULT_HN_POSITION)
        if data.get("department"):
            payload[entries["department"]] = data.get("department")
        else:
            payload[entries["department"]] = DEFAULT_HN_DEPT
        payload[entries["name"]] = data.get("name", "")
        payload[entries["age"]] = str(data.get("age", ""))
        payload[entries["gender"]] = data.get("gender", "Nam")
        payload[entries["patient_code"]] = data.get("patient_code", "")
        payload[entries["treatment_code"]] = data.get("treatment_code", "")
        
        # Room / Bed format for HN: Khoa 57 / Phòng ... / Giường ...
        room_bed = data.get("room_bed", "")
        if room_bed and not room_bed.lower().startswith("khoa"):
            room_bed = f"Khoa 57 / {room_bed}"
        payload[entries["room_bed"]] = room_bed

        payload[entries["diagnosis"]] = data.get("diagnosis", "")
        payload[entries["surgery"]] = data.get("surgery", "")
        payload[entries["urgency"]] = data.get("urgency", DEFAULT_HN_URGENCY)
        payload[entries["doctor"]] = data.get("doctor", DEFAULT_DOCTOR)
        if data.get("surgeon"):
            payload[entries["surgeon"]] = data.get("surgeon")
        if data.get("assistant"):
            payload[entries["assistant"]] = data.get("assistant")
        if data.get("note"):
            payload[entries["note"]] = data.get("note")

    return payload

def generate_prefilled_url(facility, payload):
    """Generate clickable 1-click pre-filled Google Form URL."""
    cfg = FORMS_CONFIG[facility]
    base_url = cfg["view_url"]
    query = urllib.parse.urlencode(payload)
    return f"{base_url}?usp=pp_url&{query}"

def submit_form(facility, payload):
    """Submit form via HTTP POST to formResponse."""
    cfg = FORMS_CONFIG[facility]
    post_url = cfg["post_url"]
    form_data = urllib.parse.urlencode(payload).encode("utf-8")
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Content-Type": "application/x-www-form-urlencoded"
    }

    req = urllib.request.Request(post_url, data=form_data, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            status = resp.status
            return status in (200, 302, 303), f"HTTP {status}"
    except urllib.error.HTTPError as e:
        # Google forms sometimes redirects or returns 200/302
        if e.code in (200, 302, 303):
            return True, f"HTTP {e.code}"
        return False, f"HTTP Error {e.code}: {e.reason}"
    except Exception as ex:
        return False, str(ex)

def main():
    parser = argparse.ArgumentParser(description="HIS Emergency Surgery Registration Automation (Dual-Facility: HN & NB)")
    parser.add_argument("patient", nargs="?", default="", help="Mã bệnh nhân hoặc mã điều trị (VD: 0004060486)")
    parser.add_argument("surgery", nargs="?", default="", help="Cách thức mổ dự kiến (VD: 'KHX xương đòn phải')")
    parser.add_argument("facility", nargs="?", default="", help="Cơ sở: 'HN' hoặc 'NB'")
    
    parser.add_argument("--facility", dest="opt_facility", help="Cơ sở: 'HN' hoặc 'NB'")
    parser.add_argument("--patient", dest="opt_patient", help="Mã bệnh nhân")
    parser.add_argument("--surgery", dest="opt_surgery", help="Cách thức mổ dự kiến")
    parser.add_argument("--name", help="Họ và tên bệnh nhân")
    parser.add_argument("--age", type=int, help="Tuổi bệnh nhân")
    parser.add_argument("--gender", choices=["Nam", "Nữ", "Nu"], default=None, help="Giới tính")
    parser.add_argument("--room", help="Buồng/Giường (VD: 'Phòng 733 - Giường số 85')")
    parser.add_argument("--treatment", help="Mã điều trị (bắt buộc với HN)")
    parser.add_argument("--dept", help="Khoa đang điều trị (cho HN)")
    parser.add_argument("--position", help="Vị trí bệnh nhân (cho HN)")
    parser.add_argument("--diagnosis", help="Chẩn đoán xác định/sơ bộ")
    parser.add_argument("--urgency", help="Phân loại cấp cứu (cho HN: 'Cấp cứu', 'Cấp cứu (nặng)', 'Cấp cứu có trì hoãn', 'Mổ phiên hữu trùng')")
    parser.add_argument("--doctor", default=DEFAULT_DOCTOR, help="Bác sĩ chỉ định")
    parser.add_argument("--surgeon", help="Phẫu thuật viên chính (cho HN)")
    parser.add_argument("--assistant", help="Phẫu thuật viên phụ (cho HN)")
    parser.add_argument("--note", help="Ghi chú lâm sàng / dặn dò")
    
    parser.add_argument("--submit", action="store_true", help="Gửi trực tiếp lên Google Form (POST formResponse)")
    parser.add_argument("--dry-run", action="store_true", help="Chỉ kiểm tra và in link prefilled, không gửi")
    parser.add_argument("--open-browser", action="store_true", help="Mở link prefilled trên trình duyệt")
    parser.add_argument("--json", action="store_true", help="Xuất kết quả định dạng JSON")

    args = parser.parse_args()

    patient_code = (args.opt_patient or args.patient).strip()
    surgery_text = (args.opt_surgery or args.surgery).strip()
    facility_input = (args.opt_facility or args.facility).strip()

    facility = detect_facility(facility_input)
    script_dir = os.path.dirname(os.path.abspath(__file__))

    # Patient data dict
    data = {
        "patient_code": patient_code,
        "surgery": surgery_text,
        "doctor": args.doctor or DEFAULT_DOCTOR,
        "facility": facility
    }

    # If patient code is provided, auto-lookup info from HIS
    if patient_code:
        lookup_data = lookup_patient_his(patient_code, facility, script_dir)
        for k, v in lookup_data.items():
            if v:
                data[k] = v

    # User explicitly provided flags override lookup data
    if args.name: data["name"] = args.name.strip()
    if args.age is not None: data["age"] = args.age
    if args.gender: data["gender"] = "Nữ" if args.gender.lower() in ("nữ", "nu") else "Nam"
    if args.room: data["room_bed"] = args.room.strip()
    if args.treatment: data["treatment_code"] = args.treatment.strip()
    if args.dept: data["department"] = args.dept.strip()
    if args.position: data["position"] = args.position.strip()
    if args.diagnosis: data["diagnosis"] = args.diagnosis.strip()
    if args.urgency: data["urgency"] = args.urgency.strip()
    if args.surgeon: data["surgeon"] = args.surgeon.strip()
    if args.assistant: data["assistant"] = args.assistant.strip()
    if args.note: data["note"] = args.note.strip()
    if surgery_text: data["surgery"] = surgery_text

    # Validation
    errors = []
    if not data.get("name"):
        errors.append("Thiếu thông tin 'Họ và tên' bệnh nhân.")
    if not data.get("patient_code"):
        errors.append("Thiếu 'Mã bệnh nhân'.")
    if not data.get("surgery"):
        errors.append("Thiếu 'Cách thức mổ dự kiến'.")
    if facility == "HN" and not data.get("treatment_code"):
        # If treatment code missing in HN, try using patient code or placeholder
        if data.get("patient_code"):
            data["treatment_code"] = data["patient_code"]

    # Build payload and prefilled URL
    payload = build_payload(facility, data)
    prefilled_url = generate_prefilled_url(facility, payload)

    submitted = False
    submit_msg = "Chưa gửi (chế độ kiểm tra / pre-fill)"
    if args.submit and not args.dry_run and not errors:
        success, msg = submit_form(facility, payload)
        submitted = success
        submit_msg = "ĐÃ GỬI THÀNH CÔNG VÀO DANH SÁCH MỔ CẤP CỨU (" + msg + ")" if success else "GỬI THẤT BẠI (" + msg + ")"

    if args.open_browser:
        try:
            webbrowser.open(prefilled_url)
        except Exception:
            pass

    # JSON Output
    if args.json:
        res = {
            "success": submitted or (not args.submit and len(errors) == 0),
            "facility": facility,
            "facility_name": FORMS_CONFIG[facility]["facility_name"],
            "submitted": submitted,
            "submit_message": submit_msg,
            "prefilled_url": prefilled_url,
            "errors": errors,
            "patient_data": data,
            "form_payload": payload
        }
        print(json.dumps(res, ensure_ascii=False, indent=2))
        return

    # Beautiful Human/Doctor Output
    cfg = FORMS_CONFIG[facility]
    print("=" * 79)
    print(f"🚨 [ĐĂNG KÝ DANH SÁCH MỔ CẤP CỨU - {cfg['facility_name']}]")
    print(f"• Khoa: {cfg['department_name']}")
    print(f"• Bác sĩ chỉ định: {data.get('doctor', DEFAULT_DOCTOR)}")
    print("-" * 79)
    print(f"👤 BỆNH NHÂN:   {data.get('name', '---')} ({data.get('age', '---')} tuổi - {data.get('gender', '---')})")
    print(f"🆔 MÃ BN:       {data.get('patient_code', '---')} | MÃ ĐT: {data.get('treatment_code', '---')}")
    print(f"🛏️ BUỒNG/GIƯỜNG: {data.get('room_bed', '---')}")
    print(f"🩺 CHẨN ĐOÁN:   {data.get('diagnosis', '---')}")
    print(f"🔪 CÁCH THỨC MỔ: {data.get('surgery', '---')}")
    if facility == "HN":
        print(f"⚡ PHÂN LOẠI:   {data.get('urgency', DEFAULT_HN_URGENCY)} | VỊ TRÍ: {data.get('position', DEFAULT_HN_POSITION)}")
        if data.get("surgeon"):
            print(f"👨‍⚕️ PTV CHÍNH:   {data.get('surgeon')}")
        if data.get("assistant"):
            print(f"👨‍⚕️ PTV PHỤ:     {data.get('assistant')}")
    if data.get("note"):
        print(f"📝 GHI CHÚ:     {data.get('note')}")
    print("-" * 79)
    
    if errors:
        print("❌ CẢNH BÁO / THIẾU THÔNG TIN BẮT BUỘC:")
        for err in errors:
            print(f"   - {err}")
        print("-" * 79)

    print(f"🌐 LINK XEM & ĐIỀN FORM 1-CLICK (PRE-FILLED URL):")
    print(f"{prefilled_url}")
    print("-" * 79)
    print(f"📊 TRẠNG THÁI GỬI: {submit_msg}")
    print("=" * 79)

if __name__ == "__main__":
    main()
