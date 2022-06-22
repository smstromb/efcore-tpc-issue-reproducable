using Microsoft.EntityFrameworkCore;

public abstract class Animal
{
    public int Id { get; set; }
    public string Species { get; set; }
}

public class FarmAnimal : Animal
{
    public decimal Value { get; set; }
}

public abstract class Pet : Animal
{
    public string Name { get; set; }
}

public class Cat : Pet
{
    public string EducationLevel { get; set; }
}

public class Dog : Pet
{
    public string FavoriteToy { get; set; }
}

public class SomeDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            .UseSqlite("Data Source=database.db")
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging();


    public DbSet<Cat> Cats { get; set; }
    public DbSet<Dog> Dogs { get; set; }
    public DbSet<FarmAnimal> FarmAnimals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>().UseTpcMappingStrategy();
    }
}