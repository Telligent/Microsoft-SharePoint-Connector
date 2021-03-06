﻿using System;
using ConsoleUPS.MyProfileUPSService;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleUPS.Util;

namespace ConsoleUPS
{
    public class SyncBase
    {
        public virtual bool IsValidProfile(IEnumerable<PropertyData> properties, string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "*") return true;

            var regex = new Regex(filter, RegexOptions.Compiled);
            return properties.Any(p => p != null && p.Name == "AccountName" && regex.IsMatch(GetValueData(p.Values.FirstOrDefault())));
        }

        public virtual string FieldsToJson(IEnumerable<PropertyData> properties)
        {
            var fields = new List<string>();
            var currentName = string.Empty;

            foreach (var propertyData in properties)
            {
                try
                {
                    if (propertyData == null) continue;
                    
                    currentName = propertyData.Name;
                    fields.Add(string.Format(@"""{0}"" : ""{1}""", propertyData.Name.Replace(@"""", "'"), GetValueData(propertyData.Values.FirstOrDefault())));
                }
                catch (Exception ex)
                {
                    SyncUtil.WriteLine("{0} Warning : {1}", currentName, ex.Message);
                }
            }

            return string.Concat("{", string.Join(",", fields), "}");
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
