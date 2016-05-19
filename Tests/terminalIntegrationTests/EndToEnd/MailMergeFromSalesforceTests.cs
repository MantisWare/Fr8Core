﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using terminalIntegrationTests.Fixtures;
using terminalSalesforce.Services;
using Data.Entities;
using terminalSalesforce.Infrastructure;
using HealthMonitor.Utility;
using Hub.Managers;
using terminalSalesforce.Actions;
using terminaBaselTests.Tools.Terminals;
using Data.States;
using terminalDocuSign.Services.New_Api;
using terminalDocuSign.Services;
using DocuSign.eSign.Api;
using Fr8Data.Constants;
using Fr8Data.Control;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;
using TerminalBase.Models;
using Fr8Data.Managers;
using AutoMapper;

namespace terminalIntegrationTests.EndToEnd
{
    [Explicit]
    public class MailMergeFromSalesforceTests : BaseHubIntegrationTest
    {
        private readonly IntegrationTestTools_terminalDocuSign _docuSignTestTools;
        private CrateManager _crateManager = new CrateManager();

        public MailMergeFromSalesforceTests()
        {
            _docuSignTestTools = new IntegrationTestTools_terminalDocuSign(this);
        }

        public override string TerminalName => "terminalSalesforce";

        [Test, Category("Integration.terminalSalesforce")]
        public async Task MailMergeFromSalesforceEndToEnd()
        {
            await RevokeTokens("terminalDocuSign");
            var salesforceAuthToken = await HealthMonitor_FixtureData.CreateSalesforceAuthToken();
            //Create Case object in Salesforce
            var authorizationToken = Mapper.Map<AuthorizationToken>(salesforceAuthToken);
            var caseIdAndName = await CreateCase(authorizationToken);
            PlanDTO plan = null;
            try
            {
                plan = await CreatePlan();
                var solution = plan.Plan.SubPlans.First().Activities.Single();
                await ApplyAuthTokenToSolution(solution, salesforceAuthToken);
                //Initial configuration
                solution = await ConfigureSolution(solution);
                //Folowup configuration

                using(var storage = _crateManager.GetUpdatableStorage(solution))
                {
                    storage.UpdateControls<Mail_Merge_From_Salesforce_v1.ActivityUi>(x =>
                    {
                        x.SalesforceObjectSelector.selectedKey = "Case";
                        x.SalesforceObjectSelector.Value = "Case";
                    });
                }
                //This call will make solution to load specified Salesforce object properties and clear filter
                solution = await ConfigureSolution(solution);
                //This call will run generation of child activities
                using (var storage = _crateManager.GetUpdatableStorage(solution))
                {
                    storage.UpdateControls<Mail_Merge_From_Salesforce_v1.ActivityUi>(x =>
                    {
                        x.SalesforceObjectFilter.Value = $"[{{\"field\":\"SuppliedName\",\"operator\":\"eq\",\"value\":\"{caseIdAndName.Item2}\"}}]";
                        var sendDocuSignItem = x.MailSenderActivitySelector.ListItems.FirstOrDefault(y => y.Key == "Send DocuSign Envelope");
                        Assert.IsNotNull(sendDocuSignItem, $"Send DocuSign Envelope activity is not marked with '{Tags.EmailDeliverer}' tag");
                        x.MailSenderActivitySelector.selectedKey = sendDocuSignItem.Key;
                        x.MailSenderActivitySelector.Value = sendDocuSignItem.Value;
                        x.RunMailMergeButton.Clicked = true;
                    });
                }
                solution = await ConfigureSolution(solution);
                Assert.AreEqual(2, solution.ChildrenActivities.Length, "Child activities were not generated after mail merge was requested");
                //Configure Send DocuSign Envelope activity to use proper upstream values
                var docuSignActivity = solution.ChildrenActivities[1].ChildrenActivities[0];
                var activityPayload = Mapper.Map<ActivityPayload>(docuSignActivity);
                var activityContext = new ActivityContext
                {
                    ActivityPayload = activityPayload
                };
                var docusSignAuthAndConfig = await AuthorizeAndConfigureDocuSignActivity(activityContext);
                var docuSignActivityPayload = docusSignAuthAndConfig.Item1;
                //Run plan
                var container = await Run(plan);
                Assert.AreEqual(State.Completed, container.State, "Container state is not equal to Completed");
                // Deactivate plan
                await Deactivate(plan);
                //Verify contents of envelope
                AssertEnvelopeContents(docusSignAuthAndConfig.Item2, caseIdAndName.Item2);
                // Verify that test email has been received
                EmailAssert.EmailReceived("dse_demo@docusign.net", "Test Message from Fr8");
            }
            finally
            {
                var caseWasDeleted = await DeleteCase(caseIdAndName.Item1, authorizationToken);
                Assert.IsTrue(caseWasDeleted, "Case created for test purposes failed to be deleted");
                //if (plan != null)
                //{
                //    await HttpDeleteAsync($"{_baseUrl}plans?id={plan.Plan.Id}");
                //}
            }
        }

        private void AssertEnvelopeContents(Guid docuSignTokenId, string expectedName)
        {
            var authorizationToken = new AuthorizationToken
            {
                Token = _docuSignTestTools.GetDocuSignAuthToken(docuSignTokenId).Token
            };
            var configuration = new DocuSignManager().SetUp(authorizationToken);
            //find the envelope on the Docusign Account
            var folderItems = DocuSignFolders.GetFolderItems(configuration, new DocuSignQuery()
            {
                Status = "sent",
                SearchText = expectedName
            });
            var envelope = folderItems.FirstOrDefault();
            Assert.IsNotNull(envelope, "Cannot find created Envelope in sent folder of DocuSign Account");
            var envelopeApi = new EnvelopesApi(configuration.Configuration);
            //get the recipient that receive this sent envelope
            var envelopeSigner = envelopeApi.ListRecipients(configuration.AccountId, envelope.EnvelopeId).Signers.FirstOrDefault();
            Assert.IsNotNull(envelopeSigner, "Envelope does not contain signer as recipient. Send_DocuSign_Envelope activity failed to provide any signers");
            //get the tabs for the envelope that this recipient received
            var tabs = envelopeApi.ListTabs(configuration.AccountId, envelope.EnvelopeId, envelopeSigner.RecipientId);
            Assert.IsNotNull(tabs, "Envelope does not contain any tabs. Check for problems in DocuSignManager and HandleTemplateData");
        }

        private async Task<Tuple<ActivityPayload, Guid>> AuthorizeAndConfigureDocuSignActivity(ActivityContext docuSignActivity)
        {
            var crateStorage = docuSignActivity.ActivityPayload.CrateStorage;
            var authenticationRequired = crateStorage.CratesOfType<StandardAuthenticationCM>().Any();
            var tokenId = Guid.Empty;
            if (authenticationRequired)
            {
                // Authenticate with DocuSign
                tokenId = await _docuSignTestTools.AuthenticateDocuSignAndAssociateTokenWithAction(docuSignActivity.ActivityPayload.Id, GetDocuSignCredentials(), docuSignActivity.ActivityPayload.ActivityTemplate.Terminal);
                await Configure(docuSignActivity.ActivityPayload);
            }
            var controlsDocusign = docuSignActivity.ActivityPayload.CrateStorage.FirstCrate<StandardConfigurationControlsCM>();
            var templateSelector = controlsDocusign.Content.FindByName<DropDownList>("target_docusign_template");
            templateSelector.selectedKey = "SendEnvelopeTestTemplate";
            templateSelector.Value = "392f63c3-cabb-4b21-b331-52dabf1c2993";
            
            //This configuration call will generate text source fields for selected template properties
            await Configure(docuSignActivity.ActivityPayload);
            var controls = docuSignActivity.ActivityPayload.CrateStorage.FirstCrate<StandardConfigurationControlsCM>();
            var textSource = controls.Content.FindByName<TextSource>("RolesMappingTestSigner role email");
            textSource.ValueSource = "upstream";
            textSource.selectedKey = "SuppliedEmail";
            textSource.Value = "SuppliedEmail";

            textSource = controls.Content.FindByName<TextSource>("RolesMappingTestSigner role name");
            textSource.ValueSource = "upstream";
            textSource.selectedKey = "SuppliedName";
            textSource.Value = "SuppliedName";
            return new Tuple<ActivityPayload, Guid>(await Save(docuSignActivity.ActivityPayload), tokenId);
        }

        private async Task<ActivityPayload> Save(ActivityPayload activityPayload)
        {
            var activityDTO = Mapper.Map<ActivityDTO>(activityPayload);
            var result = await HttpPostAsync<ActivityDTO, ActivityDTO>($"{_baseUrl}activities/save", activityDTO);
            return Mapper.Map<ActivityPayload>(result);
        }

        private async Task<ActivityPayload> Configure(ActivityPayload activityPayload)
        {
            var activityDTO = Mapper.Map<ActivityDTO>(activityPayload);
            var result = await HttpPostAsync<ActivityDTO, ActivityDTO>($"{_baseUrl}activities/configure?id={activityPayload.Id}", activityDTO);
            return Mapper.Map<ActivityPayload>(result);
        }

        private async Task<ActivityDTO> ConfigureSolution(ActivityDTO activity)
        {
            return await HttpPostAsync<ActivityDTO, ActivityDTO>($"{_baseUrl}activities/configure?id={activity.Id}", activity);
        }

        private async Task<ContainerDTO> Run(PlanDTO plan)
        {
            return await HttpPostAsync<string, ContainerDTO>($"{_baseUrl}plans/run?planId={plan.Plan.Id}", null);
        }

        private async Task<string> Deactivate(PlanDTO plan)
        {
            return await HttpPostAsync<string, string>($"{_baseUrl}plans/deactivate?planId={plan.Plan.Id}", null);
        }

        private async Task ApplyAuthTokenToSolution(ActivityDTO solution, AuthorizationTokenDO salesforceAuthToken)
        {
            var applyToken = new ManageAuthToken_Apply()
            {
                ActivityId = solution.Id,
                AuthTokenId = salesforceAuthToken.Id,
                IsMain = true
            };
            await HttpPostAsync<ManageAuthToken_Apply[], string>(GetHubApiBaseUrl() + "ManageAuthToken/apply", new[] { applyToken });
        }

        private async Task<PlanDTO> CreatePlan()
        {
            var solutionCreateUrl = GetHubApiBaseUrl() + "plans/createSolution?solutionName=Mail_Merge_From_Salesforce";
            return await HttpPostAsync<string, PlanDTO>(solutionCreateUrl, null);
        }

        private async Task<bool> DeleteCase(string caseId, AuthorizationToken authToken)
        {
            return await new SalesforceManager().Delete(SalesforceObjectType.Case, caseId, authToken);
        }

        private async Task<Tuple<string, string>> CreateCase(AuthorizationToken authToken)
        {
            var manager = new SalesforceManager();
            var name = Guid.NewGuid().ToString();
            var data = new Dictionary<string, object> { { "SuppliedEmail", TestEmail }, { "SuppliedName", name } };
            return new Tuple<string, string>(await manager.Create(SalesforceObjectType.Case, data, authToken), name);
        }
    }
}
