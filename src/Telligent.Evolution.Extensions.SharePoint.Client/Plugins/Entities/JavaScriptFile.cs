using System;
using System.Linq;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Entities
{
    internal class JavaScriptFile : ResourceFile
    {
        public JavaScriptFile(string assemblyName, string path) : base(assemblyName, path) { }

        protected override sealed void Init(string path)
        {
            // path: Resources.Javascript.([fileName].js)
            // const int resourcesIndex = 0;
            // const int javascriptIndex = 1;
            const int fileIndex = 2;
            Name = string.Join(".", path.Split('.').Skip(fileIndex));
        }
    }
}
