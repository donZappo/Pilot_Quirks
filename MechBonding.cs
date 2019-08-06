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

namespace Pilot_Quirks
{
    class MechBonding
    {
        public static Dictionary<string, Dictionary<string, int>> PilotsAndMechs = new Dictionary<string, Dictionary<string, int>>();


        //Track how many times the pilots drop with each 'Mech. 
        [HarmonyPatch(typeof(TurnEventNotification), "ShowTeamNotification")]
        public static class TurnEventNotification_Patch
        {
            public static void Prefix(TurnEventNotification __instance, Team team, bool ___hasBegunGame,
                CombatGameState ___Combat)
            {
                if (!___hasBegunGame && ___Combat.TurnDirector.CurrentRound <= 1)
                {
                    foreach (AbstractActor actor in team.units)
                    {
                        Pilot pilot = actor.GetPilot();
                        Logger.LogLine(pilot.Name);

                        //Add to the counter for 'Mech piloting.
                        if (!PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                        {
                            Logger.LogLine(pilot.Description.Id.ToString());
                            Logger.LogLine(actor.Description.Name);
                            Dictionary<string, int> tempD = new Dictionary<string, int>();
                            tempD.Add(actor.Description.Name, 1);
                            PilotsAndMechs.Add(pilot.Description.Id, tempD);
                        }
                        else if (!PilotsAndMechs[pilot.Description.Id].Keys.Contains(actor.Description.Name))
                            PilotsAndMechs[pilot.Description.Id].Add(actor.Description.Name, 1);
                        else
                            PilotsAndMechs[pilot.Description.Id][actor.Description.Name] += 1;


                        //Add tags based upon 'Mech experience for the pilot.
                        if (PilotsAndMechs.Keys.Contains(pilot.Description.Id))
                        {
                            var MechExperience = PilotsAndMechs[pilot.Description.Id];
                            var BondedMech = MechExperience.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                            if (BondedMech == actor.Description.Name)
                            {
                                if (MechExperience[BondedMech] >= 5)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_green");
                                if (MechExperience[BondedMech] >= 10)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_regular");
                                if (MechExperience[BondedMech] >= 15)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_veteran");
                                if (MechExperience[BondedMech] >= 20)
                                    pilot.pilotDef.PilotTags.Add("PQ_pilot_elite");
                            }
                        }
                    }
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
                if (unitResult.pilot.pilotDef.PilotTags.Contains("PQ_pilot_green") && !unitResult.pilot.pilotDef.PilotTags.Contains("PQ_Mech_Mastery"))
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
    }
}
