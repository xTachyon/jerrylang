#[derive(Debug)]
pub struct Location {
    pub start: u32,
    pub end: u32,
}

impl Location {
    pub(crate) fn new(start: usize, end: usize) -> Location {
        Location { start: start as u32, end: end as u32 }
    }
}

#[derive(Debug)]
pub enum Item {
    Function(Function)
}

#[derive(Debug)]
pub struct Function {
    pub loc: Location,
    pub name: String,
    pub args: Vec<(String, String)>
}
