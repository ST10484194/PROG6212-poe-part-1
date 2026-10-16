using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RoleID);
            e.Property(x => x.RoleName).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
            e.HasData(
                new Role { RoleID = 1, RoleName = "Organiser" },
                new Role { RoleID = 2, RoleName = "Participant" });
        });

        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContactNumber).HasMaxLength(20);
            e.Property(x => x.ProfilePictureUrl).HasMaxLength(255);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Events", t => t.HasCheckConstraint(
                "CK_Events_EventType", "EventType IN ('Run','Walk','Cycle')"));
            e.HasKey(x => x.EventID);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventDate).HasColumnType("datetime");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(6,2)");
            e.Property(x => x.EventType).HasMaxLength(10).IsRequired().HasDefaultValue("Run");
            e.Property(x => x.BannerImageUrl).HasMaxLength(255);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(6,2)");
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint(
                "CK_Enrolments_Status", "EnrolmentStatus IN ('Pending','Confirmed')"));
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.ParticipantID, x.EventID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.RecordedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EnrolmentID).IsUnique();
        });

        foreach (var p in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(string)))
        {
            p.SetIsUnicode(false);
        }
    }
}