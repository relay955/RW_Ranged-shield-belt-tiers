using System;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RangedShieldBeltTiers
{
    [StaticConstructorOnStartup]
    public class Gizmo_EnergyShieldStatus : Gizmo
    {
        public RangedShieldBelt shield;
        private static readonly Texture2D FullShieldBarTex = 
            SolidColorMaterials. NewSolidColorTexture(
                new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyShieldBarTex = 
            SolidColorMaterials .NewSolidColorTexture(Color.clear);
        private StringBuilder labelText = new StringBuilder();

        public Gizmo_EnergyShieldStatus() => this.Order = -100f;

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(
            Vector2 topLeft,
            float maxWidth,
            GizmoRenderParms parms)
        {
            Rect rect1 = new Rect(topLeft.x, topLeft.y,
                                  GetWidth(maxWidth), 75f);
            Rect rect2 = rect1.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect1);
            Rect rect3 = rect2;
            rect3.height = rect1.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, shield.LabelCap);
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;
            float fillPercent = this.shield.Energy / this.shield.EnergyMax;
            Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex,
                           EmptyShieldBarTex, false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect rect5 = rect4;
            string str1 = (shield.Energy * 100f).ToString("F0");
            string str2 = (shield.EnergyMax * 100f).ToString("F0");

            labelText.Clear();
            labelText.Append(str1);
            labelText.Append(" / ");
            labelText.Append(str2);
            if (shield.ticksToRecharge != -1 || shield.ticksToReset != -1)
            {
                labelText.Append(" (");
                labelText.Append(Math.Max((int) (shield.ticksToRecharge * 0.016),
                                 (int) (shield.ticksToReset * 0.016))+1);
                labelText.Append("s)");
            }else if (RangedShieldBeltConfig.shieldBatteryRequired) {
                labelText.Append(" [");
                labelText.Append((int)(shield.remainBatteryTick /
                    (double)shield.ChargeDurationPerBattery * 100));
                labelText.Append("%]");
            }

            string label = labelText.ToString();
            
            Widgets.Label(rect5, label);
            Text.Anchor = TextAnchor.UpperLeft;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}