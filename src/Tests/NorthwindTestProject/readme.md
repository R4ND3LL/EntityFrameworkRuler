# Northwind Test Project
***

### Do not run this project.  It is a target for rule application only.

***

### Reverse engineer with local Northwind by calling:

```
dotnet ef dbcontext scaffold "data source=localhost;initial catalog=Northwind;persist security info=True;Integrated Security=SSPI;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c NorthwindDbContext -f

```

Add "--no-build" to avoid the build step if the last build is still valid.

***