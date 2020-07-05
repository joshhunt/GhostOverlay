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
using Windows.System;
using Windows.UI.Shell;
using BungieNetApi.Model;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace GhostOverlay
{   
    public static class Definitions
    {
        private static readonly Uri IconDataRemoteUrl =
            new Uri("https://raw.githubusercontent.com/joshhunt/ghost-site/master/generated-data/icons-by-label.json");

        public static Task<string> Ready;
        public static string FallbackLanguage = "en";
        public static Dictionary<string, List<List<string>>> IconData = new Dictionary<string, List<List<string>>>();
        public static Dictionary<long, long> VendorForBounty = new Dictionary<long, long>();

        private static readonly string defaultDefinitionsPath = "@@NotDownloaded";
        private static SqliteConnection db;
        private static bool isDownloading;
        private static Task<string> currentDownloadingTask;
        private static int attemptsToOpen = 0;

        public static int HashToDbHash(uint hash)
        {
            return unchecked((int) hash);
        }

        private static readonly Logger Log = new Logger("Definitions");

        public static async Task Initialize()
        {
            Log.Info("Initialize");

            Ready = OpenOrDownloadDatabase();
            Log.Info("Called OpenOrDownloadDatabase");

            await Ready;
            Log.Info("Awaited Ready - should be good now");

            _ = CheckForLatestDefinitions();
        }

        public static async Task<string> OpenOrDownloadDatabase()
        {
            Log.Info("OpenOrDownloadDatabase attempt {AttemptsToOpen}", attemptsToOpen);

            var definitionsPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
            Log.Info("definitionsPath settings value: {definitionsPath}", definitionsPath);

            var definitionsExist = !definitionsPath.Equals(defaultDefinitionsPath) && File.Exists(definitionsPath);

            if (!definitionsExist)
            {
                Log.Info("Definitions don't exist, need to download!");
                definitionsPath = await DownloadDefinitionsDatabase();
            }

            Log.Info("Final definitions path: {definitionsPath}", definitionsPath);

            if (db != null && db.State.HasFlag(ConnectionState.Open))
            {
                Log.Info("db is already open, closing it");
                db.Close();
                db = null;
            }

            db = new SqliteConnection($"Filename={definitionsPath}");
            await db.OpenAsync();
            Log.Info("Opened database, going to test it");

            attemptsToOpen += 1;
            var databaseWorks = await TestDatabase();

            if (!databaseWorks)
            {
                if (attemptsToOpen < 2)
                {
                    Log.Info("FAILED to open definitions database. Going to clear them all and redownload");
                    ClearAllDefinitions();
                    AppState.SaveSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
                    AppState.ClearSetting(SettingsKey.DefinitionsPath);

                    Log.Info("Okay, trying to open/download definitions again");
                    return await OpenOrDownloadDatabase();
                }

                Log.Info("Failed to open definitions, giving up.");
                throw new Exception("Unable to open definitions - it seems corrupt or something?");
            }

            await Task.WhenAll(InitializeIconData(), InitializeBountyData());

            AppState.Data.DefinitionsPath = definitionsPath;

            Log.Info("Cleaning up old definitions");
            CleanUpDownloadedDefinitions(definitionsPath);

            return definitionsPath;
        }

        public static async Task CheckForLatestDefinitions()
        {
            if (await IsDefinitionsLatest())
            {
                Log.Info("CheckForLatestDefinitions, definitions are latest");
                return;
            }

            Log.Info("CheckForLatestDefinitions, definitions are NOT latest, so DOWNLOADING");
            await DownloadDefinitionsDatabase();

            await OpenOrDownloadDatabase();
        }

        public static List<string> GetSystemLanguages()
        {
            var systemLanguages = Windows.System.UserProfile.GlobalizationPreferences.Languages;
            Log.Info("windows language {lang}", systemLanguages);

            try
            {
                return systemLanguages.SelectMany(v =>
                {
                    var lang = v.ToLower();

                    Regex re = new Regex(@"^(\w+)-", RegexOptions.IgnoreCase);
                    var result = re.Match(lang);

                    if (!result.Success)
                    {
                        return new List<string> { lang.ToLower() };
                    }

                    var prefix = result.Groups?[1]?.Captures?[0]?.ToString();
                    var spread = new List<string> { lang, prefix };
                    Log.Info("Spread {originalLanguage} to {spreadLanguages}", lang, spread);

                    return spread;
                }).ToList();
            }
            catch (Exception)
            {
                return new List<string> { FallbackLanguage };
            }
        }

        public static async Task<string> FetchLatestDefinitionsPath()
        {
            var manifest = await AppState.bungieApi.GetManifest();

            var language = AppState.Data.Language.Value ?? "@@UNSET";
            Log.Info("Language from AppState is {language}", language);

            if (!manifest.MobileWorldContentPaths.ContainsKey(language))
            {
                Log.Info("Language is not in manifest, so going to find a new one");

                var systemLanguages = GetSystemLanguages();
                Log.Info("systemLanguages: {languages}", systemLanguages);

                var foundLanguage = systemLanguages.FirstOrDefault(v => manifest.MobileWorldContentPaths.ContainsKey(v));
                Log.Info("foundLanguage {language}", foundLanguage);

                language = foundLanguage ?? FallbackLanguage;
                AppState.Data.Language.Value = language;
            }

            Log.Info("Final language {language}", language);

            var remotePath = manifest.MobileWorldContentPaths[language];
            Log.Info("Remote definitions path {remotePath}", remotePath);

            return $"https://www.bungie.net{remotePath}";
        }

        public static async Task<string> DownloadDefinitionsDatabase()
        {
            if (isDownloading)
            {
                Log.Info("Already downloading, returning previous Task");
                return await currentDownloadingTask;
            }

            Log.Info("Downloading new definitions, new task");
            isDownloading = true;
            currentDownloadingTask = DownloadDefinitionsDatabaseWork();
            await currentDownloadingTask;
            Log.Info("Done downloading");
            isDownloading = false;

            Log.Info("Done downloading - returning await again");
            return await currentDownloadingTask;
        }

        public static async Task<string> DownloadDefinitionsDatabaseWork()
        {
            var appData = ApplicationData.Current;
            var urlString = await FetchLatestDefinitionsPath();

            Log.Info("urlString: {urlString}", urlString);

            // TODO: check to see if it's already downloaded?

            var source = new Uri(urlString);
            Log.Info("source {source}", source);

            var baseName = Path.GetFileNameWithoutExtension(urlString);
            var destFileName = $"{baseName}.zip";
            var destinationFile =
                await appData.LocalCacheFolder.CreateFileAsync(destFileName, CreationCollisionOption.ReplaceExisting);

            Log.Info("Downloading {sourceUrl} to {destinationPath}", source, destinationFile.Path);

            var downloader = new BackgroundDownloader();
            var download = downloader.CreateDownload(source, destinationFile);

            await download.StartAsync();

            Log.Info("Download finished, unzipping");
            await Task.Run(() => ZipFile.ExtractToDirectory(destinationFile.Path, appData.LocalCacheFolder.Path, Encoding.UTF8, true));

            var definitionsDbFile = Path.Combine(appData.LocalCacheFolder.Path, $"{baseName}.content");
            Log.Info("Finished unzipping {definitionsDbFile}", definitionsDbFile);

            if (!File.Exists(definitionsDbFile))
            {
                Log.Error("Database does not exist at the expected location");
            }

            AppState.SaveSetting(SettingsKey.DefinitionsPath, definitionsDbFile);

            Log.Info("All finished, definitions at {definitionsDbFile}", definitionsDbFile);

            return definitionsDbFile;
        }

        private static async Task<bool> IsDefinitionsLatest()
        {
            Log.Info("IsDefinitionsLatest");

            var currentPath = AppState.ReadSetting(SettingsKey.DefinitionsPath, defaultDefinitionsPath);
            Log.Info($"currentPath: {currentPath}");

            if (currentPath.Equals("@@NotDownloaded"))
            {
                Log.Info($"returning early");
                return false;
            }

            var latestRemotePath = await FetchLatestDefinitionsPath();
            Log.Info($"latestRemotePath: {latestRemotePath}");

            var currentBathPath = Path.GetFileNameWithoutExtension(currentPath);
            var latestBasePath = Path.GetFileNameWithoutExtension(latestRemotePath);

            Log.Info("currentBathPath {currentBathPath}", currentBathPath);
            Log.Info("latestBasePath {latestBasePath}", latestBasePath);

            var isLatest = currentBathPath == latestBasePath;
            Log.Info($"isLatest: {isLatest}");

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
                    Log.Info($"Deleting {storageFile.Path}");
                    await storageFile.DeleteAsync();
                }
                catch (Exception err)
                {
                    Log.Error("Unable to clean up definition", err);
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
                    Log.Info($"Deleting {storageFile.Path}");
                    await storageFile.DeleteAsync();
                }
                catch (Exception err)
                {
                    Log.Error("Unable to clean up definition", err);
                }

            }
        }

        public static async Task<bool> TestDatabase()
        {
            var querySql = "SELECT json FROM DestinyClassDefinition WHERE id = @Hash;";
            var queryHash = 2271682572; // warlock

            try
            {
                var result = await GetDefinition<DestinyDefinitionsDestinyClassDefinition>(querySql, queryHash, true); 

                if (result == null)
                {
                    throw new Exception("Test query was successful, but it returned null");
                }
                return true;
            }
            catch (Exception err)
            {
                Log.Error($"TestDatabase with query {querySql} and hash {queryHash} failed", err);
                return false;
            }
        }

        private static readonly long BOUNTY_ITEM_CATEGORY_HASH = 1784235469;
        private static async Task InitializeBountyData()
        {
            var allVendors = await GetAllVendorDefinitions();

            foreach (var vendor in allVendors)
            {
                if (vendor.ItemList == null) continue;

                foreach (var itemSale in vendor.ItemList)
                {
                    var itemDef = await GetInventoryItem(itemSale.ItemHash, skipReady:true);
                    if (itemDef.ItemCategoryHashes != null && itemDef.ItemCategoryHashes.Contains(BOUNTY_ITEM_CATEGORY_HASH))
                    {
                        VendorForBounty[itemDef.Hash] = vendor.Hash;
                    }
                }
            }
        }

        private static async Task InitializeIconData()
        {
            Log.Info("Init icon data");

            var appData = ApplicationData.Current;
            var iconDataFilePath = Path.Combine(appData.LocalCacheFolder.Path, "iconData.json");

            if (File.Exists(iconDataFilePath))
            {
                await ReadIconData(iconDataFilePath);
            }

            var newIconDataFile = await appData.LocalCacheFolder.CreateFileAsync("iconData.json", CreationCollisionOption.ReplaceExisting);

            try
            {
                var downloader = new BackgroundDownloader();
                var download = downloader.CreateDownload(IconDataRemoteUrl, newIconDataFile);
                await download.StartAsync();

                await ReadIconData(iconDataFilePath);
            }
            catch (Exception err)
            {
                Log.Error("Error trying to fetch new icon data", err);
            }
        }

        private static async Task ReadIconData(string iconDataFilePath)
        {
            Log.Info("Reading icon data from {iconDataFilePath}", iconDataFilePath);

            // This should run after definitions init, so language is set properly
            var fileString = await File.ReadAllTextAsync(iconDataFilePath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<List<string>>>>(fileString);

            IconData = data;
        }

        public static async Task<List<T>> GetMultipleDefinitions<T>(string command)
        {
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

        public static async Task<DestinyDefinitionsDestinyInventoryItemDefinition> GetInventoryItem(long hash, bool skipReady = false)
        {
            return await GetDefinition<DestinyDefinitionsDestinyInventoryItemDefinition>(
                "SELECT json FROM DestinyInventoryItemDefinition WHERE id = @Hash;", hash, skipReady);
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

        public static async Task<List<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition>> GetAllTraitCategories()
        {
            return await GetMultipleDefinitions<DestinyDefinitionsTraitsDestinyTraitCategoryDefinition> (
                "SELECT json FROM DestinyTraitCategoryDefinition;");
        }

        public static async Task<List<DestinyDefinitionsDestinyVendorDefinition>> GetAllVendorDefinitions()
        {
            return await GetMultipleDefinitions<DestinyDefinitionsDestinyVendorDefinition>(
                "SELECT json FROM DestinyVendorDefinition;");
        }

        public static async Task<DestinyDefinitionsDestinyActivityDefinition> GetActivity(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyActivityDefinition>(
                "SELECT json FROM DestinyActivityDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyActivityModeDefinition> GetActivityMode(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyActivityModeDefinition>(
                "SELECT json FROM DestinyActivityModeDefinition WHERE id = @Hash;", hash);
        }

        public static async Task<DestinyDefinitionsDestinyVendorDefinition> GetVendorDefinition(long hash)
        {
            return await GetDefinition<DestinyDefinitionsDestinyVendorDefinition>(
                "SELECT json FROM DestinyVendorDefinition WHERE id = @Hash;", hash);
        }
    }
}