using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Models
{
    public class EntityProfile
    {
        public string EntityColumnName { get; set; }
        public string MapFunction { get; set; }
        public string[] ResourceColumns { get; set; }
        public bool IsDefined { get; set; }
    }
}
