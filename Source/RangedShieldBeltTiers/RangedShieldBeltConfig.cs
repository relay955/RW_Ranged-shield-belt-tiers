using System;
using UnityEngine;
using Verse;

namespace RangedShieldBeltTiers
{
    public class RangedShieldBeltConfig : ModSettings
    {
        public static bool showShieldHitEffectOnly = false;
        public static bool showTacticalShieldBar = true;
        public static float shieldCapacityMultiplier = 1;
        public static float shieldRechargeSpeedMultiplier = 1;
        public static float rechargeWaitTimeOnHit = 1;
        public static float rechargeWaitTimeOnBroken = 1;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showShieldHitEffectOnly, "showShieldHItEffectOnly");
            Scribe_Values.Look(ref showTacticalShieldBar, "showTacticalShieldBar");
            Scribe_Values.Look(ref shieldCapacityMultiplier, "shieldCapacityMultiplier");
            Scribe_Values.Look(ref shieldRechargeSpeedMultiplier, "shieldRechargeSpeedMultiplier");
            Scribe_Values.Look(ref rechargeWaitTimeOnHit, "rechargeWaitTimeOnHit");
            Scribe_Values.Look(ref rechargeWaitTimeOnBroken, "rechargeWaitTimeOnBroken");
            base.ExposeData();
        }
    }

    class RangedShieldBeltMod : Mod
    {
        RangedShieldBeltConfig settings;
        
        public RangedShieldBeltMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RangedShieldBeltConfig>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("ShowShieldHitEffectOnly".Translate(), ref RangedShieldBeltConfig.showShieldHitEffectOnly,
                "ShowShieldHitEffectOnlyDescription".Translate());
            listingStandard.CheckboxLabeled("ShowTacticalShieldBar".Translate(), ref RangedShieldBeltConfig.showTacticalShieldBar,
                "ShowTacticalShieldBarDescription".Translate());
            listingStandard.Gap();
            
            listingStandard.Label("BalanceSettings".Translate(),-1F,"BalanceSettingsDescription".Translate());
            listingStandard.Label("ShieldCapacityMultiplier".Translate(RangedShieldBeltConfig.shieldCapacityMultiplier.ToString()),-1F,
                "ShieldCapacityMultiplierDescription".Translate());
            RangedShieldBeltConfig.shieldCapacityMultiplier = Mathf.Floor(listingStandard.Slider(RangedShieldBeltConfig.shieldCapacityMultiplier, 0.5F,5)*10)/10;
            
            listingStandard.Label("ShieldRechargeSpeedMultiplier".Translate(RangedShieldBeltConfig.shieldRechargeSpeedMultiplier.ToString()),-1F,
                "ShieldRechargeSpeedMultiplierDescription".Translate());
            RangedShieldBeltConfig.shieldRechargeSpeedMultiplier = Mathf.Floor(listingStandard.Slider(RangedShieldBeltConfig.shieldRechargeSpeedMultiplier, 0.5F,5)*10)/10;
            
            listingStandard.Label("RechargeWaitTimeOnHit".Translate(RangedShieldBeltConfig.rechargeWaitTimeOnHit.ToString()),-1F,
                "RechargeWaitTimeOnHitDescription".Translate());
            RangedShieldBeltConfig.rechargeWaitTimeOnHit = Mathf.Floor(listingStandard.Slider(RangedShieldBeltConfig.rechargeWaitTimeOnHit, 0.2F,3)*10)/10;
            
            listingStandard.Label("RechargeWaitTimeOnBroken".Translate(RangedShieldBeltConfig.rechargeWaitTimeOnBroken.ToString()),-1F,
                "RechargeWaitTimeOnBrokenDescription".Translate());
            RangedShieldBeltConfig.rechargeWaitTimeOnBroken = Mathf.Floor(listingStandard.Slider(RangedShieldBeltConfig.rechargeWaitTimeOnBroken, 0.2F,3)*10)/10;
            
            listingStandard.Gap();
            // listingStandard.Label("실드이펙트 설정",-1F,"실드 이펙트입니다.");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Ranged Shield Belt Tiers";
        }
    }
}