using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishyFishy.Content.Projectiles
{
    public class SwordFishMeleeTracker : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == ProjectileID.Swordfish || projectile.type == ProjectileID.ObsidianSwordfish;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            SwordFishProjectile.RegisterMeleeHit(target.whoAmI);
        }
    }

    public class SwordFishNPCCleanup : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            SwordFishProjectile.CleanUpTarget(npc.whoAmI);
        }
    }
}
