using System;
using System.Globalization;

namespace SabreWebtopTicketingService.Common
{
    public static class DatetimeExtensions
    {
        public static string GetSabreDatetime(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        public static Int32 GetCurrentAge(this DateTime dateOfBirth)
        {
            var today = DateTime.Today;

            var a = (today.Year * 100 + today.Month) * 100 + today.Day;
            var b = (dateOfBirth.Year * 100 + dateOfBirth.Month) * 100 + dateOfBirth.Day;

            return (a - b) / 10000;
        }

        public static Int32 GetAgeAsOfGivenDate(this DateTime dateOfBirth, DateTime givendate)
        {
            var a = (givendate.Year * 100 + givendate.Month) * 100 + givendate.Day;
            var b = (dateOfBirth.Year * 100 + dateOfBirth.Month) * 100 + dateOfBirth.Day;

            return (a - b) / 10000;
        }

        public static string GetISODateString(this DateTime dt, string format = "")
        {
            return string.IsNullOrEmpty(format) ? dt.ToString("yyyy-MM-dd") : dt.ToString(format);
        }

        public static string GetISOTimeString(this DateTime dt)
        {
            return dt.ToString("HH:mm");
        }

        public static string GetISODateTime(this DateTime dt, string format = "")
        {
            return string.IsNullOrEmpty(format) ? dt.ToString("yyyy-MM-ddTHH:mm") : dt.ToString(format);
        }
    }
}
