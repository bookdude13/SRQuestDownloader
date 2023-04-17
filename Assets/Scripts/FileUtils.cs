using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public static class FileUtils {
    /// Attempts to move a file, overwriting if dstPath already exists.
    /// Returns true if it succeeded, false if it failed.
    public static bool MoveFileOverwrite(string srcPath, string destPath, SRLogHandler logger) {
        try {
            File.Copy(srcPath, destPath, true);
            File.Delete(srcPath);
            return true;
        } catch (System.Exception e) {
            logger.ErrorLog($"Failed to move {srcPath} to {destPath}! {e.Message}");
        }

        return false;
    }

    public static async Task<bool> WriteToFile(byte[] bytes, string filePath, SRLogHandler logger) {
        try {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length);
                return true;
            }
        }
        catch (System.Exception e) {
            logger.ErrorLog($"Failed to write to {filePath}: {e.Message}");
            return false;
        }
    }

    public static async Task<bool> WriteToFile(string contents, string filePath, SRLogHandler logger) {
        return await WriteToFile(Encoding.UTF8.GetBytes(contents), filePath, logger);
    }

    public static async Task<bool> AppendToFile(byte[] bytes, string filePath, SRLogHandler logger) {
        try {
            using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length);
                return true;
            }
        }
        catch (System.Exception e)
        {
            logger.ErrorLog("Failed to append to file: " + e.Message);
            return false;
        }
    }

    public static async Task<bool> AppendToFile(string contents, string filePath, SRLogHandler logger) {
        return await AppendToFile(Encoding.UTF8.GetBytes(contents), filePath, logger);
    }

    /// Reads file contents and parses into given type. Assumes json input.
    /// Returns null on failure.
    public static async Task<T> ReadFileJson<T>(string filePath, SRLogHandler logger) {
        try {
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BufferedStream bufferedStream = new BufferedStream(stream))
            using (System.IO.StreamReader sr = new System.IO.StreamReader(bufferedStream))
            {
                T metadata = JsonConvert.DeserializeObject<T>(await sr.ReadToEndAsync());
                return metadata;
            }
        }
        catch (System.Exception e) {
            logger.ErrorLog($"Failed to parse local map {filePath}: {e.Message}");
        }

        return default(T);
    }


    /// Sets file times to the given dateModified time, assuming UTC time.
    /// Return true if updated, false if error
    public static bool SetDateModifiedUtc(string filePath, DateTime dateModifiedUtc, SRLogHandler logger) {
        try {
            // Might as well set all of them
            File.SetLastWriteTimeUtc(filePath, dateModifiedUtc);
            File.SetLastAccessTimeUtc(filePath, dateModifiedUtc);
            File.SetCreationTimeUtc(filePath, dateModifiedUtc);

            return true;
        } catch (Exception e) {
            logger.ErrorLog($"Failed to set file dates for {Path.GetFileName(filePath)}: {e.Message}");
            return false;
        }
    }
}