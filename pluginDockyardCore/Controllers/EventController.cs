﻿using System.Web.Http;
using StructureMap;

namespace pluginDockyardCore.Controllers
{
    public class EventController : ApiController
    {
        [HttpPost]
        [Route("events")]
        public void ProcessIncomingNotification()
        {
            //Implement the processing logic of dockyard core plugin
        }
    }
}
