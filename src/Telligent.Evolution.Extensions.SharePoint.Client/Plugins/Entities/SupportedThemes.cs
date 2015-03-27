using System;
using System.Collections.Generic;
using System.Linq;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Entities
{
    internal static class SupportedThemes
    {
        private static readonly List<SupportedTheme> themes = new List<SupportedTheme>
            {
                {new SupportedTheme("3fc3f82483d14ec485ef92e206116d49","Social")},
                {new SupportedTheme("424eb7d9138d417b994b64bff44bf274","Enterprise")},
                {new SupportedTheme("7e987e474b714b01ba29b4336720c446","Fiji")},
            };

        public static SupportedTheme Get(string themeName)
        {
            return themes.FirstOrDefault(theme => string.Equals(theme.Name, themeName, StringComparison.InvariantCultureIgnoreCase));
        }

        public static SupportedTheme Get(Guid themeId)
        {
            return themes.FirstOrDefault(theme => theme.Id == themeId);
        }
    }

    internal class SupportedTheme
    {
        public SupportedTheme(string id, string name)
        {
            Id = new Guid(id);
            Name = name;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }
    }
}
