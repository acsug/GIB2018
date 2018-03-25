using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace GIB2018API.Serialization
{
    public abstract class JsonConverterBase : JsonConverter
    {
        private StringEnumConverter enumConverter = new StringEnumConverter();

        protected abstract bool ShouldIgnoreProperty(PropertyInfo property);

        public abstract void WriteJsonDateTime(JsonWriter writer, object value, JsonSerializer serializer);

        public abstract object ReadJsonDateTime(string name, Type objectType, JToken jt);

        public override bool CanConvert(Type objectType)
        {
            return !enumConverter.CanConvert(objectType);
        }

        public override bool CanRead => base.CanRead;

        public override bool CanWrite => base.CanWrite;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null && serializer.NullValueHandling == NullValueHandling.Ignore)
            {
                writer.WriteNull();
                return;
            }

            var objectType = value.GetType();
            var objectTypeInfo = objectType.GetTypeInfo();
            if (IsNullableType(objectType))
            {
                var underlyingType = Nullable.GetUnderlyingType(objectType);
                objectTypeInfo = underlyingType.GetTypeInfo();
            }

            if (objectTypeInfo.IsPrimitive)
            {
                writer.WriteValue(value);
            }
            else if (objectType == typeof(string))
            {
                writer.WriteValue(value?.ToString().Trim());
            }
            else if (objectType == typeof(DateTime) || objectType == typeof(DateTimeOffset))
            {
                WriteJsonDateTime(writer, value, serializer);
            }
            else if (objectType == typeof(Uri))
            {
                writer.WriteValue(value?.ToString().Trim());
            }
            else if (objectTypeInfo.IsArray || objectTypeInfo.IsEnumerable())
            {
                writer.WriteStartArray();

                foreach (var item in (value as IEnumerable<object>))
                {
                    serializer.Serialize(writer, item);
                }

                writer.WriteEndArray();
            }
            else if (objectTypeInfo.IsClass)
            {
                writer.WriteStartObject();

                var valueProperties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).OrderBy(p => p.GetOrder());
                foreach (var property in valueProperties)
                {
                    if (!property.CanRead || ShouldIgnoreProperty(property))
                        continue;

                    object propertyValue = property.GetValue(value);

                    if (propertyValue == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                        continue;

                    var propertyName = property.GetName();

                    writer.WritePropertyName(propertyName);
                    serializer.Serialize(writer, propertyValue);
                }

                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object target = null;

            if (reader.TokenType == JsonToken.StartObject)
            {
                JToken jt = JToken.Load(reader);

                if (IsDateTimeType(objectType))
                {
                    target = ReadJsonDateTime(reader.Path, objectType, jt);
                }
                else
                {
                    var jp = jt.SelectToken("id");
                    if (jp == null)
                        jp = jt.SelectToken("@id");

                    target = CreateInstance(objectType);

                    objectType = target.GetType();

                    serializer.Populate(jt.CreateReader(), target);

                    var valueProperties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.GetOrder());
                    foreach (var property in valueProperties)
                    {
                        if (property.CanWrite && ShouldIgnoreProperty(property))
                            property.SetValue(target, null);
                    }
                }
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                target = CreateInstance(objectType);

                serializer.Populate(reader, target);
            }
            else
            {
                var jt = JToken.Load(reader);

                if (IsDateTimeType(objectType))
                    target = ReadJsonDateTime(reader.Path, objectType, jt);
                else
                    target = CreateInstance(objectType, reader.Value);
            }

            return target;
        }

        private object CreateInstance(Type objectType, object value = null)
        {
            var objectTypeInfo = objectType.GetTypeInfo();

            if (objectTypeInfo.IsPrimitive)
                return Convert.ChangeType(value, objectType);

            if (objectType == typeof(string))
                return value?.ToString().Trim();

            if (objectType == typeof(Uri))
                if (value != null)
                    return Activator.CreateInstance(objectType, value?.ToString().Trim(), UriKind.RelativeOrAbsolute);

            if (objectType == typeof(List<>))
            {
                var listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(objectType.GenericTypeArguments);

                return Activator.CreateInstance(constructedListType);
            }

            if (IsNullableType(objectType))
            {
                var underlyingType = Nullable.GetUnderlyingType(objectType);
                objectTypeInfo = underlyingType.GetTypeInfo();

                if (objectTypeInfo.IsPrimitive)
                    return Convert.ChangeType(value, underlyingType);

                else
                    return value;
            }

            if (value != null)
                return Activator.CreateInstance(objectType, value);

            return Activator.CreateInstance(objectType);
        }

        protected bool ShouldIgnoreProperty(Type objectType, string propertyName)
        {
            return ShouldIgnoreProperty(objectType.GetProperty(propertyName));
        }

        protected bool IsNullableType(Type objectType)
        {
            return objectType.Name == typeof(Nullable<>).Name;
        }

        protected bool IsDateTimeType(Type objectType)
        {
            if (IsNullableType(objectType))
                objectType = Nullable.GetUnderlyingType(objectType);

            return objectType == typeof(DateTime) ||
                   objectType == typeof(DateTimeOffset);
        }
    }
}
