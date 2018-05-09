Write-Output 'clean nupkg'
del *.nupkg
del   .\src\FM.ConsulInterop\bin\Release\*.nupkg

Write-Output 'gen nuget package'
dotnet build -c Release  .\src\FM.ConsulInterop\FM.ConsulInterop.csproj

Write-Output 'move nupkg '
mv  .\src\FM.ConsulInterop\bin\Release\*.nupkg .

