using RePlays.Integrations;
using System;
using System.Threading.Tasks;

namespace RePlays.Services
{
    public static class IntegrationService
    {
        private static readonly String LEAGUE_OF_LEGENDS = "League of Legends";
        public static Integration ActiveGameIntegration;
        public static async void Start(String gameName)
        {
            if(gameName == LEAGUE_OF_LEGENDS)
            {
                ActiveGameIntegration = new LeagueOfLegendsIntegration();
            }
            else
            {
                ActiveGameIntegration = null;
            }

            if (ActiveGameIntegration == null)
                return;

            await Task.Run(() => ActiveGameIntegration.Start());
        }

        public static async void Shutdown()
        {
            if (ActiveGameIntegration == null)
                return;
            await Task.Run(() => ActiveGameIntegration.Shutdown());
        }
        }
}
