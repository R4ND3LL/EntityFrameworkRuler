@echo off

del /s Context\*.cs 1>nul
del /s Models\*.cs 1>nul

dotnet ef dbcontext scaffold "data source=localhost;initial catalog=Northwind;persist security info=True;Integrated Security=SSPI;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c NorthwindEntities -f  --context-dir Context
