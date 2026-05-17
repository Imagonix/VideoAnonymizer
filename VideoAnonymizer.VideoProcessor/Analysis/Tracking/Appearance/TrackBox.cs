namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

internal readonly record struct TrackBox(int X, int Y, int Width, int Height)
{
    public double Area => Math.Max(0, (double)Width) * Math.Max(0, Height);
    public double Diagonal => Math.Sqrt((double)Width * Width + (double)Height * Height);
    public TrackPoint Center => new(X + Width / 2.0, Y + Height / 2.0);
}
