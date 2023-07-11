namespace ElAhorrador.Extensions
{
    public static class StringExtensions
    {
        public static string ReplaceCommaForDot(this string value)
        {
            return value.Replace(",", ".");
        }
    }
}
