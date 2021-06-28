using Microsoft.Extensions.Logging;
using SabreWebtopTicketingService.Common;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Xml;
using System.Xml.Linq;

namespace SabreWebtopTicketingService.Services
{
    public class LoggingMessageInspector : IClientMessageInspector
    {
        public LoggingMessageInspector(ILogger<LoggingMessageInspector> logger)
        {
            Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public LoggingMessageInspector()
        {
            Logger = null; ;
        }
        public ILogger<LoggingMessageInspector> Logger { get; }

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            using (var buffer = reply.CreateBufferedCopy(int.MaxValue))
            {
                var document = GetDocument(buffer.CreateMessage());
                if (Logger == null)
                {
                    Console.WriteLine(document.OuterXml.MaskLog());
                    XmlNodeList nodelist = document.GetElementsByTagName("PriceQuoteInfo");
                    Constants.xml = nodelist.Count > 0 && !nodelist[0].OuterXml.ToString().Contains("ErrorMessage") ?
                                        XElement.Parse(nodelist[0].OuterXml) :
                                        Constants.xml == null ?
                                            null :
                                            Constants.xml;
                }
                else
                {
                    Logger.LogTrace(document.OuterXml);
                }

                reply = buffer.CreateMessage();
            }
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
        {
            using (var buffer = request.CreateBufferedCopy(int.MaxValue))
            {
                var document = GetDocument(buffer.CreateMessage());
                if (Logger == null)
                {
                    Console.WriteLine(document.OuterXml.MaskLog());
                }
                else
                {
                    Logger.LogTrace(document.OuterXml);
                }

                request = buffer.CreateMessage();
                return null;
            }
        }

        private XmlDocument GetDocument(System.ServiceModel.Channels.Message request)
        {
            XmlDocument document = new XmlDocument();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // write request to memory stream
                XmlWriter writer = XmlWriter.Create(memoryStream);
                request.WriteMessage(writer);
                writer.Flush();
                memoryStream.Position = 0;

                // load memory stream into a document
                document.Load(memoryStream);
            }

            return document;
        }
    }
}
