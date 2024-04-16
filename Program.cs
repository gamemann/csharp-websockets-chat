using Program.Comm;

namespace Program {
    public class MainProgram {
        static readonly Cmd cmd = new();
        static readonly Config cfg = new();

        static readonly List<Connection> connections = new();

        static bool exit = false;
        
        static int curConnection = -1;

        private static void ListClients() {
            for (int i = 0; i < connections.Count; i++) {
                var conn = connections[i];

                if (!conn.IsClient)
                    continue;

                Console.WriteLine($"[{i}] {conn.Client?.Flow.ip ?? "N/A"}:{conn.Client?.Flow.port} => {conn.Server?.Flow.ip ?? "N/A"}:{conn.Server?.Flow.port}");
            }
        }

        private static void ListServers() {
            for (int i = 0; i < connections.Count; i++) {
                var conn = connections[i];

                if (conn.IsClient)
                    continue;

                Console.WriteLine($"[{i}] {conn.Client?.Flow.ip ?? "N/A"}:{conn.Client?.Flow.port} => {conn.Server?.Flow.ip ?? "N/A"}:{conn.Server?.Flow.port}");
            }
        }

        private static void PrintTopMenu() {
            Console.WriteLine("Commands");
            Console.WriteLine("\tls - List active server connections.");
            Console.WriteLine("\tlc - List active client connections.");
            Console.WriteLine("\tc <ip> <port> <ssl> - Establish a connection with <ip>:<port>.");
            Console.WriteLine("\tt <idx> - Use connection at index <idx>.");
            Console.WriteLine("\tq - Exit program.");
            Console.Write("Cmd: ");
        }

        private static int MakeConnection(string ip, ushort port, bool client = true, bool ssl = true) {
            // Make sure we have a valid IP.
            if (!Utils.IsValidIpv4(ip)) {
                Console.WriteLine($"Failed to add connection. IPv4 address '{ip}' is invalid.");

                return 1;
            }
            
            connections.Add(new() {
                Ssl = ssl,
                IsClient = client,
                Client = new() {
                    Flow = new() {
                        ip = client ? "127.0.0.1" : ip,
                        port = client ? (ushort) 123 : port
                    }
                },
                Server = new() {
                    Flow = new() {
                        ip = client ? ip : "127.0.0.1",
                        port = client ? port : (ushort) 123

                    }
                }
            });

            // To Do: Retrieve connection and connect to web socket.

            return 0;
        }

        private static void ParseTopLine(string line) {   
            // Get first argument.
            var split = line.Split(" ");    

            switch (split[0]) {
                case "ls":
                    Console.WriteLine("Listing Servers...");

                    ListServers();

                    break;

                case "lc":
                    Console.WriteLine("Listing Clients...");

                    ListClients();

                    break;

                case "c": {
                    // We need to split the string.

                    if (split.Length < 2) {
                        Console.WriteLine("IP not set.");

                        break;
                    }

                    if (split.Length < 3) {
                        Console.WriteLine("Port not set.");

                        break;
                    }

                    var ip = split[1];
                    var port = split[2];

                    var ssl = true;

                    if (split.Length > 3) {
                        var sslStr = split[3];

                        if (sslStr.ToLower() == "no")
                            ssl = false;
                    }

                    if (MakeConnection(ip, Convert.ToUInt16(port), true, ssl) == 0)
                        Console.WriteLine($"Added connection to {ip}:{port}...");

                    break;
                }

                case "t":
                    if (split.Length < 2) {
                        Console.WriteLine("No index set.");

                        break;
                    }

                    var idx = split[1];

                    curConnection = Convert.ToInt16(idx);

                    break;

                case "q":
                    exit = true;

                    break;

                default:
                    PrintTopMenu();
                    
                    break;
            }
        }

        static async Task<int> Main(string[] args) {
            // Parse command line options.
            try {
                cmd.Parse(args);
            } catch (Exception e) {
                Console.WriteLine("Failed to parse command line due to exception.");
                Console.WriteLine(e);

                return 1;
            }

            // Parse config.
            try {
                if (cmd.Opts.Cfg == null) {
                    Console.WriteLine("Config path somehow null?");

                    return 1;
                }

                cfg.Load(cmd.Opts.Cfg);
            } catch (Exception e) {
                Console.WriteLine("Failed to load and read config file due to exception.");
                Console.WriteLine(e);

                return 1;
            }

            // Check if we should print config and exit.
            if (cmd.Opts.List) {
                cfg.Print();

                return 0;
            }

            // To Do: Connect to servers in config.

            // Print top menu now.
            PrintTopMenu();

            while (!exit) {
                // Check our current connection.
                if (curConnection == -1) {
                    try {
                        var input = Console.ReadLine();

                        if (input != null)
                            ParseTopLine(input);

                    } catch (Exception e) {
                        Console.WriteLine("Failed to read line.");
                        Console.WriteLine(e);

                        return 1;
                    }
                } else {
                    Console.WriteLine($"Connecting to connection at index {curConnection}...");
                    
                    try {
                        var conn = connections[curConnection];

                        Console.WriteLine($"Client: {(conn.IsClient ? "Yes" : "No")}...");
                        Console.WriteLine($"Connection: '{conn.Client?.Flow.ip ?? "N/A"}:{conn.Client?.Flow.port}' => '{conn.Server?.Flow.ip ?? "N/A"}:{conn.Server?.Flow.port}'...");
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to connect to connection at index {curConnection}");
                        Console.WriteLine(e);

                        curConnection = -1;
                    }
                }
            }

            return 0;
        }
    }
}