using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Newegg.OZZO.RunnerProxy.Models
{
    public class CacheProcessOption
    {
        public CacheProcessOption()
        {
            this.WorkersForAllCacheFiles = 1;
            this.WorkersForEachFile = 0;
            this.IntervalOfWorkersForAllCacheFiles = TimeSpan.FromMilliseconds(1000);
            this.IntervalOfWorkersForEachFile = TimeSpan.FromMilliseconds(30);
        }

        public int WorkersForAllCacheFiles { get; set; }
        public int WorkersForEachFile { get; set; }
        public TimeSpan IntervalOfWorkersForAllCacheFiles { get; set; }
        public TimeSpan IntervalOfWorkersForEachFile { get; set; }
        public static CacheProcessOption Default
        {
            get
            {
                return new CacheProcessOption();
            }
        }
    }
}
