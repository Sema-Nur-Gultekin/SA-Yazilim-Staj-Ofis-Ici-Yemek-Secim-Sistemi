namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddActivityLogs : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ActivityLogs",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        UserID = c.Int(nullable: false),
                        ActionName = c.String(nullable: false, maxLength: 150),
                        AffectedRecordID = c.Int(),
                        ActionTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ActivityLogs");
        }
    }
}
