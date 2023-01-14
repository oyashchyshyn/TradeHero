using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using System;
using TradeHero.Core.Enums;

namespace TradeHero.Services.Services;

internal class SocketService
{
    private readonly ILogger<SocketService> _logger;

    private readonly IPEndPoint _ipEndPoint = new(GetLocalIpAddress(), 8888);
    private TcpListener _listenerServer;
    private TcpClient _listenerClient;


    
    
    public event EventHandler<ListenerCommand> OnServerReceive;
    public event EventHandler<ListenerCommand> OnClientReceive;
    
    public SocketService(ILogger<SocketService> logger)
    {
        _logger = logger;
    }

    public void CreateServer()
    {
        _listenerServer = new TcpListener(_ipEndPoint);
        
        _listenerServer.Start();

        Task.Run(async () =>
        {
            try
            {
                var buffer = new byte[256];

                while (true)
                {
                    using var client = await _listenerServer.AcceptTcpClientAsync();

                    await using var stream = client.GetStream();
                    int i;

                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {

                    }

                    while ((i = await stream.ReadAsync(buffer)) != 0)
                    {
                        var data = Encoding.ASCII.GetString(buffer, 0, i);
                        Console.WriteLine($"Recibido: {data}");

                        // Procesar los datos enviados por el cliente
                        data = data.ToUpper();
                        i = Encoding.ASCII.GetBytes(data, 0, data.Length, buffer, 0);

                        // Enviar una respuesta
                        await stream.WriteAsync(buffer.AsMemory(0, i));
                        Console.WriteLine($"Devuelto: {data}");
                    }

                    break;
                }
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "In {Method}", nameof(CreateServer));
            }
            finally
            {
                _listenerServer.Stop();
                
                _logger.LogInformation("Server listener finished. In {Method}", nameof(CreateServer));
            }
        });
    }

    public void ConnectToServer()
    {
        _listenerClient = new TcpClient();
        _listenerClient.Connect(GetLocalIpAddress(), 8888);
    }

    public void Close()
    {
        _cancellationTokenSource.Cancel();
    }
    
    #region Private methods

    

    #endregion
}