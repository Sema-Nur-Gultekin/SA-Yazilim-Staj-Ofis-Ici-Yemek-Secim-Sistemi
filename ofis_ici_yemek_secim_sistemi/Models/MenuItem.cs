using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("MenuItems")]
    public class MenuItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(20)]
        public string MealType { get; set; } 
     
        [Required]
        public int FoodID { get; set; }

        public int? PlannedPortion { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
