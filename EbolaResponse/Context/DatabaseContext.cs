
namespace EbolaResponse.Context
{
    using SQLite;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using EbolaResponse.Models;

    public abstract class DatabaseContext
    {
        private const string ApplicationDataBase = "Database_{0}.db";

        private const int CURRENT_DB_VERSION = 1;

        private static Dictionary<string, SQLiteAsyncConnection> connectionsDictionary = new Dictionary<string, SQLiteAsyncConnection>();

        private static TaskCompletionSource<bool> lockDataBasePointer;

        private static int? userId;

        /// <summary>
        /// Set User database id.
        /// </summary>
        /// <param name="userId"></param>
        public static void SetUserId(int userId)
        {
            DatabaseContext.userId = userId;
        }

        /// <summary>
        /// Get last user database id.
        /// </summary>
        /// <returns></returns>
        private int GetUserId()
        {
            if (!DatabaseContext.userId.HasValue)
            {
                DatabaseContext.userId = 0;//await Basis.Providers.StorageProvider.Instance.ReadValueAsync<int>(LastUserId);
            }
            return DatabaseContext.userId.Value;
        }

        /// <summary>
        /// Async method to open database for make operations.
        /// </summary>
        /// <param name="databaseName">Name of database.</param>
        /// <returns></returns>
        protected async Task<SQLiteAsyncConnection> OpenDatabase()
        {
            string databaseName = string.Format(ApplicationDataBase, this.GetUserId());

            if (lockDataBasePointer != null)
            {
                await lockDataBasePointer.Task;
            }

            SQLiteAsyncConnection connection = null;

            if (!connectionsDictionary.TryGetValue(databaseName, out connection)) ///double check
            {
                if (lockDataBasePointer != null) ///double check
                {
                    await lockDataBasePointer.Task;
                }

                if (!connectionsDictionary.TryGetValue(databaseName, out connection)) ///double check
                {
                    lockDataBasePointer = new TaskCompletionSource<bool>();
                    connection = await this.CreateDatabase(databaseName);
                    connectionsDictionary.Add(databaseName, connection);
                    lockDataBasePointer.SetResult(true);
                    lockDataBasePointer = null;
                }
            }

            return connection;
        }


        /// <summary>
        /// Method to create database if it not exist.
        /// </summary>
        /// <param name="databaseFileName"></param>
        /// <returns></returns>
        private async Task<SQLiteAsyncConnection> CreateDatabase(string databaseFileName)
        {
            bool isFileExist = false;

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EbolaResponse", databaseFileName);

            //var path = Path.Combine(Directory.GetCurrentDirectory(), databaseFileName);
            isFileExist = File.Exists(path);

            SQLiteAsyncConnection sqLiteConnection = this.OpenOrCreateDatabaseFile(path);
            if (!isFileExist)
            {
                 await this.CreateTables(sqLiteConnection);
            }

            return sqLiteConnection;
        }

        /// <summary>
        /// Method to create database file.
        /// </summary>
        /// <returns><see cref="SQLiteAsyncConnection"/></returns>
        protected SQLiteAsyncConnection OpenOrCreateDatabaseFile(string databaseName)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EbolaResponse", databaseName);
   
            try
            {
                return new SQLiteAsyncConnection(filePath, true);
            }
            catch
            {
                throw new Exception("Cannot create or open the database.");
            }
        }

        /// <summary>
        /// Method to create tables into database.
        /// </summary>
        /// <param name="db">Database connection for async calls.</param>
        /// <returns><see cref="Task"/></returns>
        protected async Task CreateTables(SQLiteAsyncConnection db)
        {
            await Task.Run(async () =>
            {
                ///Tables:
                await db.CreateTableAsync<Zip>();


                await db.ExecuteAsync(string.Format("PRAGMA user_version = {0};", CURRENT_DB_VERSION));


            });
        }
    }
}
