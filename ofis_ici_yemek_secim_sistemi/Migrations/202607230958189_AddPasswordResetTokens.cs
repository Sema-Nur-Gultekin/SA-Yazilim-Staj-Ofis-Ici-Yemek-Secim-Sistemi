namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPasswordResetTokens : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PasswordResetTokens",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        UserID = c.Int(nullable: false),
                        Token = c.String(nullable: false, maxLength: 200),
                        ExpiresAt = c.DateTime(nullable: false),
                        IsUsed = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Token, unique: true);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.PasswordResetTokens", new[] { "Token" });
            DropTable("dbo.PasswordResetTokens");
        }
    }
}
