using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;
using TradeHero.Contracts.Sockets.Args;

namespace TradeHero.Sockets;

internal class SocketClient : ISocketClient
{
    private readonly ILogger<SocketClient> _logger;
    private readonly IEnvironmentService _environmentService;

    private TcpClient? _tcpClient;
    private StreamReader? _streamReader;
    private StreamWriter? _streamWriter;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public event EventHandler<SocketMessageArgs>? OnReceiveMessageFromServer;
    
    public SocketClient(
        ILogger<SocketClient> logger, 
        IEnvironmentService environmentService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
    }

    public async Task ConnectAsync()
    {
        _tcpClient = new TcpClient();

        await _tcpClient.ConnectAsync(
            "127.0.0.1", 
            _environmentService.GetAppSettings().Application.Sockets.Port
        );

        _logger.LogInformation("Client connected to server. In {Method}",
            nameof(ConnectAsync));

        var stream = _tcpClient.GetStream();
        
        _streamReader = new StreamReader(stream);
        _streamWriter = new StreamWriter(stream);

        await SendMessageAsync("Ping");
        
        _ = Task.Run(async () =>
        {
            await StartMessageListenerAsync();
        });
    }

    public async Task SendMessageAsync(string message)
    {
        try
        {
            if (_streamWriter == null)
            {
                _logger.LogError("{PropertyName} is null. In {Method}",
                    nameof(_streamWriter), nameof(SendMessageAsync));
                            
                return;
            }

            await _streamWriter.WriteLineAsync(message);
            await _streamWriter.FlushAsync();

            _logger.LogInformation("Message was sent to server. In {Method}", nameof(SendMessageAsync));
        }
        catch (SocketException socketException)
        {
            if (socketException.SocketErrorCode == SocketError.Interrupted)
            {
                _logger.LogInformation("Socket stopped. In {Method}", 
                    nameof(SendMessageAsync));
                    
                return;
            }
                
            _logger.LogCritical(socketException, "In {Method}", 
                nameof(SendMessageAsync));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(SendMessageAsync));
        }
    }
    
    public void Close()
    {
        _cancellationTokenSource.Cancel();
    }

    #region Private methods

    private async Task StartMessageListenerAsync()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_streamReader == null)
                    {
                        _logger.LogError("{PropertyName} is null. In {Method}",
                            nameof(_streamReader), nameof(StartMessageListenerAsync));
                        
                        break;
                    }

                    var serverMessage = await _streamReader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(serverMessage))
                    {
                        continue;
                    }
                
                    _logger.LogInformation("Received message from server. Message: {Message} In {Method}",
                        serverMessage, nameof(StartMessageListenerAsync));

                    OnReceiveMessageFromServer?.Invoke(this, new SocketMessageArgs(serverMessage));
                }
                catch
                {
                    break;
                }
            }
        }
        catch (SocketException socketException)
        {
            if (socketException.SocketErrorCode == SocketError.Interrupted)
            {
                _logger.LogInformation("Socket stopped. In {Method}",
                    nameof(StartMessageListenerAsync));

                return;
            }

            _logger.LogCritical(socketException, "In {Method}",
                nameof(StartMessageListenerAsync));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}",
                nameof(StartMessageListenerAsync));
        }
        finally
        {
            _tcpClient?.Close();
            
            _logger.LogInformation("Client connection closed. In {Method}", 
                nameof(StartMessageListenerAsync));
        }
    }

    #endregion
}