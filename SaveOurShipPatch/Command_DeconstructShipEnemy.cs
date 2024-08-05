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
        private readonly CompShipBaySalvageAdvanced parent_comp;
        private readonly Map salvage_map;
        public Command_DeconstructShipEnemy(CompShipBaySalvageAdvanced parent_comp, Map salvage_map) : base()
        {
            this.parent_comp = parent_comp;
            this.salvage_map = salvage_map;
        }
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            Find.WindowStack.Add(new Dialog_Recycle(parent_comp, salvage_map));
        }
    }
}
