namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFoodImagePath : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Foods", "ImagePath", c => c.String(maxLength: 300));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Foods", "ImagePath");
        }
    }
}
