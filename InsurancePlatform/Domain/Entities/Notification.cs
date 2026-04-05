using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// this class stores notification alerts for users
namespace Domain.Entities
{
    /// <summary>
    /// This class represents a "Notification" or "Alert" sent to a user.
    /// It shows up in their inbox and tells them things like "Your claim was approved".
    /// </summary>
    public class Notification
    {
        // A unique code for each notification alert.
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // The ID of the person who should see this message.
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Links to the full User account details.
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // A short title for the alert (e.g., "Policy Expiring").
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // The full text message of the notification.
        [Required]
        public string Message { get; set; } = string.Empty;

        // The exact date and time the alert was sent.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // "True" if the user has clicked on or read the message.
        public bool IsRead { get; set; } = false;

        // The category of alert, like "Claim", "Policy", or "General".
        [MaxLength(50)]
        public string NotificationType { get; set; } = "General";
    }
}