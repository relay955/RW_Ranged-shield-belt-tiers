using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace RangedShieldBeltTiers
{
  [StaticConstructorOnStartup]
  public class RangedShieldBelt : Apparel
  {
    public int ticksToRecharge = -1;
    
    private float energy;
    public int ticksToReset = -1;
    private int lastKeepDisplayTick = -9999;
    private Vector3 impactAngleVect;
    private float impactAngle;
    private int lastAbsorbDamageTick = -9999;
    private const float MinDrawSize = 1.55f;
    private const float MaxDrawSize = 1.55f;
    private const float MaxDamagedJitterDist = 0.05f;
    private const int JitterDurationTicks = 70; 
    private float EnergyOnReset = 0.01f;
    private float EnergyLossPerDamage = 0.01f;
    private int KeepDisplayingTicks = 1000;
    private float ApparelScorePerEnergyMax = 0.25f;
    private Material BubbleMat = null;
    private Material BubbleMatAngle = null;
    private Material EmptyShieldBar = null;
    private Material FullShieldBar = null;
    public int remainBatteryTick = 0;//1개의 배터리로 지속되는 남은 tick
    private CompReloadable _compReloadable;

    public RangedShieldBelt() {
      _compReloadable = this.TryGetComp<CompReloadable>();
    }

    public int HitRechargeCooldown => 
      (int) (this.GetStatValue(StatDef.Named("HitRechargeCooldown"))*60*
             RangedShieldBeltConfig.rechargeWaitTimeOnHit);
    public int BrokenRechargeCooldown => 
      (int) (this.GetStatValue(StatDef.Named("BrokenRechargeCooldown"))*60*
             RangedShieldBeltConfig.rechargeWaitTimeOnBroken);
    public int ChargeDurationPerBattery =>
      (int)this.GetStatValue(StatDef.Named("ChargeDurationPerBattery")) * 60;

    public int EmpResist =>
      (int)this.GetStatValue(StatDef.Named("EMPResist"));
    
    public int Reinforce =>
      (int)this.GetStatValue(StatDef.Named("Reinforce"));
    
    public float EnergyMax => 
      this.GetStatValue(StatDefOf.EnergyShieldEnergyMax)*
      RangedShieldBeltConfig.shieldCapacityMultiplier;
    private float EnergyGainPerTick => 
      this.GetStatValue(StatDefOf.EnergyShieldRechargeRate) / 60f *
      RangedShieldBeltConfig.shieldRechargeSpeedMultiplier;
    public float Energy => energy;
    public ShieldState ShieldState => ticksToReset > 0 ? 
      ShieldState.Resetting : ShieldState.Active;

    private bool ShouldDisplay
    {
      get
      {
        Pawn wearer = Wearer;
        return wearer.Spawned &&
               !wearer.Dead && 
               !wearer.Downed && (
                  wearer.InAggroMentalState || 
                  wearer.Drafted ||
                  wearer.Faction.HostileTo(Faction.OfPlayer) &&
                  !wearer.IsPrisoner || 
                  Find.TickManager.TicksGame < lastKeepDisplayTick + KeepDisplayingTicks
                );
      }
    }

    public override void ExposeData()
    {
      base.ExposeData();
      Scribe_Values.Look(ref energy, "energy");
      Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
      Scribe_Values.Look(ref lastKeepDisplayTick, "lastKeepDisplayTick");
      Scribe_Values.Look(ref remainBatteryTick, "remainBatteryTick");
      
    }

    public override IEnumerable<Gizmo> GetWornGizmos()
    {
      foreach (Gizmo gizmo in base.GetWornGizmos())
      {
        yield return gizmo;
      }
      if (Find.Selector.SingleSelectedThing == Wearer)
      {
        yield return new Gizmo_EnergyShieldStatus { shield = this };
      }
    }

    public override float GetSpecialApparelScoreOffset() => 
      this.EnergyMax * this.ApparelScorePerEnergyMax;

    public override void Tick()
    {
      base.Tick();
      if (remainBatteryTick <= 0) {
        var comp = this.TryGetComp<CompReloadable>();
        if (!comp.CanBeUsed) return;
        remainBatteryTick = ChargeDurationPerBattery;
        comp.UsedOnce();
      }
      if (remainBatteryTick <= 0) return;

      if (Wearer == null) energy = 0.0f;
      else if (ShieldState == ShieldState.Resetting)
      {
        --ticksToReset;
        if (ticksToReset > 0) return;
        Reset();
      }
      else if (ticksToRecharge >= 0)
      {
          --ticksToRecharge;
      }
      else
      {
        if (ShieldState != ShieldState.Active) return;
        if ((double)energy < (double)EnergyMax) {
          energy += EnergyGainPerTick;
          remainBatteryTick -= 1;
        } else {
          energy = EnergyMax;
        }
      }
    }

    public override bool CheckPreAbsorbDamage(DamageInfo dinfo) {
      var damage = dinfo.Amount * EnergyLossPerDamage;
      if (ShieldState != ShieldState.Active) return false;
      if (dinfo.Def == DamageDefOf.EMP)
      {
        if (EmpResist > 1) {
          damage = EmpResist*EnergyLossPerDamage;
        } else {
          energy = 0.0f;
          Break();
          return false;
        }
      }

      if (!(RangedShieldBeltConfig.affectMeleeDamage  || dinfo.Def.isRanged) &&
          !dinfo.Def.isExplosive) return false;
      if (Reinforce > 1) damage = Math.Min(Reinforce*EnergyLossPerDamage, damage);
      energy -= damage;
      
      if (energy < 0.0) Break();
      else AbsorbedDamage(dinfo);
      return true;
    }

    public void KeepDisplaying() => lastKeepDisplayTick = Find.TickManager.TicksGame;

    private void AbsorbedDamage(DamageInfo dinfo)
    {
      SoundDefOf.EnergyShield_AbsorbDamage
        .PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
      impactAngle = dinfo.Angle;
      impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
      Vector3 loc = Wearer.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
      float scale = Mathf.Min(10f, (float) (2.0 + dinfo.Amount / 10.0));
      FleckMaker.Static(loc, Wearer.Map, FleckDefOf.ExplosionFlash, scale);
      lastAbsorbDamageTick = Find.TickManager.TicksGame;
      ticksToRecharge = HitRechargeCooldown;//
      KeepDisplaying();
    }

    private void Break()
    {
      SoundDefOf.EnergyShield_Broken
        .PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
      FleckMaker.Static(Wearer.TrueCenter(), Wearer.Map, 
                        FleckDefOf.ExplosionFlash, 12f);
      for (int index = 0; index < 6; ++index)
        FleckMaker.ThrowDustPuff(Wearer.TrueCenter() + 
                                 Vector3Utility.HorizontalVectorFromAngle(
                                   Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f),
                                   Wearer.Map, Rand.Range(0.8f, 1.2f)
                                 );
      energy = 0.0f;
      ticksToReset = BrokenRechargeCooldown;
      ticksToRecharge = -1;
    }

    private void Reset()
    {
      if (!(energy > 0))
      {
        if (Wearer.Spawned)
        {
          SoundDefOf.EnergyShield_Reset
            .PlayOneShot(new TargetInfo(Wearer.Position, Wearer.Map));
          FleckMaker.ThrowLightningGlow(Wearer.TrueCenter(), Wearer.Map, 3f);
        }
        energy = EnergyOnReset;
      }
      ticksToReset = -1;
    }

    public override void DrawWornExtras()
    {
      if (ShieldState != ShieldState.Active || !ShouldDisplay)
        return;
      //initialize material
      if (BubbleMat == null)
      {
        if (RangedShieldBeltConfig.shieldEffect == ShieldEffect.Default)
          BubbleMat = new Material(MaterialPool
            .MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent));
        else if(RangedShieldBeltConfig.shieldEffect == ShieldEffect.Hexagon)
          BubbleMat = new Material(MaterialPool
            .MatFrom("HexagonShield", ShaderDatabase.Transparent));
      }
      if (BubbleMatAngle == null)
      {
        if (RangedShieldBeltConfig.shieldEffect == ShieldEffect.Default)
          BubbleMatAngle = new Material(MaterialPool
            .MatFrom("ShieldBubbleAngle", ShaderDatabase.Transparent));
        else if (RangedShieldBeltConfig.shieldEffect == ShieldEffect.Hexagon)
          BubbleMatAngle = new Material(MaterialPool
            .MatFrom("HexagonShieldAngle", ShaderDatabase.Transparent));
      }
      if (EmptyShieldBar == null)
      {
        EmptyShieldBar = new Material(MaterialPool
          .MatFrom("EmptyShieldBar", ShaderDatabase.Transparent));
        EmptyShieldBar.color = new Color(1, 1, 1, 0.7F);
      }
      if (FullShieldBar == null)
      {
        FullShieldBar = new Material(MaterialPool
          .MatFrom("FullShieldBar", ShaderDatabase.Transparent));
        FullShieldBar.color = new Color(1, 1, 1, 0.7F);
      }
      
      Vector3 drawPos = Wearer.Drawer.DrawPos;
      drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
      Vector3 s = new Vector3(MaxDrawSize, 1f, MaxDrawSize);
      
      //draw shield hit effect
      int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
      if (num2 < JitterDurationTicks)
      {
        float intensity = (JitterDurationTicks - num2) / (float) JitterDurationTicks;
        BubbleMatAngle.color = new Color(1, 1, 1, intensity);
        Matrix4x4 matrix2 = new Matrix4x4();
        matrix2.SetTRS(drawPos, Quaternion.AngleAxis(impactAngle,Vector3.up), s);
        Graphics.DrawMesh(MeshPool.plane10,matrix2,BubbleMatAngle,0);
      }

      float leftEnergyPercent = energy / EnergyMax;
      
      //draw shield bubble
      if (!RangedShieldBeltConfig.showShieldHitEffectOnly)
      {
        float angle = 0;
        if(RangedShieldBeltConfig.shieldEffect == ShieldEffect.Default) angle = Rand.Range(0, 360);
        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
        BubbleMat.color = new Color(1, 1, 1, Mathf.Lerp(0.3F, 1, leftEnergyPercent));
        Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
      }

      //draw shield UI
      if (RangedShieldBeltConfig.showTacticalShieldBar)
      {
        Matrix4x4 shieldUIMatrix = new Matrix4x4();
        Vector3 shieldUiSize = new Vector3(1f, 0, 0.1f);
        shieldUIMatrix.SetTRS(drawPos + new Vector3(0, 0, 0.85f), Quaternion.AngleAxis(0, Vector3.up), shieldUiSize);
        Graphics.DrawMesh(MeshPool.plane10, shieldUIMatrix, EmptyShieldBar, 0);

        Matrix4x4 shieldFullUIMatrix = new Matrix4x4();
        Vector3 shieldFullUiSize = new Vector3(leftEnergyPercent * 0.96F, 0, 0.07f);
        shieldFullUIMatrix.SetTRS(drawPos + new Vector3(-(1 - leftEnergyPercent) * 0.48F, 0.1f, 0.85f),
          Quaternion.AngleAxis(0, Vector3.up), shieldFullUiSize);
        Graphics.DrawMesh(MeshPool.plane10, shieldFullUIMatrix, FullShieldBar, 0);
      }
    }

    public override bool AllowVerbCast(Verb verb) => true;
  }
}
        