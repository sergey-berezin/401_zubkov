using Core.WebApi.Models.Entities;
using Microsoft.EntityFrameworkCore;


namespace Core.WebApi.Context
{
    public class RecognizedImagesDb : DbContext
    {
        public DbSet<RecognizedImageEntity> RecognizedImages { get; set; }
        public DbSet<CategoryEntity> Categories { get; set; }

        public RecognizedImagesDb(DbContextOptions<RecognizedImagesDb> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
