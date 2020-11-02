using SabreWebtopTicketingService.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Services
{
    internal class TicketImage
    {
        /*
            1ELECTRONIC TICKET RECORD                          CRS CODE:1536
            INV:                  CUST:                          PNR:WJVQAO
            TKT:0819528486219     ISSUED:15OCT20   PCC:9SNJ   IATA:02365775
            NAME:MILLER/PETER MASTER                                       
            NAME REF:I9                                                    
            FORM OF PAYMENT                        FCI: 0                  
            1    QF1234567TEST                                             
            CPN  A/L  FLT  CLS DATE   BRDOFF  TIME  ST F/B             STAT
            1    QF   427   E  08FEB  SYDMEL  900A  NS EDQW45IN        VOID
                                            NVB:08FEB   NVA:08FEB   BAG:   
            2    QF   402   E  20FEB  MELSYD  600A  NS EDQW45IN        VOID
                                            NVB:20FEB   NVA:20FEB   BAG:   
                                                               
            FARE        AUD0.00                                            
            TOTAL        AUD0.00                                           
                                                               
            FARE CALCULATION                                               
            SYD QF MEL0.00QF SYD0.00AUD0.00END                             
                                                               
            SETTLEMENT AUTHORIZATION:  0816H8XZXP74B                       
                                                               
            TAX BREAKDOWN                                                  
            ALL TAXES EXEMPTED                                             
                                                               
            ENDORSEMENT                                                    
            SPECIAL FARE CONDITIONS     
            ****************************************************************
            1ELECTRONIC TICKET RECORD                          CRS CODE:1536
            INV:                  CUST:                          PNR:QIRUWJ
            TKT:0819528486228     ISSUED:16OCT20   PCC:9SNJ   IATA:02365775
            NAME:JONES/HARRY MSTR                                          
            FORM OF PAYMENT                        FCI: 0                  
            1    QF1234567TEST                                             
            CPN  A/L  FLT  CLS DATE   BRDOFF  TIME  ST F/B             STAT
            1    QF   1542  E  06FEB  LSTMEL 1115A  OK EDQW21CH        OPEN
                                            NVB:06FEB   NVA:06FEB   BAG:1PC
                                                               
            FARE       AUD89.83                                            
            TOTAL      AUD115.00                                           
                                                               
            FARE CALCULATION                                               
            LST QF MEL89.83AUD89.83END                                     
                                                               
            TAX BREAKDOWN                                                  
            TAX     14.72QR TAX     10.45UO                                
                                                               
            ENDORSEMENT                                                    
            SPECIAL FARE CONDITIONS               
         */
        private string tktimage;

        public TicketImage(string tktimage)
        {
            this.tktimage = tktimage;
        }

        internal List<string> lines => tktimage.
                                            SplitOn("\n").
                                            ToList();

        internal List<string> couponlines => lines.
                                                SkipWhile(l => !l.StartsWith("CPN ")).
                                                Skip(1).
                                                TakeWhile(t => !t.StartsWith("FARE ")).
                                                Where(w => !string.IsNullOrWhiteSpace(w) && w.IsMatch(@"\d+\s+\w{2}\s+\d{3,4}\s+[A-Z]\s+\d{1,2}[A-Z]{3}")).
                                                ToList();

        public string Route => GetRoute(couponlines);

        public string TicketNumber => lines.First(f => f.StartsWith("TKT:")).LastMatch(@"TKT:(\d{13})");

        private string GetRoute(List<string> couponlines)
        {
            string route = "";

            foreach (var fromto in couponlines.Select(s=> s.LastMatch(@"\d+\s+\w{2}\s+\d{3,4}\s+[A-Z]\s+\d{1,2}[A-Z]{3}\s+([A-Z]{6})")))
            {
                if(string.IsNullOrEmpty(fromto)||fromto.Trim().Length != 6) { continue; }
                string from = fromto.Substring(0, 3);
                string to = fromto.Substring(3);

                if(string.IsNullOrEmpty(route))
                {
                    route += $"{from}-{to}";
                    continue;
                }

                if(from != route.Substring(route.Length - 3))
                {
                    route += "//";
                }

                route += $"-{to}";
            }

            return route;
        }
    }
}