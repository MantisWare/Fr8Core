﻿using System.Threading.Tasks;
using Data.Interfaces.DataTransferObjects;
using Fr8Data.DataTransferObjects;
using TerminalBase.Models;

namespace terminalQuickBooks.Interfaces
{
    public interface IAuthenticator
    {
        string CreateAuthUrl();
        Task<AuthorizationTokenDTO> GetAuthToken(string oauthToken, string oauthVerifier, string realmId);
        Task<AuthorizationToken> RefreshAuthToken(AuthorizationToken curAuthTokenDO);
    }
}