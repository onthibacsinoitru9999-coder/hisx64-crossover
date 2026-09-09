import json
import urllib.request
import urllib.error

# Thông tin đăng nhập của bạn (Bạn cần thay đổi 2 dòng này)
USERNAME = "vmc"
PASSWORD = "789789"

def login_and_get_token():
    login_url = "http://192.168.7.200:1401/api/Token/Login"
    # Cấu trúc payload này là phỏng đoán theo chuẩn Inventec, nếu báo lỗi bạn báo lại nhé
    payload = {
        "LoginName": USERNAME,
        "Password": PASSWORD
    }
    
    req = urllib.request.Request(login_url, data=json.dumps(payload).encode('utf-8'), headers={'Content-Type': 'application/json'}, method='POST')
    try:
        res = urllib.request.urlopen(req, timeout=10)
        data = json.loads(res.read().decode('utf-8'))
        
        if data.get("Success"):
            print("=> Đăng nhập thành công!")
            # Tùy phiên bản HIS, token có thể nằm trong Data hoặc Token
            return data.get("Data", "")
        else:
            print("=> Đăng nhập thất bại. Máy chủ báo lỗi:", data)
            return None
    except urllib.error.HTTPError as e:
        print("=> Lỗi mạng khi đăng nhập:", e.code, e.read().decode('utf-8'))
        return None
    except Exception as e:
        print("=> Lỗi khác khi đăng nhập:", e)
        return None

def get_patients_in_room(token, room_code):
    # V_HIS_PATIENT hoặc V_HIS_TREATMENT tuỳ luồng khám
    mos_url = f"http://192.168.7.236:1608/api/V_HIS_TREATMENT/Get?ExecuteRoomCode={room_code}"
    
    req = urllib.request.Request(mos_url, headers={
        'Content-Type': 'application/json',
        'Authorization': f'Bearer {token}' # hoặc chuỗi token cụ thể
    }, method='GET')
    
    try:
        res = urllib.request.urlopen(req, timeout=10)
        data = json.loads(res.read().decode('utf-8'))
        
        if data.get("Success"):
            ds_benh_nhan = data.get("Data", [])
            print(f"=> Tìm thấy {len(ds_benh_nhan)} bệnh nhân trong phòng {room_code}:")
            for bn in ds_benh_nhan:
                print(f" - Tên: {bn.get('TDL_PATIENT_NAME')} | Mã: {bn.get('TDL_PATIENT_CODE')}")
        else:
            print("=> Lỗi lấy danh sách bệnh nhân:", data)
    except Exception as e:
        print("=> Gặp lỗi lấy dữ liệu:", e)

# Chạy thử
if __name__ == "__main__":
    print("Bắt đầu thử kết nối...")
    token = login_and_get_token()
    if token:
        # Gọi lấy danh sách phòng nqctchbb716
        get_patients_in_room(token, "nqctchbb716")
    else:
        print("Không thể lấy token, dừng.")
