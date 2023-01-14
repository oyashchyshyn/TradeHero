using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;
using TradeHero.Core.Enums;

namespace TradeHero.Sockets;

internal class ServerSocket : IServerSocket
{
    private readonly ILogger<ServerSocket> _logger;
    private readonly IEnvironmentService _environmentService;
    
    private TcpListener? _tcpListener;
    private TcpClient? _connectedClient;
    private StreamWriter? _streamWriter;
    private StreamReader? _streamReader;
    
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public event EventHandler<ListenerCommand> OnReceiveMessageFromClient;
    
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
        try
        {
            var appSettings = _environmentService.GetAppSettings();

            _tcpListener = new TcpListener(
                _environmentService.GetLocalIpAddress(),
                appSettings.Application.Sockets.Port
            );
            
            _tcpListener.Start();
            
            _logger.LogInformation("Listener started to server. In {Method}", 
                nameof(StartListen));
            
            Task.Run(async () =>
            {
                var bytes = new byte[1024];
                
                while(true)
                {
                    // Get a stream object for reading 
                    await using var stream = (await _tcpListener.AcceptTcpClientAsync()).GetStream();
                    int length;
                    
                    while((length = stream.Read(bytes, 0, bytes.Length)) != 0) 
                    {
                        var incomingData = new byte[length];
                        
                        Array.Copy(bytes, 0, incomingData, 0, length);
                                                  
                        var clientMessage = Encoding.ASCII.GetString(incomingData);
                    }
                }

                _connectedClient = await _tcpListener.AcceptTcpClientAsync();

                _logger.LogInformation("Client connected to server. In {Method}", 
                    nameof(StartListen));
                
                var clientStream = _connectedClient.GetStream();

                _streamWriter = new StreamWriter(clientStream);
                _streamReader = new StreamReader(clientStream);
            });
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
    }

    public async Task SendMessageAsync(string message)
    {
        if (_connectedClient == null)
        {
            _logger.LogError("Cannot send message to client because client is not connected. In {Method}", 
                nameof(SendMessageAsync));
            
            return;
        }
        
        if (_streamWriter == null)
        {
            _logger.LogError("Cannot send message because client stream does not exist. In {Method}", 
                nameof(SendMessageAsync));
            
            return;
        }
        
        await _streamWriter.WriteLineAsync(message);
    }

    public void DisconnectClient()
    {
        if (_connectedClient == null)
        {
            return;
        }
        
        
        _connectedClient.Close();
        _streamReader = null;
        _streamWriter = null;
            
        _logger.LogInformation("Client connection closed. In {Method}", 
            nameof(DisconnectClient));
    }
    
    public void Close()
    {
        DisconnectClient();
        
        _tcpListener?.Stop();
        _cancellationTokenSource.Cancel();
        
        _logger.LogInformation("Server connection closed. In {Method}", 
            nameof(Close));
    }
}