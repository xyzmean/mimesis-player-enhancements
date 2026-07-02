namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardEmbeddedAssets
    {
        private const string FeatureFolder = "WebDashboard";

        internal const string IndexWebPath = "/";

        internal static bool IsAvailable => TryRead(IndexWebPath, out _, out _);

        internal static bool TryRead(string webPath, out byte[] bytes, out string extension)
        {
            string relative = webPath == IndexWebPath ? "index.html" : webPath.TrimStart('/');
            return Util.EmbeddedAssets.TryReadFeature(FeatureFolder, relative, out bytes, out extension);
        }
    }
}
