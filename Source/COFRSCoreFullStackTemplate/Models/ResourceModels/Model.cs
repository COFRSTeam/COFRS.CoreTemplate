using System;
$if$ ($resourcebarray$ == true)using System.Collections;
$endif$using System.Collections.Generic;
$if$ ($usenpgtypes$ == true)using NpgsqlTypes;
$endif$$if$ ($resourceimage$ == true)using System.Drawing;
$endif$$if$ ($resourcenet$ == true)using System.Net;
$endif$$if$ ($resourcenetinfo$ == true)using System.Net.NetworkInformation;
$endif$$if$ ($annotations$ == true)using System.ComponentModel.DataAnnotations;
$endif$using $entitynamespace$;
using COFRS;

namespace $rootnamespace$
{
$resourceModel$}
