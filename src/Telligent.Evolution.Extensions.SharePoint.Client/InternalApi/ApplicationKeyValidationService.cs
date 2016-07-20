using System;
using Telligent.Evolution.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IApplicationKeyValidationService
    {
        bool IsValid(string key, Func<string, bool> isDuplicate);
        string MakeValid(string key, Func<string, bool> isDuplicate);
    }

    internal class ApplicationKeyValidationService : IApplicationKeyValidationService
    {
        public bool IsValid(string key, Func<string, bool> isDuplicate)
        {
            return new ApplicationKeyValidator(isDuplicate).IsValid(key);
        }

        public string MakeValid(string key, Func<string, bool> isDuplicate)
        {
            return new ApplicationKeyValidator(isDuplicate).MakeValid(key);
        }
    }

    internal class ApplicationKeyValidator
    {
        private Func<string, bool> isDuplicate;

        public ApplicationKeyValidator(Func<string, bool> isDuplicate)
        {
            this.isDuplicate = isDuplicate;
        }

        public bool IsValid(string key)
        {
            return !isDuplicate(key);
        }

        public string MakeValid(string key)
        {
            return IsValid(key) ? key : key + "-2";
        }
    }
}
