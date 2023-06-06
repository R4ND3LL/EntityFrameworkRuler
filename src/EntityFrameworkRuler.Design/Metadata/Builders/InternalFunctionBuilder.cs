// using Microsoft.EntityFrameworkCore.Infrastructure;
// using Microsoft.EntityFrameworkCore.Metadata.Internal;
//
// namespace EntityFrameworkRuler.Design.Metadata.Builders;
//
// public class InternalFunctionBuilder : AnnotatableBuilder<Function, ModelBuilderEx> {
//     public InternalFunctionBuilder(Function metadata, ModelBuilderEx modelBuilder) : base(metadata, modelBuilder) { }
//     //public IFunction Metadata { get; }
//     public ParameterBuilder CreateParameter(string paramName) {
//         return Metadata.CreateParameter(paramName);
//     }
//
//     public void HasReturnType(string returnType) {
//         Metadata.ReturnType = returnType;
//     }
//     public void HasCommandText(string commandText) {
//         Metadata.CommandText= commandText;
//     }
// }