using System.IO;
using System.Reflection;
using System.Text;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal static class EmbeddedResources
    {
        private static readonly Assembly assembly = typeof(SharePointDataService).Assembly;

        internal static string GetString(string path)
        {
            using (var stream = GetStream(path))
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                var text = Encoding.UTF8.GetString(data);

                if (text[0] > 255)
                    return text.Substring(1);

                return text;
            }
        }

        internal static Stream GetStream(string path)
        {
            return assembly.GetManifestResourceStream(path);
        }
    }
}
