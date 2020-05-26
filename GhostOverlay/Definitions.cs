using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
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
        private static bool IsDownloading;
        private static Task<string> CurrentDownloadingTask;
        public static Task<string> Ready;
        private static string DefaultLanguage = "en";
        private static int AttemptsToOpen = 0;

        public static int HashToDbHash(uint hash)
        {
            return unchecked((int) hash);
        }

        private static readonly LogFn Log = Logger.MakeLogger("Definitions");

        public static async void InitializeDatabase()
        {
            Log("InitializeDatabase");

            Ready = ActuallyInitializeDatabase();
            Log("Called ActuallyInitializeDatabase");

            await Ready;
            Log("Awaited Ready - should be good now");

            _ = CheckForLatestDefinitions();
            Log("Called CheckForLatestDefinitions");
        }

        public static async Task<string> ActuallyInitializeDatabase()
        {
            var definitionsPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
            Log($"definitionsPath from settings is {definitionsPath}");
            var definitionsExist = !definitionsPath.Equals(defaultDefinitionsPath) && File.Exists(definitionsPath);

            if (!definitionsExist)
            {
                Log("Definitions don't exist, need to download!");
                definitionsPath = await DownloadDefinitionsDatabase();
            }

            Log($"definitiosPath is {definitionsPath}");

            if (db != null && db.State.HasFlag(ConnectionState.Open))
            {
                Log("db is already open!!!");
                db.Close();
                db = null;
            }

            db = new SqliteConnection($"Filename={definitionsPath}");
            await db.OpenAsync();
            Log("Opened database, going to test it");

            AttemptsToOpen += 1;
            var databaseWorks = await TestDatabase();

            if (!databaseWorks)
            {
                if (AttemptsToOpen < 2)
                {
                    Log("FAILED to open definitions database. Going to clear them all and redownload");
                    ClearAllDefinitions();
                    AppState.SaveSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
                    AppState.ClearSetting(SettingsKey.DefinitionsPath);

                    Log("Okay, trying to open/download definitions again");
                    return await ActuallyInitializeDatabase();
                }

                Log("Failed to open definitions, giving up.");
                throw new Exception("Unable to open definitions - it seems corrupt or something?");
            }

            AppState.Data.DefinitionsPath = definitionsPath;

            Log("Cleaning up old definitions");
            CleanUpDownloadedDefinitions(definitionsPath);

            return definitionsPath;
        }

        public static async Task CheckForLatestDefinitions()
        {
            if (await IsDefinitionsLatest())
            {
                Log("CheckForLatestDefinitions, definitions are latest");
                return;
            }

            Log("CheckForLatestDefinitions, definitions are NOT latest, so DOWNLOADING");
            await DownloadDefinitionsDatabase();

            await ActuallyInitializeDatabase();
        }

        public static async Task<string> FetchLatestDefinitionsPath()
        {
            var language = AppState.ReadSetting(SettingsKey.Language, DefaultLanguage);
            Log($"language: {language}");
            var manifest = await AppState.bungieApi.GetManifest();
            Log("*** got manifest back");
            var remotePath = manifest.MobileWorldContentPaths[language];
            Log($"*** remotePath {remotePath}");

            return $"https://www.bungie.net{remotePath}";
        }

        public static async Task<string> DownloadDefinitionsDatabase()
        {
            if (IsDownloading)
            {
                Log("Already downloading, returning previous Task");
                return await CurrentDownloadingTask;
            }

            Log("Downloading new definitions, new task");
            IsDownloading = true;
            CurrentDownloadingTask = DownloadDefinitionsDatabaseWork();
            await CurrentDownloadingTask;
            Log("Done downloading");
            IsDownloading = false;

            Log("Done downloading - returning await again");
            return await CurrentDownloadingTask;
        }

        public static async Task<string> DownloadDefinitionsDatabaseWork()
        {
            var appData = ApplicationData.Current;
            var urlString = await FetchLatestDefinitionsPath();

            Log($"urlString: {urlString}");

            // TODO: check to see if it's already downloaded?

            var source = new Uri(urlString);
            Log($"source {source}");

            var baseName = Path.GetFileNameWithoutExtension(urlString);
            var destFileName = $"{baseName}.zip";
            var destinationFile =
                await appData.LocalCacheFolder.CreateFileAsync(destFileName, CreationCollisionOption.ReplaceExisting);

            Log($"Downloading {source} to {destinationFile.Path}");

            var downloader = new BackgroundDownloader();
            var download = downloader.CreateDownload(source, destinationFile);

            await download.StartAsync();

            Log("maybe the download finished?");

            Log("Unzipping");
            await Task.Run(() => ZipFile.ExtractToDirectory(destinationFile.Path, appData.LocalCacheFolder.Path, Encoding.UTF8, true));

            var definitionsDbFile = Path.Combine(appData.LocalCacheFolder.Path, $"{baseName}.content");
            Log($"maybe finished unzipping? {definitionsDbFile}");

            if (!File.Exists(definitionsDbFile)) Log("Handle the definitions not existing?");

            AppState.SaveSetting(SettingsKey.DefinitionsPath, definitionsDbFile);

            Log($"All finished, definitions at {definitionsDbFile}");

            return definitionsDbFile;
        }

        private static async Task<bool> IsDefinitionsLatest()
        {
            Log("IsDefinitionsLatest");

            var currentPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
            Log($"currentPath: {currentPath}");

            if (currentPath.Equals("@@NotDownloaded"))
            {
                Log($"returning early");
                return false;
            }

            Log("*** About to FetchLatestDefinitionsPath");
            var latestRemotePath = await FetchLatestDefinitionsPath();
            Log("*** Finished  FetchLatestDefinitionsPath");
            Log($"latestRemotePath: {latestRemotePath}");

            var currentBathPath = Path.GetFileNameWithoutExtension(currentPath);
            var latestBasePath = Path.GetFileNameWithoutExtension(latestRemotePath);

            Log($"currentBathPath: {currentBathPath}");
            Log($"latestBasePath: {latestBasePath}");

            var isLatest = currentBathPath == latestBasePath;
            Log($"isLatest: {isLatest}");

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
                    Log($"Deleting {storageFile.Path}");
                    await storageFile.DeleteAsync();
                }
                catch (Exception e)
                {
                    Log("Unable to clean up definition:");
                    Debug.WriteLine(e);
                }
                
            }
        }

        public static async void ClearAllDefinitions()
        {
            db?.Close();
            db = default;

            var folder = ApplicationData.Current.LocalCacheFolder;

            var filesToTrash = await folder.GetFilesAsync();

            foreach (var storageFile in filesToTrash)
            {
                try
                {
                    Log($"Deleting {storageFile.Path}");
                    await storageFile.DeleteAsync();
                }
                catch (Exception e)
                {
                    Log("Unable to clean up definition:");
                    Debug.WriteLine(e);
                }

            }
        }

        public static async Task<bool> TestDatabase()
        {
            try
            {
                await GetDefinition<DestinyDefinitionsDestinyClassDefinition>("SELECT json FROM DestinyClassDefinition WHERE id = @Hash;", 2271682572, true); // warlock
                return true;
            }
            catch (Exception e)
            {
                Log("Failed to get test value DestinyClassDefinition hash 2271682572 (Warlock)");
                Debug.WriteLine(e);
                return false;
            }
        }

        public static async Task<List<T>> GetMultipleDefinitions<T>(string command)
        {
            await Ready;

            var selectCommand = new SqliteCommand(command, db);
            var query = await selectCommand.ExecuteReaderAsync();

            if (!query.HasRows) return default;

            var results = new List<T>();
            while (query.Read())
            {
                var json = query.GetString(0);
                var obj = JsonConvert.DeserializeObject<T>(json);
                results.Add(obj);
            }

            return results;
        }

        public static async Task<T> GetDefinition<T>(string command, long hash, bool skipReady = false)
        {
            if (!skipReady)
            {
                await Ready;
            }

            var selectCommand = new SqliteCommand(command, db);
            var hashAsInt = Convert.ToUInt32(hash); // TODO: Maybe HashToDbHash can just take long instead?
            selectCommand.Parameters.AddWithValue("@Hash", HashToDbHash(hashAsInt));

            var query = await selectCommand.ExecuteReaderAsync();

            if (!query.HasRows) return default;

            query.Read();
            var json = query.GetString(0);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<DestinyDefinitionsDestinyInventoryItemDefinition> GetInventoryItem(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyInventoryItemDefinition>(
                "SELECT json FROM DestinyInventoryItemDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyObjectiveDefinition> GetObjective(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyObjectiveDefinition>(
                "SELECT json FROM DestinyObjectiveDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyClassDefinition> GetClass(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyClassDefinition>(
                "SELECT json FROM DestinyClassDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyGenderDefinition> GetGender(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyGenderDefinition>(
                "SELECT json FROM DestinyGenderDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyRaceDefinition> GetRace(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyRaceDefinition>(
                "SELECT json FROM DestinyRaceDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsPresentationDestinyPresentationNodeDefinition>
            GetPresentationNode(long hash)
        {
            return await GetDefinition<DestinyDefinitionsPresentationDestinyPresentationNodeDefinition>(
                "SELECT json FROM DestinyPresentationNodeDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsRecordsDestinyRecordDefinition> GetRecord(long hash)
        {
            return await GetDefinition<DestinyDefinitionsRecordsDestinyRecordDefinition>(
                "SELECT json FROM DestinyRecordDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsTraitsDestinyTraitDefinition> GetTrait(long hash)
        {
            return await GetDefinition<DestinyDefinitionsTraitsDestinyTraitDefinition>(
                "SELECT json FROM DestinyTraitDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition> GetTraitCategory(long hash)
        {
            return await GetDefinition<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition>(
                "SELECT json FROM DestinyTraitCategoryDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<List<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition>> GetAllTraitCategory()
        {
            return await GetMultipleDefinitions<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition> (
                "SELECT json FROM DestinyTraitCategoryDefinition;");
        }
    }
}