using LMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.API.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasData(
            new Category { Id = Guid.Parse("10000000-0000-4000-8000-000000000001"), Name = "Programming", CreatedAt = SeedDate },
            new Category { Id = Guid.Parse("10000000-0000-4000-8000-000000000002"), Name = "Databases", CreatedAt = SeedDate },
            new Category { Id = Guid.Parse("10000000-0000-4000-8000-000000000003"), Name = "Web Development", CreatedAt = SeedDate },
            new Category { Id = Guid.Parse("10000000-0000-4000-8000-000000000004"), Name = "Computer Science Fundamentals", CreatedAt = SeedDate },
            new Category { Id = Guid.Parse("10000000-0000-4000-8000-000000000005"), Name = "Mathematics", CreatedAt = SeedDate },
            new Category { Id = Guid.Parse("10000000-0000-4000-8000-000000000006"), Name = "Soft Skills", CreatedAt = SeedDate }
        );
    }
}