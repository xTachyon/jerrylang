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
            var module = Context.CreateModuleWithName(name);
            Module = module;
            Builder = Context.CreateBuilder();
            DebugInfoGenerator = new DebugInfoGenerator(Module, this);
            Things = new Dictionary<AstElement, LLVMValueRef>();

            module.Target = "x86_64-unknown-windows-msvc19.27.29110";
            module.DataLayout = "e-m:w-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128";
        }

        public LLVMModuleRef Generate() {
            foreach (var i in Unit.Items) {
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

            var iterator = function.GetElements().Where(x => x is VariableDeclaration).Select(x => x as VariableDeclaration);
            var variables = new List<(Variable, LLVMValueRef)>();
            foreach (var i in iterator) {
                var type = Translate(i.Variable.Type);
                var alloca = Builder.BuildAlloca(type);
                Things[i.Variable] = alloca;
                variables.Add((i.Variable, alloca));
            }
            var memsetValue = LLVMValueRef.CreateConstInt(Context.Int8Type, 0, false);
            foreach (var (variable, pointer) in variables) {
                var type = Translate(variable.Type);
                var sizeOfType = type.SizeOf;
                Builder.BuildMemSet(pointer, memsetValue, sizeOfType, 0);
            }

            Generate(function.Block);

            AddAttribute(llvmFunction, LLVMAttribute.NoInline);
            AddAttribute(llvmFunction, LLVMAttribute.OptNone);

            if (function.ReturnType.IsUnit()) {
                var ret = Builder.BuildRetVoid();
                DebugInfoGenerator.Generate(function.ClosedBrace, ret);
            }
        }

        void Generate(Item item) {
            if (item is Function function) {
                Generate(function);
                return;
            } else if (item is Struct @struct) {
                return;
            }

            throw new CompilerErrorException("unknown stmt");
        }

        void Generate(Statement statement) {
            if (statement is VariableDeclaration declaration) {
                Generate(declaration);
                return;
            } else if (statement is Assignment assignment) {
                Generate(assignment);
                return;
            } else if (statement is PointerAssignment pointerAssignment) {
                Generate(pointerAssignment);
                return;
            } else if (statement is Expression expression) {
                Generate(expression);
                return;
            }

            throw new CompilerErrorException("unknown stmt");
        }

        void Generate(VariableDeclaration declaration) {
            var alloca = Things[declaration.Variable];

            DebugInfoGenerator.Generate(declaration, alloca);

            Generate(declaration.Assignment);
        }

        void Generate(Assignment assignment) {
            var alloca = Things[assignment.Variable];
            var expression = Generate(assignment.Expression);
            var store = Builder.BuildStore(expression, alloca);

            DebugInfoGenerator.Generate(assignment, store);
        }

        void Generate(PointerAssignment assignment) {
            var alloca = Things[assignment.Variable];
            var expression = Generate(assignment.Expression);
            var load = Builder.BuildLoad(alloca);
            var store = Builder.BuildStore(expression, load);

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
            } else if (expression is StringLiteralExpression str) {
                return Generate(str);
            } else if (expression is BoolLiteralExpression boolean) {
                return Generate(boolean);
            } else if (expression is BinaryOperation binary) {
                return Generate(binary);
            } else if (expression is VariableReference reference) {
                return Generate(reference);
            } else if (expression is FunctionCall call) {
                return Generate(call);
            } else if (expression is StructInit init) {
                return Generate(init);
            } else if (expression is UnaryOperation unary) {
                return Generate(unary);
            }

            throw new CompilerErrorException("unknown expression");
        }

        LLVMValueRef Generate(UnaryOperation unary) {
            var variable = Things[unary.Variable];
            return variable;
        }

        LLVMValueRef Generate(StructInit init) {
            var last = Translate(init.Type).Undef;
            for (int i = 0; i < init.Expressions.Count; ++i) {
                var expression = Generate(init.Expressions[i]);
                last = Builder.BuildInsertValue(last, expression, (uint)i);
            }
            return last;
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
            var load = Builder.BuildLoad(variable, expression.Variable.Name);
            DebugInfoGenerator.Generate(expression, load);
            return load;
        }

        LLVMValueRef Generate(BinaryOperation expression) {
            var left = Generate(expression.Left);
            var right = Generate(expression.Right);
            var value = expression.Operation switch
            {
                BinaryOperationKind.Plus => Builder.BuildAdd(left, right, ""),
                BinaryOperationKind.Minus => Builder.BuildSub(left, right, ""),
                BinaryOperationKind.Multiply => Builder.BuildMul(left, right, ""),
                _ => throw new CompilerErrorException("unknown binary op"),
            };
            DebugInfoGenerator.Generate(expression, value);

            return value;
        }

        LLVMValueRef Generate(NumberLiteralExpression expression) {
            var type = Translate(expression.GetAstType());
            return LLVMValueRef.CreateConstInt(type, (ulong)expression.Value, true);
        }

        LLVMValueRef Generate(StringLiteralExpression expression) {
            var global = Builder.BuildGlobalString(expression.Value);
            var gep = Builder.BuildInBoundsGEP(global, new LLVMValueRef[] { LLVMValueRef.CreateConstInt(Context.Int32Type, 0), LLVMValueRef.CreateConstInt(Context.Int32Type, 0) });

            return gep;
        }

        LLVMValueRef Generate(BoolLiteralExpression expression) {
            var type = Translate(expression.GetAstType());
            return LLVMValueRef.CreateConstInt(type, Convert.ToUInt64(expression.Value), true);
        }

        public LLVMTypeRef Translate(AstType type) {
            if (type is BuiltinType builtin) {
                return Translate(builtin);
            } else if (type is StructType @struct) {
                return Translate(@struct);
            } else if (type is PointerType pointer) {
                return Translate(pointer);
            }
            throw new CompilerErrorException("unknown type");
        }

        private LLVMTypeRef Translate(PointerType pointer) {
            var pointee = Translate(pointer.Pointee);
            return LLVMTypeRef.CreatePointer(pointee, 0);
        }

        public LLVMTypeRef Translate(StructType struuct) {
            var types = struuct.GetFieldTypes().Select(x => Translate(x)).ToArray();
            return Context.GetStructType(types, false);
        }

        public LLVMTypeRef Translate(BuiltinType type) {
            switch (type.Kind) {
                case BuiltinTypeKind.Unit:
                    return Context.VoidType;
                case BuiltinTypeKind.Bool:
                    return Context.Int1Type;
                case BuiltinTypeKind.Number:
                    return Context.Int64Type;
                case BuiltinTypeKind.String:
                    return LLVMTypeRef.CreatePointer(Context.Int8Type, 0);
            }
            throw new CompilerErrorException("unknown builtin type");
        }

        public void Dispose() {
            DebugInfoGenerator.Dispose();
        }
    }
}