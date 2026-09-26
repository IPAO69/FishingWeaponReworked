using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System;


namespace FishyFishy.Content.Items
{
    public class OffhandGunDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => drawInfo.drawPlayer.GetModPlayer<OffhandGunPlayer>().gunDrawTimer > 0;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            OffhandGunPlayer gunPlayer = player.GetModPlayer<OffhandGunPlayer>();

            if (gunPlayer.gunDrawTimer <= 0) return;

            Main.instance.LoadItem(ItemID.Boomstick);
            Texture2D texture = TextureAssets.Item[ItemID.Boomstick].Value;
            Vector2 origin = new Vector2(-1f, texture.Height * 0.70f);

            float drawRotation = gunPlayer.gunAngle;
            Vector2 positionOffset = Vector2.Zero;

            if (gunPlayer.isReloading) {
                float percentDone = 1f - (gunPlayer.pendingShotDelay / (float)gunPlayer.pendingShotDelayMax); // 0 → 1
                float eased = 1f - (float)Math.Pow(1f - percentDone, 3); // ease-out cubic: fast start, slow finish

                float totalSpins = 2f; // how many full rotations during the whole reload — tune to taste
                float totalRotation = totalSpins * MathHelper.TwoPi;

                drawRotation = gunPlayer.gunAngle + totalRotation * eased;
            }
            else if (gunPlayer.recoilKickTimer > 0) {
                float kickProgress = gunPlayer.recoilKickTimer / 10f;
                positionOffset = new Vector2(0f, -5f * kickProgress);
            }
            else if (gunPlayer.gunFrozen) {
                drawRotation = gunPlayer.gunAngle;
            }

            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, drawRotation - MathHelper.PiOver2);

            Vector2 handOffset = gunPlayer.gunAngle.ToRotationVector2() * 10f;
            Vector2 drawPos = player.MountedCenter + handOffset - Main.screenPosition + positionOffset;

            float alpha = MathHelper.Clamp(gunPlayer.gunDrawTimer / 5f, 0f, 1f);

            DrawData data = new DrawData(
                texture,
                drawPos,
                null,
                Color.White * alpha,
                drawRotation,
                origin,
                1f,
                player.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None
            );

            drawInfo.DrawDataCache.Add(data);
        }
    }
}