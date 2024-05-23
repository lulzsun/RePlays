﻿using RePlays.Integrations;

namespace RePlays.Services {
    public static class IntegrationService {
        private const string LEAGUE_OF_LEGENDS = "League of Legends";
        private const string PUBG = "PLAYERUNKNOWN'S BATTLEGROUNDS";
        private const string CS2 = "Counter-Strike 2";
        private const string CSGO = "Counter-Strike Global Offensive";
        private static Integration activeGameIntegration;
        public static Integration ActiveGameIntegration { get { return activeGameIntegration; } }
        public static async void Start(string gameName) {
            switch (gameName) {
                case LEAGUE_OF_LEGENDS:
                    activeGameIntegration = new LeagueOfLegendsIntegration();
                    break;
                case PUBG:
                    activeGameIntegration = new PubgIntegration();
                    break;
                case CSGO:
                case CS2:
                    activeGameIntegration = new CS2();
                    break;
                default:
                    activeGameIntegration = null;
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