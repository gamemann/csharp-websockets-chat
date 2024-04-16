using System.Text.Json.Nodes;

namespace Program {
    public class Config {
        public struct Server {
            public string host;
            public ushort port;
        }

        private bool listen = false;
        public bool Listen {
            get => listen;
            set => listen = value;
        }

        private string listenAddr = "localhost";
        public string ListenAddr {
            get => listenAddr;
            set => listenAddr = value;
        }

        private uint listenPort = 7654;
        public uint ListenPort {
            get => listenPort;
            set => listenPort = value;
        }

        private List<Server> servers = new();
        public List<Server> Servers {
            get => servers;
            set => servers = value;
        }

        public void Load(string path) {
            JsonObject? jsonObj;

            // Read JSON string from config file and store in JSON object.
            try {
                jsonObj = ReadFromFile(path);

                if (jsonObj == null)
                    throw new Exception("Failed to laod config file. JSON object is null.");
            } catch (Exception e) {
                throw new Exception($"Failed to load config file due to exception.\n{e}");
            }

            // Attmept to load confg values from JSON object.
            try {
                LoadValues(jsonObj);
            } catch (Exception e) {
                throw new Exception($"Failed to read config values due to exception.\n{e}");
            }
        }

        public void Print() {
            // Listen settings.
            Console.WriteLine($"Listen Enabled => {listen}");
            Console.WriteLine($"Listen Address => '{listenAddr}'");
            Console.WriteLine($"Listen Port => {listenPort}");

            // Servers to connect to.
            if (servers.Count > 0) {
                Console.WriteLine("Servers");
                
                var i = 0;

                foreach (var server in servers) {
                    Console.WriteLine($"\tServer #{i + 1}");

                    Console.WriteLine($"\t\tHost => {server.host}");
                    Console.WriteLine($"\t\tPort => {server.port}");

                    i++;
                }
            }
        }

        private static JsonObject? ReadFromFile(string path) {
            JsonObject? data = null;

            try {
                // Read config file.
                var text = File.ReadAllText(path);

                // Convert JSON to JSON object.
                data = JsonNode.Parse(text)?.AsObject();
            } catch (Exception e) {
                throw new Exception($"Failed to read from config file.\n{e}");
            }

            return data;
        }

        private void LoadValues (JsonObject data) {
            // Check for listen overrides.
            var listenStr = data["listen"]?.ToString();

            if (listenStr != null)
                listen = Convert.ToBoolean(listenStr);

            var listenAddrStr = data["listenAddr"]?.ToString();

            if (listenAddrStr != null)
                listenAddr = listenAddrStr;

            var listenPortStr = data["listenPort"]?.ToString();

            if (listenPortStr != null)
                listenPort = Convert.ToUInt16(listenPortStr);

            // Check for servers.
            try {
                var serversArr = data["servers"]?.AsArray();

                if (serversArr != null) {
                    // Wipe current servers.
                    servers = new();

                    foreach (var server in serversArr) {
                        if (server == null)
                            continue;

                        Server newServer = new();

                        // Check for server host.
                        var hostStr = server["host"]?.ToString();

                        if (hostStr != null)
                            newServer.host = hostStr;

                        // Check for server port.
                        var portStr = server["port"]?.ToString();

                        if (portStr != null)
                            newServer.port = Convert.ToUInt16(portStr);

                        // Add new server to servers list.
                        servers.Add(newServer);
                    }
                }
            } catch (Exception e) {
                throw new Exception($"Failed to read values from config file: {e}");
            }
        }
    }
}