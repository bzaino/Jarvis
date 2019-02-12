using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Jarvis.Models
{

    [DataContract]
    public class UserLogModel
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string UserId { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Channel { get; set; }
        [DataMember]
        public System.DateTime ActivityDate { get; set; }
        [DataMember]
        public string Message { get; set; }
    }
}