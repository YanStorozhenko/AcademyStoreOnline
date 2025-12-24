namespace OnlineStore.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value)) 
                return value;
                
            if (value.Length <= maxLength) 
                return value;
                
            return value.Substring(0, maxLength) + suffix;
        }
    }
}