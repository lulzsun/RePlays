using RePlays.Integrations;

namespace RePlays.Services {
    public static class IntegrationService {
        private const string LEAGUE_OF_LEGENDS = "League of Legends";
        private const string PUBG = "PLAYERUNKNOWN'S BATTLEGROUNDS";
        private const string CS2 = "Counter-Strike 2";
        private const string CSGO = "Counter-Strike Global Offensive";
        static Integration ActiveGameIntegration;
        public static async void Start(string gameName) {
            switch (gameName) {
                case LEAGUE_OF_LEGENDS:
                    ActiveGameIntegration = new LeagueOfLegendsIntegration();
                    break;
                case PUBG:
                    ActiveGameIntegration = new PubgIntegration();
                    break;
                case CSGO:
                case CS2:
                    ActiveGameIntegration = new CS2();
                    break;
                default:
                    ActiveGameIntegration = null;
                    break;
            }

            if (ActiveGameIntegration == null) return;
            await ActiveGameIntegration.Start();
        }

        public static async void Shutdown() {
            if (ActiveGameIntegration == null)
                return;
            await ActiveGameIntegration.Shutdown();
        }
    }
}