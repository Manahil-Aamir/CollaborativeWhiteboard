 using System.ComponentModel.DataAnnotations;

namespace CollaborativeWhiteboard.Models
{
    public class WhiteboardSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public ICollection<DrawingAction> DrawingActions { get; set; } = new List<DrawingAction>();
    }
}