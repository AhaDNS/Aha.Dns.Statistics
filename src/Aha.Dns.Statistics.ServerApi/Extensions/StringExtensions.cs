namespace Aha.Dns.Statistics.ServerApi.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Return string as long, default to default long value (0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long AsLong(this string value)
        {
            return long.TryParse(value, out var result) 
                ? result 
                : default;
        }

        /// <summary>
        /// Return string as double, default to default double value (0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double AsDouble(this string value)
        {
            return double.TryParse(value, out var result)
                ? result
                : default;
        }
    }
}
