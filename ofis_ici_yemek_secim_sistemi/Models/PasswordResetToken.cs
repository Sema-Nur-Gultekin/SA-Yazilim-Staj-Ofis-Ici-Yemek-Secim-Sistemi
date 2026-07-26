using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ofis_ici_yemek_secim_sistemi.Models
{

    [Table("PasswordResetTokens")]
    public class PasswordResetToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(200)]
        [Index(IsUnique = true)]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsUsed { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
