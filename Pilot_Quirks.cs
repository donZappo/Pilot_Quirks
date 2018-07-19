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

        //This section works, but I'm not sure if I want the commander to utilize starting tags. They don't even start with Pilot Tags. 
        //[HarmonyPatch(typeof(SimGameState), "FirstTimeInitializeDataFromDefs")]
        //public static class Commander_Initial_Setup
        //{
        //    public static void Postfix(SimGameState __instance)
        //    {
        //        Helper.Logger.LogLine("Add Commander");
        //        if (__instance.Commander.pilotDef.PilotTags.Contains("commander_ancestry_steiner"))
        //        {
        //            Helper.Logger.LogLine("Tech Found");
        //            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster", new Type[] { typeof(PilotDef), typeof(bool) } )]
        public static class Pilot_Gained
        {
            public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false)
            {
                if (updatePilotDiscardPile == true)
                {
                    if (def.PilotTags.Contains("pilot_tech"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                    }

                    if (def.PilotTags.Contains("pilot_disgraced"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
                    }
                }
               
            }
        }

        [HarmonyPatch(typeof(SimGameState), "KillPilot", new Type[] { typeof(Pilot) })]
        public static class Pilot_Died
        {
            public static void Postfix(SimGameState __instance, Pilot p)
            {
                Helper.Logger.LogLine("Pilot_Died");
                Helper.Logger.LogLine(p.ToString());
                Helper.Logger.LogLine(p.pilotDef.PilotTags.Contains("pilot_tech").ToString());
                if (p.pilotDef.PilotTags.Contains("pilot_tech"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_tech_TechBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_disgraced"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_disgraced_MoralePenalty, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "DismissPilot", new Type[] { typeof(Pilot) })]
        public static class Pilot_Dismissed
        {
            public static void Prefix(SimGameState __instance, Pilot p)
            {
                if (p.pilotDef.PilotTags.Contains("pilot_tech"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_tech_TechBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_disgraced"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_disgraced_MoralePenalty, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "_OnDefsLoadComplete")]
        public static class Initialize_New_Game
        {
            public static void Postfix(SimGameState __instance)
            {
                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_tech"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                    }

                    if (pilot.pilotDef.PilotTags.Contains("pilot_disgraced"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "OnDayPassed")]
        public static class SaveGameUpdater
        {
            public static void Postfix(SimGameState __instance)
            {
                if (settings.IsSaveGame)
                {
                    foreach (Pilot pilot in __instance.PilotRoster)
                    {
                        if (pilot.pilotDef.PilotTags.Contains("pilot_tech"))
                        {
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                        }

                        if (pilot.pilotDef.PilotTags.Contains("pilot_disgraced"))
                        {
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SimGameState), "GetReputationShopAdjustment", new Type[] { typeof(Faction) })]
        public static class Merchant_Bonus
        {
            public static void Postfix(SimGameState __instance, ref float __result)
            {
                float MerchantCount = 0;
                foreach(Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_merchant"))
                    {
                        MerchantCount = MerchantCount + 1;
                    }
                }
                __result = __result - settings.pilot_merchant_ShopDiscount * MerchantCount/100;
            }
        }
        [HarmonyPatch(typeof(Pilot), "InjurePilot")]
        public static class Lucky_Pilot
        {
            public static void Prefix(Pilot __instance, int dmg)
            {
                var rng = new System.Random();
                int Roll = rng.Next(1, 101);
                if (Roll <= settings.pilot_lucky_InjuryAvoidance)
                {
                    dmg = 0;
                }
            }
        }

        [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInData")]
        public static class Adjust_Pilot_XP
        {
            public static void Prefix(AAR_UnitStatusWidget __instance, int xpEarned, ref int __result, UnitResult __UnitData)
            {
                if (__UnitData.pilot.pilotDef.PilotTags.Contains("pilot_naive"))
                {
                    float XPModifier = 1 - settings.pilot_naive_LessExperience;
                    __result = (int)(XPModifier * (float)xpEarned);
                }
            }
        }
            /// <summary>
            /// Following is to-hit modifiers area.
            /// </summary>

        [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
        public static class ToHit_GetAllModifiers_Patch
        {
            private static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target, Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
            {
                Pilot pilot = attacker.GetPilot();
                Pilot TargetPilot = target.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("pilot_reckless"))
                {
                    __result = __result + (float)settings.pilot_reckless_ToHitBonus;
                }
                if (TargetPilot.pilotDef.PilotTags.Contains("pilot_reckless"))
                {
                    __result = __result + (float)settings.pilot_reckless_ToBeHitBonus;
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_cautious"))
                {
                    __result = __result + (float)settings.pilot_reckless_ToHitBonus;
                }
                if (TargetPilot.pilotDef.PilotTags.Contains("pilot_cautious"))
                {
                    __result = __result + (float)settings.pilot_reckless_ToBeHitBonus;
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_drunk") && pilot.pilotDef.TimeoutRemaining > 0)
                {
                    __result = __result + (float)settings.pilot_drunk_ToHitBonus;
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_lostech") && weapon.componentDef.ComponentTags.Contains("component_type_lostech"))
                {
                    __result = __result + (float)settings.pilot_lostech_ToHitBonus;
                }
            }
        }

        [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
        public static class ToHit_GetAllModifiersDescription_Patch
        {
            private static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target, Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot)
            {
                Pilot pilot = attacker.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("pilot_reckless"))
                {
                    __result = string.Format("{0}RECKLESS {1:+#;-#}; ", __result, settings.pilot_reckless_ToHitBonus);
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_drunk") && pilot.pilotDef.TimeoutRemaining > 0)
                {
                    __result = string.Format("{0}DRUNK {1:+#;-#}; ", __result, settings.pilot_drunk_ToHitBonus);
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_lostech") && weapon.componentDef.ComponentTags.Contains("component_type_lostech"))
                {
                    __result = string.Format("{0}LOSTECH TECHNICIAN {1:+#;-#}; ", __result, settings.pilot_lostech_ToHitBonus);
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
        public static class CombatHUDWeaponSlot_SetHitChance_Patch
        {
            private static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target)
            {
                AbstractActor actor = __instance.DisplayedWeapon.parent;
                var _this = Traverse.Create(__instance);
                Pilot pilot = actor.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("pilot_reckless"))
                {
                    _this.Method("AddToolTipDetail", "RECKLESS", settings.pilot_reckless_ToHitBonus).GetValue();
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_drunk") && pilot.pilotDef.TimeoutRemaining > 0)
                {
                    _this.Method("AddToolTipDetail", "DRUNK", settings.pilot_drunk_ToHitBonus).GetValue();
                }

                if (__instance.tag.Contains("component_type_lostech") && pilot.pilotDef.PilotTags.Contains("pilot_lostech"))
                {
                    _this.Method("AddToolTipDetail", "LOSTECH TECH", settings.pilot_lostech_ToHitBonus).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "GetEvasivePipsResult")]
        public static class Drunk_Evasive_Malus
        {
            private static void Postfix(Mech __instance, ref int __result)
            {
                if (__instance.pilot.pilotDef.PilotTags.Contains("pilot_drunk") && __instance.pilot.pilotDef.TimeoutRemaining > 0)
                    __result = __result - 1;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetMechWarriorValue")]
        public static class Wealthy_For_free
        {
            private static void Postfix(SimGameState __instance, PilotDef def, ref int __result)
            {
                if (def.PilotTags.Contains("pilot_wealthy"))
                    __result = 0;
            }
        }



        [HarmonyPatch(typeof(Mech), "GetHitLocation", new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(ArmorLocation), typeof(float) })]
        public static class Assassin_Patch
        {
            private static void Prefix(Mech __instance, AbstractActor attacker, float bonusMultiplier)
            {
                Pilot pilot = attacker.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                {
                    bonusMultiplier = bonusMultiplier + settings.pilot_assassin_CalledShotBonus;
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
            public int pilot_tech_TechBonus = 100;
            public int pilot_reckless_ToHitBonus = -1;
            public int pilot_reckless_ToBeHitBonus = -1;
            public int pilot_cautious_ToHitBonus = 1;
            public int pilot_cautious_ToBeHitBonus = 1;
            public float pilot_assassin_CalledShotBonus = 0.25f;
            public float pilot_merchant_ShopDiscount = 1;
            public int pilot_lucky_InjuryAvoidance = 10;
            public int pilot_disgraced_MoralePenalty = -1;

            public int pilot_drunk_ToHitBonus = 1;
            public int pilot_lostech_ToHitBonus = -1;
            public float pilot_naive_LessExperience = 0.1f;


            public bool IsSaveGame = false;

        }
    }
}