namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Companies",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        TaxNumber = c.String(maxLength: 50),
                        Address = c.String(maxLength: 250),
                        ContactEmail = c.String(maxLength: 150),
                        ContactPhone = c.String(maxLength: 20),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.FoodCategories",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 250),
                        DisplayOrder = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.FoodRatings",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        UserID = c.Int(nullable: false),
                        MenuItemID = c.Int(nullable: false),
                        FoodID = c.Int(nullable: false),
                        OverallRating = c.Int(nullable: false),
                        TasteRating = c.Int(),
                        PresentationRating = c.Int(),
                        SatietyRating = c.Int(),
                        Comment = c.String(maxLength: 500),
                        RatingDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Foods",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 150),
                        CategoryID = c.Int(),
                        Allergens = c.String(maxLength: 300),
                        Ingredients = c.String(maxLength: 500),
                        Description = c.String(maxLength: 500),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.MenuItems",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false, storeType: "date"),
                        MealType = c.String(nullable: false, maxLength: 20),
                        FoodID = c.Int(nullable: false),
                        PlannedPortion = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.ProductionRecords",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false, storeType: "date"),
                        MealType = c.String(nullable: false, maxLength: 20),
                        FoodID = c.Int(nullable: false),
                        PlannedDemandQuantity = c.Int(),
                        ProducedQuantity = c.Int(),
                        ActualConsumedQuantity = c.Int(),
                        Note = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.RecipeIngredients",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        FoodID = c.Int(nullable: false),
                        StockItemID = c.Int(nullable: false),
                        RequiredQuantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Unit = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Selections",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        UserID = c.Int(nullable: false),
                        MenuItemID = c.Int(nullable: false),
                        Note = c.String(maxLength: 200),
                        SelectionDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.StockItems",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CompanyID = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 150),
                        Unit = c.String(nullable: false, maxLength: 20),
                        CurrentQuantity = c.Decimal(precision: 18, scale: 2),
                        MinimumQuantity = c.Decimal(precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 150),
                        PasswordHash = c.String(nullable: false, maxLength: 255),
                        Role = c.String(nullable: false, maxLength: 20),
                        CompanyID = c.Int(nullable: false),
                        Department = c.String(maxLength: 100),
                        Location = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Email, unique: true);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Users", new[] { "Email" });
            DropTable("dbo.Users");
            DropTable("dbo.StockItems");
            DropTable("dbo.Selections");
            DropTable("dbo.RecipeIngredients");
            DropTable("dbo.ProductionRecords");
            DropTable("dbo.MenuItems");
            DropTable("dbo.Foods");
            DropTable("dbo.FoodRatings");
            DropTable("dbo.FoodCategories");
            DropTable("dbo.Companies");
        }
    }
}
