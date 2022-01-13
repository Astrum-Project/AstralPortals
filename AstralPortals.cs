using Astrum.AstralCore.Types;
using MelonLoader;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRC.SDKBase;
using Logger = Astrum.AstralCore.Logger;

[assembly: MelonInfo(typeof(Astrum.AstralPortals), "AstralPortals", "0.2.2", downloadLink: "github.com/Astrum-Project/AstralPortals")]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

namespace Astrum
{
    public class AstralPortals : MelonMod
    {
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        public static MethodInfo m_ConfigurePortal;

        public static bool log = true;
        public static bool floor = true;
        public static bool theft = false;
        public static PortalMode mode = PortalMode.Normal;

        public override void OnApplicationStart()
        {
            m_ConfigurePortal = AstralCore.Hooks.Hooks.AssemblyCSharp
                .GetTypes()
                .Where(x => x.BaseType == typeof(MonoBehaviour))
                .SelectMany(x => x.GetMethods())
                .Where(x => {
                    ParameterInfo[] p = x.GetParameters();
                    return p.Length == 4
                        && p[0].ParameterType == typeof(string)
                        && p[1].ParameterType == typeof(string)
                        && p[2].ParameterType == typeof(int);
                })
                .FirstOrDefault(x => x.Name == "ConfigurePortal");

            HarmonyInstance.Patch(
                m_ConfigurePortal,
                typeof(AstralPortals).GetMethod(nameof(HookConfigurePortalPre), PrivateStatic).ToNewHarmonyMethod(),
                typeof(AstralPortals).GetMethod(nameof(HookConfigurePortalPost), PrivateStatic).ToNewHarmonyMethod()
            );

            MelonPreferences_Category category = MelonPreferences.CreateCategory("Astrum-AstralPortals", "Astral Portals");
            category.CreateEntry("log", true, "Log Portals");
            category.CreateEntry("floor", true, "Floor Portals");
            category.CreateEntry("theft", false, "Hijack Portals");
            category.CreateEntry("mode", PortalMode.Normal, "Portal Mode");

            OnPreferencesLoaded();
        }

        public override void OnPreferencesSaved() => OnPreferencesLoaded();
        public override void OnPreferencesLoaded()
        {
            MelonPreferences_Category category = MelonPreferences.GetCategory("Astrum-AstralPortals");
            log = category.GetEntry<bool>("log").Value;
            floor = category.GetEntry<bool>("floor").Value;
            theft = category.GetEntry<bool>("theft").Value;
            mode = category.GetEntry<PortalMode>("mode").Value;
        }

        private static System.Collections.IEnumerator SetupFloortal(Transform portal)
        {
            yield return new WaitForSeconds(1);

            if (portal == null) yield break;

            portal.localScale = new Vector3(1, 0.75f, 1);
            portal.position += portal.forward;
            portal.rotation = Quaternion.Euler(270, portal.rotation.eulerAngles.y, portal.rotation.eulerAngles.z);

            portal.Find("NameTag").localPosition = new Vector3(0, 1.25f, 1);
            portal.Find("Timer").localPosition = new Vector3(0, 1.25f, 0.85f);
            portal.Find("PlayerCount").localPosition = new Vector3(0, 1.25f, 0.7f);

            Transform icons = portal.Find("PlatformIcons");
            icons.localRotation = Quaternion.Euler(90, 0, 0);
            icons.localPosition = new Vector3(0.5f, 2, -1.6f);
            icons.Find("Quad").localPosition = new Vector3(-0.5f, 1.86f, 0);
        }

        private static void HookConfigurePortalPre(string __0, string __1, MonoBehaviour __3) 
        {
            Player player = new Player(__3);

            if (log)
            {
                Logger.Notif(player.APIUser.displayName + " dropped a portal");
                Logger.Info($"{__0}:{__1}");
            }
        }

        private static void HookConfigurePortalPost(MonoBehaviour __instance)
        {
            MelonCoroutines.Start(SetupFloortal(__instance.transform));

            if (theft && Networking.GetOwner(__instance.gameObject) != Networking.LocalPlayer)
                Networking.SetOwner(Networking.LocalPlayer, __instance.gameObject);

            if (mode == PortalMode.Blank)
                UnityEngine.Object.Destroy(__instance);
            else if (mode == PortalMode.Frozen)
                MelonCoroutines.Start(FreezePortal(__instance));
        }

        private static System.Collections.IEnumerator FreezePortal(MonoBehaviour portal)
        {
            yield return new WaitForSeconds(1);

            if (portal != null)
                UnityEngine.Object.Destroy(portal);
        }

        public enum PortalMode
        {
            Blank,
            Frozen,
            Normal
        }
    }
}
