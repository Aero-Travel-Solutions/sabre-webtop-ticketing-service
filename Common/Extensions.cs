using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Text;

namespace SabreWebtopTicketingService.Common
{
    public static class Extensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
                        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static string[] SplitOn(this string text, string pattern)
        {
            return text.Split(new string[] { pattern }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string MaskLog(this string text)
        {
            // mask credit card numbers
            if (text == null) return null;

            return new Regex(@"(\s*\d){13,16}").Replace(text, m => GetReplaceString(m.Value.Trim()));
        }

        public static string GetReplaceString(string text)
        {
            // replace all digits except last 4
            var result = "";

            // check is valid card
            try
            {
                CreditCardOperations.GetCreditCardType(text.LastMatch(@"\d+"));
            }
            catch (Exception)
            {
                return text;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]) || i >= text.Length - 4)
                {
                    result += text[i];
                }
                else
                {
                    result += 'X';
                }
            }

            return result;
        }

        public static string ReplaceAll(this string seed, string[] replacablestrings, string replacementstring = "")
        {
            return replacablestrings.Aggregate(seed, (s1, s2) => s1.Replace(s2, replacementstring));
        }

        public static string MaskNumber(this string text)
        {
            return text.Last(4).PadLeft(text.Length - 4, 'X');
        }

        public static string FirstMatch(this string text, string pattern, string defaultValue = null)
        {
            var match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Groups.Cast<Group>().First().Value;
            }

            return defaultValue;
        }

        public static string LastMatch(this string text, string pattern, string defaultValue = null)
        {
            var match = Regex.Match(text, pattern);

            if (match.Success)
            {
                return match.Groups.Cast<Group>().Where(w => !string.IsNullOrEmpty(w.Value)).Last().Value;
            }

            return defaultValue;
        }

        public static string Last(this string source, int tail_length)
        {
            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }

        public static string EncodeBase64(this string text)
        {
            if (text == null)
            {
                return null;
            }

            byte[] textAsBytes = Encoding.ASCII.GetBytes(text);
            return Convert.ToBase64String(textAsBytes);
        }

        public static string DecodeBase64(this string encodedText)
        {
            if (encodedText == null)
            {
                return null;
            }

            byte[] textAsBytes = Convert.FromBase64String(encodedText);
            return Encoding.ASCII.GetString(textAsBytes);
        }

        public static bool IsMatch(this string text, string pattern)
        {
            if (string.IsNullOrEmpty(text)) { return false; }

            // return true if any regex match text
            return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);
        }

        public static List<string> AllMatches(this string text, string pattern)
        {
            var matches = Regex.Matches(text, pattern);

            if (!matches.IsNullOrEmpty())
            {
                return matches.Select(s => s.Groups[0].Value).ToList();
            }
            return new List<string>();
        }

        public static List<string> SplitInChunk(this string text, int max)
        {
            var charCount = 0;
            var lines = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.GroupBy(w => (charCount += (((charCount % max) + w.Length + 1 >= max)
                            ? max - (charCount % max) : 0) + w.Length + 1) / max)
                        .Select(g => string.Join(" ", g.ToArray()))
                        .ToList();
        }

        public static string ReplaceAllSabreSpecialChar(this string seed, string replacementstring = "")
        {
            Dictionary<string, string> replacablestrings = new Dictionary<string, string>()
            {
                {"Â\u0087", "‡" },
                {"\u0087", "‡" }
            };

            return replacablestrings.
                    Aggregate(seed, (current, value) =>
                        current.Replace(value.Key, value.Value));
        }

        public static string RegexReplace(this string seed, string pattern, string replacementstring = "")
        {
            return Regex.Replace(seed, pattern, replacementstring);
        }

        public static List<string> SplitOnRegex(this string text, params string[] patterns)
        {
            List<string> results = new List<string>();
            foreach (string pattern in patterns)
            {
                results.AddRange(SplitOnRegex(text, pattern));  
            }
            return results;
        }

        public static string[] SplitOnRegex(this string text, string pattern)
        {
            return Regex.Split(text, pattern, RegexOptions.Multiline);
        }

        public static string Mask(this string text)
        {
            // mask credit card numbers
            if (text == null) return null;

            return new Regex(@"(TP|VI|DC|AX|CA|JC|JV|CKS\*)(\s*\d){13,16}").Replace(text, m => GetReplaceString(m.Value.Trim()));
        }


    }
}
