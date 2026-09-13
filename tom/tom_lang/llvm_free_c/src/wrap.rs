use std::ffi::{CStr, c_char};

use llvm_free::TyKey;
use slotmap::Key;

use crate::context::{LLVMBool, LLVMTypeRef};

pub unsafe fn unwrap_s<'a>(x: *const c_char) -> &'a str {
    assert!(!x.is_null());
    unsafe { CStr::from_ptr(x).to_str().unwrap() }
}

pub fn wrap_ty(ty: TyKey) -> LLVMTypeRef {
    ty.data().as_ffi() as *const _
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
