using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace GorillaTagCustomServers
{
    [BepInPlugin("veeanti.union-crax.gorillacrack", "Let that munky smonk some crAck!!", "1.0.0.1")]
    public class CustomServersPlugin : BaseUnityPlugin
    {
        private static CustomServersPlugin instance;

        private string photonRealtimeAppId;
        private string photonPunAppId;
        private string photonRegion;
        private string playfabTitleId;
        private string playfabPlayFabUrl;
        private uint steamAppId;

        private void Awake()
        {
            instance = this;
            // Load configuration from cfg file
            LoadConfig();

            // Set static values
            PlayFabSettings.TitleId = playfabTitleId;

            // Spoof steam_appid.txt to force Spacewar recognition
            string steamAppIdFile = Path.Combine(Paths.GameRootPath, "steam_appid.txt");
            File.WriteAllText(steamAppIdFile, steamAppId.ToString());
            Logger.LogInfo($"Spoofed steam_appid.txt to {steamAppId}");

            // Apply patches
            var harmony = new Harmony("veeanti.union-crax.gorillacrack");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo("Gorilla's crack pipe is now loaded");
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(Paths.GameRootPath, "union-crax.ini");
            if (!File.Exists(configPath))
            {
                Logger.LogWarning("Config file not found, using defaults");
                photonRealtimeAppId = "fa353756-050a-42db-bf0c-88a2d29b4ef5";
                photonPunAppId = "0f184139-4851-45d1-a250-9f062cdf121c";
                photonRegion = "us";
                playfabTitleId = "16F775";
                playfabPlayFabUrl = "https://16F775.playfabapi.com";
                steamAppId = 480;
                return;
            }

            var config = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";
            foreach (var line in File.ReadAllLines(configPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#")) continue;
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    config[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != "" && trimmed.Contains("="))
                {
                    var parts = trimmed.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        config[currentSection][parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            photonRealtimeAppId = GetConfigValue(config, "Photon", "RealtimeAppId", "");
            photonPunAppId = GetConfigValue(config, "Photon", "PunAppId", "");
            photonRegion = GetConfigValue(config, "Photon", "Region", "");
            playfabTitleId = GetConfigValue(config, "PlayFab", "TitleId", "");
            playfabPlayFabUrl = GetConfigValue(config, "PlayFab", "PlayFabUrl", "");
            steamAppId = uint.TryParse(GetConfigValue(config, "Steam", "AppId", "480"), out uint appId) ? appId : 480;
        }

        private string GetConfigValue(Dictionary<string, Dictionary<string, string>> config, string section, string key, string defaultValue)
        {
            if (config.ContainsKey(section) && config[section].ContainsKey(key))
            {
                return config[section][key];
            }
            return defaultValue;
        }

        // Photon patches
        [HarmonyPatch(typeof(PhotonNetwork), "ConnectUsingSettings")]
        public class PhotonConnectPatch
        {
            static void Prefix(ref AppSettings appSettings)
            {
                if (appSettings != null)
                {
                    appSettings.AppIdRealtime = Instance.photonRealtimeAppId;
                    appSettings.AppVersion = "1.0.0.0"; // Set custom version if needed
                    // Region can be set via PhotonNetwork.ConnectToRegion
                }
            }
        }

        [HarmonyPatch(typeof(PhotonNetwork), "ConnectToRegion")]
        public class PhotonRegionPatch
        {
            static void Prefix(ref string region)
            {
                region = Instance.photonRegion;
            }
        }

        // Steam patches
        [HarmonyPatch(typeof(SteamAPI), "Init", typeof(uint))]
        public class SteamInitPatch
        {
            static void Prefix(ref uint appId)
            {
                if (appId == 1533390 || appId == 0) appId = Instance.steamAppId;
            }
        }

        [HarmonyPatch(typeof(SteamAPI), "SteamInternal_SteamAPI_Init")]
        public class SteamInternalInitPatch
        {
            static void Prefix(ref uint appId)
            {
                if (appId == 1533390 || appId == 0) appId = Instance.steamAppId;
            }
        }

        [HarmonyPatch(typeof(SteamUtils), "GetAppID")]
        public class SteamGetAppIDPatch
        {
            static bool Prefix(ref uint __result)
            {
                __result = Instance.steamAppId;
                return false; // Skip original
            }
        }

        [HarmonyPatch(typeof(SteamApps), "BIsSubscribedApp", typeof(uint))]
        public class SteamBIsSubscribedAppPatch
        {
            static bool Prefix(uint appId, ref bool __result)
            {
                if (appId == Instance.steamAppId)
                {
                    __result = true;
                    return false; // Skip original
                }
                if (appId == 1533390)
                {
                    __result = false;
                    return false;
                }
                return true; // Call original for other apps
            }
        }

        [HarmonyPatch(typeof(SteamApps), "BIsAppInstalled", typeof(uint))]
        public class SteamBIsAppInstalledPatch
        {
            static void Prefix(ref uint appId)
            {
                if (appId == 1533390) appId = Instance.steamAppId;
            }
        }

        [HarmonyPatch(typeof(SteamApps), "GetAppInstallDir", typeof(uint), typeof(System.Text.StringBuilder), typeof(uint))]
        public class SteamGetAppInstallDirPatch
        {
            static void Prefix(ref uint appId)
            {
                if (appId == 1533390) appId = Instance.steamAppId;
            }
        }

        [HarmonyPatch(typeof(SteamAPI), "RestartAppIfNecessary", typeof(uint))]
        public class SteamRestartAppIfNecessaryPatch
        {
            static void Prefix(ref uint appId)
            {
                if (appId == 1533390) appId = Instance.steamAppId;
            }
        }

        [HarmonyPatch(typeof(SteamApps), "BIsDlcInstalled", typeof(uint), typeof(uint))]
        public class SteamBIsDlcInstalledPatch
        {
            static void Prefix(ref uint appId)
            {
                if (appId == 1533390) appId = Instance.steamAppId;
            }
        }

        // Singleton access for patches
        private static CustomServersPlugin Instance => (CustomServersPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(typeof(CustomServersPlugin));
    }
}