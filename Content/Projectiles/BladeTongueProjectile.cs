using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace FishyFishy.Content.Projectiles
{
    public class BladeTongueProjectile : BaseSwingProjectile
    {   
        protected override bool IgnoreDirectionOnCombo2 => true;
        private float auraFade = 0f;
        private static readonly SoundStyle BladeTongueHitSound = new SoundStyle("FishyFishy/Content/Sound/BladeTongueHitSound");
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;
        protected override bool AlternateSwingDirection => true; 
        protected override int ComboLength => 2;
        protected override bool DoubleSwingOnFinalCombo => false; 

        protected override float ModifyEasedPercent(float percentDone, float eased) {
            return (float)Math.Sin(percentDone * Math.PI * 0.5f);
        }
        
        protected override int TrailLength => Owner.GetModPlayer<BladeTonguePlayer>().IsBuffActive ? 6 : 12;
        protected override int TrailDecayRate => Owner.GetModPlayer<BladeTonguePlayer>().IsBuffActive ? 4 : 1;
        
        protected override float TrailMaxAlpha => 0.8f;

        protected override int BaseRecoveryTime => 12;
        protected override bool ShrinkOnRelease => true;
        protected override bool KeepBladeInFront => false; 

        protected override float DoubleSwingArcScale => 1f;
        protected override float SwingArc => 4.2f;
        protected override Texture2D GetDrawTexture() => TextureAssets.Item[ItemID.Bladetongue].Value;
        
        protected override Color TrailInnerColor => new Color(255, 220, 80);
        protected override Color TrailOuterColor => new Color(180, 140, 20);

        protected override SoundStyle SwingSound => new SoundStyle("FishyFishy/Content/Sound/SwordUseSound");

        public override void AI() {
            base.AI();
        }

        public override void OnSpawn(IEntitySource source) {
            base.OnSpawn(source);

            var modPlayer = Owner.GetModPlayer<BladeTonguePlayer>();
            if (modPlayer.IsBuffActive && Main.myPlayer == Owner.whoAmI) {
                modPlayer.SwingCounter++;
                if (modPlayer.SwingCounter % 4 == 3) {
                    FireIchorBeams(2);
                }
                else if (modPlayer.SwingCounter % 4 == 0) {
                    FireIchorBeams(3);
                }
            }
        }

        private void FireIchorBeams(int count) {
            Vector2 aim = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);
            for (int i = 0; i < count; i++) {
                float angle = count == 1 ? 0f : MathHelper.Lerp(-0.25f, 0.25f, i / (float)(count - 1));
                Vector2 dir = aim.RotatedBy(angle);
                Projectile.NewProjectile(
                    Owner.GetSource_ItemUse(Owner.HeldItem),
                    Owner.Center + dir * 40f,
                    dir * 14f,
                    ModContent.ProjectileType<BladeTongueIchorBeam>(),
                    (int)(Projectile.damage * 0.75f),
                    1f,
                    Owner.whoAmI
                );
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            var modPlayer = Owner.GetModPlayer<BladeTonguePlayer>();
            modPlayer.AddDamageTrack(damageDone, target.boss);
            
            SoundEngine.PlaySound(BladeTongueHitSound, target.Center);
            target.AddBuff(BuffID.Ichor, 300);

            if (modPlayer.IsBuffActive) {
                target.StrikeNPC(hit);
            }

            Vector2 dir = (target.Center - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            float baseAngle = dir.ToRotation();
            
            for (int i = 0; i < 18; i++) {
                float t = i / 17f;
                float angle = baseAngle + MathHelper.Lerp(-1.2f, 1.2f, t);
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(4f, 9f);
                Dust d = Dust.NewDustDirect(target.Center, 0, 0, DustID.GoldFlame, vel.X, vel.Y, 100, default, 1.4f);
                d.noGravity = true;
                d.velocity = vel;
            }
        }

       
        public override bool PreDraw(ref Color lightColor) {
            return base.PreDraw(ref lightColor);
        }

        protected override void PostDraw(Texture2D texture, Vector2 drawPos, float drawRot, Vector2 origin, float scale, SpriteEffects effects, Color lightColor) {
            bool buffActive = Owner.GetModPlayer<BladeTonguePlayer>().IsBuffActive;
            if (buffActive)
                auraFade = MathHelper.Clamp(auraFade + 0.08f, 0f, 1f);
            else
                auraFade = MathHelper.Clamp(auraFade - 0.08f, 0f, 1f);

            if (!buffActive || auraFade <= 0.01f)
                return;

            Texture2D ghost = BladetongueOutline.Get(texture);
            int pad = BladetongueOutline.Padding;

            Vector2 ghostOrigin = effects == SpriteEffects.None
                ? new Vector2(pad, ghost.Height - pad)
                : new Vector2(ghost.Width - pad, ghost.Height - pad);

            Color auraColor = new Color(255, 220, 80, 255) * 0.25f * auraFade;
            float radius = 0.5f * auraFade;
            Vector2 ghostScale = new Vector2(scale, scale); // uniform — no more vertical stretch distortion

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);
            for (int i = 0; i < 25; i++) {
                Vector2 offset = (MathHelper.TwoPi * i / 25f).ToRotationVector2() * radius;
                Main.spriteBatch.Draw(ghost, drawPos + offset, null, auraColor, drawRot, ghostOrigin, ghostScale, effects, 0f);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);
        }
    }

    internal static class BladetongueOutline
    {
        private static Texture2D _outline;
        private static bool _done;
        public static int Padding { get; private set; }

        public static Texture2D Get(Texture2D source)
        {
            if (!_done)
            {
                Generate(source);
                _done = true;
            }
            return _outline;
        }

        private static void Generate(Texture2D srcTex)
        {
            int w = srcTex.Width, h = srcTex.Height;
            Color[] srcData = new Color[w * h];
            srcTex.GetData(srcData);

            const int radius = 2;
            // Pad the canvas so the dilation ring has room to grow past the sprite's
            // own edges — this is what was clipping the blade tip before.
            int pad = radius + 1;
            int outW = w + pad * 2;
            int outH = h + pad * 2;
            Padding = pad;

            Color[] outData = new Color[outW * outH];

            for (int y = 0; y < outH; y++)
            {
                for (int x = 0; x < outW; x++)
                {
                    int sx = x - pad;
                    int sy = y - pad;
                    bool isSolid = sx >= 0 && sy >= 0 && sx < w && sy < h && srcData[sy * w + sx].A > 10;
                    if (isSolid)
                    {
                        outData[y * outW + x] = Color.Transparent;
                        continue;
                    }

                    bool nearSolid = false;
                    for (int dy = -radius; dy <= radius && !nearSolid; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = sx + dx, ny = sy + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                                continue;
                            if (srcData[ny * w + nx].A > 10)
                            {
                                nearSolid = true;
                                break;
                            }
                        }
                    }

                    outData[y * outW + x] = nearSolid ? Color.White : Color.Transparent;
                }
            }

            _outline = new Texture2D(Main.graphics.GraphicsDevice, outW, outH);
            _outline.SetData(outData);
        }
    }
}