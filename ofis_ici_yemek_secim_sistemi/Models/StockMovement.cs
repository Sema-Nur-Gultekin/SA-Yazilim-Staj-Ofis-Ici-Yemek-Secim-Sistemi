using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("StockMovements")]
    public class StockMovement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int StockItemID { get; set; }

        [Required]
        public decimal ChangeAmount { get; set; }

       
        [Required]
        public decimal ResultingQuantity { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } 

 
        public int? RelatedProductionRecordID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
