using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;

namespace JerryLang {
    class CodeGenerator {
        private TranslationUnit Unit { get; }
        public LLVMModuleRef Module { get; }
        private LLVMBuilderRef Builder { get; set; }
        private LLVMDIBuilderRef DiBuilder { get; set; }
        private Dictionary<Variable, LLVMValueRef> Variables { get; }
        private Dictionary<Function, LLVMValueRef> Functions { get; }

        [DllImport("libLLVM.dll", EntryPoint = "LLVMCreateDIBuilder", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LLVMCreateDIBuilderRaw(IntPtr module);
        private static LLVMDIBuilderRef LLVMCreateDIBuilder(LLVMModuleRef module) {
            var result = LLVMCreateDIBuilderRaw(module.Pointer);
            return new LLVMDIBuilderRef(result);
        }

        public CodeGenerator(TranslationUnit tu, string name) {
            Unit = tu;
            Module = LLVM.ModuleCreateWithName(name);
            DiBuilder = LLVMCreateDIBuilder(Module);
            Variables = new Dictionary<Variable, LLVMValueRef>();
            Functions = new Dictionary<Function, LLVMValueRef>();
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
            var argsTypes = function.Arguments.Select(x => Translate(x.Item2)).ToArray();
            var functionType = LLVM.FunctionType(returnType, argsTypes, false);
            var llvmFunction = LLVM.AddFunction(Module, function.Name, functionType);

            Functions[function] = llvmFunction;

            LLVM.DIBuilderCreateFile(DiBuilder, "a", "b");

            if (function.Block == null) {
                return;
            }

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
            var llvmFunction = Functions[expression.Function];
            var args = expression.Arguments.Select(x => Generate(x)).ToArray();
            var name = expression.Function.ReturnType.IsUnit() ? "" : expression.Function.Name;

            var call = LLVM.BuildCall(Builder, llvmFunction, args, name);
            return call;
        }

        LLVMValueRef Generate(VariableReference expression) {
            var variable = Variables[expression.Variable];
            return LLVM.BuildLoad(Builder, variable, $"tmp_var_{expression.Variable.Name}_");
        }

        LLVMValueRef Generate(BinaryOperation expression) {
            var left = Generate(expression.Left);
            var right = Generate(expression.Right);

            switch (expression.Operation) {
                case BinaryOperationKind.Plus:
                    return LLVM.BuildAdd(Builder, left, right, "tmp_add");
                case BinaryOperationKind.Multiply:
                    return LLVM.BuildMul(Builder, left, right, "tmp_mul");
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