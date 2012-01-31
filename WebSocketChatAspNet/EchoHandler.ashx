<%@ WebHandler Language="C#" Class="EchoHandler" %>

using System;
using System.Web;
using System.Net.WebSockets;
using System.Web.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class EchoHandler : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {

        if (context.IsWebSocketRequest)
        {
            context.AcceptWebSocketRequest(async wsContext =>
            {
                const int maxMessageSize = 128 * 1024;
                byte[] receiveBuffer = new byte[maxMessageSize];

                ArraySegment<byte> buffer = new ArraySegment<byte>(receiveBuffer);
                WebSocket socket = wsContext.WebSocket;

                while (socket.State == WebSocketState.Open)
                {
                    var input = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (input.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye, from the server", CancellationToken.None);
                    }
                    else
                    {
                        int offset = input.Count;
                        while (input.EndOfMessage == false)
                        {
                            input = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, offset, maxMessageSize - offset), CancellationToken.None);
                            offset += input.Count;
                        }

                        var userString = Encoding.UTF8.GetString(receiveBuffer, 0, offset);

                        if (userString == "/serverclose")
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "You asked for us to close", CancellationToken.None);
                        }
                        else
                        {
                            userString = "You said " + userString;
                            ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(userString));
                            await socket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
            });
            //}, new AspNetWebSocketOptions { Subprotocol = "chat" });
        }
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}