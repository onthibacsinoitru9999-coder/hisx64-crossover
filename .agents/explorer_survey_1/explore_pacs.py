import sys
import urllib.request
sys.stdout.reconfigure(encoding='utf-8')

def inspect_download_func():
    url = 'http://192.168.200.107:8080/pacs/modules/pacsui/study/public/babelLoader?path=autoload.json'
    data = urllib.request.urlopen(url).read().decode('utf-8', errors='ignore')
    print(data[16850:17800])

if __name__ == '__main__':
    inspect_download_func()
