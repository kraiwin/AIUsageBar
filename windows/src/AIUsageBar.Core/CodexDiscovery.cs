using System.Security.Cryptography;
using System.Text.Json;

namespace AIUsageBar.Core;

public sealed record ExecutableTuple(string Path, string MetadataVersion, string FileId, long Size, string Sha256);
public static class CodexDiscovery
{
    /// <summary>Reads only explicitly supplied npm package metadata and native candidate bytes; never executes a CLI.</summary>
    public static IReadOnlyList<ExecutableTuple> Discover(string npmRoot)
    {
        if (!System.IO.Path.IsPathFullyQualified(npmRoot)) throw new CoreException("invalid-path");
        var package = System.IO.Path.Combine(npmRoot, "node_modules", "@openai", "codex");
        var metadata = System.IO.Path.Combine(package, "package.json");
        if (!File.Exists(metadata)) return [];
        using var stream = new FileStream(metadata, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 65536) throw new CoreException("metadata-limit");
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes);
        using var doc = UsagePayloadParser.Parse(bytes);
        if (!doc.RootElement.TryGetProperty("name", out var name) || name.GetString() != "@openai/codex" ||
            !doc.RootElement.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(version.GetString())) throw new CoreException("invalid-metadata");
        var candidates = new[] {
            System.IO.Path.Combine(package,"node_modules","@openai","codex-win32-x64","vendor","x86_64-pc-windows-msvc","bin","codex.exe"),
            System.IO.Path.Combine(npmRoot,"node_modules","@openai","codex-win32-x64","vendor","x86_64-pc-windows-msvc","bin","codex.exe"),
            System.IO.Path.Combine(package,"vendor","x86_64-pc-windows-msvc","bin","codex.exe") };
        return candidates.Where(File.Exists).Select(x => Inspect(x, version.GetString()!)).ToArray();
    }
    public static ExecutableTuple Inspect(string absolutePath, string metadataVersion)
    {
        if (!System.IO.Path.IsPathFullyQualified(absolutePath) || absolutePath.StartsWith("\\\\", StringComparison.Ordinal) || metadataVersion.Length is 0 or > 128) throw new CoreException("invalid-path");
        var path = System.IO.Path.GetFullPath(absolutePath);
        // Validate every existing ancestor; format/tuple is not provenance or a compatibility approval.
        for (var parent = System.IO.Path.GetDirectoryName(path); parent is not null; parent = System.IO.Path.GetDirectoryName(parent))
            if ((File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new CoreException("unsafe-executable");
        if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw new CoreException("unsafe-executable");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var info = PrivateFiles.Information(stream.SafeFileHandle);
        if (info.Links != 1 || stream.Length < 64) throw new CoreException("unsafe-executable");
        Span<byte> header = stackalloc byte[64]; stream.ReadExactly(header);
        if (header[0] != 'M' || header[1] != 'Z') throw new CoreException("not-native-executable");
        var offset = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(header[60..]);
        if (offset < 64 || offset > stream.Length - 6) throw new CoreException("not-native-executable");
        stream.Position = offset; Span<byte> signature = stackalloc byte[6]; stream.ReadExactly(signature);
        if (!signature[..4].SequenceEqual("PE\0\0"u8) || signature[4] != 0x64 || signature[5] != 0x86) throw new CoreException("not-x64-executable");
        stream.Position = 0; var digest = Convert.ToHexString(SHA256.HashData(stream));
        return new(path, metadataVersion, $"{info.Volume:X8}:{info.FileIndexHigh:X8}{info.FileIndexLow:X8}", stream.Length, digest);
    }
}
