#WebSocket Samples
This repository is a collection of WebSocket samples for .NET 4.5

##Getting Started
To be able to compile and run you will need the [Windows 8 release preview](http://windows.microsoft.com/en-US/windows-8/download). Detailed instructions on the setup process are [here](http://www.paulbatum.com/2011/09/getting-started-with-websockets-in.html).

##Included Samples

### ASP.NET WebSocket Echo
The AspNetWebSocketEcho sample is a simple WebSocket echo server implemented using an IHttpHandler. Includes [annotated source](http://paulbatum.github.com/WebSocket-Samples/AspNetWebSocketEcho/).

### HttpListener WebSocket + ClientWebSocket
This sample is another echo sample but this one is a binary streaming echo using the ClientWebSocket type (new in Win8 Release Preview) as the client and HttpListener as the server.

### WCF Echo
Yet another echo sample, this one is the WCF version of the AspNetWebSocketEcho.

### ASP.NET Chat
The AspNetChat sample demonstrates using the Broadcast method on WebSocketCollection for implementing a simple chat server.

### WCF Chat
This is the WCF version of the chat app. Very similar to ASP.NET Chat.

### WCF NetHttpBinding
The WCFNetHttpBinding sample demonstrates traditional WCF service based development using the new NetHttpBinding. This binding uses WebSockets automatically when used with a duplex contract (i.e. a contract that has a callback contract).

##Other Samples
If you are looking for Push Frenzy, the game I demonstrated in my [BUILD 2011 talk](http://channel9.msdn.com/Events/BUILD/BUILD2011/SAC-807T), it has its own repository:

* [Push Frenzy](https://github.com/paulbatum/PushFrenzy)