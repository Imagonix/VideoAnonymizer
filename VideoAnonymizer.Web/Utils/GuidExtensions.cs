namespace VideoAnonymizer.Web.Utils;
public static class GuidExtensions
{
    public static bool IsNullOrEmpty(this Guid? guid)
    {
        return guid is null || guid == Guid.Empty;
    }

    public static bool IsNullOrEmpty(this Guid guid)
    {
        return guid == Guid.Empty;
    }
}