using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newegg.OZZO.RunnerProxy.Helpers;

namespace Newegg.OZZO.RunnerProxy.Models
{
    public class RunnerMailOption
    {
        public IRunnerMailer Mailer { get; set; }
        public RunnerMailContent Content { get; set; }
    }

    public class RunnerMailContent
    {
        public RunnerMailContent()
        {
            this.ContentType = RunnerMailOptionContentType.Html;
            this.Priority = RunnerMailOptionMailPriority.Low;
        }

        public string Subject { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CC { get; set; }
        public string Body { get; set; }
        public RunnerMailOptionContentType ContentType { get; set; }
        public RunnerMailOptionMailPriority Priority { get; set; }
    }

    public enum RunnerMailOptionContentType
    {
        Text,
        Html
    }

    public enum RunnerMailOptionMailPriority
    {
        Normal = 0,
        Low = 1,
        High = 2,
    }
}
