using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{
   
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(150)]
        public string ActionName { get; set; }

        
        public int? AffectedRecordID { get; set; }

        [Required]
        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}
