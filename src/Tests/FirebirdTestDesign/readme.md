# Firebird Test Project
***

### Do not run this project.  It is a target for rule application only.

***

### Reverse engineer with local Firebird by calling:

```
dotnet ef dbcontext scaffold "User=SYSDBA;Password=masterkey;Database=employee;DataSource=localhost;Port=3050;Dialect=3;Charset=NONE;Role=;Connection lifetime=15;Pooling=true;MinPoolSize=0;MaxPoolSize=50;PacketSize=8192;ServerType=0;" FirebirdSql.EntityFrameworkCore.Firebird -o Models -c FirebirdEntities -f  --context-dir Context
```

Add "--no-build" to avoid the build step if the last build is still valid.

***