using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Server
{
    public static class EventHubClientExtensions
    {
        public static void SendObject(this EventHubClient client, object toSend)
        {
            // Serialize
            var serializedString = JsonConvert.SerializeObject(toSend);
            // Make eventData
            var ed = new EventData(Encoding.UTF8.GetBytes(serializedString));
            // Send via client
            client.Send(ed);
        }
        public static async Task SendObjectAsync(this EventHubClient client, object toSend)
        {
            // Serialize
            var serializedString = JsonConvert.SerializeObject(toSend);
            // Make eventData
            var ed = new EventData(Encoding.UTF8.GetBytes(serializedString));
            // Send via client
            await client.SendAsync(ed);
        }

        public static void SendBatchObjects(this EventHubClient client, IEnumerable<object> toSend)
        {
            // Serialize and make event data
            var edList = ConvertBatchToEventData(toSend);
            // Send via client
            client.SendBatch(edList);
        }
        public static async Task SendBatchObjectsAsync(this EventHubClient client, IEnumerable<object> toSend)
        {
            // Serialize and make event data
            var edList = ConvertBatchToEventData(toSend);
            // Send via client
            await client.SendBatchAsync(edList);
        }
        private static List<EventData> ConvertBatchToEventData(IEnumerable<object> list)
        {
            // Initialize list to capacity = #objects in batch.
            var edList = new List<EventData>(list.Count());
            foreach (object obj in list)
            {
                edList.Add(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj))));
            }
            return edList;
        }

    }
}

