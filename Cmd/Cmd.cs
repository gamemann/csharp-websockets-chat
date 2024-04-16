using CommandLine;

namespace Program {
    public class Cmd {
        public class Options {
            [Option('z', "cfg", Required = false, Default = "./conf.json", HelpText = "The location of the config file.")]
            public string? Cfg { set; get; }

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