using System;
using System.Runtime.Serialization;

namespace GIB2018API.Model
{
    [DataContract]
    public class Error
    {
        public Error() { }

        public Error(string detail) { Detail = detail; }

        public Error(Exception exception)
        {
            if (exception != null)
                Detail = exception.Message;
        }

        [DataMember(Name = "@context", Order = 0)]
        public string Context => "http://gib2018.org";

        [DataMember(Name = "@type", Order = 1)]
        public string Type => "Error";

        [DataMember(Name = "detail", Order = 2)]
        public string Detail { get; set; }
    }
}
