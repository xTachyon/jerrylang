using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace JerryLang {
    enum LLVMAttribute {
        OptNone,
        NoInline
    }

    static class LLVMExt {
        public static LLVMMetadataRef CreateBasicType(this LLVMDIBuilderRef builder, string name, ulong sizeInBits, uint encoding, LLVMDIFlags flags) {
            using var marshaledName = new MarshaledString(name);
            var nameLength = (uint)marshaledName.Length;
            unsafe {
                return LLVM.DIBuilderCreateBasicType(builder, marshaledName.Value, (UIntPtr)marshaledName.Length, sizeInBits, encoding, flags);
            }
        }

        public static LLVMMetadataRef CreateAutoVariable(this LLVMDIBuilderRef builder, LLVMMetadataRef scope, string name, LLVMMetadataRef file,
                uint line, LLVMMetadataRef type, bool alwaysPreserve, LLVMDIFlags flags, uint alignInBits) {
            using var marshaledName = new MarshaledString(name);
            unsafe {
                return LLVM.DIBuilderCreateAutoVariable(builder, scope, marshaledName.Value, marshaledName.SizeTLength,
                                                        file, line, type, Convert.ToInt32(alwaysPreserve), flags,
                                                        alignInBits);
            }
        }

        public static LLVMMetadataRef CreateConstantValueExpression(this LLVMDIBuilderRef builder, long value) {
            unsafe {
                return LLVM.DIBuilderCreateConstantValueExpression(builder, value);
            }
        }

        public static LLVMMetadataRef CreateExpression(this LLVMDIBuilderRef builder, List<long> array) {
            unsafe {
                var memory = (long*)Marshal.AllocHGlobal(array.Count * 8);
                // leak :(
                for (var i = 0; i < array.Count; ++i) {
                    var current = array[i];
                    memory[i] = current;
                }

                return LLVM.DIBuilderCreateExpression(builder, memory, (UIntPtr)array.Count);
            }
        }

        public static LLVMValueRef InsertDeclareAtEnd(this LLVMDIBuilderRef builder, LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr,
                LLVMMetadataRef debugLoc, LLVMBasicBlockRef block) {
            unsafe {
                return LLVM.DIBuilderInsertDeclareAtEnd(builder, storage, varInfo, expr, debugLoc, block);
            }
        }

        public static void SetSubprogram(this LLVMValueRef function, LLVMMetadataRef metadata) {
            unsafe {
                LLVM.SetSubprogram(function, metadata); ;
            }
        }

        public static void AddModuleFlag(this LLVMModuleRef module, LLVMModuleFlagBehavior behavior, string key, LLVMMetadataRef value) {
            using var marshaledKey = new MarshaledString(key);
            unsafe {
                LLVM.AddModuleFlag(module, behavior, marshaledKey.Value, marshaledKey.SizeTLength, value);
            }
        }

        public static void SetTarget(this LLVMModuleRef module, string target) {
            using var marshaledTarget = new MarshaledString(target);
            unsafe {
                LLVM.SetTarget(module, marshaledTarget.Value);
            }
        }

        public static LLVMMetadataRef ValueAsMetadata(this LLVMValueRef value) {
            unsafe {
                return LLVM.ValueAsMetadata(value);
            }
        }

        public static LLVMAttributeRef CreateEnumAttribute(this LLVMContextRef context, uint kind) {
            unsafe {
                return LLVM.CreateEnumAttribute(context, kind, 0);
            }
        }

        public static void AddAttributeAtIndex(this LLVMValueRef function, uint index, LLVMAttributeRef attribute) {
            unsafe {
                LLVM.AddAttributeAtIndex(function, index, attribute);
            }
        }

        public static uint LookupAttribute(string name) {
            using var marshaledName = new MarshaledString(name);
            unsafe {
                return LLVM.GetEnumAttributeKindForName(marshaledName.Value, marshaledName.SizeTLength);
            }
        }

        public static LLVMValueRef BuildMemSet(this LLVMBuilderRef builder, LLVMValueRef pointer, LLVMValueRef value, LLVMValueRef length, uint align) {
            unsafe {
                return LLVM.BuildMemSet(builder, pointer, value, length, align);
            }
        }
    }

    internal unsafe struct MarshaledString : IDisposable {
        public MarshaledString(ReadOnlySpan<char> input) {
            if (input.IsEmpty) {
                var value = Marshal.AllocHGlobal(1);
                Marshal.WriteByte(value, 0, 0);

                Length = 0;
                Value = (sbyte*)value;
            } else {
                var valueBytes = Encoding.UTF8.GetBytes(input.ToString());
                var length = valueBytes.Length;
                var value = Marshal.AllocHGlobal(length + 1);
                Marshal.Copy(valueBytes, 0, value, length);
                Marshal.WriteByte(value, length, 0);

                Length = length;
                Value = (sbyte*)value;
            }
        }

        public int Length { get; private set; }

        public UIntPtr SizeTLength => (UIntPtr)Length; 

        public sbyte* Value { get; private set; }

        public void Dispose() {
            if (Value != null) {
                Marshal.FreeHGlobal((IntPtr)Value);
                Value = null;
                Length = 0;
            }
        }

        public static implicit operator sbyte*(in MarshaledString value) {
            return value.Value;
        }

        public override string ToString() {
            var span = new ReadOnlySpan<byte>(Value, Length);
            return Encoding.UTF8.GetString(span);
        }
    }
}