// This is an example of how to use the new low level WebSockets APIs introduced in ASP.NET 4.5.
// In practice we expect that few developers will need to use these low level APIs and will instead use the 
// [Microsoft.WebSockets](http://nuget.org/List/Packages/Microsoft.WebSockets) NuGet package.
//
// The function of this handler is simple. It receive text messages over the WebSocket connection, appends "You said "
// to the front of the message and sends it back over the WebSocket connection.
//
// This handler takes advantage of the new asynchrony features in C# 5. Explaining these features is beyond the scope of this documentation - 
// to learn more visit the [async homepage](http://msdn.com/async) or read the [async articles](http://blogs.msdn.com/b/ericlippert/archive/tags/async) 
// on Eric Lippert's blog.   
//
// The [source](https://github.com/paulbatum/WebSocket-Samples) for this sample is on GitHub.
//
// This HTML documentation was generated using [Nocco](http://dontangg.github.com/nocco/).

//### Imports
// Some standard imports, but note the last one is the new `System.Net.WebSockets` namespace.
using System;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace AspNetWebSocketEcho
{
    //## The EchoHandler class
    // The most low-level way to utilize the new WebSocket features in ASP.NET is to implement your own IHttpHandler.
    public class EchoHandler : IHttpHandler
    {        
        //### Accepting the connection
        // If the incoming request is a valid WebSocket request then accept the request and use the `HandleWebSocket` method to handle the WebSocket 
        // connection. Defining a seperate method (`HandleWebSocket` in this case) isn't necessary but it helps with readability by reducing the amount of code nesting.
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(HandleWebSocket);
            else
                context.Response.StatusCode = 400;

        }
                     
        // When a WebSocket connection request is accepted, `HandleWebSocket` is invoked and ASP.NET passes in an instance of `WebSocketContext`. The `WebSocketContext`
        // class captures relevant information available at the time of the request. For example:
        //
        // * `WebSocketContext.RequestUri` is the Uri used to establish the WebSocket connection. This might include query string parameters.
        // * `WebSocketContext.CookieCollection` contains any cookies that were included in the connection request.               
        //
        // `WebSocketContext` is a read-only type - you cannot perform any actual IO operations such as sending or receiving using the `WebSocketContext`. These operations 
        // can be performed by accessing the `System.Net.WebSocket` instance via the `WebSocketContext.WebSocket` property.
        private async Task HandleWebSocket(WebSocketContext wsContext)
        {
            //### Receiving
            // Define a maximum message size this handler can receive (1K in this case) and allocate a buffer to contain the received message. 
            // This buffer will be reused for each receive operation.
            const int maxMessageSize = 1024;
            byte[] receiveBuffer = new byte[maxMessageSize];            
            WebSocket socket = wsContext.WebSocket;            

            // While the WebSocket connection remains open we run a simple loop that receives messages and then sends them back.
            while (socket.State == WebSocketState.Open)
            {                
                // The first step is to begin a receive operation on the WebSocket. `ReceiveAsync` takes two parameters:
                //
                // * An `ArraySegment` to write the received data to. In this particular case we are passing an `ArraySegment` that points to our entire receive buffer.
                // You might be wondering what the point of `ArraySegment` is and why `ReceiveAsync` doesn't just accept a `byte[]`. This will be explained below.                
                // * A cancellation token. In this example we are not using any timeouts so we use `CancellationToken.None`.
                //
                // `ReceiveAsync` returns a `Task<WebSocketReceiveResult>`. We use the await keyword to "asynchronously wait" for the receive operation to complete and
                // extract the WebSocketReceiveResult from the completed task. The `WebSocketReceiveResult` provides information on the receive operation that was just 
                // completed, such as:                
                //
                // * `WebSocketReceiveResult.MessageType` - What type of data was received and written to the provided buffer. Was it binary, utf8, or a close message?                
                // * `WebSocketReceiveResult.Count` - How many bytes were read?                
                // * `WebSocketReceiveResult.EndOfMessage` - Have we finished reading the data for this message or is there more coming?
                WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                
                // The WebSocket protocol defines a close handshake that allows a party to send a close frame when they wish to gracefully shut down the connection.
                // The party on the other end can complete the close handshake by sending back a close frame.
                //
                // If we received a close frame then lets participate in the handshake by sending a close frame back. This is acheived by calling `CloseAsync`. 
                // `CloseAsync` will also terminate the underlying TCP connection once the close handshake is complete.
                //
                // The WebSocket protocol defines different status codes that can be sent as part of a close frame and also allows a close message to be sent. 
                // If we are just responding to the client's request to close we can just use `WebSocketCloseStatus.NormalClosure` and omit the close message.
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {                    
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                // This echo server can't handle binary messages so if we receive one we close the connection with an appropriate status code and message.
                else if (receiveResult.MessageType == WebSocketMessageType.Binary)
                {                    
                    await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept binary frame", CancellationToken.None);
                }
                else
                {
                    // At this point we know we are receiving UTF-8 data, but we don't know if we've received the entire message or if we need to call `ReceiveAsync`
                    // again. We can use the `EndOfMessage` property to determine this and start a loop that will continue to call `ReceiveAsync` until we have
                    // read the entire message or exceeded our maximum message size (in which case we close the connection using an appropriate status code).
                    // The local `count` variable is used to keep track of the total number of bytes read for this message.                    
                    int count = receiveResult.Count;

                    while (receiveResult.EndOfMessage == false)
                    {
                        if (count >= maxMessageSize)
                        {                        
                            string closeMessage = string.Format("Maximum message size: {0} bytes.", maxMessageSize);
                            await socket.CloseAsync(WebSocketCloseStatus.MessageTooLarge, closeMessage, CancellationToken.None);
                            return;
                        }

                        // Our first call to `ReceiveAsync` didn't return the data for the complete message so we are calling it again. Now the benefits of using
                        // `ArraySegment` over `byte[]` are clear - we can pass an `ArraySegment` that exposes the next section of our receive buffer by using the 
                        // count of bytes that we have already read as an offset.
                        receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, count, maxMessageSize - count), CancellationToken.None);
                        count += receiveResult.Count;
                    }

                    //### Decoding the message and sending a response
                    // The complete message is now in the receive buffer so we must convert the data to a string by decoding it as UTF-8. We know that we can do this 
                    // because the WebSocket protocol only defines two different data formats - binary and UTF-8. If we wanted our server to receive text with
                    // a different encoding then we would expect to receive binary messages instead.
                    // After appending some text to the front of the message, we UTF-8 encode the string so that can get an `ArraySegment` with the appropriate data to send.
                    var receivedString = Encoding.UTF8.GetString(receiveBuffer, 0, count);
                    var echoString = "You said " + receivedString;
                    ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(echoString));

                    // Now send the data using `SendAsync` using `WebSocketMessageType.Text` as the message type.
                    await socket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, CancellationToken.None);                    

                    // The echo operation is complete. The loop will resume and `ReceiveAsync` is called again to wait for the next message.
                    //
                    // You might be wondering about exception handling - for example, what if the client aborts the connection after sending a message but before receiving the echo?
                    // Won't an exception be thrown when `SendAsync` is called? Yes, but ASP.NET will catch the exception that bubbles up from our handler, dispose of the WebSocket
                    // for us and log the error in the Application event log. Of course a real application will certainly need error handling but its unnecessary for this simple example.
                }
            }
        }

        // Since this IHttpHandler contains no state, it is safe for IIS to reuse for multiple requests.
        public bool IsReusable
        {
            get { return true; }
        }

    }
}