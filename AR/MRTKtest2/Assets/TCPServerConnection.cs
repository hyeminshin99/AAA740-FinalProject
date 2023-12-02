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
    // ��û ���� ���� ���� ����
    public int res { get; set; }

    // �����ϰ��� �ϴ� ��� �ε���
    public int commandIndex { get; set; }

    // �����ϰ��� �ϴ� �޽���
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
    SendMessageWithGesture = 105, //���ൿ �߰�

    SendGesture = 110,
    SendGaze = 111, //gaze �߰�
    SendEmotion = 112,

    SendGameWait = 130,

    SendEXPStart = 140,

    PythonClientConnected = 198,
    PythonClientDisConnected = 199,

    PythonMessage_L = 201, // ������ ���� ����

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
    /// Ŭ���̾�Ʈ�κ��� �޾ƿ� ����� ���������� ó���ϱ� ���� Queue �ڷ� ���� 
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
    /// �񵿱� �۾��� ���� �븮�ڸ� �ʱ�ȭ�մϴ�.
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
        //Debug.Log("Server ����");
        TransferData start = new TransferData();
        start.res = 400;

        start.message = "���� ����";

        start.commandIndex = (int)Command.SERVER_START;

        status.Enqueue(start);

        // TCP ����� ���� ������ �����մϴ�.
        m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        // Ư�� ��Ʈ���� ��� �ּҷκ��� ������ ������ �ޱ� ���� ��Ʈ�� ���ε��մϴ�.
        m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));

        // ���� ��û�� �ޱ� �����ϸ�, parameter�� �󸶳� ���� Ŭ���̾�Ʈ�� ������ �������� �����Ѵ�. 
        m_ServerSocket.Listen(100);

        // BeginAccept �޼��带 �̿��� ������ ���� ��û�� �񵿱������� ó���մϴ�.
        // ���� ��û�� ó���ϴ� �Լ��� handleClientConnectionRequest �Դϴ�.
        m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);
    }

    public void ResetServer()
    {
        TransferData reset = new TransferData();
        reset.res = 400;
        reset.message = "���� ����";
        reset.commandIndex = (int)Command.SERVER_RESET;

        status.Enqueue(reset);

        StopServer();

        StartServer(MYPROT_NUMBER);
    }

    /// <summary>
    /// ������ Ŭ���̾�Ʈ�� ���� �Ǵ� ���� Ȯ�� 
    /// </summary>
    /// <param name="ar"></param>
    public void handleClientConnectionRequest(IAsyncResult ar)
    {
        //Debug.Log("������ Ŭ���̾�Ʈ�� �����");
        //status.Enqueue(0);

        // Ŭ���̾�Ʈ�� ���� ��û�� �����մϴ�.
        Socket sockClient = m_ServerSocket.EndAccept(ar);

        m_ServerSocket.BeginAccept(m_fnAcceptHandler, null); // �ٸ� Ŭ���̾�Ʈ�� �ޱ� ���� �غ��ϴ� ��. 

        // 110592 ����Ʈ�� ũ�⸦ ���� ����Ʈ �迭�� ���� AsyncObject Ŭ���� ����
        AsyncObject ao = new AsyncObject(DATASIZE);

        // �۾� ���� ������ �����ϱ� ���� sockClient �Ҵ�
        ao.WorkingSocket = sockClient;

        connectedClients.Add(sockClient.RemoteEndPoint, sockClient);

        //Debug.Log(sockClient.RemoteEndPoint);

        // Ŭ���̾�Ʈ ���� ����
        //m_ConnectedClient = sockClient;

        // �񵿱������� ������ �ڷḦ �����ϱ� ���� BeginReceive �޼��� ���!
        sockClient.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
    }

  

    /// <summary>
    /// Ŭ���̾�Ʈ�κ��� ���� �޴´�. 
    /// </summary>
    /// <param name="ar"></param>
    public void handleDataReceive(IAsyncResult ar)
    {
        // �Ѱ��� �߰� ������ �����ɴϴ�.
        // AsyncState �Ӽ��� �ڷ����� Object �����̱� ������ �� ��ȯ�� �ʿ��մϴ�~!
        AsyncObject ao = (AsyncObject)ar.AsyncState;

        // �ڷḦ �����ϰ�, ���Ź��� ����Ʈ�� �����ɴϴ�.
        int recvBytes = ao.WorkingSocket.EndReceive(ar);

        if (recvBytes == 0)
        {
            if (m_ConnectedClient != null)
            {
                // client�� ��������. 
                m_ConnectedClient.Close();

                TransferData dead = new TransferData();
                dead.res = 400;
                dead.message = "Ŭ���̾�Ʈ�� ����";
                dead.commandIndex = (int)Command.Connect_Dead;
                status.Enqueue(dead);
                //Debug.Log("���� ����� Ŭ���̾�Ʈ�� �װ�, ���ο� Ŭ�󸮾�Ʈ�� ������ ����ϴ� ��");
            }
            // ���ο� Ŭ���̾�Ʈ�� ���� �غ� �Ѵ�. 
            m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);
        }


        // ���Ź��� �ڷ��� ũ�Ⱑ 1 �̻��� ������ �ڷ� ó��
        if (recvBytes > 0)
        {
            Debug.Log("������ ���� : " + recvBytes);

            try
            {
                //Debug.Log("���� ũ�� " + ao.Buffer.Length); // 110592
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

        // ���޹��� �����Ϳ� ���� ó���� ������ ���, ���� �����͸� ���� �ޱ� ���� ���� ��⸦ �ؾ� �Ѵ�.
        // Begin~~ �޼��带 �̿��� �񵿱������� �۾��� ����ߴٸ�
        // �ݵ�� �븮�� �Լ����� End~~ �޼��带 �̿��� �񵿱� �۾��� �����ٰ� �־�� �Ѵ�. 
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

        // ���� ������ ����
        ao.WorkingSocket = m_ServerSocket;

        // ���� ����!
        try
        {
            m_ConnectedClient.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
        }
        catch (Exception ex)
        {
            Debug.Log("���� �� ���� �߻� :" + ex.ToString());
            tData.res = 400;
            tData.message = "���� �� ���� �߻� :" + ex.ToString();
            tData.commandIndex = (int)Command.Error_SendToClient;
            status.Enqueue(tData);
        }
    }


    public void SendData2PythonClientImageBuffer(List<byte> buffer)
    {
        TransferData tData = new TransferData();
        tData.res = 147;

        tData.commandIndex = 1;

        tData.data = Convert.ToBase64String(buffer.ToArray());  // ���̳ʸ� �����͸� Base64 ���ڿ��� ��ȯ
        

        string jsonJoinChatRoomInfo = JsonConvert.SerializeObject(tData);

        AsyncObject ao = new AsyncObject(500000);

        ao.Buffer = Encoding.UTF8.GetBytes(jsonJoinChatRoomInfo);

        // ���� ������ ����
        ao.WorkingSocket = m_ServerSocket;

        Debug.Log("�̸�ŭ ����" + ao.Buffer.Length);

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
            Debug.Log("���� �� ���� �߻� :" + ex.ToString());
        }
    }

    public void FlagFirst(int length)
    {
        TransferData tData = new TransferData();
        tData.res = 145;

        tData.commandIndex = 2;

        tData.message = length.ToString();
        //tData.data = Convert.ToBase64String(buffer.ToArray());  // ���̳ʸ� �����͸� Base64 ���ڿ��� ��ȯ


        string jsonJoinChatRoomInfo = JsonConvert.SerializeObject(tData);

        AsyncObject ao = new AsyncObject(500000);

        ao.Buffer = Encoding.UTF8.GetBytes(jsonJoinChatRoomInfo);

        // ���� ������ ����
        ao.WorkingSocket = m_ServerSocket;

        Debug.Log("�̸�ŭ ����" + ao.Buffer.Length);

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

            Debug.Log("���� �� ���� �߻� :" + ex.ToString());
        }

    }



    /// <summary>
    /// ������ �����Ϳ� ���� ������ �������� ������ ��. 
    /// �����͸� ������ � �����͸� ���¾����� Ȯ���� �� �ִ� �Լ���. 
    /// </summary>
    /// <param name="ar"></param>
    public void handleDataSend(IAsyncResult ar)
    {
        // �Ѱ��� �߰� ������ �����ɴϴ�.
        AsyncObject ao = (AsyncObject)ar.AsyncState;

        // �ڷḦ �����ϰ�, ������ ����Ʈ�� �����ɴϴ�.
        int sentBytes = ao.WorkingSocket.EndSend(ar);

        if (sentBytes > 0)
        {
            Debug.Log("�̸�ŭ ���� : " + sentBytes);
            // Debug.Log("�޽��� ����" + Encoding.Unicode.GetString(ao.Buffer));
        }
    }

    public void StopServer()
    {
        Debug.Log("Server Close");

        // �������� ���� ������ �ݽ��ϴ�.
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
        Debug.Log("�ߴ�!!");

        //// ��� Ŭ���̾�Ʈ ������ �ߴ��մϴ�.
        //foreach (var client in connectedClients.Values)
        //{
        //    client.Shutdown(SocketShutdown.Both);
        //    client.Close();
        //}
        //connectedClients.Clear();

        // ���� ������ �Ͻ� �ߴ��մϴ�.
        m_ServerSocket.Close();

        //if (m_ServerSocket != null && m_ServerSocket.Connected)
        //{
         

            
        //}
    }

    public void ResumeServer()
    {
        if (m_ServerSocket == null || !m_ServerSocket.Connected)
        {
            // ���� ������ �ٽ� �����մϴ�.
            m_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_ServerSocket.Bind(new IPEndPoint(IPAddress.Any, MYPROT_NUMBER));
            m_ServerSocket.Listen(100000);
            m_ServerSocket.BeginAccept(m_fnAcceptHandler, null);

            Debug.Log("�簳 �Ϸ�!!");
        }
    }


    private void OnApplicationQuit()
    {
        StopServer();
    }
}


