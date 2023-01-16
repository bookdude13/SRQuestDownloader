using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
// TODO add locking/singleton, and/or tests
public class LocalDatabase {
    [JsonIgnore]
    private readonly string LOCAL_DATABASE_NAME = "SRQD_local.db";
    [JsonIgnore]
    private DisplayManager displayManager;

    [JsonProperty]
    private List<MapZMetadata> localMapMetadata = new List<MapZMetadata>();

    [JsonIgnore]
    /// Faster lookup of maps by path
    private Dictionary<string, MapZMetadata> localMapPathLookup = new Dictionary<string, MapZMetadata>();


    public LocalDatabase(DisplayManager displayManager) {
        this.displayManager = displayManager;
    }

    /// Gets locally stored metadata based on file path.
    /// Returns null if not found
    public MapZMetadata GetFromPath(string filePath) {
        if (localMapPathLookup.ContainsKey(filePath)) {
            return localMapPathLookup[filePath];
        }

        return null;
    }

    /// Adds map metadata to database.
    /// If the file path is already present, replace
    public void AddMap(MapZMetadata mapMeta) {
        // Remove existing to replace with new
        if (localMapPathLookup.ContainsKey(mapMeta.FilePath)) {
            localMapMetadata.Remove(localMapPathLookup[mapMeta.FilePath]);
            localMapPathLookup.Remove(mapMeta.FilePath);
        }

        localMapPathLookup.Add(mapMeta.FilePath, mapMeta);
        localMapMetadata.Add(mapMeta);
    }

    /// Loads db state from file.
    /// Note: Not done implicitly upon creation!
    public async Task Load() {
        if (!File.Exists(GetDbPath())) {
            displayManager.DebugLog("DB doesn't exist; creating...");
            await Save();
        };

        displayManager.DebugLog("Loading database...");
        LocalDatabase localDb = await FileUtils.ReadFileJson<LocalDatabase>(GetDbPath(), displayManager);
        if (localDb == null) {
            displayManager.ErrorLog("Failed to load local database!");
            return;
        }

        this.localMapMetadata = localDb.localMapMetadata;
        this.localMapPathLookup.Clear();
        foreach (var mapMeta in localMapMetadata) {
            localMapPathLookup.Add(mapMeta.FilePath, mapMeta);
        }
        displayManager.DebugLog("DB loaded");
    }

    /// Saves db state to file
    /// Returns true if successful, false if not
    public async Task<bool> Save() {
        try {
            string asJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            string tempFile = Path.Join(Application.temporaryCachePath, Guid.NewGuid().ToString());
            displayManager.DebugLog($"Saving db ({localMapMetadata.Count} maps)");
            if (!await FileUtils.WriteToFile(asJson, tempFile, displayManager)) {
                displayManager.ErrorLog("Failed to write db to temp file");
                return false;
            }

            return FileUtils.MoveFileOverwrite(tempFile, GetDbPath(), displayManager);
        }
        catch (System.Exception e) {
            displayManager.ErrorLog("Failed to save db: " + e.Message);
            return false;
        }
    }

    private string GetDbPath() {
        return Path.Join(Application.persistentDataPath, LOCAL_DATABASE_NAME);
    }
}