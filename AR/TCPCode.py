import sys
from threading import Thread
import cv2
import numpy as np
import base64

import json

import socket
import time
from turtle import st


from time import sleep
from array import array



print("코드실행시작")


########################Face####################

'''
print("Pleas Enter your IP: 빈:0, 한섭:1, 기타:2")

mode = int(input("컴퓨터 번호:"))

if mode == 0:
    IP = "161.122.33.97"
elif mode == 1:    
    IP =  "161.122.33.68" #"172.30.144.1"
else:
    IP = input("Please enter ip: xxx.xxx.xx.x")

# "192.168.137.1" #한섭 주소, "192.168.1.157"
# "161.122.33.97" #빈 주소
'''

# IP = "192.168.1.74" #컴퓨터에 따라 수정
# IP = "192.0.0.2"
IP = "192.168.0.185"
# IP = "192.168.0.117"
PORT = 710


print("TCP target IP:", IP)
print("TCP target port:", PORT)

sock = socket.socket(socket.AF_INET,  # Internet
                     socket.SOCK_STREAM)  # TCP
#socket.SOCK_DGRAM) # UDP
#SOCK_STREAM
sock.connect((IP, PORT))

ConnectedResult = {
    'res': 200,
    'message': 'python server 연결됨',
    'commandIndex' : 200
}

# sock.sendto(bytes([198]), (IP, PORT))  # 연결되었을 때 보내는 값.

sock.sendto(json.dumps(ConnectedResult, ensure_ascii=False).encode('utf8'), (IP, PORT))  # 연결되었을 때 보내는 값.

class App(object):
    def __init__(self):
        pass
    
    def loop(self):
    
        time.sleep(2)

        while True:
            try:
                
                TestMessage = {
                    "res": 100,
                    "message": "python 에서 보내는 테스트 메시지",
                    "commandIndex" : 603
                }
                # unity -> python test
                sock.sendto(json.dumps(TestMessage, ensure_ascii=False).encode('utf8'), (IP, PORT))
                time.sleep(60)
            except:
                pass


    
    def rcvMsg(self, sock):
        """
            unity -> python 데이터 값 받는 것 기다리는 함수 .
            thread 로 돌고있음.
        """
        print("유니티로부터 데이터 들어오는 것 대기하는 스레드 작동! CTRL + C 종료 ")
        
        while True:
            
            try:    
                
                checkPoint = sock.recv(300).decode('utf-8')
                json_data = json.loads(checkPoint)

                if json_data['commandIndex'] == 2:

                    print(" 예상되는 데이터 길이 : " + json_data['message'] )

                    while True:
                        # full_data = b''  # 바이트 형식으로 데이터를 받습니다.

                        expected_length = int(json_data['message'])

                        print(f"예상되는 데이터 길이: {expected_length}")

                        # 두 번째 메시지: 실제 데이터 수신
                        full_data = b''
                        while len(full_data) < expected_length:
                            print("기다리는 중")
                            # 데이터를 수신하여 full_data에 추가합니다.

                            part_data = sock.recv(1024)
                            if not part_data:
                                break
                            full_data += part_data
                            print(f"수신 중... 현재 길이: {len(full_data)}")


                        # 데이터를 문자열로 변환
                        image_data = json.loads(full_data.decode('utf-8'))
                        print(" ar you here? ")
                        res = image_data['res']
                        message = image_data['message']
                        buffer = base64.b64decode(image_data['data'])

                        # commandIndex = json_data['commandIndex']

                        if res == 147:
                            print("그림을 받음")

                            print(buffer)

                            print(message)

                            image_np = np.frombuffer(buffer, dtype=np.uint8)
                            image = cv2.imdecode(image_np, cv2.IMREAD_COLOR)
                            
                            if image is None:
                                print("이미지 디코딩 실패")
                            else:
                                cv2.imshow('Received Image', image)
                                cv2.waitKey(0)
                                cv2.destroyAllWindows()
                        else:
                            print("모름")

            except json.JSONDecodeError as e:
                print(f"JSON 파싱 오류: {e}")
            except:
                    pass

    def TreadMain(self):
        try:
            thread = Thread(target=self.rcvMsg, args=(sock,))
            thread.daemon = True  # let the parent kill the child thread at exit
            thread.start()
            
            # thread2 = Thread(target=self.loop, args=())
            # thread2.daemon = True  # let the parent kill the child thread at exit
            # thread2.start()


            while thread.is_alive():
                thread.join(1)  # time out not to block KeyboardInterrupt


        except KeyboardInterrupt:
            # print "Ctrl+C pressed..."
            ThreadMessage = {
                "res": 0,
                "message": "Python server terminated",
                "commandIndex" : 199 # 다른 값?
                }
            
            sock.sendto(json.dumps(ThreadMessage, ensure_ascii=False).encode('utf8'), (IP, PORT))

            print("Ctrl+C pressed...")

            sys.exit(1)    
            
if __name__ == '__main__':
    app = App()
    app.TreadMain()        
    