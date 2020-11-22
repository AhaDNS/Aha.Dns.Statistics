using Aha.Dns.Statistics.ServerApi.Extensions;
using System.Collections.Generic;

namespace Aha.Dns.Statistics.ServerApi.Models
{
    public class UnboundControlStats
    {
        // Total
        public long TotalNumQueries { get; set; }
        public long TotalNumCacheHits { get; set; }
        public long TotalNumCacheMiss { get; set; }
        public double TotalRecursionTimeAvg { get; set; }
        public double TotalRecursionTimeMedian { get; set; }

        // Time
        public double TimeUp { get; set; }
        public double TimeElapsed { get; set; }

        // Num query
        public long NumQueryTypeA { get; set; }
        public long NumQueryTypeSOA { get; set; }
        public long NumQueryTypeNull { get; set; }
        public long NumQueryTypeTXT { get; set; }
        public long NumQueryTypeAAA { get; set; }
        public long NumQueryTypeSRV { get; set; }
        public long NumQueryTypeDNSKEY { get; set; }
        public long NumQueryTypeAny { get; set; }

        // Num answer
        public long NumAnswerNOERROR { get; set; }
        public long NumAnswerFORMERR { get; set; }
        public long NumAnswerSERVFAIL { get; set; }
        public long NumAnswerNXDOMAIN { get; set; }
        public long NumAnswerNOTIMPL { get; set; }
        public long NumAnswerREFUSED { get; set; }
        public long NumAnswerNODATA { get; set; }

        // Extra
        public long DomainsOnBlocklist { get; set; }

        /// <summary>
        /// Empty constructor to make serializable
        /// </summary>
        public UnboundControlStats()
        {

        }

        /// <summary>
        /// Populate object from a enumerable of lines
        /// where lines is the direct output from unbound-control stats command
        /// </summary>
        /// <param name="lines"></param>
        public UnboundControlStats(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var splitLine = line?.Trim()?.Split('=');

                if (splitLine?.Length != 2)
                    continue;

                var key = splitLine[0];
                var value = splitLine[1];

                switch (key)
                {
                    // Total
                    case "total.num.queries":
                        TotalNumQueries = value.AsLong();
                        break;
                    case "total.num.cachehits":
                        TotalNumCacheHits = value.AsLong();
                        break;
                    case "total.num.cachemiss":
                        TotalNumCacheMiss = value.AsLong();
                        break;
                    case "total.recursion.time.avg":
                        TotalRecursionTimeAvg = value.AsDouble();
                        break;
                    case "total.recursion.time.median":
                        TotalRecursionTimeMedian = value.AsDouble();
                        break;

                    // Time
                    case "time.up":
                        TimeUp = value.AsDouble();
                        break;
                    case "time.elapsed":
                        TimeElapsed = value.AsDouble();
                        break;

                    // Num query
                    case "num.query.type.A":
                        NumQueryTypeA = value.AsLong();
                        break;
                    case "num.query.type.SOA":
                        NumQueryTypeSOA = value.AsLong();
                        break;
                    case "num.query.type.NULL":
                        NumQueryTypeNull = value.AsLong();
                        break;
                    case "num.query.type.TXT":
                        NumQueryTypeTXT = value.AsLong();
                        break;
                    case "num.query.type.AAAA":
                        NumQueryTypeAAA = value.AsLong();
                        break;
                    case "num.query.type.SRV":
                        NumQueryTypeSRV = value.AsLong();
                        break;
                    case "num.query.type.DNSKEY":
                        NumQueryTypeDNSKEY = value.AsLong();
                        break;
                    case "num.query.type.ANY":
                        NumQueryTypeAny = value.AsLong();
                        break;

                    // Num answer
                    case "num.answer.rcode.NOERROR":
                        NumAnswerNOERROR = value.AsLong();
                        break;
                    case "num.answer.rcode.FORMERR":
                        NumAnswerFORMERR = value.AsLong();
                        break;
                    case "num.answer.rcode.SERVFAIL":
                        NumAnswerSERVFAIL = value.AsLong();
                        break;
                    case "num.answer.rcode.NXDOMAIN":
                        NumAnswerNXDOMAIN = value.AsLong();
                        break;
                    case "num.answer.rcode.NOTIMPL":
                        NumAnswerNOTIMPL = value.AsLong();
                        break;
                    case "num.answer.rcode.REFUSED":
                        NumAnswerREFUSED = value.AsLong();
                        break;
                    case "num.answer.rcode.nodata":
                        NumAnswerNODATA = value.AsLong();
                        break;

                    // Extra
                    case "domains.on.blocklist":
                        DomainsOnBlocklist = value.AsLong(); ;
                        break;
                }
            }
        }
    }
}
