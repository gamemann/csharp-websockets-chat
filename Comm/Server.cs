namespace Program.Comm {
    public class Server {
        private Flow flow = new();
        public Flow Flow {
            get => flow;
            set => flow = value;
        }
    }
}