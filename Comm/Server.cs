using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace Program.Comm {
    public class Server {
        private bool ssl = true;
        public bool Ssl {
            get => ssl;
            set => ssl = value;
        }

        private Flow bind = new();
        public Flow Bind {
            get => bind;
            set => bind = value;
        }

        private HttpListener listener = new();
        public HttpListener Listener {
            get => listener;
            set => listener = value;
        }

        private WebSocket? ws = null;
        public WebSocket? Ws {
            get => ws;
            set => ws = value;
        }

        public void Listen() {
            // Figure out the protocol we're using.
            var protocol = "http";

            if (ssl)
                protocol = "https";
            
            // Create HTTP listener.
            listener.Prefixes.Add($"{protocol}://{bind.host}:{bind.port}/");

            listener.Start();
        }

        public async Task Send(string msg) {
            if (ws == null || ws.State != WebSocketState.Open)
                throw new Exception("Failed to send message. Web socket is null.");

            var buffer = Encoding.UTF8.GetBytes(msg);

            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task<string?> Recv() {
            if (ws == null || ws.State != WebSocketState.Open)
                throw new Exception("Failed to receive message. Web socket is null.");

            var recvBuffer = new byte[2048];

            var recvRes = await ws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), CancellationToken.None);

            if (recvRes.MessageType == WebSocketMessageType.Text) {
                var msg = Encoding.UTF8.GetString(recvBuffer, 0, recvRes.Count);

                return msg;
            } else if (recvRes.MessageType == WebSocketMessageType.Close) {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }

            return null;
        }

        public async void Disconnect() {
            if (ws == null || (ws.State != WebSocketState.Open && ws.State != WebSocketState.Connecting))
                throw new Exception("Failed to disconnect server session. Web socket is not open or connecting.");

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }
}