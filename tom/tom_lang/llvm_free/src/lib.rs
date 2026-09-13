use slotmap::{new_key_type, SlotMap};

new_key_type! {
    pub struct TyKey;
}

pub struct Context {
    types: SlotMap<TyKey, Ty>,
    pub ty_f64: TyKey,
}

impl Context {
    pub fn new() -> Context {
        let mut types = SlotMap::with_key();
        let ty_f64 = types.insert(Ty::F64);
        Context {
            types: types,
            ty_f64,
        }
    }
}

pub struct Builder {}

impl Builder {
    pub fn new(ctx: &Context) -> Builder {
        Builder {}
    }
}

pub struct Module {}

impl Module {
    pub fn new(ctx: &Context, name: &str) -> Module {
        Module {}
    }
}

pub enum Ty {
    F64,
}
