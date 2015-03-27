using System;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil
{
    public class AuthenticationParam
    {
        // Internal name
        public String Name { get; private set; }
        // Description name for UI
        public String Text { get; private set; }
        public String Value { get; set; }

        public AuthenticationParam(string name, string text, string defaultValue)
        {
            Name = name;
            Text = text;
            Value = defaultValue;
        }
    }
}
