using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;

using Newtonsoft.Json;
using WebSocketsServer.Model;
using WebSocketServer.EventHandler;

namespace WebSocketServer
{
    public class Logger
    {
        NLog.Logger nLogger = NLog.LogManager.GetCurrentClassLogger();
        public bool LogEvents { get; set; }

        public Logger()
        {
            LogEvents = true;
        }

        public void Log(string Text)
        {
            if (LogEvents)
            {
                Console.WriteLine(Text);
            }
            nLogger.Debug(Text);
        }
    }

    public enum ServerStatusLevel { Off, WaitingConnection, ConnectionEstablished };

    public class WebSocketServer : IDisposable
    {
        private bool AlreadyDisposed;
        private Socket Listener;
        private int ConnectionsQueueLength;
        private int MaxBufferSize;
        private string Handshake;
        private StreamReader ConnectionReader;
        private StreamWriter ConnectionWriter;
        private Logger logger;
        private byte[] FirstByte;
        private byte[] LastByte;
        private byte[] ServerKey1;
        private byte[] ServerKey2;

        private MessageBase MSG;
        private List<string> userListOnline = new List<string>();
        public static bool userExists = false;
        private List<EndPoint> remoteEndPoint = new List<EndPoint>();

        Socket sc;                  //当前socket
        SocketConnection socketConn;//当前socket连接
        List<SocketConnection> connectionSocketList = new List<SocketConnection>();

        public ServerStatusLevel Status { get; private set; }
        public int ServerPort { get; set; }
        public string ServerLocation { get; set; }
        public string ConnectionOrigin { get; set; }
        public bool LogEvents { 
            get { return logger.LogEvents; }
            set { logger.LogEvents = value; }
        }

        //WebSocketServerTest使用
        public event NewConnectionEventHandler wsNewConnection;
        public event DataReceivedEventHandler wsDataReceived;
        public event DisconnectedEventHandler wsDisconnected;

        private void Initialize()
        {
            AlreadyDisposed = false;
            logger = new Logger();

            Status = ServerStatusLevel.Off;
            ConnectionsQueueLength = 500;
            MaxBufferSize = 1024 * 100;
            FirstByte = new byte[MaxBufferSize];
            LastByte = new byte[MaxBufferSize];
            FirstByte[0] = 0x00;
            LastByte[0] = 0xFF;
            logger.LogEvents = true;
        }

        //默认连接
        public WebSocketServer() 
        {
            ServerPort = 4141;
            ServerLocation = string.Format("ws://{0}:4141", getLocalmachineIPAddress());
            Initialize();
        }
        //指定连接
        public WebSocketServer(int serverPort, string serverLocation, string connectionOrigin)
        {
            ServerPort = serverPort;
            ConnectionOrigin = connectionOrigin;
            ServerLocation = serverLocation;
            Initialize();
        }


        ~WebSocketServer()
        {
            Close();
        }
        public void Dispose()
        {
            Close();
        }
        private void Close()
        {
            if (!AlreadyDisposed)
            {
                AlreadyDisposed = true;
                if (Listener != null) Listener.Close();
                foreach (SocketConnection item in connectionSocketList)
                {
                    item.ConnectionSocket.Close();
                }
                connectionSocketList.Clear();
                GC.SuppressFinalize(this);
            }
        }

        public static  IPAddress getLocalmachineIPAddress()
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);

            foreach (IPAddress ip in ipEntry.AddressList)
            {
                //IPV4
                if (ip.AddressFamily == AddressFamily.InterNetwork) 
                return ip;
            }
            return ipEntry.AddressList[0];
        }

        public void StartServer()
        {
            Char char1 = Convert.ToChar(65533);

            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);  
            Listener.Bind(new IPEndPoint(getLocalmachineIPAddress(), ServerPort));  
            
            Listener.Listen(ConnectionsQueueLength);  
            
            logger.Log(string.Format("聊天服务器启动。监听地址：{0}, 端口：{1}",getLocalmachineIPAddress(),ServerPort));
            logger.Log(string.Format("WebSocket服务器地址: ws://{0}:{1}/chat", getLocalmachineIPAddress(), ServerPort));

            while (true)
            {
                sc = Listener.Accept();
                if (sc != null)
                {
                    System.Threading.Thread.Sleep(100);
                    socketConn = new SocketConnection();
                    socketConn.ConnectionSocket = sc;
                    socketConn.scNewConnection += new NewConnectionEventHandler(socketConn_NewConnection);
                    socketConn.scDataReceived += new DataReceivedEventHandler(socketConn_BroadcastMessage);
                    socketConn.scDisconnected += new DisconnectedEventHandler(socketConn_Disconnected);

                    socketConn.ConnectionSocket.BeginReceive(socketConn.receivedDataBuffer,
                                                             0, socketConn.receivedDataBuffer.Length, 
                                                             0, new AsyncCallback(socketConn.ManageHandshake), 
                                                             socketConn.ConnectionSocket.Available);
                    connectionSocketList.Add(socketConn);
                }
            }
        }
        void socketConn_Disconnected(Object sender, EventArgs e)
        {
            SocketConnection sConn = sender as SocketConnection;
            if (sConn != null)
            {
                Send( JsonConvert.SerializeObject(new MessageBase() { Auth="sys", Type = MSGType.msg, Message = string.Format("【{0}】离开了聊天室！", sConn.Name), Action = MSGAction.logout }));
                if (userListOnline.Contains(sConn.Name))
                {
                    userListOnline.Remove(sConn.Name);
                    Send(JsonConvert.SerializeObject(new MessageBase() { Auth = "sys", Type = MSGType.user, Message = string.Join(",", userListOnline), Action = MSGAction.logout }));
                }
                sConn.ConnectionSocket.Close();
                connectionSocketList.Remove(sConn);
                
            }
        }
        void socketConn_BroadcastMessage(Object sender, string message, EventArgs e)
        {
            MessageBase temMsg = new MessageBase();
            string temp = string.Empty;

            MSG = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageBase>(message.Replace(@"\",@"\\"));
            //进入聊天室判断：MSG.Message 进入的昵称
            if (MSG.Type==MSGType.user && MSG.Action==MSGAction.login)
            {
                SocketConnection sConn = sender as SocketConnection;
                sConn.Name = MSG.Message;
                //昵称已存在则 单线返回错误消息，并关闭当前连接，然后return；
                if (userListOnline.Contains(MSG.Message))
                {
                    userExists = true;
                    SingleSend(
                        JsonConvert.SerializeObject(new MessageBase() { Auth = MSG.Auth, Type = MSG.Type, Message = "昵称已存在，请更换其他昵称再试！", Action = MSG.Action,State=false })
                        ,sConn);
                    //昵称重复，移除连接列表
                    connectionSocketList.Remove(sConn);
                    return;
                }
                //新成员加入成员列表
                connectionSocketList.ForEach(
                   (SocketConnection sc) =>
                   {
                       if (!userListOnline.Contains(sc.Name))
                       {
                           userListOnline.Add(sc.Name);
                       }
                   });

                //1，群发，欢迎新成员致辞
                temMsg.Auth = "";
                temMsg.Type = MSGType.msg;
                temMsg.Action = MSGAction.sendmsg;
                temMsg.Message = string.Format("欢迎【{0}】来到聊天室！", MSG.Message);
                temp = JsonConvert.SerializeObject(temMsg);
                Send(temp);
                //2，群发，获取在线用户，用于页面更新列表
                temMsg.Auth = MSG.Auth;
                temMsg.Type = MSGType.user;
                temMsg.Action = MSGAction.login;
                temMsg.Message = string.Join(",", userListOnline.ToArray());
                temp = JsonConvert.SerializeObject(temMsg);
                Send(temp);
                return;
            }
            //发送聊天消息。
            Send(JsonConvert.SerializeObject(new MessageBase() {Auth=MSG.Auth, Type = MSG.Type, Message = MSG.Message, Action = MSG.Action }));
        }
        void socketConn_NewConnection(string name, EventArgs e)
        {
            if (wsNewConnection != null)
                wsNewConnection(name,EventArgs.Empty);
        }
        //群发
        public void Send(string message)
        {
            foreach (SocketConnection item in connectionSocketList)
            {
                if (!item.ConnectionSocket.Connected) return;
                try
                {
                    if (item.IsDataMasked)
                    {
                        DataFrame dr = new DataFrame(message);
                        item.ConnectionSocket.Send(dr.GetBytes());
                    }
                    else
                    {
                        item.ConnectionSocket.Send(FirstByte);
                        item.ConnectionSocket.Send(Encoding.UTF8.GetBytes(message));
                        item.ConnectionSocket.Send(LastByte);
                    }
                }
                catch(Exception ex)
                {
                    logger.Log(ex.Message);
                }
            }
        }
        //单线发送消息
        public void SingleSend(string message,SocketConnection sconn)
        {
            if (!sconn.ConnectionSocket.Connected) return;
            try
            {
                if (sconn.IsDataMasked)
                {
                    DataFrame dr = new DataFrame(message);
                    sconn.ConnectionSocket.Send(dr.GetBytes());
                }
                else
                {
                    sconn.ConnectionSocket.Send(FirstByte);
                    sconn.ConnectionSocket.Send(Encoding.UTF8.GetBytes(message));
                    sconn.ConnectionSocket.Send(LastByte);
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message);
            }
        }
    }
}



