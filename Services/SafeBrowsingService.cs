﻿using Google.Apis.Safebrowsing.v4;
using Google.Apis.Safebrowsing.v4.Data;
using Microsoft.Extensions.Configuration;

namespace WebsiteWatcher.Services;

public class SafeBrowsingService(IConfiguration configuration)
{
    public (bool HasThreat, IReadOnlyList<string> Threats) Check(string url)
    {
        var initializer = new Google.Apis.Services.BaseClientService.Initializer
        {
            ApiKey = configuration.GetValue<string>("GoogleSafeBrowsingApiKey")
        };
        using var safeBrowsing = new SafebrowsingService(initializer);
        var request = new GoogleSecuritySafebrowsingV4FindThreatMatchesRequest
        {
            Client = GetClientInfo(),
            ThreatInfo = GetThreatInfo(url)
        };
        var threatList = new List<string>();
        var hasThreat = false;
        var threatMatches = safeBrowsing.ThreatMatches.Find(request).Execute();
        if (threatMatches?.Matches != null)
        {
            hasThreat = true;
            foreach (var match in threatMatches.Matches)
            {
                threatList.Add($"Threat found: {match.ThreatType} at {match.Threat.Url}");
            }
        }
        else
        {
            threatList.Add("No threats found.");
        }

        return (hasThreat, threatList);
    }

    private GoogleSecuritySafebrowsingV4ThreatInfo GetThreatInfo(string url)
    {
        return new GoogleSecuritySafebrowsingV4ThreatInfo
        {
            ThreatTypes = ["MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"],
            PlatformTypes = ["ANY_PLATFORM"],
            ThreatEntryTypes = ["URL"],
            ThreatEntries = new List<GoogleSecuritySafebrowsingV4ThreatEntry>
            {
                new GoogleSecuritySafebrowsingV4ThreatEntry
                {
                    Url = url
                }
            }
        };
    }

    private GoogleSecuritySafebrowsingV4ClientInfo GetClientInfo()
    {
        return new GoogleSecuritySafebrowsingV4ClientInfo
        {
            ClientId = "WebsiteWatcher",
            ClientVersion = "1.0.0"
        };
    }
}
