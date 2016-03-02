﻿using Data.Interfaces.DataTransferObjects;
using Data.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace terminalDocuSign.Tests.Fixtures
{
    public partial class TerminalFixtureData
    {
        public static ActivityTemplateDTO Generate_DocuSign_ReportTemplate()
        {
            return new ActivityTemplateDTO
            {
                Name = "Generate_DocuSign_Report",
                Label = "Generate a DocuSign Report",
                Version = "1",
                Category = ActivityCategory.Solution,
                NeedsAuthentication = true,
                MinPaneWidth = 550,
                Tags = "HideChildren",
                Id = 0
            };
        }
    }
}