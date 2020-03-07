using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using CI.TaskParallel;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BilibiliLiveHandler : SimpleChannelInboundHandler<object>
{
    readonly WebSocketClientHandshaker handshaker;
    readonly TaskCompletionSource completionSource;

    readonly int roomid;
    readonly BilibiliLiveNetty bilibiliLiveNetty;

    public BilibiliLiveHandler(WebSocketClientHandshaker handshaker, int roomid, BilibiliLiveNetty bilibiliLiveNetty)
    {
        this.handshaker = handshaker;
        completionSource = new TaskCompletionSource();
        this.roomid = roomid;
        this.bilibiliLiveNetty = bilibiliLiveNetty;
    }

    public Task HandshakeCompletion => completionSource.Task;

    public override void ChannelActive(IChannelHandlerContext ctx) =>
        handshaker.HandshakeAsync(ctx.Channel).LinkOutcome(completionSource);

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        Debug.Log("WebSocket Client disconnected!");
    }

    protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
    {
        IChannel ch = ctx.Channel;
        if (!handshaker.IsHandshakeComplete)
        {
            try
            {
                handshaker.FinishHandshake(ch, (IFullHttpResponse) msg);
                completionSource.TryComplete();
                var enterRoom = new JObject(new JProperty("roomid", roomid));
                var str = enterRoom.ToString();
                var enterRoomByteBuffer = Encode(7, str);
                ctx.WriteAndFlushAsync(new BinaryWebSocketFrame(enterRoomByteBuffer));
                UnityTask.RunOnUIThread(() => bilibiliLiveNetty.StartCoroutine(Heartbeat(ctx)));
                Debug.Log("WebSocket Client connected!");
            }
            catch (WebSocketHandshakeException e)
            {
                Debug.Log("WebSocket Client failed to connect");
                completionSource.TrySetException(e);
            }

            return;
        }

        if (msg is IFullHttpResponse response)
        {
            throw new InvalidOperationException(
                $"Unexpected FullHttpResponse (getStatus={response.Status}, content={response.Content.ToString(Encoding.UTF8)})");
        }

        if (msg is TextWebSocketFrame textFrame)
        {
            Debug.Log($"WebSocket Client received message: {textFrame.Text()}");
        }
        else if (msg is BinaryWebSocketFrame binaryFrame)
        {
            IByteBuffer byteBuffer = binaryFrame.Content;
            var (operation, body) = Decode(byteBuffer);
            Handle(operation, body);
        }
        else if (msg is PongWebSocketFrame)
        {
            Debug.Log("WebSocket Client received pong");
        }
        else if (msg is CloseWebSocketFrame)
        {
            Debug.Log("WebSocket Client received closing");
            ch.CloseAsync();
        }
    }

    IEnumerator Heartbeat(IChannelHandlerContext ctx)
    {
        while (true)
        {
            yield return new WaitForSeconds(30);
            var hearbeatByteBuffer = Encode(2, "");
            ctx.WriteAndFlushAsync(new BinaryWebSocketFrame(hearbeatByteBuffer));
        }
    }

    private IByteBuffer Encode(int operation, string body)
    {
        IByteBuffer byteBuffer = PooledByteBufferAllocator.Default.Buffer();
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        byteBuffer.WriteInt(bytes.Length + 16);
        byteBuffer.WriteShort(16);
        byteBuffer.WriteShort(1);
        byteBuffer.WriteInt(operation);
        byteBuffer.WriteInt(1);
        byteBuffer.WriteBytes(bytes);
        return byteBuffer;
    }

    private (int, object) Decode(IByteBuffer byteBuffer)
    {
        var packetLength = byteBuffer.ReadInt();
        var headerLength = byteBuffer.ReadShort();
        var protocolVersion = byteBuffer.ReadShort();
        var operation = byteBuffer.ReadInt();
        var sequenceId = byteBuffer.ReadInt();
        object body = null;
        switch (operation)
        {
            case 3:
                body = byteBuffer.ReadInt();
                break;
            case 5:
                body = byteBuffer.ReadString(packetLength - headerLength, Encoding.UTF8);
                break;
        }

        Debug.Log(
            $"WebSocket Client received message packetLength: {packetLength}, headerLength: {headerLength}, protocolVersion: {protocolVersion}, operation: {operation}, sequenceId: {sequenceId}, body: {body}");
        return (operation, body);
    }

    private void Handle(int operation, object body)
    {
        switch (operation)
        {
            case 3:
                var value = (int) body;
                Debug.Log($"value: {value}");
                break;
            case 5:
                var msg = JObject.Parse((string) body);
                Debug.Log(msg["cmd"]);
                switch ((string) msg["cmd"])
                {
                    case "DANMU_MSG":
                        var uname = (string) msg["info"][2][1];
                        var message = (string) msg["info"][1];
                        Debug.Log($"{uname}: {message}");
                        HandleDanmakuMessage(uname, message);
                        break;
                    case "SEND_GIFT":
                        uname = (string) msg["data"]["uname"];
                        var action = (string) msg["data"]["action"];
                        var num = (int) msg["data"]["num"];
                        var giftName = (string) msg["data"]["giftName"];
                        Debug.Log($"{uname} {action} {num} 个 {giftName}");
                        break;
                    case "WELCOME_GUARD":

                        break;
                }

                break;
        }
    }

    private void HandleDanmakuMessage(string uname, string message)
    {
        bilibiliLiveNetty.onDanmakuMessage(uname, message);
    }

    public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
    {
        Debug.LogException(exception);
        completionSource.TrySetException(exception);
        ctx.CloseAsync();
    }
}