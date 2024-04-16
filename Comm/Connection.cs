namespace Program.Comm {
    public class Connection {
        private bool ssl = false;
        public bool Ssl {
            get => ssl;
            set => ssl = value;
        }
        
        private bool isClient = true;
        public bool IsClient {
            get => isClient;
            set => isClient = value;
        }

        private Client? client = null;
        public Client? Client {
            get => client;
            set => client = value;
        }

        private Server? server = null;
        public Server? Server {
            get => server;
            set => server = value;
        }

        public void Connect() {

        }

        public void Listen() {

        }
    }
}