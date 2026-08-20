using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using UPA.Core;

namespace UPA.Analysis;

public sealed record CacheIndexEntry(
    string RelativePath,
    long FileLength,
    long LastWriteTimeUtcTicks,
    ulong ContentHash,
    long Offset,
    int JsonLength);

public sealed record UpdatedModelRecord(
    string RelativePath,
    long FileLength,
    long LastWriteTimeUtcTicks,
    ulong ContentHash,
    CSharpScriptModel Model);

public sealed class ProjectScannerCache : IDisposable
{
    private readonly string _cacheFilePath;
    private readonly ulong _projectIdentityHash;
    private readonly Dictionary<string, CacheIndexEntry> _index = new(StringComparer.Ordinal);
    private FileStream? _readStream;

    public const int Magic = 0x55504143; // 'UPAC'
    public const int FormatVersion = 2; // v2: Trailing index
    public const int CurrentScannerVersion = 4;

    public ProjectScannerCache(string projectRoot)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(projectRoot));
        _projectIdentityHash = BitConverter.ToUInt64(hashBytes, 0);
        _cacheFilePath = Path.Combine(Path.GetTempPath(), $"upa_bin_cache_{_projectIdentityHash:x16}.bin");
    }

    public IReadOnlyDictionary<string, CacheIndexEntry> Index => _index;
    public string CacheFilePath => _cacheFilePath;

    public bool LoadIndex()
    {
        _index.Clear();
        DisposeReadStream();

        if (!File.Exists(_cacheFilePath)) { Console.WriteLine("LoadIndex: File does not exist"); return false; }

        for (int retry = 0; retry < 5; retry++)
        {
            try
            {
                _readStream = new FileStream(_cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                break;
            }
            catch (IOException)
            {
                if (retry == 4) throw;
                System.Threading.Thread.Sleep(50);
            }
        }

        try
        {
            if (_readStream.Length < 32) { Console.WriteLine("LoadIndex: Length < 32"); return false; }

            using var reader = new BinaryReader(_readStream, System.Text.Encoding.UTF8, true);
            
            _readStream.Seek(0, SeekOrigin.Begin);
            if (reader.ReadInt32() != Magic) { Console.WriteLine("LoadIndex: Magic failed"); return false; }
            if (reader.ReadInt32() != FormatVersion) { Console.WriteLine("LoadIndex: FormatVersion failed"); return false; }
            if (reader.ReadInt32() != CurrentScannerVersion) { Console.WriteLine("LoadIndex: CurrentScannerVersion failed"); return false; }
            var readHash = reader.ReadUInt64();
            if (readHash != _projectIdentityHash) { Console.WriteLine($"LoadIndex: Hash failed. Expected {_projectIdentityHash}, got {readHash}"); return false; }

            _readStream.Seek(-12, SeekOrigin.End);
            long indexOffset = reader.ReadInt64();
            if (reader.ReadInt32() != Magic) { Console.WriteLine("LoadIndex: Footer Magic failed"); return false; }

            _readStream.Seek(indexOffset, SeekOrigin.Begin);
            int entryCount = reader.ReadInt32();
            
            for (int i = 0; i < entryCount; i++)
            {
                var path = reader.ReadString();
                var len = reader.ReadInt64();
                var ticks = reader.ReadInt64();
                var hash = reader.ReadUInt64();
                var offset = reader.ReadInt64();
                var jsonLen = reader.ReadInt32();

                _index[path] = new CacheIndexEntry(path, len, ticks, hash, offset, jsonLen);
            }

            Console.WriteLine("LoadIndex: Success!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadIndex: Exception: {ex.Message}");
            _index.Clear();
            DisposeReadStream();
            return false;
        }
    }

    public CSharpScriptModel? LoadModel(CacheIndexEntry entry)
    {
        if (_readStream == null) return null;
        try
        {
            _readStream.Seek(entry.Offset, SeekOrigin.Begin);
            var bytes = new byte[entry.JsonLength];
            _readStream.ReadExactly(bytes);
            return JsonSerializer.Deserialize<CSharpScriptModel>(bytes);
        }
        catch { return null; }
    }

    public void CommitDelta(IReadOnlyList<CacheIndexEntry> unchanged, IReadOnlyList<UpdatedModelRecord> updated)
    {
        string tmpPath = _cacheFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using var outFs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);
            using var writer = new BinaryWriter(outFs, System.Text.Encoding.UTF8, true);

            // Write Header
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(CurrentScannerVersion);
            writer.Write(_projectIdentityHash);

            var newIndex = new List<CacheIndexEntry>(unchanged.Count + updated.Count);

            // Copy Unchanged
            if (_readStream != null)
            {
                var buffer = new byte[81920]; // 80KB buffer
                foreach (var un in unchanged)
                {
                    var offset = outFs.Position;
                    _readStream.Seek(un.Offset, SeekOrigin.Begin);
                    
                    int toRead = un.JsonLength;
                    while (toRead > 0)
                    {
                        int read = _readStream.Read(buffer, 0, Math.Min(buffer.Length, toRead));
                        if (read == 0) break;
                        outFs.Write(buffer, 0, read);
                        toRead -= read;
                    }
                    
                    newIndex.Add(un with { Offset = offset });
                }
            }

            // Write Updated
            foreach (var up in updated)
            {
                var offset = outFs.Position;
                var bytes = JsonSerializer.SerializeToUtf8Bytes(up.Model);
                outFs.Write(bytes);
                newIndex.Add(new CacheIndexEntry(up.RelativePath, up.FileLength, up.LastWriteTimeUtcTicks, up.ContentHash, offset, bytes.Length));
            }

            // Write Index
            long indexOffset = outFs.Position;
            writer.Write(newIndex.Count);
            foreach (var e in newIndex)
            {
                writer.Write(e.RelativePath);
                writer.Write(e.FileLength);
                writer.Write(e.LastWriteTimeUtcTicks);
                writer.Write(e.ContentHash);
                writer.Write(e.Offset);
                writer.Write(e.JsonLength);
            }

            // Write Footer
            writer.Write(indexOffset);
            writer.Write(Magic);

            outFs.Flush();
        }
        catch
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            throw;
        }

        // Swap
        DisposeReadStream();
        
        for (int retry = 0; retry < 5; retry++)
        {
            try
            {
                File.Move(tmpPath, _cacheFilePath, overwrite: true);
                break;
            }
            catch (IOException)
            {
                if (retry == 4) throw;
                System.Threading.Thread.Sleep(50);
            }
        }
        
        // Reload index
        LoadIndex();
    }

    public void Clear()
    {
        _index.Clear();
        DisposeReadStream();
        if (File.Exists(_cacheFilePath)) File.Delete(_cacheFilePath);
    }

    public void Dispose()
    {
        DisposeReadStream();
    }

    private void DisposeReadStream()
    {
        if (_readStream != null)
        {
            _readStream.Dispose();
            _readStream = null;
        }
    }

    public static ulong ComputeHash(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return BitConverter.ToUInt64(hashBytes, 0);
    }
}
