using SabreWebtopTicketingService.Common;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    internal class PQSummary
    {
        /*
            PRICE QUOTE RECORD - SUMMARY BY NAME NUMBER            
                                                               
            RETAINED FARE                                
            NAME    PQ TYPE TKT DES        M/S/A CREATED       TKT TTL     
             1.1  S  1  ADT                  S    17DEC AUD    896.25      
             1.1     2  ADT                  S    17DEC AUD   1612.67      
                                                               
                                                               
            DELETED RECORD EXISTS - *PQD               
        */
        string text = "";

        public PQSummary(string gdstext)
        {
            text = gdstext;
        }

        public List<PQSummaryLine> PQSummaryLines
        {
            get
            {
                //No summary data exist
                if (text.Contains("NO PQ RECORD SUMMARY OR DETAIL EXISTS")) 
                { 
                    return new List<PQSummaryLine>(); 
                }

                List<PQSummaryLine> lines = new List<PQSummaryLine>();
                List<string> pqlines = text.
                                        SplitOn("\n").
                                        Skip(4).
                                        Where(w=> w.IsMatch(@"^\s+\d\.\d")).
                                        ToList();

                pqlines.ForEach(f =>
                {
                    lines.Add(new PQSummaryLine(f));
                });
                return lines;
            }

            private set { }
        }
    }

    public class PQSummaryLine
    {
        /*
             1.1 AERO/JONATHAN MR              S  1 ADT  A  I 17MAR HKD    
             1.1 AERO/JONATHAN MR              S  2 ADT  A  I 25MAR HKD    
             1.1 AERO/JONATHAN MR              S  3 ADT  A  I 25MAR HKD    
             2.1 AERO/MARIA MS                 S  2 ADT  A  I 25MAR HKD    
             2.1 AERO/MARIA MS                 S  3 ADT  A  I 25MAR HKD 
             1.1  S  1  ADT                  S    17DEC AUD    896.25      
             1.1     2  ADT                  S    17DEC AUD   1612.67 
             1.1 SMITH/PETERDENNISMR              1 ADT  A  I 13MAR TWD
             1.1 AERO/JONATHAN MR              S  1 ADT  A  I 17MAR HKD  
             1.1 AERO/DENNIS MR                   1 ADT  A  I 31MAR HKD    
             1.1 AERO/DENNIS MR                   3 ADT  A  I 31MAR HKD    
             2.1 AERO/JENNIFER MS                 1 ADT  A  I 31MAR HKD    
             2.1 AERO/JENNIFER MS                 3 ADT  A  I 31MAR HKD    
             3.1 AERO/MICHELLE MISS*C09           2 C09  A  I 31MAR HKD    
             3.1 AERO/MICHELLE MISS*C09           4 C09  A  I 31MAR HKD
             1.1     1  ADT YB               S    20JUN AUD    273.70 
         */
        string line;
        public PQSummaryLine(string line)
        {
            this.line = line;
        }

        public int PQNumber => int.Parse(line.LastMatch(@"\s*\d\.\d\s+[A-Za-z0-9\/\*\s]*(\d+)\s+\w{3}"));
        public string StoredDate => line.LastMatch(@"(\d{1,2}[A-Z]{3})\s+[A-Z]{3}");
        public string SabreFlag => line.LastMatch(@"\w{3}[\sA-Z]+([MSA])\s+[ID]*\s+\d{1,2}[A-Z]{3}\s+");
        public bool Expired => "M".Contains(SabreFlag);
    }
}
