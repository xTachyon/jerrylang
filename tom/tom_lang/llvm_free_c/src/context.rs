#![allow(non_snake_case)]

use std::ffi::{c_char, c_int, c_uint};

use llvm_free::{Builder, Context, Module};

use crate::wrap::{unwrap_bool, unwrap_s, unwrap_slice, wrap_ty};

pub type LLVMContextRef = *const Context;
pub type LLVMBuilderRef = *const Builder;
pub type LLVMModuleRef = *const Module;
pub type LLVMTypeRef = *const ();
pub type LLVMBool = c_int;

#[unsafe(no_mangle)]
pub extern "C" fn LLVMContextCreate() -> LLVMContextRef {
    let b = Box::new(Context::new());
    let p = Box::into_raw(b);
    p
}

#[unsafe(no_mangle)]
pub extern "C" fn LLVMCreateBuilderInContext(ctx: LLVMContextRef) -> LLVMBuilderRef {
    let ctx = unsafe { &*ctx };
    let builder = Box::new(Builder::new(ctx));
    Box::into_raw(builder)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMModuleCreateWithNameInContext(
    ModuleID: *const c_char,
    C: LLVMContextRef,
) -> LLVMModuleRef {
    let ctx = unsafe { &*C };
    let module_id = unsafe { unwrap_s(ModuleID) };
    let module = Box::new(Module::new(ctx, module_id));
    Box::into_raw(module)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMGetDataLayoutStr(M: LLVMModuleRef) -> *const c_char {
    c"data_layout".as_ptr()
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMDoubleTypeInContext(C: LLVMContextRef) -> LLVMTypeRef {
    let ctx = unsafe { &*C };
    wrap_ty(ctx.ty_f64)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMFunctionType(
    ReturnType: LLVMTypeRef,
    ParamTypes: *mut LLVMTypeRef,
    ParamCount: c_uint,
    IsVarArg: LLVMBool,
) -> LLVMTypeRef {
    assert!(!unwrap_bool(IsVarArg));
    let params = unwrap_slice(ParamTypes, ParamCount);
    todo!()
}
