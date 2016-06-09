﻿using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Interfaces;
using Fr8.TerminalBase.BaseClasses;
using Newtonsoft.Json;
using StructureMap;
using terminalGoogle.Interfaces;
using terminalGoogle.Services.Authorization;

namespace terminalGoogle.Controllers
{
    [RoutePrefix("authentication")]
    public class AuthenticationController : BaseTerminalController
    {
        private const string curTerminal = "terminalGoogle";

        private readonly IGoogleIntegration _googleIntegration;


        public AuthenticationController(IRestfulServiceClient restfulServiceClient)
            : base(restfulServiceClient)
        {
            _googleIntegration = new GoogleIntegration(restfulServiceClient);
        }

        [HttpPost]
        [Route("initial_url")]
        public ExternalAuthUrlDTO GenerateOAuthInitiationURL()
        {
            var externalStateToken = Guid.NewGuid().ToString();
            var url = _googleIntegration.CreateOAuth2AuthorizationUrl(externalStateToken);

            var externalAuthUrlDTO = new ExternalAuthUrlDTO()
            {
                ExternalStateToken = externalStateToken,
                Url = url
            };

            return externalAuthUrlDTO;
        }

        [HttpPost]
        [Route("token")]
        public async Task<AuthorizationTokenDTO> GenerateOAuthToken(ExternalAuthenticationDTO externalAuthDTO)
        {
            try
            {
                var query = HttpUtility.ParseQueryString(externalAuthDTO.RequestQueryString);
                string code = query["code"];
                string state = query["state"];

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    throw new ApplicationException("Code or State is empty.");
                }

                var oauthToken = _googleIntegration.GetToken(code);
                var email = await _googleIntegration.GetExternalUserId(oauthToken);

                return new AuthorizationTokenDTO()
                {
                    Token = JsonConvert.SerializeObject(oauthToken),
                    ExternalStateToken = state,
                    ExternalAccountId = email
                };
            }
            catch (Exception ex)
            {
                ReportTerminalError(curTerminal, ex, externalAuthDTO.Fr8UserId);

                return new AuthorizationTokenDTO()
                {
                    Error = "An error occurred while trying to authorize, please try again later."
                };
            }
        }
    }
}