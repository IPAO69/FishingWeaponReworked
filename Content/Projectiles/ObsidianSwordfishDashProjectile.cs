using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FishyFishy.Content.Items;
using FishyFishy.Primitive;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class ObsidianSwordfishDashProjectile : ModProjectile
    {
        // Bull Rush stats
        public const float MinChargeTime = 15f;
        public const float MaxChargeTime = 45f;
        public const float MaxChargeDistance = 720f; // 45 blocks
        public const float MaxChargeDamageMult = 6f;
        public const float PiercingDamageMult = 0.6f;
        public const float DashDuration = 21f;

        // Stab stats (used while the dash is on cooldown)
        public const float StabDistance = 110f;
        public const float StabDuration = 12f;

        // How far in front of the player the spear is held while dashing/stabbing
        public const float SpearReach = 22f;

        public Player Owner => Main.player[Projectile.owner];
        public ref float Charge => ref Projectile.ai[0];
        public ref float DashTime => ref Projectile.ai[1];
        public Vector2 DashDestination = Vector2.Zero;
        public bool IsStab;

        private PrimitiveTrail dashTrail;
        private List<Vector2> playerTrailCache;
        private const int PlayerTrailCacheSize = 20;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            if (Owner is null || Owner.dead || Owner.HeldItem.type != ItemID.ObsidianSwordfish)
                Projectile.Kill();

            Owner.heldProj = Projectile.whoAmI;

            // Dashing/stabbing behavior
            if (DashDestination != Vector2.Zero)
            {
                // Detach the owner from any mounts/hooks
                if (Owner.mount != null)
                    Owner.mount.Dismount(Owner);
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.owner != Owner.whoAmI || proj.aiStyle != ProjAIStyleID.Hook)
                        continue;
                    proj.Kill();
                }

                DashTime++;
                if (IsStab)
                {
                    // Heavy stab: the player stays in place while the spear thrusts forward
                    Vector2 aimDir = (DashDestination - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity = aimDir * 20f;
                    Projectile.Center = Owner.Center;
                    if (DashTime > StabDuration || Collision.SolidCollision(Owner.Center, 1, 1, false))
                        Projectile.Kill();

                    Owner.ChangeDir(Projectile.direction);
                    SpawnAngleParticles();
                    return;
                }

                // Set the movement of the player
                Projectile.velocity = (DashDestination - Projectile.Center).SafeNormalize(Vector2.UnitX) * MaxChargeDistance * Charge / MaxChargeTime / DashDuration;

                // Kill the projectile once the dash is over, or it clearly detects a solid tile
                if (Vector2.Distance(Projectile.Center, DashDestination) < 4f || DashTime > DashDuration || Collision.SolidCollision(Projectile.Center, 1, 1, false))
                    Projectile.Kill();

                // Actually set the owner center if there's no issues
                Owner.Center = Projectile.Center;
                Owner.ChangeDir(Projectile.direction);
                RecordPlayerPath();
                return;
            }

            // Set the center of the projectile
            Projectile.Center = Owner.MountedCenter;

            ObsidianSwordfishPlayer dashPlayer = Owner.GetModPlayer<ObsidianSwordfishPlayer>();

            // Charge up
            if (Charge < MaxChargeTime)
                Charge++;

            // Attack upon releasing the button
            if (!Owner.channel)
            {
                if (dashPlayer.DashCooldown > 0)
                    Stab();
                else if (Charge >= MinChargeTime)
                    Attack();
                else
                    Projectile.Kill();
            }
        }

        public void Attack()
        {
            ObsidianSwordfishPlayer dashPlayer = Owner.GetModPlayer<ObsidianSwordfishPlayer>();

            // Perform the dash if able to
            if (Charge >= MinChargeTime && !Owner.noItems && !Owner.CCed && dashPlayer.DashCooldown <= 0)
            {
                // Set the destination in which the dash is supposed to be
                Vector2 intendedDestination = Projectile.Center + (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX) * MaxChargeDistance * Charge / MaxChargeTime;

                // Has to be within world bounds
                if (intendedDestination.X >= 660f && intendedDestination.Y >= 660f && intendedDestination.X <= Main.maxTilesX * 16f - 680f && intendedDestination.Y <= Main.maxTilesY * 16f - 680f)
                {
                    // 20 second cooldown before the next dash
                    dashPlayer.DashCooldown = 60 * 20;

                    // Give immunity frames
                    Owner.immune = true;
                    Owner.immuneNoBlink = true;
                    Owner.immuneTime = (int)DashDuration;
                    for (int k = 0; k < Owner.hurtCooldowns.Length; k++)
                        Owner.hurtCooldowns[k] = Owner.immuneTime;

                    DashDestination = intendedDestination;
                    Projectile.damage = (int)(Projectile.damage * MaxChargeDamageMult * Charge / MaxChargeTime);
                    Projectile.position -= new Vector2(50f, 50f);
                    Projectile.width += 100;
                    Projectile.height += 100;
                    Projectile.tileCollide = true;
                    return;
                }
            }
            // Otherwise, kill the projectile
            Projectile.Kill();
        }

        // Charged stab used while the dash is on cooldown
        public void Stab()
        {
            if (!Owner.noItems && !Owner.CCed)
            {
                Vector2 aimDir = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 intendedDestination = Projectile.Center + aimDir * StabDistance;

                // Short immunity so the thrust doesn't trade hits
                Owner.immune = true;
                Owner.immuneNoBlink = true;
                Owner.immuneTime = (int)StabDuration;
                for (int k = 0; k < Owner.hurtCooldowns.Length; k++)
                    Owner.hurtCooldowns[k] = Owner.immuneTime;

                IsStab = true;
                DashDestination = intendedDestination;
                Projectile.damage = (int)(Projectile.damage * (1.5f + 2.5f * Charge / MaxChargeTime));
                Projectile.position -= new Vector2(50f, 50f);
                Projectile.width += 100;
                Projectile.height += 100;
                Projectile.tileCollide = true;
                return;
            }
            Projectile.Kill();
        }

        // Record the player's current position so the trail can follow them
        private void RecordPlayerPath()
        {
            playerTrailCache ??= new List<Vector2>();
            playerTrailCache.Add(Owner.Center);
            while (playerTrailCache.Count > PlayerTrailCacheSize)
                playerTrailCache.RemoveAt(0);
        }

        // Sharp-angle particles: the vertex sits at the swordfish tip and two rays open forward
        private void SpawnAngleParticles()
        {
            Vector2 aimDir = (Projectile.velocity.LengthSquared() > 0f ? Projectile.velocity : Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Vector2 tip = Projectile.Center + aimDir * 50f;

            for (int side = -1; side <= 1; side += 2)
            {
                // Flipped horizontally: the rays sweep back from the tip so the wedge points in the stab direction
                for (int j = 0; j < 2; j++)
                {
                    Vector2 rayDir = aimDir.RotatedBy(MathHelper.Pi + MathHelper.ToRadians(14f) * side);
                    float dist = Main.rand.NextFloat(20f, 100f);
                    Dust d = Dust.NewDustPerfect(tip + rayDir * dist, DustID.Torch, rayDir * Main.rand.NextFloat(1f, 4f), 0, new Color(255, 170, 70), Main.rand.NextFloat(0.9f, 1.4f));
                    d.noGravity = true;
                }
            }
        }

        // Preventing unintended collisions with the floor
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            width = height = 32;
            return true;
        }

        // Bull Rush damage hitboxes (slightly larger than the player)
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 center = Owner.Center;
            float radius = 90f;
            Vector2 closest = new Vector2(
                MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(center, closest) <= radius * radius;
        }

        // Can only deal damage while dashing
        public override bool? CanDamage() => DashTime > 0 ? base.CanDamage() : false;

        // Spear is held the same while charging and dashing, with a streaky trail following it
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("Terraria/Images/Item_" + ItemID.ObsidianSwordfish).Value;
            Rectangle rect = tex.Frame();
            Vector2 origin = rect.Size() * 0.5f;

            float progress = Charge / MaxChargeTime;
            Color tint = Color.Lerp(Color.White, Color.Red, progress);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            // Charge aims at the mouse; dash aims along the dash velocity
            float rotation = (DashTime > 0f ? Projectile.velocity.ToRotation() : (Main.MouseWorld - Projectile.Center).ToRotation()) + MathHelper.PiOver4;
            // Hold the spear out in front while dashing
            if (DashTime > 0f && Projectile.velocity.LengthSquared() > 0f)
                pos += Projectile.velocity.SafeNormalize(Vector2.UnitX) * SpearReach;
            Vector2 offset = Vector2.Zero;

            // Streaky trail following the player while dashing
            if (DashTime > 0f && playerTrailCache != null)
            {
                Effect trailEffect = FishyFishy.StreakyTrailEffect;
                trailEffect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                trailEffect.Parameters["verticalStretch"].SetValue(0.5f);
                trailEffect.Parameters["repeats"].SetValue(2f);
                trailEffect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                trailEffect.Parameters["overlayOpacity"].SetValue(0.5f);
                trailEffect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/FireTrail").Value);
                trailEffect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                trailEffect.Parameters["streakScale"].SetValue(1f);

                dashTrail ??= new PrimitiveTrail(PlayerTrailCacheSize, WidthFunction, ColorFunction);
                dashTrail.SetPositionsSmart(playerTrailCache, Owner.Center);
                dashTrail.NextPosition = Owner.Center;
                dashTrail.Render(trailEffect, -Main.screenPosition);
            }

            // Shake the sprite only while charging, not while the spear is out front (dash/stab)
            if (DashTime <= 0f && Charge >= MaxChargeTime)
            {
                offset = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)) * 3f;
                rotation += Main.rand.NextFloat(-0.05f, 0.05f);
            }

            Vector2 drawPos = pos + offset;

            // Glowing outline (white -> red) around the swordfish
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Vector2[] outlineOffsets =
            {
                new Vector2(2f, 0f), new Vector2(-2f, 0f), new Vector2(0f, 2f), new Vector2(0f, -2f),
                new Vector2(1.4f, 1.4f), new Vector2(-1.4f, 1.4f), new Vector2(1.4f, -1.4f), new Vector2(-1.4f, -1.4f)
            };
            foreach (Vector2 dir in outlineOffsets)
                Main.spriteBatch.Draw(tex, drawPos + dir, rect, tint * 0.8f, rotation, origin, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(tex, drawPos, rect, lightColor, rotation, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }

        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = Utils.GetLerpValue(0.2f, 1f, completionRatio, true) * Projectile.Opacity;
            return Color.Lerp(Color.DarkOrange, Color.Red, completionRatio) * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio) => 12f;

        // Hits have diminishing damage
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage = (int)(Projectile.damage * PiercingDamageMult);
            SwordFishProjectile.RegisterMeleeHit(target.whoAmI);

            // A fully charged release pops all fish stuck on the hit target
            if (Charge >= MaxChargeTime)
                SwordFishProjectile.PopStuckFishOnTarget(target.whoAmI);
        }
    }
}
