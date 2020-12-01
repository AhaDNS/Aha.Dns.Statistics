using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.ServerApi.Utilities
{
    public interface IBashUtil
    {
        /// <summary>
        /// Execute any string as a bash command
        /// and return the standard output as an enumerable of strings
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> ExecuteBash(string cmd);
    }
}
