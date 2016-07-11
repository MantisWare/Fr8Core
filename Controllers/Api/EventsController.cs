﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Managers;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Infrastructure.Utilities.Logging;
using StructureMap;
using Hub.Interfaces;
using HubWeb.Infrastructure_HubWeb;
using System.Web.Http.Description;

namespace HubWeb.Controllers
{
    /// <summary>
    /// Central logging Events controller
    /// </summary>
    public class EventsController : ApiController
    {
        private readonly ICrateManager _crate;
        private readonly IJobDispatcher _jobDispatcher;

        public EventsController()
        {
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _jobDispatcher = ObjectFactory.GetInstance<IJobDispatcher>();
        }

        public static Task ProcessEventsInternal(CrateDTO raw)
        {
            var curCrateStandardEventReport = ObjectFactory.GetInstance<ICrateManager>().FromDto(raw);
            return ObjectFactory.GetInstance<IEvent>().ProcessInboundEvents(curCrateStandardEventReport);
        }
        /// <summary>
        /// Dispatches event received from external services to respective terminal
        /// </summary>
        /// <param name="raw">Crates with data related to external event</param>
        /// <response code="200">Event was successfully dispatched to respective terminal</response>
        /// <response code="400">Crate is not specified or its content is invalid</response>
        /// <response code="403">Unauthorized request</response>
        [HttpPost]
        [Fr8TerminalAuthentication]
        public async Task<IHttpActionResult> Post(CrateDTO raw)
        {
            if (raw == null)
            {
                return BadRequest("Crate object is not specified");
            }
            var curCrateStandardEventReport = _crate.FromDto(raw);
            if (!curCrateStandardEventReport.IsOfType<EventReportCM>())
            {
                return BadRequest("Crate object doesn't contain EventReportCM manifest");
            }
            if (curCrateStandardEventReport.Get() == null)
            {
                return BadRequest("Crate content is empty");
            }
            var eventReportMS = curCrateStandardEventReport.Get<EventReportCM>();
            Logger.LogInfo($"Crate {raw.Id} with incoming event '{eventReportMS.EventNames}' is received for external account '{eventReportMS.ExternalAccountId}'");
            if (eventReportMS.EventPayload == null)
            {
                return BadRequest("EventReport can't have a null payload");
            }
            if (string.IsNullOrEmpty(eventReportMS.ExternalAccountId) && string.IsNullOrEmpty(eventReportMS.ExternalDomainId))
            {
                return BadRequest("EventReport can't have both ExternalAccountId and ExternalDomainId empty");
            }
            _jobDispatcher.Enqueue(() => ProcessEventsInternal(raw));
            return Ok();
        }
    }
}