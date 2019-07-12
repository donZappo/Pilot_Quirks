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

namespace Pilot_Quirks
{
    class UI_Changes
    {
        [HarmonyPatch(typeof(SG_HiringHall_DetailPanel), "DisplayPilot")]
        public static class SG_HiringHall_DetailPanel_DisplayPilot_Patch
        {
            public static void Prefix(Pilot p)
            {
                Logger.LogLine("Details");
                foreach (var tag in p.pilotDef.PilotTags)
                {
                    if (Pre_Control.settings.TagIDToDescription.Keys.Contains(tag)) 
                        p.Description.Details += "\n\n" + tag + "\n\n" + Pre_Control.settings.TagIDToDescription[tag];
                }

            }
        }
    }
}
