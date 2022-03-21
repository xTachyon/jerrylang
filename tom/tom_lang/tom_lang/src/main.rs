mod ast;
mod lexer;
mod parser;
mod codegen_llvm;

use crate::lexer::Lexer;
use crate::parser::Parser;
use anyhow::Result;
use std::fs;
use std::path::Path;
use crate::codegen_llvm::Gen;

fn do_for(path: &Path) -> Result<()> {
    println!("doing {}...", path.to_string_lossy());
    let text = fs::read_to_string(path)?;

    let tokens = Lexer::lex(&text);
    for i in &tokens {
        println!("{:?} -> '{}'", i.kind, &text[i.loc.start()..i.loc.end()]);
    }

    let ast = Parser::new(&tokens, &text).run();
    dbg!(&ast);

    Gen::run(&ast);

    Ok(())
}

fn main() -> Result<()> {
    let folder = std::env::args().nth(1).unwrap_or("tests".to_string());
    for i in fs::read_dir(folder)? {
        let i = i?;
        do_for(&i.path())?;
    }
    Ok(())
}
