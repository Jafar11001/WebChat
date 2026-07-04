using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WebChat.Models;

namespace WebChat.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }


    }
}
