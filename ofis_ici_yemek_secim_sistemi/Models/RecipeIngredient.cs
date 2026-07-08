using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("RecipeIngredients")]
    public class RecipeIngredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int FoodID { get; set; } 

        [Required]
        public int StockItemID { get; set; } 

        [Required]
        public decimal RequiredQuantity { get; set; } 

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } 
    }
}
