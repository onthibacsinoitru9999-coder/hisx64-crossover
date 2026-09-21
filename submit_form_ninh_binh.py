# -*- coding: utf-8 -*-
import urllib.request
import urllib.parse
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')

post_url = "https://docs.google.com/forms/d/e/1FAIpQLSeWar3VqZElU1unVs_JQL68BYUi_R7zgGJ8RYhWzi-OInMO2A/formResponse"
base_url = "https://docs.google.com/forms/d/e/1FAIpQLSeWar3VqZElU1unVs_JQL68BYUi_R7zgGJ8RYhWzi-OInMO2A/viewform"

patients = [
    {
        "name": "NGÔ THỊ LIÊN",
        "age": "68",
        "gender": "Nữ",
        "patient_code": "0004064449",
        "treatment_code": "000007285706",
        "room": "3E-22",
        "bed": "G58",
        "diagnosis": "Xẹp cấp L2 do chấn thương / Tăng huyết áp",
        "doctor_receive": "Ths.BS Nguyễn Hữu Sâm (034727)",
        "doctor_treat": "Ths.BS Nguyễn Hữu Sâm (034727)",
        "surgery_method": "Bơm xi măng sinh học thân đốt sống L2"
    },
    {
        "name": "LÊ THỊ HẢO",
        "age": "36",
        "gender": "Nữ",
        "patient_code": "0004065015",
        "treatment_code": "000007286480",
        "room": "3E-33",
        "bed": "G73",
        "diagnosis": "Gãy xương đòn trái - TD trật khớp vai trái",
        "doctor_receive": "Ths.BS Nguyễn Hữu Sâm (034727)",
        "doctor_treat": "Ths.BS Nguyễn Hữu Sâm (034727)",
        "surgery_method": "Phẫu thuật kết hợp xương đòn trái bằng nẹp vít"
    }
]

print("=" * 80)
print("🚀 BẮT ĐẦU ĐIỀN GOOGLE FORM DANH SÁCH KHOA CƠ SỞ NINH BÌNH")
print("=" * 80)

results = []

for idx, p in enumerate(patients, 1):
    data = {
        'entry.1361290475': p['name'],
        'entry.1502367240': p['age'],
        'entry.187878844':  p['gender'],
        'entry.762893400':  p['patient_code'],
        'entry.303873608':  p['room'],
        'entry.1174947570': p['bed'],
        'entry.335191251':  p['diagnosis'],
        'entry.1996467510': p['doctor_receive'],
        'entry.359254334':  p['doctor_treat'],
        'entry.1615445702': p['treatment_code'],
        'entry.402080490':  p['surgery_method']
    }
    
    # Generate prefilled link for reference
    prefilled_link = base_url + "?" + urllib.parse.urlencode({f"usp": "pp_url", **data})
    
    encoded_data = urllib.parse.urlencode(data).encode('utf-8')
    req = urllib.request.Request(
        post_url, 
        data=encoded_data, 
        headers={
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
            'Content-Type': 'application/x-www-form-urlencoded'
        }
    )
    
    print(f"\n👉 [{idx}/2] Đang nạp bệnh nhân: {p['name']} (Mã BN: {p['patient_code']} | Buồng: {p['room']} - {p['bed']})...")
    
    try:
        with urllib.request.urlopen(req) as resp:
            status = resp.status
            if status == 200:
                print(f"   ✔ NẠP THÀNH CÔNG (HTTP {status})!")
                results.append({
                    "patient": p,
                    "status": "SUCCESS",
                    "code": status,
                    "link": prefilled_link
                })
            else:
                print(f"   ⚠️ HTTP Status: {status}")
                results.append({
                    "patient": p,
                    "status": "WARNING",
                    "code": status,
                    "link": prefilled_link
                })
    except Exception as ex:
        print(f"   ❌ Lỗi khi nạp: {ex}")
        results.append({
            "patient": p,
            "status": "ERROR",
            "error": str(ex),
            "link": prefilled_link
        })
    time.sleep(1.0)

print("\n" + "=" * 80)
print("📊 BÁO CÁO KẾT QUẢ ĐIỀN FORM:")
for r in results:
    p = r['patient']
    print(f"• {p['name']}: {r['status']} (HTTP {r.get('code', 'N/A')}) | Buồng {p['room']} | Giường {p['bed']}")
print("=" * 80)
