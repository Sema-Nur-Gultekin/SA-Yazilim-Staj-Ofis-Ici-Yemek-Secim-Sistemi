using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("Selections")]
    public class Selection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int UserID { get; set; } 

        [Required]
        public int MenuItemID { get; set; } 

        [MaxLength(200)]
        public string Note { get; set; }

        public DateTime SelectionDate { get; set; } = DateTime.Now;
    }
}
