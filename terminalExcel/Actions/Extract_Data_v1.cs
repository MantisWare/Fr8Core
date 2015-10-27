﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Newtonsoft.Json;
using StructureMap;
using Data.Interfaces;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;
using Hub.Exceptions;
using Hub.Interfaces;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalExcel.Infrastructure;

namespace terminalExcel.Actions
{
    public class Extract_Data_v1 : BasePluginAction
    {
        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Run(ActionDTO curActionDTO)
        {
            return await CreateStandardPayloadDataFromStandardTableData(curActionDTO);
        }

        private async Task<PayloadDTO> CreateStandardPayloadDataFromStandardTableData(ActionDTO curActionDTO)
        {
            var processPayload = await GetProcessPayload(curActionDTO.ProcessId);

            var curActionDO = Mapper.Map<ActionDO>(curActionDTO);

            var tableDataMS = await GetTargetTableData(
                curActionDO.Id,
                curActionDO.CrateStorageDTO()
            );

            if (!tableDataMS.FirstRowHeaders)
            {
                throw new Exception("No headers found in the Standard Table Data Manifest.");
            }

            // Create a crate of payload data by using Standard Table Data manifest and use its contents to tranform into a Payload Data manifest.
            // Add a crate of PayloadData to action's crate storage
            var payloadDataCrate = Crate.CreatePayloadDataCrate("ExcelTableRow", "Excel Data", tableDataMS);
            Crate.AddCrate(processPayload, payloadDataCrate);
            
            return processPayload;            
        }

        private async Task<StandardTableDataCM> GetTargetTableData(int actionId, CrateStorageDTO curCrateStorageDTO)
        {
            // Find crates of manifest type Standard Table Data
            var standardTableDataCrates = Crate.GetCratesByManifestType(CrateManifests.STANDARD_TABLE_DATA_MANIFEST_NAME, curCrateStorageDTO);

            // If no crate of manifest type "Standard Table Data" found, try to find upstream for a crate of type "Standard File Handle"
            if (!standardTableDataCrates.Any())
            {
                return await GetUpstreamTableData(actionId, curCrateStorageDTO);
            }

            return JsonConvert.DeserializeObject<StandardTableDataCM>(standardTableDataCrates.First().Contents);
        }

        private async Task<StandardTableDataCM> GetUpstreamTableData(int actionId, CrateStorageDTO curCrateStorageDTO)
        {
            var upstreamFileHandleCrates = await GetUpstreamFileHandleCrates(actionId);

            //if no "Standard File Handle" crate found then return
            if (!upstreamFileHandleCrates.Any())
                return null;

            //if more than one "Standard File Handle" crates found then throw an exception
            if (upstreamFileHandleCrates.Count > 1)
                throw new Exception("More than one Standard File Handle crates found upstream.");

            // Deserialize the Standard File Handle crate to StandardFileHandleMS object
            StandardFileHandleMS fileHandleMS = JsonConvert.DeserializeObject<StandardFileHandleMS>(upstreamFileHandleCrates.First().Contents);

            // Use the url for file from StandardFileHandleMS and read from the file and transform the data into StandardTableData and assign it to Action's crate storage
            StandardTableDataCM tableDataMS = ExcelUtils.GetTableData(fileHandleMS.DockyardStorageUrl);

            return tableDataMS;
        }

        //private async Task<FileDO> ProcessFile(string dockyardStorageUrl, string fileName)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        var values = new Dictionary<string, string>
        //                {
        //                    { "DockyardStorageURL", dockyardStorageUrl },
        //                    { "Filename", fileName },
        //                    //{ "AuthorizationToken", curFileDO.DockyardAccountID } //ignoring for now
        //                };

        //        var content = new FormUrlEncodedContent(values);

        //        var response = await client.PostAsync(ConfigurationManager.AppSettings["FileServerApiUrl"], content);

        //        var curFileDOTask = await response.Content.ReadAsAsync<FileDO>();

        //        return curFileDOTask;
        //    }
        //}

        /// <summary>
        /// Create configuration controls crate.
        /// </summary>
        private CrateDTO CreateConfigurationControlsCrate(bool includeTextBlockControl = false)
        {
            var controlList = new List<ControlDefinitionDTO>();

            var filePickerControl = new ControlDefinitionDTO(ControlTypes.FilePicker)
            {
                Label = "Select an Excel file",
                Name = "select_file",
                Required = true,
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                },
                Source = new FieldSourceDTO
                {
                    Label = "Select an Excel file",
                    ManifestType = CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME
                },
            };
            controlList.Add(filePickerControl);

            var textBlockControlField = new TextBlockControlDefinitionDTO()
            {
                Label = "",
                Value = "This Action will try to extract a table of rows from the first spreadsheet in the file. The rows should have a header row.",
                CssClass = "well well-lg TextBlockClass"
            };
            controlList.Add(textBlockControlField);

            if (includeTextBlockControl)
            {
                var textBlockControl = new TextBlockControlDefinitionDTO()
                    {
                        Label = "",
                        Value = "File successfully uploaded.",
                        CssClass = "well well-lg"
                    };
                controlList.Add(textBlockControl);
            }

            return PackControlsCrate(controlList.ToArray());
        }

        /// <summary>
        /// Looks for upstream and downstream Creates.
        /// </summary>
        protected override async Task<ActionDTO> InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            if (curActionDTO.Id > 0)
            {
                ActionDO curActionDO = Mapper.Map<ActionDO>(curActionDTO);

                //Pack the merged fields into a new crate that can be used to populate the dropdownlistbox
                CrateDTO upstreamFieldsCrate = await MergeUpstreamFields(curActionDO.Id, "Select Excel File");

                //build a controls crate to render the pane
                CrateDTO configurationControlsCrate = CreateConfigurationControlsCrate();

                var crateStrorageDTO = AssembleCrateStorage(upstreamFieldsCrate, configurationControlsCrate);
                curActionDTO.CrateStorage = crateStrorageDTO;
            }
            else
            {
                throw new ArgumentException(
                    "Configuration requires the submission of an Action that has a real ActionId");
            }
            return curActionDTO;
        }

        /// <summary>
        /// If there's a value in select_file field of the crate, then it is a followup call.
        /// </summary>
        public override ConfigurationRequestType ConfigurationEvaluator(ActionDTO curActionDTO)
        {
            var curActionDO = Mapper.Map<ActionDO>(curActionDTO);

            var filePathsFromUserSelection = Action.FindKeysByCrateManifestType(
                    curActionDO,
                    new Manifest(Data.Constants.MT.StandardConfigurationControls),
                    "select_file"
                )
                .Result
                .Select(e => (string)e["value"])
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            var hasDesignTimeFields = curActionDTO.CrateStorage.CrateDTO
                .Any(x => x.ManifestType == CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                    && x.Label == "Spreadsheet Column Headers");

            if (filePathsFromUserSelection.Length == 1 || hasDesignTimeFields)
            {
                return ConfigurationRequestType.Followup;
            }

            return ConfigurationRequestType.Initial;
        }

        //if the user provides a file name, this action attempts to load the excel file and extracts the column headers from the first sheet in the file.
        protected override async Task<ActionDTO> FollowupConfigurationResponse(ActionDTO curActionDTO)
        {
            ActionDO curActionDO = Mapper.Map<ActionDO>(curActionDTO);

            var filePathsFromUserSelection = Action.FindKeysByCrateManifestType(curActionDO, new Manifest(Data.Constants.MT.StandardConfigurationControls), "select_file").Result
                .Select(e => (string)e["value"])
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            if (filePathsFromUserSelection.Length > 1)
                throw new AmbiguityException();

            // RFemove previously created configuration control crate
            Crate.RemoveCrateByManifestType(curActionDTO.CrateStorage.CrateDTO, CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME);
            // Creating configuration control crate with a file picker and textblock
            var configControlsCrateDTO = CreateConfigurationControlsCrate(true);
            curActionDTO.CrateStorage.CrateDTO.Add(configControlsCrateDTO);

            if (filePathsFromUserSelection.Length > 0)
            {
                var selectedFilePath = filePathsFromUserSelection[0];
                return await TransformExcelFileDataToStandardTableDataCrate(curActionDTO, selectedFilePath);
            }
            else
            {
                return curActionDTO;
            }
        }

        private async Task<ActionDTO> TransformExcelFileDataToStandardTableDataCrate(ActionDTO curActionDTO, string selectedFilePath)
        {
            var curActionDO = Mapper.Map<ActionDO>(curActionDTO);

            // Check if the file is an Excel file.
            string ext = Path.GetExtension(selectedFilePath);
            if (ext != ".xls" && ext != ".xlsx")
                throw new ArgumentException("Expected '.xls' or '.xlsx'", "selectedFile");

            FileDO curFileDO = new FileDO()
            {
                CloudStorageUrl = selectedFilePath,
            };

            IFile file = ObjectFactory.GetInstance<IFile>();

            // Read file from repository
            var fileAsByteArray = file.Retrieve(curFileDO);

            // Fetch column headers in Excel file and assign them to the action's crate storage as Design TIme Fields crate
            var headersArray = ExcelUtils.GetColumnHeaders(fileAsByteArray, ext);
            if (headersArray != null)
            {
                var headers = headersArray.ToList();
                var curCrateDTO = Crate.CreateDesignTimeFieldsCrate(
                            "Spreadsheet Column Headers",
                            headers.Select(col => new FieldDTO() { Key = col, Value = col }).ToArray()
                        );
                Crate.AddOrReplaceCrate("Spreadsheet Column Headers", curActionDO, curCrateDTO);
            }

            CreatePayloadCrate_ExcelRows(curActionDO, fileAsByteArray, headersArray, ext);

            return AutoMapper.Mapper.Map<ActionDTO>(curActionDO);
        }

        private void CreatePayloadCrate_ExcelRows(ActionDO curActionDO, byte[] fileAsByteArray, string[] headersArray, string extension)
        {
            // Fetch rows in Excel file and assign them to the action's crate storage as Standard Table Data crate
            var rowsDictionary = ExcelUtils.GetTabularData(fileAsByteArray, extension);
            if (rowsDictionary != null && rowsDictionary.Count > 0)
            {
                var rows = ExcelUtils.CreateTableCellPayloadObjects(rowsDictionary, headersArray);
                if (rows != null && rows.Count > 0)
                {
                    var curExcelPayloadRowsCrateStorageDTO = new CrateStorageDTO
                    {
                        CrateDTO = new List<CrateDTO>
                        {
                            Crate.CreateStandardTableDataCrate("Excel Payload Rows", true, rows.ToArray()),
                        }
                    };
                    Crate.AddCrate(curActionDO, curExcelPayloadRowsCrateStorageDTO.CrateDTO.ToList());
                }
            }
        }
    }
}