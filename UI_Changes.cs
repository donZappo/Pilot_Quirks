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
                else if (p.pilotDef.PilotTags.Contains("pilot_lightinjury"))
                    TagDesc += "<b>***PILOT LIGHT INJURY***</b>\nPilot cannot drop into combat. This pilot requires rest after dropping too frequently while fatigued.\n\n";

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
                if (!Pre_Control.settings.MechBonding)
                    return;

                if (tagSet.Contains("PQ_Mech_Mastery"))
                {
                    bool HasTattoo = PilotHolder.pilotDef.PilotTags.Any(x => x.StartsWith("PQ_Pilot_GUID"));
                    if (!HasTattoo)
                        return;

                    string PilotTattoo = PilotHolder.pilotDef.PilotTags.First(x => x.StartsWith("PQ_Pilot_GUID"));
                    if (!MechBonding.PilotsAndMechs.Keys.Contains(PilotTattoo) || MechBonding.PilotsAndMechs[PilotTattoo].Count() == 0)
                        return;

                    var MechExperience = MechBonding.PilotsAndMechs[PilotTattoo];
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

                    string DescriptionName = MasteryTier + " 'Mech Pilot";

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
            if (!Pre_Control.settings.MechBonding)
                return "";

            bool HasTattoo = pilot.pilotDef.PilotTags.Any(x => x.StartsWith("PQ_Pilot_GUID"));
            if (!HasTattoo)
                return "<b>No 'Mech mastery.</b>";

            string PilotTattoo = pilot.pilotDef.PilotTags.First(x => x.StartsWith("PQ_Pilot_GUID"));
            if (!MechBonding.PilotsAndMechs.Keys.Contains(PilotTattoo) || MechBonding.PilotsAndMechs[PilotTattoo].Count() == 0)
                return "<b>No 'Mech mastery.</b>";

            var MechExperience = MechBonding.PilotsAndMechs[PilotTattoo];
           
            string TierOneString = "\n• Reduced Fatigue\n\t(";
            string TierTwoString = "\n• +1 Piloting Skill\n\t(";
            string TierThreeString = "\n• +1 Gunnery Skill\n\t(";
            string TierFourString = "";
            bool TierOne = false;
            bool TierTwo = false;
            bool TierThree = false;
            bool TierFour = false;
            int h = 0;
            foreach (var BondedMech in MechExperience.OrderByDescending(x => x.Value))
            {
                if (MechExperience[BondedMech.Key] >= Pre_Control.settings.Tier1)
                {
                    TierOneString += BondedMech.Key + ", ";
                    TierOne = true;
                }
                if (MechExperience[BondedMech.Key] >= Pre_Control.settings.Tier2)
                {
                    TierTwoString += BondedMech.Key + ", ";
                    TierTwo = true;
                }
                if (MechExperience[BondedMech.Key] >= Pre_Control.settings.Tier3)
                {
                    TierThreeString += BondedMech.Key + ", ";
                    TierThree = true;
                }
                if (MechExperience[BondedMech.Key] >= Pre_Control.settings.Tier4)
                {
                    var weightClass = BondedMech.Key;
                    if (weightClass == "LIGHT")
                        TierFourString += "\n• Light 'Mechs Have Damage Reduction\n\t(";
                    if (weightClass == "MEDIUM")
                        TierFourString += "\n• Medium 'Mechs Move After Melee\n\t(";
                    if (weightClass == "HEAVY")
                        TierFourString += "\n• Heavy 'Mechs Ignore Rough Terrain\n\t(";
                    if (weightClass == "ASSAULT")
                        TierFourString += "\n• Assault 'Mechs Have Pilot Protection\n\t(";
                    TierFourString += BondedMech.Key + ", ";
                    TierFour = true;
                }
                h++;
                if (h == 3)
                    break;
            }

            char[] charsToTrim = { ' ', ',' };

            if (TierOne)
            {
                TagDesc += "<b>'Mech Mastery:</b> Bonuses when piloting 'Mechs.";
                TierOneString = TierOneString.TrimEnd(charsToTrim) + ")";
                TagDesc += TierOneString;
            }
            else
            {
                TagDesc += "<b>No 'Mech mastery.</b>";
            }
            if (TierTwo)
            {
                TierTwoString = TierTwoString.TrimEnd(charsToTrim) + ")";
                TagDesc += TierTwoString;
            }
            if (TierThree)
            {
                TierThreeString = TierThreeString.TrimEnd(charsToTrim) + ")";
                TagDesc += TierThreeString;
            }
            if (TierFour)
            {
                TierFourString = TierFourString.TrimEnd(charsToTrim) + ")";
                TagDesc += TierFourString;
            }

            TagDesc += "\n\nMech XP - ";
            int i = 0;
            foreach (var MechXP in MechExperience.OrderByDescending(x => x.Value))
            {
                TagDesc += MechXP.Key + ": " + MechXP.Value + ", ";
                i++;
                if (i == 3) break;
            }
            TagDesc = TagDesc.TrimEnd(charsToTrim);
            TagDesc = TagDesc + "\n\n";
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
