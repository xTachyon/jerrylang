using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;

namespace JerryLang {
    class CodeGenerator {
        private TranslationUnit Unit { get; }
        private LLVMContextRef Context { get; set; }
        public LLVMModuleRef Module { get; }
        private LLVMBuilderRef Builder { get; set; }
        private LLVMDIBuilderRef DiBuilder { get; set; }
        private Dictionary<AstElement, LLVMValueRef> Things { get; }

        public CodeGenerator(TranslationUnit tu, string name) {
            Unit = tu;
            Context = LLVMContextRef.Create();
            Module = Context.CreateModuleWithName(name);
            Builder = Context.CreateBuilder();
            DiBuilder = Module.CreateDIBuilder();

            Things = new Dictionary<AstElement, LLVMValueRef>();
        }

        public void Generate() {
            foreach (var i in Unit.Functions) {
                Generate(i);
            }
            Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        }

        void Generate(Function function) {
            var returnType = Translate(function.ReturnType);
            var argsTypes = function.Arguments.Select(x => Translate(x.Item2)).ToArray();
            var functionType = LLVMTypeRef.CreateFunction(returnType, argsTypes, false);
            var llvmFunction = Module.AddFunction(function.Name, functionType);

            Things[function] = llvmFunction;

            DiBuilder.CreateFile("a", "b");

            if (function.Block == null) {
                return;
            }

            LLVMBasicBlockRef entry = Context.AppendBasicBlock(llvmFunction, "entry");
            Builder = Context.CreateBuilder();
            Builder.PositionAtEnd(entry);

            Generate(function.Block);

            if (function.ReturnType.IsUnit()) {
                Builder.BuildRetVoid();
            }

            if (!llvmFunction.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction)) {
                throw new CompilerErrorException("invalid module");
            }
        }

        void Generate(Statement statement) {
            if (statement is Assignment assignment) {
                Generate(assignment);
                return;
            } else if (statement is Expression expression) {
                Generate(expression);
                return;
            }

            throw new CompilerErrorException("unknown stmt");
        }

        void Generate(Assignment assignment) {
            var llvmType = Translate(assignment.Variable.Type);
            if (assignment.IsDeclaration) {
                Things[assignment.Variable] = Builder.BuildAlloca(llvmType, assignment.Variable.Name);
            }
            
            var alloca = Things[assignment.Variable];
            var expression = Generate(assignment.Expression);

            Builder.BuildStore(expression, alloca);
        }

        void Generate(Block block) {
            foreach (var i in block.Statements) {
                Generate(i);
            }
        }

        LLVMValueRef Generate(Expression expression) {
            if (expression is NumberLiteralExpression number) {
                return Generate(number);
            } else if (expression is BinaryOperation binary) {
                return Generate(binary);
            } else if (expression is VariableReference reference) {
                return Generate(reference);
            } else if (expression is FunctionCall call) {
                return Generate(call);
            }

            throw new CompilerErrorException("unknown expression");
        }

        LLVMValueRef Generate(FunctionCall expression) {
            var llvmFunction = Things[expression.Function];
            var args = expression.Arguments.Select(x => Generate(x)).ToArray();
            var name = expression.Function.ReturnType.IsUnit() ? "" : expression.Function.Name;

            var call = Builder.BuildCall(llvmFunction, args, name);
            return call;
        }

        LLVMValueRef Generate(VariableReference expression) {
            var variable = Things[expression.Variable];
            return Builder.BuildLoad(variable, $"tmp_var_{expression.Variable.Name}_");
        }

        LLVMValueRef Generate(BinaryOperation expression) {
            var left = Generate(expression.Left);
            var right = Generate(expression.Right);

            switch (expression.Operation) {
                case BinaryOperationKind.Plus:
                    return Builder.BuildAdd(left, right, "tmp_add");
                case BinaryOperationKind.Multiply:
                    return Builder.BuildMul(left, right, "tmp_mul");
                default:
                    break;
            }

            throw new CompilerErrorException("unknown binary op");
        }

        LLVMValueRef Generate(NumberLiteralExpression expression) {
            var type = Translate(expression.GetAstType());
            return LLVMValueRef.CreateConstInt(type, (ulong)expression.Number, true);
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
                    return Context.VoidType;
                case BuiltinTypeKind.Bool:
                    return Context.Int1Type;
                case BuiltinTypeKind.Number:
                    return Context.Int64Type;
                case BuiltinTypeKind.String:
                    break;
            }
            throw new CompilerErrorException("unknown builtin type");
        }
    }
}