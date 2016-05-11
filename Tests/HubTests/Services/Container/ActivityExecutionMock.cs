﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Fr8Data.Constants;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Hub.Interfaces;
using Newtonsoft.Json;

namespace HubTests.Services.Container
{
    public class ActivityServiceMock : IActivity
    {
        private readonly IActivity _activity;

        public readonly List<ActivityExecutionCall> ExecutedActivities = new List<ActivityExecutionCall>();
        public readonly Dictionary<Guid, ActivityMockBase> CustomActivities = new Dictionary<Guid, ActivityMockBase>();

        public ActivityServiceMock(IActivity activity)
        {
            _activity = activity;
        }

        public IEnumerable<TViewModel> GetAllActivities<TViewModel>()
        {
            return _activity.GetAllActivities<TViewModel>();
        }

        public Task<ActivityDTO> SaveOrUpdateActivity(ActivityDO currentActivityDo)
        {
            return _activity.SaveOrUpdateActivity(currentActivityDo);
        }

        public Task<ActivityDTO> Configure(IUnitOfWork uow, string userId, ActivityDO curActivityDO, bool saveResult = true)
        {
            return _activity.Configure(uow, userId, curActivityDO, saveResult);
        }

        public ActivityDO GetById(IUnitOfWork uow, Guid id)
        {
            return _activity.GetById(uow, id);
        }

        public ActivityDO MapFromDTO(ActivityDTO curActivityDTO)
        {
            return _activity.MapFromDTO(curActivityDTO);
        }

        public Task<PlanNodeDO> CreateAndConfigure(IUnitOfWork uow, string userId, Guid activityTemplateId, string label = null, string name = null, int? order = null, Guid? parentNodeId = null, bool createPlan = false, Guid? authorizationTokenId = null)
        {
            return _activity.CreateAndConfigure(uow, userId, activityTemplateId, label, name, order, parentNodeId, createPlan, authorizationTokenId);
        }

        public Task<PayloadDTO> Run(IUnitOfWork uow, ActivityDO curActivityDO, ActivityExecutionMode curActionExecutionMode, ContainerDO curContainerDO)
        {
            ActivityMockBase activity;

            var payload = new PayloadDTO(curContainerDO.Id)
            {
                CrateStorage = CrateStorageSerializer.Default.ConvertToDto(CrateStorageSerializer.Default.ConvertFromDto(JsonConvert.DeserializeObject<CrateStorageDTO>(curContainerDO.CrateStorage)))
            };

            ExecutedActivities.Add(new ActivityExecutionCall(curActionExecutionMode, curActivityDO.Id));

            if (CustomActivities.TryGetValue(curActivityDO.Id, out activity))
            {
                activity.Run(curActivityDO.Id, curActionExecutionMode, payload);
            }

            return Task.FromResult(payload);
        }

        public Task<ActivityDTO> Activate(ActivityDO curActivityDO)
        {
            return Task.FromResult(Mapper.Map<ActivityDTO>(curActivityDO));
        }

        public Task<ActivityDTO> Deactivate(ActivityDO curActivityDO)
        {
            return Task.FromResult(Mapper.Map<ActivityDTO>(curActivityDO));
        }

        Task<T> IActivity.GetActivityDocumentation<T>(ActivityDTO curActivityDTO, bool isSolution)
        {
            return _activity.GetActivityDocumentation<T>(curActivityDTO, isSolution);
        }

        public List<string> GetSolutionNameList(string terminalName)
        {
            return _activity.GetSolutionNameList(terminalName);
        }

        public void Delete(Guid id)
        {
            _activity.Delete(id);
        }
    }

}