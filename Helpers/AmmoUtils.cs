using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FishyFishy.Helpers
{
    public class AmmoUtils : ModPlayer
    {
        public class Magazine
        {
            public int Ammo;
            public int MaxAmmo;
            public int ReloadTimer;
            public int ReloadTotal;
            public bool IsReloading => ReloadTimer > 0;
        }

        private readonly Dictionary<int, Magazine> magazines = new();

        public override void ResetEffects()
        {
            foreach (Magazine magazine in magazines.Values)
            {
                if (magazine.IsReloading)
                {
                    magazine.ReloadTimer--;
                    if (magazine.ReloadTimer == 0)
                        magazine.Ammo = magazine.MaxAmmo;
                }
            }
        }

        public static AmmoUtils For(Player player) => player.GetModPlayer<AmmoUtils>();

        public int GetAmmo(int itemType) => MagazineFor(itemType).Ammo;

        public int GetMaxAmmo(int itemType) => MagazineFor(itemType).MaxAmmo;

        public bool IsReloading(int itemType) => MagazineFor(itemType).IsReloading;

        public float GetReloadProgress(int itemType)
        {
            Magazine magazine = MagazineFor(itemType);
            return 1f - magazine.ReloadTimer / (float)System.Math.Max(1, magazine.ReloadTotal);
        }

        public bool TryConsumeShot(int itemType, int maxAmmo, int reloadTicks)
        {
            Magazine magazine = MagazineFor(itemType);

            if (magazine.MaxAmmo != maxAmmo)
            {
                bool firstTime = magazine.MaxAmmo == 0;
                magazine.MaxAmmo = maxAmmo;
                if (firstTime)
                    magazine.Ammo = maxAmmo;
                else if (magazine.Ammo > maxAmmo)
                    magazine.Ammo = maxAmmo;
            }

            if (magazine.IsReloading)
                return false;

            if (magazine.Ammo <= 0)
            {
                StartReload(magazine, reloadTicks);
                return false;
            }

            magazine.Ammo--;
            if (magazine.Ammo <= 0)
                StartReload(magazine, reloadTicks);

            return true;
        }

        public void Refill(int itemType, int maxAmmo)
        {
            Magazine magazine = MagazineFor(itemType);
            magazine.MaxAmmo = maxAmmo;
            magazine.Ammo = maxAmmo;
            magazine.ReloadTimer = 0;
        }

        private Magazine MagazineFor(int itemType)
        {
            if (!magazines.TryGetValue(itemType, out Magazine magazine))
            {
                magazine = new Magazine();
                magazines[itemType] = magazine;
            }
            return magazine;
        }

        private static void StartReload(Magazine magazine, int reloadTicks)
        {
            magazine.ReloadTotal = System.Math.Max(1, reloadTicks);
            magazine.ReloadTimer = magazine.ReloadTotal;
        }
    }
}
