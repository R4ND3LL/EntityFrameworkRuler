
# EDMX Ruler
Add EDMX support to EF Core, enabling a smooth upgrade from Entity Framework to EF Core.

The approach is simple:
1)	Analyze the EDMX for all customizations and generate a set of rule files.
2)	Discard the EDMX (optional of course).
3)	Proceed with EF Core database-first Reverse Engineering (scaffolding) steps to generate the entity model.
4)	Apply the customization rules to restore the original EF structure.

You only need to generate the rules from the EDMX once.  From then on, applying the customization rules is a single, easy step executed after Reverse Engineering the model (from the database).

## EDMX Customizations Supported
- Class renaming
- Property renaming (including both primitives and navigations)
- Property type changes such as in enum usage

## Installation
There are 2 ways to use this tool:
1. CLI:
   ```
   > dotnet tool install --global EdmxRuler
   ```
2. Nuget package:
   ```
   > nuget install EdmxRuler
   ```
See https://www.nuget.org/packages/EdmxRuler/ for details.
#### Coming soon: EF Power Tools built-in support.

## CLI Usage:
### To generate rules from an EDMX, run the following:
   ```
   > edmxruler -g <edmxFilePath> <efCoreProjectBasePath>
   ```
   If both paths are the same, i.e. the EDMX is in the EF Core project folder, it is acceptable to run:
   ```
   > edmxruler -g <projectFolderWithEdmx>
   ```
Structure rules will be extracted from the EDMX and saved in the EF Core project folder.

### To Apply rules to an EF Core model:
   ```
   > edmxruler -a <efCoreProjectBasePath>
   ```
This assumes that you have executed the scaffolding process to generate the model from the database.
For details on reverse engineering, go to: https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli

## API Usage
### To generate rules from an EDMX, use the following class:
```
EdmxRuler.Generator.RuleGenerator
```
### To Apply rules to an EF Core model, use the following class:
```
EdmxRuler.Applicator.RuleApplicator
```
## Examples

#### Generate and save rules:
```csharp
var generator = new RuleGenerator(edmxPath);  
var rules = generator.TryGenerateRules();  
await generator.TrySaveRules(projectBasePath);
```
#### Apply rules already in project path:
```csharp
var applicator = new RuleApplicator(projectBasePath);  
var response = await applicator.ApplyRulesInProjectPath();
```

#### More control over which rules are applied:
```csharp
var applicator = new RuleApplicator(projectBasePath);  
var loadResponse = await applicator.LoadRulesInProjectPath();  
var navRules = loadResponse.Rules.OfType<NavigationNamingRules>().First();
var enumRules = loadResponse.Rules.OfType<EnumMappingRules>().First();  
var applyResponse = await applicator.ApplyRules(enumRules);
```

#### Customize rule file names:
```csharp
var generator = new RuleGenerator(edmxPath);  
var rules = generator.TryGenerateRules();  
await generator.TrySaveRules(projectBasePath,  
    new RuleFileNameOptions() {  
        PrimitiveNamingFile = null, // null will skip this file
        NavigationNamingFile = "NavRenaming.json",   
        PropertyTypeChangingFile = "MyEnumMap.json"  
  }  
);
```
#### Handle log activity:
```csharp
var applicator = new RuleApplicator(projectBasePath);  
applicator.OnLog += (sender, message) => Console.WriteLine(message);
var response = await applicator.ApplyRulesInProjectPath();
```
