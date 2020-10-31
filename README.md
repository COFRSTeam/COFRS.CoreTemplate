# COFRS REST Service (.NET Core)
A template to create a COFRS RESTful service using Microsoft .NET Core. This product is still in alpha.

## Installation
Once this product becomes available for general release, the COFRS Core Template will be available through Microsoft's Visual Studio Marketplace. Until then, if you wish to play around with the preview package, clone this repository and build it with Visual Studio. In the resulting ./Source/COFRSInstaller/bin/Release folder, double click on the COFRSCoreInstaller.visx file, and then follow the instructions.

## Building a COFRS Rest Service
In the Visual Studio start up screen, select "Create New Project", or if you are already in Visual Studio, select File -> New -> Project. In the Create a New Project dialog, make sure that C# is selected in the language drop down, select Windows in the platform drop down and select Web in the project types dropdown. In the list, find COFRS REST Service (.NET Core), and select it.

Press the Next button.

Enter the name of your new service in the Project Name edit box, and select the folder that will hold your new service. Once you've filled in the dialog with your desired options, press the Create button.

The COFRS RESTful Service Template dialog will appear. There are four options:

* Framework - here you can select either .NET Core 2.1 or .NET Core 3.1, as at the time of this writing, these are the only two .NET Core versions supported by Microsoft.
* Security Model - You have two options: OAuth/Open Id Connect, or None. If you want to implement a security model other than OAuth/Open Id Connect, select the None option. You will have to implement this yourself, as only OAuth/Open Id Connect is supported out of the box.
* Database Technology - out of the box, COFRS supports SQL Server, Postgresql and MySQL. Choose the database technology your service will use.
* Company Moniker - enter a short name that represents your company. I.e., if you are working for Acme Motor Supplies, you might enter "acme" or "acmemotors".

Press OK to create your service.



