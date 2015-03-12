using System.Runtime.Serialization;

namespace LogEntries
{
    [DataContractAttribute]
    internal class ConnectionParameters
    {
        [DataMemberAttribute]
        public string userToken { get; set; }
        [DataMemberAttribute]
        public bool useSSL { get; set; }
        [DataMemberAttribute]
        public int port { get; set; }
    }
}
