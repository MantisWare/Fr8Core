﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using Data.States.Templates;
using StructureMap;
using Data.States.Templates;

namespace Data.Entities
{
    public class ActivityTemplateDO : BaseDO
    {
        public ActivityTemplateDO()
        {
            this.AuthenticationType = States.AuthenticationType.None;
            this.ActivityTemplateState = States.ActivityTemplateState.Active;
        }

        public ActivityTemplateDO(string name, string label, string version, int terminalId) : this()
        {
            this.Name = name;
            this.Label = label;
            this.Version = version;
            /* We don't need to validate terminalId because of EF chack ForeignKey and if terminalId doesn't exist in table Terminals then 
             * EF will throw 'System.Data.Entity.Infrastructure.DbUpdateException'  */
<<<<<<< HEAD
            this.TerminalId = terminalId;
            this.ActivityTemplateState = States.ActivityTemplateState.Active;
=======
            this.TerminalID = terminalId;
>>>>>>> DO-1441
        }

        /// <summary>
        /// Represents a ActionTemplate instance
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        ///<param name="label"></param>
        /// <param name="terminalName">Name of the new TerminalDO</param>
        /*<param name="baseEndPoint">New TerminalDO base end point</param>*/
        /// <param name="Endpoint">New TerminalDO end point</param>
        public ActivityTemplateDO(string name, string version,
            string terminalName, string endPoint, string label = "") : this()
        {

            this.Name = name;
            this.Label = label;
            this.Version = version;

            this.Terminal = new TerminalDO()
            {
                Name = terminalName,
                TerminalStatus = TerminalStatus.Active,
                Endpoint = endPoint
            };
            this.ActivityTemplateState = States.ActivityTemplateState.Active;
        }

        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Label { get; set; }

        public string Tags { get; set; }

        public string Version { get; set; }

        [Required]
        [ForeignKey("AuthenticationTypeTemplate")]
        public int AuthenticationType { get; set; }

        public virtual _AuthenticationTypeTemplate AuthenticationTypeTemplate { get; set; }

        public string ComponentActivities { get; set; }

<<<<<<< HEAD
        [Required]
        [ForeignKey("ActivityTemplateStateTemplate")]
        public int ActivityTemplateState { get; set; }

        public _ActivityTemplateStateTemplate ActivityTemplateStateTemplate { get; set; }

        [ForeignKey("Terminal")]
        public int TerminalId { get; set; }

        public virtual TerminalDO Terminal { get; set; }
=======
        [ForeignKey("Plugin")]
        public int TerminalID { get; set; }
        
        public virtual PluginDO Plugin { get; set; }
>>>>>>> DO-1441

        [Required]
        public ActivityCategory Category { get; set; }

        public int MinPaneWidth { get; set; }

		public int? WebServiceId { get; set; }

		public virtual WebServiceDO WebService { get; set; }
    }
}
