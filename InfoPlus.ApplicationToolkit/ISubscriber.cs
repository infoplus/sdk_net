using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Studio.Foundation.Json;
using InfoPlus.ApplicationToolkit.Entities;

namespace InfoPlus.ApplicationToolkit
{
    [ServiceContract]
    public interface ISubscriber
    {
        [OperationContract]
        string OnEvent(string verify, string version, string eventType, string eventData);
    }

    public enum EndPointTypes
    {
        WebService, REST, WCF
    }

    public class EndPointDescription
    {
        public string Address { get; set; }
        public EndPointTypes Binding { get; set; }
        /// <summary>
        /// When Binding is WebService: Contract is the MethodName
        /// When Binding is REST: Contract is POST/GET(maybe length-limited)
        /// When Binding is WCF: Not supported.
        /// </summary>
        public string Contract { get; set; }

        public string Identification { get; set; }
    }

}