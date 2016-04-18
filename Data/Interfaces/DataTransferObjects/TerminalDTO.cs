﻿using Data.Entities;
using Data.States.Templates;
using Newtonsoft.Json;

namespace Data.Interfaces.DataTransferObjects
{
    public class TerminalDTO 
    {
        public TerminalDTO()
        {
            AuthenticationType = States.AuthenticationType.None;
        }

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("terminalStatus")]
        public int TerminalStatus { get; set; }
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("authenticationType")]
        public int AuthenticationType { get; set; }
    }
}