using System;
using Telligent.Evolution.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Data
{
    /// <summary>
    /// Allows to create performance reports
    /// </summary>
    public class PerformanceProfiler : IDisposable
    {
        private readonly string comment;
        private readonly DateTime start;
        private readonly string category = "Profile Sync";
        private readonly int eventId = 7763453;

        /// <summary>
        /// Performance Profiler initialization
        /// </summary>
        /// <param name="comment">user comment</param>
        public PerformanceProfiler(string comment)
        {
            Format = @"{0} Execution time is {1} seconds";
            start = DateTime.Now;
            this.comment = comment;
        }

        /// <summary>
        /// Performance Profiler initialization
        /// </summary>
        /// <param name="comment">user comment</param>
        /// <param name="category">info log event category name</param>
        /// <param name="eventId">info log event id</param>
        public PerformanceProfiler(string comment, string category, int eventId)
            : this(comment)
        {
            this.category = category;
            this.eventId = eventId;
        }

        /// <summary>
        /// The report format
        /// </summary>
        public string Format { get; set; }

        public void Dispose()
        {
#if DEBUG
            var end = DateTime.Now;
            var execTime = (end - start).TotalSeconds;
            EventLogs.Info(String.Format(Format, comment, execTime), category, eventId, CSContext.Current.SettingsID);
#endif
        }
    }
}
