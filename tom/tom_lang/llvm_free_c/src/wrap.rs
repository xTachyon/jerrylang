use std::ffi::{CStr, c_char};

pub unsafe fn unwrap_s<'a>(x: *const c_char) -> &'a str {
    assert!(!x.is_null());
    unsafe { CStr::from_ptr(x).to_str().unwrap() }
}
