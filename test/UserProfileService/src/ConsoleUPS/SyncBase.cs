using ConsoleUPS.MyProfileUPSService;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleUPS
{
    public class SyncBase
    {
        public virtual bool IsValidProfile(IEnumerable<PropertyData> properties, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;

            var regex = new Regex(filter, RegexOptions.Compiled);
            return properties.Any(p => p != null && p.Name == "AccountName" && regex.IsMatch(GetValueData(p.Values.FirstOrDefault())));
        }

        public virtual string FieldsToJson(IEnumerable<PropertyData> properties)
        {
            var fields = string.Join(",", properties.Select(FormatField));
            return string.Concat("{", fields, "}");
        }

        private string FormatField(PropertyData propertyData)
        {
            return (propertyData == null) ? 
                string.Empty :
                string.Format(@"""{0}"" : ""{1}""", propertyData.Name.Replace(@"""", "'"), GetValueData(propertyData.Values.FirstOrDefault()));
        }

        private string GetValueData(ValueData data)
        {
            return data == null ? string.Empty : data.Value.ToString()
                .Replace(@"""", @"'")
                .Replace(@"\", @"\\")
                .Replace("\n", "")
                .Replace("\r", "");
        }
    }
}
