using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JerryLang {
    class DebugInfoGenerator : IDisposable {
        private LLVMContextRef Context { get; set; }
        public LLVMModuleRef Module { get; }
        private LLVMDIBuilderRef DiBuilder { get; }
        private LLVMMetadataRef CurrentFunction { get; set; }
        public LLVMBasicBlockRef CurrentBasicBlock { get; set; }
        private CodeGenerator CodeGenerator { get; }

        public DebugInfoGenerator(LLVMModuleRef module, CodeGenerator codeGenerator) {
            Context = module.Context;
            Module = module;
            DiBuilder = module.CreateDIBuilder();
            CodeGenerator = codeGenerator;

            AddFlagsToDiBuilder();
        }

        private void AddFlagsToDiBuilder() {
            var diFile = DiBuilder.CreateFile("file", @"directory");
            var lang = (LLVMDWARFSourceLanguage)new Random().Next(0, (int)LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageFortran08 - 1);
            DiBuilder.CreateCompileUnit(lang, diFile, "jerryc", 0, "", 0, "",
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

        public LLVMMetadataRef Translate(BuiltinType type) {
            const uint DW_ATE_boolean = 0x02;
            const uint DW_ATE_signed = 0x05;

            switch (type.Kind) {
                case BuiltinTypeKind.Unit:
                    return new LLVMMetadataRef();
                case BuiltinTypeKind.Bool:
                    return DiBuilder.CreateBasicType("bool", 8, DW_ATE_boolean, LLVMDIFlags.LLVMDIFlagZero);
                case BuiltinTypeKind.Number:
                    return DiBuilder.CreateBasicType("number", 64, DW_ATE_signed, LLVMDIFlags.LLVMDIFlagZero);
                case BuiltinTypeKind.String:
                    var pointee = DiBuilder.CreateBasicType("ch", 8, DW_ATE_signed, LLVMDIFlags.LLVMDIFlagZero);
                    return DiBuilder.CreatePointerType(pointee, 0, 0, 0, "ch*");
            }

            throw new CompilerErrorException("unknown builtin type");
        }

        public LLVMMetadataRef Translate(StructType type) {
            //return DiBuilder.CreateBasicType("bool", 1, 0x02, LLVMDIFlags.LLVMDIFlagZero);

            var location = type.Item.SourceLocation;
            var file = Translate(location.File);

            var elements = new List<LLVMMetadataRef>();
            ulong offset = 0;
            foreach (var i in type.Fields) {
                var fieldType = CodeGenerator.Translate(i.Type);
                uint sizeInBits = 8;
                uint alignInBits = 0;

                var el = DiBuilder.CreateMemberType(file, i.Name, file, (uint)location.Line, sizeInBits, alignInBits,
                    offset, LLVMDIFlags.LLVMDIFlagZero, Translate(i.Type));
                elements.Add(el);

                offset += sizeInBits;
            }

            return DiBuilder.CreateStructType(file, type.Name, file, (uint)location.Line, 0, 0, LLVMDIFlags.LLVMDIFlagZero,
                new LLVMMetadataRef(), elements, 0, new LLVMMetadataRef(), type.Name);
        }

        public LLVMMetadataRef Translate(AstType type) {
            if (type is BuiltinType builtin) {
                return Translate(builtin);
            } else if (type is StructType @struct) {
                return Translate(@struct);
            }

            throw new CompilerErrorException("unknown type");
        }

        private LLVMMetadataRef[] Translate(Function function) {
            var list = new List<LLVMMetadataRef>();

            var returnType = Translate(function.ReturnType);
            var args = function.Arguments.Select(x => Translate(x.Item2)).ToList();

            list.Add(returnType);
            list.AddRange(args);

            return list.ToArray();
        }

        private LLVMMetadataRef Translate(SourceFile file) {
            return DiBuilder.CreateFile(Path.GetFileName(file.Path), Path.GetDirectoryName(file.Path));
        }

        public void Generate(Function function, LLVMValueRef llvmFunction) {
            var sourceLocation = function.SourceLocation;

            var file = Translate(function.SourceLocation.File);
            var type = Translate(function);
            var subroutine = DiBuilder.CreateSubroutineType(file, type, LLVMDIFlags.LLVMDIFlagPrototyped);
            var isDefinition = Convert.ToInt32(function.Block != null);

            var diFunction = DiBuilder.CreateFunction(file, function.Name, function.Name, file,
                                                      (uint)sourceLocation.Line, subroutine, 0, isDefinition,
                                                      (uint)sourceLocation.Line, LLVMDIFlags.LLVMDIFlagPrototyped, 0);
            CurrentFunction = diFunction;
            llvmFunction.SetSubprogram(diFunction);
        }

        public void Generate(VariableDeclaration declaration, LLVMValueRef alloca) {
            var sourceLocation = declaration.Location;

            var file = Translate(declaration.Location.File);
            var type = Translate(declaration.Variable.Type);
            var location = Context.CreateDebugLocation((uint)sourceLocation.Line, (uint)sourceLocation.Column, CurrentFunction, new LLVMMetadataRef());

            var metadata = DiBuilder.CreateAutoVariable(CurrentFunction, declaration.Variable.Name, file,
                                                        (uint)sourceLocation.Line, type, false,
                                                        LLVMDIFlags.LLVMDIFlagZero, 0);
            var expr = DiBuilder.CreateExpression(new List<long>());

            DiBuilder.InsertDeclareAtEnd(alloca, metadata, expr, location, CurrentBasicBlock);
        }

        public void Generate(Statement statement, LLVMValueRef value) {
            Generate(statement.Location, value);
        }
        
        public void Generate(SourceLocation sourceLocation, LLVMValueRef value) {
            var location = CreateDebugLocation(sourceLocation);
            value.SetMetadata(0, Context.MetadataAsValue(location));
        }

        public void Dispose() {
            DiBuilder.DIBuilderFinalize();
            Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        }

        private LLVMMetadataRef CreateDebugLocation(SourceLocation location) {
            return Context.CreateDebugLocation((uint)location.Line, 0, CurrentFunction, new LLVMMetadataRef());
        }
    }
}