﻿using System;
using System.Globalization;
using Fr8Data.DataTransferObjects;
using Intuit.Ipp.Core;
using Intuit.Ipp.Core.Configuration;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Diagnostics;
using Intuit.Ipp.Security;
using StructureMap;
using terminalQuickBooks.Infrastructure;
using terminalQuickBooks.Interfaces;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using Utilities.Configuration.Azure;
using TerminalBase.Models;

namespace terminalQuickBooks.Services
{
    public class ServiceWorker : IServiceWorker
    {
        private static readonly string AppToken =
            CloudConfigurationManager.GetSetting("QuickBooksAppToken").ToString(CultureInfo.InvariantCulture);

        private readonly IAuthenticator _authenticator;

        public ServiceWorker()
        {
            _authenticator = ObjectFactory.GetInstance<IAuthenticator>();
        }

        public ServiceContext CreateServiceContext(AuthorizationToken authToken, string userId, IHubCommunicator hubCommunicator)
        {
            var tokens = authToken.Token.Split(new[] { Authenticator.TokenSeparator }, StringSplitOptions.None);
            var accessToken = tokens[0];
            var accessTokenSecret = tokens[1];
            var companyId = tokens[2];
            var expiresAt = tokens[3];
            DateTime expiresDate;
            if (DateTime.TryParse(expiresAt, out expiresDate) == false)
            {
                EventManager.TokenValidationFailed(authToken.Token, "Terminal Quickbooks token is invalid");
                throw new ArgumentException(nameof(expiresAt));
            }
            // Token renew should fit into 151-180 days period,
            // See https://developer.intuit.com/docs/0100_accounting/0060_authentication_and_authorization/connect_from_within_your_app#/manage
            // 
            if (DateTime.Now > expiresDate.AddDays(-30) && DateTime.Now <= expiresDate)
            {
                authToken = _authenticator.RefreshAuthToken(authToken).Result;
                var tokenDto = new AuthorizationTokenDTO
                {
                    Id = authToken.Id.ToString(),
                    ExternalAccountId = authToken.ExternalAccountId,
                    Token = authToken.Token
                };

                hubCommunicator.RenewToken(tokenDto, userId);

                // After token refresh we need to get new accessToken and accessTokenSecret from it
                tokens = authToken.Token.Split(new[] { Authenticator.TokenSeparator }, StringSplitOptions.None);
                accessToken = tokens[0];
                accessTokenSecret = tokens[1];
            }

            if (DateTime.Now > expiresDate)
            {
                var message = "Quickbooks token is expired. Please, get the new one";
                EventManager.TokenValidationFailed(authToken.Token, message);
                throw new TerminalQuickbooksTokenExpiredException(message);
            }

            var oauthValidator = new OAuthRequestValidator(
                accessToken,
                accessTokenSecret,
                Authenticator.ConsumerKey,
                Authenticator.ConsumerSecret);
            return new ServiceContext(AppToken, companyId, IntuitServicesType.QBO, oauthValidator);
        }

        public DataService GetDataService(AuthorizationToken authToken, string userId, IHubCommunicator hubCommunicator)
        {
            var curServiceContext = CreateServiceContext(authToken, userId, hubCommunicator);
            //Modify required settings for the Service Context
            curServiceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
            curServiceContext.IppConfiguration.MinorVersion.Qbo = "4";
            curServiceContext.IppConfiguration.Logger.RequestLog.EnableRequestResponseLogging = true;
            curServiceContext.IppConfiguration.Logger.CustomLogger = new TraceLogger();
            curServiceContext.IppConfiguration.Message.Response.SerializationFormat = SerializationFormat.Json;

            var curDataService = new DataService(curServiceContext);
            curServiceContext.UseDataServices();
            return curDataService;
        }
    }
}