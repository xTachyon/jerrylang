#![allow(non_snake_case)]

use std::ffi::c_char;

use llvm_free::{Builder, Context, Module, Ty};

use crate::wrap::unwrap_s;

pub type LLVMContextRef = *const Context;
pub type LLVMBuilderRef = *const Builder;
pub type LLVMModuleRef = *const Module;
pub type LLVMTypeRef = *const Ty;

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
    todo!()
}
