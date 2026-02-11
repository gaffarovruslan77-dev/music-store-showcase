namespace MusicStoreShowcase.Models
{
    public class SongRequest
    {
        public string Locale { get; set; } = "en-US";
        public long Seed { get; set; }
        public double AverageLikes { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
