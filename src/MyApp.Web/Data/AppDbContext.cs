using Microsoft.EntityFrameworkCore;
using MyApp.Web.Models;

namespace MyApp.Web.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}
