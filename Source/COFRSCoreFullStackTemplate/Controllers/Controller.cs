using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using $entitynamespace$;
using $resourcenamespace$;
using COFRS;
$if$ ($singleexamplenamespace$ != none)using $singleexamplenamespace$;
$endif$using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Annotations;
$if$ ($validationnamespace$ != none)using $validationnamespace$;
$endif$using $orchestrationnamespace$;
$if$ ($policy$ == using)using Microsoft.AspNetCore.Authorization;
$endif$
namespace $rootnamespace$
{
$controllerModel$}
