using Program.Comm;
using System.Net.WebSockets;

namespace Program {
    public class MainProgram {
        static readonly Cmd cmd = new();
        static readonly Config cfg = new();

        static readonly List<Client> clients = new();
        static readonly List<Server> servers = new();

        static bool exit = false;
        
        static int curIndex = -1;
        static bool isServer = false;

        static readonly List<Task> tasks = new();

        private static void PrintTopMenu() {
            Console.WriteLine("Commands");
            Console.WriteLine("\tls - List all server connections.");
            Console.WriteLine("\tlc - List all client connections.");
            Console.WriteLine("\tnew <ip> <port> <ssl> - Establish a client connection with <ip>:<port>.");
            Console.WriteLine("\tcc <idx> - Use client at index <idx>.");
            Console.WriteLine("\tcs <idx> - Use server at index <idx>.");
            Console.WriteLine("\trc <idx> - Remove client at index <idx>.");
            Console.WriteLine("\trs <idx> - Remove server at index <idx>.");
            Console.WriteLine("\th - Print top/help menu.");
            Console.WriteLine("\tq - Exit program.");
        }

        /* Listing */
        private static void ListClients() {
            for (int i = 0; i < clients.Count; i++) {
                var cl = clients[i];

                Console.WriteLine($"[{i}] {cl.Server.host}:{cl.Server.port} (SSL => {cl.Ssl})");
            }
        }

        private static void ListServers() {
            for (int i = 0; i < servers.Count; i++) {
                var srv = servers[i];

                Console.WriteLine($"[{i}] {srv.Bind.host ?? "N/A"}:{srv.Bind.port} (SSL => {srv.Ssl})");
            }
        }

        /* Retrieving indexes */
        private static int GetClientIndex(Client cl) {
            return clients.FindIndex(c => cl == c);
        }

        private static int GetServerIndex(Server srv) {
            return servers.FindIndex(c => c == srv);
        }

        /* Message processing */
        private static void ProcessClientMsg(Client cl, string msg) {
            var idx = GetClientIndex(cl);

            if (curIndex == idx && !isServer)
                Console.Write($"\nServer: {msg}\nMsg: ");
        }

        private static void ProcessServerMsg(Server srv, string msg) {
            var idx = GetServerIndex(srv);

            if (curIndex == idx && isServer)
                Console.Write($"\nClient: {msg}\nMsg: ");
        }

        /* General Processing */
        private static async void ClientProcess(Client cl) {
            while (true) {
                try {
                    var msg = await cl.Recv();

                    ProcessClientMsg(cl, msg);
                } catch (Exception e) {
                    var idx = GetClientIndex(cl);

                    // Only print exception if we're active.
                    if (curIndex == idx && !isServer)
                        Console.WriteLine($"Failed to receive message from server due to exception. Exception:\n{e}");

                    Thread.Sleep(1000);
                }
            }
        }

        private static async Task ServerProcessClient(Server srv, WebSocket ws) {
            while (true) {
                try {
                    if (ws.State == WebSocketState.Open) {
                        var msg = await srv.Recv();

                        // If null, indicates an issue or close. So break and reallow new clients.
                        if (msg == null)
                            break;

                        // Process message.
                        ProcessServerMsg(srv, msg);
                    } else {
                        Console.WriteLine($"Found connection to server '{srv.Bind.host}:{srv.Bind.port}' that isn't open. Closing current connection.");

                        break;
                    }
                } catch (Exception e) {
                    Console.WriteLine($"Found exception when receiving reply from client on server '{srv.Bind.host}:{srv.Bind.port}'. Closing current connection.");
                    Console.WriteLine(e);

                    break;
                }
            }
        }

        private static async Task ServerProcess(Server srv) {
            while (true) {
                var ctx = await srv.Listener.GetContextAsync();

                if (ctx.Request.IsWebSocketRequest) {
                    var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null);
                    srv.Ws = wsCtx.WebSocket;

                    await ServerProcessClient(srv, srv.Ws);

                    // Attempt to close current web socket since we're done.
                    try {
                        await srv.Disconnect();
                    } catch {}

                    srv.Ws = null;

                    continue;
                } else {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                }
            }
        }

        private static async Task RemoveClient(int idx) {
            try {
                // Retrieve client at index.
                var cl = clients[idx];

                try {
                    await cl.Disconnect();
                } catch (Exception e) {
                    Console.WriteLine($"Failed to disconnect client at index {idx} due to exception.");
                    Console.WriteLine(e);
                }

                if (cl.Task != null) {
                    try {
                        cl.Task.Dispose();
                    } catch (Exception e) {
                        Console.WriteLine("Failed to stop task when disconnecting client at index {idx} due to exception.");
                        Console.WriteLine(e);
                    }
                }

                // Remove from clients list.
                clients.RemoveAt(idx);
            } catch (Exception e) {
                throw new Exception($"Failed to remove client at index #{idx} due to exception. Exception:\n{e}");
            }
        }

        private static async Task RemoveServer(int idx) {
            try {
                // Retrieve server.
                var srv = servers[idx];

                // Attempt to disconnect connection.
                try {
                   await srv.Disconnect();
                } catch (Exception e) {
                    Console.WriteLine($"Failed to disconnect server at index {idx} due to exception.");
                    Console.WriteLine(e);
                }

                // Remove server from list.
                servers.RemoveAt(idx);
            } catch (Exception e) {
                Console.WriteLine($"Failed to remove server at index {idx} due to exception. Exception:\n{e}");
            }
        }

        private static async Task MakeConnection(string host, ushort port, bool ssl = true) {
            // Make sure we have a valid IP.
            if (!Utils.IsValidIpv4(host))
                throw new Exception($"Failed to make connection using '{host}:{port}' due to invalid host address. SSL => {ssl}.");
            
            var cl = new Client() {
                Server = new() {
                    host = host,
                    port = port
                },
                Ssl = ssl
            };

            // Attempt to connect to server.
            try {
                await cl.Connect();
            } catch (Exception e) {
                throw new Exception($"Failed to make connection using '{host}:{port}' due to connection error. SSL => {ssl}. Exception:\n{e}");
            }

            // Add clients to list.
            clients.Add(cl);
        }

        private static async Task ParseTopLine(string line) {   
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

                case "new": {
                    if (split.Length < 2) {
                        Console.WriteLine("IP not set.");

                        break;
                    }

                    if (split.Length < 3) {
                        Console.WriteLine("Port not set.");

                        break;
                    }

                    var ip ="";
                    var port = "";
                    var ssl = true;

                    try {
                        ip = split[1];
                        port = split[2];

                        if (split.Length > 3) {
                            var sslStr = split[3];

                            if (sslStr.ToLower() == "no")
                                ssl = false;
                        }
                    } catch (Exception e) {
                        Console.WriteLine("Bad arguments due to exception.");
                        Console.WriteLine(e);
                    }

                    try {
                        await MakeConnection(ip, Convert.ToUInt16(port), ssl);
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to make connection to '{ip ?? "N/A"}:{port}' due to exception. Exception:\n{e}");
                    }

                    break;
                }

                case "cc": {
                    if (split.Length < 2) {
                        Console.WriteLine("No index set.");

                        break;
                    }

                    var idx = "";

                    try {
                        idx = split[1];

                        curIndex = Convert.ToInt16(idx);
                        isServer = false;

                        Console.WriteLine($"Connecting to client at index {curIndex}...");
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to switch to client {idx} due to exception. Exception:\n{e}");
                    } 

                    break;
                }

                case "cs": {
                    if (split.Length < 2) {
                        Console.WriteLine("No index set.");

                        break;
                    }

                    var idx = "";

                    try {
                        idx = split[1];

                        curIndex = Convert.ToInt16(idx);
                        isServer = true;

                        Console.WriteLine($"Connecting to server at index {curIndex}...");
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to switch to server {idx} due to exception. Exception:\n{e}");
                    }

                    break;
                }

                case "rc": {
                    if (split.Length < 2) {
                        Console.WriteLine("No index set.");

                        break;
                    }

                    var idx = "";

                    try {
                        idx = split[1];

                        await RemoveClient(Convert.ToInt16(idx));
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to remove client at index {idx} due to exception. Exception:\n{e}");
                    }

                    break;
                }

                case "rs": {
                    if (split.Length < 2) {
                        Console.WriteLine("No index set.");

                        break;
                    }

                    var idx = "";

                    try {
                        idx = split[1];

                        await RemoveServer(Convert.ToInt16(idx));
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to remove server at index {idx} due to exception. Exception:\n{e}");
                    }

                    break;
                }

                case "h":
                    PrintTopMenu();

                    break;

                case "q":
                    exit = true;

                    break;

                default:
                    PrintTopMenu();
                    
                    break;
            }
        }

        private static async Task HandleMessage(string msg) {
            // If we're receiving a quit message, reset.
            if (msg == "\\q") {
                curIndex = -1;

                return;
            }

            try {
                if (isServer) {
                    // Attempt to retrieve current server.
                    var srv = servers[curIndex];

                    // Send the message to the client.
                    try {
                        await srv.Send(msg);
                    } catch (Exception e) {
                        throw new Exception($"Failed to send message to client due to exception. Exception:\n{e}");
                    }
                } else {
                    // Attempt to retrieve current client.
                    var cl = clients[curIndex];

                    // Attempt to send message to server.
                    try {
                        await cl.Send(msg);
                    } catch (Exception e) {
                        throw new Exception($"Failed to send message to server due to exception. Exception:\n{e}");
                    }
                }
            } catch (Exception e) {
                var oldConn = curIndex;

                curIndex = -1;

                throw new Exception($"Failed to handle message for current connection #{oldConn} due to exception. Is server => {isServer}. Exception:\n{e}");
            }
        }

        private static void HandleAllIncoming() {
            while (true) {
                foreach (var cl in clients) {
                    if (cl.Task != null)
                        continue;

                    try {
                        cl.Task = Task.Factory.StartNew(() => ClientProcess(cl));
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to process client '{cl.Server.host}:{cl.Server.port}' due to exception.");
                        Console.WriteLine(e);
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private static async Task StartupConnections() {
            foreach (var conn in cfg.StartupConnections) {
                try {
                    await MakeConnection(conn.srv.host, conn.srv.port, conn.ssl);
                } catch (Exception e) {
                    Console.WriteLine($"Failed to start up connection '{conn.srv.host}:{conn.srv.port}' (SSL => {conn.ssl}) due to exception.");
                    Console.WriteLine(e);
                }
            }
        }

        private static async Task HandleListenServer() {
            var ssl = cfg.ListenSsl;
            var host = cfg.ListenHost;
            var port = cfg.ListenPort;

            // Check for command line overrides.
            if (cmd.Opts.Ssl.HasValue)
                ssl = cmd.Opts.Ssl.Value;

            if (cmd.Opts.Host != null)
                host = cmd.Opts.Host;

            if (cmd.Opts.Port.HasValue)
                port = (ushort) cmd.Opts.Port.Value;

            servers.Add(new() {
                Ssl = ssl,
                Bind = new() {
                    host = host,
                    port = port
                }
            });

            var srv = servers[^1];

            // Attempt to listen.
            try {
                srv.Listen();
            } catch (Exception e) {
                Console.WriteLine($"Failed to listen on '{host}:{port}' due to exception.");
                Console.WriteLine(e);

                return;
            }

            // Attempt to process server messages.
            try {
                await ServerProcess(srv);
            } catch (Exception e) {
                Console.WriteLine($"Failed to proces server '{host}:{port}' due to exception.");
                Console.WriteLine(e);
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
                if (cmd.Opts.Cfg == null)
                    Console.WriteLine("Config path somehow null?");
                else
                    cfg.Load(cmd.Opts.Cfg);
            } catch (Exception e) {
                Console.WriteLine("Failed to load and read config file due to exception.");
                Console.WriteLine(e);
            }

            // Check if we should print config and exit.
            if (cmd.Opts.List) {
                cfg.Print();

                return 0;
            }

            // Connect to startup servers from config.
            try {
                await StartupConnections();
            } catch (Exception e) {
                Console.WriteLine("Failed to start initial server connections due to exception.");
                Console.WriteLine(e);
            }

            // We'll want to spin up a new task to handle adding client connections.
            #pragma warning disable CS4014
            Task.Factory.StartNew(() => HandleAllIncoming());
            #pragma warning restore CS4014

            // Spin up another task for listen server if enabled.
            var listen = cfg.Listen;

            if (cmd.Opts.NoListen)
                listen = false;

            if (listen) {
                Console.WriteLine($"Attempting to listen on '{cfg.ListenHost}:{cfg.ListenPort}'...");

                #pragma warning disable CS4014
                Task.Factory.StartNew(() => HandleListenServer());
                #pragma warning restore CS4014
            }

            // Print top menu now.
            PrintTopMenu();

            while (!exit) {
                // Check our current connection.
                if (curIndex == -1) {
                    Console.Write("Cmd: ");
                    try {
                        var input = Console.ReadLine();

                        if (input != null)
                            await ParseTopLine(input);

                    } catch (Exception e) {
                        Console.WriteLine("Failed to read user input due to exception.");
                        Console.WriteLine(e);

                        return 1;
                    }
                } else {
                    try {
                        // Note to self; PLEASE IMPROVE THE BELOW IN THE FUTURE. IT'S BAD!
                        if (isServer) {
                            var srv = servers[curIndex];
                        }
                        else {
                            var cl = clients[curIndex];
                        }
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to connect to {(isServer ? "server" : "client")} at index {curIndex}");
                        Console.WriteLine(e);

                        curIndex = -1;

                        continue;
                    }

                    Console.Write("Msg: ");

                    try {
                        var input = Console.ReadLine();

                        if (input != null)
                            await HandleMessage(input);
                    } catch (Exception e) {
                        Console.WriteLine($"Failed to handle message due to exception.");
                        Console.WriteLine(e);

                        continue;
                    }
                }
            }

            return 0;
        }
    }
}