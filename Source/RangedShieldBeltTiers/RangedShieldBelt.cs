using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
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
    private int lastAbsorbDamageTick = -9999;
    private const float MinDrawSize = 1.2f;
    private const float MaxDrawSize = 1.55f;
    private const float MaxDamagedJitterDist = 0.05f;
    private const int JitterDurationTicks = 8;
    private float EnergyOnReset = 0.01f;
    private float EnergyLossPerDamage = 0.01f;
    private int KeepDisplayingTicks = 1000;
    private float ApparelScorePerEnergyMax = 0.25f;
    private static readonly Material BubbleMat = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent);

    public int HitRechargeCooldown => (def as RangedShieldDef).HitRechargeCooldown;
    public int BrokenRechargeCooldown => (def as RangedShieldDef).BrokenRechargeCooldown;
    
    private float EnergyMax => this.GetStatValue(StatDefOf.EnergyShieldEnergyMax);
    private float EnergyGainPerTick => this.GetStatValue(StatDefOf.EnergyShieldRechargeRate) / 60f;
    public float Energy => energy;
    public ShieldState ShieldState => ticksToReset > 0 ? ShieldState.Resetting : ShieldState.Active;

    private bool ShouldDisplay
    {
      get
      {
        Pawn wearer = this.Wearer;
        return wearer.Spawned &&
               !wearer.Dead && 
               !wearer.Downed && (
                    wearer.InAggroMentalState || 
                    wearer.Drafted ||
                    wearer.Faction.HostileTo(Faction.OfPlayer) && !wearer.IsPrisoner || 
                    Find.TickManager.TicksGame < lastKeepDisplayTick + KeepDisplayingTicks
                );
      }
    }

    public override void ExposeData()
    {
      base.ExposeData();
      Scribe_Values.Look<float>(ref energy, "energy");
      Scribe_Values.Look<int>(ref ticksToReset, "ticksToReset", -1);
      Scribe_Values.Look<int>(ref lastKeepDisplayTick, "lastKeepDisplayTick");
    }

    public override IEnumerable<Gizmo> GetWornGizmos()
    {
      foreach (Gizmo gizmo in base.GetWornGizmos())
      {
        yield return gizmo;
      }
      IEnumerator<Gizmo> enumerator = null;
      if (Find.Selector.SingleSelectedThing == Wearer)
      {
        yield return new Gizmo_EnergyShieldStatus { shield = this };
      }
    }

    public override float GetSpecialApparelScoreOffset() => this.EnergyMax * this.ApparelScorePerEnergyMax;

    public override void Tick()
    {
      base.Tick();
      if (Wearer == null) energy = 0.0f;
      else if (ShieldState == ShieldState.Resetting)
      {
        --ticksToReset;
        if (ticksToReset > 0) return;
        Reset();
      }
      else
      {
        if (ticksToRecharge >= 0)
        {
          --ticksToRecharge;
          return;
        }
        if (ShieldState != ShieldState.Active) return;
        energy += EnergyGainPerTick;
        if ((double) energy <= (double) EnergyMax) return;
        energy = EnergyMax;
      }
    }

    public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
    {
      if (ShieldState != ShieldState.Active) return false;
      if (dinfo.Def == DamageDefOf.EMP)
      {
        energy = 0.0f;
        Break();
        return false;
      }
      if (!dinfo.Def.isRanged && !dinfo.Def.isExplosive) return false;
      energy -= dinfo.Amount * EnergyLossPerDamage;
      
      if ((double) energy < 0.0) Break();
      else AbsorbedDamage(dinfo);
      return true;
    }

    public void KeepDisplaying() => lastKeepDisplayTick = Find.TickManager.TicksGame;

    private void AbsorbedDamage(DamageInfo dinfo)
    {
      SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot((SoundInfo) new TargetInfo(Wearer.Position, Wearer.Map));
      impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
      Vector3 loc = Wearer.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
      float scale = Mathf.Min(10f, (float) (2.0 + (double) dinfo.Amount / 10.0));
      FleckMaker.Static(loc, Wearer.Map, FleckDefOf.ExplosionFlash, scale);
      int num = (int) scale;
      for (int index = 0; index < num; ++index)
        FleckMaker.ThrowDustPuff(loc, Wearer.Map, Rand.Range(0.8f, 1.2f));
      lastAbsorbDamageTick = Find.TickManager.TicksGame;
      ticksToRecharge = HitRechargeCooldown;//
      KeepDisplaying();
    }

    private void Break()
    {
      SoundDefOf.EnergyShield_Broken.PlayOneShot((SoundInfo) new TargetInfo(Wearer.Position, Wearer.Map));
      FleckMaker.Static(Wearer.TrueCenter(), Wearer.Map, FleckDefOf.ExplosionFlash, 12f);
      for (int index = 0; index < 6; ++index)
        FleckMaker.ThrowDustPuff(Wearer.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float) Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), Wearer.Map, Rand.Range(0.8f, 1.2f));
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
          SoundDefOf.EnergyShield_Reset.PlayOneShot((SoundInfo)new TargetInfo(Wearer.Position, Wearer.Map));
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
      float num1 = Mathf.Lerp(MinDrawSize, MaxDrawSize, energy);
      Vector3 drawPos = Wearer.Drawer.DrawPos;
      drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
      int num2 = Find.TickManager.TicksGame - lastAbsorbDamageTick;
      if (num2 < 8)
      {
        float num3 = (float) ((double) (8 - num2) / 8.0 * 0.0500000007450581);
        drawPos += impactAngleVect * num3;
        num1 -= num3;
      }
      float angle = (float) Rand.Range(0, 360);
      Vector3 s = new Vector3(num1, 1f, num1);
      Matrix4x4 matrix = new Matrix4x4();
      matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
      Graphics.DrawMesh(MeshPool.plane10, matrix, BubbleMat, 0);
    }

    public override bool AllowVerbCast(Verb verb) => true;
  }
}
        