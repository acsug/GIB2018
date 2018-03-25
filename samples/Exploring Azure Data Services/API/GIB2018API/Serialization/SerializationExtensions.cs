using System;
using System.Reflection;
using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace GIB2018API.Serialization
{
    internal static class SerializationExtensions
    {
        private static DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static bool IsEnumerable(this TypeInfo typeInfo)
        {
            return (typeInfo.GetInterface("IEnumerable") != null);
        }

        public static string GetName(this PropertyInfo property)
        {
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();

            if (jsonPropertyAttribute != null)
                if (!string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
                    return jsonPropertyAttribute.PropertyName;

            var dataMemberAttribute = property.GetCustomAttribute<DataMemberAttribute>();

            if (dataMemberAttribute != null)
                if (!string.IsNullOrEmpty(dataMemberAttribute.Name))
                    return dataMemberAttribute.Name;

            return property.Name;
        }

        public static int GetOrder(this PropertyInfo property)
        {
            var dataMemberAttribute = property.GetCustomAttribute<DataMemberAttribute>();

            if (dataMemberAttribute != null)
                return dataMemberAttribute.Order;

            return 0;
        }

        public static long ToUnixEpoch(this DateTime value)
        {
            if (value == null) return int.MinValue;

            value = value.ToUniversalTime();

            TimeSpan epochTimeSpan = value - EPOCH;
            return (long)epochTimeSpan.TotalSeconds;
        }

        public static long ToUnixEpoch(this DateTimeOffset value)
        {
            if (value == null) return int.MinValue;

            return value.UtcDateTime.ToUnixEpoch();
        }

        public static DateTime DateTimeFromUnixEpoch(this long value)
        {
            return EPOCH.AddSeconds(value);
        }

        public static DateTimeOffset DateTimeOffsetFromUnixEpoch(this long value)
        {
            var result = value.DateTimeFromUnixEpoch();
            return new DateTimeOffset(result.Ticks, TimeSpan.Zero);
        }

        public static DateTime WithoutMilliseconds(this DateTime value)
        {
            return new DateTime(((value.Ticks / 10000000) * 10000000), value.Kind);
        }

        public static DateTimeOffset WithoutMilliseconds(this DateTimeOffset value)
        {
            return new DateTimeOffset(((value.Ticks / 10000000) * 10000000), value.Offset);
        }
    }
}
