﻿using DocuSign.Integrations.Client;
using System;

namespace terminalDocuSign.Tests.Fixtures
{
    public partial class PluginFixtureData
    {
        private static Recipients TestRecipients1()
        {
            return new Recipients
            {
                recipientCount = "1",
                signers = new[]
                                 {
                                     TestSigner1()
                                 }
            };
        }
    }
}