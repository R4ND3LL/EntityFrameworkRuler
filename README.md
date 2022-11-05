# EDMX Ruler
Add EDMX support to EF Core, enabling a smooth upgrade from EF6 to EF Core.

The approach is simple:
1)	Analyze the EDMX for all customizations and generate a set of rule files such as property renaming and enum mapping.
2)	Discard the EDMX (optional of course).
3)	Proceed with EF Core database-first scaffolding steps to generate the vanilla entity model.
4)	Apply the customization rules to the entity model to restore the EF6 structure.

You only need to generate the rules from the EDMX once.  From then on, applying the customization rules is a single, easy step executed after regeneration of the model.
