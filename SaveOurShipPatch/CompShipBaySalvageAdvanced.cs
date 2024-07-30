using RimWorld;
using Verse;
using SaveOurShip2;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SaveOurShipPatch
{
    public class CompShipBaySalvageAdvanced : CompShipBaySalvage
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            bool nominal = mapComp.ShipMapState == ShipMapState.nominal;
            foreach (Map map in Find.Maps)
            {
                if (map == mapComp.map)
                    continue;
                ShipMapComp targetMapComp = map.GetComponent<ShipMapComp>();
                if (targetMapComp.ShipMapState != ShipMapState.isGraveyard)
                    continue;
                if (targetMapComp.MapShipCells.Any())
                {
                    var deconstructShipEnemy = new Command_DeconstructShipEnemy(mapComp.map, map)
                    {
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
            }


        }
    }
}