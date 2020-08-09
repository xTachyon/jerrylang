using JerryLang;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Data;

namespace JerryLang {
    class CodeGenerator {
        private TranslationUnit Unit { get; }
        public LLVMModuleRef Module { get; }
        private LLVMBuilderRef Builder { get; set; }
        private Dictionary<Variable, LLVMValueRef> Variables { get; }

        public CodeGenerator(TranslationUnit tu, string name) {
            Unit = tu;
            Module = LLVM.ModuleCreateWithName(name);
            Variables = new Dictionary<Variable, LLVMValueRef>();
        }

        public void Generate() {
            foreach (var i in Unit.Functions) {
                Generate(i);
            }
            if (LLVM.VerifyModule(Module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string message)) {
                throw new CompilerErrorException("invalid module" + message);
            }
        }

        void Generate(Function function) {
            var returnType = Translate(function.ReturnType);
            var functionType = LLVM.FunctionType(returnType, new LLVMTypeRef[0], false);
            var llvmFunction = LLVM.AddFunction(Module, function.Name, functionType);

            LLVMBasicBlockRef entry = LLVM.AppendBasicBlock(llvmFunction, "entry");
            Builder = LLVM.CreateBuilder();
            LLVM.PositionBuilderAtEnd(Builder, entry);

            Generate(function.Block);

            if (function.ReturnType.IsUnit()) {
                LLVM.BuildRetVoid(Builder);
            }

            if (LLVM.VerifyFunction(llvmFunction, LLVMVerifierFailureAction.LLVMPrintMessageAction)) {
                throw new CompilerErrorException("invalid module");
            }
        }

        void Generate(Statement statement) {
            if (statement is Assignment) {
                Generate((Assignment)statement);
            }
        }

        void Generate(Assignment assignment) {
            var llvmType = Translate(assignment.Variable.Type);
            if (assignment.IsDeclaration) {
                Variables[assignment.Variable] = LLVM.BuildAlloca(Builder, llvmType, assignment.Variable.Name);
            }
            
            var alloca = Variables[assignment.Variable];
            var expression = Generate(assignment.Expression);

            LLVM.BuildStore(Builder, expression, alloca);
        }

        void Generate(Block block) {
            foreach (var i in block.Statements) {
                Generate(i);
            }
        }

        LLVMValueRef Generate(Expression expression) {
            if (expression is NumberLiteralExpression) {
                return Generate((NumberLiteralExpression)expression);
            }
            if (expression is BinaryOperation) {
                return Generate((BinaryOperation)expression);
            }
            if (expression is VariableReference) {
                return Generate((VariableReference)expression);
            }

            throw new CompilerErrorException("unknown expression");
        }

        LLVMValueRef Generate(VariableReference expression) {
            var variable = Variables[expression.Variable];
            return LLVM.BuildLoad(Builder, variable, "tmp" + expression.Variable.Name);
        }

        LLVMValueRef Generate(BinaryOperation expression) {
            var left = Generate(expression.Left);
            var right = Generate(expression.Right);

            switch (expression.Operation) {
                case BinaryOperationKind.Plus:
                    return LLVM.BuildAdd(Builder, left, right, "tmpadd");
                case BinaryOperationKind.Multiply:
                    return LLVM.BuildMul(Builder, left, right, "tmpmul");
                default:
                    break;
            }

            throw new CompilerErrorException("unknown binary op");
        }

        LLVMValueRef Generate(NumberLiteralExpression expression) {
            var type = Translate(expression.GetAstType());
            return LLVM.ConstInt(type, (ulong)expression.Number, true);
        }

        LLVMTypeRef Translate(AstType type) {
            if (type is BuiltinType) {
                return Translate((BuiltinType)type);
            }
            throw new CompilerErrorException("unknown type");
        }

        LLVMTypeRef Translate(BuiltinType type) {
            switch (type.Kind) {
                case BuiltinTypeKind.Unit:
                    return LLVMTypeRef.VoidType();
                case BuiltinTypeKind.Bool:
                    return LLVMTypeRef.Int1Type();
                case BuiltinTypeKind.Number:
                    return LLVMTypeRef.Int64Type();
                case BuiltinTypeKind.String:
                    break;
            }
            throw new CompilerErrorException("unknown builtin type");
        }
    }
}