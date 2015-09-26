namespace EbolaResponse.DataAccess
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EbolaResponse.Context;
    using EbolaResponse.Models;

    public class DatabaseProvider : DatabaseContext
    {
        /// <summary>
        /// Lock object for singleton pattern.
        /// </summary>
        private static object lockPoint = new object();

        /// <summary>
        /// Static singleton instance of DatabaseProvider.
        /// </summary>
        private static DatabaseProvider instance;

        /// <summary>
        /// Gets or sets a value ...
        /// </summary>
        public static DatabaseProvider Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockPoint)
                    {
                        if (instance == null)
                        {
                            instance = new DatabaseProvider();
                        }
                    }
                }

                return instance;
            }
        }

        private DatabaseProvider()
        {
        }

        public async Task<List<Zip>> GetZips()
        {
            var db = await base.OpenDatabase();
            return await db.QueryAsync<Zip>("SELECT * FROM Zips");
        }

        public async Task InsertZip(Zip zip)
        {
            var db = await base.OpenDatabase();
            var count = await db.InsertAsync(zip);
        }

        public async Task InsertOrUpdate(Zip zip)
        {
            if (zip.Id == 0)
            {
                await this.InsertZip(zip);                
            }
            else
            {
                await  this.UpdateZip(zip);
            }
        }

        public async Task InsertZip(List<Zip> zipList)
        {
            var db = await base.OpenDatabase();
            var count = await db.InsertAllAsync(zipList);
        }

        public async Task UpdateZip(Zip zip)
        {
            var db = await base.OpenDatabase();
            var count = await db.UpdateAsync(zip);
        }

        public async Task DeleteZip(Zip zip)
        {
            var db = await base.OpenDatabase();
            var count = await db.DeleteAsync(zip);
        }

        public async Task DeleteZips(IEnumerable<int> ids)
        {
            var db = await base.OpenDatabase();
            var count = await db.ExecuteAsync(string.Format("DELETE FROM Zips WHERE Id IN ({0})", string.Join(",", ids)));
        }
    }
}
