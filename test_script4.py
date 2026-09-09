import urllib.request
import urllib.parse

def test_methods():
    url = "http://192.168.7.200:1401/api/Token/Login"
    
    # Test PUT
    try:
        req = urllib.request.Request(url, data=b'{"LoginName":"vmc","Password":"x"}', headers={'Content-Type': 'application/json'}, method='PUT')
        print("PUT Response:", urllib.request.urlopen(req, timeout=5).read().decode('utf-8'))
    except Exception as e:
        print("PUT Error:", e)

    # Test GET invalid JSON in param
    try:
        req = urllib.request.Request(f"{url}?param=NOT_JSON", method='GET')
        print("GET invalid param:", urllib.request.urlopen(req, timeout=5).read().decode('utf-8'))
    except Exception as e:
        print("GET Error:", e)

test_methods()
