﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer.EventHandler
{
    public delegate void BroadcastEventHandler(string message, EventArgs e);
}
