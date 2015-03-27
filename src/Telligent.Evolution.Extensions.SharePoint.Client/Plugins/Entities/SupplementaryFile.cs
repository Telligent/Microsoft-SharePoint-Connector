using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Entities
{
    internal class SupplementaryFile : ResourceFile
    {
        public SupplementaryFile(string assemblyName, string path) : base(assemblyName, path) { }

        protected override sealed void Init(string path)
        {
            // path: Resources.SupplementaryFiles.[theme].([fileName].[extension])
            var parts = path.Split('.');
            int extensionIndex = parts.Length - 1;
            int nameIndex = parts.Length - 2;
            int themeIndex = parts.Length - 3;

            Name = String.Concat(parts[nameIndex], ".", parts[extensionIndex]);
            Theme = parts[themeIndex];
        }
    }
}
