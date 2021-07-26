use crate::types::Type;
use crate::Value;

#[derive(Debug)]
pub struct ConstantInt<'a> {
    pub value: u64,
    pub ty: &'a Type
}

#[derive(Debug)]
pub enum Constant<'a> {
    ConstantInt(ConstantInt<'a>)
}

impl<'a> From<Constant<'a>> for Value<'a> {
    fn from(x: Constant<'a>) -> Self {
        Value::Constant(x)
    }
}