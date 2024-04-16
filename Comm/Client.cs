namespace Program.Comm {
    public class Client {
        private Flow flow = new();
        public Flow Flow {
            get => flow;
            set => flow = value;
        }
    }
}