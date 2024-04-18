using System.Net.WebSockets;
using System.Text;

namespace Program.Comm {
    public class Client {
        private Task? task = null;
        public Task? Task {
            get => task;
            set => task = value;
        }

        private bool ssl = true;
        public bool Ssl {
            get => ssl;
            set => ssl = value;
        }

        private Flow server = new();
        public Flow Server {
            get => server;
            set => server = value;
        }

        private ClientWebSocket ws = new();
        public ClientWebSocket Ws {
            get => ws;
            set => ws = value;
        }

        public async Task Connect() {
            // Determine the protocol to use based off of SSL option.
            var protocol = "ws";

            if (ssl)
                protocol = "wss";

            var uri = new Uri($"{protocol}://{server.host}:{server.port}");

            await ws.ConnectAsync(uri, CancellationToken.None);
        }

        public async Task Send(string msg) {
            if (ws.State != WebSocketState.Open)
                throw new Exception("Failed to send message to server. Web socket state is not open.");

            var buffer = Encoding.UTF8.GetBytes(msg);

            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task<string> Recv() {
            if (ws.State != WebSocketState.Open)
                throw new Exception("Failed to send receive message from server. Web socket state is not open.");

            var recvBuffer = new byte[2048];

            var recvRes = await ws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), CancellationToken.None);

            var msg = Encoding.UTF8.GetString(recvBuffer, 0, recvRes.Count);

            return msg;
        }

        public async Task Disconnect() {
            if (ws.State != WebSocketState.Open && ws.State != WebSocketState.Connecting)
                throw new Exception("Failed to disconnect client session. Web socket is not open or connecting.");

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }
}