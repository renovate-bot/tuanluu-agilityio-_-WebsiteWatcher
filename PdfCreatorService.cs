using PuppeteerSharp;

namespace WebsiteWatcher;

public class PdfCreatorService
{
    public async Task<Stream> ConvertPageToPdfAsync(string url)
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
