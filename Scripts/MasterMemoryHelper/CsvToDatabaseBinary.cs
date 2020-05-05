using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MasterMemory;
using MasterMemory.Meta;

namespace MasterMemoryHelper
{
    public static class CsvToDatabaseBinary
    {
        public static void Convert(string inputPath, string outputPath, Type databaseBuilderType, Type memoryDatabaseType)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputPath);

            var builder = Activator.CreateInstance(databaseBuilderType);

            var methodInfo = memoryDatabaseType.GetMethod("GetMetaDatabase");
            var meta = methodInfo.Invoke(null, null) as MetaDatabase;
            var table = meta.GetTableInfo(fileName);

            var tableData = new List<object>();

            using (var fs = new FileStream(inputPath, FileMode.Open))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            using (var reader = new TinyCsvReader(sr))
            {
                while ((reader.ReadValuesWithHeader() is Dictionary<string, string> values))
                {
                    // create data without call constructor
                    var data = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(table.DataType);

                    var privateFields = table.DataType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (var prop in table.Properties)
                    {
                        if (values.TryGetValue(prop.NameSnakeCase, out var rawValue) || values.TryGetValue(prop.Name, out rawValue))
                        {
                            var value = ParseValue(prop.PropertyInfo.PropertyType, rawValue);

                            var backingField = privateFields.First(x => x.Name.StartsWith($"<{prop.PropertyInfo.Name}>", StringComparison.OrdinalIgnoreCase));
                            backingField.SetValue(data, value);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Not found \"{prop.NameSnakeCase}\" in \"{fileName}.csv\" header.");
                        }
                    }

                    tableData.Add(data);
                }
            }

            // add dynamic collection.
            DatabaseBuilderExtensions.AppendDynamic(builder as DatabaseBuilderBase, table.DataType, tableData);

            using (var fs = new FileStream($"{Path.Combine(outputPath, fileName)}.bin", FileMode.Create))
            {
                var writeToStream = databaseBuilderType.GetMethod("WriteToStream");
                writeToStream.Invoke(builder, new object[] { fs });
            }
        }

        private static object ParseValue(Type type, string rawValue)
        {
            if (type == typeof(string)) return rawValue;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrWhiteSpace(rawValue)) return null;
                return ParseValue(type.GenericTypeArguments[0], rawValue);
            }

            if (type.IsEnum)
            {
                var value = Enum.Parse(type, rawValue);
                return value;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    // True/False or 0,1
                    if (int.TryParse(rawValue, out var intBool))
                    {
                        return System.Convert.ToBoolean(intBool);
                    }
                    return bool.Parse(rawValue);
                case TypeCode.Char:
                    return char.Parse(rawValue);
                case TypeCode.SByte:
                    return sbyte.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Byte:
                    return byte.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Int16:
                    return short.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.UInt16:
                    return ushort.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Int32:
                    return int.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.UInt32:
                    return uint.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Int64:
                    return long.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.UInt64:
                    return ulong.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Single:
                    return float.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Double:
                    return double.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Decimal:
                    return decimal.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.DateTime:
                    return DateTime.Parse(rawValue, CultureInfo.InvariantCulture);
                default:
                    if (type == typeof(DateTimeOffset))
                    {
                        return DateTimeOffset.Parse(rawValue, CultureInfo.InvariantCulture);
                    }
                    else if (type == typeof(TimeSpan))
                    {
                        return TimeSpan.Parse(rawValue, CultureInfo.InvariantCulture);
                    }
                    else if (type == typeof(Guid))
                    {
                        return Guid.Parse(rawValue);
                    }

                    // or other your custom parsing.
                    throw new NotSupportedException();
            }
        }
    }
}
