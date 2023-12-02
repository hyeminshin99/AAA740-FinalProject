using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using System.Text;


using MyBox;
using Newtonsoft.Json;

[HideInInspector]
public class TransferData
{
    // 요청 성공 실패 여부 전달
    public int res { get; set; }

    // 전달하고자 하는 명령 인덱스
    public int commandIndex { get; set; }

    // 전달하고자 하는 메시지
    public string message { get; set; }

    public string data { get; set; }
}

public enum Command
{
    Connect_Success = 200,
    SERVER_START = 1,
    SERVER_RESET = 3,

    SendMessage = 101,
    SendMessage_P = 102,
    SendMessage_N = 103,
    SendMessage_Angry = 104,
    SendMessageWithGesture = 105, //말행동 추가

    SendGesture = 110,
    SendGaze = 111, //gaze 추가
    SendEmotion = 112,

    SendGameWait = 130,

    SendEXPStart = 140,

    PythonClientConnected = 198,
    PythonClientDisConnected = 199,

    PythonMessage_L = 201, // 리스닝 정보 수신

    DH_Speech_START = 300,
    DH_Speech_END = 301,

    Error_SendToClient = 421,
    Connect_Dead = 422,

    TEST_LOG = 600,
    Test_SendMessage = 601,
    Test_ButtonClick = 602,
    Test_PythonMessage = 603,
    TEST = 602,

    TaskChange = 700,


    GreetingDesert = 711,
    GreetingWinter = 712,
    GreetingSea = 713,
    GreetingMoon = 714,


    ConditionChange = 800,
    HeartRateBPM = 900


}


public class TCPServerConnection : MonoBehaviour
{
    public static TCPServerConnection instance;
    // Do not Change.
    [ReadOnly]
    [SerializeField]
    private static ushort MYPROT_NUMBER = 710;

    private static int DATASIZE = 1000;

    private Socket m_ConnectedClient = null;
    private Socket m_PythonClient = null;
    private Socket m_ServerSocket = null;

    private AsyncCallback m_fnReceiveHandler;
    private AsyncCallback m_fnSendHandler;
    private AsyncCallback m_fnAcceptHandler;

    public Text status_text;

    public bool PythonConnected = false;

    public Image AndroidConnectStatusImage;

    [ReadOnly]
    public string CurIPAddress;

    Dictionary<EndPoint, Socket> connectedClients = new Dictionary<EndPoint, Socket>();

    public static int RECEIVED_DATA_FULL_SIZE = 100000;
    private byte[] receviedDataBytes = new byte[RECEIVED_DATA_FULL_SIZE];
    /// <summary>
    /// 클라이언트로부터 받아온 명령을 순차적으로 처리하기 위한 Queue 자료 구조 
    /// </summary>
    private Queue<TransferData> status = new Queue<TransferData>();

    public class AsyncObject
    {
        public byte[] Buffer;
        public Socket WorkingSocket;

        public AsyncObject(int bufferSize)
        {
            this.Buffer = new byte[bufferSize];
        }
    }


    private void Awake()
    {
        instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        allocateAsync();

        //ApplicationLabel.text += "/ " + LocalIPAddress();
        CurIPAddress = LocalIPAddress();

        StartServer(MYPROT_NUMBER);
    }


    /// <summary>
    /// 비동기 작업에 사용될 대리자를 초기화합니다.
    /// </summary>
    private void allocateAsync()
    {
        m_fnReceiveHandler = new AsyncCallback(handleDataReceive);
        m_fnSendHandler = new AsyncCallback(handleDataSend);
        m_fnAcceptHandler = new AsyncCallback(handleClientConnectionRequest);
    }


    void Update()
    {
        if (status.Count > 0)
        {
            TransferData q = status.Dequeue();
            Debug.Log(q);
            ///
            switch (q.commandIndex)
            {
                case (int)Command.SERVER_START:
                    status_text.text += 
                        "\nServer Start " + System.DateTime.Now.ToString("hh:mm:ss tt") 
                        + "\nCurrent Server IP: " + CurIPAddress;
                    break;
                case (int)Command.SERVER_RESET:
                    status_text.text += 
                        "Server Reset" + System.DateTime.Now.ToString("hh:mm:ss tt");
                    break;
                case (int)Command.TEST_LOG:
                    status_text.text += 
                        "\nLog Test\nLog Test\nLog Test\nLog Test\nLog Test";
                    break;

                case (int)Command.Connect_Success:

                    status_text.text += 
                        "\n<color=#00ff00>====================\nServer Connected - " 
                        + System.DateTime.Now.ToString("hh:mm:ss tt")
                        + " / Android Client\n====================</color>";

                    ChangeAndroidAppConnectionStatus(0);

                    break;

                case (int)Command.PythonClientDisConnected:
                    status_text.text 
                        += "\n<color=#ff0000>ERROR!! : Server Disconnected Client</color>";
                    
                    PythonConnected = false;

                    ChangeAndroidAppConnectionStatus(1);

                    break;
                case (int)Command.Error_SendToClient:
                    status_text.text += "\n<color=#ff0000>ERROR!! : maybe server disconnected to client</color>";
                    ChangeAndroidAppConnectionStatus(1);
                    break;
            }
        }
    }


    public void AddLog(string log)
    {
        status_text.text += $"\n{log}";
    }

    private void StartServer(ushort PORT)
    {
        //Debug.Log("Server 시작");
        TransferData start = new TransferData();
        start.res = 400;

        start.message = "서버 시작";

        start.commandIndex = (int)Command.SERVER_START;

        status.Enqueue(start);

        // TCP 통신을 위한 소켓을 생성합니다.
        m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        // 특정 포트에서 모든 주소로부터 들어오는 연결을 받기 위해 포트를 바인딩합니다.
        m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));

        // 연결 요청을 받기 시작하며, parameter는 얼마나 많은 클라이언트를 수용할 것인지를 지정한다. 
        m_ServerSocket.Listen(100);

        // BeginAccept 메서드를 이용해 들어오는 연결 요청을 비동기적으로 처리합니다.
        // 연결 요청을 처리하는 함수는 handleClientConnectionRequest 입니다.
        m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);
    }

    public void ResetServer()
    {
        TransferData reset = new TransferData();
        reset.res = 400;
        reset.message = "서버 리셋";
        reset.commandIndex = (int)Command.SERVER_RESET;

        status.Enqueue(reset);

        StopServer();

        StartServer(MYPROT_NUMBER);
    }

    /// <summary>
    /// 서버와 클라이언트가 연결 되는 것을 확인 
    /// </summary>
    /// <param name="ar"></param>
    public void handleClientConnectionRequest(IAsyncResult ar)
    {
        //Debug.Log("서버와 클라이언트가 연결됨");
        //status.Enqueue(0);

        // 클라이언트의 연결 요청을 수락합니다.
        Socket sockClient = m_ServerSocket.EndAccept(ar);

        m_ServerSocket.BeginAccept(m_fnAcceptHandler, null); // 다른 클라이언트를 받기 위해 준비하는 것. 

        // 110592 바이트의 크기를 갖는 바이트 배열을 가진 AsyncObject 클래스 생성
        AsyncObject ao = new AsyncObject(DATASIZE);

        // 작업 중인 소켓을 저장하기 위해 sockClient 할당
        ao.WorkingSocket = sockClient;

        connectedClients.Add(sockClient.RemoteEndPoint, sockClient);

        //Debug.Log(sockClient.RemoteEndPoint);

        // 클라이언트 소켓 저장
        //m_ConnectedClient = sockClient;

        // 비동기적으로 들어오는 자료를 수신하기 위해 BeginReceive 메서드 사용!
        sockClient.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
    }

  

    /// <summary>
    /// 클라이언트로부터 값을 받는다. 
    /// </summary>
    /// <param name="ar"></param>
    public void handleDataReceive(IAsyncResult ar)
    {
        // 넘겨진 추가 정보를 가져옵니다.
        // AsyncState 속성의 자료형은 Object 형식이기 때문에 형 변환이 필요합니다~!
        AsyncObject ao = (AsyncObject)ar.AsyncState;

        // 자료를 수신하고, 수신받은 바이트를 가져옵니다.
        int recvBytes = ao.WorkingSocket.EndReceive(ar);

        if (recvBytes == 0)
        {
            if (m_ConnectedClient != null)
            {
                // client가 죽은거임. 
                m_ConnectedClient.Close();

                TransferData dead = new TransferData();
                dead.res = 400;
                dead.message = "클라이언트가 죽음";
                dead.commandIndex = (int)Command.Connect_Dead;
                status.Enqueue(dead);
                //Debug.Log("현재 연결된 클라이언트가 죽고, 새로운 클라리언트의 연결을 대기하는 중");
            }
            // 새로운 클라이언트를 받을 준비를 한다. 
            m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);
        }


        // 수신받은 자료의 크기가 1 이상일 때에만 자료 처리
        if (recvBytes > 0)
        {
            Debug.Log("데이터 받음 : " + recvBytes);

            try
            {
                //Debug.Log("버퍼 크기 " + ao.Buffer.Length); // 110592
                //Array.Copy(ao.Buffer, 0, entireImageBytes, receivedData, recvBytes);
                Array.Clear(receviedDataBytes, 0, receviedDataBytes.Length);
                Array.Copy(ao.Buffer, receviedDataBytes, recvBytes);

                string s = Encoding.UTF8.GetString(receviedDataBytes);
                //Debug.Log(s);

                TransferData tData = JsonConvert.DeserializeObject<TransferData>(s);

                Debug.Log(tData.commandIndex);

                status.Enqueue(tData);


                if (tData.commandIndex == (int)Command.Connect_Success)
                {
                    Debug.Log("Here!");

                    PythonConnected = true;

                    m_ConnectedClient = connectedClients[ao.WorkingSocket.RemoteEndPoint];
                }


            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        // 전달받은 데이터에 대한 처리가 끝났을 경우, 다음 데이터를 수신 받기 위해 수신 대기를 해야 한다.
        // Begin~~ 메서드를 이용해 비동기적으로 작업을 대기했다면
        // 반드시 대리자 함수에서 End~~ 메서드를 이용해 비동기 작업이 끝났다고 주어야 한다. 
        ao.WorkingSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);

    }


    void ChangeAndroidAppConnectionStatus(int i)
    {
        AndroidConnectStatusImage.color = i == 0 ? Color.green : Color.red;
    }

    public void SendData2Client(int res, string message, Command name)
    {
        TransferData tData = new TransferData();
        tData.res = res;
        tData.message = message;
        tData.commandIndex = (int)name;

        string jsonJoinChatRoomInfo = JsonConvert.SerializeObject(tData);

        AsyncObject ao = new AsyncObject(1000);

        ao.Buffer = Encoding.UTF8.GetBytes(jsonJoinChatRoomInfo);

        // 사용된 소켓을 저장
        ao.WorkingSocket = m_ServerSocket;

        // 전송 시작!
        try
        {
            m_ConnectedClient.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
        }
        catch (Exception ex)
        {
            Debug.Log("전송 중 오류 발생 :" + ex.ToString());
            tData.res = 400;
            tData.message = "전송 중 오류 발생 :" + ex.ToString();
            tData.commandIndex = (int)Command.Error_SendToClient;
            status.Enqueue(tData);
        }
    }


    public void SendData2PythonClientImageBuffer(List<byte> buffer)
    {
        TransferData tData = new TransferData();
        tData.res = 147;

        tData.commandIndex = 1;

        tData.data = Convert.ToBase64String(buffer.ToArray());  // 바이너리 데이터를 Base64 문자열로 변환
        

        string jsonJoinChatRoomInfo = JsonConvert.SerializeObject(tData);

        AsyncObject ao = new AsyncObject(500000);

        ao.Buffer = Encoding.UTF8.GetBytes(jsonJoinChatRoomInfo);

        // 사용된 소켓을 저장
        ao.WorkingSocket = m_ServerSocket;

        Debug.Log("이만큼 보냄" + ao.Buffer.Length);

        FlagFirst(ao.Buffer.Length);

        StartCoroutine(Wait(ao));
    }

    IEnumerator Wait(AsyncObject ao)
    {
        yield return new WaitForSeconds(3f);
        try
        {
            m_ConnectedClient.BeginSend(ao.Buffer, 0,
                ao.Buffer.Length,
                SocketFlags.None,
                m_fnSendHandler, ao);
        }
        catch (Exception ex)
        {
            Debug.Log("전송 중 오류 발생 :" + ex.ToString());
        }
    }

    public void FlagFirst(int length)
    {
        TransferData tData = new TransferData();
        tData.res = 145;

        tData.commandIndex = 2;

        tData.message = length.ToString();
        //tData.data = Convert.ToBase64String(buffer.ToArray());  // 바이너리 데이터를 Base64 문자열로 변환


        string jsonJoinChatRoomInfo = JsonConvert.SerializeObject(tData);

        AsyncObject ao = new AsyncObject(500000);

        ao.Buffer = Encoding.UTF8.GetBytes(jsonJoinChatRoomInfo);

        // 사용된 소켓을 저장
        ao.WorkingSocket = m_ServerSocket;

        Debug.Log("이만큼 보냄" + ao.Buffer.Length);

        tData.message = ao.Buffer.Length.ToString();

        try
        {
            m_ConnectedClient.BeginSend(ao.Buffer, 0,
                ao.Buffer.Length,
                SocketFlags.None,
                m_fnSendHandler, ao);
        }
        catch (Exception ex)
        {

            Debug.Log("전송 중 오류 발생 :" + ex.ToString());
        }

    }



    /// <summary>
    /// 전달한 데이터에 대한 정보를 가져오는 역할을 함. 
    /// 데이터를 보내면 어떤 데이터를 보냈었는지 확인할 수 있는 함수임. 
    /// </summary>
    /// <param name="ar"></param>
    public void handleDataSend(IAsyncResult ar)
    {
        // 넘겨진 추가 정보를 가져옵니다.
        AsyncObject ao = (AsyncObject)ar.AsyncState;

        // 자료를 전송하고, 전송한 바이트를 가져옵니다.
        int sentBytes = ao.WorkingSocket.EndSend(ar);

        if (sentBytes > 0)
        {
            Debug.Log("이만큼 보냄 : " + sentBytes);
            // Debug.Log("메시지 보냄" + Encoding.Unicode.GetString(ao.Buffer));
        }
    }

    public void StopServer()
    {
        Debug.Log("Server Close");

        // 가차없이 서버 소켓을 닫습니다.
        m_ServerSocket.Close();

        //if (m_ConnectedClient != null)
        //{
        //    m_ConnectedClient.Close();
        //}
    }

    public static string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "0.0.0.0";
        host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                if (localIP.StartsWith("192"))
                {
                    break;
                }
            }
        }

        return localIP;
    }

    public void LogClear()
    {
        status_text.text = "";
    }

    public void PauseServer()
    {
        Debug.Log("중단!!");

        //// 모든 클라이언트 연결을 중단합니다.
        //foreach (var client in connectedClients.Values)
        //{
        //    client.Shutdown(SocketShutdown.Both);
        //    client.Close();
        //}
        //connectedClients.Clear();

        // 서버 소켓을 일시 중단합니다.
        m_ServerSocket.Close();

        //if (m_ServerSocket != null && m_ServerSocket.Connected)
        //{
         

            
        //}
    }

    public void ResumeServer()
    {
        if (m_ServerSocket == null || !m_ServerSocket.Connected)
        {
            // 서버 소켓을 다시 시작합니다.
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, MYPROT_NUMBER));
            m_ServerSocket.Listen(100000);
            m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);

            Debug.Log("재개 완료!!");
        }
    }


    private void OnApplicationQuit()
    {
        StopServer();
    }
}


