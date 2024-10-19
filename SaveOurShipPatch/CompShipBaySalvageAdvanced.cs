using RimWorld;
using Verse;
using SaveOurShip2;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Verse.Noise;

namespace SaveOurShipPatch
{
    public class CompShipBaySalvageAdvanced : CompShipBaySalvage
    {
        //指定空投的位置
        public static Building drop_pod_target = null;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            Command_Toggle command_isDropPodPos = new Command_Toggle();
            command_isDropPodPos.icon = ContentFinder<Texture2D>.Get("Things/Special/DropPod");
            command_isDropPodPos.defaultLabel = "DropPoint".Translate();
            command_isDropPodPos.defaultDesc = "DropPointDesc".Translate();
            command_isDropPodPos.isActive = (() => parent == drop_pod_target);
            command_isDropPodPos.toggleAction = delegate
            {
                if (parent != drop_pod_target)
                {
                    drop_pod_target = parent as Building;
                }
                else
                {
                    drop_pod_target = null;
                }
            };
            yield return command_isDropPodPos;

            bool nominal = mapComp.ShipMapState == ShipMapState.nominal;
            //player ship map
            if (mapComp.ShipsOnMap.Count > 1)
            {
                if (mapComp.ShipsOnMap.Values.Where(x => x.IsStuckAndNotAssisted() && x.BuildingCount > 4).Any())
                {
                    var deconstructShipEnemy = new Command_Action
                    {
                        action = () => Find.WindowStack.Add(new Dialog_Recycle(this, mapComp.map)),
                        icon = ContentFinder<Texture2D>.Get("UI/SalvageDeconstruct"),
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("SalvageDeconstructCommand", mapComp.map.Parent.Label),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("SalvageDeconstructCommandDesc", mapComp.map.Parent.Label),
                    };
                    if (!nominal || !mapComp.CanClaimNow(Faction.OfPlayer))
                    {
                        deconstructShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("SalvageDeconstructDisabled"));
                    }
                    yield return deconstructShipEnemy;
                }
                else
                {
                    if (Dialog_Recycle.map_recycle_rate.ContainsKey(mapComp.map))
                    {
                        Log.Message($"unused player map recycle rate removed: {Dialog_Recycle.map_recycle_rate[mapComp.map]}");
                        Dialog_Recycle.map_recycle_rate.Remove(mapComp.map);
                    }
                }
            }
            else
            {
                if (Dialog_Recycle.map_recycle_rate.ContainsKey(mapComp.map))
                {
                    Log.Message($"unused player map recycle rate removed: {Dialog_Recycle.map_recycle_rate[mapComp.map]}");
                    Dialog_Recycle.map_recycle_rate.Remove(mapComp.map);
                }
            }
            foreach (Map map in Find.Maps)
            {
                if (map == mapComp.map)
                    continue;
                ShipMapComp targetMapComp = map.GetComponent<ShipMapComp>();
                if (targetMapComp.ShipMapState != ShipMapState.isGraveyard)
                    continue;
                if (targetMapComp.MapShipCells.Any())
                {
                    var deconstructShipEnemy = new Command_Action
                    {
                        action = () => Find.WindowStack.Add(new Dialog_Recycle(this, map)),
                        icon = ContentFinder<Texture2D>.Get("UI/SalvageDeconstruct"),
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("SalvageDeconstructCommand", map.Parent.Label),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("SalvageDeconstructCommandDesc", map.Parent.Label),
                    };
                    if (!nominal)
                    {
                        deconstructShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("SalvageDeconstructDisabled"));
                    }
                    yield return deconstructShipEnemy;
                }

                // advanced glittertech salvage bay
                if (Props.beam && (parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false))
                {
                    var scoop_debris = new Command_Action
                    {
                        action = () => Find.WindowStack.Add(new Dialog_Scoop(this, map)),
                        icon = ContentFinder<Texture2D>.Get("UI/ScoopDebris"),
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ScoopDebrisCommand", map.Parent.Label),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ScoopDebrisCommandDesc", map.Parent.Label),
                    };
                    if (!nominal)
                    {
                        scoop_debris.Disable(TranslatorFormattedStringExtensions.Translate("SalvageDeconstructDisabled"));
                    }
                    yield return scoop_debris;
                }
            }


        }
    }
}