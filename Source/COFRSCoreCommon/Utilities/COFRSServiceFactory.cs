using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COFRSCoreCommon.Utilities
{
    public static class COFRSServiceFactory
    {
        private static object _codeService;

        public static T GetService<T>()
        {
            if ( typeof(T) == typeof(ICodeService))
            {
                if ( _codeService == null)
                    _codeService = new CodeService();

                return (T) _codeService;
            }
            
            return default(T);
        }
    }
}
