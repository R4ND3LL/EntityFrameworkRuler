# Entity Framework Ruler - Design

### This is a design-time library that interacts with EF Core's reverse engineer (scaffolding) process.  You should reference this package from the entity project in order to automatically apply customizations to the entity model.

-------

Automate the customization of the EF Core Reverse Engineered model. Features include:
- Class renaming
- Property renaming (including both primitives and navigations)
- Type changing (useful for enum mapping)
- Skipping non-mapped columns.
- Forcing inclusion of simple many-to-many entities into the model.
- Entity type configuration file splitting.

EF Ruler applies customizations from a rule document stored in the project folder.  Rules can be initialized with a call to [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli), or they can be fully generated from an EDMX such that the scaffolding output will align with the old EF6 EDMX-based model.

>"EF Ruler provides a smooth upgrade path from EF6 to EF Core by ensuring that the Reverse Engineered model maps perfectly from the old EDMX structure."

-------
### Upgrading from EF6 with EDMX:
1) Use the [command line tool](https://www.nuget.org/packages/EntityFrameworkRuler/) or the [VS Extension](https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler) to generated DB Context rules from an EDMX file. 
2) Reference [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.
3) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and the design-time service will do the rest.

-------
### Initializing DB Context Rules _without_ an EDMX:
1) Reference [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.
2) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and a complete rule file will be generated based on the reverse engineered model.  The rules can then be modified, and changes applied by re-running the [scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command.

-------
### DB Context Customization and Ongoing DB Maintenance

1) Edit the rules json by hand, or with the [VS Extension](https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler) installed, right click on the rules file and go to _Edit DB Context Rules_.
2) Apply the customizations (see below).

-------
### Applying Model Customizations:
1) Reference NuGet package [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.  This is a design-time reference, meaning it will _not_ appear in the project build output, but will interact with EF Core's reverse engineer process.
2) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and the design-time service will apply all changes as per the json rule file.  The rule file itself will also sync up with the reverse engineered model.

-------
### Adding or Removing Tables From the Model:
By default, a rule file generated from EDMX limits tables and columns to just what was in the EDMX.  That way, an identical model can be generated.

If it's time to add a table or column to the model, adjust the IncludeUnknownTables or IncludeUnknownColumns flags at the relevant level.

If the database schema contains a lot of tables that you don't want to generate entities for, then enabling IncludeUnknownTables is not a good idea.  Instead, manually create the table entry in the rule file (using the [Editor]((https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler))) and set IncludeUnknownColumns to true.  On the next scaffold, the new entity will be generated fully.

You can remove entities from the model by marking the corresponding table (or column) as _Not Mapped_.

-------

### Entity Configuration Splitting:
The [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command does not natively support splitting entity type configurations into separate files.  Instead, all type configurations are stored in the same file as the context.

With EF7, [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) can split configurations for you.

Just enable "SplitEntityTypeConfigurations" in the rule file (at the root level).

--------

## Installation of the Command Line Tool:
   ```
   > dotnet tool install --global EntityFrameworkRuler --version <the latest version>
   ```
See the [NuGet page](https://www.nuget.org/packages/EntityFrameworkRuler/) for details.

-------
## Command Line Tool Usage:

### To generate rules from an EDMX, run the following:
   ```
   > efruler -g <edmxFilePath> <efCoreProjectBasePath>
   ```
If both paths are the same, i.e. the EDMX is in the EF Core project folder, it is acceptable to run:
   ```
   > efruler -g <projectFolderWithEdmx>
   ```
DB context rules will be extracted from the EDMX and saved in the EF Core project folder.

### To Apply rules to an _already generated_ EF Core model:
It is _strongly_ recommended to just run [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) with the [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) library referenced in order to apply customizations.  However, if this is not an option, the following command can apply renaming and type mapping to existing entities (using Roslyn).  For very large projects, this can take a minute.
   ```
   > efruler -a <efCoreProjectBasePath>
   ```

-------
# API Usage
While the [command line tool](https://www.nuget.org/packages/EntityFrameworkRuler/), [EntityFrameworkRuler.Design package](https://www.nuget.org/packages/EntityFrameworkRuler.Design/), and [VS Extension](https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler) are intended to provide all the features necessary to customize the reverse engineered model, **without writing any code**, the API is available and fully extensible if you need to tailor the process further.

Reference NuGet package [EntityFrameworkRuler.Common](https://www.nuget.org/packages/EntityFrameworkRuler.Common/)
#### To override default services:

```csharp
serviceCollection
   .AddRuler()
   .AddSingleton<IRuleSerializer, MyBinaryRuleSerializer>()
   .AddTransient<IRulerNamingService, MyCustomNamingService>()
   .AddTransient<IEdmxParser, MyEdmxParser>()    
```
#### To generate rules from an EDMX:
```csharp
var generator = new RuleGenerator(); // or use injected IRuleGenerator instance
var response = generator.GenerateRules(edmxPath);
if (response.Success)
    await generator.SaveRules(response.Rules.First(), projectBasePath);
```
#### Apply rules using _Roslyn_ to a project with rules file in the same path:
```csharp
var applicator = new RuleApplicator(); // or use injected IRuleApplicator instance
var response = await applicator.ApplyRulesInProjectPath(projectBasePath);
```
#### Customize rule file name:
```csharp
var generator = new RuleGenerator();
var response = generator.GenerateRules(edmxPath);
if (response.Success)
    await generator.SaveRules(projectBasePath, dbContextRulesFile: "DbContextRules.json", response.Rules.First());
```
#### Handle log activity:
```csharp
var applicator = new RuleApplicator();
applicator.Log += (sender, message) => Console.WriteLine(message);
var response = await applicator.ApplyRulesInProjectPath(projectBasePath);
```

-------
This project is under development!  Check back often, and leave comments [here](https://github.com/R4ND3LL/EntityFrameworkRuler/issues).
