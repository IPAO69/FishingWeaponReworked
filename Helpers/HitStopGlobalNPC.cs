using FishyFishy.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace FishyFishy.Common.GlobalNPCs
{
    public class HitStopGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private Vector2 storedVelocity;
        private bool frozen;

        // Runs AFTER the NPC's AI has computed its new velocity, so overriding it here
        // actually sticks instead of getting overwritten by the AI on the next tick.
        public override void PostAI(NPC npc)
        {
            if (HitStopSystem.Active)
            {
                if (!frozen)
                {
                    storedVelocity = npc.velocity;
                    frozen = true;
                }
                npc.velocity = Vector2.Zero;
            }
            else if (frozen)
            {
                npc.velocity = storedVelocity;
                frozen = false;
            }
        }
    }
}