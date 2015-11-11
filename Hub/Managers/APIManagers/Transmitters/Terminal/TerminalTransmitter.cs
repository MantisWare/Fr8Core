﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using StructureMap;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Hub.Interfaces;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.Services;

namespace Hub.Managers.APIManagers.Transmitters.Terminal
{
    public class TerminalTransmitter : RestfulServiceClient, ITerminalTransmitter
    {
        /// <summary>
        /// Posts ActionDTO to "/actions/&lt;actionType&gt;"
        /// </summary>
        /// <param name="curActionType">Action Type</param>
        /// <param name="actionDTO">DTO</param>
        /// <remarks>Uses <paramref name="curActionType"/> argument for constructing request uri replacing all space characters with "_"</remarks>
        /// <returns></returns>
        public async Task<TResponse> CallActionAsync<TResponse>(string curActionType, ActionDTO actionDTO)
        {
            if (actionDTO == null)
            {
                throw new ArgumentNullException("actionDTO");
            }

            if ((actionDTO.ActivityTemplateId  == null || actionDTO.ActivityTemplateId == 0) && actionDTO.ActivityTemplate == null)
            {
                throw new ArgumentOutOfRangeException("actionDTO", actionDTO.ActivityTemplateId, "ActivityTemplate must be specified either explicitly or by using ActivityTemplateId");
            }

            int terminalId;

            if (actionDTO.ActivityTemplate == null)
            {
                var activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>().GetByKey(actionDTO.ActivityTemplateId.Value);
                actionDTO.ActivityTemplate = Mapper.Map<ActivityTemplateDO, ActivityTemplateDTO>(activityTemplate);
<<<<<<< HEAD:Hub/Managers/APIManagers/Transmitters/Terminal/TerminalTransmitter.cs
                terminalId = activityTemplate.TerminalId;
            }
            else
            {
                terminalId = actionDTO.ActivityTemplate.TerminalId;
            }
           
            var terminal = ObjectFactory.GetInstance<ITerminal>().GetAll().FirstOrDefault(x => x.Id == terminalId);
=======
                terminalId = activityTemplate.TerminalID;
            }
            else
            {
                terminalId = actionDTO.ActivityTemplate.PluginID;
            }
           
            var plugin = ObjectFactory.GetInstance<IPlugin>().GetAll().FirstOrDefault(x => x.Id == terminalId);
>>>>>>> DO-1441:Hub/Managers/APIManagers/Transmitters/Plugin/PluginTransmitter.cs

            if (terminal == null || string.IsNullOrEmpty(terminal.Endpoint))
            {
                BaseUri = null;
            }
            else
        {
                BaseUri = new Uri(terminal.Endpoint.StartsWith("http") ? terminal.Endpoint : "http://" + terminal.Endpoint);
            }

            var actionName = Regex.Replace(curActionType, @"[^-_\w\d]", "_");
            var requestUri = new Uri(string.Format("actions/{0}", actionName), UriKind.Relative);

            return await PostAsync<ActionDTO, TResponse>(requestUri, actionDTO);
        }
    }
}
