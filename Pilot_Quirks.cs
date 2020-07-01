using System;
using System.Reflection;
using BattleTech;
using Harmony;
using BattleTech.UI;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Localize;
using BattleTech.Framework;
using HoudiniEngineUnity;

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

        public static void StartGameAudit(SimGameState __instance)
        {
            foreach (Pilot pilot in __instance.PilotRoster)
            {
                var tags = pilot.pilotDef.PilotTags;
                var stats = __instance.CompanyStats;
                if (tags.Contains("pilot_disgraced"))
                {
                    stats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
                }

                if (tags.Contains("pilot_honest"))
                {
                    stats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_honest_MoraleBonus, -1, true);
                }

                if (tags.Contains("pilot_dishonest"))
                {
                    stats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_dishonest_MoralePenalty, -1, true);
                }
                if (tags.Contains("pilot_tech"))
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
            }
        }

        [HarmonyPatch(typeof(SimGameState), "_OnAttachUXComplete")]
        public static class PatchCampaignStartMorale
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var instructionsToInsert = new List<CodeInstruction>();
                var index = codes.FindIndex(code => code.operand == (object)"Start Game");
                var targetMethod = AccessTools.Method(typeof(Pre_Control), "StartGameAudit");

                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, targetMethod));
                codes.InsertRange(index + 2, instructionsToInsert);

                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster", typeof(PilotDef), typeof(bool), typeof(bool))]
        public static class Pilot_Gained
        {
            public static void Prefix(SimGameState __instance, ref PilotDef def)
            {
                if (def.PilotTags.Contains("pilot_military"))
                {
                    int newXP = def.ExperienceUnspent + settings.pilot_military_XP;
                    def.SetUnspentExperience(newXP);
                }
                if (def.PilotTags.Contains("pilot_mechwarrior"))
                {
                    int newXP = def.ExperienceUnspent + settings.pilot_mechwarrior_XP;
                    def.SetUnspentExperience(newXP);
                }
            }

            public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false)
            {
                if (updatePilotDiscardPile == true)
                {
                    if (def.PilotTags.Contains("pilot_tech") && !settings.pilot_tech_vanillaTech)
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                    }
                    else if (def.PilotTags.Contains("pilot_tech") && settings.pilot_tech_vanillaTech)
                    {
                        int TechCount = 0;
                        foreach (Pilot techpilot in __instance.PilotRoster)
                        {
                            if (def.PilotTags.Contains("pilot_tech"))
                                TechCount = TechCount + 1;
                        }
                        if (TechCount % settings.pilot_tech_TechsNeeded == 0)
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                    }

                    if (def.PilotTags.Contains("pilot_disgraced"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
                    }

                    if (def.PilotTags.Contains("pilot_comstar"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_comstar_TechBonus, -1, true);
                    }

                    if (def.PilotTags.Contains("pilot_honest"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_honest_MoraleBonus, -1, true);
                    }

                    if (def.PilotTags.Contains("pilot_dishonest"))
                    {
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_dishonest_MoralePenalty, -1, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "KillPilot", new Type[] { typeof(Pilot), typeof(bool), typeof(string), typeof(string) })]
        public static class Pilot_Died
        {
            public static void Postfix(SimGameState __instance, Pilot p)
            {
                if (p == null || !(__instance.PilotRoster.Contains(p) || __instance.Graveyard.Contains(p)))
                    return;

                if (p.pilotDef.PilotTags.Contains("pilot_tech") && !settings.pilot_tech_vanillaTech)
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_tech_TechBonus, -1, true);
                }
                else if (p.pilotDef.PilotTags.Contains("pilot_tech") && settings.pilot_tech_vanillaTech)
                {
                    int TechCount = 0;
                    foreach (Pilot techpilot in __instance.PilotRoster)
                    {
                        if (techpilot.pilotDef.PilotTags.Contains("pilot_tech"))
                            TechCount = TechCount + 1;
                    }
                    if (TechCount % settings.pilot_tech_TechsNeeded == 0)
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_tech_TechBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_disgraced"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_disgraced_MoralePenalty, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_comstar"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_comstar_TechBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_honest"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_honest_MoraleBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_dishonest"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_dishonest_MoralePenalty, -1, true);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "DismissPilot", new Type[] { typeof(Pilot) })]
        public static class Pilot_Dismissed
        {
            public static void Prefix(SimGameState __instance, Pilot p)
            {
                if (p.pilotDef.PilotTags.Contains("pilot_tech") && !settings.pilot_tech_vanillaTech)
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_tech_TechBonus, -1, true);
                }
                else if (p.pilotDef.PilotTags.Contains("pilot_tech") && settings.pilot_tech_vanillaTech)
                {
                    int TechCount = 0;
                    foreach (Pilot techpilot in __instance.PilotRoster)
                    {
                        if (techpilot.pilotDef.PilotTags.Contains("pilot_tech"))
                            TechCount = TechCount + 1;
                    }
                    if (TechCount % settings.pilot_tech_TechsNeeded == 0)
                        __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_tech_TechBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_disgraced"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_disgraced_MoralePenalty, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_comstar"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Subtract, settings.pilot_comstar_TechBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_honest"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_honest_MoraleBonus, -1, true);
                }

                if (p.pilotDef.PilotTags.Contains("pilot_dishonest"))
                {
                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, settings.pilot_dishonest_MoralePenalty, -1, true);
                }
            }
        }

        //[HarmonyPatch(typeof(SimGameState), "_OnDefsLoadComplete")]
        //public static class Initialize_New_Game
        //{
        //    public static void Postfix(SimGameState __instance)
        //    {
        //        foreach (Pilot pilot in __instance.PilotRoster)
        //        {
        //            if (pilot.pilotDef.PilotTags.Contains("pilot_tech"))
        //            {
        //                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
        //            }

        //            if (pilot.pilotDef.PilotTags.Contains("pilot_disgraced"))
        //            {
        //                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
        //            }

        //            if (pilot.pilotDef.PilotTags.Contains("pilot_comstar"))
        //            {
        //                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_comstar_TechBonus, -1, true);
        //            }

        //            if (pilot.pilotDef.PilotTags.Contains("pilot_honest"))
        //            {
        //                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_honest_MoraleBonus, -1, true);
        //            }

        //            if (pilot.pilotDef.PilotTags.Contains("pilot_dishonest"))
        //            {
        //                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_dishonest_MoralePenalty, -1, true);
        //            }
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(SimGameState), "OnDayPassed")]
        public static class DayPasser
        {
            public static void Postfix(SimGameState __instance)
            {
                List<string> potentialTags = new List<string>();
                bool hasTM1 = false;
                bool hasTM3 = false;

                if (settings.ArgoUpgradesAddQuirks && __instance.DayRemainingInQuarter == 30)
                {
                    if (__instance.shipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argoUpgrade_rec_gym"))))
                        potentialTags.Add("pilot_athletic");
                    if (__instance.shipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argoUpgrade_rec_library1"))))
                    {
                        potentialTags.Add("pilot_tech");
                        potentialTags.Add("pilot_merchant");
                    }
                    if (__instance.shipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argoUpgrade_rec_library2"))))
                        potentialTags.Add("pilot_lostech");
                    if (__instance.shipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argoUpgrade_trainingModule1"))))
                        hasTM1 = true;
                    if (__instance.shipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argoUpgrade_trainingModule2"))))
                    {
                        potentialTags.Add("pilot_dependable");
                        potentialTags.Add("pilot_brave");
                    }
                    if (__instance.shipUpgrades.Any(u => u.Tags.Any(t => t.Contains("argoUpgrade_trainingModule3"))))
                        hasTM3 = true;
                }


                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    var rng = new System.Random();
                    int Roll = rng.Next(1, 100);
                    if (pilot.pilotDef.PilotTags.Contains("pilot_unstable"))
                    {
                        if (Roll <= 33)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_morale_high");
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_low");
                        }
                        else if (Roll > 33 && Roll <= 66)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_morale_low");
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_high");
                        }
                        else
                        {
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_low");
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_high");
                        }
                    }

                    //Section for adding quirks due to Argo Upgrades.
                    if (settings.ArgoUpgradesAddQuirks && __instance.DayRemainingInQuarter == 30)
                    {
                        Roll = rng.Next(0, 100);
                        if (!pilot.pilotDef.PilotTags.Contains("PQ_Quirk1_Added") && hasTM1 && potentialTags.Count() != 0 && Roll == 0)
                        {
                            potentialTags.Shuffle();
                            if (!pilot.pilotDef.PilotTags.Contains(potentialTags[0]))
                            {
                                pilot.pilotDef.PilotTags.Add(potentialTags[0]);
                                pilot.pilotDef.PilotTags.Add("PQ_Quirk1_Added");
                                if (potentialTags[0] == "pilot_tech" && !settings.pilot_tech_vanillaTech)
                                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                            }
                        }
                        Roll = rng.Next(0, 100);
                        if (!pilot.pilotDef.PilotTags.Contains("PQ_Quirk2_Added") && hasTM3 && potentialTags.Count() != 0 && Roll == 0)
                        {
                            potentialTags.Shuffle();
                            if (!pilot.pilotDef.PilotTags.Contains(potentialTags[0]))
                            {
                                pilot.pilotDef.PilotTags.Add(potentialTags[0]);
                                pilot.pilotDef.PilotTags.Add("PQ_Quirk2_Added");
                                if (potentialTags[0] == "pilot_tech" && !settings.pilot_tech_vanillaTech)
                                    __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                            }
                        }
                    }

                }




                if (settings.RTCompatible)
                {
                    bool honest = false;
                    foreach (Pilot pilot in __instance.PilotRoster)
                    {
                        if (pilot.pilotDef.PilotTags.Contains("pilot_honest"))
                            honest = true;
                    }

                    foreach (Pilot pilot in __instance.PilotRoster)
                    {
                        if (pilot.pilotDef.PilotTags.Contains("pilot_criminal") && !honest)
                        {
                            var rng = new System.Random();
                            int Roll = rng.Next(1, 101);
                            if (Roll < settings.pilot_criminal_StealPercent)
                            {
                                __instance.AddFunds(settings.pilot_criminal_StealAmount, null, true);
                            }
                        }
                    }
                }

                if (settings.IsSaveGame)
                {
                    foreach (Pilot pilot in __instance.PilotRoster)
                    {
                        if (pilot.pilotDef.PilotTags.Contains("pilot_tech") && !settings.pilot_tech_vanillaTech)
                        {
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                        }
                        else if (settings.pilot_tech_vanillaTech)
                        {
                            int TechCount = 0;
                            foreach (Pilot techpilot in __instance.PilotRoster)
                            {
                                if (pilot.pilotDef.PilotTags.Contains("pilot_tech"))
                                    TechCount = TechCount + 1;
                            }
                            int TechAdd = TechCount / settings.pilot_tech_TechsNeeded;
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, settings.pilot_tech_TechBonus, -1, true);
                        }

                        if (pilot.pilotDef.PilotTags.Contains("pilot_disgraced"))
                        {
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_disgraced_MoralePenalty, -1, true);
                        }


                        if (pilot.pilotDef.PilotTags.Contains("pilot_honest"))
                        {
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_honest_MoraleBonus, -1, true);
                        }

                        if (pilot.pilotDef.PilotTags.Contains("pilot_dishonest"))
                        {
                            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Add, settings.pilot_dishonest_MoralePenalty, -1, true);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SimGameState), "GetReputationShopAdjustment", new Type[] { typeof(FactionValue) })]
        public static class Merchant_Bonus_Faction
        {
            public static void Postfix(SimGameState __instance, ref float __result)
            {
                float MerchantCount = 0;
                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_merchant"))
                    {
                        MerchantCount = MerchantCount + 1;
                    }
                }
                __result = __result - settings.pilot_merchant_ShopDiscount * MerchantCount / 100;
            }
        }

        [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
        public static class AAR_ContractObjectivesWidget_FillInObjectives
        {
            static void Postfix(AAR_ContractObjectivesWidget __instance)
            {
                if (settings.RTCompatible)
                    return;

                settings.CriminalCount = 0;
                foreach (UnitResult unitresult in __instance.theContract.PlayerUnitResults)
                {
                    if (unitresult.pilot.pilotDef.PilotTags.Contains("pilot_criminal") && !__instance.theContract.KilledPilots.Contains(unitresult.pilot))
                        settings.CriminalCount++;
                }

                if (__instance.theContract.Override.employerTeam.FactionValue.IsAuriganPirates && settings.CriminalCount > 0)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    float BonusMoney = (float)__instance.theContract.InitialContractValue;
                    BonusMoney *= __instance.theContract.PercentageContractValue;
                    BonusMoney += (float)sim.GetScaledCBillValue((float)__instance.theContract.InitialContractValue, 0f);
                    BonusMoney *= (float)settings.CriminalCount * settings.pilot_criminal_bonus / 100;
                    string missionObjectiveResultString = $"BONUS FROM CRIMINALS: �{String.Format("{0:n0}", BonusMoney)}";
                    MissionObjectiveResult missionObjectiveResult = new MissionObjectiveResult(missionObjectiveResultString, "7facf07a-626d-4a3b-a1ec-b29a35ff1ac0", false, true, ObjectiveStatus.Succeeded, false);
                    Traverse.Create(__instance).Method("AddObjective", missionObjectiveResult).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "CompleteContract")]
        public static class Contract_CompleteContract_Patch
        {
            static void Postfix(Contract __instance)
            {
                if (settings.RTCompatible)
                    return;

                settings.CriminalCount = 0;
                foreach (UnitResult unitresult in __instance.PlayerUnitResults)
                {
                    if (unitresult.pilot.pilotDef.PilotTags.Contains("pilot_criminal") && !__instance.KilledPilots.Contains(unitresult.pilot))
                        settings.CriminalCount++;
                }
                
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (__instance.Override.employerTeam.FactionValue.IsAuriganPirates && settings.CriminalCount > 0)
                {
                    float BonusMoney = (float)__instance.InitialContractValue;
                    BonusMoney *= __instance.PercentageContractValue;
                    BonusMoney += (float)sim.GetScaledCBillValue((float)__instance.InitialContractValue, 0f);
                    BonusMoney *= (float)settings.CriminalCount * settings.pilot_criminal_bonus / 100;
                    int newMoneyResults = Mathf.FloorToInt(__instance.MoneyResults + BonusMoney);
                    Traverse.Create(__instance).Property("MoneyResults").SetValue(newMoneyResults);
                }
            }
        }

        //[HarmonyPatch(typeof(SimGameState), "GetReputationShopAdjustment", new Type[] { typeof(SimGameReputation) })]
        //public static class Merchant_Bonus_SimGameRep
        //{
        //    public static void Postfix(SimGameState __instance, ref float __result)
        //    {
        //        float MerchantCount = 0;
        //        Helper.Logger.LogLine("SimGameReputation");
        //        foreach (Pilot pilot in __instance.PilotRoster)
        //        {
        //            if (pilot.pilotDef.PilotTags.Contains("pilot_merchant"))
        //            {
        //                MerchantCount = MerchantCount + 1;
        //            }
        //        }
        //        Helper.Logger.LogLine(MerchantCount.ToString());
        //        Helper.Logger.LogLine(__result.ToString());
        //        __result = __result - settings.pilot_merchant_ShopDiscount * MerchantCount / 100;
        //        Helper.Logger.LogLine(__result.ToString());
        //    }
        //}


        //[HarmonyPatch(typeof(Shop), "PopulateInventory")]
        //public static class Criminal_Shops
        //{
        //    public static void Prefix(Shop __instance, int max)
        //    {
        //        SimGameState Sim = Traverse.Create(__instance).Field("Sim").GetValue<SimGameState>();
        //        if (Sim.CurSystem.Tags.Contains("planet_other_blackmarket"))
        //        {
        //            foreach (Pilot pilot in Sim.PilotRoster)
        //            {
        //                if (pilot.pilotDef.PilotTags.Contains("pilot_criminal"))
        //                max = max + 2;
        //            }
        //        }

        //        foreach (Pilot pilot in Sim.PilotRoster)
        //        {
        //            if (pilot.pilotDef.PilotTags.Contains("pilot_comstar"))
        //            {
        //                max = max + 3;
        //            }
        //        }
        //    } 
        //}


        [HarmonyPatch(typeof(Pilot), "InjurePilot")]
        public static class Lucky_Pilot
        {
            public static void Prefix(Pilot __instance, ref int dmg)
            {
                if (__instance.pilotDef.PilotTags.Contains("pilot_lucky"))
                {
                    var rng = new System.Random();
                    int Roll = rng.Next(1, 101);
                    if (Roll <= settings.pilot_lucky_InjuryAvoidance)
                    {
                        dmg = 0;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInData")]
        public static class Adjust_Pilot_XP
        {
            public static void Prefix(AAR_UnitStatusWidget __instance, ref int xpEarned)
            {
                UnitResult unit = Traverse.Create(__instance).Field("UnitData").GetValue<UnitResult>();
                if (unit.pilot.pilotDef.PilotTags.Contains("pilot_naive"))
                {
                    float XPModifier = 1 - settings.pilot_naive_LessExperience;
                    xpEarned = (int)(XPModifier * (float)xpEarned);
                }
            }
        }

        [HarmonyPatch(typeof(AAR_UnitsResult_Screen), "FillInData")]
        public static class AAR_UnitsResult_Screen_FillInData_Patch
        {
            public static void Prefix(AAR_UnitsResult_Screen __instance)
            {
                settings.CriminalCount = 0;
                bool command = false;
                var unitResults = (List<UnitResult>) Traverse.Create(__instance).Field("UnitResults").GetValue();
                var theContract = (Contract)Traverse.Create(__instance).Field("theContract").GetValue();
                var experienceEarned = (int)Traverse.Create(theContract).Property("ExperienceEarned").GetValue();

                for (int i = 0; i < 4; i++)
                {
                    if (unitResults[i] != null)
                    {
                        if (unitResults[i].pilot.pilotDef.PilotTags.Contains("pilot_command")
                          && !theContract.KilledPilots.Contains(unitResults[i].pilot))
                            command = true;
                    }
                }
                if (command)
                {
                    int XP = theContract.ExperienceEarned;
                    experienceEarned += (int)(XP * settings.pilot_command_BonusLanceXP / 100);
                }
            }
        }


        [HarmonyPatch(typeof(Team), "CollectUnitBaseline")]
        public static class Rebellious_Area
        {
            private static void Postfix(Team __instance, ref int __result)
            {
                bool rebelpilot = false;
                bool officer = false;
                bool commander = false;
                int edgecase = 0;
                foreach (AbstractActor actor in __instance.units)
                {
                    Pilot pilot = actor.GetPilot();
                    if (pilot.pilotDef.PilotTags.Contains("pilot_rebellious"))
                    {
                        rebelpilot = true;
                    }
                    if (pilot.pilotDef.PilotTags.Contains("pilot_officer"))
                    {
                        officer = true;
                    }
                    if (pilot.pilotDef.PilotTags.Contains("commander_player"))
                        commander = true;

                    if (rebelpilot && (officer || commander))
                    {
                        edgecase = edgecase + 1;
                    }
                }
                if ((officer || commander) && rebelpilot && edgecase != 1)
                {
                    __result = __result - settings.pilot_rebellious_ResolveMalus;
                }
                if (officer || commander)
                    __result += settings.pilot_officer_BonusResolve;
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
                try
                {
                    Pilot TargetPilot = target.GetPilot();
                    if (TargetPilot.pilotDef.PilotTags.Contains("pilot_reckless"))
                    {
                        __result = __result + (float)settings.pilot_reckless_ToBeHitBonus;
                    }
                    if (TargetPilot.pilotDef.PilotTags.Contains("pilot_cautious"))
                    {
                        __result = __result + (float)settings.pilot_cautious_ToBeHitBonus;
                    }
                    if (TargetPilot.pilotDef.PilotTags.Contains("pilot_jinxed"))
                    {
                        __result = __result + (float)settings.pilot_jinxed_ToBeHitBonus;
                    }
                }
                catch (Exception)
                {
                }
                if (pilot.pilotDef.PilotTags.Contains("pilot_reckless"))
                {
                    __result = __result + (float)settings.pilot_reckless_ToHitBonus;
                }
                if (pilot.pilotDef.PilotTags.Contains("pilot_cautious"))
                {
                    __result = __result + (float)settings.pilot_cautious_ToHitBonus;
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

                if (pilot.pilotDef.PilotTags.Contains("pilot_cautious"))
                {
                    __result = string.Format("{0}CAUTIOUS {1:+#;-#}; ", __result, settings.pilot_cautious_ToHitBonus);
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_drunk") && pilot.pilotDef.TimeoutRemaining > 0)
                {
                    __result = string.Format("{0}DRUNK {1:+#;-#}; ", __result, settings.pilot_drunk_ToHitBonus);
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_lostech") && weapon.componentDef.ComponentTags.Contains("component_type_lostech"))
                {
                    __result = string.Format("{0}LOSTECH TECHNICIAN {1:+#;-#}; ", __result, settings.pilot_lostech_ToHitBonus);
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_jinxed"))
                {
                    __result = string.Format("{0}JINXED {1:+#;-#}; ", __result, settings.pilot_jinxed_ToHitBonus);
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

                if (pilot.pilotDef.PilotTags.Contains("pilot_cautious"))
                {
                    _this.Method("AddToolTipDetail", "CAUTIOUS", settings.pilot_cautious_ToHitBonus).GetValue();
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_drunk") && pilot.pilotDef.TimeoutRemaining > 0)
                {
                    _this.Method("AddToolTipDetail", "DRUNK", settings.pilot_drunk_ToHitBonus).GetValue();
                }

                if (__instance.tag.Contains("component_type_lostech") && pilot.pilotDef.PilotTags.Contains("pilot_lostech"))
                {
                    _this.Method("AddToolTipDetail", "LOSTECH TECH", settings.pilot_lostech_ToHitBonus).GetValue();
                }

                if (pilot.pilotDef.PilotTags.Contains("pilot_jinxed"))
                {
                    _this.Method("AddToolTipDetail", "JINXED", settings.pilot_jinxed_ToHitBonus).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "GetEvasivePipsResult")]
        public static class Drunk_Evasive_Malus
        {
            private static void Postfix(Mech __instance, ref int __result)
            {
                if (__instance.pilot.pilotDef.PilotTags.Contains("pilot_drunk") && __instance.pilot.pilotDef.TimeoutRemaining > 0)
                    __result = __result - settings.pilot_drunk_EP_Loss;

                if (__result < 0)
                    __result = 0;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetMechWarriorValue")]
        public static class Change_Pilot_Cost
        {
            public static void Postfix(SimGameState __instance, PilotDef def, ref int __result)
            {
                float CostPerMonth = __result;

                //Increased costs

                if (def.PilotTags.Contains("pilot_assassin"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_assassin"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_athletic"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_athletic"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_bookish"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_bookish"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_brave"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_brave"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_command"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_command"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_comstar"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_comstar"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_criminal") && !settings.RTCompatible)
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_criminal"] * settings.CostAdjustment * (__result);
                else if (def.PilotTags.Contains("pilot_criminal"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_criminal"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_dependable"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_dependable"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_gladiator"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_gladiator"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_honest"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_honest"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_lostech"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_lostech"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_lucky"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_lucky"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_mechwarrior"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_mechwarrior"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_merchant"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_merchant"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_military"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_military"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_officer"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_officer"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_spacer"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_spacer"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_tech"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_tech"] * settings.CostAdjustment * (__result);

                //Decreased costs

                if (def.PilotTags.Contains("pilot_disgraced"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_disgraced"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_dishonest"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_dishonest"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_drunk"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_drunk"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_jinxed"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_jinxed"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_klutz"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_klutz"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_naive"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_naive"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_rebellious"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_rebellious"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_unstable"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_unstable"] * settings.CostAdjustment * (__result);

                if (CostPerMonth < 0)
                    CostPerMonth = 0;

                __result = (int)CostPerMonth;

                //Global change in value after other quirks applied

                if (def.PilotTags.Contains("pilot_noble"))
                    __result += (int)(__result * (settings.pilot_noble_IncreasedCost - 1));

                if (def.PilotTags.Contains("pilot_wealthy"))
                    __result -= (int)(__result * (1 - settings.pilot_wealthy_CostFactor));
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetMechWarriorHiringCost")]
        public static class Change_Pilot_Hiring_Cost
        {
            public static void Postfix(SimGameState __instance, PilotDef def, ref int __result)
            {
                float CostPerMonth = __result;

                //Increased cost

                if (def.PilotTags.Contains("pilot_assassin"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_assassin"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_athletic"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_athletic"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_bookish"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_bookish"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_brave"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_brave"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_command"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_command"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_comstar"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_comstar"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_criminal") && !settings.RTCompatible)
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_criminal"] * settings.CostAdjustment * (__result);
                else if (def.PilotTags.Contains("pilot_criminal"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_criminal"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_dependable"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_dependable"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_gladiator"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_gladiator"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_honest"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_honest"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_lostech"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_lostech"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_lucky"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_lucky"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_mechwarrior"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_mechwarrior"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_merchant"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_merchant"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_military"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_military"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_officer"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_officer"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_spacer"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_spacer"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_tech"))
                    CostPerMonth = CostPerMonth + settings.QuirkTier["pilot_tech"] * settings.CostAdjustment * (__result);

                //Decreased cost

                if (def.PilotTags.Contains("pilot_disgraced"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_disgraced"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_dishonest"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_dishonest"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_drunk"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_drunk"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_jinxed"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_jinxed"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_klutz"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_klutz"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_naive"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_naive"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_rebellious"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_rebellious"] * settings.CostAdjustment * (__result);

                if (def.PilotTags.Contains("pilot_unstable"))
                    CostPerMonth = CostPerMonth - settings.QuirkTier["pilot_unstable"] * settings.CostAdjustment * (__result);

                if (CostPerMonth < 0)
                    CostPerMonth = 0;

                __result = (int)CostPerMonth;

                //Global change in value after other quirks applied

                if (def.PilotTags.Contains("pilot_noble"))
                    __result += (int)(__result * (settings.pilot_noble_IncreasedCost - 1));

                if (def.PilotTags.Contains("pilot_wealthy"))
                    __result -= (int)(__result * (1 - settings.pilot_wealthy_CostFactor));
            }
        }



        [HarmonyPatch(typeof(Mech), "GetHitLocation", new Type[] { typeof(AbstractActor), typeof(float), typeof(int), typeof(float) })]
        public static class Assassin_Patch
        {
            private static void Prefix(Mech __instance, AbstractActor attacker, ref float bonusMultiplier)
            {
                Pilot pilot = attacker.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                {
                    bonusMultiplier = bonusMultiplier + settings.pilot_assassin_CalledShotBonus;
                }
            }
        }

        // hacked from decompile
        [HarmonyPatch(typeof(HumanDescriptionDef), nameof(HumanDescriptionDef.GetLocalizedDetails), MethodType.Normal)]
        public static class GetLocalizedDetailsPatch
        {
            public static bool Prefix(HumanDescriptionDef __instance, ref Text __result)
            {
                var instance = __instance;
                if (instance.Details == null)
                {
                    __result = new Text();
                    return false;
                }

                var localizedDetails = (Text)Traverse.Create(instance).Field("localizedDetails").GetValue();
                var detailsParsed = (bool)Traverse.Create(instance).Field("detailsParsed").GetValue();

                if (localizedDetails != null && detailsParsed)
                {
                    __result = localizedDetails;
                    return false;
                }
                Text text = new Text();
                if (instance.isGenerated)
                {
                    string[] strArray = instance.Details.Split(new string[4]
                    {
                Environment.NewLine,
                "<b>",
                ":</b>",
                "\n\n"
                    }, StringSplitOptions.RemoveEmptyEntries);
                    // pad the array length to make it even
                    if (strArray.Length % 2 != 0)
                    {
                        Array.Resize(ref strArray, strArray.Length + 1);
                    }
                    int index = 0;
                    while (index < strArray.Length)
                    {
                        text.Append("<b>{0}:</b> {1}\n\n", (object[])new string[2]
                        {
                    strArray[index],
                    strArray[index + 1]
                        });
                        index += 2;
                    }
                }
                else if (instance.isCommander)
                {
                    string details = instance.Details;
                    string[] separator = new string[1]
                        {Environment.NewLine};
                    int num = 1;
                    foreach (object obj in details.Split(separator, (StringSplitOptions)num))
                    {
                        text.Append("{0} \n\n", obj);
                    }
                }
                else
                {
                    text.Append(instance.Details, new object[0]);
                }
                detailsParsed = true;
                localizedDetails = text;
                __result = text;
                return false;
            }
        }

        public static class Helper
        {
            //public static Settings LoadSettings()
            //{
            //    Settings result;
            //    try
            //    {
            //        using (StreamReader streamReader = new StreamReader("Mods/Pilot_Quirks/settings.json"))
            //        {
            //            result = JsonConvert.DeserializeObject<Settings>(streamReader.ReadToEnd());
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.LogError(ex);
            //        result = null;
            //    }
            //    return result;
            //}
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

        //These following four methods all are needed to change Argo upgrade costs.

        [HarmonyPatch(typeof(SimGameState), "CancelArgoUpgrade")]
        public static class SimGameState_CancelArgoUpgrade_Patch
        {
            public static void Prefix(SimGameState __instance, bool refund, ref int __state)
            {
                ShipModuleUpgrade shipModuleUpgrade = __instance.DataManager.ShipUpgradeDefs.Get(__instance.CurrentUpgradeEntry.upgradeID);
                var purchaseCost = shipModuleUpgrade.PurchaseCost;

                float TotalChange = 0;
                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_comstar"))
                        TotalChange += settings.pilot_comstar_ArgoDiscount;
                }
                if (refund)
                {
                    
                    __state = shipModuleUpgrade.PurchaseCost;
                    purchaseCost = (int)((float)purchaseCost * (100 - TotalChange) / 100);
                    shipModuleUpgrade.PurchaseCost = purchaseCost;
                }
            }
            public static void Postfix(SimGameState __instance, bool refund, ref int __state)
            {
                if (refund)
                {
                    ShipModuleUpgrade shipModuleUpgrade = __instance.DataManager.ShipUpgradeDefs.Get(__instance.CurrentUpgradeEntry.upgradeID);
                    shipModuleUpgrade.PurchaseCost = __state;
                }
            }
        }


        [HarmonyPatch(typeof(SGEngineeringScreen), "PurchaseSelectedUpgrade")]
        public static class SGEngineeringScreen_PurchaseSelectedUpgrade_Patch
        {
            public static void Prefix(SGEngineeringScreen __instance, ref int __state)
            {
                float TotalChange = 0;
                var sim = UnityGameInstance.BattleTechGame.Simulation;

                foreach (Pilot pilot in sim.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_comstar"))
                        TotalChange += settings.pilot_comstar_ArgoDiscount;
                }

                var PurchaseCost = __instance.SelectedUpgrade.PurchaseCost;

                __state = PurchaseCost;
                PurchaseCost = (int)((float)PurchaseCost * (100 - TotalChange) / 100);
                __instance.SelectedUpgrade.PurchaseCost = PurchaseCost;
            }
            public static void Postfix(SGEngineeringScreen __instance, ref int __state)
            {
                __instance.SelectedUpgrade.PurchaseCost = __state;
            }
        }

        [HarmonyPatch(typeof(SGShipModuleUpgradeViewPopulator), "Populate")]
        public static class SGShipModuleUpgradeViewPopulator_Populate_Patch
        {
            public static void Prefix(SGShipModuleUpgradeViewPopulator __instance, ShipModuleUpgrade upgrade, ref int __state)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                float TotalChange = 0;
                foreach (Pilot pilot in sim.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_comstar"))
                        TotalChange += settings.pilot_comstar_ArgoDiscount;
                }

                var PurchaseCost = upgrade.PurchaseCost;
                __state = PurchaseCost;

                PurchaseCost = (int)((float)PurchaseCost * (100 - TotalChange) / 100);
                upgrade.PurchaseCost = PurchaseCost;
            }
            public static void Postfix(ShipModuleUpgrade upgrade, ref int __state)
            {
                upgrade.PurchaseCost = __state;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetInjuryCost")]
        public static class SimGameState_GetInjuryCost_Patch
        {
            public static int DamageCostHolder = 0;
            public static bool Prefixed = false;
            public static void Prefix(SimGameState __instance, Pilot p)
            {
                Prefixed = true;
                DamageCostHolder = __instance.Constants.Pilot.BaseInjuryDamageCost;
                if (p.pilotDef.PilotTags.Contains("pilot_spacer"))
                    __instance.Constants.Pilot.BaseInjuryDamageCost = (int)(__instance.Constants.Pilot.BaseInjuryDamageCost * settings.pilot_spacer_InjuryTimeReduction);
            }

            public static void Postfix(SimGameState __instance)
            {
                if (Prefixed)
                    __instance.Constants.Pilot.BaseInjuryDamageCost = DamageCostHolder;
                Prefixed = false;
            }
        }

        //[HarmonyPatch(typeof(ShipModuleUpgrade))]
        //[HarmonyPatch("PurchaseCost", MethodType.Getter)]
        //public static class ShipModuleUpgrade_PurchaseCost_Patch
        //{
        //    public static void Postfix(ref int __result)
        //    {
        //        Helper.Logger.LogLine("Is it even?");
        //        var sim = UnityGameInstance.BattleTechGame.Simulation;
        //        float TotalDiscount = 0;
        //        foreach (Pilot pilot in sim.PilotRoster)
        //        {
        //            if (pilot.pilotDef.PilotTags.Contains("pilot_comstar"))
        //                TotalDiscount += settings.pilot_comstar_ArgoDiscount;
        //        }
        //        Helper.Logger.LogLine("Original Cost: " + __result);
        //        __result -= (int)(TotalDiscount / 100);
        //        Helper.Logger.LogLine("New Cost: " + __result);
        //    }
        //}


        internal class ModSettings
        {
            public float CostAdjustment = 1.0f;
            public Dictionary<string, float> QuirkTier = new Dictionary<string, float>();

            public bool ArgoUpgradesAddQuirks = false;
            public int pilot_tech_TechBonus = 100;
            public bool pilot_tech_vanillaTech = false;
            public int pilot_tech_TechsNeeded = 3;
            public int pilot_reckless_ToHitBonus = -1;
            public int pilot_reckless_ToBeHitBonus = -1;
            public int pilot_cautious_ToHitBonus = 1;
            public int pilot_cautious_ToBeHitBonus = 1;
            public float pilot_assassin_CalledShotBonus = 0.25f;
            public float pilot_merchant_ShopDiscount = 1;
            public int pilot_lucky_InjuryAvoidance = 10;
            public int pilot_disgraced_MoralePenalty = -1;
            public int pilot_jinxed_ToHitBonus = 1;
            public int pilot_jinxed_ToBeHitBonus = -1;

            public int pilot_drunk_ToHitBonus = 1;
            public int pilot_drunk_EP_Loss = 1;
            public int pilot_lostech_ToHitBonus = -1;
            public float pilot_naive_LessExperience = 0.1f;
            public float pilot_noble_IncreasedCost = 0.5f;
            public int pilot_criminal_StealPercent = 0;
            public int pilot_criminal_StealAmount = 0;
            public float pilot_spacer_DecreasedCost = -0.5f;
            public int pilot_comstar_TechBonus = 200;
            public int pilot_comstar_StoreBonus = 3;
            public float pilot_comstar_ArgoDiscount = 2.0f;
            public int pilot_honest_MoraleBonus = 1;
            public int pilot_dishonest_MoralePenalty = -1;
            public int pilot_military_XP = 2000;
            public int pilot_mechwarrior_XP = 4000;
            public float pilot_XP_change = 0.1f;
            public int pilot_officer_BonusResolve = 5;
            public float pilot_command_BonusLanceXP = 5;
            public int CriminalCount = 0;
            public float pilot_criminal_bonus = 2.0f;
            public float pilot_spacer_InjuryTimeReduction = 0.9f;
            public float pilot_wealthy_CostFactor = 0.9f;
            public int pilot_rebellious_ResolveMalus = 1;


            public Dictionary<string, string> TagIDToDescription = new Dictionary<string, string>();
            public Dictionary<string, string> TagIDToNames = new Dictionary<string, string>();

            public bool IsSaveGame = false;
            public bool RTCompatible = false;
            public bool MechBonding = true;
            public bool MechBondingTesting = false;

            //Tiers for Mech Mastery.
            public int Tier1 = 5;
            public int Tier2 = 10;
            public int Tier3 = 15;
            public int Tier4 = 20;

            public int pilot_drops_for_8_pilot = 50;



        }
    }
}