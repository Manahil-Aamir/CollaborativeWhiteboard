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

            // WhiteboardSession config
            modelBuilder.Entity<WhiteboardSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnType("varchar(36)")
                      .IsRequired();

                entity.Property(e => e.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.CreatedDate)
                      .HasColumnType("datetime")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.LastModified)
                      .HasColumnType("datetime")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAddOrUpdate();
            });

            // DrawingAction config
            modelBuilder.Entity<DrawingAction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasColumnType("varchar(36)")
                      .IsRequired();

                entity.Property(e => e.SessionId)
                      .HasColumnType("varchar(36)")
                      .IsRequired();

                entity.Property(e => e.Timestamp)
                      .HasColumnType("datetime")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAdd();

                entity.HasOne(d => d.Session)
                      .WithMany(s => s.DrawingActions)
                      .HasForeignKey(d => d.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}
