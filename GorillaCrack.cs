using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using static MothershipApi;
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
    [BepInPlugin("veeanti.union-crax.gorillacrack", "Let Your Gorilla Smoke A Little Crack", "1.0.0.0")]
    public class CustomServersPlugin : BaseUnityPlugin
    {
        private static CustomServersPlugin instance;

        private string photonRealtimeAppId;
        private string photonFusionAppId;
        private string photonPunAppId;
        private string photonVoiceAppId;
        private string photonQuantumAppId;
        private string photonFunAppId;
        private string photonChatAppId;
        private string photonRegion;
        private string playfabTitleId;
        private string playfabUrl;
        private string mothershipUrl;
        private uint steamAppId;

        private void Awake()
        {
            instance = this;
            // Load configuration from cfg file
            LoadConfig();

            // Apply patches
            var harmony = new Harmony("veeanti.union-crax.gorillacrack");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo("Gorilla's crack pipe is now loaded");
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "veeanti.union-crax.gorillacrack.cfg");
            if (!File.Exists(configPath))
            {
                Logger.LogWarning("Config file not found, using defaults");
                photonRealtimeAppId = "your-realtime-app-id";
                photonFusionAppId = "your-fusion-app-id";
                photonPunAppId = "your-pun-app-id";
                photonVoiceAppId = "your-voice-app-id";
                photonQuantumAppId = "your-quantum-app-id";
                photonFunAppId = "your-fun-app-id";
                photonChatAppId = "your-chat-app-id";
                photonRegion = "us";
                playfabTitleId = "your-custom-playfab-title-id";
                playfabUrl = "https://your-custom-playfab-url.playfabapi.com";
                mothershipUrl = "https://your-custom-mothership-url.com";
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

            photonRealtimeAppId = GetConfigValue(config, "Photon", "RealtimeAppId", "your-realtime-app-id");
            photonFusionAppId = GetConfigValue(config, "Photon", "FusionAppId", "your-fusion-app-id");
            photonPunAppId = GetConfigValue(config, "Photon", "PunAppId", "your-pun-app-id");
            photonVoiceAppId = GetConfigValue(config, "Photon", "VoiceAppId", "your-voice-app-id");
            photonQuantumAppId = GetConfigValue(config, "Photon", "QuantumAppId", "your-quantum-app-id");
            photonFunAppId = GetConfigValue(config, "Photon", "FunAppId", "your-fun-app-id");
            photonChatAppId = GetConfigValue(config, "Photon", "ChatAppId", "your-chat-app-id");
            photonRegion = GetConfigValue(config, "Photon", "Region", "us");
            playfabTitleId = GetConfigValue(config, "PlayFab", "TitleId", "your-custom-playfab-title-id");
            playfabUrl = GetConfigValue(config, "PlayFab", "Url", "https://your-custom-playfab-url.playfabapi.com");
            mothershipUrl = GetConfigValue(config, "Mothership", "BaseUrl", "https://your-custom-mothership-url.com");
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
                appId = Instance.steamAppId;
            }
        }

        [HarmonyPatch(typeof(SteamAPI), "SteamInternal_SteamAPI_Init")]
        public class SteamInternalInitPatch
        {
            static void Prefix(ref uint appId)
            {
                appId = Instance.steamAppId;
            }
        }

        // Singleton access for patches
        private static CustomServersPlugin Instance => (CustomServersPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(typeof(CustomServersPlugin));
    }
}