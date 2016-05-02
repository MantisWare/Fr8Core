﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Data.Entities;
using Hub.Managers.APIManagers.Transmitters.Restful;
using ServiceStack.Text;
using terminalSlack.Interfaces;
using terminalSlack.RtmClient;
using Utilities.Configuration.Azure;
using Utilities.Logging;

namespace terminalSlack.Services
{
    public class SlackEventManager : ISlackEventManager
    {
        private readonly IRestfulServiceClient _resfultClient;

        private readonly Dictionary<string, SlackClientWrapper> _clientsByTeamId = new Dictionary<string, SlackClientWrapper>();

        private readonly Dictionary<Guid, string> _teamIdByActivity = new Dictionary<Guid, string>();
        
        private readonly object _locker = new object();

        private readonly Uri _eventsUri;

        private bool _disposed;

        public SlackEventManager(IRestfulServiceClient resfultClient)
        {
            if (resfultClient == null)
            {
                throw new ArgumentNullException(nameof(resfultClient));
            }
            _resfultClient = resfultClient;
            _eventsUri = new Uri($"{CloudConfigurationManager.GetSetting("terminalSlack.TerminalEndpoint")}/terminals/terminalslack/events", UriKind.Absolute);
        }

        public Task Subscribe(AuthorizationTokenDO token, Guid activityId)
        {
            //Logger.GetLogger().Info($"SlackEventManager: subscribing on thread {Thread.CurrentThread.ManagedThreadId}");
            Logger.LogInfo($"SlackEventManager: subscribing on thread {Thread.CurrentThread.ManagedThreadId}, ActivityId = {activityId}");
            lock (_locker)
            {
                if (_disposed)
                {
                    //Logger.GetLogger().Info("SlackEventManager: can't subscribe to disposed object");
                    Logger.LogWarning($"SlackEventManager: can't subscribe to disposed object. ActivityId = {activityId}");
                    return Task.FromResult(0);
                }
                Unsubscribe(activityId);
                SlackClientWrapper client;
                var teamId = token.ExternalDomainId;
                if (!_clientsByTeamId.TryGetValue(teamId, out client))
                {
                    //Logger.GetLogger().Info("SlackEventManager: creating new subscription and opening socket");
                    Logger.LogInfo("SlackEventManager: creating new subscription and opening socket");
                    //This team doesn't have subscription yet - create a new subscription
                    client = new SlackClientWrapper(token.Token);
                    client.MessageReceived += OnMessageReceived;
                    _clientsByTeamId.Add(teamId, client);
                }
                client.Subscribe(activityId);
                _teamIdByActivity[activityId] = teamId;
                var result = client.Connect();
                result.ContinueWith(x => { if (x.IsFaulted) OnSubscriptionFailed(client); }, TaskContinuationOptions.OnlyOnFaulted);
                return result;
            }
        }

        private void OnSubscriptionFailed(SlackClientWrapper client)
        {
            //Logger.GetLogger().Info($"SlackEventManager: subscription fail on thread {Thread.CurrentThread.ManagedThreadId}");
            Logger.LogWarning($"SlackEventManager: subscription fail on thread {Thread.CurrentThread.ManagedThreadId}. ActivityId's = {String.Join(", ", client.SubscribedActivities)}");
            lock (_locker)
            {
                foreach (var acitivityId in client.SubscribedActivities)
                {
                    _teamIdByActivity.Remove(acitivityId);
                }
                _clientsByTeamId.Remove(client.SlackData.Team.Id);
                client.Dispose();
            }
        }

        public void Unsubscribe(Guid activityId)
        {
            //Logger.GetLogger().Info($"SlackEventManager: usubscribing in thread {Thread.CurrentThread.ManagedThreadId}");
            Logger.LogInfo($"SlackEventManager: usubscribing in thread {Thread.CurrentThread.ManagedThreadId}");
            lock (_locker)
            {
                if (_disposed)
                {
                    return;
                }
                string existingTeamId;
                if (_teamIdByActivity.TryGetValue(activityId, out existingTeamId))
                {
                    _teamIdByActivity.Remove(activityId);
                }
                SlackClientWrapper client;
                if (!string.IsNullOrEmpty(existingTeamId) && _clientsByTeamId.TryGetValue(existingTeamId, out client))
                {
                    //We've removed last subscription - disconnect from RTM websocket and remove it
                    if (client.Unsubsribe(activityId))
                    {
                        //Logger.GetLogger().Info("SlackEventManager: unsubscribing closes socket");
                        Logger.LogInfo($"SlackEventManager: unsubscribing closes socket, ActivitieId's = {string.Join(", ",client.SubscribedActivities)}");
                        _clientsByTeamId.Remove(existingTeamId);
                        client.MessageReceived -= OnMessageReceived;
                        client.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            //Logger.GetLogger().Info("SlackEventManager: Dispose() closes all clients");
            Logger.LogInfo("SlackEventManager: Dispose() closes all clients");
            lock (_locker)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                //This is to perform graceful exit in case of terminal shutdown
                foreach (var client in _clientsByTeamId.Values)
                {
                    client.Dispose();
                }
            }
        }

        private async void OnMessageReceived(object sender, DataEventArgs<WrappedMessage> e)
        {
            //Logger.GetLogger().Info("SlackEventManager: message is received");
            Logger.LogInfo($"SlackEventManager: message is received. Slack UserName = {e.Data.UserId}");
            //The naming conventions of message property is for backwards compatibility with existing event processing logic
            var valuePairs = new List<KeyValuePair<string, string>>
                             {
                                 new KeyValuePair<string, string>("team_id", e.Data.TeamId),
                                 new KeyValuePair<string, string>("team_domain", e.Data.TeamName),
                                 new KeyValuePair<string, string>("channel_id", e.Data.ChannelId),
                                 new KeyValuePair<string, string>("channel_name", e.Data.ChannelName),
                                 new KeyValuePair<string, string>("timestamp", e.Data.Timestamp.ToUniversalTime().ToUnixTime().ToString()),
                                 new KeyValuePair<string, string>("user_id", e.Data.UserId),
                                 new KeyValuePair<string, string>("user_name", e.Data.UserName),
                                 new KeyValuePair<string, string>("text", e.Data.Text)
                             };
            var encodedMessage = string.Join("&", valuePairs.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            try
            {
                await _resfultClient.PostAsync(_eventsUri, content: encodedMessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                //Logger.GetLogger().Info($"Failed to post event from SlackEventMenager with following payload: {encodedMessage}", ex);
                Logger.LogError($"Failed to post event from SlackEventMenager with following payload: {encodedMessage}. {ex}");
            }
        }
    }
}