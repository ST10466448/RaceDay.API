using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
using RaceDayRoute = RaceDay.API.Models.Route;

namespace RaceDay.API.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(
            DbContextOptions<RaceDayDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<RaceDayRoute> Routes => Set<RaceDayRoute>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventCategory> EventCategories => Set<EventCategory>();
        public DbSet<Entry> Entries => Set<Entry>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EventCategory>()
               .Property(c => c.DistanceKM)
               .HasPrecision(10, 2);

            modelBuilder.Entity<EventCategory>()
                .Property(c => c.EntryFee)
                .HasPrecision(10, 2);

            modelBuilder.Entity<RaceDayRoute>()
                .Property(r => r.DistanceKM)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Result>()
                .Property(r => r.AveragePace)
                .HasPrecision(10, 2);

            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.DateCreated)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Users_Role",
                    "[Role] IN ('Organiser', 'Participant')"));

            // Routes
            modelBuilder.Entity<RaceDayRoute>()
     .ToTable(t => t.HasCheckConstraint(
         "CK_Routes_DistanceKM",
         "[DistanceKM] > 0"));

            // Events: one organiser and one route per event
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Route)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.RouteID)
                .OnDelete(DeleteBehavior.Restrict);

            // Event categories
            modelBuilder.Entity<EventCategory>()
                .HasKey(c => c.CategoryID);

            modelBuilder.Entity<EventCategory>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EventCategory>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventCategory>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_EventCategories_DistanceKM",
                        "[DistanceKM] > 0");

                    t.HasCheckConstraint(
                        "CK_EventCategories_EntryFee",
                        "[EntryFee] >= 0");

                    t.HasCheckConstraint(
                        "CK_EventCategories_MaxParticipants",
                        "[MaxParticipants] > 0");
                });

            // Entries
            modelBuilder.Entity<Entry>()
                .HasOne(e => e.Participant)
                .WithMany()
                .HasForeignKey(e => e.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Entry>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Entries)
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Entry>()
                .HasIndex(e => e.RaceNumber)
                .IsUnique()
                .HasFilter("[RaceNumber] IS NOT NULL");

            modelBuilder.Entity<Entry>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Entries_EntryStatus",
                    "[EntryStatus] IN ('Pending', 'Confirmed', 'Cancelled')"));

            // Results: each entry can have at most one result
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Entry)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EntryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Result>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Results_Position",
                        "[Position] > 0");

                    t.HasCheckConstraint(
                        "CK_Results_FinishTime",
                        "[FinishTime] > '00:00:00'");

                    t.HasCheckConstraint(
                        "CK_Results_AveragePace",
                        "[AveragePace] > 0");
                });
        }
    }
}