using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

public static class MinecraftHeadFetcher
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> GetUUIDAsync(string username)
    {
        var response = await client.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{username}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("id", out var idElement))
        {
            return idElement.GetString();
        }

        return null;
    }

    public static async Task<Stream> GetHeadImageAsync(string uuid)
    {
        var imageBytes = await client.GetByteArrayAsync($"https://crafatar.com/avatars/{uuid}?size=32&overlay");
        return new MemoryStream(imageBytes);
    }

}
