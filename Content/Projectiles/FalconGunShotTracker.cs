using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class FalconGunShotTracker : GlobalProjectile
    {
        private static readonly HashSet<int> FalconShots = new HashSet<int>();

        public static void MarkFalconShot(int whoAmI)
        {
            FalconShots.Add(whoAmI);
        }

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ProjectileID.DD2ExplosiveTrapT2Explosion;
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (FalconShots.Contains(projectile.whoAmI))
            {
                modifiers.HideCombatText();
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (FalconShots.Contains(projectile.whoAmI))
            {
                CombatText.NewText(target.Hitbox, Color.Yellow, $"{hit.Damage}!", true);
            }
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            FalconShots.Remove(projectile.whoAmI);
        }
    }
}
