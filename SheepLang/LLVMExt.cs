using LLVMSharp.Interop;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace JerryLang {
    static class LLVMExt {
        public static LLVMMetadataRef CreateBasicType(this LLVMDIBuilderRef builder, string name, ulong sizeInBits, uint encoding, LLVMDIFlags flags) {
            using var marshaledName = new MarshaledString(name);
            var nameLength = (uint)marshaledName.Length;
            unsafe {
                return LLVM.DIBuilderCreateBasicType(builder, marshaledName.Value, (UIntPtr)marshaledName.Length, sizeInBits, encoding, flags);
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