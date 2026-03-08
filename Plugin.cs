using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Consoleless
{
    [BepInPlugin("BrokenStone.Consoleless", "Consoleless", "1.0.3")]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly System.Random random = new System.Random();

        public void Awake()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string id = new string(Enumerable.Repeat(chars, random.Next(8, 513))
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var harmony = new Harmony(id);
            harmony.PatchAll();

            GameObject obj = new GameObject("ConsolelessNotifications");
            obj.AddComponent<NotificationManager>();

            Debug.Log("[Consoleless] Initialized");
        }
    }

    public class Constants
    {
        public static List<string> BlockedUrls = new List<string>()
        {
            "https://iidk.online/",
            "https://raw.githubusercontent.com/iiDk-the-actual/Console",
            "https://hamburbur.org/data",
            "https://hamburbur.org/telemetry",
            "https://data.hamburbur.org",
            "https://files.hamburbur.org",
            "https://faggot.click",
            "https://sentinelhook.lol",
            "https://menutrackerapi.onrender.com"
        };
    }

    // ---------------------------
    // Notification System
    // ---------------------------

    public class NotificationManager : MonoBehaviour
    {
        private class Notification
        {
            public string message;
            public float time;
        }

        private static List<Notification> notifications = new List<Notification>();
        private static GUIStyle style;

        public static void Show(string msg, float duration = 4f)
        {
            notifications.Add(new Notification
            {
                message = msg,
                time = Time.time + duration
            });
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.red;
        }

        void OnGUI()
        {
            float y = 10;

            for (int i = notifications.Count - 1; i >= 0; i--)
            {
                var n = notifications[i];

                if (Time.time > n.time)
                {
                    notifications.RemoveAt(i);
                    continue;
                }

                GUI.Label(new Rect(10, y, 900, 30), n.message, style);
                y += 25;
            }
        }
    }

    // ---------------------------
    // UnityWebRequest Patch
    // ---------------------------

    [HarmonyPatch(typeof(UnityWebRequest), nameof(UnityWebRequest.SendWebRequest))]
    public class UnityWebRequestPatch
    {
        [HarmonyPrefix]
        static bool Prefix(UnityWebRequest __instance)
        {
            if (__instance.url != null &&
                Constants.BlockedUrls.Any(blocked => __instance.url.StartsWith(blocked)))
            {
                Debug.Log($"[Consoleless] Blocked {__instance.url}");
                NotificationManager.Show($"Blocked request: {__instance.url}");

                __instance.url = null;
            }

            return true;
        }
    }

    // ---------------------------
    // HttpClient Patch
    // ---------------------------

    [HarmonyPatch(typeof(HttpClient), nameof(HttpClient.GetByteArrayAsync), new[] { typeof(string) })]
    public class HttpClientPatch
    {
        [HarmonyPrefix]
        static bool Prefix(string requestUri, ref Task<byte[]> __result)
        {
            if (requestUri != null &&
                Constants.BlockedUrls.Any(blocked => requestUri.StartsWith(blocked)))
            {
                Debug.Log($"[Consoleless] Blocked {requestUri}");
                NotificationManager.Show($"Blocked request: {requestUri}");

                __result = Task.FromResult(new byte[0]);
                return false;
            }

            return true;
        }
    }
}
