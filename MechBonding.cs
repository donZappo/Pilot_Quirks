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

                        if (!PilotsAndMechs.Keys.Contains(pilot.GUID))
                        {
                            Dictionary<string, int> tempD = new Dictionary<string, int>();
                            tempD.Add(actor.Description.Name, 1);
                            PilotsAndMechs.Add(pilot.GUID, tempD);
                        }
                        else if (!PilotsAndMechs[pilot.GUID].Keys.Contains(actor.Description.Name))
                            PilotsAndMechs[pilot.GUID].Add(actor.Description.Name, 1);
                        else
                            PilotsAndMechs[pilot.GUID][actor.Description.Name] += 1;
                    }
                }
            }
        }





    }
}
