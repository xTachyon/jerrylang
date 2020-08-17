using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JerryLang {
    class CodeGenerator : IDisposable {
        private TranslationUnit Unit { get; }
        private LLVMContextRef Context { get; set; }
        public LLVMModuleRef Module { get; }
        private LLVMBuilderRef Builder { get; set; }
        private Dictionary<AstElement, LLVMValueRef> Things { get; }
        private DebugInfoGenerator DebugInfoGenerator { get; }

        public CodeGenerator(TranslationUnit tu, string name) {
            Unit = tu;
            Context = LLVMContextRef.Create();
            Module = Context.CreateModuleWithName(name);
            Builder = Context.CreateBuilder();
            DebugInfoGenerator = new DebugInfoGenerator(Module);
            Things = new Dictionary<AstElement, LLVMValueRef>();

            Module.SetTarget("x86_64-unknown-windows-msvc19.27.29110");
        }

        public LLVMModuleRef Generate() {
            foreach (var i in Unit.Functions) {
                Generate(i);
            }

            return Module;
        }

        private void AddAttribute(LLVMValueRef function, LLVMAttribute attribute) {
            var name = attribute switch
            {
                LLVMAttribute.OptNone => "optnone",
                LLVMAttribute.NoInline => "noinline",
                _ => throw new CompilerErrorException("unknown attribute"),
            };
            var attrValue = LLVMExt.LookupAttribute(name);
            var toAdd = Context.CreateEnumAttribute(attrValue);
            function.AddAttributeAtIndex(unchecked((uint)-1), toAdd);
        }

        void Generate(Function function) {
            var returnType = Translate(function.ReturnType);
            var argsTypes = function.Arguments.Select(x => Translate(x.Item2)).ToArray();
            var functionType = LLVMTypeRef.CreateFunction(returnType, argsTypes, false);
            var llvmFunction = Module.AddFunction(function.Name, functionType);

            Things[function] = llvmFunction;
            if (function.Block == null) {
                return;
            }
            DebugInfoGenerator.Generate(function, llvmFunction);

            LLVMBasicBlockRef entry = Context.AppendBasicBlock(llvmFunction, "entry");
            DebugInfoGenerator.CurrentBasicBlock = entry;
            Builder = Context.CreateBuilder();
            Builder.PositionAtEnd(entry);

            Generate(function.Block);

            AddAttribute(llvmFunction, LLVMAttribute.NoInline);
            AddAttribute(llvmFunction, LLVMAttribute.OptNone);

            if (function.ReturnType.IsUnit()) {
                Builder.BuildRetVoid();
            }
        }

        void Generate(Statement statement) {
            if (statement is VariableDeclaration declaration) {
                Generate(declaration);
                return;
            } else
            if (statement is Assignment assignment) {
                Generate(assignment);
                return;
            } else if (statement is Expression expression) {
                Generate(expression);
                return;
            }

            throw new CompilerErrorException("unknown stmt");
        }

        void Generate(VariableDeclaration declaration) {
            var llvmType = Translate(declaration.Variable.Type);
            var alloca = Builder.BuildAlloca(llvmType, declaration.Variable.Name);
            Things[declaration.Variable] = alloca;

            DebugInfoGenerator.Generate(declaration, alloca);
         
            Generate(declaration.Assignment);
        }

        void Generate(Assignment assignment) {
            var alloca = Things[assignment.Variable];
            var expression = Generate(assignment.Expression);
            var store = Builder.BuildStore(expression, alloca);

            DebugInfoGenerator.Generate(assignment, store);
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

            DebugInfoGenerator.Generate(expression, call);

            return call;
        }

        LLVMValueRef Generate(VariableReference expression) {
            var variable = Things[expression.Variable];
            var load = Builder.BuildLoad(variable, $"tmp_var_{expression.Variable.Name}_");
            DebugInfoGenerator.Generate(expression, load);
            return load;
        }

        LLVMValueRef Generate(BinaryOperation expression) {
            var left = Generate(expression.Left);
            var right = Generate(expression.Right);
            var value = expression.Operation switch
            {
                BinaryOperationKind.Plus => Builder.BuildAdd(left, right, "tmp_add"),
                BinaryOperationKind.Minus => Builder.BuildSub(left, right, "tmp_sub"),
                BinaryOperationKind.Multiply => Builder.BuildMul(left, right, "tmp_mul"),
                _ => throw new CompilerErrorException("unknown binary op"),
            };
            DebugInfoGenerator.Generate(expression, value);

            return value;
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

        public void Dispose() {
            DebugInfoGenerator.Dispose();
        }
    }
}