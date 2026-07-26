namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StockLedgerAndCategory : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StockMovements",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        StockItemID = c.Int(nullable: false),
                        ChangeAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ResultingQuantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Reason = c.String(nullable: false, maxLength: 100),
                        RelatedProductionRecordID = c.Int(),
                        UserID = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            AddColumn("dbo.StockItems", "Category", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StockItems", "Category");
            DropTable("dbo.StockMovements");
        }
    }
}
