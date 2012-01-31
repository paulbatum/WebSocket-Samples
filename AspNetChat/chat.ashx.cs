using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Web.WebSockets;

namespace AspNetChat
{
    public class ChatHttpHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(new WebSocketChatHandler());
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}