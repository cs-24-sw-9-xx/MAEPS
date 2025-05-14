using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Maes.Statistics.Writer.TypeConverter
{
    public sealed class NullableIntToStringConverter : DefaultTypeConverter
    {
        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            return value is int intValue ? intValue.ToString(row.Configuration.CultureInfo) : string.Empty;
        }
    }
}