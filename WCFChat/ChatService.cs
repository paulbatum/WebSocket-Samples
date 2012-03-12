using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.ServiceModel.WebSockets;

namespace WCFChat
{
    public class ChatService : WebSocketService
    {
        private static WebSocketCollection<ChatService> clients = new WebSocketCollection<ChatService>();
        private static int count = 0;

        private string name;
        private bool faulted;

        public override void OnOpen()
        {
            clients.Add(this);            
            this.name = string.Format("Client {0}", 
                Interlocked.Increment(ref count));

            clients.Broadcast(string.Format("{0} joined.", name));
        }

        protected override void OnClose()
        {
            if (!faulted)
            {
                clients.Remove(this);
                clients.Broadcast(string.Format("{0} left.", name));
            }
        }

        protected override void OnError()
        {
            faulted = true;
            clients.Remove(this);
            clients.Broadcast(string.Format("{0} left (error).", name));
        }

        public override void OnMessage(string message)
        {
            clients.Broadcast(string.Format("{0}: {1}", name, message));
        }        
    }




    
}
