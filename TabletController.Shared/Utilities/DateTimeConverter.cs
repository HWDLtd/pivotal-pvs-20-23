using System.Text.Json;
using System.Text.Json.Serialization;

namespace TabletController.Shared.Utilities
{
    /// <summary>
    /// Custom JSON converter for DateTime that uses ISO 8601 format.
    /// </summary>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return DateTime.MinValue;

            // Handle the format with space before timezone: "2026-01-16T16:12:18.7325862 00:00"
            // Replace space with + or - for proper parsing
            if (value.Contains(" 00:00") || value.Contains(" +") || value.Contains(" -"))
            {
                // Replace space before timezone with proper format
                value = value.Replace(" 00:00", "+00:00")
                             .Replace(" +", "+")
                             .Replace(" -", "-");
            }

            // Try parsing with ISO 8601 format
            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                return result;

            // Try parsing with various formats
            var formats = new[]
            {
                "yyyy-MM-ddTHH:mm:ss.fffffffzzz",
                "yyyy-MM-ddTHH:mm:ss.fffffff",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fffZ"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(value, format, null, System.Globalization.DateTimeStyles.None, out result))
                    return result;
            }

            // Fallback to standard parse
            return DateTime.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Write in ISO 8601 format with Z for UTC
            var utcValue = value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
                : value.ToUniversalTime();
            writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}
