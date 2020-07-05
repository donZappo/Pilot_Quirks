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
using Error = BestHTTP.SocketIO.Error;
using Logger = Pilot_Quirks.Pre_Control.Helper.Logger;
using HBS.Collections;
using BattleTech.Data;

namespace Pilot_Quirks
{
    class MechBonding
    {
        public static Dictionary<string, Dictionary<string, int>> PilotsAndMechs = new Dictionary<string, Dictionary<string, int>>();
        public static int PQ_GUID = 0;

        //Add bonded mechs to generated pilots. 
        [HarmonyPatch(typeof(StarSystem), "GeneratePilots")]
        public static class StarSystem_Patch
        {
            public static void Postfix(StarSystem __instance)
            {
                if (!Pre_Control.settings.MechBonding)
                    return;

                try
                {
                    var Sim = UnityGameInstance.BattleTechGame.Simulation;
                    if (Sim.IsCampaign && !Sim.CompanyTags.Contains("map_travel_1"))
                        return;

                    foreach (PilotDef pilot in __instance.AvailablePilots)
                    {
                        // 8-8-8-8 pilot = 56,000xp
                        int drops = ((pilot.ExperienceSpent + pilot.ExperienceUnspent) * Pre_Control.settings.pilot_drops_for_8_pilot / 56000);

                        pilot.PilotTags.Add("PQ_Mech_Mastery");
                        string PilotTattoo = "PQ_Pilot_GUID_" + PQ_GUID;
                        pilot.PilotTags.Add(PilotTattoo);
                        PQ_GUID++;
                        
                        if (drops > 0)
                        {
                            int thisDrops1 = UnityEngine.Random.Range(0, drops);

                            var rand = new System.Random();
                            if (!PilotsAndMechs.Keys.Contains(PilotTattoo))
                            {
                                Dictionary<string, int> tempD = new Dictionary<string, int>();
                                tempD.Add("LIGHT", 0);
                                tempD.Add("MEDIUM", 0);
                                tempD.Add("HEAVY", 0);
                                tempD.Add("ASSAULT", 0);
                                PilotsAndMechs.Add(PilotTattoo, tempD);
                            }
                            for (int i = 0; i < thisDrops1; i++)
                            {
                                var chance = rand.NextDouble();
                                if (chance < 0.5)
                                    PilotsAndMechs[PilotTattoo]["LIGHT"]++;
                                else if (chance < 0.75)
                                    PilotsAndMechs[PilotTattoo]["MEDIUM"]++;
                                else if (chance < 0.90)
                                    PilotsAndMechs[PilotTattoo]["HEAVY"]++;
                                else
                                    PilotsAndMechs[PilotTattoo]["ASSAULT"]++;
                            }
                            if (Pre_Control.settings.MechBondingTesting)
                            {
                                PilotsAndMechs[PilotTattoo]["LIGHT"] = 100;
                                PilotsAndMechs[PilotTattoo]["MEDIUM"] = 100;
                                PilotsAndMechs[PilotTattoo]["HEAVY"] = 100;
                                PilotsAndMechs[PilotTattoo]["ASSAULT"] = 100;
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }

                //Trim out pilots that are no longer needed from the master list. 
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                Dictionary<string, Dictionary<string, int>> PAM_Clone = new Dictionary<string, Dictionary<string, int>>(PilotsAndMechs);
                foreach (string pilot in PAM_Clone.Keys)
                {
                    bool TrimPilot = true;
                    foreach (PilotDef pilotdef in __instance.AvailablePilots)
                    {
                        string PilotTattoo = pilotdef.PilotTags.FirstOrDefault(x => x.StartsWith("PQ_Pilot_GUID"));
                        if (PilotTattoo == pilot)
                            TrimPilot = false;
                    }
                    foreach (Pilot hiredpilot in sim.PilotRoster)
                    {
                        string PilotTattoo = hiredpilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith("PQ_Pilot_GUID"));
                        if (PilotTattoo == pilot)
                            TrimPilot = false;
                    }
                    foreach (Pilot deadpilot in sim.Graveyard)
                    {
                        string PilotTattoo = deadpilot.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith("PQ_Pilot_GUID"));
                        if (PilotTattoo == pilot)
                            TrimPilot = false;
                    }
                    string CommanderTattoo = sim.Commander.pilotDef.PilotTags.FirstOrDefault(x => x.StartsWith("PQ_Pilot_GUID"));
                    if (CommanderTattoo == pilot)
                        TrimPilot = false;

                    if (TrimPilot)
                        PilotsAndMechs.Remove(pilot);
                }
            }
        }



        //Clear out them tags and add the Mech Mastery Tag. PQ_pilot_green is processed in Pilot Fatigue and cleared there. 
        [HarmonyPatch(typeof(AAR_UnitStatusWidget), "FillInPilotData")]
        public static class AAR_UnitStatusWidget_FillInPilotData_Prefix
        {
            public static void Prefix(AAR_UnitStatusWidget __instance, SimGameState ___simState)
            {
                if (!Pre_Control.settings.MechBonding)
                    return;

                UnitResult unitResult = Traverse.Create(__instance).Field("UnitData").GetValue<UnitResult>();
                if (!unitResult.pilot.pilotDef.PilotTags.Contains("PQ_Mech_Mastery"))
                    unitResult.pilot.pilotDef.PilotTags.Add("PQ_Mech_Mastery");

                bool HasTattoo = unitResult.pilot.pilotDef.PilotTags.Any(x => x.StartsWith("PQ_Pilot_GUID"));
                if (!HasTattoo)
                {
                    string PilotTattoo = "PQ_Pilot_GUID_" + PQ_GUID;
                    unitResult.pilot.pilotDef.PilotTags.Add(PilotTattoo);
                    PQ_GUID++;
                }

                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_regular"))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_pilot_regular");
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_pilot_veteran");
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_elite") && !(unitResult.mech.Chassis.weightClass == WeightClass.ASSAULT
                    && unitResult.pilot.IsIncapacitated))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_pilot_elite");
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_Pilot_MissionTattoo"))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_Pilot_MissionTattoo");

            }
        }

        //Adjust skills based upon mastery. 
        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("Piloting", MethodType.Getter)]
        public class Pilot_Piloting_Patch
        {
            public static void Postfix(Pilot __instance, ref int __result)
            {
                if (!Pre_Control.settings.MechBonding)
                    return;

                if (__instance.pilotDef.PilotTags.Contains("PQ_pilot_regular"))
                    __result += 1;
            }
        }

        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("Gunnery", MethodType.Getter)]
        public class Pilot_Gunnery_Patch
        {
            public static void Postfix(Pilot __instance, ref int __result)
            {
                if (!Pre_Control.settings.MechBonding)
                    return;

                if (__instance.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
                    __result += 1;
            }
        }

        //[HarmonyPatch(typeof(LineOfSight), "GetAllSensorRangeAbsolutes")]
        //public static class LineOfSight_GetAllSensorRangeAbsolutes_Patch
        //{
        //    public static void Postfix(AbstractActor source, ref float __result)
        //    {
        //        if (!Pre_Control.settings.MechBonding)
        //            return;

        //        Pilot pilot = source.GetPilot();
        //        if (pilot.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
        //            __result += pilot.Tactics * 5;
        //    }
        //}

        //[HarmonyPatch(typeof(LineOfSight), "GetAllSpotterAbsolutes")]
        //public static class LineOfSight_GetAllSpotterAbsolutes_Patch
        //{
        //    public static void Postfix(AbstractActor source, ref float __result)
        //    {
        //        if (!Pre_Control.settings.MechBonding)
        //            return;

        //        Pilot pilot = source.GetPilot();
        //        if (pilot.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
        //            __result += pilot.Tactics * 5;
        //    }
        //}

        //Track how many times the pilots drop with each 'Mech. 
        [HarmonyPatch(typeof(CombatHUDMWStatus), "InitForPilot")]
        public static class CombatHUDMWStatus_InitForPilot_Patches
        {
            public static void Prefix(AbstractActor actor, Pilot pilot)
            {
                if (!Pre_Control.settings.MechBonding)
                    return;

                //I've added a silent catch to deal with NPC pilots. 
                try
                {
                    if (actor is Mech ourMech)
                    {
                        bool HasTattoo = pilot.pilotDef.PilotTags.Any(x => x.StartsWith("PQ_Pilot_GUID"));
                        string TempTattoo = "PQ_Pilot_MissionTattoo";
                        if (pilot.pilotDef.PilotTags.Contains(TempTattoo))
                            return;

                        if (!HasTattoo)
                        {
                            pilot.pilotDef.PilotTags.Add("PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                            MechBonding.PQ_GUID++;
                        }

                        string PilotTattoo = pilot.pilotDef.PilotTags.First(x => x.StartsWith("PQ_Pilot_GUID"));
                        //Add to the counter for 'Mech piloting.
                        if (!PilotsAndMechs.Keys.Contains(PilotTattoo))
                        {
                            Dictionary<string, int> tempD = new Dictionary<string, int>();
                            tempD.Add(ourMech.MechDef.Chassis.weightClass.ToString(), 1);
                            PilotsAndMechs.Add(PilotTattoo, tempD);
                        }
                        else 
                        {
                            if (!PilotsAndMechs[PilotTattoo].Keys.Contains(ourMech.MechDef.Chassis.weightClass.ToString()))
                                PilotsAndMechs[PilotTattoo].Add(ourMech.MechDef.Chassis.weightClass.ToString(), 1);
                            else if (PilotsAndMechs[PilotTattoo][ourMech.MechDef.Chassis.weightClass.ToString()] < Pre_Control.settings.Tier4)
                                PilotsAndMechs[PilotTattoo][ourMech.MechDef.Chassis.weightClass.ToString()] += 1;
                            var tempDict = new Dictionary<string, Dictionary<string, int>>(PilotsAndMechs);
                            foreach (var mech in tempDict[PilotTattoo].Keys)
                            {
                                if (mech != ourMech.MechDef.Chassis.weightClass.ToString())
                                {
                                    PilotsAndMechs[PilotTattoo][mech]--;
                                    if (PilotsAndMechs[PilotTattoo][mech] == 0)
                                        PilotsAndMechs[PilotTattoo].Remove(mech);

                                }
                            }
                        }


                        //Add tags based upon 'Mech experience for the pilot.
                        List<string> TopThreeMechs = new List<string>();
                        if (PilotsAndMechs.Keys.Contains(PilotTattoo))
                        {
                            var MechExperience = PilotsAndMechs[PilotTattoo];
                            int i = 0;
                            foreach (var mech in MechExperience.OrderByDescending(x => x.Value))
                            {
                                TopThreeMechs.Add(mech.Key);
                                i++;
                                if (i == 3)
                                    break;
                            }

                            if (TopThreeMechs.Contains(ourMech.MechDef.Chassis.weightClass.ToString()))
                            {
                                if (MechExperience[ourMech.MechDef.Chassis.weightClass.ToString()] >= Pre_Control.settings.Tier1)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_green");
                                if (MechExperience[ourMech.MechDef.Chassis.weightClass.ToString()] >= Pre_Control.settings.Tier2)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_regular");
                                if (MechExperience[ourMech.MechDef.Chassis.weightClass.ToString()] >= Pre_Control.settings.Tier3)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_veteran");
                                if (MechExperience[ourMech.MechDef.Chassis.weightClass.ToString()] >= Pre_Control.settings.Tier4)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_elite");
                            }
                        }
                        pilot.pilotDef.PilotTags.Add(TempTattoo);
                    }
                }
                catch (Exception e)
                {
                    Pre_Control.Helper.Logger.LogError(e);
                }
            }
        }

        //Heavy mech mastery allows for non-hindered movement.
        [HarmonyPatch(typeof(PathNodeGrid), "GetTerrainCost", typeof(MapTerrainDataCell), typeof(AbstractActor), typeof(MoveType))]
        public static class PathNodeGrid_GetTerrainCost_Patches
        {
            public static void Postfix(PathNodeGrid __instance, MapTerrainDataCell cell, AbstractActor unit, MoveType moveType, ref float __result)
            {
                if (unit.UnitType == UnitType.Mech)
                {
                    var mech = unit as Mech;
                    if (mech.weightClass == WeightClass.HEAVY && mech.pilot.pilotDef.PilotTags.Contains("PQ_pilot_elite"))
                    {
                        var gtcScratchMask = unit.Combat.MapMetaData.GetPriorityDesignMask(cell);
                        if (__result <= 2.5 && __result >= 1)
                            __result = 1;
                    }
                }
            }
        }
    }
}
