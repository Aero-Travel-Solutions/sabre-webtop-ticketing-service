using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SabreWebtopTicketingService.Services
{
    internal static class SabreSharedServices
    {
        public static string GetSabreToken(string username, string password, string pcc)
        {
            string token = string.Format("V1:{0}:{1}:AA", username, pcc).EncodeBase64();
            token += ":";
            token += password.EncodeBase64();
            token = token.EncodeBase64();
            return token;
        }

        public static string GetPaxType(string paxtype, bool IsINF = false)
        {
            string result = string.IsNullOrEmpty(paxtype) ?
                                IsINF ?
                                    "INF" :
                                    "ADT" :
                                paxtype.StartsWith("A") ? "ADT" :
                                paxtype.StartsWith("C") ? "CHD" :
                                paxtype.StartsWith("C") ? "INF" :
                                //defaulted to ADT for all other pax types
                                paxtype;

            return result.ToUpper();

        }

        public static sectorinfo GetSectorNo(PQSector pqsec, List<PNRSector> pnrsec)
        {
            sectorinfo sec = new sectorinfo();
            sec.SectorNo = -2;
            PNRSector pnrsector = pnrsec.
                        FirstOrDefault(w => w.From == pqsec.DepartureCityCode &&
                                            w.To == pqsec.ArrivalCityCode &&
                                            (pqsec.ClassOfService.IsNullOrEmpty() || w.Class == pqsec.ClassOfService) &&
                                            (pqsec.MarketingFlight.IsNullOrEmpty() || w.Carrier == pqsec.MarketingFlight) &&
                                            (pqsec.MarketingFlightNumber.IsNullOrEmpty() || w.Flight == pqsec.MarketingFlightNumber.PadLeft(4, '0')) &&
                                            (pqsec.DepartureDateTime.IsNullOrEmpty() || w.DepartureDate == pqsec.DepartureDateTime.SplitOn("T")[0]));

            sec.Void = pnrsector == null;
            sec.SectorNo = sec.Void ? -2 : pnrsector.SectorNo;
            sec.DepartureDate = sec.Void ? "" : pnrsector.DepartureDate;

            return sec;
        }

        public static string GenerateGroupKey(string RFIC, string RFISC, string commercialname, string groupcode)
        {
            if (RFIC == "G" && commercialname.Contains("SKYCOUCH"))
            {
                return "SKYCOUCH";
            }
            if (groupcode == "BG")
            {
                return "C";
            }
            else if ("A|E".Contains(RFIC) && (commercialname.Contains("UNACCOMPANIED ") || commercialname.Contains("UNMR ")))
            {
                return "UNMR";
            }
            else if (RFIC == "C" && commercialname.Contains("PET "))
            {
                return "PET";
            }
            else if (RFIC == "A")
            {
                return RFIC;
            }
            else if (groupcode == "BG")
            {
                return "C";
            }

            return "OTHER";
        }


        public static string GetBaggageDiscription(string baggageallowance)
        {
            string result = baggageallowance;
            try
            {
                if (baggageallowance.IsMatch(@"[KGPC]{1,2}\d+") || baggageallowance.IsMatch(@"\d+[KGPC]{1,2}"))
                {
                    string noofpc = baggageallowance.LastMatch(@"(\d+)[KGPC]{1,2}");
                    if (string.IsNullOrEmpty(noofpc))
                    {
                        noofpc = baggageallowance.LastMatch(@"[KGPC]{1,2}(\d+)");
                        if (string.IsNullOrEmpty(noofpc))
                        {
                            return baggageallowance;
                        }
                    }

                    int intnoofpc = int.Parse(noofpc);
                    string unitofmesure = baggageallowance.LastMatch(@"\d+([KGPC]{1,2})");
                    if (string.IsNullOrEmpty(unitofmesure))
                    {
                        unitofmesure = baggageallowance.LastMatch(@"([KGPC]{1,2})\d+");
                    }

                    unitofmesure = unitofmesure.StartsWith("P") ? "PC" : unitofmesure.StartsWith("K") ? "KG" : unitofmesure;
                    result = $"{intnoofpc.ToString()}{unitofmesure}";
                }
            }
            catch
            {
                return baggageallowance;
            }

            return result;
        }

        public static List<AirlineTTL> GetAirlineTTLs(List<string> ttllines, DateTime? bookingcreateddate, List<string> applicablecarriers)
        {
            /*
                BY 1400/04JUN NZLT
                TO AC BY 31MAY 1027 MEL TIME ZONE OTHERWISE WILL BE XLD
                PLS ADV TKT NBR BY 15FEB21/2359Z OR LH OPTG/MKTG FLTS WILL BE CANX / APPLIC FARE RULE APPLIES IF IT DEMANDS EARLIER TKTG
                A3/OA AUTO XX IF ELECTRONIC TKT NR NOT RCVD BY 01JAN21 0905 MEL LT
                TO AF BY 04JUN 1100 MEL OTHERWISE WILL BE XLD
                SUBJ CXL ON/BEFORE 30MAY 0005Z WITHOUT PAYMENT
                TO VY ON/BEFORE 30MAY 0005Z OTHERWISE WILL BE XLD
                ADV TKT NBR TO CX/KA BY 10JUL 2300 GMT OR SUBJECT TO CANCEL
                RITK/ADTKT BY 26JUN 0900 MEL LT
                TO SQ BY 22NOV 2300 MEL TIME ZONE OTHERWISE WILL BE XLD
                TO PG BY 30DEC 1625 MEL TIME ZONE OTHERWISE WILL BE XLD
                TO MH BY 29DEC 0800 ZZZ TIME ZONE OTHERWISE WILL BE XLD
                TO JL BY 29DEC 2359 MEL TIME ZONE OTHERWISE WILL BE XLD
                TO TG BY 27JUN 1000 OTHERWISE WILL BE XLD
                TO OZ BY 11JUN 1000 OTHERWISE WILL BE XLD

                ADTK NO LATER THAN 17JUN TO AVOID CANCELLATION
                ADV TKT BY 19JUN20 1259MELAU OR WL BE CXLD
                AGT/BA PLS ISSUE E-TICKET BY 10JUL20 PLS ACTION OR WILL CANCEL/MSGPREM
             */
            List<AirlineTTL> ttls = new List<AirlineTTL>();
            if (ttllines.IsNullOrEmpty()) { return ttls; }

            CancellationToken ct = new CancellationToken();

            ParallelOptions options = new ParallelOptions { CancellationToken = ct };

            Parallel.ForEach(ttllines, options, (text) =>
            {
                List<string> lineitempatterns = new List<string>()
                {
                    @"(\w{2}\s*BY\s\d{3,4}\/\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\s*[A-Z\s]*[(LT)(TIME ZONE)])",
                    @"(\w{2}\s*BY\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\s*\d{3,4}[A-Z\s]*[(LT)(TIME ZONE)])",
                    @"(\w{2}\s*BY\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\s*\d{3,4}).*",
                    @"(\w{2}\s*ON\/BEFORE\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\s*\d{3,4}Z).*",
                    @"(BY\s(\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}\/\d{3,4}Z)\s*OR\s*\w{2})",
                    @"(\w{2}\s*BY\s\d{3,4}\/\d{2}[A-Z]{3}\d{2,4}.*)",
                    @"(\w{2}\s*TICKET BY\s\d{3,4}\/\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2,4}.*)",
                    @"(\w{2}\s*BY\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC])[\s\/]?\d{3,4}.*",
                    @"BY\s[A-Z]{3}(\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}\/\d{3,4}).*",
                    @"AGT[\/](\w{2}).*\s*BY\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC].*",
                    @"\w{2}\s+PLS\s+TKT\s+BY\s+(\d{3,4}\s+\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}\s+[A-Z]{3}\s+OR\s+\w{2})\s",
                    @"NO\s+LATER\s+THAN\s+(\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC])"
                };
                string match = "";

                foreach (var patern in lineitempatterns)
                {
                    match = text.FirstMatch(patern, "");
                    if (!string.IsNullOrEmpty(match)) { break; }
                }
                AirlineTTL ttl = new AirlineTTL();

                if (string.IsNullOrEmpty(match)) { return; }

                //Airline
                List<string> carrierpatterns = new List<string>()
                {
                    @"[\/\s](\w{2})\s*BY\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]",
                    @"BY\s\d{2}\s*[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}\/\d{4}Z\s*OR\s*(\w{2})",
                    @"[\/\s](\w{2})\s*ON\/BEFORE\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]",
                    @"(\w{2})\s*RITK\/ADTKT\s*BY\s*\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]",
                    @"\sBY\s\d{3,4}[\s\/]?\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}\s(\w{2})LT",
                    @"AGT[\/](\w{2}).*\s*BY\s\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC]",
                    @"\d{3,4}\s+\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}\s+[A-Z]{3}\s+OR\s+(\w{2})\s"
                };

                foreach (var patern in carrierpatterns)
                {
                    ttl.Airline = text.LastMatch(patern, "");
                    if (!string.IsNullOrEmpty(ttl.Airline))
                    {
                        if (!applicablecarriers.Contains(ttl.Airline) && applicablecarriers.Count == 1)
                        {
                            ttl.Airline = applicablecarriers.First();
                        }
                        else
                        {
                            ttl.Airline = "";
                        }
                        break;
                    }
                }

                if (string.IsNullOrEmpty(ttl.Airline) && applicablecarriers.Count == 1)
                {
                    ttl.Airline = applicablecarriers.First();
                }


                //Date
                var datestring = match.FirstMatch(@"(\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2})");

                DateTime date = DateTime.MinValue;
                if (!string.IsNullOrEmpty(datestring))
                {
                    if (datestring.Length == 5)
                    {
                        DateTime.TryParseExact(datestring, "ddMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                    }
                    else if (datestring.Length == 7)
                    {
                        DateTime.TryParseExact(datestring, "ddMMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                    }

                    if (!bookingcreateddate.HasValue) { bookingcreateddate = DateTime.Today; }
                    if (date < bookingcreateddate.Value) { date = date.AddYears(1); }
                }

                if (date != DateTime.MinValue)
                {
                    ttl.TTL = date;
                    ttl.TTLDate = date.GetISODateString();
                }

                //Time
                List<string> timepatterns = new List<string>()
                {
                    @"(\d{3,4})[\s\/]?\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}",
                    @"(\d{3,4})[\s\/]?\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]",
                    @"\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}[\s\/]?(\d{3,4})",
                    @"TICKET BY\s(\d{3,4})\/\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}"
                };

                foreach (var patern in timepatterns)
                {
                    ttl.TTLTime = text.LastMatch(patern, "");
                    if (!string.IsNullOrEmpty(ttl.TTLTime)) { break; }
                }

                //Timezone
                List<string> timezonepatterns = new List<string>()
                {
                    @"\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}[\s\/]?\d{3,4}\s([A-Z]*)\sTIME\sZONE",
                    @"\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}[\s\/]?\d{3,4}\s([A-Z]*)\sLT",
                    @"\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}[\s\/]?\d{3,4}\s(GMT)",
                    @"\d{2}[JFMAASOND][AEPUCO][NBRYLGPTVC](\d){0,2}[\s\/]?\d{3,4}(Z)",
                    @"\d{3,4}\s+\d{1,2}[JFMAASOND][AEPUCO][NBRYLGPTVC]\d{2}\s+([A-Z]{3})\s+OR\s+\w{2}\s"
                };

                foreach (var patern in timezonepatterns)
                {
                    ttl.TimeZone = text.LastMatch(patern, "");
                    if (!string.IsNullOrEmpty(ttl.TimeZone)) { break; }
                }

                ttls.Add(ttl);
            });

            //remove items where airline is not presented
            ttls.RemoveAll(r => string.IsNullOrEmpty(r.Airline));

            //Mark the most restrictive TTL
            if (!ttls.IsNullOrEmpty())
            {
                ttls.
                    OrderBy(o => o.TTL).
                    First().
                    MostRestrictive = true;
            }

            return ttls;
        }

        public static async Task<string> InvokeRestAPI(Token token, RestServices service, string requestjson, ILogger logger, string mode = "")
        {
            //Reference: https://beta.developer.sabre.com/guides/travel-agency/how-to/get-token
            if (string.IsNullOrEmpty(requestjson) || string.IsNullOrWhiteSpace(requestjson))
            {
                throw new AeronologyException(string.Format(
                                    "Invalid request json found when executing rest service {0}",
                                    Enum.GetName(typeof(RestServices), service)),
                                    "");
            }

            using (HttpClient client = new HttpClient())
            {
                string url = Constants.GetRestUrl() + GetURLPostfix(service, logger);
                UriBuilder uribuilder = new UriBuilder(url);

                //Append mode to URL query string if required
                if (!string.IsNullOrEmpty(mode))
                {
                    var query = HttpUtility.ParseQueryString(uribuilder.Query);
                    query["mode"] = mode;
                    uribuilder.Query = query.ToString();
                }

                //URL
                var uri = new Uri(uribuilder.ToString());

                //Authorization Header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    token.access_token);

                logger.LogInformation("InvokeRestAPI invoked.");
                var sw = Stopwatch.StartNew();

                // List data response.
                HttpResponseMessage response = await client.PostAsync(uri, new StringContent(requestjson, Encoding.UTF8, "application/json"));

                logger.LogInformation($"InvokeRestAPI completed is {sw.ElapsedMilliseconds} ms.");
                sw.Stop();

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    logger.LogInformation($"Sabre Error: {string.Join(Environment.NewLine, response.Headers.GetValues("Error-Message"))}");
                    logger.LogInformation($"Request JSON: {requestjson.MaskLog()}");
                    throw new GDSException("50000073", string.Join(Environment.NewLine, response.Headers.GetValues("Error-Message")));
                }

                response.EnsureSuccessStatusCode();

                using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    string content = reader.ReadToEnd();

                    if (string.IsNullOrEmpty(content))
                    {
                        throw new GDSException("5000002", $"{Enum.GetName(typeof(RestServices), service)} rest service return no content.");
                    }

                    return content.ReplaceAllSabreSpecialChar();
                }
            }
        }


        public static string GetURLPostfix(RestServices service, ILogger logger)
        {
            switch (service)
            {
                case RestServices.EnhanceIssueTicket:
                    return $"/v{Constants.EnhancedAirTicketVersion}/air/ticket";
                //case RestServices.UpdatePassengerNameRecord:
                //    return $"/v{Constants.UpdatePNRVersion}/passenger/records";
                default:
                    logger.LogInformation($"Service {Enum.GetName(typeof(RestServices), service)} not supported by the GDS");
                    throw new NotImplementedException();
            }
        }
        public enum RestServices
        {
            EnhanceIssueTicket,
            Seatmap,
            UpdatePassengerNameRecord
        }

    }

    public class Token
    {
        //{"access_token":"T1RLAQKmCUAo4apoHPiNN44ErYVQpvpvWxAFBCLBeNCFf2su+FVDqk7jAACwlEZxNdp7AB8+/5OlpCygqz2G8naK2X/Js5iUOV5+E4fVpjSu1dGVqewh7fewMvxNl/frLBmm5woZX21hzC//MxUCDN4yKaLVbeN1ahSSqVQtmy08qneMoSxPemW6/Ub5RJT10A82M2fsYsXCsfdOC7j1z9kZyshGaqQjnJC/3gK2QTjU2ScKHw2qVhUe94AGKVw1DOhSVpWLJV++6uJVnYKBe+lSch43I2rLYrv8/iI*","token_type":"bearer","expires_in":604800}
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
}
