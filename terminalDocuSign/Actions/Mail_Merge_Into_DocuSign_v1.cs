﻿using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Interfaces.Manifests;
using Hub.Managers;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services;
using Utilities.Configuration.Azure;
using terminalDocuSign.Infrastructure;
using Data.Constants;
using Data.Repositories;
using UtilitiesTesting.Fixtures;

namespace terminalDocuSign.Actions
{
    public class Mail_Merge_Into_DocuSign_v1 : BaseDocuSignAction
    {
        readonly DocuSignManager _docuSignManager;
        string _dataSourceValue;
        DropDownList _docuSignTemplate;


        string _docuSignTemplateValue;
        private const string SolutionName = "Mail Merge Into DocuSign";
        private const double SolutionVersion = 1.0;
        private const string TerminalName = "DocuSign";
        public Mail_Merge_Into_DocuSign_v1()
            : base()
        {
            _docuSignManager = new DocuSignManager();
        }

        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Run(ActivityDO curActivityDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            /*
            var payloadCrates = await GetPayload(curActivityDO, containerId);

            if (NeedsAuthentication(authTokenDO))
            {
                return NeedsAuthenticationError(payloadCrates);
            }

            var storage = Crate.GetStorage(curActivityDO);
            DropDownList docuSignTemplate = GetStdConfigurationControl<DropDownList>(storage, "DocuSignTemplate");
            string envelopeId = docuSignTemplate.Value;

            // Make sure that it exists
            if (string.IsNullOrEmpty(envelopeId))
            {
                return Error(payloadCrates, "EnvelopeId", ActionErrorCode.PAYLOAD_DATA_MISSING);
            }

            //Create run-time fields
            var fields = CreateDocuSignEventFields();
            foreach (var field in fields)
            {
                field.Value = GetValueForKey(payloadCrates, field.Key);
            }

            using (var updater = Crate.UpdateStorage(payloadCrates))
            {
                updater.CrateStorage.Add(Data.Crates.Crate.FromContent("DocuSign Envelope Payload Data", new StandardPayloadDataCM(fields)));

                //var userDefinedFieldsPayload = _docuSignManager.CreateActionPayload(curActivityDO, authTokenDO, envelopeId);
                //updater.CrateStorage.Add(Data.Crates.Crate.FromContent("DocuSign Envelope Data", userDefinedFieldsPayload));
            }
            */
            //i (bahadir) think solutions should not do anything on their run method
            //they are just preconfiguring existing activities
            return Success(await GetPayload(curActivityDO, containerId));
        }

        /// <summary>
        /// Create configuration controls crate.
        /// </summary>
        private async Task<Crate> CreateConfigurationControlsCrate(ActivityDO activityDO)
        {
            var controlList = new List<ControlDefinitionDTO>();

            controlList.Add(new DropDownList()
            {
                Label = "1. Where is your Source Data?",
                Name = "DataSource",
                ListItems = await GetDataSourceListItems(activityDO, "Table Data Generator")
            });

            controlList.Add(DocuSignManager.CreateDocuSignTemplatePicker(false, "DocuSignTemplate", "2. Use which DocuSign Template?"));
            controlList.Add(new Button()
            {
                Label = "Continue",
                Name = "Continue",
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onClick", "requestConfig")
                }
            });

            return PackControlsCrate(controlList.ToArray());
        }

        private async Task<List<ListItem>> GetDataSourceListItems(ActivityDO activityDO, string tag)
        {
            var curActivityTemplates = await HubCommunicator.GetActivityTemplates(activityDO, tag)
                .ContinueWith(x => x.Result.Where(y => y.Name.StartsWith("Get", StringComparison.InvariantCultureIgnoreCase) && y.Category == Data.States.ActivityCategory.Receivers));
            return curActivityTemplates.Select(at => new ListItem() { Key = at.Label, Value = at.Name }).ToList();
        }

        /// <summary>
        /// Looks for upstream and downstream Creates.
        /// </summary>
        protected override async Task<ActivityDO> InitialConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            if (curActivityDO.Id != Guid.Empty)
            {
                using (var updater = Crate.UpdateStorage(curActivityDO))
                {
                    if (authTokenDO == null || authTokenDO.Token == null)
                    {
                        updater.CrateStorage = new CrateStorage(await CreateNoAuthCrate());
                    }
                    else
                    {
                        var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(authTokenDO.Token);

                        //build a controls crate to render the pane
                        var configurationControlsCrate = await CreateConfigurationControlsCrate(curActivityDO);
                        var templatesFieldCrate = _docuSignManager.PackCrate_DocuSignTemplateNames(docuSignAuthDTO);

                        updater.CrateStorage.Add(configurationControlsCrate);
                        updater.CrateStorage.Add(templatesFieldCrate);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Configuration requires the submission of an Action that has a real ActionId");
            }

            //validate if any DocuSignTemplates has been linked to the Account
            ValidateDocuSignAtLeastOneTemplate(curActivityDO);
            return curActivityDO;
        }

        private Task<Crate> CreateNoAuthCrate()
        {
            var controlList = new List<ControlDefinitionDTO>();

            controlList.Add(new TextBlock()
            {
                Value = "This activity requires authentication. Please authenticate."
            });
            return Task.FromResult((Crate)PackControlsCrate(controlList.ToArray()));
        }

        private T GetStdConfigurationControl<T>(CrateStorage storage, string name)
            where T : ControlDefinitionDTO
        {
            var controls = storage.CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            if (controls == null)
            {
                return null;
            }

            var control = (T)controls.FindByName(name);
            return control;
        }


        /// <summary>
        /// All validation scenarios for Mail_Merge_Into_DocuSign action
        /// </summary>
        /// <param name="curActivityDO"></param>
        /// <returns></returns>
        protected override async Task<CrateStorage> ValidateActivity(ActivityDO curActivityDO)
        {
            ValidateDocuSignAtLeastOneTemplate(curActivityDO);

            return await Task.FromResult<CrateStorage>(null);
        }

        private void ValidateDocuSignAtLeastOneTemplate(ActivityDO curActivityDO)
        {
            //validate DocuSignTemplate for present selected template 
            using (var updater = Crate.UpdateStorage(curActivityDO))
            {
                var docuSignTemplate = updater.CrateStorage.CrateContentsOfType<StandardDesignTimeFieldsCM>(x => x.Label == "Available Templates").FirstOrDefault();
                if (docuSignTemplate != null && docuSignTemplate.Fields != null && docuSignTemplate.Fields.Count != 0) return;//await Task.FromResult<CrateDTO>(null);

                var configControl = GetStdConfigurationControl<DropDownList>(updater.CrateStorage, "DocuSignTemplate");
                if (configControl != null)
                {
                    configControl.ErrorMessage = "Please link some templates to your DocuSign account.";
                }
            }
        }

        /// <summary>
        /// If there's a value in select_file field of the crate, then it is a followup call.
        /// </summary>
        public override ConfigurationRequestType ConfigurationEvaluator(ActivityDO curActivityDO)
        {
            // Do not tarsnfer to follow up when child actions are already present 
            if (curActivityDO.ChildNodes.Count() > 0) return ConfigurationRequestType.Initial;

            var storage = Crate.GetStorage(curActivityDO);
            if (storage == null || storage.Count() == 0)
            {
                return ConfigurationRequestType.Initial;
            }

            // "Follow up" phase is when Continue button is clicked 
            Button button = GetStdConfigurationControl<Button>(storage, "Continue");
            if (button == null) return ConfigurationRequestType.Initial;
            if (button.Clicked == false) return ConfigurationRequestType.Initial;

            // If no values selected in textboxes, remain on initial phase
            DropDownList dataSource = GetStdConfigurationControl<DropDownList>(storage, "DataSource");
            if (dataSource.Value == null) return ConfigurationRequestType.Initial;
            _dataSourceValue = dataSource.Value;

            _docuSignTemplate = GetStdConfigurationControl<DropDownList>(storage, "DocuSignTemplate");
            if (_docuSignTemplate.Value == null) return ConfigurationRequestType.Initial;

            return ConfigurationRequestType.Followup;
        }

        /// <summary>
        /// Checks if activity template generates table data
        /// TODO: find a smoother (unified) way for this
        /// </summary>
        /// <returns></returns>
        private bool DoesActivityTemplateGenerateTableData(ActivityTemplateDO activityTemplate)
        {
            return activityTemplate.Tags != null && activityTemplate.Tags.Split(',').Any(t => t.ToLowerInvariant().Contains("table"));
        }

        //if the user provides a file name, this action attempts to load the excel file and extracts the column headers from the first sheet in the file.
        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(authTokenDO.Token);

            //extract fields in docusign form
            _docuSignManager.UpdateUserDefinedFields(curActivityDO, authTokenDO, Crate.UpdateStorage(curActivityDO), _docuSignTemplate.Value);


            var curActivityTemplates = (await HubCommunicator.GetActivityTemplates(curActivityDO, null))
                .Select(x => Mapper.Map<ActivityTemplateDO>(x))
                .ToList();

            //let's check if activity template generates table data
            var selectedReceiver = curActivityTemplates.Single(x => x.Name == _dataSourceValue);


            var dataSourceActivity = await AddAndConfigureChildActivity(curActivityDO, selectedReceiver.Id.ToString(), order: 1);

            ActivityDO parentOfSendDocusignEnvelope = null;
            int orderOfSendDocusignEnvelope = 0;

            if (DoesActivityTemplateGenerateTableData(selectedReceiver))
            {
                //we need to configure this but it is hard to do
                var loopActivity = await AddAndConfigureChildActivity(curActivityDO, "Loop", order: 2);
                parentOfSendDocusignEnvelope = loopActivity;
                orderOfSendDocusignEnvelope = 1;
            }
            else
            {
                parentOfSendDocusignEnvelope = curActivityDO;
                orderOfSendDocusignEnvelope = 2;
            }

            var sendDocuSignEnvActivity = await AddAndConfigureChildActivity(parentOfSendDocusignEnvelope, "Send_DocuSign_Envelope", order: orderOfSendDocusignEnvelope);
            //set docusign template
            SetControlValue(sendDocuSignEnvActivity, "target_docusign_template", _docuSignTemplate.ListItems.Where(a => a.Key == _docuSignTemplate.selectedKey).FirstOrDefault());


            await ConfigureChildActivity(parentOfSendDocusignEnvelope, sendDocuSignEnvActivity);
            return await Task.FromResult(curActivityDO);
        }
        /// <summary>
        /// This method provides documentation in two forms:
        /// SolutionPageDTO for general information and 
        /// ActivityResponseDTO for specific Help on minicon
        /// </summary>
        /// <param name="activityDO"></param>
        /// <param name="curDocumentation"></param>
        /// <returns></returns>
        public dynamic Documentation(ActivityDO activityDO, string curDocumentation)
        {
            if (curDocumentation.Contains("MainPage"))
            {
            var curSolutionPage = new SolutionPageDTO
            {
                Name = SolutionName,
                Version = SolutionVersion,
                Terminal = TerminalName,
                Body = @"<p>This is a solution action</p>"
            };
            return Task.FromResult(curSolutionPage);
        }
            if (curDocumentation.Contains("HelpMenu"))
            {
                if (curDocumentation.Contains("ExplainMailMerge"))
                {
                    return Task.FromResult(GenerateDocumentationRepsonce(@"This solution helps you to work with email and move data from them to DocuSign service"));
                }
                if (curDocumentation.Contains("ExplainService"))
                {
                    return Task.FromResult(GenerateDocumentationRepsonce(@"This solution works and DocuSign service and uses Fr8 infrastructure"));
                }
                return Task.FromResult(GenerateErrorRepsonce("Unknown contentPath"));
            }
            return
                Task.FromResult(
                    GenerateErrorRepsonce("Unknown displayMechanism: we currently support MainPage and HelpMenu cases"));
        }
    }
}