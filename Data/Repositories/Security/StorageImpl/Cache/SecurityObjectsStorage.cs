﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Repositories.Security.Entities;
using Fr8Data.DataTransferObjects;
using StructureMap;

namespace Data.Repositories.Security.StorageImpl.Cache
{
    /// <summary>
    /// Wrapper for security objects storage.  
    /// Implements all members of SecurityObjectStorageProvider and hold reference to cache for security objects
    /// Inside all methods for get security data, at first, we check if that data is found inside cache, if not then we return from security data provider and we update cache with that data
    /// Inside all create/update methods at first we create data inside storage provider, and then we update the cache. 
    /// </summary>
    public class SecurityObjectsStorage : ISecurityObjectsStorageProvider
    {
        private readonly ISecurityObjectsCache _cache;
        //todo: provide a context like logic for sqlObjectsStorageProvider and connect it to UnitOfWork.SaveChanges()
        private readonly ISecurityObjectsStorageProvider _securityObjectStorageProvider;

        public SecurityObjectsStorage(ISecurityObjectsCache cache, ISecurityObjectsStorageProvider securityObjectStorageProvider)
        {
            _cache = cache;
            _securityObjectStorageProvider = securityObjectStorageProvider;
        }

        public int InsertRolePermission(RolePermission rolePermission)
        {
            return _securityObjectStorageProvider.InsertRolePermission(rolePermission);
        }

        public int UpdateRolePermission(RolePermission rolePermission)
        {
            return _securityObjectStorageProvider.UpdateRolePermission(rolePermission);
        }

        public int InsertObjectRolePermission(string dataObjectId, Guid rolePermissionId, string dataObjectType, string propertyName = null)
        {
            var affectedRows = _securityObjectStorageProvider.InsertObjectRolePermission(dataObjectId, rolePermissionId, dataObjectType, propertyName);

            InvokeCacheUpdate(dataObjectId);

            return affectedRows;
        }

        public int RemoveObjectRolePermission(string dataObjectId, Guid rolePermissionId, string propertyName = null)
        {
            var affectedRows = _securityObjectStorageProvider.RemoveObjectRolePermission(dataObjectId, rolePermissionId, propertyName);

            InvokeCacheUpdate(dataObjectId);

            return affectedRows;
        }

        public ObjectRolePermissionsWrapper GetRecordBasedPermissionSetForObject(string dataObjectId)
        {
            lock (_cache)
            {
                var permissionSet = _cache.GetRecordBasedPermissionSet(dataObjectId);
                if (permissionSet != null) return permissionSet;

                permissionSet = _securityObjectStorageProvider.GetRecordBasedPermissionSetForObject(dataObjectId);
                if (!permissionSet.RolePermissions.Any() && !permissionSet.Properties.Any())
                    return new ObjectRolePermissionsWrapper();

                _cache.AddOrUpdateRecordBasedPermissionSet(dataObjectId, permissionSet);
                return permissionSet;
            }
        }

        public List<PermissionDTO> GetAllPermissionsForUser(Guid profileId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var result = new List<PermissionDTO>();
                lock (_cache)
                {
                    var permissionSets = _cache.GetProfilePermissionSets(profileId.ToString());
                    if (!permissionSets.Any())
                    {
                        permissionSets = uow.PermissionSetRepository.GetQuery().Where(x => x.ProfileId == profileId).ToList();
                        _cache.AddOrUpdateProfile(profileId.ToString(), permissionSets);
                    }
                    foreach (var set in permissionSets)
                    {
                        result.AddRange(set.Permissions.Select(x=> new PermissionDTO() { Permission = x.Id, ObjectType = set.ObjectType}).ToList());
                    }
                }

                return result;
            }
        }

        public List<int> GetObjectBasedPermissionSetForObject(string dataObjectId, string dataObjectType, Guid profileId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var result = new List<int>();
                lock (_cache)
                {
                    var permissionSets = _cache.GetProfilePermissionSets(profileId.ToString());
                    if (!permissionSets.Any())
                    {
                        permissionSets = uow.PermissionSetRepository.GetQuery().Where(x => x.ProfileId == profileId).ToList();
                        _cache.AddOrUpdateProfile(profileId.ToString(), permissionSets);
                    }
                    result.AddRange(permissionSets.Where(x => x.ObjectType == dataObjectType).SelectMany(l => l.Permissions.Select(m => m.Id)).ToList());
                }

                return result;
            }
        }

        public void SetDefaultObjectSecurity(string dataObjectId, string dataObjectType)
        {
            _securityObjectStorageProvider.SetDefaultObjectSecurity(dataObjectId, dataObjectType);

            InvokeCacheUpdate(dataObjectId);
        }

        private void InvokeCacheUpdate(string dataObjectId)
        {
            lock (_cache)
            {
                //update cache with new ObjectRolePermissions
                var rolePermissions = GetRecordBasedPermissionSetForObject(dataObjectId);
                _cache.AddOrUpdateRecordBasedPermissionSet(dataObjectId, rolePermissions);
            }
        }
    }
}