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
using BattleTech.UI.Tooltips;

namespace Pilot_Quirks
{
    class UI_Changes
    {
        [HarmonyPatch(typeof(SG_HiringHall_DetailPanel), "DisplayPilot")]
        public static class SG_HiringHall_DetailPanel_DisplayPilot_Patch
        {
            public static void Prefix(Pilot p)
            {
                if (!p.pilotDef.PilotTags.Contains("PQ_Tagged"))
                {
                    p.pilotDef.PilotTags.Add("PQ_Tagged");
                    if (p.pilotDef.Description.Id.StartsWith("pilot_ronin") || p.pilotDef.Description.Id.StartsWith("pilot_backer"))
                        p.Description.Details += "\n\n";

                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    foreach (var tag in p.pilotDef.PilotTags)
                    {
                        if (p.pilotDef.Description.Id.StartsWith("pilot_ronin") || p.pilotDef.Description.Id.StartsWith("pilot_backer"))
                        {
                            if (Pre_Control.settings.TagIDToDescription.Keys.Contains(tag))
                            {
                                p.Description.Details += "<b>" + Pre_Control.settings.TagIDToNames[tag] + ": </b>" +
                                       Pre_Control.settings.TagIDToDescription[tag] + "\n\n";
                            }
                        }
                        else
                        {
                            if (Pre_Control.settings.TagIDToDescription.Keys.Contains(tag))
                                p.Description.Details += Pre_Control.settings.TagIDToNames[tag] + "\n\n"
                                        + Pre_Control.settings.TagIDToDescription[tag] + "\n\n";
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
        public static class SGBarracksRosterSlot_Refresh_Patch
        {
            public static void Postfix(SGBarracksRosterSlot __instance)
            {
                if (__instance.Pilot == null)
                    return;

                var tooltip = __instance.gameObject.GetComponent<HBSTooltip>()
                              ?? __instance.gameObject.AddComponent<HBSTooltip>();

                var p = __instance.Pilot;
                string TagDesc = "";
                foreach (var tag in p.pilotDef.PilotTags)
                {
                    if (Pre_Control.settings.TagIDToDescription.Keys.Contains(tag))
                    {
                        TagDesc += "<b>" + Pre_Control.settings.TagIDToNames[tag] + ": </b>" +
                                Pre_Control.settings.TagIDToDescription[tag] + "\n\n";
                    }
                }

                    var descriptionDef = new BaseDescriptionDef("Tags", p.Callsign,
                    TagDesc, null);

                tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descriptionDef));
            }
        }
    }
}
