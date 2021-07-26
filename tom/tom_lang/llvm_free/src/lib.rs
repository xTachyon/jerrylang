mod types;
mod constants;

use crate::types::{FunctionType, Type, };
use derive_new::new;
use typed_arena::Arena;
pub use types::IntegerType;
use crate::constants::Constant;
use crate::constants::ConstantInt;

#[derive(Debug)]
pub enum Value<'a> {
    Constant(Constant<'a>)
}

pub struct Module<'a> {
    name: String,
    functions: Arena<Function<'a>>,
    basic_blocks: Arena<BasicBlock<'a>>,
    values: Arena<Value<'a>>,
    types: Arena<Type>,
}

impl<'a> Module<'a> {
    pub fn new(name: String) -> Module<'a> {
        Module {
            name,
            functions: Default::default(),
            basic_blocks: Default::default(),
            values: Default::default(),
            types: Default::default()
        }
    }

    pub fn function(&self, name: Option<String>) -> &'a Function {
        let function = Function { module: self, name };
        let function = self.functions.alloc(function);
        function
    }
}

pub struct BasicBlock<'a> {
    module: &'a Module<'a>,
    function: Option<&'a Function<'a>>,
    name: Option<String>
}

impl<'a> BasicBlock<'a> {
    pub fn builder(&'a self) -> Builder {
        Builder {module:self.module,bb:self}
    }
}

pub struct Function<'a> {
    module: &'a Module<'a>,
    name: Option<String>,
}

impl<'a> Function<'a> {
    pub fn add_basic_block(&self, name: Option<String>) -> &'a BasicBlock {
        let bb = BasicBlock { module:self.module,function: Some(self), name };
        self.module.basic_blocks.alloc(bb)
    }
}

pub struct Builder<'a> {
    module: &'a Module<'a>,
    bb: &'a BasicBlock<'a>
}

impl<'a> Builder<'a> {
    pub fn type_int(&self, bits: u32) -> &'a Type {
        let int = IntegerType{ bits };
        self.bb.module.types.alloc(int.into())
    }

    pub fn const_int(&self, value: u64, ty: &'a Type) -> &Value {
        let int = ConstantInt{value, ty};
        let constant = Constant::ConstantInt(int);
        self.module.values.alloc(constant.into())
    }

}

enum LinkageType {
    External,
    AvailableExternally,
    LinkOnceAny,
    LinkOnceODR,
    WeakAny,
    WeakODR,
    Appending,
    Internal,
    Private,
    ExternalWeak,
    Common,
}
