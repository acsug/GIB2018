using System;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GIB2018API.Serialization
{
    public class CosmosDbJsonConverter : JsonConverterBase
    {
        protected override bool ShouldIgnoreProperty(PropertyInfo property)
        {
            if (property == null)
                return false;

            return property.GetCustomAttribute<JsonIgnoreForDbAttribute>() != null;
        }

        public override void WriteJsonDateTime(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                if (serializer.NullValueHandling == NullValueHandling.Include)
                    writer.WriteValue((string)null);
            }
            else if (value is DateTime)
            {
                var dtValue = (DateTime)value;

                writer.WriteStartObject();

                writer.WritePropertyName("date");
                writer.WriteValue(dtValue.WithoutMilliseconds().ToString("s"));

                writer.WritePropertyName("epoch");
                writer.WriteValue(dtValue.ToUnixEpoch());

                writer.WriteEndObject();
            }
            else if (value is DateTimeOffset)
            {
                var dtoValue = (DateTimeOffset)value;

                writer.WriteStartObject();

                writer.WritePropertyName("date");
                writer.WriteValue(dtoValue.WithoutMilliseconds());

                writer.WritePropertyName("epoch");
                writer.WriteValue(dtoValue.ToUnixEpoch());

                writer.WriteEndObject();
            }
        }

        public override object ReadJsonDateTime(string name, Type objectType, JToken jt)
        {
            if (objectType == null) return null;
            if (jt == null) return null;

            if (IsNullableType(objectType))
                objectType = Nullable.GetUnderlyingType(objectType);

            var epoch = jt.SelectToken("epoch");

            if (epoch != null)
            {
                var epochValue = epoch.Value<long>();

                if (objectType == typeof(DateTime))
                    return epochValue.DateTimeFromUnixEpoch();

                if (objectType == typeof(DateTimeOffset))
                    return epochValue.DateTimeOffsetFromUnixEpoch();
            }

            var value = jt.Value<DateTime>();

            if (value != null)
            {
                value = value.ToUniversalTime();

                if (objectType == typeof(DateTime))
                    return value;

                if (objectType == typeof(DateTimeOffset))
                    return new DateTimeOffset(value.Ticks, TimeSpan.Zero);
            }

            return null;
        }
    }
}
