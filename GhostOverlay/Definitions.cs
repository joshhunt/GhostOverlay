using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using BungieNetApi.Model;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public static class Definitions
    {
        private static SqliteConnection db;
        public static Task<string> Ready;

        public static int HashToDbHash(uint hash)
        {
            return unchecked((int)hash);
        }

        public static async void InitializeDatabase()
        {
            Debug.WriteLine("top InitializeDatabase");
            Ready = ActuallyInitializeDatabase();
            Debug.WriteLine("post ActuallyInitializeDatabase");
            await Ready;
        }

        public static async Task<string> ActuallyInitializeDatabase()
        {
            Debug.WriteLine("top ActuallyInitializeDatabase");
            var definitionsPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, "@@NotDownloaded");
            var definitionsExist = !definitionsPath.Equals("@@NotDownloaded") && File.Exists(definitionsPath);
            Debug.WriteLine("after ActuallyInitializeDatabase checks");

            if (!definitionsExist)
            {
                Debug.WriteLine("Definitions don't exist, need to download!");
                definitionsPath = await DownloadDefinitionsDatabase();
            }

            Debug.WriteLine($"definitionsPath is {definitionsPath}");

            db = new SqliteConnection($"Filename={definitionsPath}");
            await db.OpenAsync();

            Debug.WriteLine("Opened database!");

            AppState.WidgetData.DefinitionsPath = definitionsPath;

            return definitionsPath;
        }

        public static async Task<string> DownloadDefinitionsDatabase()
        {
            var appData = ApplicationData.Current;
            var language = AppState.ReadSetting(SettingsKey.Language, "en");
            var manifest = await AppState.bungieApi.GetManifest();
            var remotePath = manifest.MobileWorldContentPaths[language];
            var urlString = $"https://www.bungie.net{remotePath}";

            Debug.WriteLine($"remotePath: {remotePath}");
            Debug.WriteLine($"urlString: {urlString}");

            // TODO: check to see if it's already downloaded?

            var source = new Uri(urlString);
            Debug.WriteLine($"source {source}");

            var baseName = Path.GetFileNameWithoutExtension(remotePath);
            var destFileName = $"{baseName}.zip";
            var destinationFile = await appData.LocalCacheFolder.CreateFileAsync(destFileName, CreationCollisionOption.ReplaceExisting);

            Debug.WriteLine($"Downloading {source} to {destinationFile.Path}");

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, destinationFile);

            // Attach progress and completion handlers.
            HandleDownloadAsync(download, true);

            await download.StartAsync();

            Debug.WriteLine("maybe the download finished?");

            Debug.WriteLine("Unzipping");
            try
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(destinationFile.Path, appData.LocalCacheFolder.Path));
            }
            catch (IOException ext)
            {
                Debug.WriteLine("Exception when trying to unzip:");
                Debug.WriteLine(ext);
            }

            var definitionsDbFile = Path.Combine(appData.LocalCacheFolder.Path, $"{baseName}.content");
            Debug.WriteLine($"maybe finished unzipping? {definitionsDbFile}");

            if (!File.Exists(definitionsDbFile))
            {
                Debug.WriteLine("Handle the definitions not existing?");
            }

            AppState.SaveSetting(SettingsKey.DefinitionsPath, definitionsDbFile);
            
            Debug.WriteLine($"All finished, definitions at {definitionsDbFile}");

            return definitionsDbFile;
        }

        private static void HandleDownloadAsync(DownloadOperation download, bool v)
        {
            Debug.WriteLine($"HandleDownloadAsync stats:{download.Progress.Status}, progress: {download.Progress.BytesReceived} / {download.Progress.TotalBytesToReceive}");
        }

        public static async Task<T> GetDefinition<T>(string command, uint hash)
        {
            await Ready;

            SqliteCommand selectCommand = new SqliteCommand(command, db);
            selectCommand.Parameters.AddWithValue("@Hash", HashToDbHash(hash));

            SqliteDataReader query = await selectCommand.ExecuteReaderAsync();

            if (!query.HasRows)
            {
                return default(T);
            }

            query.Read();
            var json = query.GetString(0);

            return JsonConvert.DeserializeObject<T>(json);
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