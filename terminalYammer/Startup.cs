﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;
using TerminalBase;
using TerminalBase.BaseClasses;
using System.Threading.Tasks;
using System.Web.Http.Dispatcher;

[assembly: OwinStartup(typeof(terminalYammer.Startup))]

namespace terminalYammer
{
    public class Startup : BaseConfiguration
    {
        public void Configuration(IAppBuilder app, bool selfHost)
        {
            ConfigureProject(selfHost, null);
            WebApiConfig.Register(_configuration);
            app.UseWebApi(_configuration);
            StartHosting("terminalYammer");
        }

        public void Configuration(IAppBuilder app)
        {
            Configuration(app, false);
        }

        public override ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            return new Type[] {
                    typeof(Controllers.ActivityController),
                    typeof(Controllers.TerminalController),
                    typeof(Controllers.AuthenticationController)
                };
        }
    }
}