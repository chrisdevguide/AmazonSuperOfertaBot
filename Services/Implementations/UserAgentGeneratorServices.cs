namespace AmazonSuperOfertaBot.Services.Implementations
{
    public class UserAgentGeneratorServices
    {

        public string GenerateRandomUserAgent()
        {
            // List of possible user-agent components
            string[] browsers = {
                "Chrome", "Firefox", "Safari", "Edge", "Opera", "Internet Explorer"
            };
            string[] operatingSystems = {
                "Windows NT 10.0", "Windows NT 6.3", "Windows NT 6.1", "Macintosh", "X11", "Linux"
            };

            // Generate a random browser and operating system
            Random random = new Random();
            int browserIndex = random.Next(browsers.Length);
            int osIndex = random.Next(operatingSystems.Length);

            // Construct the user-agent string
            string userAgent = $"{browsers[browserIndex]}/{random.Next(50, 100)}.0 " +
                $"({operatingSystems[osIndex]}; {GetRandomPlatform()}; {GetRandomLanguage()})";

            return userAgent;
        }

        private string GetRandomPlatform()
        {
            string[] platforms = { "Win64; x64", "WOW64", "Win32", "x86_64", "i686" };

            Random random = new Random();
            int index = random.Next(platforms.Length);

            return platforms[index];
        }

        private string GetRandomLanguage()
        {
            string[] languages = { "en-US", "en-GB", "fr-FR", "es-ES", "de-DE", "ja-JP" };

            Random random = new();
            int index = random.Next(languages.Length);

            return languages[index];
        }
    }
}
