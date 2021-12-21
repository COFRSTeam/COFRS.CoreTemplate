namespace COFRS.Template.Common.ServiceUtilities
{
    internal static class COFRSServiceFactory
    {
        private static object _codeService = null;

        public static T GetService<T>()
        {
            if ( typeof(T) == typeof(ICodeService))
            {
                if (_codeService == null)
                    _codeService = new CodeService();

                return (T)_codeService;
            }

            return default(T);  
        }
    }
}
