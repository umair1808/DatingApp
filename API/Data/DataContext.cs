using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options){}

        public DbSet<AppUser> Users { get; set; }
        public DbSet<UserLike> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder){

            base.OnModelCreating(builder);

            builder.Entity<UserLike>()
                   .HasKey(k => new {k.SourceUserId, k.LikedUserId}); //Setting up composite primary key for the entity

            builder.Entity<UserLike>()
                    .HasOne(s => s.SourceUser)
                    .WithMany(l => l.LikedUsers)
                    .HasForeignKey(s => s.SourceUserId)
                    .OnDelete(DeleteBehavior.Cascade); //take care while using SQL server, Cascade might not work, use NoAction in that case

            builder.Entity<UserLike>()
                    .HasOne(l => l.LikedUser)
                    .WithMany(s => s.LikedByUsers)
                    .HasForeignKey(f => f.LikedUserId)
                    .OnDelete(DeleteBehavior.Cascade);

            // builder.Entity<Message>()
            //         .HasKey(k => new {k.SenderId, k.RecipientId});

            builder.Entity<Message>()
                    .HasOne(s => s.Sender)
                    .WithMany(r => r.MessagesSent)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                    .HasOne(r => r.Recipient)
                    .WithMany(s => s.MessagesReceived)
                    .OnDelete(DeleteBehavior.Restrict);

        }
    }
}