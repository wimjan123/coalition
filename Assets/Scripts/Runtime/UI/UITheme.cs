using UnityEngine;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// UI theme configuration for the COALITION demo.
    /// Defines colors, fonts, and styling for authentic Dutch government aesthetics.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Theme", menuName = "Coalition/UI/Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Color Palette")]
        [SerializeField] private Color backgroundColor = Color.white;
        [SerializeField] private Color panelBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color textColor = Color.black;
        [SerializeField] private Color accentColor = new Color(0.08f, 0.26f, 0.45f, 1f); // Rijksblauw

        [Header("Dutch Government Colors")]
        [SerializeField] private Color rijksBlauw = new Color(0.08f, 0.26f, 0.45f, 1f); // #154273
        [SerializeField] private Color secondaryBlue = new Color(0.15f, 0.35f, 0.55f, 1f);
        [SerializeField] private Color lightGray = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color darkGray = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Header("Button Colors")]
        [SerializeField] private Color buttonNormalColor = new Color(0.08f, 0.26f, 0.45f, 1f);
        [SerializeField] private Color buttonHighlightColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        [SerializeField] private Color buttonPressedColor = new Color(0.05f, 0.20f, 0.35f, 1f);
        [SerializeField] private Color buttonDisabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Header("Political Party Colors")]
        [SerializeField] private PartyColorScheme[] partyColors = new PartyColorScheme[]
        {
            new PartyColorScheme("VVD", new Color(0.0f, 0.4f, 0.8f, 1f)),        // Blue
            new PartyColorScheme("PVV", new Color(1.0f, 0.8f, 0.0f, 1f)),        // Blonde/Yellow
            new PartyColorScheme("NSC", new Color(0.0f, 0.6f, 0.4f, 1f)),        // Teal
            new PartyColorScheme("GL-PvdA", new Color(0.6f, 0.8f, 0.2f, 1f)),    // Green-Red mix
            new PartyColorScheme("D66", new Color(0.0f, 0.8f, 0.2f, 1f)),        // Green
            new PartyColorScheme("BBB", new Color(1.0f, 0.6f, 0.0f, 1f)),        // Orange
            new PartyColorScheme("CDA", new Color(0.2f, 0.6f, 0.8f, 1f)),        // Light Blue
            new PartyColorScheme("SP", new Color(0.8f, 0.2f, 0.2f, 1f)),         // Red
            new PartyColorScheme("FvD", new Color(0.5f, 0.3f, 0.7f, 1f)),        // Purple
            new PartyColorScheme("CU", new Color(0.3f, 0.5f, 0.8f, 1f)),         // Blue-Purple
            new PartyColorScheme("SGP", new Color(1.0f, 0.4f, 0.0f, 1f)),        // Orange-Red
            new PartyColorScheme("Volt", new Color(0.8f, 0.0f, 0.8f, 1f))        // Magenta
        };

        [Header("Typography")]
        [SerializeField] private Font font;
        [SerializeField] private Font headingFont;
        [SerializeField] private int defaultFontSize = 14;
        [SerializeField] private int headingFontSize = 18;

        [Header("Spacing and Layout")]
        [SerializeField] private float smallPadding = 4f;
        [SerializeField] private float mediumPadding = 8f;
        [SerializeField] private float largePadding = 16f;
        [SerializeField] private float elementSpacing = 8f;

        [Header("Window and Panel Styling")]
        [SerializeField] private Color windowBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color windowTitleBarColor = new Color(0.08f, 0.26f, 0.45f, 1f);
        [SerializeField] private Color windowTitleTextColor = Color.white;
        [SerializeField] private float windowBorderWidth = 2f;

        #region Properties

        public Color BackgroundColor => backgroundColor;
        public Color PanelBackgroundColor => panelBackgroundColor;
        public Color TextColor => textColor;
        public Color AccentColor => accentColor;

        public Color RijksBlauw => rijksBlauw;
        public Color SecondaryBlue => secondaryBlue;
        public Color LightGray => lightGray;
        public Color DarkGray => darkGray;

        public Color ButtonNormalColor => buttonNormalColor;
        public Color ButtonHighlightColor => buttonHighlightColor;
        public Color ButtonPressedColor => buttonPressedColor;
        public Color ButtonDisabledColor => buttonDisabledColor;

        public Font Font => font;
        public Font HeadingFont => headingFont;
        public int DefaultFontSize => defaultFontSize;
        public int HeadingFontSize => headingFontSize;

        public float SmallPadding => smallPadding;
        public float MediumPadding => mediumPadding;
        public float LargePadding => largePadding;
        public float ElementSpacing => elementSpacing;

        public Color WindowBorderColor => windowBorderColor;
        public Color WindowTitleBarColor => windowTitleBarColor;
        public Color WindowTitleTextColor => windowTitleTextColor;
        public float WindowBorderWidth => windowBorderWidth;

        #endregion

        #region Party Color Methods

        /// <summary>
        /// Get the color for a specific political party.
        /// </summary>
        /// <param name="partyName">Name of the party</param>
        /// <returns>Party color, or white if not found</returns>
        public Color GetPartyColor(string partyName)
        {
            foreach (var scheme in partyColors)
            {
                if (scheme.partyName.Equals(partyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return scheme.color;
                }
            }

            Debug.LogWarning($"[UITheme] Party color not found for: {partyName}");
            return Color.white;
        }

        /// <summary>
        /// Set the color for a specific political party.
        /// </summary>
        /// <param name="partyName">Name of the party</param>
        /// <param name="color">Color to set</param>
        public void SetPartyColor(string partyName, Color color)
        {
            for (int i = 0; i < partyColors.Length; i++)
            {
                if (partyColors[i].partyName.Equals(partyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    partyColors[i].color = color;
                    return;
                }
            }

            // If party not found, add new entry
            var newArray = new PartyColorScheme[partyColors.Length + 1];
            System.Array.Copy(partyColors, newArray, partyColors.Length);
            newArray[partyColors.Length] = new PartyColorScheme(partyName, color);
            partyColors = newArray;
        }

        /// <summary>
        /// Get all configured party colors.
        /// </summary>
        /// <returns>Array of party color schemes</returns>
        public PartyColorScheme[] GetAllPartyColors()
        {
            return (PartyColorScheme[])partyColors.Clone();
        }

        #endregion

        #region Color Utilities

        /// <summary>
        /// Get a lighter version of the accent color.
        /// </summary>
        /// <param name="lightnessFactor">Factor to lighten (0-1)</param>
        /// <returns>Lightened accent color</returns>
        public Color GetLightAccentColor(float lightnessFactor = 0.3f)
        {
            return Color.Lerp(accentColor, Color.white, lightnessFactor);
        }

        /// <summary>
        /// Get a darker version of the accent color.
        /// </summary>
        /// <param name="darknessFactor">Factor to darken (0-1)</param>
        /// <returns>Darkened accent color</returns>
        public Color GetDarkAccentColor(float darknessFactor = 0.3f)
        {
            return Color.Lerp(accentColor, Color.black, darknessFactor);
        }

        /// <summary>
        /// Get a color with modified alpha.
        /// </summary>
        /// <param name="baseColor">Base color</param>
        /// <param name="alpha">New alpha value (0-1)</param>
        /// <returns>Color with modified alpha</returns>
        public Color GetColorWithAlpha(Color baseColor, float alpha)
        {
            return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate the theme configuration.
        /// </summary>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateTheme()
        {
            var result = new ValidationResult();

            // Check for missing fonts
            if (font == null)
                result.AddWarning("Default font not assigned");

            if (headingFont == null)
                result.AddWarning("Heading font not assigned");

            // Validate font sizes
            if (defaultFontSize <= 0)
                result.AddError("Default font size must be positive");

            if (headingFontSize <= 0)
                result.AddError("Heading font size must be positive");

            // Validate padding values
            if (smallPadding < 0)
                result.AddError("Small padding cannot be negative");

            if (mediumPadding < smallPadding)
                result.AddWarning("Medium padding should be larger than small padding");

            if (largePadding < mediumPadding)
                result.AddWarning("Large padding should be larger than medium padding");

            // Validate political party colors
            if (partyColors.Length == 0)
                result.AddWarning("No political party colors defined");

            return result;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure positive values
            defaultFontSize = Mathf.Max(1, defaultFontSize);
            headingFontSize = Mathf.Max(1, headingFontSize);
            smallPadding = Mathf.Max(0, smallPadding);
            mediumPadding = Mathf.Max(smallPadding, mediumPadding);
            largePadding = Mathf.Max(mediumPadding, largePadding);
            elementSpacing = Mathf.Max(0, elementSpacing);
            windowBorderWidth = Mathf.Max(0, windowBorderWidth);
        }

        [ContextMenu("Validate Theme")]
        private void ValidateThemeInEditor()
        {
            var validation = ValidateTheme();
            if (validation.HasErrors || validation.HasWarnings)
            {
                Debug.LogWarning($"Theme validation for '{name}':\n{validation.GetFullSummary()}");
            }
            else
            {
                Debug.Log($"Theme '{name}' validation passed");
            }
        }

        [ContextMenu("Reset to Dutch Government Default")]
        private void ResetToDutchDefault()
        {
            backgroundColor = Color.white;
            panelBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            textColor = Color.black;
            accentColor = new Color(0.08f, 0.26f, 0.45f, 1f); // Rijksblauw

            buttonNormalColor = rijksBlauw;
            buttonHighlightColor = secondaryBlue;
            buttonPressedColor = new Color(0.05f, 0.20f, 0.35f, 1f);

            windowTitleBarColor = rijksBlauw;
            windowTitleTextColor = Color.white;

            Debug.Log("Reset theme to Dutch government defaults");
        }
#endif
    }

    /// <summary>
    /// Color scheme for a political party.
    /// </summary>
    [System.Serializable]
    public struct PartyColorScheme
    {
        public string partyName;
        public Color color;

        public PartyColorScheme(string name, Color partyColor)
        {
            partyName = name;
            color = partyColor;
        }
    }
}