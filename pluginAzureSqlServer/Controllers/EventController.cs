﻿using System.Web.Http;
using Core.Interfaces;
using StructureMap;

namespace pluginAzureSqlServer.Controllers
{
    public class EventController : ApiController
    {
        private const string curPlugin = "pluginAzureSqlServer";
        private IEvent _event;

        public EventController()
        {
            _event = ObjectFactory.GetInstance<IEvent>();
        }

        [HttpPost]
        [Route("events")]
        public async void ProcessIncomingNotification()
        {
            _event.Process(curPlugin, await Request.Content.ReadAsStringAsync());
        }
    }
}
