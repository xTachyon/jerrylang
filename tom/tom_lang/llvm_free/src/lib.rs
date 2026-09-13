use std::cell::RefCell;

use slotmap::{new_key_type, SlotMap};

new_key_type! {
    pub struct TyKey;
    pub struct ValueKey;
}

pub struct Context {
    types: RefCell<SlotMap<TyKey, Ty>>,
    pub ty_f64: TyKey,
}

impl Context {
    pub fn new() -> Context {
        let mut types = SlotMap::with_key();
        let ty_f64 = types.insert(Ty::F64);
        Context {
            types: RefCell::new(types),
            ty_f64,
        }
    }

    pub fn fn_ty(&self, ret_ty: TyKey, param_tys: Vec<TyKey>) -> TyKey {
        let mut types = self.types.borrow_mut();

        let ty = FnType { ret_ty, param_tys };
        types.insert(Ty::FnType(ty))
    }
}

pub struct Builder {}

impl Builder {
    pub fn new(ctx: &Context) -> Builder {
        Builder {}
    }
}

pub struct Fn {
    name: String,
    fn_ty: TyKey,
}

pub struct Module {
    fns: RefCell<Vec<Fn>>,
    values: RefCell<SlotMap<ValueKey, Value>>,
}

impl Module {
    pub fn new(ctx: &Context, name: &str) -> Module {
        Module {
            fns: RefCell::default(),
            values: RefCell::default(),
        }
    }

    pub fn add_fn(&self, name: &str, fn_ty: TyKey) -> ValueKey {
        let mut values = self.values.borrow_mut();

        let f = Fn {
            name: name.to_string(),
            fn_ty,
        };

        values.insert(Value::Fn(f))
    }

    pub fn value_is_fn(&self, key: ValueKey) -> bool {
        let values = self.values.borrow();
        let v = &values[key];
        matches!(v, Value::Fn(_))
    }
}

pub struct FnType {
    pub ret_ty: TyKey,
    pub param_tys: Vec<TyKey>,
}

pub enum Ty {
    F64,
    FnType(FnType),
}

pub enum Value {
    Fn(Fn),
}
