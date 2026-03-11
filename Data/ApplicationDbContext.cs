using KanbanApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Board> Boards { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<BoardMember> BoardMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Board>(entity =>
            {
                entity.HasOne(b => b.Owner)
                    .WithMany(u => u.OwnedBoards)
                    .HasForeignKey(b => b.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(b => b.Columns)
                    .WithOne(c => c.Board)
                    .HasForeignKey(c => c.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Column>(entity =>
            {
                entity.HasMany(c => c.Cards)
                    .WithOne(card => card.Column)
                    .HasForeignKey(card => card.ColumnId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Card>(entity =>
            {
                entity.HasOne(card => card.AssignedTo)
                    .WithMany(u => u.AssignedCards)
                    .HasForeignKey(card => card.AssignedToUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<BoardMember>(entity =>
            {
                entity.HasKey(bm => new { bm.UserId, bm.BoardId });

                entity.HasOne(bm => bm.Board)
                    .WithMany(b => b.BoardMembers)
                    .HasForeignKey(bm => bm.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bm => bm.ApplicationUser)
                    .WithMany(u => u.BoardMembers)
                    .HasForeignKey(bm => bm.UserId);
            });
        }
    }
}
