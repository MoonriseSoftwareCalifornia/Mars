using System.ComponentModel.DataAnnotations;

namespace Cosmos.IdentityManagement.Website.Data
{
    /// <summary>
    /// Cosmos Identity Manager Setting
    /// </summary>
    public class CimSetting
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(64)]
        [Display(Name = "Key Name")]
        public string KeyName { get; set; }

        [Required]
        [Display(Name = "Key Value")]
        public string Value { get; set; }
    }
}
