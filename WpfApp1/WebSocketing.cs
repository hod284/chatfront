using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class WebSocketing : IDisposable
    {
        private ClientWebSocket WebSocket;
        private CancellationTokenSource CancellationToken;

        public Action  Connected;
        public Action Disconnected;
        public Action<string> Error;
        public Action<string> Message;


        public bool IsConnected => WebSocket != null && WebSocket.State == WebSocketState.Open; 


        public async Task ConnectedAsync(string uri)
        {
            try
            {
                WebSocket = new ClientWebSocket();
                CancellationToken = new CancellationTokenSource();
                await WebSocket.ConnectAsync(new Uri(uri), CancellationToken.Token);
                Connected?.Invoke();
                //_ 이말은task를 돌리되 결과값을 받아오든 말든 무시한다 
                _ = MessageRoop(CancellationToken.Token);
            }
            catch (Exception ex) 
            {
                Error.Invoke(ex.Message);
            }
        }
        public async Task SendtheMessageAsync(string msg)
        {
            if (!IsConnected)
                return;
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            var segment = new ArraySegment<byte>(bytes);
            // endofmessage 는 완전히 수신되었는지 확인하는것
            await WebSocket.SendAsync(segment, WebSocketMessageType.Text,true,CancellationToken.Token);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (IsConnected)
                {
                    CancellationToken.Cancel();
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "clientdisconnect", System.Threading.CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex.Message);
            }
            finally
            { 
                Disconnected?.Invoke(); 
            }
        }
        public async Task MessageRoop(CancellationToken cancellationToken)
        { 
            var buffer = new byte[4096];
            try
            {
                while (!cancellationToken.IsCancellationRequested && WebSocket.State == WebSocketState.Open)
                { 
                      ArraySegment<byte> bytes = new ArraySegment<byte>(buffer);
                    var result = await WebSocket.ReceiveAsync(bytes, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,"byer",cancellationToken);
                        Disconnected?.Invoke();
                        break;
                    }
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Message?.Invoke(msg);
                }
            }
            catch (Exception e)
            {
               Error.Invoke(e.Message);
                Disconnected?.Invoke();
            }
        }

        public void Dispose()
        {
            CancellationToken?.Cancel();
            CancellationToken?.Dispose();
                 WebSocket?.Dispose();
        }
    }
}
