using System;

namespace Aha.Dns.Statistics.Common.Models
{
    public class DnsServerStatistics
    {
        /// <summary>
        /// Server name i.e. nl
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Number of queries requested
        /// </summary>
        public long QueriesRequested { get; set; }

        /// <summary>
        /// Number of queries that have been blocked
        /// </summary>
        public long QueriesBlocked { get; set; }

        /// <summary>
        /// Number of domains on block list
        /// </summary>
        public long DomainsOnBlockList { get; set; }

        /// <summary>
        /// DateTime when these statistics were creaqted
        /// </summary>
        public DateTime CreatedDate { get; set; }

        //
        // Counters for query types
        //
        public long QueryTypeA { get; set; }
        public long QueryTypeSOA { get; set; }
        public long QueryTypeNull { get; set; }
        public long QueryTypeTXT { get; set; }
        public long QueryTypeAAA { get; set; }
        public long QueryTypeSRV { get; set; }
        public long QueryTypeDNSKEY { get; set; }
        public long QueryTypeAny { get; set; }

        //
        // Counter for DNS response types
        //
        public long AnswerNOERROR { get; set; }
        public long AnswerFORMERR { get; set; }
        public long AnswerSERVFAIL { get; set; }
        public long AnswerNXDOMAIN { get; set; }
        public long AnswerNOTIMPL { get; set; }
        public long AnswerREFUSED { get; set; }
        public long AnswerNODATA { get; set; }
    }
}
