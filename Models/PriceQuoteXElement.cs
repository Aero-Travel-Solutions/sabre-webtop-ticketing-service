using SabreWebtopTicketingService.Common;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

[assembly: InternalsVisibleTo("Aeronology.Sabre.Test")]
namespace SabreWebtopTicketingService.Models
{
    #region Sample Responses
    //Success
    // <PriceQuoteInfo xmlns = "http://www.sabre.com/ns/Ticketing/pqs/1.0" >
    //  < Reservation updateToken="eNc:::5CCG/nWSG6+mKjikHPfDuA==" />
    //  <Summary>
    //    <NameAssociation firstName = "MARY MRS" lastName="SMITH" nameId="1" nameNumber="1.1">
    //      <PriceQuote latestPQFlag = "true" number="1" pricingType="S" status="A" type="PQ">
    //        <Passenger passengerTypeCount = "2" requestedType="ADT" type="ADT" />
    //        <ItineraryType>I</ItineraryType>
    //        <ValidatingCarrier>QF</ValidatingCarrier>
    //        <Amounts>
    //          <Total currencyCode = "AUD" decimalPlace="2">1241.52</Total>
    //        </Amounts>
    //        <LocalCreateDateTime>2019-10-14T13:55:00</LocalCreateDateTime>
    //      </PriceQuote>
    //    </NameAssociation>
    //    <NameAssociation firstName = "CARA MISS" lastName="SMITH" nameId="2" nameNumber="1.2">
    //      <PriceQuote latestPQFlag = "true" number="1" pricingType="S" status="A" type="PQ">
    //        <Passenger passengerTypeCount = "2" requestedType="ADT" type="ADT" />
    //        <ItineraryType>I</ItineraryType>
    //        <ValidatingCarrier>QF</ValidatingCarrier>
    //        <Amounts>
    //          <Total currencyCode = "AUD" decimalPlace="2">1241.52</Total>
    //        </Amounts>
    //        <LocalCreateDateTime>2019-10-14T13:55:00</LocalCreateDateTime>
    //      </PriceQuote>
    //    </NameAssociation>
    //    <NameAssociation firstName = "IAN MSTR" lastName="I/1SMITH" nameId="3" nameNumber="2.1">
    //      <PriceQuote latestPQFlag = "true" number="2" pricingType="S" status="A" type="PQ">
    //        <Passenger passengerTypeCount = "1" requestedType="INF" type="INF" />
    //        <ItineraryType>I</ItineraryType>
    //        <TicketDesignator>IN</TicketDesignator>
    //        <ValidatingCarrier>QF</ValidatingCarrier>
    //        <Amounts>
    //          <Total currencyCode = "AUD" decimalPlace="2">105.00</Total>
    //        </Amounts>
    //        <LocalCreateDateTime>2019-10-14T13:55:00</LocalCreateDateTime>
    //      </PriceQuote>
    //    </NameAssociation>
    //  </Summary>
    //  <Details number = "1" passengerType="ADT" pricingType="S" status="A" type="PQ">
    //    <AgentInfo duty = "*" sine="AAF">
    //      <HomeLocation>9SNJ</HomeLocation>
    //      <WorkLocation>9SNJ</WorkLocation>
    //    </AgentInfo>
    //    <TransactionInfo>
    //      <CreateDateTime>2019-10-13T21:55:00</CreateDateTime>
    //      <UpdateDateTime>2019-10-13T21:55:00</UpdateDateTime>
    //      <LastDateToPurchase>2019-10-22T23:59:00</LastDateToPurchase>
    //      <LocalCreateDateTime>2019-10-14T13:55:00</LocalCreateDateTime>
    //      <LocalUpdateDateTime>2019-10-14T13:55:00</LocalUpdateDateTime>
    //      <InputEntry>WPRQ</InputEntry>
    //    </TransactionInfo>
    //    <NameAssociationInfo firstName = "MARY MRS" lastName="SMITH" nameId="1" nameNumber="1.1" />
    //    <NameAssociationInfo firstName = "CARA MISS" lastName="SMITH" nameId="2" nameNumber="1.2" />
    //    <SegmentInfo number = "1" segmentStatus="OK">
    //      <Flight connectionIndicator = "O" >
    //        < MarketingFlight number="35">QF</MarketingFlight>
    //        <ClassOfService>S</ClassOfService>
    //        <Departure>
    //          <DateTime>2020-02-20T12:10:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 02 - 20T17:05:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Arrival >
    //      </ Flight >
    //      < FareBasis > SSAS </ FareBasis >
    //      < NotValidBefore > 2020 - 02 - 20 </ NotValidBefore >
    //      < NotValidAfter > 2020 - 02 - 20 </ NotValidAfter >
    //      < Baggage allowance="30" type="K" />
    //    </SegmentInfo>
    //    <SegmentInfo number = "2" segmentStatus="OK">
    //      <Flight connectionIndicator = "O" >
    //        < MarketingFlight number="36">QF</MarketingFlight>
    //        <ClassOfService>S</ClassOfService>
    //        <Departure>
    //          <DateTime>2020-03-01T19:40:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 03 - 02T06:00:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Arrival >
    //      </ Flight >
    //      < FareBasis > SSAS </ FareBasis >
    //      < NotValidBefore > 2020 - 03 - 01 </ NotValidBefore >
    //      < NotValidAfter > 2020 - 03 - 01 </ NotValidAfter >
    //      < Baggage allowance="30" type="K" />
    //    </SegmentInfo>
    //    <FareInfo source = "ATPC" >
    //      < FareIndicators />
    //      < BaseFare currencyCode="AUD" decimalPlace="2">1045.00</BaseFare>
    //      <TotalTax currencyCode = "AUD" decimalPlace="2">196.52</TotalTax>
    //      <TotalFare currencyCode = "AUD" decimalPlace="2">1241.52</TotalFare>
    //      <TaxInfo>
    //        <CombinedTax code = "AU" >
    //          < Amount currencyCode="AUD" decimalPlace="2">60.00</Amount>
    //        </CombinedTax>
    //        <CombinedTax code = "WG" >
    //          < Amount currencyCode="AUD" decimalPlace="2">4.14</Amount>
    //        </CombinedTax>
    //        <CombinedTax code = "XT" >
    //          < Amount currencyCode="AUD" decimalPlace="2">132.38</Amount>
    //        </CombinedTax>
    //        <Tax code = "AU" >
    //          < Amount currencyCode="AUD" decimalPlace="2">60.00</Amount>
    //        </Tax>
    //        <Tax code = "WG" >
    //          < Amount currencyCode="AUD" decimalPlace="2">4.14</Amount>
    //        </Tax>
    //        <Tax code = "WY" >
    //          < Amount currencyCode="AUD" decimalPlace="2">43.68</Amount>
    //        </Tax>
    //        <Tax code = "YR" >
    //          < Amount currencyCode="AUD" decimalPlace="2">35.00</Amount>
    //        </Tax>
    //        <Tax code = "SG" >
    //          < Amount currencyCode="AUD" decimalPlace="2">35.40</Amount>
    //        </Tax>
    //        <Tax code = "L7" >
    //          < Amount currencyCode="AUD" decimalPlace="2">11.70</Amount>
    //        </Tax>
    //        <Tax code = "OP" >
    //          < Amount currencyCode="AUD" decimalPlace="2">6.60</Amount>
    //        </Tax>
    //      </TaxInfo>
    //      <FareCalculation>MEL QF SIN355.70QF MEL355.70NUC711.40END ROE1.46891</FareCalculation>
    //      <FareComponent fareBasisCode = "SSAS" number="1">
    //        <FlightSegmentNumbers>
    //          <SegmentNumber>1</SegmentNumber>
    //        </FlightSegmentNumbers>
    //        <FareDirectionality roundTrip = "true" />
    //        < Departure >
    //          < DateTime > 2020 - 02 - 20T12:10:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 02 - 20T17:05:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Arrival >
    //        < Amount currencyCode="NUC" decimalPlace="2">355.70</Amount>
    //        <GoverningCarrier>QF</GoverningCarrier>
    //      </FareComponent>
    //      <FareComponent fareBasisCode = "SSAS" number="2">
    //        <FlightSegmentNumbers>
    //          <SegmentNumber>2</SegmentNumber>
    //        </FlightSegmentNumbers>
    //        <FareDirectionality inbound = "true" roundTrip="true" />
    //        <Departure>
    //          <DateTime>2020-03-01T19:40:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 03 - 02T06:00:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Arrival >
    //        < Amount currencyCode="NUC" decimalPlace="2">355.70</Amount>
    //        <GoverningCarrier>QF</GoverningCarrier>
    //      </FareComponent>
    //    </FareInfo>
    //    <FeeInfo>
    //      <OBFee code = "FCA" noChargeIndicator="X" type="OB">
    //        <Amount currencyCode = "AUD" decimalPlace="2">0.00</Amount>
    //        <Total currencyCode = "AUD" decimalPlace="2">1241.52</Total>
    //        <Description>CC NBR BEGINS WITH 1081</Description>
    //        <BankIdentificationNumber>1081</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FCA" noChargeIndicator="X" type="OB">
    //        <Amount currencyCode = "AUD" decimalPlace="2">0.00</Amount>
    //        <Total currencyCode = "AUD" decimalPlace="2">1241.52</Total>
    //        <Description>CC NBR BEGINS WITH 1611</Description>
    //        <BankIdentificationNumber>1611</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FCA" type="OB">
    //        <Amount currencyCode = "AUD" decimalPlace="2">12.80</Amount>
    //        <Total currencyCode = "AUD" decimalPlace="2">1254.32</Total>
    //        <Description>ANY CC</Description>
    //      </OBFee>
    //      <OBFee code = "FDA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 1241.52 </ Total >
    //        < Description > CC NBR BEGINS WITH 1081</Description>
    //        <BankIdentificationNumber>1081</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FDA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 1241.52 </ Total >
    //        < Description > CC NBR BEGINS WITH 1611</Description>
    //        <BankIdentificationNumber>1611</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FDA" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 4.50 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 1246.02 </ Total >
    //        < Description > ANY CC</Description>
    //      </OBFee>
    //    </FeeInfo>
    //    <MiscellaneousInfo>
    //      <ValidatingCarrier>QF</ValidatingCarrier>
    //      <ItineraryType>I</ItineraryType>
    //    </MiscellaneousInfo>
    //    <MessageInfo>
    //      <Message number = "301" type= "INFO" > One or more form of payment fees may apply</Message>
    //      <Message number = "302" type= "INFO" > Actual total will be based on form of payment used</Message>
    //      <Message number = "201" type= "WARNING" > Fare not guaranteed until ticketed</Message>
    //      <Message type = "WARNING" > VALIDATING CARRIER - QF</Message>
    //      <Message type = "WARNING" > CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES</Message>
    //      <Remarks type = "ENS" > CARRIER RESTRICTIONS APPLY/FEES APPLY</Remarks>
    //      <PricingParameters>WPRQ</PricingParameters>
    //    </MessageInfo>
    //    <HistoryInfo>
    //      <AgentInfo sine = "AAF" >
    //        < HomeLocation > 9SNJ</HomeLocation>
    //      </AgentInfo>
    //      <TransactionInfo>
    //        <LocalDateTime>2019-10-14T13:55:00</LocalDateTime>
    //        <InputEntry>WPRQ</InputEntry>
    //      </TransactionInfo>
    //    </HistoryInfo>
    //  </Details>
    //  <Details number = "2" passengerType= "INF" pricingType= "S" status= "A" type= "PQ" >
    //    < AgentInfo duty= "*" sine= "AAF" >
    //      < HomeLocation > 9SNJ</HomeLocation>
    //      <WorkLocation>9SNJ</WorkLocation>
    //    </AgentInfo>
    //    <TransactionInfo>
    //      <CreateDateTime>2019-10-13T21:55:00</CreateDateTime>
    //      <UpdateDateTime>2019-10-13T21:55:00</UpdateDateTime>
    //      <LastDateToPurchase>2019-10-22T23:59:00</LastDateToPurchase>
    //      <LocalCreateDateTime>2019-10-14T13:55:00</LocalCreateDateTime>
    //      <LocalUpdateDateTime>2019-10-14T13:55:00</LocalUpdateDateTime>
    //      <InputEntry>WPRQ</InputEntry>
    //    </TransactionInfo>
    //    <NameAssociationInfo firstName = "IAN MSTR" lastName= "I/1SMITH" nameId= "3" nameNumber= "2.1" />
    //    < SegmentInfo number= "1" segmentStatus= "NS" >
    //      < Flight connectionIndicator= "O" >
    //        < MarketingFlight number= "35" > QF </ MarketingFlight >
    //        < ClassOfService > S </ ClassOfService >
    //        < Departure >
    //          < DateTime > 2020 - 02 - 20T12:10:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 02 - 20T17:05:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Arrival >
    //      </ Flight >
    //      < FareBasis > SSASIN </ FareBasis >
    //      < NotValidBefore > 2020 - 02 - 20 </ NotValidBefore >
    //      < NotValidAfter > 2020 - 02 - 20 </ NotValidAfter >
    //      < Baggage allowance= "10" type= "K" />
    //    </ SegmentInfo >
    //    < SegmentInfo number= "2" segmentStatus= "NS" >
    //      < Flight connectionIndicator= "O" >
    //        < MarketingFlight number= "36" > QF </ MarketingFlight >
    //        < ClassOfService > S </ ClassOfService >
    //        < Departure >
    //          < DateTime > 2020 - 03 - 01T19:40:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 03 - 02T06:00:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Arrival >
    //      </ Flight >
    //      < FareBasis > SSASIN </ FareBasis >
    //      < NotValidBefore > 2020 - 03 - 01 </ NotValidBefore >
    //      < NotValidAfter > 2020 - 03 - 01 </ NotValidAfter >
    //      < Baggage allowance= "10" type= "K" />
    //    </ SegmentInfo >
    //    < FareInfo source= "ATPC" >
    //      < FareIndicators />
    //      < BaseFare currencyCode= "AUD" decimalPlace= "2" > 105.00 </ BaseFare >
    //      < TotalTax currencyCode= "AUD" decimalPlace= "2" > 0.00 </ TotalTax >
    //      < TotalFare currencyCode= "AUD" decimalPlace= "2" > 105.00 </ TotalFare >
    //      < FareCalculation > MEL QF SIN35.57QF MEL35.57NUC71.14END ROE1.46891</FareCalculation>
    //      <FareComponent fareBasisCode = "SSASIN" number= "1" >
    //        < FlightSegmentNumbers >
    //          < SegmentNumber > 1 </ SegmentNumber >
    //        </ FlightSegmentNumbers >
    //        < FareDirectionality roundTrip= "true" />
    //        < Departure >
    //          < DateTime > 2020 - 02 - 20T12:10:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 02 - 20T17:05:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Arrival >
    //        < Amount currencyCode= "NUC" decimalPlace= "2" > 35.57 </ Amount >
    //        < GoverningCarrier > QF </ GoverningCarrier >
    //        < TicketDesignator > IN </ TicketDesignator >
    //      </ FareComponent >
    //      < FareComponent fareBasisCode= "SSASIN" number= "2" >
    //        < FlightSegmentNumbers >
    //          < SegmentNumber > 2 </ SegmentNumber >
    //        </ FlightSegmentNumbers >
    //        < FareDirectionality inbound= "true" roundTrip= "true" />
    //        < Departure >
    //          < DateTime > 2020 - 03 - 01T19:40:00</DateTime>
    //          <CityCode name = "SINGAPORE" > SIN </ CityCode >
    //        </ Departure >
    //        < Arrival >
    //          < DateTime > 2020 - 03 - 02T06:00:00</DateTime>
    //          <CityCode name = "MELBOURNE" > MEL </ CityCode >
    //        </ Arrival >
    //        < Amount currencyCode= "NUC" decimalPlace= "2" > 35.57 </ Amount >
    //        < GoverningCarrier > QF </ GoverningCarrier >
    //        < TicketDesignator > IN </ TicketDesignator >
    //      </ FareComponent >
    //    </ FareInfo >
    //    < FeeInfo >
    //      < OBFee code= "FCA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 105.00 </ Total >
    //        < Description > CC NBR BEGINS WITH 1081</Description>
    //        <BankIdentificationNumber>1081</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FCA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 105.00 </ Total >
    //        < Description > CC NBR BEGINS WITH 1611</Description>
    //        <BankIdentificationNumber>1611</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FCA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 105.00 </ Total >
    //        < Description > ANY CC</Description>
    //      </OBFee>
    //      <OBFee code = "FDA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 105.00 </ Total >
    //        < Description > CC NBR BEGINS WITH 1081</Description>
    //        <BankIdentificationNumber>1081</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FDA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 105.00 </ Total >
    //        < Description > CC NBR BEGINS WITH 1611</Description>
    //        <BankIdentificationNumber>1611</BankIdentificationNumber>
    //      </OBFee>
    //      <OBFee code = "FDA" noChargeIndicator= "X" type= "OB" >
    //        < Amount currencyCode= "AUD" decimalPlace= "2" > 0.00 </ Amount >
    //        < Total currencyCode= "AUD" decimalPlace= "2" > 105.00 </ Total >
    //        < Description > ANY CC</Description>
    //      </OBFee>
    //    </FeeInfo>
    //    <MiscellaneousInfo>
    //      <ValidatingCarrier>QF</ValidatingCarrier>
    //      <ItineraryType>I</ItineraryType>
    //    </MiscellaneousInfo>
    //    <MessageInfo>
    //      <Message number = "301" type= "INFO" > One or more form of payment fees may apply</Message>
    //      <Message number = "302" type= "INFO" > Actual total will be based on form of payment used</Message>
    //      <Message number = "201" type= "WARNING" > Fare not guaranteed until ticketed</Message>
    //      <Message type = "WARNING" > REQUIRES ACCOMPANYING ADT PASSENGER</Message>
    //      <Message type = "WARNING" > EACH INF REQUIRES ACCOMPANYING ADT PASSENGER</Message>
    //      <Message type = "WARNING" > VALIDATING CARRIER - QF</Message>
    //      <Message type = "WARNING" > CAT 15 SALES RESTRICTIONS FREE TEXT FOUND - VERIFY RULES</Message>
    //      <Remarks type = "ENS" > CARRIER RESTRICTIONS APPLY/FEES APPLY</Remarks>
    //      <PricingParameters>WPRQ</PricingParameters>
    //    </MessageInfo>
    //    <HistoryInfo>
    //      <AgentInfo sine = "AAF" >
    //        < HomeLocation > 9SNJ</HomeLocation>
    //      </AgentInfo>
    //      <TransactionInfo>
    //        <LocalDateTime>2019-10-14T13:55:00</LocalDateTime>
    //        <InputEntry>WPRQ</InputEntry>
    //      </TransactionInfo>
    //    </HistoryInfo>
    //  </Details>
    //</PriceQuoteInfo>

    //Failure
    //<TTL:Error xmlns:TTL="http://www.sabre.com/ns/Ticketing/TTL/1.0" status="Incomplete" timeStamp="2019-10-13T21:54:31" type="Application">
    //  <TTL:Source>PQS WS</TTL:Source>
    //  <TTL:System>TKT-WS</TTL:System>
    //  <TTL:SystemSpecificResults>
    //    <TTL:ErrorMessage code = "47200" > Search error: No Matching Price Quotes found as per the Request.Please amend your request and try again</TTL:ErrorMessage>
    //    <TTL:ShortText>Search error</TTL:ShortText>
    //  </TTL:SystemSpecificResults>
    //</TTL:Error>

    //<TTL:Error xmlns:TTL="http://www.sabre.com/ns/Ticketing/TTL/1.0" status="Incomplete" type="Transport">
    //<TTL:Source>PSS</TTL:Source>
    //<TTL:System>TKT-WS</TTL:System>
    //<TTL:SystemSpecificResults>
    //<TTL:ErrorMessage>ERROR : APPLICATION DATA - NO PQ RECORD EXISTS</TTL:ErrorMessage>
    //<TTL:ShortText>E041</TTL:ShortText>
    //</TTL:SystemSpecificResults>
    //</TTL:Error>
    #endregion


    internal class PriceQuoteXElement
    {
        internal XElement pq;
        public PriceQuoteXElement(XElement pricequote)
        {
            pq = pricequote;
        }

        public bool NoQuotes => pq == null || pq.ToString().Contains("ErrorMessage");

        public bool InvalidQuotesExist => !NoQuotes ?
                                            (pq.GetFirstElement("Summary").GetElements("NameAssociation") != null &&
                                             pq.GetFirstElement("Summary").GetElements("NameAssociation").SelectMany(s => s.GetElements("PriceQuote")) != null) ?
                                                pq.
                                                GetFirstElement("Summary").
                                                GetElements("NameAssociation").
                                                SelectMany(s => s.GetElements("PriceQuote")).
                                                Any(a => (a.GetFirstElement("Indicators") == null ||
                                                        a.GetFirstElement("Indicators").GetAttribute("isExpired") == null) ?
                                                            false :
                                                            bool.Parse(a.GetFirstElement("Indicators").GetAttribute("isExpired").Value)) :
                                                false :
                                            false;

        internal bool ValidQuotesExist => !NoQuotes ?
                                            (pq.GetFirstElement("Summary").GetElements("NameAssociation") != null &&
                                             pq.GetFirstElement("Summary").GetElements("NameAssociation").SelectMany(s => s.GetElements("PriceQuote")) != null) ?
                                                pq.
                                                GetFirstElement("Summary").
                                                GetElements("NameAssociation").
                                                SelectMany(s => s.GetElements("PriceQuote")).
                                                Any(a => (a.GetFirstElement("Indicators") == null ||
                                                        a.GetFirstElement("Indicators").GetAttribute("isExpired") == null) ?
                                                            true :
                                                            bool.Parse(a.GetFirstElement("Indicators").GetAttribute("isExpired").Value)) :
                                                false :
                                            false;

        public List<SabrePriceQuoteSummaryLine> PQSummaryLines => ValidQuotesExist ? 
                                                                    ((from res in pq.GetFirstElement("Summary").GetElements("NameAssociation")
                                                                    let nameassociation = res
                                                                    from q in res.GetElements("PriceQuote")
                                                                    select new SabrePriceQuoteSummaryLine(q, nameassociation)).ToList()):
                                                                    new List<SabrePriceQuoteSummaryLine>();

        public List<SabrePriceQuote> PriceQuotes => ValidQuotesExist ?
                                                    pq.
                                                    GetElements("Details").
                                                    Select(q => new SabrePriceQuote(q)).
                                                    ToList() : 
                                                    new List<SabrePriceQuote>();
    }
}
