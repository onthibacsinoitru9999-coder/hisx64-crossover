import json
import urllib.request
import urllib.error
import hashlib

url = "http://192.168.7.200:1401/api/Token/Login"
password = "789789"
password_md5 = hashlib.md5(password.encode()).hexdigest().lower()

test_bodies = [
    {"LoginName": "vmc", "Password": password_md5},
    {"loginName": "vmc", "password": password_md5},
    {"param": {"LoginName": "vmc", "Password": password_md5}},
    {"param": {"loginName": "vmc", "password": password_md5}},
    {"loginReq": {"loginName": "vmc", "password": password_md5}},
    {"param": {"LoginName": "vmc", "Password": password_md5, "ApplicationCode": "HIS"}},
    {"loginName": "vmc", "password": password_md5, "applicationCode": "HIS"},
    # With CommonParam?
    {"loginName": "vmc", "password": password_md5, "commonParam": {"ApplicationCode": "HIS"}},
    # Or param as string
    {"param": json.dumps({"loginName": "vmc", "password": password_md5})}
]

for body in test_bodies:
    print("\nTesting POST body:", body)
    req = urllib.request.Request(url, data=json.dumps(body).encode('utf-8'), headers={'Content-Type': 'application/json'}, method='POST')
    try:
        res = urllib.request.urlopen(req, timeout=5)
        print("Success:", res.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        print(f"HTTPError: {e.code} Message: {e.read().decode('utf-8')}")
    except Exception as e:
        print(f"Error: {e}")
