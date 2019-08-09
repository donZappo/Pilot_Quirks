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
using HBS.Collections;

namespace Pilot_Quirks
{
    class UI_Changes
    {
        public static Pilot PilotHolder;

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

                if (p.pilotDef.PilotTags.Contains("pilot_fatigued"))
                {
                    TagDesc += "<b>***PILOT FATIGUED***</b>\nPilot will suffer from Low Spirits if used in combat. The lance will also experience reduced Resolve per turn during combat.\n\n";
                }

                foreach (var tag in p.pilotDef.PilotTags)
                {
                    if (Pre_Control.settings.TagIDToDescription.Keys.Contains(tag))
                    {
                        TagDesc += "<b>" + Pre_Control.settings.TagIDToNames[tag] + ": </b>" +
                                Pre_Control.settings.TagIDToDescription[tag] + "\n\n";
                    }
                    else if (tag == "PQ_Mech_Mastery")
                    {
                        TagDesc = MechMasterTagDescription(__instance.pilot, TagDesc);
                    }
                }

                var descriptionDef = new BaseDescriptionDef("Tags", p.Callsign, TagDesc, null);
                tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(descriptionDef));
            }
        }

        [HarmonyPatch(typeof(TagDataStructFetcher), "GetItem")]
        public static class TagDataStructFetcher_GetItem_Patch
        {
            public static void Postfix(string id, TagDataStruct __result)
            {
                if (id == "HACK_GENCON_UNIT")
                    __result.DescriptionTag += MechMasterTagDescription(UI_Changes.PilotHolder, __result.DescriptionTag);

                if (!Pre_Control.settings.TagIDToDescription.ContainsKey(id))
                    return;

                __result.DescriptionTag += "\n\n" + Pre_Control.settings.TagIDToDescription[id];
            }
        }

        [HarmonyPatch(typeof(HBSTagView), "Initialize")]
        public static class HBSTagView_Initialize_Patch
        {
            public static void Postfix(HBSTagView __instance, TagSet tagSet, GameContext context)
            {
                if (tagSet.Contains("PQ_Mech_Mastery"))
                {
                    var MechExperience = MechBonding.PilotsAndMechs[UI_Changes.PilotHolder.Description.Id];
                    var BondedMech = MechExperience.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    string MasteryTier = "";
                    if (MechExperience[BondedMech] >= Pre_Control.settings.Tier4)
                        MasteryTier = "Elite ";
                    else if (MechExperience[BondedMech] >= Pre_Control.settings.Tier3)
                        MasteryTier = "Veteran ";
                    else if (MechExperience[BondedMech] >= Pre_Control.settings.Tier2)
                        MasteryTier = "Regular ";
                    else if (MechExperience[BondedMech] >= Pre_Control.settings.Tier1)
                        MasteryTier = "Green ";

                    string DescriptionName = MasteryTier + BondedMech + " Pilot";

                    var item = new TagDataStruct("HACK_GENCON_UNIT", true, true, "name", DescriptionName , "description");
                    string contextItem = string.Format("{0}[{1}]", "TDSF", item.Tag);
                    string friendlyName = item.FriendlyName;
                    var itemTT = TooltipUtilities.GetGameContextTooltipString(contextItem, friendlyName);

                    __instance.AddTag(itemTT, item.FriendlyName);
                }
            }
        }

        public static string MechMasterTagDescription(Pilot pilot, string TagDesc)
        {
            var MechExperience = MechBonding.PilotsAndMechs[pilot.Description.Id];
            var BondedMech = MechExperience.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            //string MasteryTier = "";
            //if (MechExperience[BondedMech] >= Pre_Control.settings.Tier4)
            //    MasteryTier = "Elite ";
            //else if (MechExperience[BondedMech] >= Pre_Control.settings.Tier3)
            //    MasteryTier = "Veteran ";
            //else if (MechExperience[BondedMech] >= Pre_Control.settings.Tier2)
            //    MasteryTier = "Regular ";
            //else if (MechExperience[BondedMech] >= Pre_Control.settings.Tier1)
            //    MasteryTier = "Green ";

            //string DescriptionName = MasteryTier + BondedMech + " Pilot";
            //TagDesc += "<b>" + DescriptionName + "</b>";

            if (MechExperience[BondedMech] >= Pre_Control.settings.Tier1)
                TagDesc += "<b>" + BondedMech + " Mastery:</b> Bonuses when piloting a " + BondedMech + ".";
            else
                TagDesc += "<b>No 'Mech mastery.</b>";

            if (MechExperience[BondedMech] >= Pre_Control.settings.Tier1)
                TagDesc += "\n• Reduced Fatigue";
            if (MechExperience[BondedMech] >= Pre_Control.settings.Tier2)
                TagDesc += "\n• +1 Piloting Skill";
            if (MechExperience[BondedMech] >= Pre_Control.settings.Tier3)
                TagDesc += "\n• Increased Sensor and Spotting Range";
            if (MechExperience[BondedMech] >= Pre_Control.settings.Tier4)
                TagDesc += "\n• +1 Gunnery Skill";

            TagDesc += "\n\nMech Experience - ";
            int i = 0;
            foreach (var MechXP in MechExperience.OrderByDescending(x => x.Value))
            {
                TagDesc += MechXP.Key + ": " + MechXP.Value + ", ";
                i++;
                if (i == 2) break;
            }
            char[] charsToTrim = { ' ' , ',' };
            TagDesc = TagDesc.TrimEnd(charsToTrim);
            return TagDesc;
        }
    }

    //Hold Pilot for tag generation.
    [HarmonyPatch(typeof(SGBarracksServicePanel), "SetPilot")]
    public static class SGBarracksServicePanel_SetPilot_Patch
    {
        public static void Prefix(Pilot p)
        {
            UI_Changes.PilotHolder = p;
        }
    }

    [HarmonyPatch(typeof(SGMemorialWallAdditionalDetailsPanel), "DisplayPilot")]
    public static class SGMemorialWallAdditionalDetailsPanel_DisplayPilot_Patch
    {
        public static void Prefix(Pilot pilot)
        {
            UI_Changes.PilotHolder = pilot;
        }
    }
}
