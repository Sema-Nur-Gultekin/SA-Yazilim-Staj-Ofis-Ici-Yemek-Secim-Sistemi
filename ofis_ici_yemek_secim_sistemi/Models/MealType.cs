using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("MealTypes")]
    public class MealType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

      
        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; } 

    
        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
