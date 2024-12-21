using k8s;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;

namespace KubeNavigator.Model;
public sealed partial class PodExecSession : IDisposable
{
    private readonly WebSocket _webSocket;
    private readonly StreamDemuxer _demux;
    private readonly Stream _resizeStream;

    public event EventHandler Closed;

    public Stream Stream { get; }

    public PodExecSession(WebSocket webSocket)
    {
        _webSocket = webSocket;
        _demux = new StreamDemuxer(webSocket);
        _demux.ConnectionClosed += OnConnectionClosed;

        Stream = _demux.GetStream(ChannelIndex.StdOut, ChannelIndex.StdIn);
        _resizeStream = _demux.GetStream(null, ChannelIndex.Resize);


        _demux.Start();
    }

    public void Resize(TerminalSize size)
    {
        try
        {
            _resizeStream.Write(JsonSerializer.SerializeToUtf8Bytes(size));
        }
        catch (Exception e)
        {
            Debug.WriteLine("Terminal Resize failed: " + e.Message);
        }
    }

    private void OnConnectionClosed(object? sender, EventArgs e)
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _demux.Dispose();
        _webSocket.Dispose();
    }
}
