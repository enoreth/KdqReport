using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Options;

namespace KdqParser;

internal sealed partial class KdqDownloader(IKdqApi kdqApi, IOptions<FileSettings> downloadSettings)
{
    private static readonly Regex ExtractId = MyRegex();
    private static readonly Regex RemoveNamespace = MyRegex1();
    
    internal async Task<int> DownloadAndSaveAsync()
    {
        var kdqListXml = await kdqApi.GetAllKdqItemsAsync();
        var urls = GetKdqBaseListXml(kdqListXml);

        var result = await SaveKdqFilesAsync(urls);
        Console.WriteLine($"{result} files written");

        return urls.Count;
    }

    private async Task<int> SaveKdqFilesAsync(IReadOnlyCollection<string> urls)
    {
        int counter = 0;
        ArgumentNullException.ThrowIfNull(urls);
        ConcurrentBag<string> kdqIds = new (urls);
        await Parallel.ForEachAsync(kdqIds, async (kdqId, _) =>
        {
            try
            {
                var kdqXml = await kdqApi.GetKdqItemAsync(kdqId);
                var cleanedXml = RemoveNamespace.Replace(kdqXml, string.Empty);
                await File.WriteAllTextAsync($"{downloadSettings.Value.DownloadDir}/{kdqId}.xml", cleanedXml, CancellationToken.None);
                Interlocked.Increment(ref counter);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Saved {downloadSettings.Value.DownloadDir}/{kdqId}.xml");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving file: {kdqId}");
                Console.WriteLine(ex);
            }
        });
        
        Console.ResetColor();
        return counter;
    }

    private IReadOnlyCollection<string> GetKdqBaseListXml(string kdqListXml)
    {
        ArgumentNullException.ThrowIfNull(kdqListXml);
        XmlDocument kdqBaseList = new();
        kdqBaseList.LoadXml(kdqListXml);
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(kdqBaseList.NameTable);
        nsmgr.AddNamespace("ns", "http://www.brz.gv.at/eproc/kdq/20180626");
        XmlNodeList nodes = kdqBaseList.DocumentElement!.SelectNodes("//ns:item/ns:url", nsmgr)!;
        List<string> urls = new(nodes.Count);
        foreach (XmlNode node in nodes)
        {
            var match = ExtractId.Match(node.InnerText);
            if (match.Success)
            {
                urls.Add(match.Value);
            }
        }
        return urls;
    }

    [GeneratedRegex("[^/]+$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
    [GeneratedRegex("\\s+xmlns=\"[^\"]+\"", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();
}