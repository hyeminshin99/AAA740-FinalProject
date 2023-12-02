import sys
from threading import Thread
#from SpeakerDetection import *

import json

import socket
import time
from turtle import st

################# Face ###############

import cv2
from time import sleep
from array import array

preVirtualHuman = 0
VirtualHuman = 0 #0: idel, 1: see participant left, 2: see participant right
y_pos = 0

Left_P = 0 # 안보면 0, 보면 1
Right_P = 0 #안보면 0, 보면 1


######## Facial Landmark  ########
ALL = list(range(0, 68))
RIGHT_EYEBROW = list(range(17, 22))
LEFT_EYEBROW = list(range(22, 27))
RIGHT_EYE = list(range(36, 42))
LEFT_EYE = list(range(42, 48))
NOSE = list(range(27, 36))
MOUTH_OUTLINE = list(range(48, 61))
MOUTH_INNER = list(range(61, 68))
JAWLINE = list(range(0, 17))

CamerWidth = 960 # 얼굴 인식 할 떄 사용하는 카메라 사이즈
CameraHeight = 540

MouthThreshold = 0.26 # Mouth open 민감도 조절 

##### Sound setting######
CHANNELS = 1  #수정? -원래 2
RATE=44100
CHUNK=1024
RECORD_SECONDS=15

DBThreshold = 900 #2200 #수정 가능함 - 마이크 소리 민감도에 따라서 조절 가능함

Last_SoundDetect = time.time()

####Detection 관련 함수

print("코드실행시작")

cv2.ocl.setUseOpenCL(False)
# cap = cv2.VideoCapture(0)
cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 960)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 540)
#  capture.FrameWidth = 5000;
#                     capture.FrameHeight = 5000;


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
                    # python -> unity test   
                    data = sock.recv(1024) 
                    if not data:
                        break
                    val = int.from_bytes(data, "little")

                    if val == 400:
                        print("400 을 전달 받았음") 
                    elif val == 12:
                        print("12 을 전달 받았음")
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
    