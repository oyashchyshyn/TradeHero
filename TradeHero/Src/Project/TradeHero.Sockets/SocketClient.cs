using System.Net.Sockets;
using System.Text;
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

    public void Connect()
    {
        Task.Run(async () =>
        {
            try
            {
                _tcpClient = new TcpClient();
        
                await _tcpClient.ConnectAsync(
                    _environmentService.GetLocalIpAddress(), 
                    _environmentService.GetAppSettings().Application.Sockets.Port
                );

                _logger.LogInformation("Client connected to server. In {Method}",
                    nameof(Connect));
                
                SendMessage("Ping");
                
                var bytes = new byte[1024];             
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await using var stream = _tcpClient.GetStream();
                    int length;
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) 
                    { 						
                        var incomingData = new byte[length]; 						
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        var serverMessage = Encoding.ASCII.GetString(incomingData);
                            
                        _logger.LogInformation("Received message from server. Message: {Message} In {Method}",
                            serverMessage, nameof(Connect));

                        OnReceiveMessageFromServer?.Invoke(this, new SocketMessageArgs(serverMessage));
                    }
                }
            }
            catch (SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.Interrupted)
                {
                    _logger.LogInformation("Socket stopped. In {Method}",
                        nameof(Connect));

                    return;
                }

                _logger.LogCritical(socketException, "In {Method}",
                    nameof(Connect));
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "In {Method}",
                    nameof(Connect));
            }
            finally
            {
                _tcpClient?.Close();
                
                _logger.LogInformation("Client connection closed. In {Method}", 
                    nameof(Connect));
            }
        });
    }

    public void SendMessage(string message)
    {
        try
        {
            if (_tcpClient == null)
            {
                _logger.LogError("Cannot send message to client because client is not connected to server. In {Method}",
                    nameof(SendMessage));

                return;
            }

            var stream = _tcpClient.GetStream();
            if (!stream.CanWrite)
            {
                _logger.LogWarning("Cannot write to server. Waiting for sending. In {Method}", nameof(SendMessage));

                while (!stream.CanWrite) { }
            }

            _logger.LogInformation("Can write to server. Preparing for sending. In {Method}", nameof(SendMessage));
            
            var serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
            stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);

            _logger.LogInformation("Message was sent to server. Message: {Message}. In {Method}", 
                message, nameof(SendMessage));
        }
        catch (SocketException socketException)
        {
            if (socketException.SocketErrorCode == SocketError.Interrupted)
            {
                _logger.LogInformation("Socket stopped. In {Method}", 
                    nameof(SendMessage));
                    
                return;
            }
                
            _logger.LogCritical(socketException, "In {Method}", 
                nameof(SendMessage));
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", 
                nameof(SendMessage));
        }
    }
    
    public void Close()
    {
        _cancellationTokenSource.Cancel();
    }
}