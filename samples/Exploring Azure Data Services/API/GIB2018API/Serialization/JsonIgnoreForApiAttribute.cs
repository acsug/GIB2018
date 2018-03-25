using System;

namespace GIB2018API.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonIgnoreForApiAttribute : Attribute
    {
    }
}
