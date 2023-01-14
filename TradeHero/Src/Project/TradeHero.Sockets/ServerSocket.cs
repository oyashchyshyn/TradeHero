using System.Net;
using System.Net.Sockets;
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
    private StreamReader? _streamReader;

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
        _tcpListener = new TcpListener(
            IPAddress.Any,
            _environmentService.GetAppSettings().Application.Sockets.Port
        );

        _tcpListener.Start();

        _logger.LogInformation("Listener started server connection. In {Method}",
            nameof(StartListen));
        
        Task.Run(async () =>
        {
            await StartMessageListenerAsync();
        });
    }

    public void DisconnectClient()
    {
        if (_connectedClient == null)
        {
            return;
        }
        
        _connectedClient.Close();
        _connectedClient.Dispose();

        _streamReader?.Close();

        _logger.LogInformation("Disconnect client. In {Method}", 
            nameof(DisconnectClient));
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
            if (_tcpListener == null)
            {
                _logger.LogWarning("Listener is null. In {Method}",
                    nameof(StartListen));
                    
                return;
            }
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_connectedClient == null)
                {
                    _connectedClient = await _tcpListener.AcceptTcpClientAsync();
                    _streamReader = new StreamReader(_connectedClient.GetStream());

                    _logger.LogInformation("Client connected to server. In {Method}",
                        nameof(StartListen));
                }

                try
                {
                    if (_streamReader == null)
                    {
                        _logger.LogError("{PropertyName} is null. In {Method}",
                            nameof(_streamReader), nameof(StartListen));
                        
                        break;
                    }

                    var clientMessage = await _streamReader.ReadLineAsync();
                    if (clientMessage == null)
                    {
                        continue;
                    }
                
                    _logger.LogInformation("Received message from client. Message: {Message} In {Method}",
                        clientMessage, nameof(StartListen));

                    OnReceiveMessageFromClient?.Invoke(this, new SocketMessageArgs(clientMessage));
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
    }

    #endregion
}