﻿using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SabreWebtopTicketingService.Services
{
    internal static class PnrResponseValidator
    {
        public static void InvokePostPNRRetrivalActions(this PNR pnr)
        {
            Validate(pnr);
            GetWarnings(pnr);
        }

        private static void Validate(this PNR pnr)
        {
            if (pnr == null)
            {
                throw new AeronologyException("PNR_NOT_FOUND", "PNR not found");
            }

            if (pnr.Sectors.IsNullOrEmpty())
            {
                throw new AeronologyException("NO_FLIGHT", "No flights found in the PNR");
            }

            if (pnr.Sectors.All(a => a.Flown))
            {
                throw new AeronologyException("ALL_SECTORS_FLOWN", "All sectors were flown");
            }

            if (!pnr.
                    Sectors.
                    Where(w => w.From != "ARUNK").
                    Any(a => "HK,KK,KL,RR,TK,EK,GK,AK,BK,YK,DS".Contains(a.Status)))
            {
                throw new AeronologyException("NO_LIVE_SECTORS", "No confirmed/live sectors were found in the PNR");
            }
        }

        private static void GetWarnings(this PNR pnr)
        {
            List<WebtopWarning> warnings = new List<WebtopWarning>();

            #region Pax Age validation for CHD/INF
            DateTime FirstDepartureDate = DateTime.Parse(pnr.
                                            Sectors.
                                            Where(w => w.From != "ARUNK").
                                            First().
                                            DepartureDate);

            DateTime LastDepartureDate = DateTime.Parse(pnr.
                                            Sectors.
                                            Where(w => w.From != "ARUNK").
                                            Last().
                                            DepartureDate);

            foreach (var chd in pnr.Passengers.Where(w => w.PaxType == "CHD"))
            {
                if (string.IsNullOrEmpty(chd.DOB)) { continue; }
                DateTime dob = DateTime.Parse(chd.DOB);
                if (dob == DateTime.MinValue) { continue; }

                int chdageatfirstdept = dob.GetAgeAsOfGivenDate(FirstDepartureDate);
                int chdageatlastdept = dob.GetAgeAsOfGivenDate(LastDepartureDate);

                if (chdageatfirstdept > 11 || chdageatfirstdept < 2)
                {
                    //CHD age is incorrect
                    warnings.Add(new WebtopWarning()
                    {
                        code = "INVALID_DOB",
                        message = string.Format("Date of birth for {0} may have entered incorrectly. Please check.", chd.Passengername)
                    });

                }
                else if (chdageatlastdept > 11 && chdageatfirstdept < chdageatlastdept)
                {
                    //Check CHD become ADT during flight
                    warnings.Add(new WebtopWarning()
                    {
                        code = "PAX_TURN_ONE",
                        message = string.Format("Passenger {0} will turn {1} years old while travelling. Please check with the plating carrier before ticketing.", chd.Passengername, chdageatlastdept)
                    });
                }

                //INF 
            }
            #endregion

            #region Sector Validation
            if (pnr.UnconfirmedSectorsExist)
            {
                string unconfimedsecs = string.Join(",",
                                           pnr.
                                            Sectors.
                                            Where(a => !(a.Status.IsNullOrEmpty() || "HK,KK,KL,RR,TK,EK".Contains(a.Status))).
                                            Select(s => s.SectorNo).
                                            Distinct());

                warnings.Add(new WebtopWarning()
                {
                    code = "HX_SECTOR",
                    message = string.Format("Unconfirmed sectors {0} found. Please confirm the sectors before issuing.", unconfimedsecs)
                });
            }
            #endregion

            pnr.Warnings = warnings;
        }
    }
}
