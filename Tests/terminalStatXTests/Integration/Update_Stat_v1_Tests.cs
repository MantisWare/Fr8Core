﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fr8.Infrastructure.Data.Control;
using Fr8.Infrastructure.Data.Crates;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Testing.Integration;
using NUnit.Framework;
using terminalStatXTests.Fixtures;

namespace terminalStatXTests.Integration
{
    [Explicit]
    public class Update_Stat_v1_Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName => "terminalStatX";

        [Test]
        public async Task Update_Stat_Initial_Configuration_Check_Crate_Structure()
        {
            var responseDTO = await CompleteInitialConfiguration();
            AssertInitialConfigurationResponse(responseDTO);
        }

        [Test]
        public async Task Update_Stat_FollowUp_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();
            var responseDTO = await CompleteInitialConfiguration();
            responseDTO.AuthToken = FixtureData.StatX_AuthToken();
            var dataDTO = new Fr8DataDTO
            {
                ActivityDTO = responseDTO
            };

            responseDTO = await HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, dataDTO);
            AssertInitialConfigurationResponse(responseDTO);
        }

        private async Task<ActivityDTO> CompleteInitialConfiguration()
        {
            var configureUrl = GetTerminalConfigureUrl();
            var requestDataDTO = FixtureData.Update_Stat_InitialConfiguration_Fr8DataDTO();
            return await HttpPostAsync<Fr8DataDTO, ActivityDTO>(configureUrl, requestDataDTO);
        }

        private void AssertInitialConfigurationResponse(ActivityDTO responseDTO)
        {
            Assert.NotNull(responseDTO, "Response is null on initial configuration");
            Assert.NotNull(responseDTO.CrateStorage, "Crate storage is null on initial configuration");
            var crateStorage = Crate.FromDto(responseDTO.CrateStorage);
            AssertConfigureCrate(crateStorage);
        }

        private void AssertConfigureCrate(ICrateStorage crateStorage)
        {
            Assert.AreEqual(1, crateStorage.Count, "Crate storage count is not equal to 1");
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count(), "StandardConfigurationControlsCM count is not 1");
            AssertConfigureControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        private void AssertConfigureControls(StandardConfigurationControlsCM control)
        {
            Assert.AreEqual(1, control.Controls.Count, "Control count is not 1");
            Assert.IsTrue(control.Controls[0] is TextSource, "First control isn't a TextSource");
            Assert.AreEqual("Message", control.Controls[0].Label, "Invalid Label on control");
            Assert.AreEqual("Message", control.Controls[0].Name, "Invalid Name on control");
        }
    }
}
