using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using BungieNetApi.Model;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace GhostOverlay
{
    public static class Definitions
    {
        private static readonly string defaultDefinitionsPath = "@@NotDownloaded";
        private static SqliteConnection db;
        public static Task<string> Ready;

        public static int HashToDbHash(uint hash)
        {
            return unchecked((int) hash);
        }

        public static async void InitializeDatabase()
        {
            Ready = ActuallyInitializeDatabase();

            _ = IsDefinitionsLatest();

            await Ready;
        }

        public static async Task<string> ActuallyInitializeDatabase()
        {
            var definitionsPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
            var definitionsExist = !definitionsPath.Equals("@@NotDownloaded") && File.Exists(definitionsPath);

            if (!definitionsExist)
            {
                Debug.WriteLine("Definitions don't exist, need to download!");
                definitionsPath = await DownloadDefinitionsDatabase();
            }

            Debug.WriteLine($"definitionsPath is {definitionsPath}");

            db = new SqliteConnection($"Filename={definitionsPath}");
            await db.OpenAsync();

            AppState.WidgetData.DefinitionsPath = definitionsPath;

            return definitionsPath;
        }

        public static async Task CheckForLatestDefinitions()
        {
            if (await IsDefinitionsLatest())
            {
                return;
            }

            await DownloadDefinitionsDatabase();
            await ActuallyInitializeDatabase();
        }

        public static async Task<string> FetchLatestDefinitionsPath()
        {
            var language = AppState.ReadSetting(SettingsKey.Language, "en");
            var manifest = await AppState.bungieApi.GetManifest();
            var remotePath = manifest.MobileWorldContentPaths[language];

            return $"https://www.bungie.net{remotePath}";
        }

        public static async Task<string> DownloadDefinitionsDatabase()
        {
            var appData = ApplicationData.Current;
            var urlString = await FetchLatestDefinitionsPath();

            Debug.WriteLine($"urlString: {urlString}");

            // TODO: check to see if it's already downloaded?

            var source = new Uri(urlString);
            Debug.WriteLine($"source {source}");

            var baseName = Path.GetFileNameWithoutExtension(urlString);
            var destFileName = $"{baseName}.zip";
            var destinationFile =
                await appData.LocalCacheFolder.CreateFileAsync(destFileName, CreationCollisionOption.ReplaceExisting);

            Debug.WriteLine($"Downloading {source} to {destinationFile.Path}");

            var downloader = new BackgroundDownloader();
            var download = downloader.CreateDownload(source, destinationFile);

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

            if (!File.Exists(definitionsDbFile)) Debug.WriteLine("Handle the definitions not existing?");

            AppState.SaveSetting(SettingsKey.DefinitionsPath, definitionsDbFile);

            Debug.WriteLine($"All finished, definitions at {definitionsDbFile}");

            return definitionsDbFile;
        }

        private static async Task<bool> IsDefinitionsLatest()
        {
            var currentPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);

            if (currentPath.Equals("@@NotDownloaded")) return false;

            var latestRemotePath = await FetchLatestDefinitionsPath();

            var currentBathPath = Path.GetFileNameWithoutExtension(currentPath);
            var latestBasePath = Path.GetFileNameWithoutExtension(latestRemotePath);

            Debug.WriteLine($"currentBathPath: {currentBathPath}");
            Debug.WriteLine($"latestBasePath: {latestBasePath}");

            var answer = currentBathPath == latestBasePath;
            Debug.WriteLine($"answer: {answer}");

            return answer;
        }

        public static async Task<T> GetDefinition<T>(string command, uint hash)
        {
            await Ready;

            var selectCommand = new SqliteCommand(command, db);
            selectCommand.Parameters.AddWithValue("@Hash", HashToDbHash(hash));

            var query = await selectCommand.ExecuteReaderAsync();

            if (!query.HasRows) return default;

            query.Read();
            var json = query.GetString(0);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<DestinyDefinitionsDestinyInventoryItemDefinition> GetItemDefinition(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyInventoryItemDefinition>(
                "SELECT json FROM DestinyInventoryItemDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyObjectiveDefinition> GetObjectiveDefinition(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyObjectiveDefinition>(
                "SELECT json FROM DestinyObjectiveDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyClassDefinition> GetClassDefinition(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyClassDefinition>(
                "SELECT json FROM DestinyClassDefinition WHERE id = @Hash;", hash);
        }
    }
}