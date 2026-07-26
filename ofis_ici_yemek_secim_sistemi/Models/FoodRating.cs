using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("FoodRatings")]
    public class FoodRating
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

        [Required]
        public int FoodID { get; set; }

        [Required]
        [Range(1, 5)]
        public int OverallRating { get; set; } 

        [Range(1, 5)]
        public int? TasteRating { get; set; } 

        [Range(1, 5)]
        public int? PresentationRating { get; set; } 

        [Range(1, 5)]
        public int? SatietyRating { get; set; } 

        [MaxLength(500)]
        public string Comment { get; set; }

        public DateTime RatingDate { get; set; } = DateTime.Now;
    }
}
