﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Interfaces.DataTransferObjects;
using HealthMonitor.Utility;
using NUnit.Framework;

namespace HubTests.Integration
{
    [Explicit]
    [Category("HubTests.Integration")]
    public class ActivityController_EndToEnd_Tests : BaseHubIntegrationTest
    {
        private string Mail_Merge_Description = @"<p>Pull data from a variety of sources, including Excel files, 
                                            Google Sheets, and databases, and merge the data into your DocuSign template. 
                                            You can link specific fields from your source data to DocuSign fields</p>";
        private string Extract_Data_Description = @"<p>This powerful report generator extends the capabilities of the standard DocuSign reporting tools. 
                                                Search by Recipient or Template and build powerful queries with a few mouse clicks</p>";
        private string Track_DocuSign_Description = @"<p>Link your important outgoing envelopes to Fr8's powerful notification Activities, 
                                            which allow you to receive SMS notices, emails, or receive posts to popular tracking systems like Slack and Yammer. 
                                            Get notified when recipients take too long to sign!</p>";
        private string Generate_DocuSign_Report_Description = @"<p>This is Generate DocuSign Report solution action</p>";

        private string SearchFr8Warehouse_Description =
                                            @"<p>The Search Fr8 Warehouse solution allows you to search the Fr8 Warehouse 
                                            for information we're storing for you. This might be event data about your cloud services that we track on your 
                                            behalf. Or it might be files or data that your plans have stored.</p>";

        private string FindObjects_Solution_Description = @"<p>This is the FindObjects Solution.</p>";
        public override string TerminalName
        {
            get { return "Hub"; }
        }
        [Test]
        public async Task GetSolutionListByTerminalName_EndToEnd()
        {
            await GetDocuSignSolutionList();
            await GetFr8CoreSolutionList();
        }

        [Test]
        private async Task GetDocuSignSolutionList()
        {
            var solutionNames = new List<string> { "Mail Merge Into DocuSign", "Extract Data From Envelopes", "Track DocuSign Recipients" };
            var baseUrl = GetHubApiBaseUrl();
            var getSolutionListUrl = baseUrl + "activities/Documentation";
            var emptyActivityDTO = new ActivityDTO { Documentation = "Terminal=terminalDocuSign", ActivityTemplate = new ActivityTemplateDTO() };
            var solutionPages = await HttpPostAsync<ActivityDTO, List<SolutionPageDTO>>(getSolutionListUrl, emptyActivityDTO);
            Assert.IsNotNull(solutionPages);
            Assert.IsTrue(solutionPages.Any());
            //We provide 3 Solution Pages for the DocuSign terminal
            Assert.AreEqual(3, solutionPages.Count);
            foreach (var solutionPage in solutionPages)
            {
                Assert.IsTrue(solutionNames.Contains(solutionPage.Name));
                switch (solutionPage.Name)
                {
                    case "Mail Merge Into DocuSign":
                        Assert.AreEqual(Mail_Merge_Description, solutionPage.Body);
                        break;
                    case "Extract Data From Envelopes":
                        Assert.AreEqual(Extract_Data_Description, solutionPage.Body);
                        break;
                    case "Track DocuSign Recipients":
                        Assert.AreEqual(Track_DocuSign_Description, solutionPage.Body);
                        break;
                }
            }

        }
        [Test]
        private async Task GetFr8CoreSolutionList()
        {
            var solutionNames = new List<string> { "Find Objects Solution", "Search Fr8 Warehouse" };
            var baseUrl = GetHubApiBaseUrl();
            var getSolutionListUrl = baseUrl + "activities/Documentation";
            var emptyActivityDTO = new ActivityDTO { Documentation = "Terminal=terminalFr8Core", ActivityTemplate = new ActivityTemplateDTO() };
            var solutionPages = await HttpPostAsync<ActivityDTO, List<SolutionPageDTO>>(getSolutionListUrl, emptyActivityDTO);
            Assert.IsNotNull(solutionPages);
            Assert.IsTrue(solutionPages.Any());
            //We provide 2 Solution Pages for the Fr8Core terminal
            Assert.AreEqual(2, solutionPages.Count);
            foreach (var solutionPage in solutionPages)
            {
                Assert.IsTrue(solutionNames.Contains(solutionPage.Name));
                switch (solutionPage.Name)
                {
                    case "Find Objects Solution":
                        Assert.AreEqual(FindObjects_Solution_Description, solutionPage.Body);
                        break;
                    case "Search Fr8 Warehouse":
                        Assert.AreEqual(SearchFr8Warehouse_Description, solutionPage.Body);
                        break;
                }
            }
        }

    }
}