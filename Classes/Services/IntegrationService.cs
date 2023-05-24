using RePlays.Integrations;
using System.Threading.Tasks;

namespace RePlays.Services {
    public static class IntegrationService {
        private const string LEAGUE_OF_LEGENDS = "League of Legends";
        private const string PUBG = "PLAYERUNKNOWN'S BATTLEGROUNDS";
        public static Integration ActiveGameIntegration;
        public static async void Start(string gameName) {
            switch (gameName) {
                case LEAGUE_OF_LEGENDS:
                    ActiveGameIntegration = new LeagueOfLegendsIntegration();
                    break;
                case PUBG:
                    ActiveGameIntegration = new PubgIntegration();
                    break;
                default:
                    ActiveGameIntegration = null;
                    break;
            }

            if (ActiveGameIntegration == null) return;
            await Task.Run(() => ActiveGameIntegration.Start());
        }

        public static async void Shutdown() {
            if (ActiveGameIntegration == null)
                return;
            await Task.Run(() => ActiveGameIntegration.Shutdown());
        }
    }
}
