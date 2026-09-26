using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace FishyFishy.Content.Items
{
    public class BloodRainChargeUI : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Entity Health Bars");
            if (idx == -1) return;

            layers.Insert(idx++, new LegacyGameInterfaceLayer("FishyFishy: Blood Rain Charge", DrawBar, InterfaceScaleType.UI));
        }

        private bool DrawBar()
        {
            var player = Main.LocalPlayer;
            if (player == null || !player.active) return true;

            var bp = player.GetModPlayer<BloodRainBowPlayer>();
            if (bp.chargeDamage <= 0f)
                return true;

            float percent = bp.chargeDamage / BloodRainBowPlayer.MaxCharge;
            float barWidth = 30f;
            float barHeight = 6f;

            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f + 60f);
            Vector2 barPos = center - new Vector2(barWidth / 2f, barHeight / 2f);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Main.spriteBatch.Draw(pixel, new Rectangle((int)barPos.X - 2, (int)barPos.Y - 2, (int)barWidth + 4, (int)barHeight + 4), new Color(20, 20, 20) * 0.9f);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)barWidth, (int)barHeight), new Color(50, 50, 50) * 0.8f);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)(barWidth * percent), (int)barHeight),
                percent >= 1f ? Color.Red : Color.Lerp(Color.Green, Color.OrangeRed, percent));

            return true;
        }
    }
}
