using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using WebSocketServer.EventHandler;

namespace WebSocketServer
{
    class WebSocketServerTest : IDisposable
    {
        private WebSocketServer WSServer;
        public static List<string> userListOnline = new List<string>();
        public WebSocketServerTest()
        {
            //使用默认的设置
            WSServer = new WebSocketServer();  
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            WSServer.Dispose();
            GC.SuppressFinalize(this);
        }

        ~WebSocketServerTest()
        {
            Close();
        }

        public void Start()
        {
            WSServer.wsNewConnection += new NewConnectionEventHandler(WSServer_NewConnection);
            WSServer.wsDisconnected += new DisconnectedEventHandler(WSServer_Disconnected);
            WSServer.wsDataReceived += new DataReceivedEventHandler(WSServer_BroadcastMessage);

            WSServer.StartServer();
        }

        void WSServer_Disconnected(Object sender, EventArgs e)
        {
        }

        void WSServer_NewConnection(string loginName, EventArgs e)
        {
            //if (!userListOnline.Contains(loginName))
            //{
            //    userListOnline.Add(loginName);
            //}
        }
        void WSServer_BroadcastMessage(Object sender, string message, EventArgs e)
        {
           
        }
    }
}
