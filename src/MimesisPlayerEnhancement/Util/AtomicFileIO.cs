using System;
using System.IO;
using System.Text;

namespace MimesisPlayerEnhancement.Util
{
    internal static class AtomicFileIO
    {
        private const string BackupSuffix = ".bak";
        private const string TempSuffix = ".tmp";

        internal static void WriteBytes(string filePath, byte[] data, string logFeature = "Сохранение данных")
        {
            string tmpPath = filePath + TempSuffix;
            string bakPath = filePath + BackupSuffix;

            File.WriteAllBytes(tmpPath, data);

            if (File.Exists(filePath))
            {
                try { File.Copy(filePath, bakPath, true); }
                catch (Exception ex)
                {
                    ModLog.Warn(logFeature, $"Backup failed for {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tmpPath, filePath);
        }

        internal static void WriteText(string filePath, string text, string logFeature = "Сохранение данных")
        {
            WriteBytes(filePath, Encoding.UTF8.GetBytes(text), logFeature);
        }

        internal static byte[]? ReadBytes(string filePath, string logFeature = "Сохранение данных")
        {
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(filePath);
                    if (data.Length > 0)
                    {
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(logFeature, $"Main file read failed ({Path.GetFileName(filePath)}): {ex.Message}");
                }
            }

            string bakPath = filePath + BackupSuffix;
            if (File.Exists(bakPath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(bakPath);
                    if (data.Length > 0)
                    {
                        ModLog.Warn(logFeature, $"Recovered from backup: {Path.GetFileName(bakPath)}");
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Error(logFeature, $"Backup also failed ({Path.GetFileName(bakPath)}): {ex.Message}");
                }
            }

            return null;
        }

        internal static string? ReadText(string filePath, string logFeature = "Сохранение данных")
        {
            byte[]? data = ReadBytes(filePath, logFeature);
            return data == null ? null : Encoding.UTF8.GetString(data);
        }

        internal static void Delete(string filePath, string logFeature = "Сохранение данных")
        {
            foreach (string path in new[] { filePath, filePath + BackupSuffix, filePath + TempSuffix })
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    File.Delete(path);
                    ModLog.Debug(logFeature, $"Deleted stale file: {Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    ModLog.Warn(logFeature, $"Failed to delete {Path.GetFileName(path)}: {ex.Message}");
                }
            }
        }
    }
}
