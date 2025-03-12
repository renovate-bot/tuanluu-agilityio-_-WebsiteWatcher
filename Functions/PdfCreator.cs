using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using WebsiteWatcher.Services;

namespace WebsiteWatcher.Functions;

public class PdfCreator(ILogger<PdfCreator> logger, PdfCreatorService pdfCreatorService)
{
    // Visit https://aka.ms/sqltrigger to learn how to use this trigger binding
    [Function(nameof(PdfCreator))]
    public async Task<byte[]?> Run(
        [SqlTrigger("[dbo].[Websites]", "WebsiteWatcher")] SqlChange<Website>[] changes)
    {
        byte[]? buffer = null;
        foreach (var change in changes)
        {
            if (change.Operation == SqlChangeOperation.Insert)
            {
                var result = await pdfCreatorService.ConvertPageToPdfAsync(change.Item.Url);
                buffer = new byte[result.Length];
                await result.ReadAsync(buffer.AsMemory(0, buffer.Length));

                logger.LogInformation($"PDF stream length is: {result.Length}");

                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:WebsiteWatcherStorage");
                var blobClient = new BlobClient(connectionString, "pdfs", $"{change.Item.Id}.pdf");
                await blobClient.UploadAsync(new MemoryStream(buffer));
            }
        }

        return buffer;
    }

    
}
