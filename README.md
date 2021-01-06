# Aha.Dns.Statistics

Everything that's related to the AhaDNS server statistics collection can be found in this repository.  
Everywhere you read _Our_ and _We_, it refers to [AhaDNS](https://ahadns.com).

## Intended usecase

The solution in this repository is created for [AhaDNS](https://ahadns.com). It was created to enable AhaDNS to show request statistics on the website and to post daily updates on their social media accounts.

## Technologies and frameworks used

- [ASP.NET Core](https://dotnet.microsoft.com/learn/aspnet/what-is-aspnet-core)
- [Azure Functions](https://azure.microsoft.com/en-us/services/functions/)
- [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/)
- Programming language C#

## Disclaimer

This solution have been written on late evenings by [Fredrik](https://www.linkedin.com/in/fredrikopettersson/) and you should not look here to find any best practices on how to do .NET development. Fredrik can do better but have not had enough time to spend here so some shortcuts have probably been taken. Despite this, we (AhaDNS) want to have this project open-source so the public can see exactly what statistics AhaDNS do collect and how we (AhaDNS) collect it.  
You're very much welcome to come with improvements!

## Projects in this repository

Following is an overview of all projects in this repository and their function.

### Aha.Dns.Statistics.ServerApi

Web API to be able to retrieve statistics from [unbound-control](https://www.nlnetlabs.nl/documentation/unbound/unbound-control/) running on each DNS server. This project is installed on all AhaDNS DNS servers & all DNS server statistics are collected through this API.

### Aha.Dns.Statistics.CloudFunctions

An [Azure Function](https://azure.microsoft.com/en-us/services/functions/) project responsible for retrieving & storing statistics from all DNS servers running the _Aha.Dns.Statistics.ServerApi_ project.  
The project contains three main functions:

#### **TimeTriggeredStatisticsRetriever-function** (automatically triggered every 15 minutes)

1. Queries each DNS server for statistics.
2. Stores the retrieved server statistics in a [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/) account.
3. Summarize statistics over the last 24 hours and sends the summarized information to the wordpress website running at [ahadns.com](https://ahadns.com).

#### **MonthlyStatisticsSummarizer-function** (automatically triggered every hour)

1. Summarize statistics over the last 30 days and sends the summarized information to the wordpress website running at [ahadns.com](https://ahadns.com).

#### **Statistics web API function** (HTTP triggered)

1. A simple GET endpoint to retrieve the summarized statistics for a server over a given TimeSpan. Primarely used by our notifcation project [Aha.Dns.Notifications](https://github.com/AhaDNS/Aha.Dns.Notifications) to post daily updates in our social media accounts.

### Aha.Dns.Statistics.WebUI

An ASP.NET Core web app that shows DNS query statistics for the last 24h. Can be seen live at [statistics.ahadns.com](https://statistics.ahadns.com). Gets all information from the [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/) used to store statistics.
