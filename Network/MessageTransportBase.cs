#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fida.Network
{
    /// <summary>
    /// 消息传输基类：持有公共的消息队列、接收循环、发送循环和拆卸逻辑。
    /// 子类只需实现连接建立、连接/流引用和断开通知。
    /// </summary>
    public abstract class MessageTransportBase : IDisposable
    {
        protected CancellationTokenSource _cts = null!;
        protected readonly ConcurrentQueue<NetworkMessage> _incomingMessages = new();
        protected readonly ConcurrentQueue<NetworkMessage> _outgoingMessages = new();
        private bool _disposed;
        private readonly string _disconnectMessage;

        protected MessageTransportBase(string disconnectMessage)
        {
            _disconnectMessage = disconnectMessage;
        }

        /// <summary>子类提供用于连接检查的 TcpClient（可为 null）</summary>
        protected abstract TcpClient Connection { get; }

        /// <summary>子类提供用于 I/O 的 NetworkStream（可为 null）</summary>
        protected abstract NetworkStream DataStream { get; }

        /// <summary>
        /// 当传输层检测到连接丢失时调用（接收循环退出）。
        /// 子类在此检查自身状态、更新标志并触发断开事件。
        /// </summary>
        protected abstract void NotifyDisconnected(string message);

        /// <summary>子类实现的停止逻辑（设置标志、取消、关闭资源、置空字段）</summary>
        protected abstract void OnStop();

        /// <summary>
        /// 获取所有待处理的消息（非阻塞）
        /// </summary>
        public List<NetworkMessage> ReceiveMessages()
        {
            var messages = new List<NetworkMessage>();
            while (_incomingMessages.TryDequeue(out NetworkMessage? message))
            {
                messages.Add(message);
            }
            return messages;
        }

        protected async Task ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && Connection != null && Connection.Connected)
                {
                    NetworkMessage? message = await Task.Run(() =>
                    {
                        try
                        {
                            return NetworkMessage.ReadFromStream(DataStream);
                        }
                        catch (IOException)
                        {
                            return null;
                        }
                    }, ct);

                    if (message == null)
                    {
                        break;
                    }

                    _incomingMessages.Enqueue(message);
                }
            }
            catch (OperationCanceledException) { }
            catch { }

            NotifyDisconnected(_disconnectMessage);
        }

        protected async Task SendLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && Connection != null && Connection.Connected)
                {
                    if (_outgoingMessages.TryDequeue(out NetworkMessage? message))
                    {
                        byte[] data = message.Serialize();
                        await DataStream.WriteAsync(data, 0, data.Length, ct);
                        await DataStream.FlushAsync(ct);
                    }
                    else
                    {
                        await Task.Delay(10, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        /// <summary>取消 CTS 并关闭流与 TcpClient</summary>
        protected void TearDownTransport()
        {
            _cts?.Cancel();
            try
            {
                DataStream?.Close();
                DataStream?.Dispose();
                Connection?.Close();
                Connection?.Dispose();
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            OnStop();
            _cts?.Dispose();
        }
    }
}
