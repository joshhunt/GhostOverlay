using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static string DefaultLanguage = "en";

        public static int HashToDbHash(uint hash)
        {
            return unchecked((int) hash);
        }

        public static async void InitializeDatabase()
        {
            Ready = ActuallyInitializeDatabase();
            _ = CheckForLatestDefinitions();

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

            if (db != null && db.State.HasFlag(ConnectionState.Open))
            {
                Debug.WriteLine($"db is open!");
                db.Close();
                db = null;
            }

            db = new SqliteConnection($"Filename={definitionsPath}");
            await db.OpenAsync();

            AppState.Data.DefinitionsPath = definitionsPath;

            Debug.WriteLine("Cleaning up old definitions");
            CleanUpDownloadedDefinitions(definitionsPath);

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
            var language = AppState.ReadSetting(SettingsKey.Language, DefaultLanguage);
            Debug.WriteLine($"language: {language}");
            var manifest = await AppState.bungieApi.GetManifest();
            Debug.WriteLine("*** got manifest back");
            var remotePath = manifest.MobileWorldContentPaths[language];
            Debug.WriteLine($"*** remotePath {remotePath}");

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
            Debug.WriteLine("IsDefinitionsLatest");
            var currentPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
            Debug.WriteLine($"currentPath: {currentPath}");

            if (currentPath.Equals("@@NotDownloaded"))
            {
                Debug.WriteLine($"returning early");
                return false;
            }

            Debug.WriteLine("*** About to FetchLatestDefinitionsPath");
            var latestRemotePath = await FetchLatestDefinitionsPath();
            Debug.WriteLine("*** Finished  FetchLatestDefinitionsPath");
            Debug.WriteLine($"latestRemotePath: {latestRemotePath}");

            var currentBathPath = Path.GetFileNameWithoutExtension(currentPath);
            var latestBasePath = Path.GetFileNameWithoutExtension(latestRemotePath);

            Debug.WriteLine($"currentBathPath: {currentBathPath}");
            Debug.WriteLine($"latestBasePath: {latestBasePath}");

            var isLatest = currentBathPath == latestBasePath;
            Debug.WriteLine($"isLatest: {isLatest}");

            return isLatest;
        }

        public static async void CleanUpDownloadedDefinitions(string currentPath)
        {
            var folder = ApplicationData.Current.LocalCacheFolder;
            var currentBasePath = Path.GetFileNameWithoutExtension(currentPath);

            var files = await folder.GetFilesAsync();

            var filesToTrash = files.Where(v =>
            {
                var vBase = Path.GetFileNameWithoutExtension(v.Path);
                var match = Regex.Match(vBase, "^world_sql_content");
                return match.Success && vBase != currentBasePath;
            });

            foreach (var storageFile in filesToTrash)
            {
                try
                {
                    Debug.WriteLine($"Deleting {storageFile.Path}");
                    await storageFile.DeleteAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Unable to clean up definition:");
                    Debug.WriteLine(e);
                }
                
            }
        }

        public static async void ClearAllDefinitions()
        {
            db.Close();
            db = default;

            var folder = ApplicationData.Current.LocalCacheFolder;

            var filesToTrash = await folder.GetFilesAsync();

            foreach (var storageFile in filesToTrash)
            {
                try
                {
                    Debug.WriteLine($"Deleting {storageFile.Path}");
                    await storageFile.DeleteAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Unable to clean up definition:");
                    Debug.WriteLine(e);
                }

            }
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

        public static async Task<DestinyDefinitionsDestinyInventoryItemDefinition> GetInventoryItem(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyInventoryItemDefinition>(
                "SELECT json FROM DestinyInventoryItemDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyObjectiveDefinition> GetObjective(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyObjectiveDefinition>(
                "SELECT json FROM DestinyObjectiveDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyClassDefinition> GetClass(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyClassDefinition>(
                "SELECT json FROM DestinyClassDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsPresentationDestinyPresentationNodeDefinition> GetPresentationNode(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsPresentationDestinyPresentationNodeDefinition>(
                "SELECT json FROM DestinyPresentationNodeDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsRecordsDestinyRecordDefinition> GetRecord(uint hash)
        {
            return await GetDefinition<DestinyDefinitionsRecordsDestinyRecordDefinition>(
                "SELECT json FROM DestinyRecordDefinition WHERE id = @Hash;", hash);
        }
    }
}