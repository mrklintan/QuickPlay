using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickPlay.Core;

public readonly record struct ReleaseVersion(
    int Major,
    int Minor,
    int Build,
    int Revision,
    string? PreRelease = null) : IComparable<ReleaseVersion>
{
    public static ReleaseVersion Parse(string value) =>
        TryParse(value, out var version)
            ? version
            : throw new FormatException($"'{value}' is not a valid release version.");

    public static bool TryParse(string? value, out ReleaseVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V')) normalized = normalized[1..];
        normalized = normalized.Split('+', 2)[0];
        var versionAndPreRelease = normalized.Split('-', 2);
        var parts = versionAndPreRelease[0].Split('.');
        if (parts.Length is < 1 or > 4) return false;
        var numbers = new int[4];
        for (var index = 0; index < parts.Length; index++)
            if (!int.TryParse(parts[index], out numbers[index]) || numbers[index] < 0) return false;
        var preRelease = versionAndPreRelease.Length == 2 ? versionAndPreRelease[1] : null;
        if (preRelease is not null && string.IsNullOrWhiteSpace(preRelease)) return false;
        version = new ReleaseVersion(numbers[0], numbers[1], numbers[2], numbers[3], preRelease);
        return true;
    }

    public int CompareTo(ReleaseVersion other)
    {
        foreach (var comparison in new[]
        {
            Major.CompareTo(other.Major), Minor.CompareTo(other.Minor),
            Build.CompareTo(other.Build), Revision.CompareTo(other.Revision)
        })
            if (comparison != 0) return comparison;
        if (PreRelease is null || other.PreRelease is null)
            return PreRelease is null ? other.PreRelease is null ? 0 : 1 : -1;
        return ComparePreRelease(PreRelease, other.PreRelease);
    }

    public override string ToString() =>
        $"{Major}.{Minor}.{Build}.{Revision}{(PreRelease is null ? string.Empty : $"-{PreRelease}")}";

    private static int ComparePreRelease(string left, string right)
    {
        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        for (var index = 0; index < Math.Min(leftParts.Length, rightParts.Length); index++)
        {
            var leftNumeric = int.TryParse(leftParts[index], out var leftNumber);
            var rightNumeric = int.TryParse(rightParts[index], out var rightNumber);
            var comparison = leftNumeric && rightNumeric
                ? leftNumber.CompareTo(rightNumber)
                : leftNumeric != rightNumeric
                    ? leftNumeric ? -1 : 1
                    : StringComparer.OrdinalIgnoreCase.Compare(leftParts[index], rightParts[index]);
            if (comparison != 0) return comparison;
        }
        return leftParts.Length.CompareTo(rightParts.Length);
    }
}

public sealed record UpdateAsset(string Name, Uri DownloadUrl);

public sealed record UpdateCheckResult(
    ReleaseVersion InstalledVersion,
    ReleaseVersion AvailableVersion,
    UpdateAsset? MsiAsset)
{
    public bool IsUpdateAvailable => AvailableVersion.CompareTo(InstalledVersion) > 0;
}

public sealed class GitHubUpdateService(HttpClient httpClient)
{
    public const string LatestReleaseUrl = "https://api.github.com/repos/mrklintan/QuickPlay/releases/latest";

    public async Task<UpdateCheckResult> CheckAsync(
        ReleaseVersion installedVersion,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("QuickPlay", installedVersion.ToString()));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("GitHub returned an empty release response.");
        if (!ReleaseVersion.TryParse(release.TagName, out var availableVersion))
            throw new InvalidDataException("GitHub returned an invalid release version.");
        var assets = (release.Assets ?? [])
            .Where(asset => !string.IsNullOrWhiteSpace(asset.Name) &&
                            Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out _))
            .Select(asset => new UpdateAsset(asset.Name!, new Uri(asset.DownloadUrl!)))
            .ToArray();
        return new UpdateCheckResult(installedVersion, availableVersion, SelectX64Msi(assets));
    }

    public async Task<string> DownloadMsiAsync(
        UpdateAsset asset,
        string? temporaryDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (!asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
            !asset.DownloadUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !asset.DownloadUrl.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The update asset is not a valid GitHub MSI download.");
        var directory = Path.Combine(temporaryDirectory ?? Path.GetTempPath(), "QuickPlay", "Updates");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, Path.GetFileName(asset.Name));
        try
        {
            using var response = await httpClient.GetAsync(
                asset.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(target, cancellationToken);
            return destination;
        }
        catch
        {
            try { File.Delete(destination); } catch (IOException) { }
            throw;
        }
    }

    public static UpdateAsset? SelectX64Msi(IEnumerable<UpdateAsset> assets) => assets
        .Where(asset => asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) &&
                        asset.Name.Contains("x64", StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(asset => asset.Name.StartsWith("QuickPlay-", StringComparison.OrdinalIgnoreCase))
        .ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault();

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; init; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? DownloadUrl { get; init; }
    }
}

public static class MsiInstallerLauncher
{
    public static ProcessStartInfo CreateStartInfo(string msiPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(msiPath);
        var startInfo = new ProcessStartInfo("msiexec.exe") { UseShellExecute = true };
        startInfo.ArgumentList.Add("/i");
        startInfo.ArgumentList.Add(Path.GetFullPath(msiPath));
        return startInfo;
    }

    public static Process Start(string msiPath) =>
        Process.Start(CreateStartInfo(msiPath)) ?? throw new InvalidOperationException("The MSI installer could not be started.");
}

public static class FolderLoadingPolicy
{
    public const int ProgressThreshold = 100;

    public static bool ShouldShowProgress(int supportedTrackCount) => supportedTrackCount > ProgressThreshold;
}
