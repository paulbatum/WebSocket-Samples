// This program is a binary echo server for WebSockets using the new WebSockets API in .NET 4.5. It is designed to run on the Windows 8 developer preview.
//
// This console application uses `HttpListener` to receive WebSocket connections. It expects to receive binary data and it streams back the data as it receives it.
//
// This program takes advantage of the new asynchrony features in C# 5. Explaining these features is beyond the scope of this documentation - 
// to learn more visit the [async homepage](http://msdn.com/async) or read the [async articles](http://blogs.msdn.com/b/ericlippert/archive/tags/async) 
// on Eric Lippert's blog.   
//
// The [source](https://github.com/paulbatum/WebSocket-Samples) for this sample is on GitHub.
//
// This HTML documentation was generated using [Nocco](http://dontangg.github.com/nocco/).

//### Imports
// Some standard imports, but note the last one is the new `System.Net.WebSockets` namespace.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Microsoft.ServiceBus.Messaging;

namespace HttpListenerWebSocketEcho
{
    // Passes an HttpListener prefix for the server to listen on. The prefix 'http://+:80/wsDemo/' indicates that the server should listen on 
    // port 80 for requests to wsDemo (e.g. http://localhost/wsDemo). For more information on HttpListener prefixes see [MSDN](http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx).            
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            server.Start("http://+:80/wsDemo/");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    class Server
    {
        private EventHubClient client;
        private int count = 0;

        public async void Start(string listenerPrefix)
        {
            // TODO: Put connection string in settings file.
            client = EventHubClient.CreateFromConnectionString("Endpoint=sb://testsbwebsocket.servicebus.windows.net/;SharedAccessKeyName=Managed;SharedAccessKey=Wf6ALbH9pSc02IGmP7ThPKbosjM2lxSzFAWBX58sKqw=;EntityPath=testwebsocketreceiver");

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(listenerContext);
                }
                else
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {

            WebSocketContext webSocketContext = null;
            try
            {
                // When calling `AcceptWebSocketAsync` the negotiated subprotocol must be specified. This sample assumes that no subprotocol 
                // was requested. 
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref count);
                Console.WriteLine("Processed: {0}", count);
            }
            catch (Exception e)
            {
                // The upgrade process failed somehow. For simplicity lets assume it was a failure on the part of the server and indicate this using 500.
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            try
            {
                //### Receiving
                // Define a receive buffer to hold data received on the WebSocket connection. The buffer will be reused.
                byte[] receiveBuffer = new byte[1024];

                while (webSocket.State == WebSocketState.Open)
                {
                    // The first step is to begin a receive operation on the WebSocket. `ReceiveAsync` takes two parameters:
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Received message of MessageType Close. Closing connection...");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        var str = Encoding.Default.GetString(receiveBuffer, 0, receiveResult.Count);
                        client.Send(new EventData(Encoding.UTF8.GetBytes(str)));
                    }
                    else
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count), WebSocketMessageType.Binary, receiveResult.EndOfMessage, CancellationToken.None);
                        var str = Encoding.Default.GetString(receiveBuffer, 0, receiveResult.Count);
                        client.Send(new EventData(Encoding.UTF8.GetBytes(str)));
                    }

                    // Forwarding to event hub operation complete.
                }
            }
            catch (Exception e)
            {
                // Just log any exceptions to the console. Pretty much any 
                // exception that occurs when calling 
                // `SendAsync`/`ReceiveAsync`/`CloseAsync` is unrecoverable 
                // in that it will abort the connection and leave the 
                // `WebSocket` instance in an unusable state.
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }
    }

    // This extension method wraps the BeginGetContext / EndGetContext methods on HttpListener as a Task, using a helper function from the Task Parallel Library (TPL).
    // This makes it easy to use HttpListener with the C# 5 asynchrony features.
    public static class HelperExtensions
    {
        public static Task GetContextAsync(this HttpListener listener)
        {
            return Task.Factory.FromAsync<HttpListenerContext>(listener.BeginGetContext, listener.EndGetContext, TaskCreationOptions.None);
        }
    }
}
