using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Data.Sqlite;

namespace SkyWingViewer.Services;

//AI: このクラスの処理はほぼ AI 生成。仕様のみ指示し、細かい調整のみ手動で実施
public class ClipStudioThumbnailLoader : IThumbnailProvider
{
    public HashSet<string> SupportedExtensions { get; } = new()
    {
        ".clip"
    };

    /// <summary>
    /// .clipファイル内の全テーブルをスキャンし、最初に見つかった画像バイナリをFreezeして返します。
    /// </summary>
    public  BitmapImage GetBitmapImage(string filePath)
    {
        int decodeWidth = 0;
        if (!File.Exists(filePath)) return null;

        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            // 1. SQLiteシグネチャの切り出し
            byte[] allBytes = File.ReadAllBytes(filePath);
            byte[] signature = System.Text.Encoding.ASCII.GetBytes("SQLite format 3");
            int offset = -1;

            for (int i = 0; i < allBytes.Length - signature.Length; i++)
            {
                if (allBytes[i] == 0x53 && allBytes[i + 1] == 0x51) // 'S', 'Q' 
                {
                    bool match = true;
                    for (int j = 0; j < signature.Length; j++)
                    {
                        if (allBytes[i + j] != signature[j]) { match = false; break; }
                    }
                    if (match) { offset = i; break; }
                }
            }

            if (offset == -1) return null;

            using (var fs = new FileStream(tempFile, FileMode.Create))
            {
                fs.Write(allBytes, offset, allBytes.Length - offset);
            }

            // 2. データベーススキャン
            using (var connection = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly;"))
            {
                connection.Open();

                var tables = new List<string>();
                using (var cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table'", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) tables.Add(reader.GetString(0));
                }

                // サムネイルがありそうなテーブルを優先
                tables.Sort((a, b) => GetTablePriority(b) - GetTablePriority(a));

                foreach (var table in tables)
                {
                    var columns = new List<string>();
                    using (var cmd = new SqliteCommand($"PRAGMA table_info(\"{table}\")", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) columns.Add(reader.GetString(1));
                    }

                    foreach (var col in columns)
                    {
                        try
                        {
                            using var cmd = new SqliteCommand($"SELECT \"{col}\" FROM \"{table}\" WHERE \"{col}\" IS NOT NULL LIMIT 1", connection);
                            var result = cmd.ExecuteScalar();

                            if (result is byte[] blob)
                            {
                                // 内部で Freeze() された BitmapImage が返る
                                var bitmap = CreateBitmapImageFromBytes(blob, decodeWidth);
                                if (bitmap != null) return bitmap;
                            }
                        }
                        catch { continue; }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClipStudio Loader Error: {ex.Message}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }

        return null;
    }

    /// <summary>
    /// 画像バイナリを検出し、Freeze済みのBitmapImageを生成します。
    /// </summary>
    private static BitmapImage? CreateBitmapImageFromBytes(byte[] bytes, int decodeWidth)
    {
        if (bytes == null || bytes.Length < 8) return null;

        try
        {
            int imgOffset = -1;
            // ヘッダー（PNG/JPEG）の位置を特定
            for (int i = 0; i < Math.Min(bytes.Length, 256); i++)
            {
                if (bytes[i] == 0x89 && i + 3 < bytes.Length && bytes[i + 1] == 0x50 && bytes[i + 2] == 0x4E && bytes[i + 3] == 0x47)
                { imgOffset = i; break; }
                if (bytes[i] == 0xFF && i + 1 < bytes.Length && bytes[i + 1] == 0xD8)
                { imgOffset = i; break; }
            }

            if (imgOffset == -1) return null;

            using (var stream = new MemoryStream(bytes, imgOffset, bytes.Length - imgOffset))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                if (decodeWidth > 0)
                {
                    bitmap.DecodePixelWidth = decodeWidth;
                }

                bitmap.EndInit();

                // 【重要】スレッド間転送を可能にするために Freeze
                if (bitmap.CanFreeze)
                {
                    bitmap.Freeze();
                }

                return bitmap;
            }
        }
        catch
        {
            return null;
        }
    }

    private static int GetTablePriority(string tableName)
    {
        return tableName switch
        {
            "ExternalThumbnail" => 100,
            "CanvasPreview" => 90,
            "Canvas" => 80,
            _ => 0
        };
    }
}