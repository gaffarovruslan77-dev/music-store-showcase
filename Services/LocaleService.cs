using System.Text.Json;

namespace MusicStoreShowcase.Services
{
    public class LocaleData
    {
        public string Locale { get; set; }
        public List<string> TitlePrefixes { get; set; }
        public List<string> TitleNouns { get; set; }
        public List<string> TitleAdjectives { get; set; }
        public List<string> FirstNames { get; set; }
        public List<string> LastNames { get; set; }
        public List<string> BandPrefixes { get; set; }
        public List<string> BandNouns { get; set; }
        public List<string> AlbumWords { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Reviews { get; set; }
        public List<string> LyricsLines { get; set; }
    }

    public class LocaleService
    {
        private readonly Dictionary<string, LocaleData> _locales = new();

        public LocaleService()
        {
            LoadLocales();
        }

        private void LoadLocales()
        {
            var localesPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Locales");
            var localeFiles = Directory.GetFiles(localesPath, "*.json");

            foreach (var file in localeFiles)
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<LocaleData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data != null)
                {
                    _locales[data.Locale] = data;
                }
            }
        }

        public LocaleData GetLocaleData(string locale)
        {
            return _locales.ContainsKey(locale) ? _locales[locale] : _locales["en-US"];
        }

        public List<string> GetAvailableLocales()
        {
            return _locales.Keys.ToList();
        }
    }
}
