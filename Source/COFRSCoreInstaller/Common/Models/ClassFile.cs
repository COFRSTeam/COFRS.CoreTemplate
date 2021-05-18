using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Models
{
    /// <summary>
    /// Base class for Class File
    /// </summary>
    public class ClassFile
    {
        /// <summary>
        /// The class name of the entity represented by the file
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// The file name where the file is stored
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The namespace that contains the class
        /// </summary>
        public string ClassNameSpace { get; set; }

        /// <summary>
        /// The type of class this file represents
        /// </summary>
        public ElementType ElementType { get; set; }


    }
}
