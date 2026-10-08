using System.Text.Json;
using CrisGameRoom.Configuration;
using SocketIOClient;
using SocketIOClientSocket = SocketIOClient.SocketIO;

namespace CrisGameRoom.Services.Realtime;

public sealed class GameRoomRealtimeClient : IAsyncDisposable
{
    private SocketIOClientSocket? _socket;
    private string? _token;

    public bool IsConnected =>
        _socket?.Connected == true;

    public event Action<string>? StatusChanged;
    public event Action<string, JsonElement>? ServerEvent;

    public async Task ConnectAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException(
                "Tokenul realtime lipsește.",
                nameof(accessToken));

        await DisconnectAsync();

        _token = accessToken;

        _socket = new SocketIOClientSocket(
            ServerConfiguration.RealtimeBaseUrl,
            new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Reconnection = true,
                ReconnectionAttempts = int.MaxValue,
                ReconnectionDelay = 1000,
                Auth = new Dictionary<string, string>
                {
                    ["token"] = accessToken
                }
            });

        RegisterEvents(_socket);

        StatusChanged?.Invoke("Se conectează la server...");

        await _socket.ConnectAsync();

        cancellationToken.ThrowIfCancellationRequested();
    }

    private void RegisterEvents(SocketIOClientSocket socket)
    {
        socket.OnConnected += (_, _) =>
            StatusChanged?.Invoke("Conectat la server.");

        socket.OnDisconnected += (_, reason) =>
            StatusChanged?.Invoke(
                $"Conexiune realtime închisă: {reason}");

        socket.OnError += (_, error) =>
            StatusChanged?.Invoke(
                $"Eroare realtime: {error}");

        Register("realtime:connected");
        Register("presence:update");

        Register("room.updated");
        Register("room.participant_joined");
        Register("room.participant_left");
        Register("room.participant_muted");
        Register("room.participant_unmuted");
        Register("room.participant_kicked");
        Register("room.participant_blocked");
        Register("room.owner_changed");
        Register("room.closed");
        Register("room.deleted");
        Register("room.message_created");
        Register("room.message_deleted");

        Register("room:greeting");
        Register("room:left");
        Register("room:error");

        Register("room.access_requested");
        Register("room.access_accepted");
        Register("room.access_rejected");
        Register("room.invited");
    }

    private void Register(string eventName)
    {
        if (_socket is null)
            return;

        _socket.On(eventName, response =>
        {
            try
            {
                var json = response.GetValue<JsonElement>();
                ServerEvent?.Invoke(eventName, json);
            }
            catch
            {
                ServerEvent?.Invoke(
                    eventName,
                    JsonSerializer.SerializeToElement(
                        response.ToString()));
            }
        });
    }

    public async Task JoinRoomAsync(string code)
    {
        EnsureConnected();

        await _socket!.EmitAsync(
            "room:join",
            new
            {
                code = code.Trim().ToUpperInvariant()
            });
    }

    public async Task RequestRoomStateAsync()
    {
        EnsureConnected();
        await _socket!.EmitAsync("room:state");
    }

    public async Task LeaveRoomAsync(string code)
    {
        EnsureConnected();

        await _socket!.EmitAsync(
            "room:leave",
            new
            {
                code = code.Trim().ToUpperInvariant()
            });
    }

    public async Task SendChatAsync(
        string code,
        string text)
    {
        EnsureConnected();

        await _socket!.EmitAsync(
            "room:chat:send",
            new
            {
                code = code.Trim().ToUpperInvariant(),
                content = text
            });
    }

    public async Task ListChatAsync(
        string code,
        int limit = 100)
    {
        EnsureConnected();

        await _socket!.EmitAsync(
            "room:chat:list",
            new
            {
                code = code.Trim().ToUpperInvariant(),
                limit
            });
    }

    private void EnsureConnected()
    {
        if (!IsConnected || _socket is null)
            throw new InvalidOperationException(
                "Clientul realtime nu este conectat.");
    }

    public async Task DisconnectAsync()
    {
        if (_socket is not null)
        {
            try
            {
                await _socket.DisconnectAsync();
            }
            catch
            {
            }

            _socket.Dispose();
            _socket = null;
        }

        _token = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
