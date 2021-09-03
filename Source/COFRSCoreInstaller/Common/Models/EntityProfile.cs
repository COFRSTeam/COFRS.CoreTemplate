﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Models
{
    public class EntityProfile
    {
        public string EntityColumnName { get; set; }
        public string MapFunction { get; set; }
        public string[] ResourceColumns { get; set; }
    }
}