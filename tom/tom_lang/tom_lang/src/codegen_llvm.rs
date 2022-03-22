use crate::ast::{Ast, BuiltinTy, Expr, ExprKind, Func, Item, ItemId, Local, Stmt, Ty, TyId};
use llvm_sys::analysis::{LLVMVerifierFailureAction, LLVMVerifyModule};
use llvm_sys::core::{
    LLVMAddFunction, LLVMAppendBasicBlockInContext, LLVMBuildAlloca, LLVMBuildCall,
    LLVMBuildGlobalStringPtr, LLVMBuildRet, LLVMBuildRetVoid, LLVMBuildStore, LLVMConstInt,
    LLVMContextCreate, LLVMContextDispose, LLVMCreateBuilderInContext, LLVMDisposeBuilder,
    LLVMDisposeModule, LLVMFunctionType, LLVMGetParam, LLVMInt1TypeInContext,
    LLVMInt64TypeInContext, LLVMInt8TypeInContext, LLVMModuleCreateWithNameInContext,
    LLVMPointerType, LLVMPositionBuilderAtEnd, LLVMPrintModuleToString, LLVMSetTarget,
    LLVMSetValueName2, LLVMVoidTypeInContext,
};
use llvm_sys::prelude::{
    LLVMBasicBlockRef, LLVMBuilderRef, LLVMContextRef, LLVMModuleRef, LLVMTypeRef, LLVMValueRef,
};
use llvm_sys::target::{
    LLVMSetModuleDataLayout, LLVM_InitializeAllAsmParsers, LLVM_InitializeAllAsmPrinters,
    LLVM_InitializeAllDisassemblers, LLVM_InitializeAllTargetInfos, LLVM_InitializeAllTargetMCs,
    LLVM_InitializeAllTargets,
};
use llvm_sys::target_machine::{
    LLVMCodeGenOptLevel, LLVMCodeModel, LLVMCreateTargetDataLayout, LLVMCreateTargetMachine,
    LLVMGetDefaultTargetTriple, LLVMGetTargetFromTriple, LLVMRelocMode,
};
use std::collections::HashMap;
use std::ffi::CString;
use std::ptr::null_mut;

// Safety? We don't do that here.

macro_rules! cstring {
    ($e:expr) => {
        CString::new(&*$e).unwrap()
    };
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
        LLVMFunctionType(
            return_ty,
            params.as_ptr() as *mut _,
            params.len() as u32,
            false as i32,
        )
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

    unsafe fn call(&mut self, func: LLVMValueRef, args: &[LLVMValueRef]) -> LLVMValueRef {
        LLVMBuildCall(
            self.builder,
            func,
            args.as_ptr() as *mut _,
            args.len() as u32,
            b"\0".as_ptr().cast(),
        )
    }

    unsafe fn ret_void(&mut self) -> LLVMValueRef {
        LLVMBuildRetVoid(self.builder)
    }

    unsafe fn ret(&mut self, value: LLVMValueRef) -> LLVMValueRef {
        LLVMBuildRet(self.builder, value)
    }
}

pub struct Gen<'a> {
    ast: &'a Ast,
    symbols: HashMap<ItemId, LLVMValueRef>,

    ctx: LLVMContextRef,
    module: LLVMModuleRef,
    builder: Builder,

    ty_void: LLVMTypeRef,
    ty_i1: LLVMTypeRef,
    ty_i8: LLVMTypeRef,
    ty_i64: LLVMTypeRef,
}

impl<'a> Gen<'a> {
    pub fn run(ast: &Ast) {
        unsafe {
            LLVM_InitializeAllTargets();
            LLVM_InitializeAllTargetInfos();
            LLVM_InitializeAllAsmParsers();
            LLVM_InitializeAllDisassemblers();
            LLVM_InitializeAllTargetMCs();
            LLVM_InitializeAllAsmPrinters();

            let ctx = LLVMContextCreate();
            let module = LLVMModuleCreateWithNameInContext(b"jerry\0".as_ptr().cast(), ctx);
            let builder = Builder::new(ctx);

            let default_triple = LLVMGetDefaultTargetTriple();
            let mut target = null_mut();
            LLVMGetTargetFromTriple(default_triple, &mut target, null_mut());
            let target_machine = LLVMCreateTargetMachine(
                target,
                default_triple,
                std::ptr::null(),
                std::ptr::null(),
                LLVMCodeGenOptLevel::LLVMCodeGenLevelDefault,
                LLVMRelocMode::LLVMRelocPIC,
                LLVMCodeModel::LLVMCodeModelDefault,
            );

            let target_data = LLVMCreateTargetDataLayout(target_machine);
            LLVMSetModuleDataLayout(module, target_data);
            LLVMSetTarget(module, default_triple);

            let ty_void = LLVMVoidTypeInContext(ctx);
            let ty_i1 = LLVMInt1TypeInContext(ctx);
            let ty_i8 = LLVMInt8TypeInContext(ctx);
            let ty_i64 = LLVMInt64TypeInContext(ctx);

            let mut gen = Gen {
                ast,
                symbols: HashMap::new(),
                ctx,
                module,
                builder,
                ty_void,
                ty_i1,
                ty_i8,
                ty_i64,
            };
            for (index, item) in ast.items.iter().enumerate() {
                gen.gen(&item, ItemId(index as u32));
            }

            let str_raw = LLVMPrintModuleToString(module); // todo: leak eh
            let c_string = CString::from_raw(str_raw);
            let str = c_string.to_str().unwrap();
            std::fs::write("jerry.ll", str).unwrap();

            LLVMVerifyModule(
                module,
                LLVMVerifierFailureAction::LLVMAbortProcessAction,
                std::ptr::null_mut(),
            );
        }
    }

    unsafe fn gen(&mut self, item: &Item, id: ItemId) {
        match item {
            Item::Func(func) => self.gen_func(func, id),
        }
    }

    unsafe fn gen_func(&mut self, func: &Func, id: ItemId) {
        let mut args = Vec::new();
        for (_, ty) in func.args.iter() {
            let ty = self.translate_ty(*ty);
            args.push(ty);
        }
        let ret_ty = self.translate_ty(func.return_ty);
        let ty = self.builder.fn_type(ret_ty, &args);
        let name = cstring!(func.name);
        let l_func = LLVMAddFunction(self.module, name.as_ptr(), ty);

        for (index, i) in func.args.iter().enumerate() {
            let arg = LLVMGetParam(l_func, index as u32);
            LLVMSetValueName2(arg, i.0.as_ptr().cast(), i.0.len());
        }

        self.symbols.insert(id, l_func);

        if let Some(stmts) = &func.stmts {
            let entry = self.builder.bb(l_func);
            self.builder.set(entry);

            for i in stmts {
                self.gen_stmt(i);
            }
        }

        if func.return_ty == Ast::TY_VOID {
            self.builder.ret_void();
        }
    }

    unsafe fn gen_expr(&mut self, expr: &Expr) -> LLVMValueRef {
        use ExprKind::*;
        let ty = self.translate_ty(expr.ty);
        match &expr.kind {
            BoolLit(x) => LLVMConstInt(ty, *x as u64, 0),
            NumberLit(x) => LLVMConstInt(ty, *x as u64, 0),
            StringLit(x) => {
                let str = cstring!(x.as_str());
                LLVMBuildGlobalStringPtr(self.builder.builder, str.as_ptr(), b"\0".as_ptr().cast())
            }
            FuncCall(x) => {
                let mut args = Vec::new();
                for i in x.args.iter() {
                    args.push(self.gen_expr(i));
                }
                let func = *self.symbols.get(&x.func).unwrap();
                self.builder.call(func, &args)
            }
        }
    }

    unsafe fn gen_stmt(&mut self, stmt: &Stmt) {
        use Stmt::*;
        match stmt {
            Expr(expr) => {
                self.gen_expr(expr);
            }
            Local(local) => self.gen_local(local),
            Return(ret) => {
                if let Some(value) = &ret.value {
                    let value = self.gen_expr(value);
                    self.builder.ret(value);
                }
            }
        }
    }

    unsafe fn gen_local(&mut self, local: &Local) {
        let l_ty = self.translate_ty(local.init.ty);
        let alloca = self.builder.alloca(l_ty, &local.name);

        let init = self.gen_expr(&local.init);
        self.builder.store(init, alloca);
    }

    unsafe fn translate_ty(&mut self, ty: TyId) -> LLVMTypeRef {
        match self.ast.ty(ty) {
            Ty::Builtin(builtin) => self.translate_builtin(builtin),
        }
    }

    unsafe fn translate_builtin(&mut self, builtin: &BuiltinTy) -> LLVMTypeRef {
        match builtin {
            BuiltinTy::Bool => self.ty_i1,
            BuiltinTy::I64 => self.ty_i64,
            BuiltinTy::Str => LLVMPointerType(self.ty_i8, 0),
            BuiltinTy::Void => self.ty_void,
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
