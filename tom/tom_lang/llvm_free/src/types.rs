use derive_new::new;

#[derive(Debug)]
pub struct IntegerType {
    pub bits: u32,
}

#[derive(Debug)]
pub struct FunctionType {
    self_index: usize,
    arguments: Vec<Type>,
    vararg: bool,
}

#[derive(Debug)]
pub enum Type {
    Integer(IntegerType),
    Function(FunctionType),
}

impl From<IntegerType> for Type {
    fn from(x: IntegerType) -> Self {
        Type::Integer(x)
    }
}