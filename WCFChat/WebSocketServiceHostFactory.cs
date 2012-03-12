using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Microsoft.ServiceModel.WebSockets;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace WCFChat
{
    public class WebSocketServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var host = new WebSocketHost(serviceType, baseAddresses);
            var binding = WebSocketHost.CreateWebSocketBinding(https: false, subProtocol: "chatprotocol");                        
            host.AddWebSocketEndpoint(binding);            
            return host;
        }        
    }    

}