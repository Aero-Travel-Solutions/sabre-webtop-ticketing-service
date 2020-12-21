using SabreWebtopTicketingService.Models;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace SabreWebtopTicketingService.Common
{
    public class ConsolidatorPccDataSource
    {
        private readonly LambdaHelper lambda;
        private readonly SessionDataSource session;
        private readonly string PFX_ADB;

        public ConsolidatorPccDataSource(LambdaHelper lambda, SessionDataSource session)
        {
            this.lambda = lambda;
            this.session = session;
            PFX_ADB = $"agent-database-{Environment.GetEnvironmentVariable("ENVIRONMENT")??"dev"}";
        }

        public async Task<Pcc> GetWebServicePccByGdsCode(string gdsCode, string contextID, string sessionid)
        {
            ConsolidatorPccList list = await RetrieveWebServicePccs(contextID, sessionid);
            if (list==null || list.PccList.Count() == 0)
            {
                //Connecxes
                //return new Pcc()
                //{
                //    Username = "3666",
                //    Password = "CTLWS20",
                //    PccCode = "R6G8",
                //    GdsCode = "1W"
                //};
                //ETG
                return new Pcc()
                {
                    Username = "381507",
                    Password = "EVAQWE20",
                    PccCode = "F7Z7",
                    GdsCode = "1W"
                };
                //Global Travel
                //return new Pcc()
                //{
                //    Username = "514203",
                //    Password = "gl0b4l20",
                //    PccCode = "5DXJ",
                //    GdsCode = "1W"
                //};
                //ACN
                //return new Pcc()
                //{
                //    Username = "539206",
                //    Password = "73WS0581",
                //    PccCode = "0M4J",
                //    GdsCode = "1W"
                //};
                return new Pcc()
                {
                    Username = "420817",
                    Password = "a7med01",
                    PccCode = "G4AK",
                    GdsCode = "1W"
                };
            }
            return list.PccList.FirstOrDefault(p => p.GdsCode == gdsCode);
        }

        private async Task<ConsolidatorPccList> RetrieveWebServicePccs(string contextID, string sessionid)
        {
            var consolidator_id = (await session.GetSessionUser(sessionid))?.ConsolidatorId;
            if (string.IsNullOrEmpty(consolidator_id)) 
            {
                throw new Exception("ConsolidatorID not found!"); 
            }
 
            var result = await lambda.Invoke<Pcc[]>($"{PFX_ADB}-pcc-details-list", new { consolidator_id }, sessionid);
            return new ConsolidatorPccList { PccList = result };
        }
    }
}
