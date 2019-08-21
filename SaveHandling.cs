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

            bool OldSave = true;
            if (sim.CompanyTags.First(x => x.StartsWith("PilotQuirksSave{")) != null)
                MechBonding.PilotsAndMechs = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(sim.CompanyTags.First(x => x.StartsWith("PilotQuirksSave{")).Substring(15));

            bool SecondPQS2 = sim.CompanyTags.Any(x => x.StartsWith("PilotQuirksSave2"));
            if (SecondPQS2 && sim.CompanyTags.First(x => x.StartsWith("PilotQuirksSave2")) != null)
            {
                MechBonding.PQ_GUID = JsonConvert.DeserializeObject<int>(sim.CompanyTags.First(x => x.StartsWith("PilotQuirksSave2")).Substring(16));
                OldSave = false;
            }
            if (OldSave)
            {
                UpdateSavedGame();
            }
        }

        //This is necessary to correct previous saves to the new save dictionary
        internal static void UpdateSavedGame()
        {
            Dictionary<string, Dictionary<string, int>> OldDictionaryHolder = new Dictionary<string, Dictionary<string, int>>(MechBonding.PilotsAndMechs);
            MechBonding.PilotsAndMechs.Clear();
            Dictionary<string, string> PilotTransfer = new Dictionary<string, string>();

            var sim = UnityGameInstance.BattleTechGame.Simulation;
            PilotTransfer.Add(sim.Commander.pilotDef.Description.Id, "PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
            sim.Commander.pilotDef.PilotTags.Add("PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
            MechBonding.PQ_GUID++;

            foreach (PilotDef pilotdef in sim.CurSystem.AvailablePilots)
            {
                PilotTransfer.Add(pilotdef.Description.Id, "PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                pilotdef.PilotTags.Add("PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                MechBonding.PQ_GUID++;
            }
            foreach (Pilot hiredpilot in sim.PilotRoster)
            {
                PilotTransfer.Add(hiredpilot.pilotDef.Description.Id, "PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                hiredpilot.pilotDef.PilotTags.Add("PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                MechBonding.PQ_GUID++;
            }
            foreach (Pilot deadpilot in sim.Graveyard)
            {
                PilotTransfer.Add(deadpilot.pilotDef.Description.Id, "PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                deadpilot.pilotDef.PilotTags.Add("PQ_Pilot_GUID_" + MechBonding.PQ_GUID);
                MechBonding.PQ_GUID++;
            }
            foreach (string OldPilot in OldDictionaryHolder.Keys)
            {
                if (PilotTransfer.Keys.Contains(OldPilot))
                    MechBonding.PilotsAndMechs.Add(PilotTransfer[OldPilot], OldDictionaryHolder[OldPilot]);
            }
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
            sim.CompanyTags.Add("PilotQuirksSave2" + JsonConvert.SerializeObject(MechBonding.PQ_GUID));
        }
    }
}
