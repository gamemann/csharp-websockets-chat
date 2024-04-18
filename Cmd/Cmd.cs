using CommandLine;

namespace Program {
    public class Cmd {
        public class Options {
            [Option('z', "cfg", Required = false, Default = "./conf.json", HelpText = "The location of the config file.")]
            public string? Cfg { set; get; }

            [Option(longName:"nolisten", Required = false, Default = false, HelpText = "Disables the listen server.")]
            public bool NoListen { get; set; }

            [Option(longName:"host", Required = false, Default = null, HelpText = "The host to listen on. Overrides config.")]
            public string? Host { get; set; }
    
            [Option(longName:"port", Required = false, Default = null, HelpText = "The port to listen on. Overrides config.")]
            public int? Port { get; set; }

            [Option(longName:"ssl", Required = false, Default = null, HelpText = "Whether to listen with SSL. Overrides config.")]
            public bool? Ssl { get; set; }

            [Option('l', "list", Required = false, Default = false, HelpText = "Whether to print config file.")]
            public bool List { set; get; }
        }

        private Options opts = new();
        public Options Opts {
            get => opts;
            set => opts = value;
        }

        public void Parse(string[] args) {
            Parser.Default.ParseArguments<Options>(args).WithParsed((o) => {
                opts = o;
            });
        }
    }
}