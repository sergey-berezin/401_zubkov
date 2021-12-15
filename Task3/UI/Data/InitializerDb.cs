using System.Threading.Tasks;
using Core.DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;


namespace UI.Data
{
    internal class InitializerDb
    {
        private readonly RecognizedImagesDb db;

        public InitializerDb(RecognizedImagesDb db)
        {
            this.db = db;
        }

        public async Task InitializeAsync()
        {
            //await db.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await db.Database.MigrateAsync().ConfigureAwait(false);
        }
    }
}
