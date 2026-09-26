using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using FishyFishy.UI;
using System;

namespace FishyFishy.Content.Projectiles
{
    public class BladeTonguePlayer : ModPlayer
    {
        public int DamageStack = 0;
        public int BuffTimer = 0;
        public bool IsBuffActive => BuffTimer > 0;

        private static Texture2D meterFrame;
        private static Texture2D fillBar;
        private static Texture2D fullBar;
        private static readonly Rectangle FillSource = new Rectangle(32, 5, 7, 35);

        public override void Load()
        {
            meterFrame = ModContent.Request<Texture2D>("FishyFishy/Content/UI/Asset/BladetongueMeter", AssetRequestMode.ImmediateLoad).Value;
            fillBar = ModContent.Request<Texture2D>("FishyFishy/Content/UI/Asset/BladetongueBar", AssetRequestMode.ImmediateLoad).Value;
            fullBar = fillBar;
        }

        public override void Unload()
        {
            meterFrame = null;
            fillBar = null;
            fullBar = null;
        }

        private int lastHeldItemType = 0;
        public int SwingCounter = 0;

        public override void ResetEffects() {
            if (IsBuffActive) {
                BuffTimer--;
                Player.statDefense += 15;
            }
        }

        public override void PostUpdateEquips() {
            if (IsBuffActive && Player.HeldItem.type == ItemID.Bladetongue) {
                Player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
            }
        }

        public override void PostUpdate() {
            if (Player.HeldItem.type != lastHeldItemType) {
                DamageStack = 0;
                BuffTimer = 0;
                SwingCounter = 0;
            }
            lastHeldItemType = Player.HeldItem.type;

            // Meter only shows while the blade tongue is held. Red fill = damage charge
            // (0 -> 3000); full green = buff active.
            if (Player.HeldItem.type == ItemID.Bladetongue) {
                int currentTicks = IsBuffActive ? 0 : Math.Max(0, 3000 - DamageStack);
                WeaponCooldownUI.SetMeter("BladeTongue", meterFrame, fillBar, fullBar, FillSource, currentTicks, 3000);
            }
            else {
                WeaponCooldownUI.ClearCooldown("BladeTongue");
            }

            if (IsBuffActive) {
                Vector2 eyePos = GetEyePosition();

                Lighting.AddLight(eyePos, 1.5f, 1.3f, 0.3f);

                for (int i = 0; i < 2; i++) {
                    Vector2 offset = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-3f, 3f));
                    Dust d = Dust.NewDustPerfect(eyePos + offset, DustID.GoldFlame, Player.velocity * 0.3f, 0, new Color(255, 225, 80), 1.35f);
                    d.noGravity = true;
                }

                if (Main.rand.NextBool(3)) {
                    Dust halo = Dust.NewDustPerfect(eyePos, DustID.GoldFlame, Vector2.Zero, 100, new Color(255, 210, 60), 2.2f);
                    halo.noGravity = true;
                }
            }
        }

        public Vector2 GetEyePosition() {
            Player p = Player;

            int frame = p.bodyFrame.Y / 56;
            if (frame >= Main.OffsetsPlayerHeadgear.Length) {
                frame = 0;
            }
            Vector2 headgearOffset = Main.OffsetsPlayerHeadgear[frame] * p.Directions;
            Vector2 offset = new Vector2(p.width / 2f, p.height / 2f) + headgearOffset + (p.MountedCenter - p.Center);
            p.sitting.GetSittingOffsetInfo(p, out Vector2 sitOffset, out float seatAdjustment);
            offset += sitOffset + new Vector2(0f, seatAdjustment);

            Vector2 eyeLocal = new Vector2(3 * p.direction - (p.direction == 1 ? 1 : 0), -11.5f * p.gravDir);
            return p.position + offset + eyeLocal + Vector2.UnitY * p.gfxOffY;
        }

        public void AddDamageTrack(int damage, bool isBoss) {
            if (IsBuffActive) {
                return;
            }

            if (BossHelpers.IsAnyBossAlive() && !isBoss) {
                return;
            }

            DamageStack += damage;

            if (DamageStack >= 3000) {
                DamageStack = 0;
                BuffTimer = 15 * 60;
                SwingCounter = 0;
                SoundEngine.PlaySound(SoundID.NPCDeath59, Player.Center);
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo) {
            if (IsBuffActive) {
                drawInfo.colorEyes = Color.Yellow;
                drawInfo.colorEyeWhites = Color.Yellow;
            }
        }
    }

    public class BladeTongueEyeGlowLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool IsHeadLayer => false;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            return drawInfo.drawPlayer.GetModPlayer<BladeTonguePlayer>().IsBuffActive && !drawInfo.drawPlayer.dead;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            if (drawInfo.shadow != 0f) {
                return;
            }

            Player player = drawInfo.drawPlayer;
            Vector2 eyePos = player.GetModPlayer<BladeTonguePlayer>().GetEyePosition() - Main.screenPosition;
            Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            Vector2 glowOrigin = glow.Size() / 2f;

            for (int i = -1; i <= 1; i += 2) {
                Vector2 eye = eyePos + new Vector2(i * 4f, 0f);

                drawInfo.DrawDataCache.Add(new DrawData(glow, eye, null, Color.Yellow * 0.35f, 0f, glowOrigin, 0.7f, SpriteEffects.None, 0));
                drawInfo.DrawDataCache.Add(new DrawData(glow, eye, null, Color.Yellow * 0.65f, 0f, glowOrigin, 0.4f, SpriteEffects.None, 0));
                drawInfo.DrawDataCache.Add(new DrawData(glow, eye, null, Color.White * 0.9f, 0f, glowOrigin, 0.22f, SpriteEffects.None, 0));
            }
        }
    }
}