call buildLocalPackages.cmd
copy /Y libraries\Microsoft.PowerFx.Core\bin\Debug\netstandard2.0\Microsoft.PowerFx.Core*.* ..\..\CRM.Services.WEM\external\PowerFx\netstandard2.0
copy /Y libraries\Microsoft.PowerFx.Interpreter\bin\Debug\netstandard2.0\Microsoft.PowerFx.Interpreter*.* ..\..\CRM.Services.WEM\external\PowerFx\netstandard2.0
copy /Y libraries\Microsoft.PowerFx.Transport.Attributes\bin\Debug\netstandard2.0\Microsoft.PowerFx.Transport.Attributes*.* ..\..\CRM.Services.WEM\external\PowerFx\netstandard2.0