﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using Data.Crates;
using StructureMap;
using Data.Interfaces.DataTransferObjects;
using Hub.Interfaces;
using Hub.Managers;
using Newtonsoft.Json.Linq;

namespace HubWeb.Controllers
{
    public class DockyardEventController : ApiController
    {
        private readonly IDockyardEvent _dockyardEvent;
        private readonly ICrateManager _crate;
        private IRoute _route;


        public DockyardEventController()
        {
            _dockyardEvent = ObjectFactory.GetInstance<IDockyardEvent>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _route = ObjectFactory.GetInstance<IRoute>();

        }

        [HttpPost]
        [Route("dockyard_events")]
        public async Task<IHttpActionResult> ProcessDockyardEvents(CrateSerializationProxy raw)
        {
            //check if its not null
            if (raw == null)
                throw new ArgumentNullException("Parameter Standard Event Report is null.");

            var curCrateStandardEventReport = _crate.Deserialize(raw);

            //check if Standard Event Report inside CrateDTO
            if (curCrateStandardEventReport.ManifestType.Type != null && !curCrateStandardEventReport.ManifestType.Type.Equals("Standard Event Report", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentNullException("CrateDTO passed is not a Standard Event Report.");
            if (curCrateStandardEventReport.Get<object>() == null)
                throw new ArgumentNullException("CrateDTO Content is empty.");
             
            await _dockyardEvent.ProcessInboundEvents(curCrateStandardEventReport);

            return Ok();
        }
    }
}