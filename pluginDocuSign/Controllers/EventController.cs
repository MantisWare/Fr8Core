﻿using System.Web.Http;
using Core.Interfaces;
using pluginDocuSign.Services;
using StructureMap;

namespace pluginDocuSign.Controllers
{
    public class EventController : ApiController
    {
        private const string curPlugin = "pluginDocuSign";
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
