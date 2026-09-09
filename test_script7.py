import urllib.request
import urllib.parse
import hashlib

url = "http://192.168.7.200:1401/api/Token/Login"

password = "789789"
pwd_md5 = hashlib.md5(password.encode()).hexdigest().lower()

data = {
    "loginName": "vmc",
    "password": pwd_md5,
    "applicationCode": "HIS"
}

encoded_data = urllib.parse.urlencode(data).encode('utf-8')
req = urllib.request.Request(url, data=encoded_data, headers={'Content-Type': 'application/x-www-form-urlencoded'}, method='POST')

try:
    res = urllib.request.urlopen(req, timeout=5)
    print("Success:", res.read().decode('utf-8'))
except urllib.error.HTTPError as e:
    print(f"HTTPError: {e.code} Message: {e.read().decode('utf-8')}")
except Exception as e:
    print(f"Error: {e}")
