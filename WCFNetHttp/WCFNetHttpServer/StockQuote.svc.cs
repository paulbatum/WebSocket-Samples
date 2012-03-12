using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace WCFNetHttpServer
{
    [ServiceContract(CallbackContract = typeof(IStockQuoteCallback))]
    public class StockQuote
    {
        [OperationContract(IsOneWay = true)]
        public async Task StartSendingQuotes()
        {
            var callback = OperationContext.Current.GetCallbackChannel<IStockQuoteCallback>();
            var random = new Random();
            double price = 29.00;

            while (((IChannel)callback).State == CommunicationState.Opened)
            {
                await callback.SendQuote("MSFT", price);
                price += random.NextDouble();
                await Task.Delay(1000);
            }
        }

    }

    [ServiceContract]
    public interface IStockQuoteCallback
    {
        [OperationContract(IsOneWay = true)]
        Task SendQuote(string code, double value);
    }
}
