using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GIB2018API.Model
{
    [DataContract]
    public class OrderItem
    {
        [Required]
        [DataMember(Name = "product", Order = 1)]
        public Product Product { get; set; }

        [Required]
        [DataMember(Name = "quantity", Order = 2)]
        public int? Quantity { get; set; }

        [DataMember(Name = "totalCost", Order = 3)]
        public double? TotalCost { get; set; }

        [DataMember(Name = "totalTax", Order = 4)]
        public double? TotalTax { get; set; }
    }
}
