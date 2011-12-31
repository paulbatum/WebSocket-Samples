using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Client
{
    class Client
    {
        private static object consoleLock = new object();
        private const int sendChunkSize = 64;
        private const int receiveChunkSize = 256;
        private const bool verbose = true;
        private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(1000);

        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            Connect("ws://localhost/wsDemo").Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static async Task Connect(string uri)
        {
            StreamWebSocket webSocket = null;

            try
            {
                webSocket = new StreamWebSocket();
                await webSocket.ConnectAsync(new Uri(uri));                
                await Task.WhenAll(Receive(webSocket), Send(webSocket));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Close();
                Console.WriteLine();
                Console.WriteLine("WebSocket closed.");
            }
        }

        private static async Task Send(StreamWebSocket webSocket)
        {
            var random = new Random();
            byte[] buffer = new byte[sendChunkSize];            

            while (true)
            {
                random.NextBytes(buffer);

                await webSocket.OutputStream.WriteAsync(buffer.AsBuffer());
                LogStatus(false, buffer);
   
                if(delay > TimeSpan.Zero)
                    await Task.Delay(delay);
            }
        }

        private static async Task Receive(StreamWebSocket webSocket)
        {
            byte[] buffer = new byte[receiveChunkSize];
            while (true)
            {                
                await webSocket.InputStream.ReadAsync(buffer.AsBuffer(), (uint)buffer.Length, InputStreamOptions.None);                
                LogStatus(true, buffer);
            }
        }

        private static void LogStatus(bool receiving, byte[] buffer)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = receiving ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.WriteLine("{0} {1} bytes... ", receiving ? "Received" : "Sent", buffer.Length);

                if (verbose)
                    Console.WriteLine(BitConverter.ToString(buffer));

                Console.ResetColor();
            }
        }
    }

   
}
