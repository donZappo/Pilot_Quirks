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
                        
                        // Add training in a Chameleon if Inner sphere
                        //if (pilot.PilotTags.Contains("pilot_davion") ||
                        //    pilot.PilotTags.Contains("pilot_liao") ||
                        //    pilot.PilotTags.Contains("pilot_kurita") ||
                        //    pilot.PilotTags.Contains("pilot_steiner") ||
                        //    pilot.PilotTags.Contains("pilot_marik"))
                        //{
                        //    int trainingDrops = UnityEngine.Random.Range(0, 4);
                        //    if (trainingDrops > drops)
                        //    {
                        //        trainingDrops = drops;
                        //    }
                        //    if (trainingDrops > 0)
                        //    {
                        //        if (!PilotsAndMechs.Keys.Contains(PilotTattoo))
                        //        {
                        //            Dictionary<string, int> tempD = new Dictionary<string, int>();
                        //            tempD.Add("Light Mech", trainingDrops);
                        //            PilotsAndMechs.Add(PilotTattoo, tempD);
                        //            drops -= trainingDrops;
                        //        }
                        //    }
                        //}
                        
                        if (drops > 0)
                        {
                            int thisDrops1 = UnityEngine.Random.Range(1, drops);
                            int thisDrops2 = 0;
                            int thisDrops3 = 0;

                            drops -= thisDrops1;
                            if (drops > 0)
                            {
                                thisDrops2 = UnityEngine.Random.Range(1, drops);
                                drops -= thisDrops2;
                            }
                            if (drops > 0)
                            {
                                thisDrops3 = drops;
                            }
                            
                            List<string> includes = new List<string>();
                            List<string> excludes = new List<string>();

                            string team = "";

                            if (pilot.PilotTags.Contains("pilot_davion"))
                            {
                                team = "Davion";
                            }
                            else if (pilot.PilotTags.Contains("pilot_kurita"))
                            {
                                team = "Kurita";
                            }
                            else if (pilot.PilotTags.Contains("pilot_liao"))
                            {
                                team = "Liao";
                            }
                            else if (pilot.PilotTags.Contains("pilot_marik"))
                            {
                                team = "Marik";
                            }
                            else if (pilot.PilotTags.Contains("pilot_steiner"))
                            {
                                team = "Steiner";
                            }
                            else
                            {
                                team = "AuriganMercenaries";
                            }
                            
                            includes.Add("unit_mech");

                            excludes.Add($"unit_very_rare_{team}");
                            excludes.Add($"unit_ext_rare_{team}");
                            excludes.Add($"unit_none_{team}");
                            excludes.Add($"unit_hero_{team}");
                            excludes.Add("unit_LosTech");
                            excludes.Add("unit_era_2900");
                            excludes.Add("unit_clan");
                            if (drops < 20)
                            {
                                excludes.Add("unit_assault");
                            }
                            if (drops < 10)
                            {
                                excludes.Add("unit_heavy");
                            }
                            
                            TagSet unitTagSet = new TagSet(includes);
                            TagSet unitExcludedTagSet = new TagSet(excludes);

                            DataManager dm = __instance.Sim.DataManager;
                            List<UnitDef_MDD> list = MetadataDatabase.Instance.GetMatchingUnitDefs(unitTagSet, unitExcludedTagSet, true, __instance.Sim.CurrentDate, __instance.Sim.CompanyTags);
                            
                            string mechName = "";
                            if (list.Count > 0)
                            {
                                list.Shuffle<UnitDef_MDD>();
                                mechName = dm.MechDefs.Get(list[0].UnitDefID).Chassis.Description.UIName;
                                if (!PilotsAndMechs.Keys.Contains(PilotTattoo))
                                {
                                    Dictionary<string, int> tempD = new Dictionary<string, int>();
                                    tempD.Add(mechName, thisDrops3);
                                    PilotsAndMechs.Add(PilotTattoo, tempD);
                                }
                                else if (PilotsAndMechs[PilotTattoo].ContainsKey(mechName))
                                {
                                    PilotsAndMechs[PilotTattoo][mechName] += thisDrops3;
                                }
                                else
                                {
                                    PilotsAndMechs[PilotTattoo].Add(mechName, thisDrops3);
                                }
                            }
                            else
                            {
                                if (!PilotsAndMechs.Keys.Contains(PilotTattoo))
                                {
                                    Dictionary<string, int> tempD = new Dictionary<string, int>();
                                    tempD.Add("Griffin", thisDrops3);
                                    PilotsAndMechs.Add(PilotTattoo, tempD);
                                }
                                else if (PilotsAndMechs[PilotTattoo].ContainsKey("Griffin"))
                                {
                                    PilotsAndMechs[PilotTattoo]["Griffin"] += thisDrops3;
                                }
                                else
                                {
                                    PilotsAndMechs[PilotTattoo].Add("Griffin", thisDrops3);
                                }
                            }
                            
                            excludes.Add($"unit_rare_{team}");
                            list = MetadataDatabase.Instance.GetMatchingUnitDefs(unitTagSet, unitExcludedTagSet, true, __instance.Sim.CurrentDate, __instance.Sim.CompanyTags);

                            if (list.Count > 0)
                            {
                                list.Shuffle<UnitDef_MDD>();
                                mechName = dm.MechDefs.Get(list[0].UnitDefID).Chassis.Description.UIName;
                                if (PilotsAndMechs[PilotTattoo].ContainsKey(mechName))
                                {
                                    PilotsAndMechs[PilotTattoo][mechName] += thisDrops2;
                                }
                                else
                                {
                                    PilotsAndMechs[PilotTattoo].Add(mechName, thisDrops2);
                                }
                            }
                            else
                            {
                                if (PilotsAndMechs[PilotTattoo].ContainsKey("Griffin"))
                                {
                                    PilotsAndMechs[PilotTattoo]["Griffin"] += thisDrops2;
                                }
                                else
                                {
                                    PilotsAndMechs[PilotTattoo].Add("Griffin", thisDrops2);
                                }
                            }

                            excludes.Add($"unit_uncommon_{team}");
                            list = MetadataDatabase.Instance.GetMatchingUnitDefs(unitTagSet, unitExcludedTagSet, true, __instance.Sim.CurrentDate, __instance.Sim.CompanyTags);

                            if (list.Count > 0)
                            {
                                list.Shuffle<UnitDef_MDD>();
                                mechName = dm.MechDefs.Get(list[0].UnitDefID).Chassis.Description.UIName;
                                if (PilotsAndMechs[PilotTattoo].ContainsKey(mechName))
                                {
                                    PilotsAndMechs[PilotTattoo][mechName] += thisDrops1;
                                }
                                else
                                {
                                    PilotsAndMechs[PilotTattoo].Add(mechName, thisDrops1);
                                }
                            }
                            else
                            {
                                if (PilotsAndMechs[PilotTattoo].ContainsKey("Griffin"))
                                {
                                    PilotsAndMechs[PilotTattoo]["Griffin"] += thisDrops1;
                                }
                                else
                                {
                                    PilotsAndMechs[PilotTattoo].Add("Griffin", thisDrops1);
                                }
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
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_elite"))
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
                        else if (!PilotsAndMechs[PilotTattoo].Keys.Contains(ourMech.MechDef.Chassis.weightClass.ToString()))
                            PilotsAndMechs[PilotTattoo].Add(ourMech.MechDef.Chassis.weightClass.ToString(), 1);
                        else
                            PilotsAndMechs[PilotTattoo][ourMech.MechDef.Chassis.weightClass.ToString()] += 1;


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
                                if (MechExperience[ourMech.MechDef.Chassis.Description.UIName] >= Pre_Control.settings.Tier1)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_green");
                                if (MechExperience[ourMech.MechDef.Chassis.Description.UIName] >= Pre_Control.settings.Tier2)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_regular");
                                if (MechExperience[ourMech.MechDef.Chassis.Description.UIName] >= Pre_Control.settings.Tier3)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_veteran");
                                if (MechExperience[ourMech.MechDef.Chassis.Description.UIName] >= Pre_Control.settings.Tier4)
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
    }
}
