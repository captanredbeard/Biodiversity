using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using biodiversity;
using ProperVersion;

namespace biodiversity.src.System
{
    public class bdWelcomeSystem : ModSystem
    {
        private ICoreClientAPI capi;
        List<ModInfo> installedMods = new List<ModInfo>();
        List<string> latestversions = new List<string>(); // Fix 1: initialize the list

        public override double ExecuteOrder()
        {
            return 0.11;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            api.ChatCommands.Create("nobdwelcome")
                .WithDescription("Disables the Biodiversity welcome message.")
                .RequiresPlayer()
                .HandleWith(OnWelcomeMsgDisable);

           // versionInstalledSubmods(api);
            api.Event.PlayerJoin += Event_WelcomeMessage;
        }
        private TextCommandResult OnWelcomeMsgDisable(TextCommandCallingArgs args)
        {
            biodiversityModSystem.cConfig.DisableWelcomeMsg = true;
            capi.StoreModConfig(biodiversityModSystem.cConfig, "biodiversity/client.json");

            capi.ShowChatMessage(Lang.Get("biodiversity:welcomedisabled"));
            return TextCommandResult.Success();
        }

        private void Event_WelcomeMessage(IClientPlayer byPlayer)
        {
            if (biodiversityModSystem.cConfig.DisableWelcomeMsg)
            {
                return;
            }

            var installedModsString = parseInstalledMods();

            // Fix 5: check for empty string, not null, since StringBuilder never returns null
            if (string.IsNullOrEmpty(installedModsString))
            {
                installedModsString = Lang.Get("biodiversity:nomodsmsg");
            }

            var welcomeMsg = capi.IsSinglePlayer
                ? Lang.Get("biodiversity:singleplayerwelcome", byPlayer.PlayerName, installedModsString)
                : Lang.Get("biodiversity:serverwelcome", byPlayer.PlayerName, installedModsString);

            capi.ShowChatMessage(welcomeMsg);
        }


        private string parseInstalledMods()
        {
            var s = new StringBuilder();
            
            List<string> subMods = new List<string>()
            {
                "bdaqua",
                "bdcrop",
                "bdflower",
                "bdherb",
                "bdorchard",
                "bdshrub",
                "bdtree"
            };

            foreach (var item in capi.ModLoader.Mods)
            {
                if (subMods.Contains(item.Info.ModID))
                {
                    s.AppendLine($"{item.Info.Name} ({item.Info.Version})");
                }
            }
            return s.ToString();
        }

        public async Task<string?> LatestModVersionAsync(string modid)
        {
            JObject? mod = await GetModJsonAsync(modid);

            if (mod == null) return null;

            var releases = mod["mod"]?["releases"] as JArray;

            if (releases == null || !releases.Any()) return null;

            var latest = releases
                .OrderByDescending(r => DateTime.Parse(r["created"]!.ToString()))
                .First();

            return latest["modversion"]?.ToString();
        }

        public static async Task<JObject?> GetModJsonAsync(string modid)
        {
            using HttpClient client = new();

            string url = $"https://mods.vintagestory.at/api/mod/{modid}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            return JObject.Parse(json);
        }
    }
}