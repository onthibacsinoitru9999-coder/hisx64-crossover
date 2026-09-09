import json
import urllib.request
import urllib.parse
import hashlib

url = "http://192.168.7.200:1401/api/Token/Login"

def test_get(params):
    query = urllib.parse.urlencode(params)
    req_url = f"{url}?{query}"
    print(f"Testing URL: {req_url}")
    req = urllib.request.Request(req_url, method='GET')
    try:
        res = urllib.request.urlopen(req, timeout=5)
        print("Success:", res.read().decode('utf-8'))
    except Exception as e:
        print(f"Error: {e}")

password = "789789"
password_md5_lower = hashlib.md5(password.encode()).hexdigest().lower()
password_md5_upper = hashlib.md5(password.encode()).hexdigest().upper()

test_permutations = [
    # standard params
    {"loginname": "vmc", "password": password_md5_lower},
    {"LoginName": "vmc", "Password": password_md5_lower},
    # upper case hash
    {"loginname": "vmc", "password": password_md5_upper},
    {"LoginName": "vmc", "Password": password_md5_upper},
    # with param JSON
    {"param": json.dumps({"LoginName": "vmc", "Password": password_md5_lower})},
    {"param": json.dumps({"loginName": "vmc", "password": password_md5_lower})},
]

for p in test_permutations:
    test_get(p)
