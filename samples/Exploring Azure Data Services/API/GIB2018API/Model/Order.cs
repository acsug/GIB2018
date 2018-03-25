using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GIB2018API.Model
{
    [DataContract]
    public class Order : Thing
    {
        [DataMember(Name = "@type", Order = 1)]
        public override string Type => "Order";

        [Required]
        [DataMember(Name = "customer", Order = 3)]
        public Customer Customer { get; set; }

        [DataMember(Name = "orderDate", Order = 4)]
        public DateTime? OrderDate { get; set; }

        [Required]
        [DataMember(Name = "items", Order = 5)]
        public List<OrderItem> Items { get; set; }

        [DataMember(Name = "totalCost", Order = 6)]
        public double? TotalCost { get; set; }

        [DataMember(Name = "totalTax", Order = 7)]
        public double? TotalTax { get; set; }
    }
}
