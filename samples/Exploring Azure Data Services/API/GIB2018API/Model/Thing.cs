using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using GIB2018API.Serialization;

namespace GIB2018API.Model
{
    [DataContract]
    public class Thing : DbObject, IThing
    {
        [DataMember(Name = "@context", Order = 0)]
        public virtual string Context => "http://gib2018.org";

        [DataMember(Name = "@type", Order = 1)]
        public virtual string Type { get; }

        [DataMember(Name = "id", Order = 2)]
        public virtual string Id { get; set; }
    }

    public class DbObject
    {
        [JsonIgnoreForApi]
        [DataMember(Name = "deleted", Order = 5000)]
        public bool? Deleted { get; set; }

        [JsonIgnoreForApi]
        [DataMember(Name = "createdAt", Order = 5001)]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonIgnoreForApi]
        [DataMember(Name = "updatedAt", Order = 5002)]
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}