using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer.EventHandler
{
    public delegate void DataReceivedEventHandler(Object sender, string message, EventArgs e);
}
