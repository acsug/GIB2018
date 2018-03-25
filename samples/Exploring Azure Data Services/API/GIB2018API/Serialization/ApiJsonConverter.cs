using System;
using System.Globalization;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GIB2018API.Serialization
{
    public class ApiJsonConverter : JsonConverterBase
    {
        protected override bool ShouldIgnoreProperty(PropertyInfo property)
        {
            if (property == null)
                return false;

            return property.GetCustomAttribute<JsonIgnoreForApiAttribute>() != null;
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

                writer.WriteValue(dtValue.WithoutMilliseconds().ToString("s"));
            }
            else if (value is DateTimeOffset)
            {
                var dtoValue = (DateTimeOffset)value;

                writer.WriteValue(dtoValue.WithoutMilliseconds());
            }
        }

        public override object ReadJsonDateTime(string name, Type objectType, JToken jt)
        {
            if (objectType == null) return null;
            if (jt == null) return null;

            var value = jt.Value<DateTime>();

            if (value != null)
            {
                var stringValue = jt.Value<string>();
                var dateTime = DateTime.Parse(stringValue, null, DateTimeStyles.AdjustToUniversal);

                if (objectType == typeof(DateTime))
                {
                    return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Utc);
                }
                if (objectType == typeof(DateTimeOffset))
                {
                    return new DateTimeOffset(new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Utc));
                }
            }

            return null;
        }
    }
}
