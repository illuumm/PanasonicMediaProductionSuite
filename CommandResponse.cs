using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanasonicMediaProductionSuite
{
    public class CommandResponse
    {
        public string Command { get; set; }
        public string Parameter { get; set; }
        public string Response { get; set; }
        public string NACKDetail { get; set; }
    }
}
