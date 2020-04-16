using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using BungieNetApi.Model;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public static class Definitions
    {
        private static SqliteConnection db;
        public static Task OpeningTask;

        public static int HashToDbHash(uint hash)
        {
            return unchecked((int)hash);
        }

        public static async void InitializeDatabase()
        {
            var installationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            var dbpath = Path.Combine(installationFolder, "Assets", "definitions.sqlite");

            Debug.WriteLine($"db path is {dbpath}");

            db = new SqliteConnection($"Filename={dbpath}");
            OpeningTask = db.OpenAsync();

            await OpeningTask;
        }

        public static async Task<T> GetDefinition<T>(string command, uint hash)
        {
            SqliteCommand selectCommand = new SqliteCommand(command, db);
            selectCommand.Parameters.AddWithValue("@Hash", HashToDbHash(hash));

            SqliteDataReader query = await selectCommand.ExecuteReaderAsync();

            query.Read();
            var json = query.GetString(0);

            var data = JsonConvert.DeserializeObject<T>(json);

            return data;
        }

        public static async Task<DestinyDefinitionsDestinyInventoryItemDefinition> GetItemDefinition(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyInventoryItemDefinition>("SELECT json FROM DestinyInventoryItemDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyObjectiveDefinition> GetObjectiveDefinition(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyObjectiveDefinition>("SELECT json FROM DestinyObjectiveDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyClassDefinition> GetClassDefinition(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyClassDefinition>("SELECT json FROM DestinyClassDefinition WHERE id = @Hash;", hash);
        }
    }
}