using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web;
using Microsoft.ServiceModel.WebSockets;

namespace WCFEcho
{
    public class WebSocketServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            var host = new WebSocketHost(serviceType, baseAddresses);
            host.AddWebSocketEndpoint();
            
            return host;
        }
    }    
}