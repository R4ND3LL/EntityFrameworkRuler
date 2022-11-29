# Entity Framework Ruler - Common

Automate the customization of the EF Core Reverse Engineered model. Supported changes include:
- Class renaming
- Property renaming (including both primitives and navigations)
- Type changing (useful for enum mapping)
- Skipping non-mapped columns.
- Forcing inclusion of simple many-to-many entities into the model.

EF Ruler applies customizations from a rule document stored in the project folder.  Rules can be fully generated from an EDMX (from old Entity Framework) such that the scaffolding output will align with the old EF6 EDMX-based model.

>"EF Ruler provides a smooth upgrade path from EF6 to EF Core by ensuring that the Reverse Engineered model maps perfectly from the old EDMX structure."

### Upgrading from EF6 couldn't be simpler:
1) The the [CLI tool](https://www.nuget.org/packages/EntityFrameworkRuler/) to analyze the EDMX for all customizations and generate the rules.
2) Reference [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.
3) Run the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command and the design-time service will do the rest.

Done.  

## Applying Model Customizations:
There are two options for applying the rules to a DB context model:
1) Simply reference [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) from the EF Core project.  Proceed with CLI scaffolding (as mentioned above).  [EntityFrameworkRuler.Design](https://www.nuget.org/packages/EntityFrameworkRuler.Design/) is a Design-Time reference package, meaning EF Core can use it during the scaffolding process to customize the generated model, but the assembly will NOT appear in the project build output.  It can't get more automated than this.
2) Use the [CLI tool](https://www.nuget.org/packages/EntityFrameworkRuler/) to apply changes to an already generated EF Core model.  This approach uses [Roslyn](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview) to apply code changes.  The code analysis is reliable, but for very large models this option can take a minute. 

## Road Map:
- EF Power Tools built-in support.
- Editor library to manage the rules and edit the EF Core model structure with a UI.

This project is under development!  Check back often, and leave comments [here](https://github.com/R4ND3LL/EntityFrameworkRuler/issues).


## Installation of the [CLI tool](https://www.nuget.org/packages/EntityFrameworkRuler/):
There are 2 ways to use the [CLI tool](https://www.nuget.org/packages/EntityFrameworkRuler/):
1. Command line:
   ```
   > dotnet tool install --global EntityFrameworkRuler 
   ```
2. API:
   ```
   > dotnet add package EntityFrameworkRuler   
   PM> NuGet\Install-Package EntityFrameworkRuler
   ```
See the [NuGet page](https://www.nuget.org/packages/EntityFrameworkRuler/) for details.

## CLI Usage:
### To generate rules from an EDMX, run the following:
   ```
   > efruler -g <edmxFilePath> <efCoreProjectBasePath>
   ```
   If both paths are the same, i.e. the EDMX is in the EF Core project folder, it is acceptable to run:
   ```
   > efruler -g <projectFolderWithEdmx>
   ```
Structure rules will be extracted from the EDMX and saved in the EF Core project folder.

### To Apply rules to an _already generated_ EF Core model:
   ```
   > efruler -a <efCoreProjectBasePath>
   ```
This assumes that you have executed the [ef dbcontext scaffold](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli) command to generate the model previously.

# API Usage
### To generate rules from an EDMX, use the following class:
```
EntityFrameworkRuler.Generator.RuleGenerator
```
### To Apply rules to an existing EF Core model, use the following class:
```
EntityFrameworkRuler.Applicator.RuleApplicator
```
## Examples

#### Generate and save rules:
```csharp
var generator = new RuleGenerator();
var response = generator.GenerateRules(edmxPath);
if (response.Success)
    await generator.SaveRules(response.Rules.First(), projectBasePath);
```
#### Apply rules already in project path:
```csharp
var applicator = new RuleApplicator();
var response = await applicator.ApplyRulesInProjectPath(projectBasePath);
```

#### More control over which rules are applied:
```csharp
var applicator = new RuleApplicator();
var loadResponse = await applicator.LoadRulesInProjectPath(projectBasePath);
var applyResponse = await applicator.ApplyRules(projectBasePath, loadResponse.Rules.First());
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