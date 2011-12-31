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

namespace HttpListenerWebSocketEcho
{
    //## The program entry point    
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

    //## The Server class        
    class Server
    {        
        private int count = 0;

        //### Starting the server        
        // Using HttpListener is reasonably straightforward. Start the listener and run a loop that receives and processes incoming WebSocket connections.
        // Each iteration of the loop "asynchronously waits" for the next incoming request using the `GetContextAsync` extension method (defined below).             
        // If the request is for a WebSocket connection then pass it on to `ProcessRequest` - otherwise set the status code to 400 (bad request). 
        public async void Start(string listenerPrefix)
        {
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

        //### Accepting WebSocket connections
        // Calling `AcceptWebSocketAsync` on the `HttpListenerContext` will accept the WebSocket connection, sending the required 101 response to the client
        // and return an instance of `WebSocketContext`. This class captures relevant information available at the time of the request and is a read-only 
        // type - you cannot perform any actual IO operations such as sending or receiving using the `WebSocketContext`. These operations can be 
        // performed by accessing the `System.Net.WebSocket` instance via the `WebSocketContext.WebSocket` property.        
        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            WebSocket webSocket = null;

            try
            {                
                // When calling `AcceptWebSocketAsync` the negotiated subprotocol must be specified. This sample assumes that no subprotocol 
                // was requested. There is a small bug here in the developer preview where string.Empty is treated as "no subprotocol". Instead it would be correct to pass null
                // and later versions of this sample will do so.
                WebSocketContext webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: string.Empty);
                                
                Interlocked.Increment(ref count);
                Console.WriteLine("Processed: {0}", count);
                
                webSocket = webSocketContext.WebSocket;

                //### Receiving
                // Define a receive buffer to hold data received on the WebSocket connection. The buffer will be reused as we only need to hold on to the data
                // long enough to send it back to the sender.
                byte[] receiveBuffer = new byte[1024];

                // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
                while (webSocket.State == WebSocketState.Open)
                {
                    // The first step is to begin a receive operation on the WebSocket. `ReceiveAsync` takes two parameters:
                    //
                    // * An `ArraySegment` to write the received data to. 
                    // * A cancellation token. In this example we are not using any timeouts so we use `CancellationToken.None`.
                    //
                    // `ReceiveAsync` returns a `Task<WebSocketReceiveResult>`. The `WebSocketReceiveResult` provides information on the receive operation that was just 
                    // completed, such as:                
                    //
                    // * `WebSocketReceiveResult.MessageType` - What type of data was received and written to the provided buffer. Was it binary, utf8, or a close message?                
                    // * `WebSocketReceiveResult.Count` - How many bytes were read?                
                    // * `WebSocketReceiveResult.EndOfMessage` - Have we finished reading the data for this message or is there more coming?
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    // The WebSocket protocol defines a close handshake that allows a party to send a close frame when they wish to gracefully shut down the connection.
                    // The party on the other end can complete the close handshake by sending back a close frame.
                    //
                    // If we received a close frame then lets participate in the handshake by sending a close frame back. This is achieved by calling `CloseAsync`. 
                    // `CloseAsync` will also terminate the underlying TCP connection once the close handshake is complete.
                    //
                    // The WebSocket protocol defines different status codes that can be sent as part of a close frame and also allows a close message to be sent. 
                    // If we are just responding to the client's request to close we can just use `WebSocketCloseStatus.NormalClosure` and omit the close message.
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    // This echo server can't handle text frames so if we receive any we close the connection with an appropriate status code and message.
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {                    
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);
                    }
                    // Otherwise we must have received binary data. Send it back by calling `SendAsync`. Note the use of the `EndOfMessage` flag on the receive result. This
                    // means that if this echo server is sent one continuous stream of binary data (with EndOfMessage always false) it will just stream back the same thing.
                    // If binary messages are received then the same binary messages are sent back.
                    else
                    {                        
                        await webSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count), WebSocketMessageType.Binary, receiveResult.EndOfMessage, CancellationToken.None);
                    }

                    // The echo operation is complete. The loop will resume and `ReceiveAsync` is called again to wait for the next data frame.
                }
            }
            catch(Exception e)
            {
                // Just log any exceptions to the console. Pretty much any exception that occurs when calling `SendAsync`/`ReceiveAsync`/`CloseAsync` is unrecoverable in that it will abort the connection and leave the `WebSocket` instance in an unusable state.
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
