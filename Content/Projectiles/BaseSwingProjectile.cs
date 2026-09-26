using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public abstract class BaseSwingProjectile : ModProjectile
    {
        protected virtual float SwingArc => 4f;
        protected virtual int SwingSnapPasses => 2;
        protected virtual int BaseRecoveryTime => 10;
        protected virtual float RecoveryLerpSpeed => 0.25f;
        protected virtual float CollisionWidth => 40f;
        protected virtual float SwingStartAngleOffset => 0f;
        protected virtual bool CanCancelRecovery => true;
        protected virtual bool AlternateSwingDirection => false;
        protected virtual int ComboLength => 1;
        protected virtual bool DoubleSwingOnFinalCombo => false;
        protected virtual float DoubleSwingArcScale => 0.4f;
        protected virtual bool ShrinkOnRelease => false;
        protected virtual int ReleaseShrinkTicks => 8;
        protected virtual bool KeepBladeInFront => false;
        protected virtual int? FixedSwingDuration => null;

        protected virtual int TrailLength => 12;
        protected virtual float TrailMaxAlpha => 0.5f;
        protected virtual Color TrailInnerColor => Color.White;
        protected virtual Color TrailOuterColor => new Color(140, 220, 255);
        protected virtual int TrailDecayRate => 1;

        protected virtual SoundStyle SwingSound => SoundID.Item1;

        protected virtual int CollisionSubSteps => 3;
        protected virtual float RotationBlendWindow => 0.12f;

        private int recoveryShrinkTicks = -1;
        private int recoveryShrinkStart = 1;
        private float recoveryShrink = 1f;

        private static readonly Dictionary<(int, int), int> _lastComboStep = new();
        private static readonly Dictionary<(int, int), float> _lastEndRotation = new();
        private static readonly Dictionary<(int, int), List<float>> _lastTrailRotations = new();
        private static readonly Dictionary<(int, int), int> _lastTrailUpdateTick = new();
        private static readonly Dictionary<(int, int), int> _lastComboTime = new();

        protected virtual int TrailStalenessThreshold => 20;

        private (int, int) comboKey;

        protected int comboStep;
        protected int direction;

        protected int swingDuration;
        protected int recoveryTime;
        private bool soundPlayed;
        private float lockedBaseAngle;
        protected float previousRotation;
        protected readonly List<float> _trailRotations = new();

        private float finalSwingAngle = 0f;
        private float blendFromRotation;

        protected void SetSwingBaseAngle(float angle) {
            lockedBaseAngle = angle;
        }

        protected float GetSwingBelowAngle() => lockedBaseAngle - SwingArc * 0.5f + SwingStartAngleOffset;
        protected float GetSwingAboveAngle() => lockedBaseAngle + SwingArc * 0.5f;

        protected float ClampBladeInFront(float angle) {
            return angle;
        }

        protected virtual bool IgnoreDirectionOnCombo2 => false;

        protected virtual float GetSwingStartAngle() {
            int side = Owner.direction;
            bool isDownward = !AlternateSwingDirection || (comboStep % 2 == 0);

            if (IgnoreDirectionOnCombo2) {
                return isDownward ? (lockedBaseAngle - SwingArc * 0.5f) : (lockedBaseAngle + SwingArc * 0.5f);
            }

            if (isDownward) {
                return side > 0 ? (lockedBaseAngle - SwingArc * 0.5f) : (lockedBaseAngle + SwingArc * 0.5f);
            }
            else {
                return side > 0 ? (lockedBaseAngle + SwingArc * 0.5f) : (lockedBaseAngle - SwingArc * 0.5f);
            }
        }

        protected virtual float GetSwingRotation(float percentDone, float eased) {
            int side = Owner.direction;
            bool isDownward = !AlternateSwingDirection || (comboStep % 2 == 0);
            
            float startAngle, endAngle;

            if (IgnoreDirectionOnCombo2) {
                startAngle = isDownward ? (lockedBaseAngle - SwingArc * 0.5f) : (lockedBaseAngle + SwingArc * 0.5f);
                endAngle = isDownward ? (lockedBaseAngle + SwingArc * 0.5f) : (lockedBaseAngle - SwingArc * 0.5f);
                return MathHelper.Lerp(startAngle, endAngle, eased);
            }

            if (isDownward) {
                startAngle = side > 0 ? (lockedBaseAngle - SwingArc * 0.5f) : (lockedBaseAngle + SwingArc * 0.5f);
                endAngle = side > 0 ? (lockedBaseAngle + SwingArc * 0.5f) : (lockedBaseAngle - SwingArc * 0.5f);
            }
            else {
                startAngle = side > 0 ? (lockedBaseAngle + SwingArc * 0.5f) : (lockedBaseAngle - SwingArc * 0.5f);
                endAngle = side > 0 ? (lockedBaseAngle - SwingArc * 0.5f) : (lockedBaseAngle + SwingArc * 0.5f);
            }

            return MathHelper.Lerp(startAngle, endAngle, eased);
        }
        protected virtual float ModifyEasedPercent(float percentDone, float eased) {
            return eased;
        }

        protected Player Owner => Main.player[Projectile.owner];
        public bool IsSwinging => Projectile.timeLeft - recoveryTime > 0;
        protected abstract Texture2D GetDrawTexture();

        public override void SetStaticDefaults() {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 10000;
            DrawHeldProjInFrontOfHeldItemAndArms = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source) {
            soundPlayed = false;
            
            Vector2 mouseDirection = Main.MouseWorld - Owner.MountedCenter;
            lockedBaseAngle = mouseDirection.ToRotation();
            
            direction = mouseDirection.X > 0 ? 1 : -1;
            Projectile.spriteDirection = direction;
            Owner.direction = direction;

            var key = (Owner.whoAmI, Type);
            comboKey = key;

            if (ComboLength > 1) {
                bool comboExpired = _lastComboTime.TryGetValue(key, out int lastTime)
                    && (Main.GameUpdateCount - lastTime) > 150;

                if (comboExpired)
                    _lastComboStep[key] = -1;

                comboStep = (_lastComboStep.GetValueOrDefault(key, -1) + 1) % ComboLength;
                _lastComboStep[key] = comboStep;
                _lastComboTime[key] = (int)Main.GameUpdateCount;
            }
            else {
                comboStep = 0;
            }

            StartSwingTiming();

            Texture2D tex = GetDrawTexture();
            if (tex == null)
                tex = TextureAssets.Item[Owner.HeldItem.type].Value;
            Projectile.width = tex.Width;
            Projectile.height = tex.Height;

            Projectile.rotation = GetSwingStartAngle();
            if (KeepBladeInFront)
                Projectile.rotation = ClampBladeInFront(Projectile.rotation);

            blendFromRotation = _lastEndRotation.TryGetValue(key, out float lastRot) ? lastRot : Projectile.rotation;

            previousRotation = Projectile.rotation;

            _trailRotations.Clear();
            bool trailIsFresh = _lastTrailUpdateTick.TryGetValue(key, out int lastTick)
                && (Main.GameUpdateCount - lastTick) <= TrailStalenessThreshold;

            if (trailIsFresh && _lastTrailRotations.TryGetValue(key, out List<float> savedTrail)) {
                _trailRotations.AddRange(savedTrail);
            }

            if (_trailRotations.Count == 0)
                _trailRotations.Add(Projectile.rotation);
        }

        protected void StartSwingTiming() {
            float attackSpeed = Owner.GetTotalAttackSpeed(Projectile.DamageType);
            if (FixedSwingDuration.HasValue) {
                swingDuration = Math.Max(1, (int)(FixedSwingDuration.Value / attackSpeed));
            }
            else {
                swingDuration = Owner.HeldItem.useTime > 0 ? (int)Math.Max(1, Owner.HeldItem.useTime / attackSpeed) : (Owner.itemAnimationMax > 0 ? Owner.itemAnimationMax : Owner.HeldItem.useAnimation);
            }
            recoveryTime = BaseRecoveryTime == 0 ? 0 : Math.Max(1, (int)(BaseRecoveryTime / attackSpeed));
            Projectile.timeLeft = swingDuration + recoveryTime;
        }

        public override void AI() {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed) {
                Projectile.Kill();
                return;
            }

            if (Projectile.timeLeft <= 1 && BaseRecoveryTime == 0) {
                UpdateHeldArm();
                return;
            }

            if (recoveryShrinkTicks >= 0) {
                if (--recoveryShrinkTicks <= 0) {
                    Projectile.Kill();
                    return;
                }
                float progress = 1f - (recoveryShrinkTicks / (float)recoveryShrinkStart);
                recoveryShrink = 1f - progress;
                Owner.itemAnimation = recoveryShrinkTicks;
                Owner.itemTime = recoveryShrinkTicks;
                for (int r = 0; r < TrailDecayRate && _trailRotations.Count > 0; r++)
                    _trailRotations.RemoveAt(_trailRotations.Count - 1);
                UpdateHeldArm();
                return;
            }

            if (ShrinkOnRelease && !Owner.controlUseItem) {
                recoveryShrinkTicks = ReleaseShrinkTicks;
                recoveryShrinkStart = recoveryShrinkTicks;
            }

            previousRotation = Projectile.rotation;
            int swingTimeLeft = Projectile.timeLeft - recoveryTime;

            if (swingTimeLeft > 0) {
                Owner.direction = direction;
                Projectile.spriteDirection = direction;
                Owner.itemAnimation = 2;
                Owner.itemTime = 2;

                float percentDone = 1f - ((float)swingTimeLeft / swingDuration);
                percentDone = MathHelper.Clamp(percentDone, 0f, 1f);

                if (!soundPlayed && percentDone >= 0.15f) {
                    SoundEngine.PlaySound(SwingSound);
                    soundPlayed = true;
                }

                float eased = 0.5f - 0.5f * (float)Math.Cos(Math.PI * percentDone);
                for (int i = 0; i < SwingSnapPasses; i++) {
                    eased = eased * eased * (3f - 2f * eased);
                }

                float finalEased = ModifyEasedPercent(percentDone, eased);
                float targetRotation = GetSwingRotation(percentDone, finalEased);

                if (RotationBlendWindow > 0f && percentDone < RotationBlendWindow) {
                    float blendT = percentDone / RotationBlendWindow;
                    blendT = blendT * blendT * (3f - 2f * blendT);
                    Projectile.rotation = blendFromRotation.AngleLerp(targetRotation, blendT);
                }
                else {
                    Projectile.rotation = targetRotation;
                }
                
                if (KeepBladeInFront)
                    Projectile.rotation = ClampBladeInFront(Projectile.rotation);

                finalSwingAngle = Projectile.rotation - lockedBaseAngle;

                _trailRotations.Insert(0, Projectile.rotation);
                
                while (_trailRotations.Count > TrailLength) {
                    _trailRotations.RemoveAt(_trailRotations.Count - 1);
                }

                _lastEndRotation[comboKey] = Projectile.rotation;
                _lastTrailRotations[comboKey] = new List<float>(_trailRotations);
                _lastTrailUpdateTick[comboKey] = (int)Main.GameUpdateCount;

                UpdateHeldArm();
            }
            else {
                Projectile.Kill();
            }
        }
        protected void UpdateHeldArm() {
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);

            if (Owner.gravDir == -1f) {
                Projectile.rotation = 0f - Projectile.rotation;
                armPosition.Y = Owner.Bottom.Y + (Owner.position.Y - armPosition.Y);
            }

            armPosition.Y += Owner.gfxOffY;
            Projectile.Center = armPosition;
            Projectile.scale = Owner.GetAdjustedItemScale(Owner.HeldItem) * recoveryShrink;
            Owner.heldProj = Projectile.whoAmI;

            Owner.itemRotation = Projectile.rotation;
            if (direction < 0) {
                Owner.itemRotation += MathHelper.Pi;
            }
            Owner.itemLocation = Projectile.Center;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = GetDrawTexture();
            if (texture == null) {
                Item heldItem = Owner.HeldItem;
                texture = TextureAssets.Item[heldItem.type].Value;
            }

            Vector2 origin;
            SpriteEffects effects;
            float rotationOffset = MathHelper.ToRadians(45f);

            if (Projectile.spriteDirection > 0) {
                origin = new Vector2(0, texture.Height);
                effects = SpriteEffects.None;
            }
            else {
                origin = new Vector2(texture.Width, texture.Height);
                effects = SpriteEffects.FlipHorizontally;
                rotationOffset = MathHelper.ToRadians(135f);
            }

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float drawRot = Projectile.rotation + rotationOffset;

            DrawLightTrail();

            Main.spriteBatch.Draw(texture, drawPos, null, lightColor,
                drawRot, origin, Projectile.scale, effects, 0);

            PostDraw(texture, drawPos, drawRot, origin, Projectile.scale, effects, lightColor);
            return false;
        }

        protected virtual void DrawLightTrail() {
            Vector2 pivot = Owner.MountedCenter;
            float bladeLength = new Vector2(Projectile.width, Projectile.height).Length() * Projectile.scale * 1.025f;
            
            // แก้ไข: เปลี่ยนค่า startDistance จากเดิมที่ล็อกไว้ที่ 80% (bladeLength * 0.8f) 
            // ให้เหลือเพียง 10% เพื่อให้หางดาบวาดคลุมพื้นที่แผ่ออกมาจากโคนดาบได้ยาวขึ้น
            float startDistance = bladeLength * 0.1f; 
    
        SwordTrailRenderer.Draw(pivot, bladeLength, _trailRotations, TrailMaxAlpha, TrailInnerColor, TrailOuterColor, startDistance);
        }

        protected virtual void PostDraw(Texture2D texture, Vector2 drawPos, float drawRot, Vector2 origin, float scale, SpriteEffects effects, Color lightColor) { }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Projectile.timeLeft <= recoveryTime) return false;
            if (recoveryShrinkTicks >= 0) return false;

            Vector2 start = Owner.MountedCenter;
            float length = new Vector2(Projectile.width, Projectile.height).Length() * Projectile.scale;
            float width = CollisionWidth * Projectile.scale;

            int steps = Math.Max(1, CollisionSubSteps);
            for (int i = 0; i <= steps; i++) {
                float t = i / (float)steps;
                float sampledRotation = previousRotation.AngleLerp(Projectile.rotation, t);
                Vector2 end = start + sampledRotation.ToRotationVector2() * length;

                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref collisionPoint))
                    return true;
            }
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            modifiers.HitDirectionOverride = target.position.X > Owner.MountedCenter.X ? 1 : -1;
        }
    }
}