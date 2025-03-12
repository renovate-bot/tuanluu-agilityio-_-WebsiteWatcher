using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WebsiteWatcher.Services;

namespace WebsiteWatcher.Functions;

public class Register(ILogger<Register> logger, SafeBrowsingService safeBrowsingService)
{
    [Function(nameof(Register))]
    public async Task<OutputType> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var website = JsonSerializer.Deserialize<Website>(requestBody, options);
        website.Id = Guid.NewGuid();

        var result = safeBrowsingService.Check(website.Url);
        if (result.HasThreat)
        {
            var threats = string.Join(", ", result.Threats);
            logger.LogInformation($"Website {website.Url} has threats: {threats}");
            return new OutputType()
            {
                Website = website,
                HttpResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest)
            };
        }

        return new OutputType()
        {
            Website = website,
            HttpResponse = req.CreateResponse(System.Net.HttpStatusCode.Created)
        };
    }
}

public class Website
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public string? XPathExpression { get; set; }
}

public class OutputType
{
    [SqlOutput("dbo.Websites", "WebsiteWatcher")]
    public Website Website { get; set; }

    public HttpResponseData HttpResponse { get; set; }
}
