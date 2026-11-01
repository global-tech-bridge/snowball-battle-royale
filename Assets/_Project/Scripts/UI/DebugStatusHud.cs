using SnowballBattleRoyale.Domain;
using SnowballBattleRoyale.Player;
using UnityEngine;

namespace SnowballBattleRoyale.UI
{
    /// <summary>
    /// Placeholder HP / stamina / cold bars drawn with IMGUI.
    ///
    /// Deliberately throwaway: Phase 10 replaces it with the real UI Toolkit HUD. It exists so the
    /// Phase 2 stamina rules are visible without committing to a UI layout yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugStatusHud : MonoBehaviour
    {
        private const float BarWidth = 260f;
        private const float BarHeight = 18f;
        private const float Margin = 16f;
        private const float RowSpacing = 24f;

        [SerializeField]
        private PlayerMovement _player;

        [Tooltip("Also show the current movement mode and speed.")]
        [SerializeField]
        private bool _showMovementDebug = true;

        private Texture2D _backgroundTexture;
        private Texture2D _fillTexture;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerMovement>();
            }
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }

            if (_fillTexture != null)
            {
                Destroy(_fillTexture);
            }
        }

        private void OnGUI()
        {
            PlayerStats stats = _player != null ? _player.Stats : null;
            if (stats == null)
            {
                return;
            }

            EnsureResources();

            float y = Margin;

            DrawBar(y, "HP", stats.HealthNormalized, new Color(0.85f, 0.25f, 0.25f),
                $"{stats.Health:0} / {stats.MaxHealth:0}");
            y += RowSpacing;

            Color staminaColor = stats.IsExhausted
                ? new Color(0.6f, 0.5f, 0.15f)
                : new Color(0.3f, 0.75f, 0.35f);
            string staminaSuffix = stats.IsExhausted ? "  (EXHAUSTED)" : string.Empty;
            DrawBar(y, "STA", stats.StaminaNormalized, staminaColor,
                $"{stats.Stamina:0} / {stats.MaxStamina:0}{staminaSuffix}");
            y += RowSpacing;

            Color coldColor = stats.IsColdPenaltyActive
                ? new Color(0.35f, 0.6f, 0.95f)
                : new Color(0.45f, 0.65f, 0.8f);
            string coldSuffix = stats.IsColdPenaltyActive ? "  (x2 DRAIN)" : string.Empty;
            DrawBar(y, "COLD", stats.ColdNormalized, coldColor, $"{stats.Cold:0}%{coldSuffix}");
            y += RowSpacing;

            if (_showMovementDebug)
            {
                GUI.Label(
                    new Rect(Margin, y, BarWidth + 200f, BarHeight),
                    $"{_player.CurrentMode}  |  {_player.CurrentSpeed:0.0} m/s  |  " +
                    $"grounded: {_player.IsGrounded}",
                    _labelStyle);
            }
        }

        private void DrawBar(float y, string label, float fill, Color color, string value)
        {
            const float labelWidth = 46f;

            GUI.Label(new Rect(Margin, y, labelWidth, BarHeight), label, _labelStyle);

            float barX = Margin + labelWidth;
            var background = new Rect(barX, y, BarWidth, BarHeight);
            GUI.DrawTexture(background, _backgroundTexture);

            float clamped = Mathf.Clamp01(fill);
            if (clamped > 0f)
            {
                Color previous = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(new Rect(barX, y, BarWidth * clamped, BarHeight), _fillTexture);
                GUI.color = previous;
            }

            GUI.Label(new Rect(barX + BarWidth + 8f, y, 240f, BarHeight), value, _labelStyle);
        }

        private void EnsureResources()
        {
            if (_backgroundTexture == null)
            {
                _backgroundTexture = MakeTexture(new Color(0f, 0f, 0f, 0.55f));
            }

            if (_fillTexture == null)
            {
                _fillTexture = MakeTexture(Color.white);
            }

            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white },
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
