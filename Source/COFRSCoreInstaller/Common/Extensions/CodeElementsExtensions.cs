using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRS.Template.Common.Extensions
{
    public static class CodeElementsExtensions
    {
        public static List<T> GetTypes<T>(this CodeElements codeElements)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var results = new List<T>();

            foreach ( CodeElement element in codeElements)
            {
                if (element.Kind == vsCMElement.vsCMElementNamespace && typeof(T) == typeof(CodeNamespace))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementClass && typeof(T) == typeof(CodeClass))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementInterface && typeof(T) == typeof(CodeInterface))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementProperty && typeof(T) == typeof(CodeProperty))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementEnum && typeof(T) == typeof(CodeEnum))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementAttribute && typeof(T) == typeof(CodeAttribute))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementParameter && typeof(T) == typeof(CodeParameter))
                    results.Add((T)element);

                else if (element.Kind == vsCMElement.vsCMElementFunction && typeof(T) == typeof(CodeFunction))
                    results.Add((T)element);
            }

            return results;
        }
    }
}
