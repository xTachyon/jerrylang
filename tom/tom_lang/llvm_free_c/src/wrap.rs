use std::ffi::{CStr, c_char};

use llvm_free::{Ty, TyKey, ValueKey};
use slotmap::{Key, KeyData};

use crate::context::{LLVMBool, LLVMTypeRef, LLVMValueRef};

pub unsafe fn unwrap_s<'a>(x: *const c_char) -> &'a str {
    assert!(!x.is_null());
    unsafe { CStr::from_ptr(x).to_str().unwrap() }
}

pub fn unwrap_bool(x: LLVMBool) -> bool {
    x != 0
}

pub fn unwrap_slice<'a, T, I: Into<u64>>(ptr: *const T, s: I) -> &'a [T] {
    let len = s.into() as usize;
    if len == 0 {
        &[]
    } else {
        unsafe { std::slice::from_raw_parts(ptr, len) }
    }
}

pub unsafe fn unwrap_ty(ty: LLVMTypeRef) -> TyKey {
    unsafe { &*ty }.ty
}

pub fn wrap_value(v: ValueKey) -> LLVMValueRef {
    v.data().as_ffi() as LLVMValueRef
}
