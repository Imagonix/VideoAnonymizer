namespace VideoAnonymizer.Database;
public static class GapHandlingModes
{
    public const string Interpolate = "Interpolate";
    public const string UseBuffers = "UseBuffers";

    public static string Resolve(string? stored) =>
        string.Equals(stored, UseBuffers, StringComparison.OrdinalIgnoreCase)
            ? UseBuffers
            : Interpolate;

    public static bool IsUseBuffers(string? stored) =>
        string.Equals(stored, UseBuffers, StringComparison.OrdinalIgnoreCase);
}
