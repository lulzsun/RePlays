using System.Threading.Tasks;

namespace RePlays.Integrations {
    public abstract class Integration {
        public abstract Task Start();
        public abstract Task Shutdown();
    }
}
