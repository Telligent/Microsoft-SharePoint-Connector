using System;
using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPDocumentVersionComparer : IComparer<string>
    {
        public int Compare(String version1, String version2)
        {
            double val1;
            double val2;
            if (double.TryParse(version1, out val1) && double.TryParse(version2, out val2))
            {
                return val1.CompareTo(val2);
            }
            return 0;
        }
    }
}
