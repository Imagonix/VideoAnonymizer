using MudBlazor;

public static class AppTheme
{
    public static readonly MudTheme ModernDarkTheme = new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#05070A",
            Background = "#0B0F14",
            BackgroundGray = "#111827",

            Surface = "#121822",
            DrawerBackground = "#0F141C",
            AppbarBackground = "#0B0F14",

            Primary = "#7C3AED",
            PrimaryContrastText = "#FFFFFF",

            Secondary = "#06B6D4",
            SecondaryContrastText = "#041014",

            Tertiary = "#22C55E",

            Info = "#38BDF8",
            Success = "#22C55E",
            Warning = "#F59E0B",
            Error = "#EF4444",

            TextPrimary = "#F8FAFC",
            TextSecondary = "#CBD5E1",
            TextDisabled = "#64748B",

            ActionDefault = "#CBD5E1",
            ActionDisabled = "#475569",
            ActionDisabledBackground = "#1E293B",

            Divider = "#263241",
            LinesDefault = "#263241",
            LinesInputs = "#334155",

            TableLines = "#263241",
            TableStriped = "#0F172A",
            TableHover = "#1E293B",

            HoverOpacity = 0.08,
            RippleOpacity = 0.12,
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "14px",
            DrawerWidthLeft = "280px",
            AppbarHeight = "64px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "Roboto", "Arial", "sans-serif" },
                FontSize = "0.95rem",
                FontWeight = "400",
                LineHeight = "1.6"
            },

            H5 = new H5Typography
            {
                FontWeight = "700",
                LetterSpacing = "-0.02em"
            },

            H6 = new H6Typography
            {
                FontWeight = "700",
                LetterSpacing = "-0.01em"
            },

            Button = new ButtonTypography
            {
                FontWeight = "600",
                TextTransform = "none",
                LetterSpacing = "0.01em"
            }
        },

        Shadows = new Shadow
        {
            Elevation = new string[]
            {
                "none",
                "0 1px 2px rgba(0,0,0,.25)",
                "0 4px 12px rgba(0,0,0,.28)",
                "0 8px 24px rgba(0,0,0,.32)",
                "0 12px 32px rgba(0,0,0,.36)",
                "0 16px 40px rgba(0,0,0,.40)",
                "0 20px 48px rgba(0,0,0,.44)",
                "0 24px 56px rgba(0,0,0,.48)",
                "0 28px 64px rgba(0,0,0,.52)",
                "0 32px 72px rgba(0,0,0,.56)",
                "0 36px 80px rgba(0,0,0,.60)",
                "0 40px 88px rgba(0,0,0,.64)",
                "0 44px 96px rgba(0,0,0,.68)",
                "0 48px 104px rgba(0,0,0,.72)",
                "0 52px 112px rgba(0,0,0,.76)",
                "0 56px 120px rgba(0,0,0,.80)",
                "0 60px 128px rgba(0,0,0,.84)",
                "0 64px 136px rgba(0,0,0,.88)",
                "0 68px 144px rgba(0,0,0,.90)",
                "0 72px 152px rgba(0,0,0,.92)",
                "0 76px 160px rgba(0,0,0,.94)",
                "0 80px 168px rgba(0,0,0,.96)",
                "0 84px 176px rgba(0,0,0,.98)",
                "0 88px 184px rgba(0,0,0,1)",
                "0 92px 192px rgba(0,0,0,1)",
                "0 96px 200px rgba(0,0,0,1)"
            }
        }
    };
}