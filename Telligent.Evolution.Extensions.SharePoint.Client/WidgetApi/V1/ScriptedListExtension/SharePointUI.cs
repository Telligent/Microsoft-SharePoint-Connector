using System;
using System.Globalization;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointUIExtension : IScriptedContentFragmentExtension
    {
        #region IScriptedContentFragmentExtension

        public string ExtensionName
        {
            get { return "sharepoint_v1_ui"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointUI>(); }
        }

        public string Name
        {
            get { return "SharePoint UI functionality (sharepoint_v1_ui)"; }
        }

        public string Description
        {
            get { return "Allows to use culture dependency values for UI."; }
        }

        public void Initialize() { }

        #endregion
    }

    public interface ISharePointUI
    {
        /// <summary>
        /// Returns total number of days in month on the base of a user culture
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        int TotalDays(string year, string month);

        DateTime? ParseDateTimeExact(string date);

        string TranslateDayOfWeek(int weekDay);

        string ToCultureSafeString(DateTime date);

        int DayOfWeek(DateTime date);

        int DaysInMonth(string year, string month);

        int FirstDayOfWeek();
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointUI : ISharePointUI
    {
        private readonly CultureInfo userCulture;

        private SharePointUI(CultureInfo culture)
        {
            userCulture = culture;
        }

        public SharePointUI()
            : this(CultureInfo.CurrentUICulture)
        {
        }

        public int TotalDays(string year, string month)
        {
            const int firstDayInMonth = 1;
            const int numberOfDaysInWeek = 7;

            int inYear = int.Parse(year);
            int inMonth = int.Parse(month);

            var currentDate = new DateTime(inYear, inMonth, firstDayInMonth);
            int startOffset = (int)currentDate.DayOfWeek - FirstDayOfWeek();
            int totalDays = startOffset + DateTime.DaysInMonth(inYear, inMonth);
            if (totalDays % numberOfDaysInWeek != 0)
            {
                totalDays += numberOfDaysInWeek - totalDays % numberOfDaysInWeek;
            }
            return totalDays;
        }

        [Documentation(Description = "Converts a string in 'mm/dd/yyyy HH:mm:ss' format to a DateTime.")]
        public DateTime? ParseDateTimeExact(string date)
        {
            DateTime result;

            if (string.IsNullOrEmpty(date))
                return DateTime.MinValue;

            if (DateTime.TryParseExact(date, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            if (DateTime.TryParseExact(date, "M/d/yyyy H:m:s", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            return null;
        }

        public string TranslateDayOfWeek(int weekDay)
        {
            return ((DayOfWeek)weekDay).ToString();
        }

        public string ToCultureSafeString(DateTime date)
        {
            return date.ToString(CultureInfo.InvariantCulture);
        }

        public int DayOfWeek(DateTime date)
        {
            return (int)date.DayOfWeek;
        }

        public int DaysInMonth(string year, string month)
        {
            int inYear = int.Parse(year);
            int inMonth = int.Parse(month);
            return DateTime.DaysInMonth(inYear, inMonth);
        }

        public int FirstDayOfWeek()
        {
            return (int)userCulture.DateTimeFormat.FirstDayOfWeek;
        }
    }
}
