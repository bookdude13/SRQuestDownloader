using System;
using System.IO;

public static class FileUtils {
    /// Attempts to move a file, overwriting if dstPath already exists.
    /// Returns true if it succeeded, false if it failed.
    public static bool MoveFileOverwrite(string srcPath, string destPath, DisplayManager displayManager) {
        try {
            File.Copy(srcPath, destPath, true);
            File.Delete(srcPath);
            return true;
        } catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to move {srcPath} to {destPath}! {e.Message}");
        }

        return false;
    }

    public static bool WriteToFile(byte[] bytes, string filePath, DisplayManager displayManager) {
        try {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.Write(bytes, 0, bytes.Length);
                return true;
            }
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to write to {filePath}: {e.Message}");
            return false;
        }
    }
}