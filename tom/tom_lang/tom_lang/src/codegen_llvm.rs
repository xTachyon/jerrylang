use std::ffi::{CString};
use crate::ast::{Ast, BuiltinTy, Expr, ExprKind, Func, Item, Local, Stmt, Ty, TyId};
use llvm_sys::core::{LLVMAddFunction, LLVMAppendBasicBlockInContext, LLVMBuildAlloca, LLVMBuildStore, LLVMConstInt, LLVMContextCreate, LLVMContextDispose, LLVMCreateBuilderInContext, LLVMDisposeBuilder, LLVMDisposeModule, LLVMFunctionType, LLVMGetParam, LLVMInt64TypeInContext, LLVMInt8TypeInContext, LLVMModuleCreateWithNameInContext, LLVMPointerType, LLVMPositionBuilderAtEnd, LLVMPrintModuleToString, LLVMSetValueName2, LLVMVoidTypeInContext};
use llvm_sys::prelude::{LLVMBasicBlockRef, LLVMBuilderRef, LLVMContextRef, LLVMModuleRef, LLVMTypeRef, LLVMValueRef};

// Safety? We don't do that here.

macro_rules! cstring {
    ($e:expr) => { CString::new(&*$e).unwrap() };
}

struct Builder {
    ctx: LLVMContextRef,
    builder: LLVMBuilderRef,
}

impl Drop for Builder {
    fn drop(&mut self) {
        unsafe {
            LLVMDisposeBuilder(self.builder);
        }
    }
}

impl Builder {
    unsafe fn new(ctx: LLVMContextRef) -> Builder {
        let builder = LLVMCreateBuilderInContext(ctx);
        Builder { ctx, builder }
    }

    unsafe fn fn_type(&mut self, return_ty: LLVMTypeRef, params: &[LLVMTypeRef]) -> LLVMTypeRef {
        LLVMFunctionType(return_ty, params.as_ptr() as *mut _, params.len() as u32, false as i32)
    }

    unsafe fn set(&mut self, bb: LLVMBasicBlockRef) {
        LLVMPositionBuilderAtEnd(self.builder, bb);
    }

    unsafe fn bb(&mut self, func: LLVMValueRef) -> LLVMBasicBlockRef {
        LLVMAppendBasicBlockInContext(self.ctx, func, b"\0".as_ptr().cast())
    }

    unsafe fn alloca(&mut self, ty: LLVMTypeRef, name: &str) -> LLVMValueRef {
        let name = cstring!(name);
        LLVMBuildAlloca(self.builder, ty, name.as_ptr())
    }

    unsafe fn store(&mut self, value: LLVMValueRef, ptr: LLVMValueRef) -> LLVMValueRef {
        LLVMBuildStore(self.builder, value, ptr)
    }
}

pub struct Gen<'a> {
    ast: &'a Ast,

    ctx: LLVMContextRef,
    module: LLVMModuleRef,
    builder: Builder,

    ty_void: LLVMTypeRef,
    ty_i8: LLVMTypeRef,
    ty_i64: LLVMTypeRef,
}

impl<'a> Gen<'a> {
    pub fn run(ast: &Ast) {
        unsafe {
            let ctx = LLVMContextCreate();
            let module = LLVMModuleCreateWithNameInContext(b"jerry\0".as_ptr().cast(), ctx);
            let builder = Builder::new(ctx);

            let ty_void = LLVMVoidTypeInContext(ctx);
            let ty_i8 = LLVMInt8TypeInContext(ctx);
            let ty_i64 = LLVMInt64TypeInContext(ctx);

            let mut gen = Gen { ast, ctx, module, builder, ty_void, ty_i8, ty_i64 };
            for i in &ast.items {
                gen.gen(&i);
            }

            let str_raw = LLVMPrintModuleToString(module); // todo: leak eh
            let c_string = CString::from_raw(str_raw);
            let str = c_string.to_str().unwrap();
            std::fs::write("jerry.ll", str).unwrap();
        }
    }

    unsafe fn gen(&mut self, item: &Item) {
        match item {
            Item::Func(func) => self.gen_func(func)
        }
    }

    unsafe fn gen_func(&mut self, func: &Func) {
        let mut args = Vec::new();
        for (_, ty) in func.args.iter() {
            let ty = self.translate_ty(*ty);
            args.push(ty);
        }
        let ty = self.builder.fn_type(self.ty_void, &args);
        let name = cstring!(func.name);
        let l_func = LLVMAddFunction(self.module, name.as_ptr(), ty);

        for (index, i) in func.args.iter().enumerate() {
            let arg = LLVMGetParam(l_func, index as u32);
            LLVMSetValueName2(arg, i.0.as_ptr().cast(), i.0.len());
        }

        if let Some(stmts) = &func.stmts {
            let entry = self.builder.bb(l_func);
            self.builder.set(entry);

            for i in stmts {
                self.gen_stmt(i);
            }
        }
    }

    unsafe fn gen_expr(&mut self, expr: &Expr) -> LLVMValueRef {
        let ty = self.translate_ty(expr.ty);
        match expr.kind {
            ExprKind::NumberLit(x) => LLVMConstInt(ty, x as u64, 0)
        }
    }

    unsafe fn gen_stmt(&mut self, stmt: &Stmt) {
        match stmt { Stmt::Local(local) => self.gen_local(local) }
    }

    unsafe fn gen_local(&mut self, local: &Local) {
        let l_ty = self.translate_ty(local.init.ty);
        let alloca = self.builder.alloca(l_ty, &local.name);

        let init = self.gen_expr(&local.init);
        self.builder.store(init, alloca);
    }

    unsafe fn translate_ty(&mut self, ty: TyId) -> LLVMTypeRef {
        match self.ast.get(ty) {
            Ty::Builtin(builtin) => self.translate_builtin(builtin)
        }
    }

    unsafe fn translate_builtin(&mut self, builtin: &BuiltinTy) -> LLVMTypeRef {
        match builtin {
            BuiltinTy::I64 => self.ty_i64,
            BuiltinTy::Str => LLVMPointerType(self.ty_i8, 0)
        }
    }
}

impl<'a> Drop for Gen<'a> {
    fn drop(&mut self) {
        unsafe {
            LLVMDisposeModule(self.module);
            LLVMContextDispose(self.ctx);
        }
    }
}