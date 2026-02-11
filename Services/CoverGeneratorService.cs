using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using Bogus;

namespace MusicStoreShowcase.Services
{
    public class CoverGeneratorService
    {
        public byte[] GenerateCover(string title, string artist, int seed)
        {
            var faker = new Faker { Random = new Randomizer(seed) };
            
            using var image = new Image<Rgba32>(400, 400);
            
            var color1 = GenerateColor(faker);
            var color2 = GenerateColor(faker);
            
            image.Mutate(ctx =>
            {
                for (int y = 0; y < 400; y++)
                {
                    float ratio = y / 400f;
                    var r = (byte)(color1.R * (1 - ratio) + color2.R * ratio);
                    var g = (byte)(color1.G * (1 - ratio) + color2.G * ratio);
                    var b = (byte)(color1.B * (1 - ratio) + color2.B * ratio);
                    
                    var lineColor = new Rgba32(r, g, b);
                    ctx.Fill(Color.FromPixel(lineColor), new RectangleF(0, y, 400, 1));
                }
                
                AddRandomShapes(ctx, faker);
            });
            
            AddText(image, title, artist);
            
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms);
            return ms.ToArray();
        }
        
        private Rgba32 GenerateColor(Faker faker)
        {
            return new Rgba32(
                (byte)faker.Random.Int(50, 255),
                (byte)faker.Random.Int(50, 255),
                (byte)faker.Random.Int(50, 255)
            );
        }
        
        private void AddRandomShapes(IImageProcessingContext ctx, Faker faker)
        {
            int shapeCount = faker.Random.Int(3, 8);
            
            for (int i = 0; i < shapeCount; i++)
            {
                var color = GenerateColor(faker);
                var alpha = (byte)faker.Random.Int(20, 80);
                var shapeColor = new Rgba32(color.R, color.G, color.B, alpha);
                
                var x = faker.Random.Float(0, 350);
                var y = faker.Random.Float(0, 350);
                var width = faker.Random.Float(30, 100);
                var height = faker.Random.Float(30, 100);
                
                ctx.Fill(Color.FromPixel(shapeColor), new RectangleF(x, y, width, height));
            }
        }
        
        private void AddText(Image image, string title, string artist)
        {
            Font titleFont;
            Font artistFont;
            
            try
            {
                var fontNames = new[] { 
                    "DejaVu Sans", 
                    "Liberation Sans", 
                    "FreeSans",
                    "Noto Sans",
                    "Ubuntu",
                    "Arial"
                };
                
                FontFamily fontFamily = SystemFonts.Families.First();
                
                foreach (var fontName in fontNames)
                {
                    try
                    {
                        fontFamily = SystemFonts.Get(fontName);
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
                
                titleFont = fontFamily.CreateFont(32, FontStyle.Bold);
                artistFont = fontFamily.CreateFont(20, FontStyle.Regular);
            }
            catch
            {
                var fontFamily = SystemFonts.Families.First();
                titleFont = fontFamily.CreateFont(32, FontStyle.Bold);
                artistFont = fontFamily.CreateFont(20, FontStyle.Regular);
            }
            
            image.Mutate(ctx =>
            {
                var overlayColor = new Rgba32(0, 0, 0, 100);
                ctx.Fill(Color.FromPixel(overlayColor), new RectangleF(0, 280, 400, 120));
                
                var titleOptions = new RichTextOptions(titleFont)
                {
                    Origin = new PointF(200, 310),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = 360
                };
                
                ctx.DrawText(titleOptions, WrapText(title, 25), Color.White);
                
                var artistOptions = new RichTextOptions(artistFont)
                {
                    Origin = new PointF(200, 360),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = 360
                };
                
                ctx.DrawText(artistOptions, WrapText(artist, 30), Color.WhiteSmoke);
            });
        }
        
        private string WrapText(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
