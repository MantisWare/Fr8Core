﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace terminalSalesforce.Infrastructure
{
    public class SalesforceNotificationParser
    {
        public static Envelope GetEnvelopeInformation(string xmlPayload)
        {
            if (string.IsNullOrEmpty(xmlPayload))
                throw new ArgumentNullException("xmlPayload");

            Envelope salesforceEnvelopeInfo;
            var serializer = new XmlSerializer(typeof(Envelope));
            using (var reader = new StringReader(xmlPayload))
            {
                salesforceEnvelopeInfo = (Envelope)serializer.Deserialize(reader);
            }
            return salesforceEnvelopeInfo;
        }
    }
}
