using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hs_HealthChecks.Models
{
    public class HealthCheckRunnerOptions
    {
        public int RetryCount { get; set; } = 3;
        public int DelaySeconds { get; set; } = 5;
        public int TimeoutSeconds { get; set; } = 10;
        public int MaxParallelism { get; set; } = 4;
    }

}
