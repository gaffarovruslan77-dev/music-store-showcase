using MusicStoreShowcase.Models;
using Bogus;

namespace MusicStoreShowcase.Services
{
    public class SongGeneratorService
    {
        private readonly LocaleService _localeService;

        public SongGeneratorService(LocaleService localeService)
        {
            _localeService = localeService;
        }

        public List<Song> GenerateSongs(SongRequest request)
        {
            var songs = new List<Song>();
            var localeData = _localeService.GetLocaleData(request.Locale);
            
            var startIndex = (request.Page - 1) * request.PageSize + 1;
            
            for (int i = 0; i < request.PageSize; i++)
            {
                var index = startIndex + i;
                var combinedSeed = CombineSeed(request.Seed, index);
                
                var song = GenerateSong(index, combinedSeed, localeData, request.AverageLikes, request.Seed);
                songs.Add(song);
            }
            
            return songs;
        }

        private Song GenerateSong(int index, int seed, LocaleData localeData, double averageLikes, long baseSeed)
        {
            var faker = new Faker { Random = new Randomizer(seed) };
            
            var title = GenerateTitle(faker, localeData);
            var artist = GenerateArtist(faker, localeData);
            
            var song = new Song
            {
                Index = index,
                Title = title,
                Artist = artist,
                Album = GenerateAlbum(faker, localeData),
                Genre = faker.PickRandom(localeData.Genres),
                Likes = GenerateLikes(faker, averageLikes),
                CoverUrl = $"/api/cover/{index}?title={Uri.EscapeDataString(title)}&artist={Uri.EscapeDataString(artist)}&seed={baseSeed}",
                AudioUrl = $"/api/audio/{index}?seed={baseSeed}",
                Review = GenerateReview(faker, localeData),
                Lyrics = GenerateLyrics(faker, localeData)
            };
            
            return song;
        }

        private string GenerateTitle(Faker faker, LocaleData localeData)
        {
            var prefix = faker.PickRandom(localeData.TitlePrefixes);
            var adjective = faker.PickRandom(localeData.TitleAdjectives);
            var noun = faker.PickRandom(localeData.TitleNouns);
            
            var templates = new[]
            {
                $"{prefix} {noun}",
                $"{adjective} {noun}",
                $"{prefix} {adjective} {noun}",
                $"{noun}"
            };
            
            return faker.PickRandom(templates);
        }

        private string GenerateArtist(Faker faker, LocaleData localeData)
        {
            var isBand = faker.Random.Bool();
            
            if (isBand)
            {
                var prefix = faker.PickRandom(localeData.BandPrefixes);
                var noun = faker.PickRandom(localeData.BandNouns);
                return $"{prefix} {noun}";
            }
            else
            {
                var firstName = faker.PickRandom(localeData.FirstNames);
                var lastName = faker.PickRandom(localeData.LastNames);
                return $"{firstName} {lastName}";
            }
        }

        private string GenerateAlbum(Faker faker, LocaleData localeData)
        {
            var isSingle = faker.Random.Bool();
            
            if (isSingle)
            {
                return "Single";
            }
            
            var word1 = faker.PickRandom(localeData.AlbumWords);
            var word2 = faker.PickRandom(localeData.AlbumWords);
            
            return faker.Random.Bool() ? word1 : $"{word1} {word2}";
        }

        private int GenerateLikes(Faker faker, double averageLikes)
        {
            // Handle edge cases
            if (averageLikes == 0) return 0;
            if (averageLikes >= 10) return 10;
            
            // Probabilistic implementation for fractional values
            int baseLikes = (int)Math.Floor(averageLikes);
            double fractionalPart = averageLikes - baseLikes;
            
            int likes = baseLikes;
            
            // Add 1 more like based on fractional probability
            if (faker.Random.Double() < fractionalPart)
            {
                likes++;
            }
            
            // No variance for exact integer values (per requirements)
            // Example: averageLikes=5.0 should give exactly 5 likes for all songs
            // Example: averageLikes=0.5 should give 0 or 1 with 50/50 probability
            
            return Math.Max(0, Math.Min(10, likes));
        }

        private string GenerateReview(Faker faker, LocaleData localeData)
        {
            return faker.PickRandom(localeData.Reviews);
        }

        private string GenerateLyrics(Faker faker, LocaleData localeData)
        {
            var lines = new List<string>();
            
            // Generate 8 lines of lyrics
            for (int i = 0; i < 8; i++)
            {
                lines.Add(faker.PickRandom(localeData.LyricsLines));
            }
            
            return string.Join("\n", lines);
        }

        private int CombineSeed(long baseSeed, int index)
        {
            return (int)((baseSeed * 31 + index) % int.MaxValue);
        }
    }
}
