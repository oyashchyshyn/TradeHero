using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;
using TradeHero.Core.Enums;

namespace TradeHero.Sockets;

internal class ClientSocket : IClientSocket
{
    private readonly ILogger<ClientSocket> _logger;
    private readonly IEnvironmentService _environmentService;

    private TcpClient _tcpClient;
    
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public event EventHandler<ListenerCommand> OnReceiveMessageFromServer;
    
    public ClientSocket(
        ILogger<ClientSocket> logger, 
        IEnvironmentService environmentService
        )
    {
        _logger = logger;
        _environmentService = environmentService;
    }

    public void Connect()
    {
        var appSettings = _environmentService.GetAppSettings();
        
        _tcpClient = new TcpClient();
        
        _tcpClient.Connect(
            _environmentService.GetLocalIpAddress(), 
            appSettings.Application.Sockets.Port
        );
    }

    public void Close()
    {
        _cancellationTokenSource.Cancel();
    }

    public void SendMessage(string message)
    {
        
    }
}