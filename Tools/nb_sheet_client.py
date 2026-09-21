# -*- coding: utf-8 -*-
"""
Google Sheet Ninh Binh Client & Patient Cross-Reference Tool
URL: https://docs.google.com/spreadsheets/d/1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM/edit?usp=drivesdk
Tab: DANH SÁCH BN (gid=914015390)
"""
import urllib.request
import csv
import io
import sys
import os

sys.stdout.reconfigure(encoding='utf-8')

SHEET_ID = "1m9VoSwkHVwKpqI9ucIMoMm_ulE77Ab6pf0wXJZ-HPEM"
GID_DANH_SACH_BN = "914015390"
CSV_URL = f"https://docs.google.com/spreadsheets/d/{SHEET_ID}/export?format=csv&gid={GID_DANH_SACH_BN}"

def fetch_patients():
    req = urllib.request.Request(CSV_URL, headers={'User-Agent': 'Mozilla/5.0'})
    content = urllib.request.urlopen(req, timeout=10).read().decode('utf-8')
    reader = csv.reader(io.StringIO(content))
    rows = list(reader)
    if not rows:
        return []
    
    header = rows[0]
    patients = []
    for r in rows[1:]:
        if not any(r):
            continue
        # Standard columns: STT, Mã BN, Họ và tên, Tuổi, Giới, Phòng, Giường, Chẩn đoán chính
        p = {
            "stt": r[0].strip() if len(r) > 0 else "",
            "patient_code": r[1].strip() if len(r) > 1 else "",
            "name": r[2].strip() if len(r) > 2 else "",
            "age": r[3].strip() if len(r) > 3 else "",
            "gender": r[4].strip() if len(r) > 4 else "",
            "room": r[5].strip() if len(r) > 5 else "",
            "bed": r[6].strip() if len(r) > 6 else "",
            "diagnosis": r[7].strip() if len(r) > 7 else ""
        }
        if p["name"] or p["patient_code"]:
            patients.append(p)
    return patients

def remove_diacritics(text):
    import unicodedata
    if not text:
        return ""
    text = unicodedata.normalize('NFD', text)
    text = ''.join(c for c in text if unicodedata.category(c) != 'Mn')
    return unicodedata.normalize('NFC', text).lower().replace('đ', 'd')

def search_patients(keyword):
    pts = fetch_patients()
    if not keyword:
        return pts
    
    kw_clean = remove_diacritics(keyword.strip())
    matches = []
    for p in pts:
        name_clean = remove_diacritics(p["name"])
        pcode = p["patient_code"]
        room = p["room"].lower()
        diag = remove_diacritics(p["diagnosis"])
        
        if (kw_clean in name_clean or 
            keyword.strip() in pcode or 
            kw_clean in room or 
            kw_clean in diag):
            matches.append(p)
    return matches

def print_table(pts):
    if not pts:
        print("❌ Không tìm thấy bệnh nhân nào khớp với yêu cầu trong Google Sheet Ninh Bình.")
        return
    
    print("=" * 110)
    print(f"📋 DANH SÁCH BỆNH NHÂN NINH BÌNH (GOOGLE SHEET: {len(pts)} BN)")
    print("=" * 110)
    print(f"{'STT':<4} | {'Mã BN':<10} | {'Họ và tên':<24} | {'Tuổi':<4} | {'Giới':<4} | {'Phòng':<7} | {'Giường':<6} | {'Chẩn đoán chính'}")
    print("-" * 110)
    for p in pts:
        print(f"{p['stt']:<4} | {p['patient_code']:<10} | {p['name']:<24} | {p['age']:<4} | {p['gender']:<4} | {p['room']:<7} | {p['bed']:<6} | {p['diagnosis'][:38]}")
    print("=" * 110)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        pts = fetch_patients()
        print_table(pts)
    else:
        cmd = sys.argv[1].lower()
        if cmd == "list":
            pts = fetch_patients()
            print_table(pts)
        elif cmd == "lookup" or cmd == "search":
            kw = " ".join(sys.argv[2:]) if len(sys.argv) > 2 else ""
            matches = search_patients(kw)
            print_table(matches)
        else:
            kw = " ".join(sys.argv[1:])
            matches = search_patients(kw)
            print_table(matches)
