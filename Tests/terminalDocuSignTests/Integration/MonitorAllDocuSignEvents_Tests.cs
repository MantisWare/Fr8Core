﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using HealthMonitor.Utility;
using HealthMonitorUtility;
using terminalDocuSign.Services.New_Api;
using Newtonsoft.Json;
using System.Diagnostics;
using AutoMapper;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;

namespace terminalDocuSignTests.Integration
{
    [Explicit]
    [SkipLocal]
    public class MonitorAllDocuSignEvents_Tests : BaseHubIntegrationTest
    {
        // private const string UserAccountName = "y.gnusin@gmail.com";
        private const string UserAccountName = "integration_test_runner@fr8.company";
        private const int MaxAwaitPeriod = 300000;

        private const int SingleAwaitPeriod = 10000;

        private const string templateId = "b0c8eb61-ff16-410d-be0b-6a2feec57f4c"; // "392f63c3-cabb-4b21-b331-52dabf1c2993"; // "SendEnvelopeIntegrationTest" template

        private const string ToEmail = "fr8.madse.testing@gmail.com"; // "freight.testing@gmail.com";
        private const string DocuSignEmail = "fr8.madse.testing@gmail.com"; // "freight.testing@gmail.com";
        private const string DocuSignApiPassword = "I6HmXEbCxN";

        private string ConnectName = "madse-connect";
        private string publishUrl;


        protected override string TestUserEmail
        {
            get { return UserAccountName; }
        }

        /*protected override string TestUserPassword
        {
            get { return "123qwe"; }
        }*/


        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        [Test]
        public async void Test_MonitorAllDocuSignEvents_Plan()
        {
            using (var unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var testAccount = unitOfWork.UserRepository
                    .GetQuery()
                    .SingleOrDefault(x => x.UserName == UserAccountName);

                var docuSignTerminal = unitOfWork.TerminalRepository
                    .GetQuery()
                    .SingleOrDefault(x => x.Name == TerminalName);

                if (testAccount == null)
                {
                    throw new ApplicationException(
                        string.Format("No test account found with UserName = {0}", UserAccountName)
                    );
                }

                if (docuSignTerminal == null)
                {
                    throw new ApplicationException(
                        string.Format("No terminal found with Name = {0}", TerminalName)
                    );
                }

                await RecreateDefaultAuthToken(unitOfWork, testAccount, docuSignTerminal);

                var mtDataCountBefore = unitOfWork.MultiTenantObjectRepository
                    .AsQueryable<DocuSignEnvelopeCM_v2>(testAccount.Id.ToString())
                    .Count();

                //Set up DS
                var authToken = await Authenticate();
                var authTokenDO = new AuthorizationTokenDO() { Token = authToken.Token };
                var docuSignManager = new DocuSignManager();
                var loginInfo = docuSignManager.SetUp(authTokenDO);

                //let's wait 10 seconds to ensure that MADSE plan was created/activated by re-authentication
                await Task.Delay(SingleAwaitPeriod);

                //send envelope
                SendDocuSignTestEnvelope(docuSignManager, loginInfo, authTokenDO);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                int mtDataCountAfter = mtDataCountBefore;
                while (stopwatch.ElapsedMilliseconds <= MaxAwaitPeriod)
                {
                    await Task.Delay(SingleAwaitPeriod);

                    mtDataCountAfter = unitOfWork.MultiTenantObjectRepository
                        .AsQueryable<DocuSignEnvelopeCM_v2>(testAccount.Id.ToString()).Count();

                    if (mtDataCountBefore < mtDataCountAfter)
                    {
                        break;
                    }
                }

                Assert.IsTrue(mtDataCountBefore < mtDataCountAfter,
                    $"The number of MtData: ({mtDataCountAfter}) records for user {UserAccountName} remained unchanged within {MaxAwaitPeriod} miliseconds.");
            }
        }

        private async Task RecreateDefaultAuthToken(IUnitOfWork uow,
            Fr8AccountDO account, TerminalDO docuSignTerminal)
        {
            Console.WriteLine($"Reauthorizing tokens for {account.EmailAddress.Address}");
            var tokens = await HttpGetAsync<IEnumerable<ManageAuthToken_Terminal>>(
                _baseUrl + "manageauthtoken/"
            );

            if (tokens != null)
            {
                var docusignTokens = tokens.FirstOrDefault(x => x.Name == "terminalDocuSign");
                if (docusignTokens != null)
                {
                    foreach (var token in docusignTokens.AuthTokens)
                    {
                        await HttpPostAsync<string>(
                            _baseUrl + "manageauthtoken/revoke?id=" + token.Id.ToString(),
                            null
                        );
                    }
                }
            }

            var creds = new CredentialsDTO()
            {
                Username = DocuSignEmail,
                Password = DocuSignApiPassword,
                IsDemoAccount = true,
                Terminal = Mapper.Map<TerminalDTO>(docuSignTerminal)
            };

            var tokenResponse = await HttpPostAsync<CredentialsDTO, JObject>(
                _baseUrl + "authentication/token",
                creds
            );

            Assert.NotNull(
                tokenResponse["authTokenId"],
                "AuthTokenId is missing in API response."
            );
        }

        private async Task<AuthorizationTokenDTO> Authenticate()
        {
            var creds = new CredentialsDTO()
            {
                Username = DocuSignEmail,
                Password = DocuSignApiPassword,
                IsDemoAccount = true
            };

            string endpoint = GetTerminalUrl() + "/authentication/internal";
            var jobject = await HttpPostAsync<CredentialsDTO, JObject>(endpoint, creds);
            var docuSignToken = JsonConvert.DeserializeObject<AuthorizationTokenDTO>(jobject.ToString());
            Assert.IsTrue(
                string.IsNullOrEmpty(docuSignToken.Error),
                $"terminalDocuSign call to /authentication/internal has failed with following error: {docuSignToken.Error}"
            );

            return docuSignToken;
        }

        private  void SendDocuSignTestEnvelope(DocuSignManager docuSignManager, DocuSignApiConfiguration loginInfo, AuthorizationTokenDO authTokenDO)
        {
            var rolesList = new List<FieldDTO>()
            {
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key = "role name",
                    Value = ToEmail
                },
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key = "role email",
                    Value = ToEmail
                }
            };

            var fieldsList = new List<FieldDTO>()
            {
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key="companyTabs",
                    Value="test"
                },
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key="textTabs",
                    Value="test"
                },
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key="noteTabs",
                    Value="test"
                },
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key="checkboxTabs",
                    Value="Radio 1"
                },
                new FieldDTO()
                {
                    Tags = "recipientId:1",
                    Key="listTabs",
                    Value="1"
                }
            };

            docuSignManager.SendAnEnvelopeFromTemplate(
                loginInfo,
                rolesList,
                fieldsList,
                templateId
            );
        }
    }
}