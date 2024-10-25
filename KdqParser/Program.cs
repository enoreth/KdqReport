using KdqParser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", false, false);
    })
    .ConfigureServices((ctx, svc) =>
    {
        svc.AddOptions();
        svc.AddOptions<KdqClientSettings>()
            .BindConfiguration(nameof(KdqClientSettings))
            .ValidateOnStart();

        svc.AddOptions<FileSettings>()
            .BindConfiguration(nameof(FileSettings))
            .ValidateOnStart();

        svc.AddRefitClient<IKdqApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<KdqClientSettings>>();
                client.BaseAddress = new Uri(options.Value.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutInSecs);
            });

        svc.AddTransient<KdqDownloader>();
        svc.AddTransient<ReportWriter>();
    }).Build();


 using var scope = host.Services.CreateScope();
 var downloader = scope.ServiceProvider.GetRequiredService<KdqDownloader>();
 var reportWriter = scope.ServiceProvider.GetRequiredService<ReportWriter>();

 Console.WriteLine("1 - Download All Kdq Files to Localdir");
 Console.WriteLine("2 - Create a Report of downloaded Kdq files");
 Console.WriteLine("3 - Download Kdq Files and create a report -DEFAULT");
 Console.WriteLine("4 - Clean XML");
 var option = Console.ReadLine();

 switch (option)
 {
     case "1":
         var downloadResult = await downloader.DownloadAndSaveAsync();
         break;
     case "2":
         var createReportresult = await reportWriter.CreateReportAsync();
         break;
     case "3":
         await downloader.DownloadAndSaveAsync();
         await reportWriter.CreateReportAsync();
         break;
     case "4":
         await reportWriter.CleanXmlFiles();
         break;
}
