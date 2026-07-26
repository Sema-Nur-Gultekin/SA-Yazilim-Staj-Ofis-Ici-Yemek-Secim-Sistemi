using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{
    [Table("StockItems")]
    public class StockItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } 

        [Required]
        [MaxLength(20)]
        public string Unit { get; set; } 

        public decimal? CurrentQuantity { get; set; }

        public decimal? MinimumQuantity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
