using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restoran.Data.Entities
{
    [Table("MalzemeTalepleri")]
    public class MalzemeTalep
    {
        [Key]
        public int TalepId { get; set; }

        [Required]
        public int MalzemeId { get; set; }

        [ForeignKey("MalzemeId")]
        public virtual Malzemeler Malzeme { get; set; }

        [Required]
        public int Miktar { get; set; }

        [Required]
        [MaxLength(20)]
        public string Birim { get; set; } = "adet";

        [Required]
        [MaxLength(100)]
        public string TalepEden { get; set; } = string.Empty;

        public int? PersonelId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Durum { get; set; } = "BEKLIYOR";

        [MaxLength(500)]
        public string? Aciklama { get; set; }

        public DateTime TalepTarihi { get; set; } = DateTime.Now;

        // ✅ BU ALANLARI EKLEYİN (Eğer veritabanında yoksa migration oluşturun)
        public DateTime? CevaplamaTarihi { get; set; }

        [MaxLength(100)]
        public string? Cevaplayan { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
