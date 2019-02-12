using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Jarvis.Models
{
    [DataContract]
    public class ErrorModel
    {
        [DataMember]
        public int ErrorId { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
        [DataMember]
        public int UserLogId { get; set; }
        [DataMember]
        public string ActivityId { get; set; }
        [DataMember]
        public System.DateTime CreatedDate { get; set; }
    }
}