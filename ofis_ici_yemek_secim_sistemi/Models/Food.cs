using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("Foods")]
    public class Food
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

      
        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        public int? CategoryID { get; set; }

        [MaxLength(300)]
        public string Allergens { get; set; }

        [MaxLength(500)]
        public string Ingredients { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
