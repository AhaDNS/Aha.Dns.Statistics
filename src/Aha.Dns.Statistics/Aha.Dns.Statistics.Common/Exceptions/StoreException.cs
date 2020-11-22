using System;
using System.Net;

namespace Aha.Dns.Statistics.Common.Exceptions
{
    public class StoreException : Exception
    {
        public StoreException()
        {

        }

        public StoreException(HttpStatusCode httpStatusCode, string message, Exception innerException) : base(message, innerException)
        {
            HttpStatusCode = httpStatusCode;
        }

        public HttpStatusCode HttpStatusCode { get; set; }
    }
}
