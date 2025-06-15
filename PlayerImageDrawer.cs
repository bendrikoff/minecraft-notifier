using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

public static class PlayerImageDrawer
{
    public static async Task<MemoryStream> DrawPlayerListAsync(List<(string Name, Stream HeadStream)> players, int maxPlayers)
    {
        int width = 300;
        int height = System.Math.Max(60 + players.Count * 40, 100); // адаптируем под количество игроков

        Image<Rgba32> background = Image.Load<Rgba32>(Path.Combine(AppContext.BaseDirectory, "Images", "Background.png"));


        var image = new Image<Rgba32>(width, height);
        background.Mutate(x => x.Resize(width, height));

        image.Mutate(ctx => ctx.DrawImage(background, 1f));

        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "Minecraftia-Regular.ttf");
        var fontCollection = new FontCollection();
        var minecraftFontFamily = fontCollection.Add(fontPath);
        var font = minecraftFontFamily.CreateFont(16);
        var smallFont = minecraftFontFamily.CreateFont(14);

        // Заголовок
        string header = $"Игроков онлайн: {players.Count}/{maxPlayers}";
        image.Mutate(ctx => ctx.DrawText(header, font, Color.White, new PointF(10, 10)));

        int y = 40;

        foreach (var (name, headStream) in players)
        {
            headStream.Position = 0;
            using var headImage = await Image.LoadAsync<Rgba32>(headStream);
            headImage.Mutate(x => x.Resize(32, 32));
            image.Mutate(ctx => ctx.DrawImage(headImage, new Point(10, y), 1f));

            image.Mutate(ctx => ctx.DrawText($"- {name}", smallFont, Color.White, new PointF(50, y + 6)));

            y += 40;
        }

        var outputStream = new MemoryStream();
        await image.SaveAsPngAsync(outputStream);
        outputStream.Position = 0;
        return outputStream;
    }
}
