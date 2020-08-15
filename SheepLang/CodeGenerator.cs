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
        private LLVMMetadataRef CurrentFunction { get; set; }
        private LLVMBasicBlockRef CurrentBasicBlock { get; set; }

        public CodeGenerator(TranslationUnit tu, string name) {
            Unit = tu;
            Context = LLVMContextRef.Create();
            Module = Context.CreateModuleWithName(name);
            Builder = Context.CreateBuilder();
            DiBuilder = Module.CreateDIBuilder();
            Things = new Dictionary<AstElement, LLVMValueRef>();

            AddFlagsToDiBuilder();

            Module.SetTarget("x86_64-unknown-windows-msvc19.27.29110");
        }

        private void AddFlagsToDiBuilder() {
            var diFile = DiBuilder.CreateFile("input.jerry", @"C:\Users\andre\source\repos\SheepLang\SheepLang\working");
            DiBuilder.CreateCompileUnit(LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC_plus_plus, diFile,
                                        "clang version 10.0.0 ", 0, "", 0, "split",
                                        LLVMDWARFEmissionKind.LLVMDWARFEmissionFull, 0, 0, 0);
            
            Module.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "CodeView", IntAsMetadata(1));
            Module.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Debug Info Version", IntAsMetadata(3));
            Module.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorError, "wchar_size", IntAsMetadata(2));
            Module.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "PIC Level", IntAsMetadata(2));
        }

        private LLVMMetadataRef IntAsMetadata(int value) {
            var one = LLVMValueRef.CreateConstInt(Context.Int32Type, (ulong)value);
            var oneMetadata = one.ValueAsMetadata();
            return oneMetadata;
        }

        public void Generate() {
            foreach (var i in Unit.Functions) {
                Generate(i);
            }
            DiBuilder.DIBuilderFinalize();
            Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        }

        LLVMMetadataRef TranslateMetadata(BuiltinType type) {
            const uint DW_ATE_boolean = 0x02;
            const uint DW_ATE_signed = 0x05;

            switch (type.Kind) {
                case BuiltinTypeKind.Unit:
                    return DiBuilder.CreateBasicType("unit", 1, DW_ATE_signed, LLVMDIFlags.LLVMDIFlagZero);
                case BuiltinTypeKind.Bool:
                    return DiBuilder.CreateBasicType("bool", 1, DW_ATE_boolean, LLVMDIFlags.LLVMDIFlagZero);
                case BuiltinTypeKind.Number:
                    return DiBuilder.CreateBasicType("number", 64, DW_ATE_signed, LLVMDIFlags.LLVMDIFlagZero);
                case BuiltinTypeKind.String:
                    break;
            }

            throw new CompilerErrorException("unknown builtin type");
        }

        LLVMMetadataRef TranslateMetadata(AstType type) {
            if (type is BuiltinType builtin) {
                return TranslateMetadata(builtin);
            }

            throw new CompilerErrorException("unknown type");
        }

        LLVMMetadataRef[] TranslateMetadataFunctionType(Function function) {
            var list = new List<LLVMMetadataRef>();

            var returnType = TranslateMetadata(function.ReturnType);
            var args = function.Arguments.Select(x => TranslateMetadata(x.Item2)).ToList();

            list.Add(returnType);
            list.AddRange(args);

            return list.ToArray();
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
            {
                var sourceLocation = function.SourceLocation;

                var diFile = DiBuilder.CreateFile("input.jerry", @"C:\Users\andre\source\repos\SheepLang\SheepLang\working");
                var diFunctionType = TranslateMetadataFunctionType(function);
                var subroutine = DiBuilder.CreateSubroutineType(diFile, diFunctionType, LLVMDIFlags.LLVMDIFlagPrototyped);
                var isDefinition = Convert.ToInt32(function.Block != null);

                var diFunction = DiBuilder.CreateFunction(diFile, function.Name, function.Name, diFile,
                                                          (uint)sourceLocation.Line, subroutine, 0, isDefinition,
                                                          (uint)sourceLocation.Line, LLVMDIFlags.LLVMDIFlagZero, 0);
                CurrentFunction = diFunction;

                llvmFunction.SetSubprogram(diFunction);
            }


            LLVMBasicBlockRef entry = Context.AppendBasicBlock(llvmFunction, "entry");
            CurrentBasicBlock = entry;
            Builder = Context.CreateBuilder();
            Builder.PositionAtEnd(entry);

            Generate(function.Block);

            if (function.ReturnType.IsUnit()) {
                Builder.BuildRetVoid();
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

            if (assignment.IsDeclaration) {
                var sourceLocation = assignment.SourceLocation;
                var diFile = DiBuilder.CreateFile("input.jerry", @"C:\Users\andre\source\repos\SheepLang\SheepLang\working");
                var diType = TranslateMetadata(assignment.Variable.Type);
                var diLocation = Context.CreateDebugLocation((uint)sourceLocation.Line, (uint)sourceLocation.Column, CurrentFunction, new LLVMMetadataRef());

                var metadata = DiBuilder.CreateAutoVariable(CurrentFunction, assignment.Variable.Name, diFile,
                                                            (uint)sourceLocation.Line, diType, false,
                                                            LLVMDIFlags.LLVMDIFlagZero, 0);
                var expr = DiBuilder.CreateExpression(new List<long>());

                DiBuilder.InsertDeclareAtEnd(alloca, metadata, expr, diLocation, CurrentBasicBlock);
            }

            var store = Builder.BuildStore(expression, alloca);
            if (!assignment.IsDeclaration) {
                var sourceLocation = assignment.SourceLocation;
                var metadata = Context.CreateDebugLocation((uint)sourceLocation.Line, (uint)sourceLocation.Column, CurrentFunction, new LLVMMetadataRef());
                store.SetMetadata(0, Context.MetadataAsValue(metadata));
            }
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

            {
                var sourceLocation = expression.SourceLocation;
                var metadata = Context.CreateDebugLocation((uint)sourceLocation.Line, (uint)sourceLocation.Column, CurrentFunction, new LLVMMetadataRef());
                call.SetMetadata(0, Context.MetadataAsValue(metadata));
            }

            return call;
        }

        LLVMValueRef Generate(VariableReference expression) {
            var variable = Things[expression.Variable];
            return Builder.BuildLoad(variable, $"tmp_var_{expression.Variable.Name}_");
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
            {
                var sourceLocation = expression.SourceLocation;
                var metadata = Context.CreateDebugLocation((uint)sourceLocation.Line, (uint)sourceLocation.Column, CurrentFunction, new LLVMMetadataRef());
                value.SetMetadata(0, Context.MetadataAsValue(metadata));
            }

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
    }
}