using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CrisGameRoom.Configuration;
using CrisGameRoom.Models;

namespace CrisGameRoom.Services.Api;

public sealed class GameRoomApiClient
{
    private readonly HttpClient _http;
    private string? _accessToken;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GameRoomApiClient()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(ServerConfiguration.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(_accessToken);

    public void SetAccessToken(string? token)
    {
        _accessToken = string.IsNullOrWhiteSpace(token)
            ? null
            : token.Trim();

        _http.DefaultRequestHeaders.Authorization =
            _accessToken is null
                ? null
                : new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    public async Task<AuthResult> LoginAsync(
        string identifier,
        string password,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            identifier,
            password
        };

        using var response = await _http.PostAsJsonAsync(
            "auth/login",
            body,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response);

        var result =
            await response.Content.ReadFromJsonAsync<AuthResult>(
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Răspunsul de autentificare este gol.");

        SetAccessToken(result.AccessToken);

        return result;
    }

    public async Task<PublicUser> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            "auth/me",
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<PublicUser>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Serverul nu a returnat utilizatorul autentificat.");
    }

    public async Task<List<Room>> GetRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            "rooms",
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<List<Room>>(
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<Room> GetRoomAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(
            $"rooms/{Uri.EscapeDataString(code)}",
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<Room>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Camera nu a putut fi încărcată.");
    }

    public async Task<Room> CreateRoomAsync(
        string name,
        string? description = null,
        string? greeting = null,
        string? rules = null,
        string visibility = "PUBLIC",
        int? maxPlayers = null,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            name,
            description,
            greeting,
            rules,
            visibility,
            maxPlayers
        };

        using var response = await _http.PostAsJsonAsync(
            "rooms",
            body,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<Room>(
                   JsonOptions,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Serverul nu a returnat camera creată.");
    }

    public async Task<JsonElement> JoinRoomAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"rooms/{Uri.EscapeDataString(code)}/join",
            content: null,
            cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<JsonElement>(
            JsonOptions,
            cancellationToken);
    }

    public async Task LeaveRoomAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync(
            $"rooms/{Uri.EscapeDataString(code)}/leave",
            content: null,
            cancellationToken);

        await EnsureSuccessAsync(response);
    }

    public async Task LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
            return;

        using var response = await _http.PostAsync(
            "auth/logout",
            content: null,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.NoContent &&
            !response.IsSuccessStatusCode)
        {
            await EnsureSuccessAsync(response);
        }

        SetAccessToken(null);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = "";

        try
        {
            body = await response.Content.ReadAsStringAsync();
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(body))
            body = response.ReasonPhrase ?? "Eroare HTTP.";

        throw new HttpRequestException(
            $"Serverul a răspuns cu {(int)response.StatusCode}: {body}");
    }
}
