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

        //Add bonded mechs to generated pilots. 
        [HarmonyPatch(typeof(StarSystem), "GeneratePilots")]
        public static class StarSystem_Patch
        {
            public static void Postfix(StarSystem __instance)
            {
                try
                {
                    foreach (PilotDef pilot in __instance.AvailablePilots)
                    {
                        // 8-8-8-8 pilot = 56,000xp
                        int drops = ((pilot.ExperienceSpent + pilot.ExperienceUnspent) * Pre_Control.settings.pilot_drops_for_8_pilot / 56000);

                        if (!pilot.PilotTags.Contains("PQ_Mech_Mastery"))
                            pilot.PilotTags.Add("PQ_Mech_Mastery");
                        
                        // Add training in a Chameleon if Inner sphere
                        if (pilot.PilotTags.Contains("pilot_davion") ||
                            pilot.PilotTags.Contains("pilot_liao") ||
                            pilot.PilotTags.Contains("pilot_kurita") ||
                            pilot.PilotTags.Contains("pilot_steiner") ||
                            pilot.PilotTags.Contains("pilot_marik"))
                        {
                            int trainingDrops = UnityEngine.Random.Range(0, 4);
                            if (trainingDrops > drops)
                            {
                                trainingDrops = drops;
                            }
                            if (trainingDrops > 0)
                            {
                                if (!PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                                {
                                    Dictionary<string, int> tempD = new Dictionary<string, int>();
                                    tempD.Add("Chameleon", trainingDrops);
                                    PilotsAndMechs.Add(pilot.Description.Id, tempD);
                                    drops -= trainingDrops;
                                }
                            }
                        }
                        
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
                                mechName = dm.MechDefs.Get(list[0].UnitDefID).Description.Name;
                                if (!PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                                {
                                    Dictionary<string, int> tempD = new Dictionary<string, int>();
                                    tempD.Add(mechName, thisDrops3);
                                    PilotsAndMechs.Add(pilot.Description.Id, tempD);
                                }
                                else if (PilotsAndMechs[pilot.Description.Id].ContainsKey(mechName))
                                {
                                    PilotsAndMechs[pilot.Description.Id][mechName] += thisDrops3;
                                }
                                else
                                {
                                    PilotsAndMechs[pilot.Description.Id].Add(mechName, thisDrops3);
                                }
                            }
                            else
                            {
                                if (!PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                                {
                                    Dictionary<string, int> tempD = new Dictionary<string, int>();
                                    tempD.Add("Griffin", thisDrops3);
                                    PilotsAndMechs.Add(pilot.Description.Id, tempD);
                                }
                                else if (PilotsAndMechs[pilot.Description.Id].ContainsKey("Griffin"))
                                {
                                    PilotsAndMechs[pilot.Description.Id]["Griffin"] += thisDrops3;
                                }
                                else
                                {
                                    PilotsAndMechs[pilot.Description.Id].Add("Griffin", thisDrops3);
                                }
                            }
                            
                            excludes.Add($"unit_rare_{team}");
                            list = MetadataDatabase.Instance.GetMatchingUnitDefs(unitTagSet, unitExcludedTagSet, true, __instance.Sim.CurrentDate, __instance.Sim.CompanyTags);

                            if (list.Count > 0)
                            {
                                list.Shuffle<UnitDef_MDD>();
                                mechName = dm.MechDefs.Get(list[0].UnitDefID).Description.Name;
                                if (PilotsAndMechs[pilot.Description.Id].ContainsKey(mechName))
                                {
                                    PilotsAndMechs[pilot.Description.Id][mechName] += thisDrops2;
                                }
                                else
                                {
                                    PilotsAndMechs[pilot.Description.Id].Add(mechName, thisDrops2);
                                }
                            }
                            else
                            {
                                if (PilotsAndMechs[pilot.Description.Id].ContainsKey("Griffin"))
                                {
                                    PilotsAndMechs[pilot.Description.Id]["Griffin"] += thisDrops2;
                                }
                                else
                                {
                                    PilotsAndMechs[pilot.Description.Id].Add("Griffin", thisDrops2);
                                }
                            }

                            excludes.Add($"unit_uncommon_{team}");
                            list = MetadataDatabase.Instance.GetMatchingUnitDefs(unitTagSet, unitExcludedTagSet, true, __instance.Sim.CurrentDate, __instance.Sim.CompanyTags);

                            if (list.Count > 0)
                            {
                                list.Shuffle<UnitDef_MDD>();
                                mechName = dm.MechDefs.Get(list[0].UnitDefID).Description.Name;
                                if (PilotsAndMechs[pilot.Description.Id].ContainsKey(mechName))
                                {
                                    PilotsAndMechs[pilot.Description.Id][mechName] += thisDrops1;
                                }
                                else
                                {
                                    PilotsAndMechs[pilot.Description.Id].Add(mechName, thisDrops1);
                                }
                            }
                            else
                            {
                                if (PilotsAndMechs[pilot.Description.Id].ContainsKey("Griffin"))
                                {
                                    PilotsAndMechs[pilot.Description.Id]["Griffin"] += thisDrops1;
                                }
                                else
                                {
                                    PilotsAndMechs[pilot.Description.Id].Add("Griffin", thisDrops1);
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
                foreach (string pilot in PilotsAndMechs.Keys)
                {
                    bool TrimPilot = true;
                    foreach (PilotDef pilotdef in __instance.AvailablePilots)
                    {
                        if (pilotdef.Description.Id == pilot)
                            TrimPilot = false;
                    }
                    foreach (Pilot hiredpilot in sim.PilotRoster)
                    {
                        if (hiredpilot.pilotDef.Description.Id == pilot)
                            TrimPilot = false;
                    }
                    foreach (Pilot deadpilot in sim.Graveyard)
                    {
                        if (deadpilot.pilotDef.Description.Id == pilot)
                            TrimPilot = false;
                    }
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
                UnitResult unitResult = Traverse.Create(__instance).Field("UnitData").GetValue<UnitResult>();
                if (!unitResult.pilot.pilotDef.PilotTags.Contains("PQ_Mech_Mastery"))
                    unitResult.pilot.pilotDef.PilotTags.Add("PQ_Mech_Mastery");
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_regular"))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_pilot_regular");
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_pilot_veteran");
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_elite"))
                    unitResult.pilot.pilotDef.PilotTags.Remove("PQ_pilot_elite");
            }
        }

        //Adjust skills based upon mastery. 
        [HarmonyPatch(typeof(Pilot))]
        [HarmonyPatch("Piloting", MethodType.Getter)]
        public class Pilot_Piloting_Patch
        {
            public static void Postfix(Pilot __instance, ref int __result)
            {
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
                if (__instance.pilotDef.PilotTags.Contains("PQ_pilot_elite"))
                    __result += 1;
            }
        }

        [HarmonyPatch(typeof(LineOfSight), "GetAllSensorRangeAbsolutes")]
        public static class LineOfSight_GetAllSensorRangeAbsolutes_Patch
        {
            public static void Postfix(AbstractActor source, ref float __result)
            {
                Pilot pilot = source.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
                    __result += pilot.Tactics * 5;
            }
        }

        [HarmonyPatch(typeof(LineOfSight), "GetAllSpotterAbsolutes")]
        public static class LineOfSight_GetAllSpotterAbsolutes_Patch
        {
            public static void Postfix(AbstractActor source, ref float __result)
            {
                Pilot pilot = source.GetPilot();
                if (pilot.pilotDef.PilotTags.Contains("PQ_pilot_veteran"))
                    __result += pilot.Tactics * 5;
            }
        }

        //Track how many times the pilots drop with each 'Mech. 
        [HarmonyPatch(typeof(CombatHUDMWStatus), "InitForPilot")]
        public static class CombatHUDMWStatus_InitForPilot_Patches
        {
            public static void Prefix(AbstractActor actor, Pilot pilot)
            {
                if (actor is Mech ourMech)
                {
                    //Add to the counter for 'Mech piloting.
                    if (!PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                    {
                        Dictionary<string, int> tempD = new Dictionary<string, int>();
                        tempD.Add(ourMech.MechDef.Description.Name, 1);
                        PilotsAndMechs.Add(pilot.Description.Id, tempD);
                    }
                    else if (!PilotsAndMechs[pilot.Description.Id].Keys.Contains(ourMech.MechDef.Description.Name))
                        PilotsAndMechs[pilot.Description.Id].Add(ourMech.MechDef.Description.Name, 1);
                    else
                        PilotsAndMechs[pilot.Description.Id][ourMech.MechDef.Description.Name] += 1;


                    //Add tags based upon 'Mech experience for the pilot.
                    List<string> TopThreeMechs = new List<string>();
                    if (PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                    {
                        var MechExperience = PilotsAndMechs[pilot.Description.Id];
                        int i = 0;
                        foreach (var mech in MechExperience.OrderByDescending(x => x.Value))
                        {
                            TopThreeMechs.Add(mech.Key);
                            i++;
                            if (i == 2)
                                break;
                        }

                        if (TopThreeMechs.Contains(ourMech.MechDef.Description.Name))
                        {
                            if (MechExperience[ourMech.MechDef.Description.Name] >= Pre_Control.settings.Tier1)
                                pilot.pilotDef.PilotTags.Add("PQ_pilot_green");
                            if (MechExperience[ourMech.MechDef.Description.Name] >= Pre_Control.settings.Tier2)
                                pilot.pilotDef.PilotTags.Add("PQ_pilot_regular");
                            if (MechExperience[ourMech.MechDef.Description.Name] >= Pre_Control.settings.Tier3)
                                pilot.pilotDef.PilotTags.Add("PQ_pilot_veteran");
                            if (MechExperience[ourMech.MechDef.Description.Name] >= Pre_Control.settings.Tier4)
                                pilot.pilotDef.PilotTags.Add("PQ_pilot_elite");
                        }
                    }
                }
            }
        }
    }
}
