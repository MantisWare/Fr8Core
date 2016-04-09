using System.Security.AccessControl;
using Data.States;

namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OwnerOfObject_Role_Data : DbMigration
    {
        public override void Up()
        {

            string sqlScript = @"

            DECLARE @newRoleId uniqueidentifier
            SET @newRoleId = NEWID()
            
            insert into dbo.AspnetRoles(Id, Name, LastUpdated, CreateDate, Discriminator) values (@newRoleId,'OwnerOfCurrentObject', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00', 'AspNetRolesDO')
            insert into dbo.AspNetUserRoles select Id, @newRoleId, '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00', 'AspNetUserRolesDO' from AspNetUsers

            DECLARE @readRolePrivilegeId uniqueidentifier
            SET @readRolePrivilegeId = NEWID()
            insert into dbo.RolePrivileges(Id, PrivilegeName, RoleId, LastUpdated, CreateDate) values (@readRolePrivilegeId,'ReadObject', @newRoleId, '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00')

            DECLARE @editRolePrivilegeId uniqueidentifier
            SET @editRolePrivilegeId = NEWID()
            insert into dbo.RolePrivileges(Id, PrivilegeName, RoleId, LastUpdated, CreateDate) values (@editRolePrivilegeId,'EditObject', @newRoleId, '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00')
    
            DECLARE @deleteRolePrivilegeId uniqueidentifier
            SET @deleteRolePrivilegeId = NEWID()
            insert into dbo.RolePrivileges(Id, PrivilegeName, RoleId, LastUpdated, CreateDate) values (@deleteRolePrivilegeId,'DeleteObject', @newRoleId, '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00')
            
            insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @readRolePrivilegeId, 'PlanDO', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00' from dbo.Plans
            insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @editRolePrivilegeId, 'PlanDO', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00' from dbo.Plans
            insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @deleteRolePrivilegeId, 'PlanDO', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00' from dbo.Plans

            insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @editRolePrivilegeId, 'ContainerDO', CreateDate, LastUpdated from dbo.Containers
            ";
 
 //insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @readRolePrivilegeId, 'ContainerDO', CreateDate, LastUpdated from dbo.Containers
 //insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @editRolePrivilegeId, 'ContainerDO', CreateDate, LastUpdated from dbo.Containers
 //insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @deleteRolePrivilegeId, 'ContainerDO', CreateDate, LastUpdated from dbo.Containers

 //--insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @readRolePrivilegeId,, 'ActivityDO', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00' from dbo.Actions

 //--insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @editRolePrivilegeId, 'ActivityDO', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00' from dbo.Actions

 //--insert into dbo.ObjectRolePrivileges(ObjectId, RolePrivilegeId, Type, CreateDate, LastUpdated)  select Id, @deleteRolePrivilegeId, 'ActivityDO', '12-10-25 12:32:10 +01:00', '12-10-25 12:32:10 +01:00' from dbo.Actions

 Sql(sqlScript);
        }


        public override void Down()
        {
        }
    }
}
