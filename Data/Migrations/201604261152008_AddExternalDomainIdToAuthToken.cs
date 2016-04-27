namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExternalDomainIdToAuthToken : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuthorizationTokens", "ExternalDomainId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuthorizationTokens", "ExternalDomainId");
        }
    }
}
