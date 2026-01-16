using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using OpenGameVerse.Core.Common;
using OpenGameVerse.Metadata.Abstractions;
using OpenGameVerse.Metadata.Models;
using OpenGameVerse.Metadata.Serialization;

namespace OpenGameVerse.Metadata.Services;

/// <summary>
/// Client for interacting with the IGDB API.
/// </summary>
public sealed class IgdbClient : IIgdbClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string BaseUrl = "https://api.igdb.com/v4";
    private const string AuthUrl = "https://id.twitch.tv/oauth2/token";

    public IgdbClient(HttpClient httpClient, string clientId, string clientSecret)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
    }

    public async Task<Result<IgdbGame[]>> SearchGamesAsync(
        string query,
        int limit = 10,
        CancellationToken ct = default
    )
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);

            var body =
                $"search \"{query}\"; fields name,summary,cover,first_release_date,rating,rating_count,genres,themes,platforms,url; limit {limit};";
            var response = await PostAsync(
                $"{BaseUrl}/games",
                body,
                IgdbJsonContext.Default.IgdbGameArray,
                ct
            );

            return response.IsSuccess
                ? response
                : Result<IgdbGame[]>.Success(Array.Empty<IgdbGame>());
        }
        catch (Exception ex)
        {
            return Result<IgdbGame[]>.Failure($"Failed to search games: {ex.Message}");
        }
    }

    public async Task<Result<IgdbGame?>> GetGameByIdAsync(
        long gameId,
        CancellationToken ct = default
    )
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);

            var body =
                $"fields name,summary,cover,first_release_date,rating,rating_count,genres,themes,platforms,url; where id = {gameId};";
            var response = await PostAsync(
                $"{BaseUrl}/games",
                body,
                IgdbJsonContext.Default.IgdbGameArray,
                ct
            );

            var game =
                response.IsSuccess && response.Value is { Length: > 0 } ? response.Value[0] : null;

            return Result<IgdbGame?>.Success(game);
        }
        catch (Exception ex)
        {
            return Result<IgdbGame?>.Failure($"Failed to get game: {ex.Message}");
        }
    }

    public async Task<Result<IgdbCover?>> GetCoverAsync(
        long coverId,
        CancellationToken ct = default
    )
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);

            var body = $"fields game,image_id,url,width,height; where id = {coverId};";
            var response = await PostAsync(
                $"{BaseUrl}/covers",
                body,
                IgdbJsonContext.Default.IgdbCoverArray,
                ct
            );

            var cover =
                response.IsSuccess && response.Value is { Length: > 0 } ? response.Value[0] : null;

            return Result<IgdbCover?>.Success(cover);
        }
        catch (Exception ex)
        {
            return Result<IgdbCover?>.Failure($"Failed to get cover: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> DownloadImageAsync(
        string imageUrl,
        CancellationToken ct = default
    )
    {
        try
        {
            var response = await _httpClient.GetAsync(imageUrl, ct);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsByteArrayAsync(ct);
            return Result<byte[]>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure($"Failed to download image: {ex.Message}");
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        // Check if token is still valid (with 5-minute buffer)
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
        {
            return;
        }

        // Request new token
        var authRequest = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["grant_type"] = "client_credentials",
            }
        );

        var response = await _httpClient.PostAsync(AuthUrl, authRequest, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("access_token", out var tokenElement))
        {
            throw new InvalidOperationException("Failed to obtain access token from Twitch");
        }

        _accessToken = tokenElement.GetString();

        if (root.TryGetProperty("expires_in", out var expiresElement))
        {
            var expiresIn = expiresElement.GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
        }
        else
        {
            _tokenExpiry = DateTime.UtcNow.AddDays(60); // Default IGDB token lifetime
        }
    }

    private async Task<Result<T>> PostAsync<T>(
        string url,
        string body,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken ct
    )
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };

            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize(content, jsonTypeInfo);

            return result != null
                ? Result<T>.Success(result)
                : Result<T>.Failure("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"API request failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
