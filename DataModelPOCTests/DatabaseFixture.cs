using DataModelPOC.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace DataModelPOCTests
{
    public class DatabaseFixture : IDisposable
    {

        public CareswitchDbContext CreateContextForSQLite()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var option = new DbContextOptionsBuilder<CareswitchDbContext>().UseSqlite(connection).Options;

            var context = new CareswitchDbContext(option);

            if (context is null)
            {
                throw new Exception("No context");
            }

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return context;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}