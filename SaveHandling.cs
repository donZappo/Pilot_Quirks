using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.Save.Test;
using Harmony;
using Newtonsoft.Json;
using UnityEngine;
using BattleTech.Save;
using System;
using System.Collections.Generic;

namespace Pilot_Quirks
{
    public static class SaveHandling
    {
        //This patch is useful for setting up the Sim after the career or campaign is initially started. 
        [HarmonyPatch(typeof(SimGameState), "_OnAttachUXComplete")]
        public static class SimGameState__OnAttachUXComplete_Patch
        {
            public static void Postfix()
            {
                //var sim = UnityGameInstance.BattleTechGame.Simulation;
                //if ()
                //{
                //}
                //else
                //{
                //    LogDebug("_OnAttachUXComplete Postfix");
                //}
            }
        }

        //On game load.
        [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
        public static class SimGameState_Rehydrate_Patch
        {
            static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
            {
                bool NewPQ = true;
                foreach (string tag in __instance.CompanyTags)
                {
                    if (tag.StartsWith("PilotQuirksSave{"))
                        NewPQ = false;
                }
                if (!NewPQ)
                {
                    DeserializePilotQuirks();
                }
            }
        }

        internal static void DeserializePilotQuirks()
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            if (sim.CompanyTags.First(x => x.StartsWith("PilotQuirksSave{")) != null)
                MechBonding.PilotsAndMechs = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(sim.CompanyTags.First(x => x.StartsWith("PilotQuirksSave{")).Substring(15));
            
        }

        //What happens on game save.
        [HarmonyPatch(typeof(SimGameState), "Dehydrate")]
        public static class SimGameState_Dehydrate_Patch
        {
            public static void Prefix()
            {
                SerializePilotQuirks();
            }
        }
        internal static void SerializePilotQuirks()
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            sim.CompanyTags.Where(tag => tag.StartsWith("PilotQuirks")).Do(x => sim.CompanyTags.Remove(x));
            sim.CompanyTags.Add("PilotQuirksSave" + JsonConvert.SerializeObject(Pilot_Quirks.MechBonding.PilotsAndMechs));
        }
    }
}
