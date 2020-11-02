﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SabreWebtopTicketingService.Services
{
    internal static class Constants
    {
        //Sabre special characters
        public const char CROSS_OF_LORRAINE_CHAR = (char)0xA5;
        public const char CHANGE_KEY_CHAR = (char)0xA4;
        public const char END_ITEM_CHAR = (char)0xA7;

        //SOAP web service versions
        public const string SessionCreateVersion = "1.0.0";
        public const string SessionCloseVersion = "1.0.1";
        public const string GetReservationVersion = "1.19.0";
        public const string IgnoreTransactionVersion = "2.0.0";
        public const string TKT_ElectronicDocumentVersion = "1.0.0";
        public const string SabreCommandVersion = "1.8.1";

        public static string GetSoapUrl()
        {
            //CERT
            //return Environment.GetEnvironmentVariable(URL_KEY_SOAP) ?? "https://sws-crt.cert.havail.sabre.com";
            //PROD
            return Environment.GetEnvironmentVariable("SABRE_SOAP_URI") ?? "https://webservices.havail.sabre.com";
        }
    }

}
