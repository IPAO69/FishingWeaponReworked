using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace FishyFishy.UI
{
    [Autoload(Side = ModSide.Client)]
    public sealed class WeaponCooldownUI : ModSystem
    {
        private class CooldownEntry
        {
            public string Key;
            public Texture2D Icon;
            public Texture2D Frame;
            public Texture2D FillBar;
            public Texture2D FullBar;
            public Rectangle FillSource;
            public int CurrentTicks;
            public int MaxTicks;
            public bool Ready => CurrentTicks <= 0;
        }

        private const float DefaultPosX = 50f;
        private const float DefaultPosY = 85f;
        private const float MouseDragEpsilon = 0.05f;
        private const int IconSize = 79;
        private const int IconPadding = 6;
        private const int IconsPerRow = 1;
        private static Vector2? dragOffset;
        private static float posX = DefaultPosX;
        private static float posY = DefaultPosY;
        private static readonly Dictionary<string, CooldownEntry> entries = new();

        public override void OnModLoad()
        {
            posX = DefaultPosX;
            posY = DefaultPosY;
            dragOffset = null;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            posX = tag.TryGet("WeaponCooldownUIPosX", out float x) ? x : DefaultPosX;
            posY = tag.TryGet("WeaponCooldownUIPosY", out float y) ? y : DefaultPosY;
            posX = MathHelper.Clamp(posX, 0f, 100f);
            posY = MathHelper.Clamp(posY, 0f, 100f);
            dragOffset = null;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["WeaponCooldownUIPosX"] = posX;
            tag["WeaponCooldownUIPosY"] = posY;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Entity Health Bars");
            if (idx == -1)
                return;

            layers.Insert(idx++, new LegacyGameInterfaceLayer("FishyFishy: Weapon Cooldowns", DrawLayer, InterfaceScaleType.UI));
        }

        private static bool DrawLayer()
        {
            Draw(Main.spriteBatch);
            return true;
        }

        public override void Unload()
        {
            entries.Clear();
            dragOffset = null;
        }

        public static void SetCooldown(string key, Texture2D icon, int currentTicks, int maxTicks)
        {
            if (currentTicks <= 0)
            {
                entries.Remove(key);
                return;
            }

            if (!entries.TryGetValue(key, out CooldownEntry entry))
            {
                entry = new CooldownEntry { Key = key };
                entries[key] = entry;
            }

            entry.Icon = icon;
            entry.CurrentTicks = currentTicks;
            entry.MaxTicks = Math.Max(maxTicks, 1);
        }

        public static void SetMeter(string key, Texture2D frame, Texture2D fill, Texture2D full, Rectangle fillSource, int currentTicks, int maxTicks)
        {
            if (!entries.TryGetValue(key, out CooldownEntry entry))
            {
                entry = new CooldownEntry { Key = key };
                entries[key] = entry;
            }

            entry.Frame = frame;
            entry.FillBar = fill;
            entry.FullBar = full;
            entry.FillSource = fillSource;
            entry.CurrentTicks = currentTicks;
            entry.MaxTicks = Math.Max(maxTicks, 1);
        }

        public static void ClearCooldown(string key) => entries.Remove(key);

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (entries.Count == 0)
                return;

            float uiScale = Main.UIScale;

            posX = MathHelper.Clamp(posX, 0f, 100f);
            posY = MathHelper.Clamp(posY, 0f, 100f);

            Vector2 anchor = new Vector2(
                (int)(posX * 0.01f * Main.screenWidth),
                (int)(posY * 0.01f * Main.screenHeight));

            int slotSize = (int)((IconSize + IconPadding) * uiScale);
            int drawnIconSize = (int)(IconSize * uiScale);

            var list = entries.Values.ToList();
            int count = list.Count;
            int columns = Math.Min(count, IconsPerRow);
            int rows = (int)Math.Ceiling(count / (float)IconsPerRow);

            int gridWidth = columns * slotSize - (int)(IconPadding * uiScale);
            int gridHeight = rows * slotSize - (int)(IconPadding * uiScale);

            Vector2 groupTopLeft = anchor - new Vector2(gridWidth * 0.5f, gridHeight * 0.5f);
            Rectangle groupHitbox = new Rectangle((int)groupTopLeft.X, (int)groupTopLeft.Y, gridWidth, gridHeight);

            for (int i = 0; i < count; i++)
            {
                CooldownEntry entry = list[i];
                int col = i % IconsPerRow;
                int row = i / IconsPerRow;

                Vector2 slotPos = groupTopLeft + new Vector2(col * slotSize, row * slotSize);
                Rectangle destRect = new Rectangle((int)slotPos.X, (int)slotPos.Y, drawnIconSize, drawnIconSize);

                DrawIcon(spriteBatch, entry, destRect);
            }

            HandleDragging(groupHitbox);
        }

        private static void DrawIcon(SpriteBatch spriteBatch, CooldownEntry entry, Rectangle destRect)
        {
            if (entry.Frame != null)
            {
                DrawMeter(spriteBatch, entry, destRect);
                return;
            }

            if (entry.Icon == null)
                return;

            spriteBatch.Draw(entry.Icon, destRect, Color.White);

            if (entry.Ready)
                return;

            float remainingRatio = MathHelper.Clamp((float)entry.CurrentTicks / entry.MaxTicks, 0f, 1f);
            int overlayHeight = (int)(destRect.Height * remainingRatio);
            Rectangle overlayRect = new Rectangle(destRect.X, destRect.Y, destRect.Width, overlayHeight);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, overlayRect, Color.Black * 0.6f);

            string secondsText = Math.Ceiling(entry.CurrentTicks / 60f).ToString("0");
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(secondsText);
            Vector2 textCenter = new Vector2(destRect.X + destRect.Width * 0.5f, destRect.Y + destRect.Height * 0.5f);
            Utils.DrawBorderString(spriteBatch, secondsText, textCenter - textSize * 0.5f, Color.White);
        }

        private static void DrawMeter(SpriteBatch spriteBatch, CooldownEntry entry, Rectangle destRect)
        {
            float uiScale = Main.UIScale;
            int drawSize = (int)(entry.Frame.Width * uiScale);
            Rectangle frameRect = new Rectangle(
                destRect.X + (destRect.Width - drawSize) / 2,
                destRect.Y + (destRect.Height - drawSize) / 2,
                drawSize, drawSize);

            spriteBatch.Draw(entry.Frame, frameRect, Color.White);

            if (entry.FillBar == null || entry.FillSource.IsEmpty)
                return;

            Rectangle src = entry.FillSource;
            float scale = frameRect.Width / (float)entry.Frame.Width;
            Rectangle strip = new Rectangle(
                (int)(frameRect.X + src.X * scale),
                (int)(frameRect.Y + src.Y * scale),
                (int)Math.Max(1, src.Width * scale),
                (int)Math.Max(1, src.Height * scale));

            if (entry.Ready)
            {
                if (entry.FullBar != null)
                    spriteBatch.Draw(entry.FullBar, strip, src, Color.White);
                return;
            }

            float remainingRatio = MathHelper.Clamp((float)entry.CurrentTicks / entry.MaxTicks, 0f, 1f);
            float fillRatio = 1f - remainingRatio;
            int visibleH = (int)Math.Round(src.Height * fillRatio);
            if (visibleH <= 0)
                return;

            Rectangle sourceRect = new Rectangle(src.X, src.Y + (src.Height - visibleH), src.Width, visibleH);
            int destH = (int)Math.Round(strip.Height * fillRatio);
            Rectangle destRectFill = new Rectangle(strip.X, strip.Y + (strip.Height - destH), strip.Width, destH);
            spriteBatch.Draw(entry.FillBar, destRectFill, sourceRect, Color.White);
        }

        private static void HandleDragging(Rectangle groupHitbox)
        {
            MouseState ms = Mouse.GetState();
            Vector2 mousePos = Main.MouseScreen;
            Rectangle mouseHitbox = new Rectangle((int)mousePos.X, (int)mousePos.Y, 8, 8);

            if (!groupHitbox.Intersects(mouseHitbox))
            {
                if (dragOffset.HasValue && ms.LeftButton == ButtonState.Released)
                    dragOffset = null;
                return;
            }

            Main.LocalPlayer.mouseInterface = true;

            if (ms.LeftButton == ButtonState.Pressed)
            {
                Vector2 anchor = new Vector2(
                    (int)(posX * 0.01f * Main.screenWidth),
                    (int)(posY * 0.01f * Main.screenHeight));

                if (!dragOffset.HasValue)
                    dragOffset = mousePos - anchor;

                Vector2 newAnchor = mousePos - dragOffset.GetValueOrDefault(Vector2.Zero);

                float newPosX = (100f * newAnchor.X) / Main.screenWidth;
                float newPosY = (100f * newAnchor.Y) / Main.screenHeight;

                if (Math.Abs(newPosX - posX) >= MouseDragEpsilon || Math.Abs(newPosY - posY) >= MouseDragEpsilon)
                {
                    posX = newPosX;
                    posY = newPosY;
                }
            }
            else if (dragOffset.HasValue && ms.LeftButton == ButtonState.Released)
            {
                dragOffset = null;
            }
        }
    }
}
