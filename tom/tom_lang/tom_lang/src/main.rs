use llvm_free::{Module, Function};
use pest_derive::Parser;
use pest::Parser;

#[derive(Parser)]
#[grammar = "tom.pest"]
pub struct JsonParser;

fn main() {
    // let s = "{\"a\" : [1, 2, 3]}";
    // println!("{:?}", JsonParser::parse(Rule::json, s));

    let module = Module::new("branza".into());
    let function = module.function(Some("hello".into()));

    let bb = function.add_basic_block(Some("entry".into()));
    let builder = bb.builder();
    let type_int32 = builder.type_int(32);
    let const_int32 = builder.const_int(53, type_int32);

    println!("{:?}", const_int32);
}
