﻿using System.Collections.Generic;
using System.Configuration;
using Core.Interfaces;
using Newtonsoft.Json;
using Data.Entities;
using Newtonsoft.Json.Linq;

namespace Core.PluginRegistrations
{
    public class AzureSqlPluginRegistration : BasePluginRegistration
    {
        public const string baseUrl = "AzureSql.BaseUrl";
        private const string availableActions = @"[{ ""ActionType"" : ""Write"" , ""Version"": ""1.0""}]";

        public AzureSqlPluginRegistration(IAction action)
            : base(availableActions, baseUrl)
        {

        }
    }
}
