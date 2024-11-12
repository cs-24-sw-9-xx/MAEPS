using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Maes.Statistics.Writer.TypeConverter
{
    public class BooleanNullableToBinaryConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            return value is bool boolValue ? (boolValue ? "1" : "0") : string.Empty;
        }
    }
}