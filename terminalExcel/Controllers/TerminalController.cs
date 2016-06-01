﻿using System.Collections.Generic;
using System.Web.Http.Description;
using System.Web.Http;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;
using Fr8Data.States;
using Utilities.Configuration.Azure;
using TerminalBase.Services;

namespace terminalExcel.Controllers
{
    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        /// <summary>
        /// Terminal discovery infrastructure.
        /// Action returns list of supported actions by terminal.
        /// </summary>
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]
        public IHttpActionResult DiscoverTerminals()
        {
            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = TerminalData.TerminalDTO,
                Activities = ActivityStore.GetAllActivities(TerminalData.TerminalDTO)
            };
            return Json(curStandardFr8TerminalCM);
        }
    }
}