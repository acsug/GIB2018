using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GIB2018API.Model
{
    [DataContract]
    public class Customer : Thing
    {
        [DataMember(Name = "@type", Order = 1)]
        public override string Type => "Customer";

        [Required]
        [DataMember(Name = "name", Order = 3)]
        public string Name { get; set; }
    }
}
