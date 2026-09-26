using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.DataStructures;
using System.Collections.Generic;
using Terraria.GameContent;
using ReLogic.Content;
using FishyFishy.Content.Items;
using FishyFishy.Primitive;

namespace FishyFishy.Content.Projectiles
{
    public class SwordFishProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.Swordfish;

        private static readonly SoundStyle PopSound = new SoundStyle("FishyFishy/Content/Sound/SwordfishPop");
        private static readonly SoundStyle ThrownHitSound = new SoundStyle("FishyFishy/Content/Sound/SwordfishThrownHit");

        public int MeleeHitsToTrigger = 10;
        private const int DashDelay = 60;
        private const float DashSpeed = 24f;
        public int MaxStuckFish = 5;
        public string SpriteTexture = "Terraria/Images/Item_" + ItemID.Swordfish;
        public bool IsObsidian = false;
        private float popDamageBonus = 1f;

        // Pop impact: 2x the original fish width (50) as the area damage radius
        public const float PopExplosionRadius = 100f;
        private const int PopTrailCacheSize = 12;
        private const float PopVisualDuration = 40f;
        private PrimitiveTrail popTrail;
        private List<Vector2> popTrailCache;
        private PrimitiveTrail popRingTrail;
        private float PopVisualTimer = 0f;
        private Vector2 PopVisualCenter;
        private static float PopRingProgress = 1f;
        private int popBaseDamage = 0;
        public int MeleeHits = 0;

        private static readonly Dictionary<int, List<int>> StuckFishByTarget = new();

        private enum State
        {
            Flying = 0,
            Stuck = 1,
            Popped = 2,
            Dashing = 3
        }

        private State CurrentState
        {
            get => (State)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public bool IsStickingToTarget
        {
            get => CurrentState == State.Stuck;
            set => CurrentState = value ? State.Stuck : State.Flying;
        }

        public int TargetWhoI
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public int Timer
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        public float StickTimer
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public Vector2 StuckOffset
        {
            get => new Vector2(Projectile.localAI[1], Projectile.localAI[2]);
            set
            {
                Projectile.localAI[1] = value.X;
                Projectile.localAI[2] = value.Y;
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 27;
            Projectile.aiStyle = 0;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.penetrate = 2;
            Projectile.knockBack = 0f;
        }

        private const int GravityDelay = 45;
        public override void AI()
        {
            UpdateAlpha();
            switch (CurrentState)
            {
                case State.Flying:
                    NormalAI();
                    break;
                case State.Stuck:
                    StickAI();
                    break;
                case State.Popped:
                    PoppedAI();
                    break;
                case State.Dashing:
                    DashAI();
                    break;
            }
        }

        public void NormalAI()
        {
            Timer++;
            if (Timer > GravityDelay)
            {
                Timer = GravityDelay;
                Projectile.velocity.X *= 0.98f;
                Projectile.velocity.Y += 0.35f;
            }
            FaceDirection(Projectile.velocity);

            SpawnWaterTrail();
        }

        private void SpawnWaterTrail()
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
            if (dir == Vector2.Zero)
            {
                return;
            }

            Vector2 spawn = Projectile.Center - dir * (Projectile.width * 0.4f);

            if (IsObsidian)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (!Main.rand.NextBool(2))
                    {
                        continue;
                    }

                    Dust d = Dust.NewDustDirect(spawn, 0, 0, DustID.Torch, 0f, 0f, 0, new Color(255, 110, 40), Main.rand.NextFloat(1.2f, 1.7f));
                    d.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f) - Projectile.velocity * 0.15f;
                    d.noGravity = true;
                }

                if (Main.rand.NextBool(2))
                {
                    Dust b = Dust.NewDustDirect(spawn, 0, 0, DustID.Lava, 0f, 0f, 0, new Color(255, 160, 60), Main.rand.NextFloat(0.9f, 1.3f));
                    b.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f) - Projectile.velocity * 0.12f;
                    b.noGravity = true;
                }
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                if (!Main.rand.NextBool(2))
                {
                    continue;
                }

                Dust d = Dust.NewDustDirect(spawn, 0, 0, DustID.Water, 0f, 0f, 0, Color.LightBlue, Main.rand.NextFloat(1.2f, 1.7f));
                d.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f) - Projectile.velocity * 0.15f;
                d.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Dust b = Dust.NewDustDirect(spawn, 0, 0, DustID.VenomStaff, 0f, 0f, 0, Color.LightBlue, Main.rand.NextFloat(0.9f, 1.3f));
                b.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f) - Projectile.velocity * 0.12f;
                b.noGravity = true;
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 direction = Main.MouseWorld - owner.Center;
            FaceDirection(direction);
        }

        private const int StickTime = 60 * 15;
        public void StickAI()
        {
            Projectile.tileCollide = false;
            StickTimer += 1f;

            NPC target = TargetNPC();
            if (target == null || StickTimer >= StickTime)
            {
                Projectile.Kill();
                return;
            }

            Vector2 offset = StuckOffset;
            Projectile.Center = target.Center + offset;
            Projectile.gfxOffY = target.gfxOffY;

            float progress = GetStuckProgress();
            Color glowColor = Color.Lerp(Color.White, Color.Red, progress);
            Lighting.AddLight(GetEyePosition(), glowColor.ToVector3() * (0.3f + progress * 0.7f));
        }

        public Vector2 GetEyePosition()
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            bool flip = Projectile.spriteDirection == -1;
            Vector2 eyeFrac = new Vector2(0.5f, 0.20f);
            Vector2 offset = (new Vector2(flip ? 1f - eyeFrac.X : eyeFrac.X, eyeFrac.Y) - new Vector2(0.5f)) * new Vector2(tex.Width, tex.Height);
            return Projectile.Center + offset.RotatedBy(Projectile.rotation) * Projectile.scale;
        }

        public float GetStuckProgress()
        {
            return MathHelper.Clamp(MeleeHits / (float)MeleeHitsToTrigger, 0f, 1f);
        }

        public void PoppedAI()
        {
            NPC target = TargetNPC();
            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            Vector2 toTarget = target.Center - Projectile.Center;
            FaceDirection(toTarget);
            Projectile.velocity *= 0.92f;

            Timer--;
            if (Timer <= 0)
            {
                CurrentState = State.Dashing;
                Projectile.friendly = true;
                Projectile.tileCollide = true;
                popBaseDamage = Projectile.damage;
                Projectile.damage = (int)(Projectile.damage * popDamageBonus);
                Projectile.velocity = toTarget.SafeNormalize(Vector2.Zero) * DashSpeed;
                Projectile.timeLeft = 180;

                // Clear local immunity so the dash hit can deal damage again
                // (the stick hit already used up this projectile's single hit on the target)
                for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
                    Projectile.localNPCImmunity[i] = 0;
            }
        }

        public void DashAI()
        {
            // Record the pop dash path so the orange trail can follow it
            popTrailCache ??= new List<Vector2>();
            popTrailCache.Add(Projectile.Center);
            while (popTrailCache.Count > PopTrailCacheSize)
                popTrailCache.RemoveAt(0);

            NPC target = TargetNPC();
            if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * DashSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.1f);
            }
            FaceDirection(Projectile.velocity);

            SpawnWaterTrail();
        }

        private static Color PopTrailColorFunction(float completionRatio) => Color.Lerp(Color.OrangeRed, Color.Orange, completionRatio) * (1f - completionRatio * 0.4f);

        private static float PopTrailWidthFunction(float completionRatio) => 14f * (0.4f + 0.6f * completionRatio);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (CurrentState == State.Dashing)
            {
                modifiers.HideCombatText();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CurrentState == State.Flying)
            {
                StickToTarget(target);
            }
            else if (CurrentState == State.Dashing)
            {
                if (popBaseDamage > 0)
                {
                    CombatText.NewText(target.Hitbox, Color.Yellow, $"{hit.Damage}!", true);
                }
                Projectile.Kill();
            }
        }

        private static void DoPopExplosion(int targetNpcIndex, int baseDamage, float bonus)
        {
            if (targetNpcIndex < 0 || targetNpcIndex >= Main.maxNPCs || !Main.npc[targetNpcIndex].active)
                return;

            Vector2 center = Main.npc[targetNpcIndex].Center;

            // Area damage around the target, 2x the original fish width
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                if (Vector2.Distance(npc.Center, center) <= PopExplosionRadius)
                {
                    NPC.HitInfo info = new NPC.HitInfo
                    {
                        Damage = (int)(baseDamage * bonus),
                        HitDirection = npc.Center.X < center.X ? -1 : 1,
                        Knockback = 0f,
                        DamageType = DamageClass.Melee
                    };
                    npc.StrikeNPC(info);
                }
            }

            if (Main.dedServ)
                return;

            // Smoke cloud billowing out from the pop
            for (int i = 0; i < 18; i++)
            {
                Vector2 pos = center + Main.rand.NextVector2Circular(PopExplosionRadius * 0.8f, PopExplosionRadius * 0.8f);
                Dust d = Dust.NewDustDirect(pos, 0, 0, DustID.Smoke, 0f, 0f, 120, default, Main.rand.NextFloat(1.6f, 2.6f));
                d.velocity = (pos - center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 6f) + new Vector2(0f, -1.5f);
            }

            // A couple of orange embers for punch
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustDirect(center, 0, 0, DustID.Torch, 0f, 0f, 0, new Color(255, 140, 50), Main.rand.NextFloat(1f, 1.8f));
                d.velocity = Main.rand.NextVector2Circular(6f, 6f);
                d.noGravity = true;
            }
        }

        private void StickToTarget(NPC target)
        {
            if (!StuckFishByTarget.TryGetValue(target.whoAmI, out List<int> list))
            {
                list = new List<int>();
                StuckFishByTarget[target.whoAmI] = list;
            }

            // Keep at most MaxStuckFish stuck: kill the oldest extra fish
            while (list.Count >= MaxStuckFish)
            {
                int oldest = list[0];
                list.RemoveAt(0);
                Projectile old = Main.projectile[oldest];
                if (old.active && old.type == Type)
                    old.Kill();
            }

            CurrentState = State.Stuck;
            TargetWhoI = target.whoAmI;
            Timer = 0;
            StickTimer = 0f;
            StuckOffset = Projectile.Center - target.Center;
            list.Add(Projectile.whoAmI);

            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 10000;
            Projectile.netUpdate = true;
        }

        public static void RegisterMeleeHit(int npcIndex)
        {
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return;
            if (!Main.npc[npcIndex].active)
                return;

            if (!StuckFishByTarget.TryGetValue(npcIndex, out List<int> list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                Projectile p = Main.projectile[list[i]];
                if (!p.active || !(p.ModProjectile is SwordFishProjectile fish))
                {
                    list.RemoveAt(i);
                    continue;
                }

                fish.MeleeHits++;
            }

            bool allCharged = true;
            for (int i = 0; i < list.Count; i++)
            {
                Projectile p = Main.projectile[list[i]];
                if (!p.active || !(p.ModProjectile is SwordFishProjectile fish))
                {
                    allCharged = false;
                    break;
                }

                if (fish.MeleeHits < fish.MeleeHitsToTrigger)
                {
                    allCharged = false;
                    break;
                }
            }

            if (list.Count > 0 && allCharged)
            {
                PopFishInList(list, npcIndex);
                StuckFishByTarget.Remove(npcIndex);
            }
        }

        private static void PopFishInList(List<int> list, int targetNpcIndex)
        {
            int count = list.Count;
            int poppedCount = 0;
            int ownerWhoAmI = -1;
            int baseDamage = 0;
            float bonus = 1f + 0.1f * count;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Projectile p = Main.projectile[list[i]];
                if (!p.active || !(p.ModProjectile is SwordFishProjectile fish))
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (ownerWhoAmI < 0)
                    ownerWhoAmI = p.owner;
                if (baseDamage <= 0)
                    baseDamage = fish.Projectile.damage;
                poppedCount++;

                if (poppedCount == 1)
                {
                    fish.PopVisualTimer = PopVisualDuration;
                    fish.PopVisualCenter = Main.npc[targetNpcIndex].Center;
                }

                fish.CurrentState = State.Popped;
                fish.popDamageBonus = bonus;
                fish.Timer = DashDelay;
                fish.Projectile.friendly = false;
                fish.Projectile.tileCollide = false;
                fish.Projectile.velocity = (fish.Projectile.Center - Main.npc[targetNpcIndex].Center).SafeNormalize(Vector2.UnitX) * 3f;
                fish.Projectile.timeLeft = 10000;
                SoundEngine.PlaySound(PopSound, fish.Projectile.Center);
            }
            list.Clear();

            // The pop itself is the explosion
            if (baseDamage > 0)
                DoPopExplosion(targetNpcIndex, baseDamage, bonus);

            // Popping the full 10-stack instantly readies the Obsidian Swordfish dash again
            if (poppedCount >= 10 && ownerWhoAmI >= 0 && ownerWhoAmI < Main.maxPlayers)
            {
                Player owner = Main.player[ownerWhoAmI];
                if (owner != null && owner.active && owner.GetModPlayer<ObsidianSwordfishPlayer>() is { } dashPlayer)
                    dashPlayer.DashCooldown = 0;
            }
        }

        public static void PopAllStuckFish()
        {
            foreach (KeyValuePair<int, List<int>> kvp in StuckFishByTarget)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                    PopFishInList(kvp.Value, kvp.Key);
            }
            StuckFishByTarget.Clear();
        }

        public static void PopStuckFishOnTarget(int npcIndex)
        {
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return;

            if (StuckFishByTarget.TryGetValue(npcIndex, out List<int> list) && list != null && list.Count > 0)
            {
                PopFishInList(list, npcIndex);
                StuckFishByTarget.Remove(npcIndex);
            }
        }

        public static void CleanUpTarget(int npcIndex)
        {
            StuckFishByTarget.Remove(npcIndex);
        }

        private void FaceDirection(Vector2 direction)
        {
            Projectile.spriteDirection = direction.X >= 0 ? 1 : -1;
            float offset = MathHelper.ToRadians(45f);
            if (Projectile.spriteDirection == 1)
            {
                Projectile.rotation = direction.ToRotation() + offset;
            }
            else
            {
                Projectile.rotation = direction.ToRotation() - offset + MathHelper.ToRadians(180f);
            }
        }

        private NPC TargetNPC()
        {
            int index = TargetWhoI;
            if (index < 0 || index >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[index];
            return npc.active ? npc : null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Pop explosion visual: flash + expanding orange ring
            if (PopVisualTimer > 0f)
            {
                PopVisualTimer--;
                DrawPopExplosion(PopVisualCenter, 1f - PopVisualTimer / PopVisualDuration);
            }

            // Orange ichor-bolt style trail following the pop dash
            if (CurrentState == State.Dashing && popTrailCache != null)
            {
                popTrail ??= new PrimitiveTrail(PopTrailCacheSize, PopTrailWidthFunction, PopTrailColorFunction);
                Effect effect = FishyFishy.StreakyTrailEffect;
                effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
                effect.Parameters["verticalStretch"].SetValue(0.5f);
                effect.Parameters["repeats"].SetValue(4f);
                effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
                effect.Parameters["overlayOpacity"].SetValue(0.5f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/FireTrail").Value);
                effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
                effect.Parameters["streakScale"].SetValue(1f);

                popTrail.SetPositionsSmart(popTrailCache, Projectile.Center);
                popTrail.NextPosition = Projectile.Center + Projectile.velocity;
                popTrail.Render(effect, -Main.screenPosition);
            }

            if (CurrentState == State.Stuck)
            {
                float progress = GetStuckProgress();
                Color tint = Color.Lerp(Color.White, Color.Red, progress);

                // The charge spark travels through 3 corners of the target's hitbox (Obsidian only)
                Vector2 eyePos;
                if (IsObsidian)
                {
                    NPC target = TargetNPC();
                    Vector2 sparkPos = Projectile.Center;
                    if (target != null)
                    {
                        Rectangle hitbox = target.Hitbox;
                        Vector2 corner1 = new Vector2(hitbox.Left, hitbox.Top);
                        Vector2 corner2 = new Vector2(hitbox.Right, hitbox.Top);
                        Vector2 corner3 = new Vector2(hitbox.Right, hitbox.Bottom);
                        if (progress < 0.5f)
                            sparkPos = Vector2.Lerp(corner1, corner2, progress / 0.5f);
                        else
                            sparkPos = Vector2.Lerp(corner2, corner3, (progress - 0.5f) / 0.5f);
                    }
                    eyePos = sparkPos - Main.screenPosition;
                }
                else
                {
                    eyePos = GetEyePosition() - Main.screenPosition;
                }
                float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);

                Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
                Main.spriteBatch.Draw(glow, eyePos, null, tint * (0.55f + 0.35f * pulse), 0f, glow.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, eyePos, null, Color.White * (0.2f + 0.8f * progress) * (0.7f + 0.3f * pulse), 0f, glow.Size() * 0.5f, 0.25f, SpriteEffects.None, 0f);
            }

            Texture2D tex = ModContent.Request<Texture2D>(SpriteTexture).Value;
            Rectangle rect = tex.Frame();
            Vector2 origin = rect.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.spriteBatch.Draw(tex, pos, rect, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0f);
            return false;
        }

        private void DrawPopExplosion(Vector2 center, float progress)
        {
            float flash = 1f - progress;
            Texture2D glow = TextureAssets.Extra[ExtrasID.ThePerfectGlow].Value;
            Main.spriteBatch.Draw(glow, center - Main.screenPosition, null, Color.OrangeRed * (flash * 0.9f), 0f, glow.Size() * 0.5f, 0.5f + progress * 2.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, center - Main.screenPosition, null, Color.White * (flash * 0.7f), 0f, glow.Size() * 0.5f, 0.35f + progress * 1.5f, SpriteEffects.None, 0f);

            PopRingProgress = flash;
            float radius = 16f + progress * PopExplosionRadius;
            Vector2[] ring = new Vector2[16];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = MathHelper.TwoPi * i / ring.Length;
                ring[i] = center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * radius;
            }

            popRingTrail ??= new PrimitiveTrail(16, PopRingWidthFunction, PopRingColorFunction);
            popRingTrail.Positions = ring;
            popRingTrail.NextPosition = ring[0] + (ring[0] - ring[ring.Length - 1]);

            Effect effect = FishyFishy.StreakyTrailEffect;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(4f);
            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);
            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("FishyFishy/Content/Noise/FireTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(TextureAssets.MagicPixel.Value);
            effect.Parameters["streakScale"].SetValue(1f);
            popRingTrail.Render(effect, -Main.screenPosition);
        }

        private static float PopRingWidthFunction(float factorAlongTrail) => 8f * (0.4f + 0.6f * PopRingProgress);

        private static Color PopRingColorFunction(float factorAlongTrail) => Color.Lerp(Color.OrangeRed, Color.Orange, factorAlongTrail) * (0.85f * PopRingProgress);

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (CurrentState == State.Stuck)
            {
                int npcIndex = TargetWhoI;
                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                {
                    if (Main.npc[npcIndex].behindTiles)
                    {
                        behindNPCsAndTiles.Add(index);
                    }
                    else
                    {
                        behindNPCs.Add(index);
                    }
                    return;
                }
            }
            behindProjectiles.Add(index);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (CurrentState == State.Dashing)
            {
                Projectile.Kill();
                return false;
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default, Main.rand.NextFloat(1.2f, 2f));
                d.velocity = Main.rand.NextVector2Circular(5f, 5f) - Projectile.velocity * 0.2f;
                d.noGravity = true;
            }

            // Mini version of the spear's pop effect when the thrown fish dies
            for (int i = 0; i < 32; i++)
            {
                Vector2 rayDir = Main.rand.NextVector2Unit();
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, rayDir * Main.rand.NextFloat(2f, 7f), 0, new Color(255, 170, 70), Main.rand.NextFloat(1.1f, 1.8f));
                d.noGravity = true;
            }
            for (int i = 0; i < 6; i++)
            {
                Vector2 rayDir = Main.rand.NextVector2Unit();
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, rayDir * Main.rand.NextFloat(4f, 8f), 0, Color.White, Main.rand.NextFloat(1.5f, 2f));
                d.noGravity = true;
            }
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Smoke, 0f, 0f, 100, default, Main.rand.NextFloat(1.6f, 2.4f));
                d.velocity = Main.rand.NextVector2Circular(5f, 5f);
                d.noGravity = true;
            }

            if (CurrentState == State.Popped || CurrentState == State.Dashing)
            {
                SoundEngine.PlaySound(ThrownHitSound, Projectile.Center);
            }

            if (CurrentState == State.Stuck)
            {
                if (StuckFishByTarget.TryGetValue(TargetWhoI, out List<int> list))
                {
                    list.Remove(Projectile.whoAmI);
                    if (list.Count == 0)
                        StuckFishByTarget.Remove(TargetWhoI);
                }
            }
        }

        public static void ClearStuckFish()
        {
            StuckFishByTarget.Clear();
        }

        private const int AlphaFadeInSpeed = 25;
        private void UpdateAlpha()
        {
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= AlphaFadeInSpeed;
            }

            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
        }
    }
}