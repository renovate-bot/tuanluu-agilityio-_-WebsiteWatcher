using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Net;
using WebsiteWatcher.Services;

namespace WebsiteWatcher;

public class SafeBrowsingMiddleware(SafeBrowsingService safeBrowsingService) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();
        if (!context.BindingContext.BindingData.ContainsKey("url"))
        {
            var response = request!.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync("Please pass a 'url' on the query string");
            return;
        }

        var url = context.BindingContext.BindingData["Url"]?.ToString();
        if (!IsValidUrl(url))
        {
            var response = request!.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync("Please pass a valid 'url' on the query string");
            return;
        }

        var safeCheckResult = safeBrowsingService.Check(url);
        if (safeCheckResult.HasThreat)
        {
            var response = request!.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync(string.Join("", safeCheckResult.Threats));
            return;
        }
        else
        {
            await next(context);
        }
    }

    private bool IsValidUrl(string url)
    {
        // Check if the URL is valid
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
