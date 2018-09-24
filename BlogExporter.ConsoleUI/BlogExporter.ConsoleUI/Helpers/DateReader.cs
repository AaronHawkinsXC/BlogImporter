using System;
using System.Globalization;

namespace BlogExporter.ConsoleUI.Helpers
{
    public class DateReader
    {
        public static DateTime GetDateFromSitecoreFieldValue(string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue))
            {
                return DateTime.MinValue;
            }

            if (DateTimeOffset.TryParseExact(fieldValue, "yyyyMMdd'T'HHmmss'Z'",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset createdDateOffset))
            {
                return createdDateOffset.DateTime;
            }
            else if (fieldValue.IndexOf(':') > -1 && DateTimeOffset.TryParseExact(fieldValue.Substring(0, fieldValue.IndexOf(':')) + "Z", "yyyyMMdd'T'HHmmss'Z'",
                         CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out createdDateOffset))
            {
                return createdDateOffset.DateTime;
            }
            else if (DateTimeOffset.TryParseExact(fieldValue + "Z", "yyyyMMdd'T'HHmmss'Z'",
                         CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out createdDateOffset))
            {
                return createdDateOffset.DateTime;
            }
            else
            {
                Console.WriteLine($"Received unexpected date format as {fieldValue}.");
                return DateTime.MinValue;
            }
        }
    }
}
