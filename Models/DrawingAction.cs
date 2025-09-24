 using System.ComponentModel.DataAnnotations;

namespace CollaborativeWhiteboard.Models
{
    public class DrawingAction
    {
        public String Id { get; set; }
        
        [Required]
        public String SessionId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string ActionType { get; set; } = string.Empty; // "draw", "erase", "clear"
        
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndX { get; set; }
        public float EndY { get; set; }
        
        [StringLength(7)]
        public string Color { get; set; } = "#000000";
        
        public float LineWidth { get; set; } = 2;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public WhiteboardSession? Session { get; set; }
    }
}
