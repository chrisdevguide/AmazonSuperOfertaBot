namespace ElAhorrador.Extensions
{
    public static class StringExtensions
    {
        public static string ReplaceCommaForDot(this string value)
        {
            return value.Replace(",", ".");
        }

        public static string RemoveFirstDot(this string value)
        {
            return value.Remove(value.IndexOf("."), 1);
        }
    }
}
