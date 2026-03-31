.\incrementversion.ps1
dotnet run --project CakeBuild/CakeBuild.csproj -- $args
exit $LASTEXITCODE;