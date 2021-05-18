using EnvDTE;

namespace COFRS.Template.Common.Models
{
    public class MemberInfo
    {
        public string ClassName { get; set; }
        public string EntityName { get; set; }
        public ElementType ElementType { get; set; }
        public CodeElement Namespace { get; set; }
        public CodeElement Member { get; set; }
    }
}
