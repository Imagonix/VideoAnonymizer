using MudBlazor;

public static class AppTheme
{
    public static readonly MudTheme NeonPurpleCyan = new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#05070A",
            Background = "#0A0F1C",
            BackgroundGray = "#0F1629",

            Surface = "#12182B",
            DrawerBackground = "#0D1324",
            AppbarBackground = "#0A0F1C",

            Primary = "#8B5CF6", 
            PrimaryContrastText = "#FFFFFF",

            Secondary = "#22D3EE",
            SecondaryContrastText = "#041014",

            Tertiary = "#A3E635",

            Info = "#38BDF8",
            Success = "#22C55E",
            Warning = "#F59E0B",
            Error = "#EF4444",

            TextPrimary = "#F1F5F9",
            TextSecondary = "#94A3B8",
            TextDisabled = "#64748B",

            ActionDefault = "#CBD5E1",
            ActionDisabled = "#475569",
            ActionDisabledBackground = "#1E293B",

            Divider = "#1F2937",
            LinesDefault = "#1F2937",
            LinesInputs = "#334155",

            TableLines = "#1F2937",
            TableStriped = "#0F172A",
            TableHover = "#1E293B",

            HoverOpacity = 0.06,
            RippleOpacity = 0.10,
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px"
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "Segoe UI", "Roboto", "sans-serif" },
                FontSize = "0.95rem",
                LineHeight = "1.6"
            },

            H5 = new H5Typography
            {
                FontWeight = "700",
                LetterSpacing = "-0.02em"
            },

            H6 = new H6Typography
            {
                FontWeight = "600"
            },

            Button = new ButtonTypography
            {
                FontWeight = "600",
                TextTransform = "none"
            }
        }
    };
}