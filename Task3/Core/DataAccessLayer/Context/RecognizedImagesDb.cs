using Core.DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;


namespace Core.DataAccessLayer.Context
{
    public class RecognizedImagesDb : DbContext
    {
        public DbSet<RecognizedImage> RecognizedImages { get; set; }

        public RecognizedImagesDb(DbContextOptions<RecognizedImagesDb> options) : base(options) { }
    }
}
