using System.Net;
using System.Text;
using QuickPlay.Core;

namespace QuickPlay.Tests;

internal static class ReleaseUpdateTests
{
    public static void Run()
    {
        VersionComparisonIsNumeric();
        MsiSelectionExcludesZip();
        GitHubResponseIsParsed();
        MsiDownloadIsWritten();
        InstallerIsInteractive();
        LargeFolderThresholdIsStrict();
    }

    private static void VersionComparisonIsNumeric()
    {
        TestAssert.True(ReleaseVersion.Parse("v1.3.1.0").CompareTo(ReleaseVersion.Parse("1.3.0.0")) > 0);
        TestAssert.True(ReleaseVersion.Parse("1.10.0").CompareTo(ReleaseVersion.Parse("1.9.9.9")) > 0);
        TestAssert.True(ReleaseVersion.Parse("1.3.1.0").CompareTo(ReleaseVersion.Parse("1.3.1.0-beta.2")) > 0);
        TestAssert.Equal("1.3.1.0", ReleaseVersion.Parse("v1.3.1").ToString());
    }

    private static void MsiSelectionExcludesZip()
    {
        var selected = GitHubUpdateService.SelectX64Msi([
            new UpdateAsset("QuickPlay-1.3.1.0-win-x64.zip", new Uri("https://github.com/a.zip")),
            new UpdateAsset("QuickPlay-1.3.1.0-x64.msi", new Uri("https://github.com/a.msi"))
        ]);
        TestAssert.Equal("QuickPlay-1.3.1.0-x64.msi", selected?.Name);
    }

    private static void GitHubResponseIsParsed()
    {
        const string json = """
            {"tag_name":"v1.3.1.0","assets":[
              {"name":"QuickPlay-1.3.1.0-win-x64.zip","browser_download_url":"https://github.com/mrklintan/QuickPlay/a.zip"},
              {"name":"QuickPlay-1.3.1.0-x64.msi","browser_download_url":"https://github.com/mrklintan/QuickPlay/a.msi"}]}
            """;
        using var client = new HttpClient(new FakeHandler(_ => Json(json)));
        var result = new GitHubUpdateService(client)
            .CheckAsync(ReleaseVersion.Parse("1.3.0.0")).GetAwaiter().GetResult();
        TestAssert.True(result.IsUpdateAvailable);
        TestAssert.Equal("1.3.1.0", result.AvailableVersion.ToString());
        TestAssert.Equal("QuickPlay-1.3.1.0-x64.msi", result.MsiAsset?.Name);
    }

    private static void MsiDownloadIsWritten()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"QuickPlayUpdate-{Guid.NewGuid():N}");
        try
        {
            using var client = new HttpClient(new FakeHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) }));
            var path = new GitHubUpdateService(client).DownloadMsiAsync(
                new UpdateAsset("QuickPlay-1.3.1.0-x64.msi", new Uri("https://github.com/update.msi")),
                temporaryDirectory).GetAwaiter().GetResult();
            TestAssert.True(File.Exists(path));
            TestAssert.Equal(3L, new FileInfo(path).Length);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
        }
    }

    private static void InstallerIsInteractive()
    {
        var info = MsiInstallerLauncher.CreateStartInfo(@"C:\Temp\QuickPlay.msi");
        TestAssert.Equal("msiexec.exe", info.FileName);
        TestAssert.Equal(2, info.ArgumentList.Count);
        TestAssert.Equal("/i", info.ArgumentList[0]);
        TestAssert.True(!info.ArgumentList.Any(argument => argument.Equals("/qn", StringComparison.OrdinalIgnoreCase)));
    }

    private static void LargeFolderThresholdIsStrict()
    {
        TestAssert.True(!FolderLoadingPolicy.ShouldShowProgress(100));
        TestAssert.True(FolderLoadingPolicy.ShouldShowProgress(101));
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}
