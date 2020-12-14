using Newtonsoft.Json;
using SabreWebtopTicketingService.Common;
using SabreWebtopTicketingService.CustomException;
using SabreWebtopTicketingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UpdatePNRService;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SabreWebtopTicketingService.Services
{
    public class SabreUpdatePNRService : ConnectionStubs, IDisposable
    {
        private readonly SessionDataSource sessionData;
        private readonly ILogger logger;
        private readonly string url;

        public SabreUpdatePNRService(
            SessionDataSource sessionData,
            ILogger logger)
        {
            this.sessionData = sessionData;
            this.logger = logger;
            url = Constants.GetSoapUrl();
        }

        public async Task<List<SeatResponse>> BookSeat(Token token, string agentpcc, BookSeatRequest rq)
        {
            //generate request
            UpdatePassengerNameRecordRQ UpdatePassengerNameRecordRQ = GetBookSeatRequest(agentpcc, rq);

            //generate json request
            string json = JsonConvert.
                            SerializeObject(
                                new { UpdatePassengerNameRecordRQ },
                                Formatting.Indented,
                                new JsonSerializerSettings()
                                {
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    NullValueHandling = NullValueHandling.Ignore
                                });

            logger.LogInformation($"Book Seat Request:{json.Mask()}");
            //invoke update passenger name record REST service
            string response = await SabreSharedServices.InvokeRestAPI(token, SabreSharedServices.RestServices.UpdatePassengerNameRecord, json, logger, "update");
            logger.LogMaskInformation($"Book Seat Response:{response}");

            //Retry if needed
            if (response.Contains("NUMBER OF SEATS DOES NOT EQUAL NUMBER IN PARTY FOR SEGMENT") ||
                                     response.Contains("NR IN PTY"))
            {
                response = await SabreSharedServices.InvokeRestAPI(token, SabreSharedServices.RestServices.UpdatePassengerNameRecord, json, logger, "update");
            }

            return ParseSeatBookRequest(response, rq);
        }

        private List<SeatResponse> ParseSeatBookRequest(string response, BookSeatRequest rq)
        {
            var res = JsonSerializer.
                            Deserialize<UpdatePassengerNameRecordRqResponse>(
                                response,
                                new System.Text.Json.JsonSerializerOptions()
                                {
                                    PropertyNameCaseInsensitive = true
                                });

            UpdatePassengerNameRecordRS seatres = res.UpdatePassengerNameRecordRS;

            if (seatres.ApplicationResults.status != "Complete")
            {
                //Errors
                List<string> messages = seatres.
                                    ApplicationResults.
                                    Error.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    Select(s=> s.content).
                                    Where(w=> !w.Contains("PNR has not been updated successfully, see remaining messages for details")).
                                    Distinct().
                                    ToList();
                //Warnings
                messages.
                    AddRange(
                        GetWarningsFromBookSeatResponse(
                            seatres.
                                    ApplicationResults.
                                    Warning.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    Select(s => s.content).
                                    Distinct(),
                            null,
                            false));

                messages = messages.Distinct().ToList();

                int indexofunabletoprocess = messages.FindIndex(f => f.Contains("UNABLE TO PROCESS"));
                if(indexofunabletoprocess >= 0)
                {
                    messages[indexofunabletoprocess] = "Unable to Process, Carrier cannot process this request. Please contact Airline.";
                }

                throw new GDSException(
                            "50000067",
                            string.Join(Environment.NewLine, messages));
            }

            List<SeatResponse> bookSeatResponse = new List<SeatResponse>();

            if (seatres.ApplicationResults.Warning == null)
            {
                bookSeatResponse = rq.
                                    BookSeats.
                                    Select(s => new SeatResponse()
                                    {
                                        SeatNumber = s.SeatNumber,
                                        Success = true,
                                        AncillaryBooked = false,
                                        Warnings = new List<string>()
                                    }).ToList();
            }
            else
            {
                bookSeatResponse = seatres.
                                        ApplicationResults.
                                        Warning.
                                        Select(m => m.SystemSpecificResults).
                                        Select((seat, index) => new SeatResponse()
                                        {
                                            SeatNumber = rq.BookSeats[(index > rq.BookSeats.Count - 1) ? rq.BookSeats.Count - 1 : index].SeatNumber,
                                            Success = seat.
                                                        SelectMany(m => m.Message).
                                                        Select(m => m.content).
                                                        Any(a => a.Contains("SEAT PRS ASSIGNED") ||
                                                                    a.Contains("PAYMENT REQUIRED OR SEAT IS SUBJECT TO CANCELLATION BY CARRIER")),
                                            AncillaryBooked = seat.
                                                                SelectMany(m => m.Message).
                                                                Select(m => m.content).
                                                                Any(a => a.Contains(
                                                                    "PAYMENT REQUIRED OR SEAT IS SUBJECT TO CANCELLATION BY CARRIER")),
                                            Warnings = GetWarningsFromBookSeatResponse(
                                                            seat.
                                                            SelectMany(m => m.Message).
                                                            Select(m => m.content),
                                                            rq.BookSeats[(index > rq.BookSeats.Count - 1) ? rq.BookSeats.Count - 1 : index])
                                        }).
                                        ToList();
            }

            //rework the sucess flag
            bookSeatResponse.
                Where(w => !w.Success).
                ToList().
                ForEach(f => f.Success = f.Warnings.IsNullOrEmpty());

            //grouping results
            bookSeatResponse = bookSeatResponse.
                                    GroupBy(grp => grp.SeatNumber).
                                    Select(s => new SeatResponse()
                                    {
                                        SeatNumber = s.Key,
                                        AncillaryBooked = s.First().AncillaryBooked,
                                        Success = s.First().Success,
                                        Warnings = s.SelectMany(m => m.Warnings).Distinct().ToList()
                                    }).
                                    ToList();

            if(bookSeatResponse.All(a=> !a.Success))
            {
                throw new GDSException("30000063", string.Join(",", bookSeatResponse.SelectMany(s => s.Warnings).Distinct()));
            }

            return bookSeatResponse;
        }

        private static List<string> RewordWarnings(SeatData rq, List<string> warnings)
        {
            if (warnings.IsNullOrEmpty()) { return new List<string>(); }

            int index = warnings.FindIndex(w => w.StartsWith("PAYMENT REQUIRED OR SEAT IS SUBJECT TO CANCELLATION BY CARRIER"));
            if (index >= 0)
            {
                warnings[index] = $"Seat confirmation require payment. Please issue the EMD or seats will be subjected to cancellation by airline.";
            }

            index = warnings.FindIndex(w => w.StartsWith("‡NR IN PTY‡"));
            if (index >= 0)
            {
                warnings[index] = $"Seat allocation for all passenger required by airline. Please consider booking seats for all passengers.";
            }

            index = warnings.FindIndex(f => f.Trim().IsMatch(@"^UNABLE TO PROCESS$"));
            if (index >= 0)
            {
                warnings[index] = "Unable to Process, Carrier cannot process this request. Please contact Airline.";
            }

            index = warnings.FindIndex(f => f.Contains("PRS NOT ALLOWED ON THIS SEAT"));
            if (index >= 0)
            {
                warnings[index] = "Pre-reserved seat is not available for this service.";
            }

            if (rq != null)
            {
                index = warnings.FindIndex(w => w.StartsWith("‡PRS PREVIOUSLY ASSIGNED‡"));
                if (index >= 0)
                {
                    warnings[index] = $"Seat already booked for passeger {rq.NameNumber}, sector {rq.SegmentNumber}.";
                }
            }

            return warnings;
        }

        private List<string> GetWarningsFromBookSeatResponse(IEnumerable<string> messagetext, SeatData rq, bool statuscomplete = true)
        {
            List<string> excludedwarnings = new List<string>()
            {
                "‡SEAT PRS ASSIGNED‡",
                "TTY REQ PEND",
                "INSERT C IN 2ND SEAT REQUEST",
                "AIRCRAFT CHG ENROUTE/SEAT PARTIALLY ASSIGNED/",
                "UNABLE TO PROCESS LAST SEG INTERACTIVELY, ET TO SEND TTY",
                "AirSeatLLSRQ: AIRCRAFT CHG ENROUTE/SEAT PARTIALLY ASSIGNED/",
                "EndTransactionLLSRQ: AIRCRAFT CHANGE SEGMENT 01 - USE 4G OR 4GC TO COMPLETE SEATS",
                "AirSeatLLSRQ: UNABLE TO PROCESS LAST SEG INTERACTIVELY, ET TO SEND TTY"
            };

            if(!statuscomplete)
            {
                excludedwarnings.Add("‡NR IN PTY‡");
            }

            var warnings = messagetext.
                                Where(w => !w.IsMatch(@"[A-Z0-9]{2}\s*[0-9]{1,4}[A-Z]\s*[0-9]{1,2}[A-Z]{9}")).
                                Where(w => excludedwarnings.All(a => !w.Contains(a))).
                                Select(s => s.ReplaceAllSabreSpecialChar()).
                                Select(s => s.SplitOn("AirSeatLLSRQ:").Last().Trim()).
                                Select(s => s.SplitOn("EndTransactionLLSRQ:").Last().Trim()).
                                Select(s => s.SplitOn("CreatePassengerNameRecordRQ:").Last().Trim()).
                                Distinct().
                                ToList();

            return RewordWarnings(rq, warnings);
        }

        private static UpdatePassengerNameRecordRQ GetBookSeatRequest(string agentpcc, BookSeatRequest rq)
        {
            return new UpdatePassengerNameRecordRQ()
            {
                version = Constants.UpdatePNRVersion,
                targetCity = agentpcc,
                Itinerary = new UpdatePassengerNameRecordRQItinerary()
                {
                    id = rq.Locator
                },
                SpecialReqDetails = new UpdatePassengerNameRecordRQSpecialReqDetails()
                {
                    AirSeat = new UpdatePassengerNameRecordRQSpecialReqDetailsAirSeat()
                    {
                        Seats = new Seats()
                        {
                            Seat = rq.
                                    BookSeats.
                                    Where(w=> !string.IsNullOrEmpty(w.SeatNumber)).
                                    Select(seat => new UpdatePassengerNameRecordRQSpecialReqDetailsAirSeatSeat()
                                    {
                                        NameNumber = seat.NameNumber,
                                        SegmentNumber = seat.SegmentNumber,
                                        Number = seat.SeatNumber
                                    }).
                                ToArray()
                        }
                    }
                },
                PostProcessing = new UpdatePassengerNameRecordRQPostProcessing()
                {
                    EndTransaction = new UpdatePassengerNameRecordRQPostProcessingEndTransaction()
                    {
                        Source = new UpdatePassengerNameRecordRQPostProcessingEndTransactionSource()
                        {
                            ReceivedFrom = "Aeronology"
                        }
                    }
                }
            };
        }

        public async Task<List<BookedSSR>> BookSpecialService(Token token, string agentpcc, BookSpecialServiceRequest rq)
        {
            //generate request
            UpdatePassengerNameRecordRQ UpdatePassengerNameRecordRQ = GetBookSpecialServiceRequest(agentpcc, rq);

            //generate json request
            string json = JsonConvert.
                            SerializeObject(
                                new { UpdatePassengerNameRecordRQ },
                                Formatting.Indented,
                                new JsonSerializerSettings()
                                {
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    NullValueHandling = NullValueHandling.Ignore
                                });

            //invoke update passenger name record REST service
            string response = await SabreSharedServices.InvokeRestAPI(token, SabreSharedServices.RestServices.UpdatePassengerNameRecord, json, logger, "update");

            //parse response
            return ParseBookSSRResponse(response, rq.ServiceData);
        }

        private List<BookedSSR> ParseBookSSRResponse(string response, List<ServiceData> serviceData)
        {
            var res = JsonSerializer.
                            Deserialize<UpdatePassengerNameRecordRqResponse>(
                                response,
                                new System.Text.Json.JsonSerializerOptions()
                                {
                                    PropertyNameCaseInsensitive = true
                                });

            UpdatePassengerNameRecordRS ssrres = res.UpdatePassengerNameRecordRS;

            if (ssrres.ApplicationResults.status != "Complete")
            {
                //Errors
                List<string> messages = ssrres.
                                    ApplicationResults.
                                    Error.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    Select(s => s.content).
                                    Where(w => !w.Contains("PNR has not been updated successfully, see remaining messages for details")).
                                    Distinct().
                                    ToList();
                //Warnings
                messages.
                    AddRange(
                        GetWarningsFromBookSSRResponse(
                            ssrres.
                                    ApplicationResults.
                                    Warning.
                                    SelectMany(s => s.SystemSpecificResults).
                                    SelectMany(s => s.Message).
                                    Select(s => s.content).
                                    Distinct(),
                            null));

                messages = messages.Distinct().ToList();

                int indexofunabletoprocess = messages.FindIndex(f => f.Contains("UNABLE TO PROCESS"));
                if (indexofunabletoprocess >= 0)
                {
                    messages[indexofunabletoprocess] = "Unable to Process, Carrier cannot process this request. Please contact Airline.";
                }

                throw new GDSException(
                            "50000067",
                            string.Join(Environment.NewLine, messages));
            }

            List<BookedSSR> bookSSRResponse = new List<BookedSSR>();
            var warningmsgs = ssrres.ApplicationResults.Warning?.SelectMany(s => s.SystemSpecificResults)?.SelectMany(s => s.Message)?.Select(s => s.content).ToList();
            if (warningmsgs.IsNullOrEmpty() || (warningmsgs.Count() == 1 && warningmsgs.First() == "EndTransactionLLSRQ: TTY REQ PEND"))
            {
                bookSSRResponse = serviceData.
                                    Select(s => new BookedSSR()
                                    {
                                        SSRCode = s.SSRCode,
                                        Carrier = s.Carrier,
                                        NameNumber = s.NameNumber,
                                        SegmentNumber = s.SegmentNumber,
                                        AllSectors = s.AllSectors,
                                        OSI = s.OSI,
                                        Success = true,
                                        Warnings = new List<string>(),
                                        PassengerDetails = s.PassengerDetails,
                                        FreeText = serviceData.
                                                        First(w=> w.SSRCode == s.SSRCode && w.NameNumber == s.NameNumber).
                                                        SpecialText
                                    }).ToList();
            }
            else
            {
                bookSSRResponse = ssrres.
                                        ApplicationResults.
                                        Warning.
                                        Select(m => m.SystemSpecificResults).
                                        Select((ssr, index) => new BookedSSR()
                                        {
                                            SSRCode = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].SSRCode,
                                            Carrier = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].Carrier,
                                            NameNumber = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].NameNumber,
                                            SegmentNumber = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].SegmentNumber,
                                            AllSectors = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].AllSectors,
                                            OSI = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].OSI,
                                            Success = ssr.
                                                        SelectMany(m => m.Message).
                                                        Select(m => m.content).
                                                        Any(a => a.Contains("EndTransactionLLSRQ: TTY REQ PEND")),
                                            Warnings = GetWarningsFromBookSSRResponse(
                                                            ssr.
                                                            SelectMany(m => m.Message).
                                                            Select(m => m.content),
                                                            serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index]),
                                            PassengerDetails = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].PassengerDetails,
                                            FreeText = serviceData[(index > serviceData.Count - 1) ? serviceData.Count - 1 : index].SpecialText
                                        }).
                                        ToList();
            }

            //grouping results
            bookSSRResponse = bookSSRResponse.
                                    GroupBy(grp => new { grp.SSRCode, grp.NameNumber, grp.SegmentNumber, grp.AllSectors, grp.Carrier, grp.OSI, grp.FreeText }).
                                    Select(s => new BookedSSR()
                                    {
                                        SSRCode = s.Key.SSRCode,
                                        Carrier = s.Key.Carrier,
                                        NameNumber = s.Key.NameNumber,
                                        SegmentNumber = s.Key.SegmentNumber,
                                        AllSectors = s.Key.AllSectors,
                                        OSI = s.Key.OSI,
                                        Success = s.First().Success,
                                        Warnings = s.SelectMany(m => m.Warnings).Distinct().ToList(),
                                        PassengerDetails = s.First().PassengerDetails,
                                        FreeText = s.Key.FreeText
                                    }).
                                    ToList();

            bookSSRResponse.Where(w => !w.Success).ToList().ForEach(f => f.Success = f.Warnings.IsNullOrEmpty());
            
            if (bookSSRResponse.All(a => !a.Success))
            {
                throw new GDSException("30000067", string.Join(",", bookSSRResponse.SelectMany(s => s.Warnings).Distinct()));
            }

            return bookSSRResponse;
        }

        private List<string> GetWarningsFromBookSSRResponse(IEnumerable<string> reswarnings, ServiceData serviceData)
        {
            List<string> excludederrors = new List<string>()
            {
                "TTY REQ PEND",
                "INSERT C IN 2ND SEAT REQUEST",
                "UNABLE TO PROCESS LAST SEG INTERACTIVELY, ET TO SEND TTY",
                "AirSeatLLSRQ: AIRCRAFT CHG ENROUTE/SEAT PARTIALLY ASSIGNED/",
                "EndTransactionLLSRQ: AIRCRAFT CHANGE SEGMENT 01 - USE 4G OR 4GC TO COMPLETE SEATS",
                "AirSeatLLSRQ: UNABLE TO PROCESS LAST SEG INTERACTIVELY, ET TO SEND TTY"
            };

            List<string> warnings = new List<string>();

            warnings = reswarnings.
                            Where(w => !w.StartsWith("3")).
                            Select(s => s.SplitOn(".NOT ENT BGNG WITH").First()).
                            Select(s => s.SplitOn("SpecialServiceLLSRQ:").Last()).
                            Select(s => s.SplitOn("EndTransactionLLSRQ:").Last()).
                            Select(s => s.SplitOn(",").First().Trim()).
                            Select(s => s.TrimStart(new char[] { '.' })).
                            Where(w => !excludederrors.Contains(w)).
                            Distinct().
                            ToList();

            RewordBookSSRWarnings(warnings);

            return warnings;
        }

        private static void RewordBookSSRWarnings(List<string> warnings)
        {
            int index = warnings.FindIndex(w => w.Contains("MEAL ORDER EXISTS THIS CUSTOMER"));
            if (index >= 0)
            {
                warnings[index] = "Meal order exists, please consider cancelling the service before requesting another meal type.";
            }

            index = warnings.FindIndex(w => w.Contains("INVALID FREE TEXT CHARACTERS. MODIFY AND RE-ENTER"));
            if (index >= 0)
            {
                warnings[index] = "Invalid free text, these characters are not supported ! # $ % & * ( ) + ¥ [ ] { } ; : , < > ? ¤ ¶ . Please modify.";
            }
        }

        private UpdatePassengerNameRecordRQ GetBookSpecialServiceRequest(string agentpcc, BookSpecialServiceRequest rq)
        {
            return new UpdatePassengerNameRecordRQ()
            {
                version = Constants.UpdatePNRVersion,
                targetCity = agentpcc,
                Itinerary = new UpdatePassengerNameRecordRQItinerary()
                {
                    id = rq.Locator
                },
                SpecialReqDetails = new UpdatePassengerNameRecordRQSpecialReqDetails()
                {
                    SpecialService = new UpdatePassengerNameRecordRQSpecialReqDetailsSpecialService()
                    {
                        SpecialServiceInfo = new UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfo()
                        {
                            Service = GetServiceArray(rq.ServiceData)
                        }
                    } 
                },
                PostProcessing = new UpdatePassengerNameRecordRQPostProcessing()
                {
                    EndTransaction = new UpdatePassengerNameRecordRQPostProcessingEndTransaction()
                    {
                        Source = new UpdatePassengerNameRecordRQPostProcessingEndTransactionSource()
                        {
                            ReceivedFrom = "Aeronology"
                        }
                    }
                }
            };
        }

        private UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoService[] GetServiceArray(List<ServiceData> serviceData)
        {
            List<UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoService> services = new List<UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoService>();

            foreach (var service in serviceData)
            {
                services.Add(
                new UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoService()
                {
                    SSR_Code = service.OSI ? "OSI" : service.SSRCode,
                    PersonName = service.SSRCode == "CLID"?
                    null:
                    new UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoServicePersonName()
                    {
                        NameNumber = service.NameNumber
                    },
                    SegmentNumber = service.OSI ? "" : service.SegmentNumber,
                    VendorPrefs = service.OSI ?
                                    new UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoServiceVendorPrefs()
                                    {
                                        Airline = new UpdatePassengerNameRecordRQSpecialReqDetailsSpecialServiceSpecialServiceInfoServiceVendorPrefsAirline()
                                        {
                                            Code = string.IsNullOrEmpty(service.Carrier) ?
                                                        "YY" :
                                                        service.Carrier
                                        }
                                    } :
                                    null,
                    Text = GetFreeText(service)
                });
            }

            return services.ToArray();
        }

        private string GetFreeText(ServiceData service)
        {
            return service.SSRCode switch
            {
                "INFT" => $"{service.PassengerDetails.LastName}/{service.PassengerDetails.FirstName}/{Convert.ToDateTime(service.PassengerDetails.DateOfBirth):ddMMMyy}",
                "CHLD" => $"{Convert.ToDateTime(service.PassengerDetails.DateOfBirth):ddMMMyy}",
                _ => ReplaceSpecialCharacters(service.SpecialText),
            };
        }

        private string ReplaceSpecialCharacters(string specialText)
        {
            string result = specialText;
            result = result.Replace("@", "//");
            result = result.Replace("_", "..");
            result = result.Replace("-", "./");
            return result;
        }

        public async Task AddGeneralDOBRemarks(Token token, string agentpcc, AddRemarkRequest rq)
        {
            try
            {
                //generate request
                UpdatePassengerNameRecordRQ UpdatePassengerNameRecordRQ = GetAddGeneralDOBRemarkRequest(agentpcc, rq);

                await AddRemarks(token, UpdatePassengerNameRecordRQ);
            }
            catch(Exception ex)
            {
                logger.LogError(ex.Message, ex);
                //error not thrown as its not a required operation to alert user
            }
        }

        public async Task AddGeneralAGTCOMMRemarks(Token token, string agentpcc, AddRemarkRequest rq)
        {
            try
            {
                //generate request
                UpdatePassengerNameRecordRQ UpdatePassengerNameRecordRQ = GetAddGeneralAGTCOMMRemarkRequest(agentpcc, rq);

                await AddRemarks(token, UpdatePassengerNameRecordRQ);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                //error not thrown as its not a required operation to alert user
            }
        }

        private async Task AddRemarks(Token token, UpdatePassengerNameRecordRQ UpdatePassengerNameRecordRQ)
        {
            //generate json request
            string json = JsonConvert.
                            SerializeObject(
                                new { UpdatePassengerNameRecordRQ },
                                Formatting.Indented,
                                new JsonSerializerSettings()
                                {
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    NullValueHandling = NullValueHandling.Ignore,
                                    StringEscapeHandling = StringEscapeHandling.Default,
                                    Converters = new List<JsonConverter>()
                                    {
                                            new Newtonsoft.Json.Converters.StringEnumConverter()
                                    }
                                });

            //invoke update passenger name record REST service
            string response = await SabreSharedServices.InvokeRestAPI(token, SabreSharedServices.RestServices.UpdatePassengerNameRecord, json, logger, "update");
        }

        //public async Task AddAgentCommissionRemarks(Token token, string agentpcc, decimal agentcommissionrate)
        //{
        //    try
        //    {
        //        //generate request
        //        UpdatePassengerNameRecordRQ UpdatePassengerNameRecordRQ = GetAddGeneralRemarkRequest(agentpcc, rq);

        //        //generate json request
        //        string json = JsonConvert.
        //                        SerializeObject(
        //                            new { UpdatePassengerNameRecordRQ },
        //                            Formatting.Indented,
        //                            new JsonSerializerSettings()
        //                            {
        //                                DefaultValueHandling = DefaultValueHandling.Ignore,
        //                                NullValueHandling = NullValueHandling.Ignore,
        //                                StringEscapeHandling = StringEscapeHandling.Default,
        //                                Converters = new List<JsonConverter>()
        //                                {
        //                                    new Newtonsoft.Json.Converters.StringEnumConverter()
        //                                }
        //                            });

        //        //invoke update passenger name record REST service
        //        string response = await SabreSharedService.InvokeRestAPI(token, SabreSharedService.RestServices.UpdatePassengerNameRecord, json, logger, "update");
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex.Message, ex);
        //        //error not thrown as its not a required operation to alert user
        //    }
        //}

        private UpdatePassengerNameRecordRQ GetAddGeneralDOBRemarkRequest(string agentpcc, AddRemarkRequest rq)
        {
            return new UpdatePassengerNameRecordRQ()
            {
                version = Constants.UpdatePNRVersion,
                targetCity = agentpcc,
                Itinerary = new UpdatePassengerNameRecordRQItinerary()
                {
                    id = rq.Locator
                },
                SpecialReqDetails = new UpdatePassengerNameRecordRQSpecialReqDetails()
                {
                    AddRemark = new UpdatePassengerNameRecordRQSpecialReqDetailsAddRemark()
                    {
                        RemarkInfo = new UpdatePassengerNameRecordRQSpecialReqDetailsAddRemarkRemarkInfo()
                        {
                            Remark = rq.
                                        Remarks.
                                        Select(r => new UpdatePassengerNameRecordRQSpecialReqDetailsAddRemarkRemarkInfoRemark()
                                        {
                                            Type = UpdatePassengerNameRecordRQSpecialReqDetailsAddRemarkRemarkInfoRemarkType.General,
                                            Code = r.Code,
                                            SegmentNumber = r.SegmentNumber,
                                            Text = r.RemarkText,
                                        }).
                                        ToArray()
                        }
                    }
                },
                PostProcessing = new UpdatePassengerNameRecordRQPostProcessing()
                {
                    EndTransaction = new UpdatePassengerNameRecordRQPostProcessingEndTransaction()
                    {
                        Source = new UpdatePassengerNameRecordRQPostProcessingEndTransactionSource()
                        {
                            ReceivedFrom = "Aeronology"
                        }
                    }
                }
            };
        }

        private UpdatePassengerNameRecordRQ GetAddGeneralAGTCOMMRemarkRequest(string agentpcc, AddRemarkRequest rq)
        {
            return new UpdatePassengerNameRecordRQ()
            {
                version = Constants.UpdatePNRVersion,
                targetCity = agentpcc,
                Itinerary = new UpdatePassengerNameRecordRQItinerary()
                {
                    id = rq.Locator
                },
                SpecialReqDetails = new UpdatePassengerNameRecordRQSpecialReqDetails()
                {
                    AddRemark = new UpdatePassengerNameRecordRQSpecialReqDetailsAddRemark()
                    {
                        RemarkInfo = new UpdatePassengerNameRecordRQSpecialReqDetailsAddRemarkRemarkInfo()
                        {
                            Remark = rq.
                                        Remarks.
                                        Select(r => new UpdatePassengerNameRecordRQSpecialReqDetailsAddRemarkRemarkInfoRemark()
                                        {
                                            Type = UpdatePassengerNameRecordRQSpecialReqDetailsAddRemarkRemarkInfoRemarkType.General,
                                            Code = r.Code,
                                            Text = r.RemarkText,
                                        }).
                                        ToArray()
                        }
                    }
                },
                PostProcessing = new UpdatePassengerNameRecordRQPostProcessing()
                {
                    EndTransaction = new UpdatePassengerNameRecordRQPostProcessingEndTransaction()
                    {
                        Source = new UpdatePassengerNameRecordRQPostProcessingEndTransactionSource()
                        {
                            ReceivedFrom = "Aeronology"
                        }
                    }
                }
            };
        }


        public void Dispose()
        {

        }
    }

    public class BookedSSR
    {
        public string SSRCode { get; set; }
        public string NameNumber { get; set; }
        public string SegmentNumber { get; set; }
        public bool AllSectors { get; set; }
        public string Carrier { get; set; }
        public bool OSI { get; set; }
        public List<string> Warnings { get; set; }
        public bool Success { get; set; }
        public string FreeText { get; set; }
        [JsonIgnore]
        public PassengerDetail PassengerDetails { get; set; }
    }

    public class SeatResponse
    {
        public string SeatNumber { get; set; }
        public List<string> Warnings { get; set; }
        public bool Success { get; set; }
        public bool AncillaryBooked { get; set; }
    }


    #region UpdatePassengerNameRecordResponse JSON proxy
    public class ItineraryRef
    {
        public string ID { get; set; }
    }

    public class UpdatePassengerNameRecordRS
    {
        public ApplicationResults ApplicationResults { get; set; }
        public ItineraryRef ItineraryRef { get; set; }
    }


    public class UpdatePassengerNameRecordRqResponse
    {
        public UpdatePassengerNameRecordRS UpdatePassengerNameRecordRS { get; set; }
        public List<Link> Links { get; set; }
    }
    #endregion
}
