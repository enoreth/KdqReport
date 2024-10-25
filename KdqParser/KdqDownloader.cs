using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Microsoft.Extensions.Options;

namespace KdqParser;

internal sealed partial class KdqDownloader(IKdqApi kdqApi, IOptions<FileSettings> downloadSettings)
{
    private static readonly Regex ExtractId = MyRegex();
    public static readonly Regex RemoveNamespace = MyRegex1();
    public static readonly Regex RemoveNamespace2 = MyRegex2();
    public static readonly Regex RemoveNamespace3 = MyRegex3();
    public static readonly Regex RemoveNoise = MyRegex4();
    public static readonly Regex RemoveSpaceAfterXsd = MyRegex5();
    

    public static string CleanXmlText(string text)
    {
        var cleanedXml = RemoveNoise.Replace(text, string.Empty);
        cleanedXml = RemoveNamespace.Replace(cleanedXml, string.Empty);
        cleanedXml = RemoveNamespace2.Replace(cleanedXml, string.Empty);
        cleanedXml = RemoveNamespace3.Replace(cleanedXml, string.Empty);
        cleanedXml = RemoveSpaceAfterXsd.Replace(cleanedXml, string.Empty);
        return cleanedXml;
    }
        
    internal async Task<int> DownloadAndSaveAsync()
    {
        //var kdqListXml = await kdqApi.GetAllKdqItemsAsync();
        var kdqListXml = await kdqApi.GetAllKdqItemsAusschreibungAtAsync();
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
        foreach(var kdqId in kdqIds)
        {
            try
            {
                var matches = Regex.Match(@kdqId, "id=([A-Za-z0-9]+)");
                Console.WriteLine(matches.Groups[1].Value);
                
                var kdqXml = await kdqApi.GetKdqItemAusschreibungAtAsync(matches.Groups[1].Value);
                var cleanedXml = CleanXmlText(kdqXml);
                
                await File.WriteAllTextAsync($"{downloadSettings.Value.DownloadDir}/{matches.Groups[1].Value}.xml", cleanedXml, CancellationToken.None);
                Interlocked.Increment(ref counter);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Saved {downloadSettings.Value.DownloadDir}/{kdqId}.xml");
                Thread.Sleep(15);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving file: {kdqId}");
                Console.WriteLine(ex);
            }
        };
        
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
    
    [GeneratedRegex("\\s+xmlns\\s*=\"[^\"]+\"", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();
    
    
    [GeneratedRegex("\\s+xmlns:xsi=\"[^\"]+\"", RegexOptions.Compiled)]
    private static partial Regex MyRegex2();
    
    [GeneratedRegex("\\s+xsi:schemaLocation=\"[^\"]+\"", RegexOptions.Compiled)]
    private static partial Regex MyRegex3();
    
    [GeneratedRegex(@"[\t\r\n]+")]
    private static partial Regex MyRegex4();
    
    [GeneratedRegex(@"(?<=\.xsd)\s+")]
    private static partial Regex MyRegex5();
}