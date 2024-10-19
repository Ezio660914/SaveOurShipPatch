using RimWorld;
using SaveOurShip2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace SaveOurShipPatch
{
    public class Dialog_Scoop : Dialog_Recycle
    {
        public Dialog_Scoop(CompShipBaySalvageAdvanced parent_comp, Map salvage_map) : base(parent_comp, salvage_map)
        {
        }
        protected override void AddItemsToTransferables()
        {
            ShipMapComp targetMapComp = salvage_map.GetComponent<ShipMapComp>();
            foreach (IntVec3 vec in salvage_map.AllCells.Where(v => !targetMapComp.MapShipCells.ContainsKey(v)))
            {
                foreach (Thing t in salvage_map.thingGrid.ThingsAt(vec))
                {
                    if (!t.Destroyed && !(t is Building) && t.def != ResourceBank.ThingDefOf.DetachedShipPart)
                    {
                        TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                        if (transferableOneWay == null)
                        {
                            transferableOneWay = new TransferableOneWay();
                            transferables.Add(transferableOneWay);
                        }
                        if (transferableOneWay.things.Contains(t))
                        {
                            Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
                        }
                        else
                        {
                            transferableOneWay.things.Add(t);
                        }
                    }
                }
            }
        }
        protected override void SendTransferablesAndRemoveSalvages()
        {
            ShipMapComp targetMapComp = salvage_map.GetComponent<ShipMapComp>();
            //send all resources to player's map using pod
            var thing_owner = new ThingOwner<Thing>();
            foreach (TransferableOneWay tr in transferables)
            {
                TransferableUtility.Transfer(tr.things, tr.CountToTransfer, delegate (Thing splitPiece, IThingHolder originalHolder)
                {
                    thing_owner.TryAddOrTransfer(splitPiece);
                    if (splitPiece is Pawn pawn && !pawn.Dead)
                    {
                        ShipInteriorMod2.AddPawnToLord(PlayerMap, pawn);
                    }
                });
            }
            ActiveDropPodInfo activeDropPodInfo = new ActiveDropPodInfo
            {
                innerContainer = thing_owner,
                leaveSlag = false
            };
            var drop_point = CompShipBaySalvageAdvanced.drop_pod_target == null ? DropCellFinder.TradeDropSpot(PlayerMap) : CompShipBaySalvageAdvanced.drop_pod_target.Position;
            DropPodUtility.MakeDropPodAt(drop_point, PlayerMap, activeDropPodInfo);
            if (thing_owner.Any)
            {
                string drop_pod_received_text = "";
                foreach (Thing thing in thing_owner)
                {
                    drop_pod_received_text += thing.def.label + ": " + thing.stackCount.ToString() + "\n";
                }
                Find.LetterStack.ReceiveLetter(
                        "DropPodReceivedLabel".Translate(),
                        "DropPodReceivedText".Translate(drop_pod_received_text),
                        LetterDefOf.PositiveEvent, thing_owner.InnerListForReading, null, null, null, null
                        );
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "ScoopDebris".Translate(salvage_map.Parent.Label));
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
        protected override void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate()))
            {
                if (TryAccept())
                {
                    if (transferables.Any(x => x.CountToTransfer > 0))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmScoop".Translate(), delegate
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
    }
}
