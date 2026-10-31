using System.Linq;
using AugaUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    // The look of the enemy health bars. Applied once to the templates of Auga's EnemyHud (m_baseHud, m_baseHudBoss)
    // in the EnemyHud.Awake prefix, so every hud vanilla clones from them already has it. Every vanilla name stays:
    // EnemyHud.ShowHud finds "Health/health_fast", "Health/health_slow" and "Name" by path, and StarLevelSystem finds
    // the bars, the stars and the backgrounds by the same names.
    public static class EnemyHudBars
    {
        // An ordinary enemy's bar, about 40% thinner than the prefab's 9.
        private const float EnemyBarHeight = 5.5f;
        // How far the flat background behind an ordinary enemy's bar reaches past it on each side.
        private const float EnemyBarFrame = 2f;

        // The boss bar is drawn with the player health bar's sprites, sliced with the player bar's pixels-per-unit
        // multiplier, which makes one sprite pixel one unit across. Measured on those sprites at half height: the
        // inside of the slanted frame starts FrameInset units in from either end, and the fill sprite's slanted tip
        // ends FillTip units short of the right edge of its rect.
        private const float SpriteScale = 2f;
        private const float FrameInset = 25f;
        private const float FillTip = 21f;
        // The slow bar's tip stays this far behind the fast bar's when both show the same health, or the edge of the
        // fast bar's slanted tip lets a thin line of the slow bar's colour through.
        private const float SlowFillLag = 1.5f;
        // The tick sprite drawn at its own width slants like the frame, at any bar height.
        private const float TickWidth = 24f;
        private static readonly float[] TickFractions = { 0.25f, 0.5f, 0.75f };
        // How long the divider lines beside a boss's name are.
        private const float BossNameLineLength = 60f;

        // The EnemyHud these templates belong to, so a setting change only touches huds Auga built.
        private static EnemyHud _styledHud;

        private class PlayerBarLook
        {
            public Image Background;
            public Image Border;
            public Image Fill;
            public Image Mask;
            public Image Tick;
        }

        public static void Apply(EnemyHud enemyHud)
        {
            _styledHud = enemyHud;
            if (enemyHud.m_baseHud != null)
            {
                ShortenEnemyBar(enemyHud.m_baseHud.transform);
                PlaceStars(enemyHud.m_baseHud.transform);
            }

            if (enemyHud.m_baseHudBoss != null)
            {
                var look = FindPlayerBarLook();
                if (look != null)
                    StyleBossBar(enemyHud.m_baseHudBoss.transform, look);
                else
                    Auga.LogWarning("The player health bar was not found in the HUD prefab, so the boss bar keeps its flat look.");
                AddBossNameDivider(enemyHud.m_baseHudBoss.transform);
            }
        }

        // [EnemyHud] StarsBelowHealthBar changed: move the stars of the template and of every hud already shown.
        public static void ApplyStarSetting()
        {
            var enemyHud = EnemyHud.instance;
            if (enemyHud == null || enemyHud != _styledHud)
                return;
            if (enemyHud.m_baseHud != null)
                PlaceStars(enemyHud.m_baseHud.transform);
            foreach (var hud in enemyHud.m_huds.Values)
            {
                if (hud?.m_gui == null || hud.m_isMount || hud.m_character == null || hud.m_character.IsBoss() || hud.m_character.IsPlayer())
                    continue;
                PlaceStars(hud.m_gui.transform);
            }
        }

        // The bar keeps its top edge, so the gap to the name above it stays the same.
        private static void ShortenEnemyBar(Transform hud)
        {
            var health = hud.Find("Health") as RectTransform;
            if (health == null)
                return;
            // anchored and pivoted at its bottom edge
            var top = health.anchoredPosition.y + health.sizeDelta.y;
            health.sizeDelta = new Vector2(health.sizeDelta.x, EnemyBarHeight);
            health.anchoredPosition = new Vector2(health.anchoredPosition.x, top - EnemyBarHeight);
        }

        // Every star row ("level_2", "level_3" and Auga's "level_4" to "level_X") sits just below the bar, or on it with
        // [EnemyHud] StarsBelowHealthBar off, which leaves room for mods that write text below the bar.
        private static void PlaceStars(Transform hud)
        {
            var health = hud.Find("Health") as RectTransform;
            if (health == null)
                return;
            // The bar is anchored to the bottom edge of the hud, the star rows to its centre.
            var barBottom = health.anchoredPosition.y - ((RectTransform)hud).rect.height / 2f;
            var below = ValConfig.EnemyHudStarsBelowBar.Value;
            foreach (Transform child in hud)
            {
                if (!child.name.StartsWith("level_") || !(child is RectTransform row))
                    continue;
                var y = below ? barBottom - EnemyBarFrame - row.rect.height / 2f : barBottom + health.rect.height / 2f;
                row.anchoredPosition = new Vector2(row.anchoredPosition.x, y);
            }
        }

        private static PlayerBarLook FindPlayerBarLook()
        {
            if (Auga.Assets.Hud == null)
                return null;
            var bar = Auga.Assets.Hud.GetComponentsInChildren<AugaHealthBar>(true).FirstOrDefault(x => x.Mode == AugaHealthBar.ModeType.Health);
            if (bar == null || bar.FastBar == null || bar.Background == null || bar.Border == null || bar.TickPrefab == null)
                return null;
            var look = new PlayerBarLook
            {
                Background = bar.Background.GetComponent<Image>(),
                Border = bar.Border.GetComponent<Image>(),
                Fill = bar.FastBar.GetComponent<Image>(),
                Mask = bar.FastBar.transform.parent != null ? bar.FastBar.transform.parent.GetComponent<Image>() : null,
                Tick = bar.TickPrefab.GetComponent<Image>(),
            };
            return look.Background != null && look.Border != null && look.Fill != null && look.Mask != null && look.Tick != null ? look : null;
        }

        // The player's slanted bar: its background, a fill cut to the slanted frame, a tick at each quarter and the
        // frame on top. The fast bar gets the player's health colour; the slow bar behind it keeps its own.
        private static void StyleBossBar(Transform hud, PlayerBarLook look)
        {
            var health = hud.Find("Health") as RectTransform;
            if (health == null)
                return;
            var width = health.rect.width;

            if (health.Find("bkg") is RectTransform bkg && bkg.GetComponent<Image>() is Image bkgImage)
            {
                // the frame is part of the sprite now, not a margin around the bar
                Slice(bkgImage, look.Background.sprite);
                bkgImage.color = look.Background.color;
                bkg.offsetMin = bkg.offsetMax = Vector2.zero;
            }

            StyleBossFill(health.Find("health_slow"), look, width, null, SlowFillLag);
            StyleBossFill(health.Find("health_fast"), look, width, look.Fill.color, 0f);
            StyleBossFill(health.Find("health_fast_friendly"), look, width, null, 0f);

            var ticks = new GameObject("Ticks", typeof(RectTransform));
            ticks.layer = health.gameObject.layer;
            var ticksRect = (RectTransform)ticks.transform;
            ticksRect.SetParent(health, false);
            AugaPanelRestyler.Stretch(ticksRect);
            foreach (var fraction in TickFractions)
            {
                var tick = AddImage(ticksRect, $"tick_{Mathf.RoundToInt(fraction * 100)}", look.Tick.sprite);
                tick.color = AugaPanelRestyler.WithAlpha(look.Tick.color, fraction == 0.5f ? 0.75f : 0.5f);
                // The ticks mark the inside of the frame, which is the bar's width less FrameInset at each end:
                // FrameInset + (width - 2 * FrameInset) * fraction, written so that it holds at any width.
                var rect = tick.rectTransform;
                rect.anchorMin = new Vector2(fraction, 0f);
                rect.anchorMax = new Vector2(fraction, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TickWidth, 0f);
                rect.anchoredPosition = new Vector2(FrameInset * (1f - 2f * fraction), 0f);
            }

            var border = AddImage(health, "Border", look.Border.sprite);
            Slice(border, look.Border.sprite);
            border.color = look.Border.color;
            AugaPanelRestyler.Stretch(border.rectTransform);
        }

        // GuiBar sets its bar's width to its full width times the health, while the slanted ends need an empty bar to
        // still draw FrameInset + FillTip units of fill before its tip reaches the inside of the frame. So the bar
        // vanilla drives starts that far in and spans only the inside of the frame, and draws nothing itself. The
        // fill is a child of it that reaches back to the start of the frame: its tip then meets the ticks at every
        // health value. A mask with the frame's outline cuts the fill's square left end to the slant.
        private static void StyleBossFill(Transform holder, PlayerBarLook look, float width, Color? color, float lag)
        {
            var bar = holder != null ? holder.Find("bar") as RectTransform : null;
            var barImage = bar != null ? bar.GetComponent<Image>() : null;
            if (barImage == null || holder.GetComponent<Graphic>() != null)
                return;

            var mask = holder.gameObject.AddComponent<Image>();
            Slice(mask, look.Mask.sprite);
            mask.raycastTarget = false;
            holder.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            if (color.HasValue)
                barImage.color = color.Value;
            // anchored to the left edge of the holder and pivoted on its own left edge
            bar.anchoredPosition = new Vector2(FrameInset + FillTip, bar.anchoredPosition.y);
            bar.sizeDelta = new Vector2(width - 2f * FrameInset, bar.sizeDelta.y);

            var fill = AddImage(bar, "fill", look.Fill.sprite);
            Slice(fill, look.Fill.sprite);
            fill.color = barImage.color;
            AugaPanelRestyler.Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(-(FrameInset + FillTip), 0f);
            fill.rectTransform.offsetMax = new Vector2(-lag, 0f);
            // GuiBar still reads and sets this image's colour; the EnemyHud.UpdateHuds postfix copies it to the fill.
            barImage.enabled = false;
        }

        // Another mod may colour a boss bar through GuiBar.SetColor, which reaches the hidden bar image.
        public static void SyncBossFill(GuiBar bar)
        {
            if (bar == null || bar.m_bar == null || bar.m_barImage == null)
                return;
            var fill = bar.m_bar.Find("fill");
            var image = fill != null ? fill.GetComponent<Image>() : null;
            if (image != null && image.color != bar.m_barImage.color)
                image.color = bar.m_barImage.color;
        }

        // The divider around "Close Menu" in the pause menu, behind the boss's name. It is a child of the name, so it
        // follows wherever Auga or another mod moves the name, and its lines keep their length beside any name.
        private static void AddBossNameDivider(Transform hud)
        {
            var name = hud.Find("Name") as RectTransform;
            var text = name != null ? name.GetComponent<TMP_Text>() : null;
            if (text == null || Auga.Assets.DividerMedium == null)
                return;
            var divider = Object.Instantiate(Auga.Assets.DividerMedium, name, false);
            divider.name = "NameDivider";
            var rect = (RectTransform)divider.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var header = divider.AddComponent<AugaDividerHeader>();
            header.Content = divider.transform.Find("Content") as RectTransform;
            header.Text = text;
            header.LineLength = BossNameLineLength;
        }

        private static Image AddImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Slice(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = SpriteScale;
        }
    }
}
