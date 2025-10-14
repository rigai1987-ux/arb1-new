using Fleck;
using SpreadAggregator.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadAggregator.Infrastructure.Services;

public class FleckWebSocketServer : Application.Abstractions.IWebSocketServer, IDisposable
{
    private readonly WebSocketServer _server;
    private readonly List<IWebSocketConnection> _sockets;

    public FleckWebSocketServer(string location)
    {
        _server = new WebSocketServer(location);
        _sockets = new List<IWebSocketConnection>();
    }

    public void Start()
    {
        _server.Start(socket =>
        {
            socket.OnOpen = () => _sockets.Add(socket);
            socket.OnClose = () => _sockets.Remove(socket);
        });
    }

    public Task BroadcastAsync(string message)
    {
        var tasks = _sockets.Where(s => s.IsAvailable).Select(s => s.Send(message));
        return Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }
}