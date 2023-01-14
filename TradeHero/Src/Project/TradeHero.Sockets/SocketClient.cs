﻿using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Sockets;

namespace TradeHero.Sockets;

internal class SocketClient : ISocketClient
{
    private readonly ILogger<SocketClient> _logger;
    private readonly IEnvironmentService _environmentService;

    private TcpClient? _tcpClient;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

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
        
        await SendMessageAsync("Ping");
    }

    public async Task SendMessageAsync(string message)
    {
        try
        {
            if (_tcpClient == null)
            {
                _logger.LogError("Cannot send message because client is null. In {Method}",
                    nameof(SendMessageAsync));
                            
                return;
            }

            await using (var streamWriter = new StreamWriter(_tcpClient.GetStream()))
            {
                await streamWriter.WriteLineAsync(message);
                await streamWriter.FlushAsync();
            }

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
}