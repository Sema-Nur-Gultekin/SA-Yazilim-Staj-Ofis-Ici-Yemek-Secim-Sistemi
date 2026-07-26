namespace ofis_ici_yemek_secim_sistemi.Migrations
{
    using System.Data.Entity.Migrations;
    using System.Linq;

    public partial class AddMealTypes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MealTypes",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    CompanyID = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 20),
                    DisplayOrder = c.Int(),
                    IsActive = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.ID);

            // GERİYE DÖNÜK UYUMLULUK: Migration çalıştığında sistemde zaten kayıtlı olan
            // her şirket için, önceden sabit kodlanmış üç öğünü (Sabah/Öğle/Akşam) otomatik
            // olarak "MealTypes" tablosuna ekliyoruz. Böylece mevcut MenuItems kayıtları
            // (MealType alanı string olarak "Sabah"/"Öğle"/"Akşam" tutuyor) hiçbir veri
            // kaybı yaşamadan yeni dinamik yapıyla uyumlu hale gelir.
            Sql(@"
                INSERT INTO dbo.MealTypes (CompanyID, Name, DisplayOrder, IsActive)
                SELECT c.ID, 'Sabah', 1, 1 FROM dbo.Companies c
                UNION ALL
                SELECT c.ID, 'Öğle', 2, 1 FROM dbo.Companies c
                UNION ALL
                SELECT c.ID, 'Akşam', 3, 1 FROM dbo.Companies c
            ");
        }

        public override void Down()
        {
            DropTable("dbo.MealTypes");
        }
    }
}
