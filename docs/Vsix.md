# Entity Framework Ruler

Automate the customization of the EF Core Reverse Engineered model. Supported changes include:
- Support for EF Core 6 through 10.
- Legacy EF6 (EDMX) migration support
- Class renaming
- Property renaming (including both primitives and navigations)
- Type changing (useful for enum mapping)
- Custom navigations that have no database foreign key, such as adding navigations to views
- Table splitting (multiple entities using subsets of columns from the same table)
- Entity inheritance (TPH, TPT, TPC)
- Auto-naming tables and columns using regex
- Skipping tables or columns
- Forcing inclusion of simple many-to-many entities into the model
- Entity type configuration file splitting

EF Ruler applies customizations from a rule document stored in the project folder.  Rules can be initialized with a call to [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli), or they can be fully generated from an EDMX such that the scaffolding output will align with the old EF6 EDMX-based model.

>"EF Ruler provides a smooth upgrade path from EF6 to EF Core by ensuring that the Reverse Engineered model maps perfectly from the old EDMX structure."

-------
### Upgrading from EF6 with EDMX:
1) Install the [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler), right click on an EDMX and go to _Convert EDMX to DB Context Rules_. ![EdmxConverterPreview.png](EdmxConverterPreview.png)
2) Reference [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.
3) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and the design-time service will do the rest.

-------
### Initializing DB Context Rules _without_ an EDMX:
1) Reference [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.
2) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and a complete rule file will be generated based on the reverse engineered model.  The rules can then be modified, and changes applied by re-running the [scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command.

-------
### DB Context Customization and Ongoing DB Maintenance

1) Edit the rules json by hand, or with the [VS Extension](https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler) installed, right click on the rules file and go to _Edit DB Context Rules_.
2) Adjust the model as necessary using the editor:  
![RuleEditorPreview.png](RuleEditorPreview.png)
3) Apply the customizations (see below).

-------
### Applying Model Customizations:
1) Reference NuGet package [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.  This is a design-time reference, meaning it will _not_ appear in the project build output, but will interact with EF Core's reverse engineering process.
2) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and the design-time service will apply all changes as per the json rule file.  The rule file itself will also sync up with the reverse engineered model.

-------
### Adding or Removing Tables From the Model:
By default, a rule file generated from EDMX limits tables and columns to just what was in the EDMX.  That way, an identical model can be generated.

If it's time to add a table or column to the model, adjust the IncludeUnknownTables/IncludeUnknownColumns flags at the relevant level.

If the database schema contains a lot of tables that you don't want to generate entities for, then enabling IncludeUnknownTables is not a good idea.  Instead, manually create the table entry in the rule file (using the [Editor](https://marketplace.visualstudio.com/items?itemName=Randell.EF-Ruler)) and set IncludeUnknownColumns to true.  On the next scaffold, the new entity will be generated fully.

You can remove entities from the model by marking the corresponding table (or column) as _Not Mapped_.

-------

### Entity Configuration Splitting:
The [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command does not natively support splitting entity type configurations into separate files.  Instead, all type configurations are stored in the same file as the context.

With EF Core 7 and later, [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) can split configurations for you.

Just enable "SplitEntityTypeConfigurations" in the rule file (at the root level).

-------

This project is under development!  Check back often, and leave comments [here](https://github.com/R4ND3LL/EntityFrameworkRuler/issues).
