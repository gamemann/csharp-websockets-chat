using System.Text.Json.Nodes;

namespace Program {
    public class Config {
        public struct Server {
            public string host;
            public ushort port;
        }

        public struct Connection {
            public Server srv;
            public bool ssl;
        }

        private bool listen = true;
        public bool Listen {
            get => listen;
            set => listen = value;
        }

        private string listenHost = "127.0.0.1";
        public string ListenHost {
            get => listenHost;
            set => listenHost = value;
        }

        private ushort listenPort = 2222;
        public ushort ListenPort {
            get => listenPort;
            set => listenPort = value;
        }

        private bool listenSsl = false;
        public bool ListenSsl {
            get => listenSsl;
            set => listenSsl = value;
        }

        private List<Connection> startupConnections = new();
        public List<Connection> StartupConnections {
            get => startupConnections;
            set => startupConnections = value;
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
            Console.WriteLine($"Listen Address => '{listenHost}'");
            Console.WriteLine($"Listen Port => {listenPort}");
            Console.WriteLine($"Listen SSL => {listenSsl}");

            // Servers to connect to.
            if (startupConnections.Count > 0) {
                Console.WriteLine("Startup Connections");
                
                var i = 0;

                foreach (var conn in startupConnections) {
                    Console.WriteLine($"\tConnection #{i + 1}");

                    Console.WriteLine($"\t\tHost => {conn.srv.host}");
                    Console.WriteLine($"\t\tPort => {conn.srv.port}");
                    Console.WriteLine($"\t\tSSL => {conn.ssl}");

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

            var listenHostStr = data["listenHost"]?.ToString();

            if (listenHostStr != null)
                listenHost = listenHostStr;

            var listenPortStr = data["listenPort"]?.ToString();

            if (listenPortStr != null)
                listenPort = Convert.ToUInt16(listenPortStr);

            var listenSslStr = data["listenSsl"]?.ToString();

            if (listenSslStr != null)
                listenSsl = Convert.ToBoolean(listenSslStr);

            // Check for servers.
            try {
                var startupConnectionsArr = data["startupConnections"]?.AsArray();

                if (startupConnectionsArr != null) {
                    // Wipe current startup connections.
                    startupConnections = new();

                    foreach (var conn in startupConnectionsArr) {
                        if (conn == null)
                            continue;

                        Connection newConn = new();

                        // Check for server host.
                        var hostStr = conn["host"]?.ToString();

                        if (hostStr != null)
                            newConn.srv.host = hostStr;

                        // Check for server port.
                        var portStr = conn["port"]?.ToString();

                        if (portStr != null)
                            newConn.srv.port = Convert.ToUInt16(portStr);

                        // Check for SSL.
                        var sslStr = conn["ssl"]?.ToString();

                        if (sslStr != null)
                            newConn.ssl = Convert.ToBoolean(sslStr);

                        // Add new connection to startup connections.
                        startupConnections.Add(newConn);
                    }
                }
            } catch (Exception e) {
                throw new Exception($"Failed to read startup connections from config file due to exception. Exception:\n{e}");
            }
        }
    }
}