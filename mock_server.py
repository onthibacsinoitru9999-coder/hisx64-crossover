import socket

def start_server():
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('127.0.0.1', 8080))
    server_socket.listen(1)
    print("Listening on port 8080...")
    
    server_socket.settimeout(15) # 15 seconds timeout
    try:
        conn, addr = server_socket.accept()
        print(f"Connection from {addr}")
        
        request = conn.recv(4096)
        print("--- HTTP Request ---")
        print(request.decode('utf-8'))
        print("--------------------")
        
        response = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n"
        conn.sendall(response.encode('utf-8'))
        conn.close()
    except socket.timeout:
        print("Timeout waiting for connection.")
    finally:
        server_socket.close()

if __name__ == "__main__":
    start_server()
