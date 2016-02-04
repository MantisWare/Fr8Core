﻿using System;
using Data.Interfaces.DataTransferObjects;

namespace terminalAzureTests.Fixtures
{
    public class HealthMonitor_FixtureData
    {
        public static ActivityTemplateDTO Write_To_Sql_Server_v1_ActivityTemplate()
        {
            return new ActivityTemplateDTO()
            {
                Id = 1,
                Name = "Write_To_Sql_Server_TEST",
                Version = "1"
            };
        }
        

        public static Fr8DataDTO Write_To_Sql_Server_v1_InitialConfiguration_Fr8DataDTO()
        {
            var activityTemplate = Write_To_Sql_Server_v1_ActivityTemplate();

            var activityDTO = new ActivityDTO()
            {
                Id = Guid.NewGuid(),
                Name = "Write_To_Sql_Server",
                Label = "Write To Sql Server",
                ActivityTemplate = activityTemplate,
                ActivityTemplateId = activityTemplate.Id
            };

            return new Fr8DataDTO
            {
                ActivityDTO = activityDTO
            };
        }
    }
}
