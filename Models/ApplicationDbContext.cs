 using Microsoft.EntityFrameworkCore;

namespace CollaborativeWhiteboard.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        public DbSet<WhiteboardSession> WhiteboardSessions { get; set; }
        public DbSet<DrawingAction> DrawingActions { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure DrawingAction
            modelBuilder.Entity<DrawingAction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.HasOne(d => d.Session)
                    .WithMany(s => s.DrawingActions)
                    .HasForeignKey(d => d.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.Timestamp);
            });
            
            // Configure WhiteboardSession
            modelBuilder.Entity<WhiteboardSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }
}