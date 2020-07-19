﻿using System;
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

namespace Pilot_Quirks
{
    class ExperienceAdjustments
    {
        public static bool isIncreased = false;
        public static bool isDecreased = false;
        public static float ExpHolder;
        public static float MultHolder;
        public static bool isBookish = false;
        public static bool isKlutz = false;
        public static bool isAssassin = false;
        public static bool OnValueClick;
        public static SimGameState saveSimState;
        public static Pilot SavedPilot;
        public static bool UseSavedPilot = false;

        [HarmonyPatch(typeof(PilotGenerator), "GenerateRandomPilot")]
        public static class PilotGenerator_GenerateRandomPilot_Patch
        {
            public static void Postfix(ref PilotDef __result)
            {
                if (__result.PilotTags.Contains("pilot_bookish"))
                {
                    __result.ExperienceSpent = CalculateExperience(__result, 1);
                }
                if (__result.PilotTags.Contains("pilot_klutz"))
                {
                    __result.ExperienceSpent = CalculateExperience(__result, 1);
                }
                if (__result.PilotTags.Contains("pilot_assassin"))
                {
                    __result.ExperienceSpent = CalculateExperience(__result, 1);
                }
            }
        }


        public static int CalculateExperience(PilotDef pilotdef, int minLevel)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            int TotalXP = 0;
            float exp = sim.Constants.Pilot.PilotLevelCostExponent;
            float mult = sim.Constants.Pilot.PilotLevelCostMultiplier;

            for (int i = pilotdef.SkillGunnery; i > minLevel; i--)
            {
                TotalXP += Mathf.CeilToInt(Mathf.Pow((float)i, (exp + exp * Pre_Control.settings.pilot_XP_change))
                    * (mult + mult * Pre_Control.settings.pilot_XP_change));
            }
            for (int i = pilotdef.SkillPiloting; i > 1; i--)
            {
                TotalXP += Mathf.CeilToInt(Mathf.Pow((float)i, (exp + exp * Pre_Control.settings.pilot_XP_change))
                    * (mult + mult * Pre_Control.settings.pilot_XP_change));
            }
            for (int i = pilotdef.SkillGuts; i > 1; i--)
            {
                TotalXP += Mathf.CeilToInt(Mathf.Pow((float)i, (exp + exp * Pre_Control.settings.pilot_XP_change))
                    * (mult + mult * Pre_Control.settings.pilot_XP_change));
            }
            for (int i = pilotdef.SkillTactics; i > 1; i--)
            {
                TotalXP += Mathf.CeilToInt(Mathf.Pow((float)i, (exp + exp * Pre_Control.settings.pilot_XP_change))
                    * (mult + mult * Pre_Control.settings.pilot_XP_change));
            }
            return TotalXP;
        }

        [HarmonyPatch(typeof(SimGameState), "RespecPilot")]
        public static class SimGameState_RespecPilot_Patch
        {
            public static void Postfix(SimGameState __instance, ref Pilot pilot)
            {
                try
                {
                    PilotDef pilotDef = pilot.pilotDef.CopyToSim();
                    isIncreased = false;
                    isDecreased = false;
                    isBookish = pilot.pilotDef.PilotTags.Contains("pilot_bookish");
                    isKlutz = pilot.pilotDef.PilotTags.Contains("pilot_klutz");
                    isAssassin = pilot.pilotDef.PilotTags.Contains("pilot_assassin");
                    
                    if (isBookish || isKlutz || isAssassin)
                    {
                        int NewXP = 0;
                        if (pilotDef.BonusPiloting > 0)
                        {
                            if (isKlutz)
                                isIncreased = true;
                            else
                                isIncreased = false;
                            isDecreased = false;
                            NewXP += __instance.GetLevelRangeCost(pilotDef.BasePiloting, pilotDef.SkillPiloting - 1);
                        }
                        if (pilotDef.BonusGunnery > 0)
                        {
                            isIncreased = false;
                            if (isAssassin)
                                isDecreased = true;
                            else
                                isDecreased = false;
                            NewXP += __instance.GetLevelRangeCost(pilotDef.BaseGunnery, pilotDef.SkillGunnery - 1);
                        }
                        if (pilotDef.BonusGunnery > 0)
                        {
                            if (isBookish)
                                isIncreased = true;
                            else
                                isIncreased = false;
                            isDecreased = false;
                            NewXP += __instance.GetLevelRangeCost(pilotDef.BaseGunnery, pilotDef.SkillGunnery - 1);
                        }
                        if (pilotDef.BonusTactics > 0)
                        {
                            isIncreased = false;
                            if (isBookish)
                                isDecreased = true;
                            else
                                isDecreased = false;
                            NewXP += __instance.GetLevelRangeCost(pilotDef.BaseTactics, pilotDef.SkillTactics - 1);
                        }
                        int XPAdjust = NewXP - pilot.pilotDef.ExperienceUnspent;
                        pilot.AddExperience(0, "Respec", XPAdjust);
                    }
                   
                }
                catch (Exception e)
                {
                    Pre_Control.Log.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetLevelCost")]
        public static class SimGameState_GetLevelCost_Patch
        {
            public static void Prefix(SimGameState __instance)
            {
                try
                {
                    ExpHolder = __instance.Constants.Pilot.PilotLevelCostExponent;
                    MultHolder = __instance.Constants.Pilot.PilotLevelCostMultiplier;

                    if (isIncreased)
                    {
                        __instance.Constants.Pilot.PilotLevelCostExponent *= 1 + Pre_Control.settings.pilot_XP_change;
                        __instance.Constants.Pilot.PilotLevelCostMultiplier *= 1 + Pre_Control.settings.pilot_XP_change;
                    }
                    if (isDecreased)
                    {
                        __instance.Constants.Pilot.PilotLevelCostExponent *= 1 - Pre_Control.settings.pilot_XP_change;
                        __instance.Constants.Pilot.PilotLevelCostMultiplier *= 1 - Pre_Control.settings.pilot_XP_change;
                    }
                }
                catch (Exception e)
                {
                    Pre_Control.Log.Error(e);
                }
            }

            public static void Postfix(SimGameState __instance)
            {
                try
                {
                    __instance.Constants.Pilot.PilotLevelCostExponent = ExpHolder;
                    __instance.Constants.Pilot.PilotLevelCostMultiplier = MultHolder;
                }
                catch (Exception e)
                {
                    Pre_Control.Log.Error(e);
                }
            }
        }
        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "ForceResetCharacter")]
        public static class SGBarracksAdvancementPanel_ForceResetCharacter_Patch
        {
            public static void Prefix(SGBarracksAdvancementPanel __instance)
            {
                try
                {
                    isIncreased = false;
                    isDecreased = false;
                    isBookish = false;
                    isKlutz = false;
                    isAssassin = false;
                    OnValueClick = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_bookish"))
                    {
                        isBookish = true;
                    }
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_klutz"))
                    {
                        isKlutz = true;
                    }
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                    {
                        isAssassin = true;
                    }
                }
                catch (Exception e)
                {
                    Pre_Control.Log.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "Initialize")]
        public static class SGBarracksAdvancementPanel_Initialize_Patch
        {
            public static void Prefix(SGBarracksAdvancementPanel __instance)
            {
                try
                {
                    isBookish = false;
                    isKlutz = false;
                    isAssassin = false;

                    if (UseSavedPilot)
                        __instance.curPilot = SavedPilot;

                    if (__instance.curPilot == null)
                        return;

                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_bookish"))
                    {
                        isBookish = true;
                    }
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_klutz"))
                    {
                        isKlutz = true;
                    }
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                    {
                        isAssassin = true;
                    }
                }
                catch (Exception e)
                {
                    Pre_Control.Log.Error(e);
                }
            }
            public static void Postfix(SGBarracksAdvancementPanel __instance)

            {
                saveSimState = __instance.simState;
            }
        }

            [HarmonyPatch(typeof(SGBarracksSkillPip), "Initialize")]
            public static class SGBarracksSkillPip_Initialize_Patch
            {
                public static void Prefix(string type, int index, ref int cost)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    try
                    {
                        isIncreased = false;
                        isDecreased = false;
                        if (isBookish || isKlutz || isAssassin)
                        {
                            if (type == "Piloting")
                            {
                                if (isKlutz)
                                    isIncreased = true;
                                else
                                    isIncreased = false;

                                isDecreased = false;
                                cost = sim.GetLevelCost(index);
                            }
                            if (type == "Gunnery")
                            {
                                isIncreased = false;

                            if (isAssassin)
                                isDecreased = true;
                            else
                                isDecreased = false;

                                cost = sim.GetLevelCost(index);
                            }

                            if (type == "Guts")
                            {
                            if (isBookish)
                                isIncreased = true;
                            else
                                isIncreased = false;

                                isDecreased = false;
                                cost = sim.GetLevelCost(index);
                            }
                            if (type == "Tactics")
                            {
                                isIncreased = false;

                            if (isBookish)
                                isDecreased = true;
                            else
                                isDecreased = false;

                                cost = sim.GetLevelCost(index);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Pre_Control.Log.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "OnValueClick")]
            public static class SGBarracksAdvancementPanel_OnValueClick_Patch
            {
                public static void Prefix(SGBarracksAdvancementPanel __instance, string type)
                {
                    try
                    {
                        isBookish = false;
                        if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_bookish"))
                            isBookish = true;

                    isKlutz = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_klutz"))
                        isKlutz = true;

                    isAssassin = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                        isAssassin = true;

                    OnValueClick = true;
                        if (isBookish || isKlutz || isAssassin)
                        {
                            if (type == "Piloting")
                            {
                            if (isKlutz)
                                isIncreased = true;
                            else
                                isIncreased = false;

                                isDecreased = false;
                            }
                            if (type == "Gunnery")
                            {
                                isIncreased = false;

                            if (isAssassin)
                                isDecreased = true;
                            else
                                isDecreased = false;
                            }
                            if (type == "Guts")
                            {
                            if (isBookish)
                                isIncreased = true;
                            else
                                isIncreased = false;

                                isDecreased = false;
                            }
                            if (type == "Tactics")
                            {
                                isIncreased = false;

                            if (isBookish)
                                isDecreased = true;
                            else
                                isDecreased = false;
                            }
                        }
                        else
                        {
                            isIncreased = false;
                            isDecreased = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Pre_Control.Log.Error(e);
                    }
                }
                public static void Postfix()
                {
                    isIncreased = false;
                    isDecreased = false;
                }
            }

            [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "SetTempPilotSkill")]
            public static class SGBarracksAdvancementPanel_SetTempPilotSkill_Patch
            {
                public static void Prefix(SGBarracksAdvancementPanel __instance, string type, int skill, ref int expAmount)
                {
                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                    try
                    {
                        isBookish = false;
                        if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_bookish"))
                            isBookish = true;

                    isKlutz = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_klutz"))
                        isKlutz = true;

                    isAssassin = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                        isAssassin = true;

                    int twiddle = -1;
                    if (OnValueClick)
                        twiddle = 1;

                    if (isBookish || isKlutz || isAssassin)
                        {
                            if (type == "Piloting")
                            {
                            if (isKlutz)
                                isIncreased = true;
                            else
                                isIncreased = false;

                                isDecreased = false;
                                expAmount = twiddle * sim.GetLevelCost(skill);
                            }
                            if (type == "Gunnery")
                            {
                                isIncreased = false;

                            if (isAssassin)
                                isDecreased = true;
                            else
                                isDecreased = false;

                                expAmount = twiddle * sim.GetLevelCost(skill);
                            }
                            if (type == "Guts")
                            {
                            if (isBookish)
                                isIncreased = true;
                            else
                                isIncreased = false;
                            
                                isDecreased = false;
                                expAmount = twiddle * sim.GetLevelCost(skill);
                            }
                            if (type == "Tactics")
                            {
                                isIncreased = false;
                            if (isBookish)
                                isDecreased = true;
                            else
                                isDecreased = false;

                                expAmount = twiddle * sim.GetLevelCost(skill);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Pre_Control.Log.Error(e);
                    }
                }
            }

            [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "SetPilot")]
            public static class SGBarracksAdvancementPanel_SetPilot_Patch
            {
                public static void Prefix(SGBarracksAdvancementPanel __instance)
                {
                    try
                    {
                        isBookish = false;
                    isKlutz = false;
                        if (__instance.curPilot == null)
                            return;
                        if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_bookish"))
                            isBookish = true;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_klutz"))
                        isKlutz = true;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                        isAssassin = true;
                }
                    catch (Exception e)
                    {
                        Pre_Control.Log.Error(e);
                    }
                }
            }
            [HarmonyPatch(typeof(SGBarracksAdvancementPanel), "SetPips")]
            public static class SGBarracksAdvancementPanel_SetPips_Patch
            {
                public static void Prefix(SGBarracksAdvancementPanel __instance, List<SGBarracksSkillPip> pips, ref bool needsXP, int idx)
                {
                    try
                    {
                        isBookish = false;
                        if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_bookish"))
                            isBookish = true;

                    isKlutz = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_klutz"))
                        isKlutz = true;

                    isAssassin = false;
                    if (__instance.curPilot.pilotDef.PilotTags.Contains("pilot_assassin"))
                        isAssassin = true;

                    var sim = UnityGameInstance.BattleTechGame.Simulation;
                        if (__instance.curPilot == null)
                            return;
                        Pilot p = __instance.curPilot;
                        isIncreased = false;
                        isDecreased = false;
                        if (isBookish || isKlutz || isAssassin)
                        {
                            if (pips == __instance.pilotPips)
                            {
                            if (isKlutz)
                                isIncreased = true;
                            else
                                isIncreased = false;

                                isDecreased = false;
                                bool XPLevelCheck = p.UnspentXP < sim.GetLevelCost(idx);
                                needsXP = XPLevelCheck && p.Piloting - 1 < idx;
                            }
                            if (pips == __instance.gunPips)
                            {
                                isIncreased = false;

                            if (isAssassin)
                                isDecreased = true;
                            else
                                isDecreased = false;

                                bool XPLevelCheck = p.UnspentXP < sim.GetLevelCost(idx);
                                needsXP = XPLevelCheck && p.Gunnery - 1 < idx;
                            }
                            if (pips == __instance.gutPips)
                            {
                            if (isBookish)
                                isIncreased = true;
                            else
                                isIncreased = false;

                                isDecreased = false;
                                bool XPLevelCheck = p.UnspentXP < sim.GetLevelCost(idx);
                                needsXP = XPLevelCheck && p.Guts - 1 < idx;
                            }
                            if (pips == __instance.tacPips)
                            {
                                isIncreased = false;

                            if (isBookish)
                                isDecreased = true;
                            else
                                isDecreased = false;

                                bool XPLevelCheck = p.UnspentXP < sim.GetLevelCost(idx);
                                needsXP = XPLevelCheck && p.Tactics - 1 < idx;
                            }
                            isIncreased = false;
                            isDecreased = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Pre_Control.Log.Error(e);
                    }
                }
            }
            [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "DisplayPilot")]
            public static class SGBarracksMWDetailPanel_DisplayPilot_Patch
            {
                public static void Prefix(SGBarracksMWDetailPanel __instance, Pilot p)
                {
                UseSavedPilot = true;
                SavedPilot = p;
                    isBookish = false;
                    if (p.pilotDef.PilotTags.Contains("pilot_bookish"))
                    {
                        isBookish = true;
                    }

                isKlutz = false;
                if (p.pilotDef.PilotTags.Contains("pilot_klutz"))
                {
                    isKlutz = true;
                }

                isAssassin = false;
                if (p.pilotDef.PilotTags.Contains("pilot_assassin"))
                {
                    isAssassin = true;
                }

                __instance.Initialize(saveSimState);
            }
            public static void Postfix()
            {
                UseSavedPilot = false;
            }
        }
    }
}
