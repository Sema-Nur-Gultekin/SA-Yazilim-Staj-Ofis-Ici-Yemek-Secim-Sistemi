namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserIsActive : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "IsActive", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "IsActive");
        }
    }
}
