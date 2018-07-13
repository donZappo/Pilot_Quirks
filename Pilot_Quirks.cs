using System;
using System.Reflection;
using BattleTech;
using Harmony;
using BattleTech.UI;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using System.Collections.Generic;


namespace Pilot_Quirks
{
    public static class Pre_Control
    {
        public const string ModName = "Pilot_Quirks";
        public const string ModId = "dZ.Zappo.Pilot_Quirks";

        internal static ModSettings settings;
        internal static string ModDirectory;

        public static void Init(string directory, string modSettings)
        {
            Helper.Logger.LogLine("Init");
            ModDirectory = directory;
            try
            {
                settings = JsonConvert.DeserializeObject<ModSettings>(modSettings);
            }
            catch (Exception)
            {
                settings = new ModSettings();
            }

            var harmony = HarmonyInstance.Create(ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }


        [HarmonyPatch(typeof(SimGameState), "HirePilot", new Type[] { typeof(Pilot) })]
        public static class Pilot_Gained
        {
            public static void Postfix(SimGameState __instance, PilotDef __ref)
            {
                Helper.Logger.LogLine("HirePilot");
                Helper.Logger.LogLine(__ref.ToString());
                Helper.Logger.LogLine(__ref.PilotTags.Contains("pilot_tech").ToString());
                if (__ref.PilotTags.Contains("pilot_tech"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.TechBonus, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "KillPilot", new Type[] { typeof(Pilot) })]
        public static class Pilot_Died
        {
            public static void Postfix(SimGameState __instance, Pilot __ref)
            {
                Helper.Logger.LogLine("Pilot_Died");
                Helper.Logger.LogLine(__ref.ToString());
                Helper.Logger.LogLine(__ref.pilotDef.PilotTags.Contains("pilot_tech").ToString());
                if (__ref.pilotDef.PilotTags.Contains("pilot_tech"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.TechBonus, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "DismissPilot", new Type[] { typeof(Pilot) })]
        public static class Pilot_Dismissed
        {
            public static void Prefix(SimGameState __instance, Pilot p)
            {
                Helper.Logger.LogLine("FirePilot");
                Helper.Logger.LogLine(p.ToString());
                Helper.Logger.LogLine(p.pilotDef.PilotTags.Contains("pilot_tech").ToString());
                if (p.pilotDef.PilotTags.Contains("pilot_tech"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.TechBonus, -1, true);
                }
            }
        }

        public static class Helper
        {
            public static Settings LoadSettings()
            {
                Settings result;
                try
                {
                    using (StreamReader streamReader = new StreamReader("Mods/Pilot_Quirks/settings.json"))
                    {
                        result = JsonConvert.DeserializeObject<Settings>(streamReader.ReadToEnd());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    result = null;
                }
                return result;
            }
            public class Logger
            {
                public static void LogError(Exception ex)
                {
                    using (StreamWriter streamWriter = new StreamWriter("Mods/Pilot_Quirks/Log.txt", true))
                    {
                        streamWriter.WriteLine(string.Concat(new string[]
                        {
                        "Message :",
                        ex.Message,
                        "<br/>",
                        Environment.NewLine,
                        "StackTrace :",
                        ex.StackTrace,
                        Environment.NewLine,
                        "Date :",
                        DateTime.Now.ToString()
                        }));
                        streamWriter.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                    }
                }

                public static void LogLine(string line)
                {
                    string path = "Mods/Pilot_Quirks/Log.txt";
                    using (StreamWriter streamWriter = new StreamWriter(path, true))
                    {
                        streamWriter.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                        streamWriter.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                    }
                }
            }
        }
        internal class ModSettings
        {
            public int TechBonus = 100;


            public int FatigueTimeStart = 7;
            public int MoraleModifier = 5;
            public int StartingMorale = 25;
            public int FatigueMinimum = 0;
            public int MoralePositiveTierOne = 5;
            public int MoralePositiveTierTwo = 15;
            public int MoraleNegativeTierOne = -5;
            public int MoraleNegativeTierTwo = -15;
            public double FatigueFactor = 2.5;
            public bool InjuriesHurt = true;
        }
    }
}