using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ScreenPeekr.Services;

internal sealed class DiscordWebhookClient : IDisposable
{
    private readonly HttpClient _client = new();

    public async Task UploadScreenshotAsync(string webhookUrl, string imagePath, string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new InvalidOperationException("Discord webhook URL is not configured.");
        }

        using var form = new MultipartFormDataContent();
        var payload = new
        {
            content = $"{title}{Environment.NewLine}Sent: {DateTime.Now:HH:mm:ss}"
        };
        form.Add(new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), "payload_json");

        await using var stream = File.OpenRead(imagePath);
        using var imageContent = new StreamContent(stream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(imageContent, "file", "screenshot.png");

        using var response = await _client.PostAsync(webhookUrl, form, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _client.Dispose();
}
