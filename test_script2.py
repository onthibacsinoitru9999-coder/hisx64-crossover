import json
import urllib.request
import urllib.error
import urllib.parse

urls_to_test = [
    ("http://192.168.7.200:1401/api/Token/Login", "GET", None),
    ("http://192.168.7.200:1401/api/Token/Login", "GET", {"LoginName": "vmc", "Password": "789789"}),
    ("http://192.168.7.200:1401/Token", "POST", {"grant_type": "password", "username": "vmc", "password": "789789"}),
    ("http://192.168.7.200:1401/api/Token", "POST", {"grant_type": "password", "username": "vmc", "password": "789789"}),
]

for url, method, data in urls_to_test:
    print(f"\n--- Testing {method} to {url} with data {data}")
    try:
        if method == "GET":
            if data:
                query = urllib.parse.urlencode(data)
                req_url = f"{url}?{query}"
            else:
                req_url = url
            req = urllib.request.Request(req_url, method='GET')
        else:
            encoded_data = urllib.parse.urlencode(data).encode('utf-8')
            req = urllib.request.Request(url, data=encoded_data, headers={'Content-Type': 'application/x-www-form-urlencoded'}, method='POST')
        
        res = urllib.request.urlopen(req, timeout=5)
        print("Success:", res.read().decode('utf-8'))
    except urllib.error.HTTPError as e:
        print(f"HTTPError: {e.code} {e.reason}")
        try:
            print("Response:", e.read().decode('utf-8'))
        except:
            pass
    except Exception as e:
        print(f"Error: {e}")
