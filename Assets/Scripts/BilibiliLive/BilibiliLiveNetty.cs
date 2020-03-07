using System;
using System.Net;
using CI.TaskParallel;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Codecs.Http.WebSockets.Extensions.Compression;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using UnityEngine;
using UnityEngine.Events;

public class BilibiliLiveNetty : SingletonUtil<BilibiliLiveNetty>
{
    public Action<string, string> onDanmakuMessage;
    
    // Start is called before the first frame update
    void Start()
    {
        UnityTask.InitialiseDispatcher();
        Connect();
    }

    async void Connect()
    {
        var workerGroup = new MultithreadEventLoopGroup();

        // Connect with V13 (RFC 6455 aka HyBi-17). You can change it to V08 or V00.
        // If you change it to V00, ping is not supported and remember to change
        // HttpResponseDecoder to WebSocketHttpResponseDecoder in the pipeline.
        var handler = new BilibiliLiveHandler(
            WebSocketClientHandshakerFactory.NewHandshaker(
                new Uri("ws://broadcastlv.chat.bilibili.com:2244/sub"), WebSocketVersion.V13, null, true,
                new DefaultHttpHeaders()), 885302, this);

        var bootstrap = new Bootstrap()
            .Group(workerGroup)
            .Option(ChannelOption.TcpNodelay, true)
            .Channel<TcpSocketChannel>()
            .Handler(new ActionChannelInitializer<IChannel>(channel =>
            {
                var pipeline = channel.Pipeline;
                pipeline.AddLast(new HttpClientCodec(),
                    new HttpObjectAggregator(8192),
                    WebSocketClientCompressionHandler.Instance,
                    handler);
            }));
        await bootstrap.ConnectAsync(new DnsEndPoint("broadcastlv.chat.bilibili.com", 2244));
    }

    // Update is called once per frame
    void Update()
    {
    }
}