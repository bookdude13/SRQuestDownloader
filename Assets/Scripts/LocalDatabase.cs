// using System;
// using System.IO;
// using System.Linq;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Newtonsoft.Json;
// using SRTimestampLib;
// using SRTimestampLib.Models;
// using UnityEngine;
//
// [Serializable]
// // TODO add locking/singleton, and/or tests
// public class LocalDatabase {
//     [JsonIgnore]
//     private readonly string LOCAL_DATABASE_NAME = "SRQD_local.db";
//     [JsonIgnore]
//     private SRLogHandler logger;
//
//     [JsonProperty]
//     private List<MapZMetadata> localMapMetadata = new();
//
//     /// Faster lookup of maps by path
//     [JsonIgnore]
//     private Dictionary<string, MapZMetadata> localMapPathLookup = new();
//
//     /// Faster lookup of maps by hash
//     [JsonIgnore]
//     private Dictionary<string, MapZMetadata> localMapHashLookup = new();
//
//
//     public LocalDatabase(SRLogHandler logger) {
//         this.logger = logger;
//     }
//
//     /// Gets locally stored metadata based on file path.
//     /// Returns null if not found
//     public MapZMetadata GetFromPath(string filePath) {
//         if (localMapPathLookup.ContainsKey(filePath)) {
//             return localMapPathLookup[filePath];
//         }
//
//         return null;
//     }
//
//     /// Gets locally stored metadata based on map hash.
//     /// Returns null if not found
//     public MapZMetadata GetFromHash(string hash) {
//         if (localMapHashLookup.ContainsKey(hash)) {
//             return localMapHashLookup[hash];
//         }
//
//         return null;
//     }
//
//     public int GetNumberOfMaps() {
//         return localMapMetadata.Count;
//     }
//
//     /// Adds map metadata to database.
//     /// If the file path is already present or hash is already present replace
//     public void AddMap(MapZMetadata mapMeta, SRLogHandler logger) {
//         // Remove existing to replace with new
//         if (localMapPathLookup.ContainsKey(mapMeta.FilePath)) {
//             logger.DebugLog($"Removing map with existing path {mapMeta.FilePath}");
//             localMapMetadata.Remove(localMapPathLookup[mapMeta.FilePath]);
//             localMapPathLookup.Remove(mapMeta.FilePath);
//         }
//
//         if (localMapHashLookup.ContainsKey(mapMeta.hash)) {
//             logger.DebugLog($"Removing map with matching hash {mapMeta.hash}");
//             localMapMetadata.Remove(localMapHashLookup[mapMeta.hash]);
//             localMapHashLookup.Remove(mapMeta.hash);
//         }
//
//         logger.DebugLog($"Adding map {Path.GetFileNameWithoutExtension(mapMeta.FilePath)}");
//         localMapPathLookup.Add(mapMeta.FilePath, mapMeta);
//         localMapHashLookup.Add(mapMeta.hash, mapMeta);
//         localMapMetadata.Add(mapMeta);
//     }
//
//     /// Remove maps that aren't in the list of hashes
//     public void RemoveMissingHashes(HashSet<string> savedHashes) {
//         var toRemove = new List<MapZMetadata>();
//         foreach (var mapMeta in localMapMetadata) {
//             if (!savedHashes.Contains(mapMeta.hash)) {
//                 // Not saved; remove from db
//                 toRemove.Add(mapMeta);
//             }
//         }
//
//         foreach (var mapMeta in toRemove) {
//             logger.DebugLog($"db map not found in filesystem; removing {Path.GetFileName(mapMeta.FilePath)}");
//             localMapMetadata.Remove(mapMeta);
//             localMapPathLookup.Remove(mapMeta.FilePath);
//             localMapHashLookup.Remove(mapMeta.hash);
//         }
//     }
//
//     /// Loads db state from file.
//     /// Note: Not done implicitly upon creation!
//     public async Task Load() {
//         if (!File.Exists(GetDbPath())) {
//             logger.DebugLog("DB doesn't exist; creating...");
//             await Save();
//         };
//
//         logger.DebugLog("Loading database...");
//         var localDb = await FileUtils.ReadFileJson<LocalDatabase>(GetDbPath(), logger);
//         if (localDb == null) {
//             logger.ErrorLog("Failed to load local database!");
//             return;
//         }
//
//         this.localMapMetadata = localDb.localMapMetadata;
//         this.localMapPathLookup.Clear();
//         this.localMapHashLookup.Clear();
//         foreach (var mapMeta in localMapMetadata) {
//             localMapPathLookup.Add(mapMeta.FilePath, mapMeta);
//             localMapHashLookup.Add(mapMeta.hash, mapMeta);
//         }
//         logger.DebugLog("DB loaded");
//     }
//
//     /// Saves db state to file
//     /// Returns true if successful, false if not
//     public async Task<bool> Save() {
//         try {
//             string asJson = JsonConvert.SerializeObject(this, Formatting.Indented);
//             string tempFile = Path.Join(Application.temporaryCachePath, Guid.NewGuid().ToString());
//             logger.DebugLog($"Saving db ({localMapMetadata.Count} maps)");
//             if (!await FileUtils.WriteToFile(asJson, tempFile, logger)) {
//                 logger.ErrorLog("Failed to write db to temp file");
//                 return false;
//             }
//
//             return FileUtils.MoveFileOverwrite(tempFile, GetDbPath(), logger);
//         }
//         catch (System.Exception e) {
//             logger.ErrorLog("Failed to save db: " + e.Message);
//             return false;
//         }
//     }
//
//     private string GetDbPath() {
//         return Path.Join(Application.persistentDataPath, LOCAL_DATABASE_NAME);
//     }
// }