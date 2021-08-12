mod untyped_ast;

use lalrpop_util::lalrpop_mod;

lalrpop_mod!(pub jerry); // synthesized by LALRPOP

fn main() {
    let x = jerry::ItemParser::new().parse("fn aaa {}");
    println!("{:?}", x);
}
