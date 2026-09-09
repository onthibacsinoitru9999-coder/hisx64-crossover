import json
import urllib.request
import urllib.parse
import urllib.error

url = "http://192.168.7.200:1401/api/Token/Login"

def test_get(params):
    query = urllib.parse.urlencode(params)
    req_url = f"{url}?{query}"
    print(f"Testing URL: {req_url}")
    req = urllib.request.Request(req_url, method='GET')
    try:
        res = urllib.request.urlopen(req, timeout=5)
        print("Success:", res.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        print(f"HTTPError: {e.code}")
    except Exception as e:
        print(f"Error: {e}")

test_permutations = [
    {"LoginName": "vmc", "Password": "x", "ApplicationCode": "HIS"},
    {"loginName": "vmc", "password": "x", "applicationCode": "HIS"},
    {"username": "vmc", "password": "x"},
    {"loginname": "vmc", "password": "x"},
    {"appCode": "HIS", "LoginName": "vmc", "Password": "x"},
    {"param": json.dumps({"LoginName": "vmc", "Password": "x"})},
    {"param": json.dumps({"loginName": "vmc", "password": "x"})}
]

for p in test_permutations:
    test_get(p)
