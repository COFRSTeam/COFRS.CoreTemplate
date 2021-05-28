using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using $entitynamespace$;
using $resourcenamespace$;
using COFRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
$if$ ($validationnamespace$ != none)using $validationnamespace$;
$endif$using $orchestrationnamespace$;
$if$ ($policy$ == using)using Microsoft.AspNetCore.Authorization;
$endif$
namespace $rootnamespace$
{
$model$}
