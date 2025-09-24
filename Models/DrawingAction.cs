 using System.ComponentModel.DataAnnotations;

namespace CollaborativeWhiteboard.Models
{
    public class DrawingAction
    {
        public String Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public String SessionId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string ActionType { get; set; } = string.Empty; // "draw", "erase", "clear"
        
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        
        [StringLength(7)]
        public string Color { get; set; } = "#000000";
        
        public float LineWidth { get; set; } = 2;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public WhiteboardSession? Session { get; set; }
    }
}
