using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace RangedShieldBeltTiers
{
    public class RangedShieldDef : ThingDef
    {
        public int HitRechargeCooldown;
        public int BrokenRechargeCooldown;
        public int ChargeDurationPerBattery;
    }
}