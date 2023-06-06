// using Microsoft.EntityFrameworkCore.Infrastructure;
//
// namespace EntityFrameworkRuler.Design.Metadata.Builders;
//
// public class InternalParameterBuilder : AnnotatableBuilder<Parameter, ModelBuilderEx> {
//     public InternalParameterBuilder(Parameter metadata, ModelBuilderEx modelBuilder) : base(metadata, modelBuilder) { }
//     //public IParameter Metadata { get; }
//     public void HasType(string dbParameterStoreType) {
//         Metadata.StoreType = dbParameterStoreType;
//     }
//
//     public void HasOutput(bool isOutput) {
//         Metadata.IsOutput = isOutput;
//     }
// }