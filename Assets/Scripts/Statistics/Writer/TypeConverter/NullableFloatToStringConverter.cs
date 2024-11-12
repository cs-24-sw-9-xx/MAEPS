using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Maes.Statistics.Writer.TypeConverter
{
    public class NullableFloatToStringConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            return value is float floatValue ? floatValue.ToString(row.Configuration.CultureInfo) : string.Empty;
        }
    }
}