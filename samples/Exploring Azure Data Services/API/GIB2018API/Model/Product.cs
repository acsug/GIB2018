using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GIB2018API.Model
{
    [DataContract]
    public class Product : Thing
    {
        [DataMember(Name = "@type", Order = 1)]
        public override string Type => "Product";

        [Required]
        [DataMember(Name = "name", Order = 3)]
        public string Name { get; set; }

        [Required]
        [DataMember(Name = "cost", Order = 4)]
        public double? Cost { get; set; }

        [Required]
        [DataMember(Name = "tax", Order = 5)]
        public double? Tax { get; set; }
    }
}
