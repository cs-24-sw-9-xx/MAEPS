using System;

namespace Maes.Utilities
{
    public static class TimeUtilities
    {
        /// <summary>
        /// Use this method to get the current time in UTC in the standard format.
        /// Especially useful for file and folder names.
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentTimeUTC()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
        }
    }
}