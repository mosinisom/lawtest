using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<LawBranch> LawBranches { get; set; }
    public DbSet<TestCollection> TestCollections { get; set; }
    public DbSet<TestQuestion> TestQuestions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<Question> Questions { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }
    
}