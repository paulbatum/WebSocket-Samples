using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.ServiceModel.WebSockets;

namespace WCFEcho
{    
    public class EchoService : WebSocketService
    {
        public override void OnMessage(string message)
        {
            this.Send(message);
        }
    }
}