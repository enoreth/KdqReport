
using System.Globalization;
using System.Text;
using System.Xml;
using CsvHelper;
using Microsoft.Extensions.Options;

namespace KdqParser;

internal sealed class ReportWriter(IOptions<FileSettings> fileSettings)
{
    internal async Task<string>CreateReportAsync()
    {
        DirectoryInfo filesDir = new(fileSettings.Value.DownloadDir);
        if (!filesDir.Exists)
        {
            throw new DirectoryNotFoundException($"Directory {fileSettings.Value.DownloadDir} does not exist");
        }

        var files = filesDir.GetFiles("*.xml");
        ArgumentNullException.ThrowIfNull(files);
        
        var reportItems = await CreateReportItems(files.Select(s => s.FullName));
        var fileName = await SaveReportAsync(reportItems);
        return fileName;
    }

    private async Task<string> SaveReportAsync(IReadOnlyCollection<ReportItem> reportItems)
    {
        ArgumentNullException.ThrowIfNull(reportItems);
        var filePath = Path.Join(fileSettings.Value.CsvFilePath, $"{DateTime.Now:yyyyMMddHHmmss}.csv"); 
        await using var writer = new StreamWriter(filePath, Encoding.UTF8, new FileStreamOptions(){ Access = FileAccess.Write, Mode = FileMode.OpenOrCreate});
        await using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);
        await csvWriter.WriteRecordsAsync(reportItems);
        await csvWriter.FlushAsync();
        await writer.FlushAsync();
        
        Console.WriteLine($"Wrote report to {filePath}");
        return filePath;
    }

    private async Task<IReadOnlyCollection<ReportItem>> CreateReportItems(IEnumerable<string> kdqFilePaths)
    {
        ArgumentNullException.ThrowIfNull(kdqFilePaths);
        List<ReportItem> reportItems = new();

        await Parallel.ForEachAsync(kdqFilePaths, async (filePath, _) =>
        {
            var kdqXmlText = await File.ReadAllTextAsync(filePath, CancellationToken.None);
            XmlDocument doc = new();
            doc.LoadXml(kdqXmlText);

            var contractingBody = doc.SelectSingleNode("//CONTRACTING_BODY");

            if (contractingBody == null)
            {
                Warn("Warning: No CONTRACTING_BODY element found");
                return;
            }
            
            var officialNameNode = contractingBody.SelectSingleNode("//ADDRESS_CONTRACTING_BODY/OFFICIALNAME");
            var emailNode = contractingBody.SelectSingleNode("//ADDRESS_CONTRACTING_BODY/E_MAIL");

            if (officialNameNode == null)
            {
                Warn($"Warning: No Officialname in {filePath}");
                return;
            }
            
            var email = string.Empty;
            if (emailNode != null)
            {
                email = emailNode.InnerText;
            }
            
            reportItems.Add(new(officialNameNode.InnerText, email));
        });

        //return reportItems.Distinct().ToList();
        return reportItems;
    }

    private void Warn(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}