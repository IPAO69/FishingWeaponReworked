using FishyFishy.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class AnchorThrownProjectile : ModProjectile
    {
        private Player Owner => Main.player[Projectile.owner];

        private const float AnchorScale = 2f;
        private const float MaxTravelDistance = 4200f;
        private const float ReturnSpeed = 16f;
        private const float KillDistance = 40f;
        private const float SpinSpeed = 0.35f;

        private List<Vector2> trailCache;
        private PrimitiveTrail trail;
        private const int TrailCacheSize = 30;

        private bool returning;
        private bool hasDirectHit;
        private int returnSpriteDir;
        private float returnRotation;

        private static readonly SoundStyle ThrowSound = new("FishyFishy/Content/Sound/AnchorThrown");
        private static readonly SoundStyle HitSound = new("FishyFishy/Content/Sound/AnchorHit");

        public override string Texture => "Terraria/Images/Item_" + ItemID.Anchor;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 1000;
            Projectile.knockBack = 7f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            FaceDirection(Projectile.velocity);
            SoundEngine.PlaySound(ThrowSound, Projectile.Center);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation += SpinSpeed;

            if (!returning)
            {
                Projectile.velocity.Y += 0.3f;
                Projectile.velocity *= 0.995f;

                float traveled = Vector2.Distance(Owner.MountedCenter, Projectile.Center);
                bool offScreen = Projectile.Center.X < Main.screenPosition.X - 200f
                    || Projectile.Center.X > Main.screenPosition.X + Main.screenWidth + 200f
                    || Projectile.Center.Y < Main.screenPosition.Y - 200f
                    || Projectile.Center.Y > Main.screenPosition.Y + Main.screenHeight + 200f;

                if (traveled >= MaxTravelDistance || offScreen)
                {
                    returning = true;
                    Projectile.tileCollide = false;
                    returnSpriteDir = Projectile.spriteDirection;
                    returnRotation = Projectile.rotation;
                    Vector2 burstDir = (Owner.MountedCenter - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.velocity = burstDir * ReturnSpeed;
                }
            }
            else
            {
                Vector2 toOwner = Owner.Center - Projectile.Center;
                float dist = toOwner.Length();

                if (dist < KillDistance)
                {
                    Projectile.Kill();
                    return;
                }

                Vector2 desired = toOwner.SafeNormalize(Vector2.Zero) * ReturnSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.15f);
            }

            if (returning)
            {
                FaceDirection(-Projectile.velocity);
            }
            else
            {
                FaceDirection(Projectile.velocity);
            }

            if (!Main.dedServ)
            {
                trailCache ??= new List<Vector2>();
                trailCache.Add(Projectile.Center);
                while (trailCache.Count > TrailCacheSize)
                    trailCache.RemoveAt(0);
            }
        }

        private static float TrailWidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(14f, 2f, completionRatio);
        }

        private static Color TrailColorFunction(float completionRatio)
        {
            return Color.Lerp(new Color(80, 80, 95), new Color(40, 40, 55), completionRatio) * (0.7f - completionRatio * 0.3f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.dedServ && trailCache != null && trailCache.Count >= 2)
            {
                trail ??= new PrimitiveTrail(TrailCacheSize, TrailWidthFunction, TrailColorFunction);
                List<Vector2> reversed = new List<Vector2>(trailCache);
                reversed.Reverse();
                trail.SetPositionsSmart(reversed, Projectile.Center);
                trail.NextPosition = Projectile.Center + Projectile.velocity;

                Effect effect = FishyFishy.StreakyTrailEffect;
                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(TextureAssets.MagicPixel.Value);
                effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                effect.Parameters["streakScale"].SetValue(1f);

                trail.Render(effect, -Main.screenPosition);
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Draw chain from hand to anchor
            Texture2D chainTexture = TextureAssets.Item[ItemID.Chain].Value;
            Vector2 chainStart = Owner.MountedCenter;
            Vector2 chainEnd = Projectile.Center;
            Vector2 chainDelta = chainEnd - chainStart;
            float chainLen = chainDelta.Length();
            float chainAngle = chainDelta.ToRotation();
            int segmentCount = (int)(chainLen / (chainTexture.Height * 0.8f));

            for (int i = 0; i < segmentCount; i++)
            {
                float t = segmentCount > 1 ? (float)i / (segmentCount - 1) : 0f;
                Vector2 segPos = Vector2.Lerp(chainStart, chainEnd, t) - Main.screenPosition;

                float segRotation = chainAngle + MathHelper.PiOver2;
                Vector2 segOrigin = new Vector2(chainTexture.Width * 0.5f, 0f);
                Main.spriteBatch.Draw(chainTexture, segPos, null, lightColor * 0.9f,
                    segRotation, segOrigin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPos, null, lightColor,
                Projectile.rotation + MathHelper.ToRadians(0f), origin, Projectile.scale * AnchorScale, SpriteEffects.None, 0);

            Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            float pulse = 0.4f + 0.3f * (float)Math.Sin(Main.timeForVisualEffects * 0.1f);
            Color glowColor = returning
                ? new Color(120, 110, 140) * pulse * 0.3f
                : new Color(100, 95, 120) * pulse * 0.2f;
            Main.spriteBatch.Draw(glow, drawPos, null, glowColor,
                Projectile.rotation + MathHelper.ToRadians(0f), glow.Size() * 0.5f, Projectile.scale * AnchorScale * 1.4f, SpriteEffects.None, 0);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float shaftLength = 32f * Projectile.scale * AnchorScale;
            float crossLength = 20f * Projectile.scale * AnchorScale;
            float width = 10f * Projectile.scale;
            Vector2 center = Projectile.Center;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 perp = dir.RotatedBy(MathHelper.PiOver2);

            Vector2 shaftEnd = center + dir * shaftLength;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), center, shaftEnd, width, ref collisionPoint))
                return true;

            Vector2 crossA = center + perp * crossLength;
            Vector2 crossB = center - perp * crossLength;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), crossA, crossB, width, ref collisionPoint))
                return true;

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (hasDirectHit)
            {
                modifiers.FinalDamage *= 0.2f;
                modifiers.Knockback *= 0.5f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(HitSound, target.Center);

            target.velocity = (target.Center - Owner.MountedCenter).SafeNormalize(Vector2.UnitX) * hit.Knockback;

            Player player = Main.player[Projectile.owner];

            if (!hasDirectHit)
            {
                hasDirectHit = true;

                player.GetModPlayer<AnchorShakePlayer>().shakeTimer = 20;
                player.GetModPlayer<AnchorShakePlayer>().shakeIntensity = 30f;

                // AOE explosion: 10 tile radius, damage + knockback scaled by distance
                float explosionRadius = 10f * 16f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.active || npc.friendly || npc.immortal)
                        continue;

                    float dist = Vector2.Distance(Projectile.Center, npc.Center);
                    if (dist > explosionRadius)
                        continue;

                    float knockbackScale = 1f - (dist / explosionRadius);
                    float damageScale = 0.5f + knockbackScale * 0.5f;
                    int explosionDamage = (int)(hit.Damage * damageScale);
                    float explosionKb = hit.Knockback * knockbackScale * 2f;

                    int kbDir = npc.Center.X > Projectile.Center.X ? 1 : -1;
                    npc.SimpleStrikeNPC(explosionDamage, kbDir, crit: false, explosionKb);
                }

                // Spawn explosion ring visuals
                int visualType = ModContent.ProjectileType<AnchorExplosionVisual>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, visualType, 0, 0f, Projectile.owner);
                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(6f, 6f);
                    Dust d = Dust.NewDustDirect(Projectile.Center - Vector2.One * 8, 16, 16, DustID.Torch, vel.X, vel.Y, 150, default, Main.rand.NextFloat(1.2f, 2f));
                    d.noGravity = true;
                    d.velocity *= 1.5f;
                }

                // Visual-only second explosion (80% smaller = 1 tile radius)
                for (int i = 0; i < 15; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(3f, 3f);
                    Dust d = Dust.NewDustDirect(Projectile.Center - Vector2.One * 4, 8, 8, DustID.Iron, vel.X, vel.Y, 120, default, Main.rand.NextFloat(0.8f, 1.4f));
                    d.noGravity = true;
                    d.velocity *= 1.2f;
                }
            }

            returning = true;
            Projectile.tileCollide = false;
            returnSpriteDir = Projectile.spriteDirection;
            returnRotation = Projectile.rotation;
            Projectile.velocity = (Owner.MountedCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * ReturnSpeed;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!returning)
            {
                returning = true;
                Projectile.tileCollide = false;
                returnSpriteDir = Projectile.spriteDirection;
                returnRotation = Projectile.rotation;
                Projectile.velocity = (Owner.MountedCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * ReturnSpeed;
                SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);

                Player player = Main.player[Projectile.owner];
                player.GetModPlayer<AnchorShakePlayer>().shakeTimer = 10;
                player.GetModPlayer<AnchorShakePlayer>().shakeIntensity = 8f;

                return false;
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                Dust d = Dust.NewDustDirect(Projectile.Center - Vector2.One * 8, 16, 16, DustID.Iron, vel.X, vel.Y, 100, default, Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        private void FaceDirection(Vector2 direction)
        {
            Projectile.spriteDirection = direction.X >= 0 ? 1 : -1;
            Projectile.rotation = direction.ToRotation() + MathHelper.ToRadians(0f);
        }
    }
}
