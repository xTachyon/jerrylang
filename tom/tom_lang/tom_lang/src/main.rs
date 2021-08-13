mod untyped_ast;

use lalrpop_util::lalrpop_mod;
use std::fs;
use std::path::Path;
use anyhow::Result;
use anyhow::Error;

lalrpop_mod!(pub jerry);

fn do_for(path: &Path) -> Result<()> {
    println!("doing {}...", path.to_string_lossy());
    let text = fs::read_to_string(path)?;

    match jerry::ItemParser::new().parse(&text) {
        Ok(x) => {
            println!("{:?}", x);
            Ok(())
        }
        Err(e) => Err(Error::msg(format!("{:?}", e)))
    }
}

fn main() -> Result<()> {
    let folder = std::env::args().nth(1).unwrap_or("tests".to_string());
    for i in fs::read_dir(folder)? {
        let i = i?;
        do_for(&i.path())?;
    }
    Ok(())
}
