using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShipPatch
{
    using static ModSettings_SaveOurShipPatch;
    public class ModSettings_SaveOurShipPatch : ModSettings
    {
        protected static double recycle_rate_min = 0.4;
        public static double RecycleRateMin
        {
            get { return recycle_rate_min; }
            set { recycle_rate_min = Math.Min(value, recycle_rate_max); }
        }
        protected static double recycle_rate_max = 0.5;
        public static double RecycleRateMax
        {
            get { return recycle_rate_max; }
            set { recycle_rate_max = Math.Max(value, recycle_rate_min); }
        }
        public static bool limit_mass_per_bay = false;
        public static int mass_per_bay = 1000;
        public static bool auto_calculate_ticks = false;
        public static int ticks_complete_recycle = 0;
        public static void Reset()
        {
            recycle_rate_min = 0.4;
            recycle_rate_max = 0.5;
            limit_mass_per_bay = false;
            mass_per_bay = 1000;
            auto_calculate_ticks = false;
            ticks_complete_recycle = 0;
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref recycle_rate_min, "recycle_rate_min", 0.4);
            Scribe_Values.Look(ref recycle_rate_max, "recycle_rate_max", 0.5);
            Scribe_Values.Look(ref limit_mass_per_bay, "limit_mass_per_bay", false);
            Scribe_Values.Look(ref mass_per_bay, "mass_per_bay", 1000);
            Scribe_Values.Look(ref auto_calculate_ticks, "auto_calculate_ticks", false);
            Scribe_Values.Look(ref ticks_complete_recycle, "ticks_complete_recycle", 0);
            base.ExposeData();
        }
    }
    public class Mod_SaveOurShipPatch : Mod
    {
        public Mod_SaveOurShipPatch(ModContentPack content) : base(content)
        {
            base.GetSettings<ModSettings_SaveOurShipPatch>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard options = new Listing_Standard();
            options.Begin(inRect);

            options.Label("SaveOurShipPatch.Settings.RecycleRateMin".Translate("0", "1", "0.4", RecycleRateMin.ToString("0.00")), tooltip: "SaveOurShipPatch.Settings.RecycleRate.Desc".Translate());
            RecycleRateMin = options.Slider((float)RecycleRateMin, 0, 1);

            options.Label("SaveOurShipPatch.Settings.RecycleRateMax".Translate("0", "1", "0.5", RecycleRateMax.ToString("0.00")), tooltip: "SaveOurShipPatch.Settings.RecycleRate.Desc".Translate());
            RecycleRateMax = options.Slider((float)RecycleRateMax, 0, 1);

            options.Gap();
            options.Gap();

            options.CheckboxLabeled("SaveOurShipPatch.Settings.LimitMassPerBay".Translate(), ref limit_mass_per_bay, "SaveOurShipPatch.Settings.LimitMassPerBay.Desc".Translate());

            options.Label("SaveOurShipPatch.Settings.MassPerBay".Translate("1000kg"));
            var mass_per_bay_str = mass_per_bay.ToString();
            options.TextFieldNumeric(ref mass_per_bay, ref mass_per_bay_str, 0, int.MaxValue);

            options.Gap();
            options.Gap();

            options.CheckboxLabeled("SaveOurShipPatch.Settings.AutoCalculateTicks".Translate(), ref auto_calculate_ticks, "SaveOurShipPatch.Settings.AutoCalculateTicks.Desc".Translate());

            options.Label("SaveOurShipPatch.Settings.TicksCompleteRecycle".Translate("0"), tooltip: "SaveOurShipPatch.Settings.TicksCompleteRecycle.Desc".Translate());
            var ticks_complete_recycle_str = ticks_complete_recycle.ToString();
            options.TextFieldNumeric(ref ticks_complete_recycle, ref ticks_complete_recycle_str, 0, int.MaxValue);

            options.End();
            float y = inRect.height + Window.CloseButSize.y + 3f;
            float num = inRect.width - Window.CloseButSize.x;
            Rect rect = new Rect(num, y, Window.CloseButSize.x, Window.CloseButSize.y);
            if (Widgets.ButtonText(rect, "SaveOurShipPatch.Settings.ResetSettingsLabel".Translate()))
            {
                ModSettings_SaveOurShipPatch.Reset();
            }
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        public override string SettingsCategory()
        {
            return "SaveOurShipPatch.Settings.ModName".Translate();
        }
    }
}
