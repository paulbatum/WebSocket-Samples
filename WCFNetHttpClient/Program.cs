using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WCFNetHttpClient.ServiceReference1;

namespace WCFNetHttpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new InstanceContext(new QuoteHandler());
            StockQuoteClient client = new StockQuoteClient(context);

            client.StartSendingQuotes();
            Console.ReadLine();
        }
    }

    public class QuoteHandler : StockQuoteCallback
    {

        public void SendQuote(string code, double value)
        {
            Console.WriteLine("{0}: {1}", code, value);
        }

        public async Task SendQuoteAsync(string code, double value)
        {
            Console.WriteLine("{0}: {1} (async)", code, value);
        }
    }
}
