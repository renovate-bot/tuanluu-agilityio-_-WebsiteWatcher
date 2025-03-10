using System;
using Azure.Storage.Blobs;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace WebsiteWatcher;

public class Watcher(ILogger<Watcher> logger)
{
    private const string SqlInputQuery = @"SELECT w.Id, w.Url, w.XPathExpression, s.[Content] AS LatestContent
                                        FROM dbo.Websites AS w 
                                        LEFT OUTER JOIN dbo.Snapshots AS s ON w.Id = s.Id
                                        WHERE (s.Timestamp = (SELECT MAX(Timestamp) FROM dbo.Snapshots WHERE (Id = w.Id)))";
    [Function(nameof(Watcher))]
    [SqlOutput("dbo.Snapshots", "WebsiteWatcher")]
    public async Task<SnapshotRecord?> Run([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer,
        [SqlInput(SqlInputQuery, "WebsiteWatcher")] IReadOnlyList<WebsiteModel> websites)
    {
        logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        SnapshotRecord? result = null;

        foreach (var website in websites)
        {
            HtmlWeb web = new();
            HtmlDocument doc = web.Load(website.Url);

            var divWithContent = doc.DocumentNode.SelectSingleNode(website.XPathExpression);
            var content = divWithContent != null ? divWithContent.InnerText.Trim() : "No content";
            content = content.Replace("Microsoft Entra", "Azure AD");

            var contentHasChanged = website.LatestContent != content;
            if (contentHasChanged)
            {
                logger.LogInformation($"Content has changed for website: {website.Url}");
                logger.LogInformation($"Old content: {website.LatestContent}");
                logger.LogInformation($"New content: {content}");

                var newPdf = await ConvertPageToPdfAsync(website.Url);
                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:WebsiteWatcherStorage");
                var blobClient = new BlobClient(connectionString, "pdfs", $"{website.Id}-{DateTime.UtcNow:MMddyyyyhhmmss}.pdf");
                await blobClient.UploadAsync(newPdf);

                logger.LogInformation($"New PDF uploaded.");

                result = new SnapshotRecord(website.Id, content);
            }
            else
            {
                logger.LogInformation($"Content has not changed for website: {website.Url}");
            }
        }
        logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        return result;
    }

    private async Task<Stream> ConvertPageToPdfAsync(string url)
    {
        var browserFetcher = new BrowserFetcher();

        await browserFetcher.DownloadAsync();
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true
        });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url);
        await page.EvaluateExpressionAsync("document.fonts.ready");
        var result = await page.PdfStreamAsync();
        result.Position = 0;

        return result;
    }
}

public class WebsiteModel
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public string? XPathExpression { get; set; }
    public string LatestContent { get; set; }
}
