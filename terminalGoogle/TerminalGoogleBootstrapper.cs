﻿using StructureMap;
using terminalGoogle.Interfaces;
using terminalGoogle.Services;
using terminalGoogle.Services.Authorization;
using Fr8Infrastructure.StructureMap;
using Fr8Data.Managers;

namespace terminalGoogle
{
    public static class TerminalGoogleBootstrapper
	{
        public static void ConfigureGoogleDependencies(this StructureMap.IContainer container, StructureMapBootStrapper.DependencyType type)
        {
            switch (type)
            {
                case StructureMapBootStrapper.DependencyType.TEST:
                    container.Configure(ConfigureLive); // no test mode yet
                    break;

                case StructureMapBootStrapper.DependencyType.LIVE:
                    container.Configure(ConfigureLive);
                    break;
            }
        }

        /**********************************************************************************/

        public static void ConfigureLive(ConfigurationExpression configurationExpression)
        {
            configurationExpression.For<IGoogleIntegration>().Use<GoogleIntegration>();
            configurationExpression.For<IGoogleSheet>().Use<GoogleSheet>();
            configurationExpression.For<ICrateManager>().Use<CrateManager>();
        }

	}
}