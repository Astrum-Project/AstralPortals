using Astrum.AstralCore.Types;
using MelonLoader;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = Astrum.AstralCore.Logger;

[assembly: MelonInfo(typeof(Astrum.AstralPortals), "AstralPortals", "0.1.0", downloadLink: "github.com/Astrum-Project/AstralPortals")]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

namespace Astrum
{
    public class AstralPortals : MelonMod
    {
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        public static MethodInfo m_ConfigurePortal;

        //public static bool infinite;
        //public static bool theft;
        //public static bool multi;
        public static bool floor = true;
        public static bool log = true;
        //public static bool spoof = true;

        public override void OnApplicationStart()
        {
            m_ConfigurePortal = AppDomain.CurrentDomain.GetAssemblies()
                .First(x => x.GetName().Name == "Assembly-CSharp")
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
            category.CreateEntry("floor", true, "Floor Portals");
            category.CreateEntry("log", true, "Log Portals");

            OnPreferencesLoaded();
        }

        public override void OnPreferencesSaved() => OnPreferencesLoaded();
        public override void OnPreferencesLoaded()
        {
            MelonPreferences_Category category = MelonPreferences.GetCategory("Astrum-AstralPortals");
            floor = category.GetEntry<bool>("floor").Value;
            log = category.GetEntry<bool>("log").Value;
        }

        private static System.Collections.IEnumerator SetupFloortal(Transform portal)
        {
            yield return null;
            yield return null;

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

        private static void HookConfigurePortalPre(MonoBehaviour __3) 
        {
            Player player = new Player(__3);

            if (log)
            {
                Logger.Info(player.APIUser.displayName + " dropped a portal");
                MelonCoroutines.Start(AstralCore.Managers.LogManager.DisplayOnScreen($"<color=#5ab2a8>{player.APIUser.displayName}</color> dropped a portal", 5f));
            }
        }

        private static void HookConfigurePortalPost(MonoBehaviour __instance)
        {
            MelonCoroutines.Start(SetupFloortal(__instance.transform));
        }
    }
}
