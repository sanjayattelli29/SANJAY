using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// this class stores notification alerts for users
namespace Domain.Entities
{
    public class Notification
    {
        // unique id for each notification
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // who this notification is for
        [Required]
        public string UserId { get; set; } = string.Empty;

        // link to the user object
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // short heading of the notification
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // detailed message text
        [Required]
        public string Message { get; set; } = string.Empty;

        // when this notification was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // whether user has seen this notification
        public bool IsRead { get; set; } = false;

        // category of notification like policy or claim
        [MaxLength(50)]
        public string NotificationType { get; set; } = "General";
    }
}