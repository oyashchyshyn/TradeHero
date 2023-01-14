using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;
using TradeHero.Contracts.Sockets.Args;

namespace TradeHero.Sockets;

internal class ServerSocket : IServerSocket
{
    private readonly ILogger<ServerSocket> _logger;
    private readonly IEnvironmentService _environmentService;
    
    private TcpListener? _tcpListener;
    private TcpClient? _connectedClient;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public event EventHandler<SocketMessageArgs>? OnReceiveMessageFromClient;
    
    public ServerSocket(
        ILogger<ServerSocket> logger, 
        IEnvironmentService environmentService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
    }

    public void StartListen()
    {
        Task.Run(async () =>
        {
            try
            {
                var appSettings = _environmentService.GetAppSettings();

                _tcpListener = new TcpListener(
                    _environmentService.GetLocalIpAddress(),
                    appSettings.Application.Sockets.Port
                );

                _tcpListener.Start();

                _logger.LogInformation("Listener started server connection. In {Method}",
                    nameof(StartListen));

                var bytes = new byte[1024];

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var newConnectedClient = await _tcpListener.AcceptTcpClientAsync();
                    
                    _connectedClient?.Dispose();
                    _connectedClient = newConnectedClient;
                        
                    _logger.LogInformation("Client connected to server. In {Method}",
                        nameof(StartListen));

                    await using (var stream = _connectedClient.GetStream())
                    {
                        int length;
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            var clientMessage = Encoding.ASCII.GetString(incomingData);

                            _logger.LogInformation("Received message from client. Message: {Message} In {Method}",
                                clientMessage, nameof(StartListen));

                            OnReceiveMessageFromClient?.Invoke(this, new SocketMessageArgs(clientMessage));
                        }
                    }
                }
            }
            catch (SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.Interrupted)
                {
                    _logger.LogInformation("Socket stopped. In {Method}",
                        nameof(StartListen));

                    return;
                }

                _logger.LogCritical(socketException, "In {Method}",
                    nameof(StartListen));
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "In {Method}",
                    nameof(StartListen));
            }
            finally
            {
                DisconnectClient();
                
                _tcpListener?.Stop();
                
                _logger.LogInformation("Server connection closed. In {Method}", 
                    nameof(StartListen));
            }
        });
    }

    public void SendMessage(string message)
    {
        try
        {
            if (_connectedClient == null)
            {
                _logger.LogError("Cannot send message to client because client is not connected. In {Method}",
                    nameof(SendMessage));

                return;
            }

            var stream = _connectedClient.GetStream();
            if (!stream.CanWrite)
            {
                _logger.LogWarning("Cannot write to client. Waiting for sending. In {Method}", nameof(SendMessage));

                while (!stream.CanWrite) { }
            }

            _logger.LogWarning("Can write to client. Preparing for sending. In {Method}", nameof(SendMessage));
            
            var serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
            stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);

            _logger.LogInformation("Message was sent to client. In {Method}", nameof(SendMessage));
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

    public void DisconnectClient()
    {
        if (_connectedClient == null)
        {
            return;
        }
        
        _connectedClient.Close();
        _connectedClient.Dispose();
        
        _logger.LogInformation("Disconnect client. In {Method}", 
            nameof(DisconnectClient));
    }
    
    public void Close()
    {
        _cancellationTokenSource.Cancel();
    }
}