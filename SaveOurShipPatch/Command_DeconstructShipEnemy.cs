using RimWorld;
using Verse;
using SaveOurShip2;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace SaveOurShipPatch
{
    public class Command_DeconstructShipEnemy : Command
    {
        private Map player_map;
        private Map salvage_map;
        public Command_DeconstructShipEnemy(Map player_map, Map salvage_map) : base()
        {
            this.player_map = player_map;
            this.salvage_map = salvage_map;

        }
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            Find.WindowStack.Add(new Dialog_Recycle(player_map, salvage_map));
        }
        /*public void ConvertShipToResources()
        {
            ShipMapComp targetMapComp = salvage_map.GetComponent<ShipMapComp>();
            //Count all buildings costs in enemy ship map
            HashSet<IntVec3> area = new HashSet<IntVec3>();
            foreach (var ship_cache in targetMapComp.ShipsOnMap.Values)
            {
                area.AddRange(ship_cache.Area);
            }
            List<Thing> things = new List<Thing>();
            foreach (IntVec3 pos in area)
            {
                foreach (Thing t in pos.GetThingList(salvage_map))
                {
                    if (!(t is Pawn) && !things.Contains(t))
                    {
                        things.Add(t);
                    }
                }
            }
            Dictionary<ThingDef, int> cost_list = new Dictionary<ThingDef, int>();
            foreach (Thing t in things)
            {
                try
                {
                    if (t is Building && t.def.destroyable && !t.Destroyed)
                    {
                        foreach (var cost in t.def.costList)
                        {
                            if (!cost_list.ContainsKey(cost.thingDef))
                            {
                                cost_list[cost.thingDef] = 0;
                            }
                            cost_list[cost.thingDef] += cost.count;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("" + e);
                }
            }
            //send all resources to player's map using pod
            List<Thing> thing_list = new List<Thing>();
            string drop_pod_received_text = "";
            foreach (var thing_def_count in cost_list)
            {
                Thing thing = ThingMaker.MakeThing(thing_def_count.Key, null);
                thing.stackCount = thing_def_count.Value;
                TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(player_map), player_map, thing);
                drop_pod_received_text += thing_def_count.Key.label + ": " + thing_def_count.Value.ToString() + "\n";
                thing_list.Add(thing);
            }
            Find.LetterStack.ReceiveLetter(
                    TranslatorFormattedStringExtensions.Translate("DropPodReceivedLabel"),
                    TranslatorFormattedStringExtensions.Translate("DropPodReceivedText", drop_pod_received_text),
                    LetterDefOf.PositiveEvent, thing_list, null, null, null, null
                    );
            //destroy all ships in that map
            while (targetMapComp.ShipsOnMap.Count > 0)
            {
                var ship_index = targetMapComp.ShipsOnMap.Keys.First();
                ShipInteriorMod2.RemoveShipOrArea(salvage_map, ship_index);
            }
        }*/
    }
}
