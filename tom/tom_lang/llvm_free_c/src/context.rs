#![allow(non_snake_case)]

use std::{
    cell::RefCell,
    collections::HashMap,
    ffi::{c_char, c_int, c_uint},
    sync::atomic::AtomicPtr,
};

use bumpalo::Bump;
use llvm_free::{Builder, Context, Module, TyKey};

use crate::wrap::{unwrap_bool, unwrap_s, unwrap_slice, unwrap_ty, wrap_value};

pub type LLVMContextRef = *const RawContext;
pub type LLVMBuilderRef = *const Builder;
pub type LLVMModuleRef = *const Module;
pub type LLVMTypeRef = *const RawType;
pub type LLVMValueRef = *const ();
pub type LLVMBool = c_int;

pub struct RawContext {
    ctx: Context,
    types_map: RefCell<HashMap<TyKey, *const RawType>>,
    bump: Bump,
}

impl RawContext {
    fn new() -> RawContext {
        RawContext {
            ctx: Context::new(),
            types_map: RefCell::default(),
            bump: Bump::new(),
        }
    }

    fn get_type(&self, ty: TyKey) -> *const RawType {
        let mut types = self.types_map.borrow_mut();
        let v = types
            .entry(ty)
            .or_insert_with(|| self.bump.alloc(RawType { ty, ctx: self }));
        *v
    }
}

pub struct RawType {
    pub(crate) ty: TyKey,
    ctx: *const RawContext,
}

static GLOBAL_CTX: AtomicPtr<RawContext> = AtomicPtr::new(std::ptr::null_mut());

#[unsafe(no_mangle)]
pub extern "C" fn LLVMContextCreate() -> LLVMContextRef {
    let b = Box::new(RawContext::new());
    let p = Box::into_raw(b);

    let old = GLOBAL_CTX.swap(p, std::sync::atomic::Ordering::SeqCst);
    assert!(old.is_null());
    p
}

#[unsafe(no_mangle)]
pub extern "C" fn LLVMCreateBuilderInContext(ctx: LLVMContextRef) -> LLVMBuilderRef {
    let ctx = unsafe { &*ctx };
    let builder = Box::new(Builder::new(&ctx.ctx));
    Box::into_raw(builder)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMModuleCreateWithNameInContext(
    ModuleID: *const c_char,
    C: LLVMContextRef,
) -> LLVMModuleRef {
    let ctx = unsafe { &*C };
    let module_id = unsafe { unwrap_s(ModuleID) };
    let module = Box::new(Module::new(&ctx.ctx, module_id));
    Box::into_raw(module)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMGetDataLayoutStr(M: LLVMModuleRef) -> *const c_char {
    c"data_layout".as_ptr()
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMDoubleTypeInContext(C: LLVMContextRef) -> LLVMTypeRef {
    let ctx = unsafe { &*C };
    ctx.get_type(ctx.ctx.ty_f64)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMFunctionType(
    ReturnType: LLVMTypeRef,
    ParamTypes: *mut LLVMTypeRef,
    ParamCount: c_uint,
    IsVarArg: LLVMBool,
) -> LLVMTypeRef {
    assert!(!unwrap_bool(IsVarArg));
    let ret_ty = unsafe { &*ReturnType };
    let params = unwrap_slice(ParamTypes, ParamCount)
        .iter()
        .map(|x| unsafe { &**x }.ty)
        .collect();

    let ctx = unsafe { &*ret_ty.ctx };

    let ty = ctx.ctx.fn_ty(ret_ty.ty, params);
    ctx.get_type(ty)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMAddFunction(
    M: LLVMModuleRef,
    Name: *const c_char,
    FunctionTy: LLVMTypeRef,
) -> LLVMValueRef {
    let module = unsafe { &*M };
    let name = unsafe { unwrap_s(Name) };
    let fn_ty = unsafe { unwrap_ty(FunctionTy) };

    let v = module.add_fn(name, fn_ty);

    wrap_value(v)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn LLVMIsAFunction(Val: LLVMValueRef) -> LLVMValueRef {
    todo!()
}
