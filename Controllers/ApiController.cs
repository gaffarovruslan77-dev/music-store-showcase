using Microsoft.AspNetCore.Mvc;
using MusicStoreShowcase.Models;
using MusicStoreShowcase.Services;

namespace MusicStoreShowcase.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly SongGeneratorService _songGenerator;
        private readonly LocaleService _localeService;
        private readonly CoverGeneratorService _coverGenerator;
        private readonly MusicGeneratorService _musicGenerator;

        public ApiController(
            SongGeneratorService songGenerator, 
            LocaleService localeService,
            CoverGeneratorService coverGenerator,
            MusicGeneratorService musicGenerator)
        {
            _songGenerator = songGenerator;
            _localeService = localeService;
            _coverGenerator = coverGenerator;
            _musicGenerator = musicGenerator;
        }

        [HttpGet("songs")]
        public IActionResult GetSongs(
            [FromQuery] string locale = "en-US",
            [FromQuery] long seed = 12345,
            [FromQuery] double averageLikes = 5.0,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var request = new SongRequest
            {
                Locale = locale,
                Seed = seed,
                AverageLikes = averageLikes,
                Page = page,
                PageSize = pageSize
            };

            var songs = _songGenerator.GenerateSongs(request);
            
            return Ok(new
            {
                songs = songs,
                page = page,
                pageSize = pageSize,
                locale = locale,
                seed = seed,
                averageLikes = averageLikes
            });
        }

        [HttpGet("locales")]
        public IActionResult GetLocales()
        {
            var locales = _localeService.GetAvailableLocales();
            return Ok(locales);
        }
        
        [HttpGet("cover/{index}")]
        public IActionResult GetCover(
            [FromRoute] int index,
            [FromQuery] string title = "Untitled",
            [FromQuery] string artist = "Unknown",
            [FromQuery] long seed = 12345)
        {
            var combinedSeed = (int)((seed * 31 + index) % int.MaxValue);
            var coverBytes = _coverGenerator.GenerateCover(title, artist, combinedSeed);
            
            return File(coverBytes, "image/jpeg");
        }
        
        [HttpGet("audio/{index}")]
        public IActionResult GetAudio(
            [FromRoute] int index,
            [FromQuery] long seed = 12345)
        {
            var combinedSeed = (int)((seed * 31 + index) % int.MaxValue);
            var audioBytes = _musicGenerator.GenerateMusic(combinedSeed);
            
            return File(audioBytes, "audio/wav");
        }
    }
}
