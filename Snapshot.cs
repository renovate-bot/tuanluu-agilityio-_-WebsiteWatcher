using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;

namespace WebsiteWatcher;

public class Snapshot(ILogger<Snapshot> logger)
{
    [Function(nameof(Snapshot))]
    public void Run(
            [SqlTrigger("[dbo].[Websites]", "WebsiteWatcher")] IReadOnlyList<SqlChange<Website>> changes)
    {
        foreach (var change in changes)
        {
            logger.LogInformation($"Change Type: {change.Operation}");
            logger.LogInformation($"Website Id: {change.Item.Id} Website Url: {change.Item.Url}");
        }
    }
}
