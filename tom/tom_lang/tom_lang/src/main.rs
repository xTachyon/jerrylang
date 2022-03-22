mod ast;
mod ast_printer;
mod codegen_llvm;
mod lexer;
mod parser;

use crate::ast_printer::print_ast;
use crate::codegen_llvm::Gen;
use crate::lexer::Lexer;
use crate::parser::Parser;
use anyhow::Result;
use std::fs;
use std::path::Path;

fn do_for(path: &Path) -> Result<()> {
    println!("doing {}...", path.to_string_lossy());
    let text = fs::read_to_string(path)?;

    let tokens = Lexer::lex(&text);
    for (index, i) in tokens.iter().enumerate() {
        println!(
            "{}. {:?} -> '{}'",
            index,
            i.kind,
            &text[i.loc.start()..i.loc.end()]
        );
    }

    let ast = Parser::new(&tokens, &text).run();
    println!("\n{}", print_ast(&ast)?);

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
