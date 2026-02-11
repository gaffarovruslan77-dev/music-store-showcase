namespace MusicStoreShowcase.Models
{
    public class Song
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public int Likes { get; set; }
        public string CoverUrl { get; set; }
        public string AudioUrl { get; set; }
        public string Review { get; set; }
        public string Lyrics { get; set; }
    }
}
