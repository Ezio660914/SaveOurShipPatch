using RimWorld;
using SaveOurShip2;
using UnityEngine;
using Verse;
using System.Linq;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse.Noise;
using System;
using Unity.Mathematics;
using Verse.Sound;
using System.Reflection;
using UnityEngine.UIElements;
namespace SaveOurShipPatch
{

    public class Dialog_Recycle : Window
    {
        protected readonly CompShipBaySalvageAdvanced parent_comp;
        protected Map PlayerMap { get { return parent_comp.mapComp.map; } }
        protected readonly Map salvage_map;
        public static Dictionary<Map, float> map_recycle_rate = new Dictionary<Map, float>();
        protected int NumSalvageBays
        {
            get
            {
                return parent_comp.mapComp.Bays.Where(b => b is CompShipBaySalvage && b.parent.Faction == Faction.OfPlayer).Count();
            }
        }
        protected List<TransferableOneWay> transferables;
        protected TransferableOneWayWidget items_transfer;
        protected float cachedMassUsage;
        protected bool CountToTransferChanged { get; set; }
        protected float MassCapacity
        {
            get
            {
                if (ModSettings_SaveOurShipPatch.limit_mass_per_bay)
                    return NumSalvageBays * ModSettings_SaveOurShipPatch.mass_per_bay;
                else
                    return float.PositiveInfinity;
            }
        }

        protected float MassUsage
        {
            get
            {
                if (CountToTransferChanged)
                {
                    CountToTransferChanged = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMass: true);
                }
                return cachedMassUsage;
            }
        }

        protected float EnergyUsage
        {
            get
            {
                return MassUsage * ModSettings_SaveOurShipPatch.energy_per_kg;
            }
        }
        protected PowerNet PowerNet
        {
            get
            {
                return parent_comp.parent.TryGetComp<CompPowerTrader>().PowerNet;
            }
        }
        protected float CurrentStoredEnergy
        {
            get
            {
                return this.PowerNet.CurrentStoredEnergy();
            }
        }
        protected static readonly List<Pair<float, Color>> ColorGrad = new List<Pair<float, Color>>
        {
            new Pair<float, Color>(0.37f, Color.green),
            new Pair<float, Color>(0.82f, Color.yellow),
            new Pair<float, Color>(1f, new Color(1f, 0.6f, 0f))
        };
        protected readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);
        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);
        protected override float Margin => 0f;
        public Dialog_Recycle(CompShipBaySalvageAdvanced parent_comp, Map salvage_map)
        {
            this.parent_comp = parent_comp;
            this.salvage_map = salvage_map;
            //Remove invalid map recycle rate in case the map was not recycled using this mod.
            var map_keys = map_recycle_rate.Keys.ToList();
            foreach (Map map in map_keys)
            {
                if (!Find.Maps.Contains(map))
                {
                    Log.Message($"invalid map recycle rate removed: {map_recycle_rate[map]}");
                    map_recycle_rate.Remove(map);
                }
            }
            //Add new map recycle rate
            if (!map_recycle_rate.ContainsKey(salvage_map))
            {
                map_recycle_rate[salvage_map] = Rand.Range(ModSettings_SaveOurShipPatch.RecycleRateMin, ModSettings_SaveOurShipPatch.RecycleRateMax);
                Log.Message($"map recycle rate (length: {map_recycle_rate.Count()}) added: {salvage_map.Parent.Label}: {map_recycle_rate[salvage_map]}");
            }
            forcePause = true;
            absorbInputAroundWindow = true;
        }
        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }
        protected static Color GetColor(float usage, float capacity, bool lerpColor)
        {
            if (capacity == float.PositiveInfinity)
            {
                return Color.white;
            }
            if (usage > capacity)
            {
                return Color.red;
            }
            if (lerpColor)
            {
                return GenUI.LerpColor(ColorGrad, usage / capacity);
            }
            return Color.white;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "RecycleShip".Translate(salvage_map.Parent.Label, map_recycle_rate[salvage_map].ToString("P0")));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect2 = new Rect(12f, 35f, inRect.width / 3f, 40f);
            List<TransferableUIUtility.ExtraInfo> tmp_info = new List<TransferableUIUtility.ExtraInfo>();
            TaggedString tagged_string = MassUsage.ToString() + " / " + MassCapacity.ToString("F0") + " " + "kg".Translate();
            tmp_info.Add(new TransferableUIUtility.ExtraInfo("Mass".Translate(), tagged_string, GetColor(MassUsage, MassCapacity, true), ""));
            TaggedString tagged_string2 = EnergyUsage.ToString() + " / " + CurrentStoredEnergy.ToString("F0") + " " + "Wd";
            tmp_info.Add(new TransferableUIUtility.ExtraInfo("Power".Translate(), tagged_string2, GetColor(EnergyUsage, CurrentStoredEnergy, true), ""));
            TransferableUIUtility.DrawExtraInfo(tmp_info, rect2);
            inRect.yMin += 119f;
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect3 = inRect.AtZero();
            DoBottomButtons(rect3);
            Rect inRect2 = rect3;
            inRect2.yMax -= 59f;
            bool anythingChanged = false;
            items_transfer.OnGUI(inRect2, out anythingChanged);
            if (anythingChanged)
            {
                CountToTransferChanged = true;
            }
            GUI.EndGroup();
        }
        protected virtual void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate()))
            {
                if (TryAccept())
                {
                    if (transferables.Any(x => x.CountToTransfer > 0))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmRecycling".Translate(), delegate
                        {
                            ConsumeEnergy();
                            SendTransferablesAndRemoveSalvages();
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            Close(doCloseSound: false);
                        }));
                    }
                    else
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        Close(doCloseSound: false);
                    }
                }
            }
            if (Widgets.ButtonText(new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                CalculateAndRecacheTransferables();
            }
            if (Widgets.ButtonText(new Rect(rect2.x - 20f - BottomButtonSize.x * 2, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "SelectEverything".Translate()))
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                SetToLoadEverything();
            }
        }
        protected bool TryAccept()
        {
            if (MassUsage > MassCapacity)
            {
                Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            else if (EnergyUsage > CurrentStoredEnergy)
            {
                Messages.Message("TooLargeEnergyUsage".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }
        protected void ConsumeEnergy()
        {
            //draw the same percentage from each cap: needed*current/currenttotal
            float current_stored_energy = CurrentStoredEnergy;
            Log.Message($"current stored energy: {current_stored_energy}");
            foreach (CompPowerBattery bat in this.PowerNet.batteryComps)
            {
                float amount = Mathf.Min(EnergyUsage * bat.StoredEnergy / current_stored_energy, bat.StoredEnergy);
                Log.Message($"total energy usage: {EnergyUsage}, each battery consume amount: {amount}");
                bat.DrawPower(amount);
            }
            Log.Message($"remaining stored energy: {CurrentStoredEnergy}");
        }
        protected virtual void SendTransferablesAndRemoveSalvages()
        {
            ShipMapComp targetMapComp = salvage_map.GetComponent<ShipMapComp>();
            //send all resources to player's map using pod
            List<Thing> thing_list = new List<Thing>();
            string drop_pod_received_text = "";

            foreach (TransferableOneWay tr in transferables)
            {
                Thing thing = ThingMaker.MakeThing(tr.ThingDef, null);
                thing.stackCount = tr.CountToTransfer;
                if (thing.stackCount > 0)
                {
                    TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(PlayerMap), PlayerMap, thing);
                    drop_pod_received_text += thing.def.label + ": " + thing.stackCount.ToString() + "\n";
                    thing_list.Add(thing);
                }
            }
            if (thing_list.Count > 0)
            {
                Find.LetterStack.ReceiveLetter(
                        "DropPodReceivedLabel".Translate(),
                        "DropPodReceivedText".Translate(drop_pod_received_text),
                        LetterDefOf.PositiveEvent, thing_list, null, null, null, null
                        );
                List<int> shipStuck;
                // if is player map, select salvage only
                if (salvage_map == PlayerMap)
                {
                    shipStuck = targetMapComp.ShipsOnMap.Keys.Where(s => targetMapComp.ShipsOnMap[s].IsStuckAndNotAssisted() && targetMapComp.ShipsOnMap[s].BuildingCount > 4).ToList();
                }
                else
                {
                    shipStuck = targetMapComp.ShipsOnMap.Keys.ToList();
                }
                //remove all salvages in that map
                foreach (int ship_index in shipStuck)
                {
                    ShipInteriorMod2.RemoveShipOrArea(salvage_map, ship_index, null, salvage_map != PlayerMap, false);
                }
                Log.Message($"map recycle rate removed: {salvage_map.Parent.Label}: {map_recycle_rate[salvage_map]}");
                map_recycle_rate.Remove(salvage_map);
            }
        }


        protected void SetToLoadEverything()
        {
            for (int i = 0; i < transferables.Count; i++)
            {
                transferables[i].AdjustTo(transferables[i].GetMaximumToTransfer());
            }
            CountToTransferChanged = true;
        }
        protected void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            AddItemsToTransferables();
            items_transfer = new TransferableOneWayWidget(transferables, null, null, "FormCaravanColonyThingCountTip".Translate(), drawMass: true, IgnorePawnsInventoryMode.Ignore,
                availableMassGetter: delegate
                {
                    if (ModSettings_SaveOurShipPatch.energy_per_kg > 0)
                        return Math.Min(MassCapacity - MassUsage, (CurrentStoredEnergy - EnergyUsage) / ModSettings_SaveOurShipPatch.energy_per_kg);
                    else
                        return MassCapacity - MassUsage;
                },
                drawMarketValue: true);
            CountToTransferChanged = true;
        }
        protected Dictionary<ThingDef, int> CountAllBuildingCost()
        {
            ShipMapComp targetMapComp = salvage_map.GetComponent<ShipMapComp>();
            // if is player map, count building costs in salvage only
            List<SpaceShipCache> ship_cache_list;
            if (salvage_map == PlayerMap)
            {
                ship_cache_list = targetMapComp.ShipsOnMap.Values.Where(c => c.IsStuckAndNotAssisted() && c.BuildingCount > 4).ToList();
            }
            else
            {
                ship_cache_list = targetMapComp.ShipsOnMap.Values.ToList();
            }
            //Count all buildings costs in target ship map
            HashSet<IntVec3> area = new HashSet<IntVec3>();
            foreach (var ship_cache in ship_cache_list)
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
            return cost_list;
        }
        protected virtual void AddItemsToTransferables()
        {
            var building_cost = CountAllBuildingCost();
            foreach (var thing_def_count in building_cost)
            {// multiplied by recyling rate
                int stack_count = ((int)Math.Round(thing_def_count.Value * map_recycle_rate[salvage_map]));
                if (stack_count > 0)
                {
                    Thing thing = ThingMaker.MakeThing(thing_def_count.Key);
                    thing.stackCount = stack_count;
                    var transferable_one_way = new TransferableOneWay();
                    transferable_one_way.things.Add(thing);
                    transferables.Add(transferable_one_way);
                }
            }
        }
        public override bool CausesMessageBackground()
        {
            return true;
        }
    }
}

