use crate::ast::{Ast, BuiltinTy, Expr, ExprKind, Func, Item, Stmt, Ty, TyId};
use std::fmt::Write;

type Result<T> = std::result::Result<T, std::fmt::Error>;

struct AstPrinter<'a> {
    ast: &'a Ast,
    output: String,
    indent: u32,
}

impl<'a> AstPrinter<'a> {
    fn print_item(&mut self, item: &Item) -> Result<()> {
        match item {
            Item::Func(x) => self.print_func(x),
        }
    }

    fn print_func(&mut self, func: &Func) -> Result<()> {
        write!(self.output, "fn {}(", func.name)?;
        let mut first = true;
        for (name, ty) in func.args.iter() {
            if first {
                first = false;
            } else {
                write!(self.output, ", ")?;
            }
            write!(self.output, "{}: ", name)?;
            self.print_ty(*ty)?;
        }
        write!(self.output, ")")?;
        if !func.return_ty.is_void() {
            write!(self.output, " -> ")?;
            self.print_ty(func.return_ty)?;
        }

        if let Some(body) = &func.stmts {
            writeln!(self.output, " {{")?;
            for i in body {
                self.print_stmt(i)?;
            }
            writeln!(self.output, "}}\n")?;
        } else {
            writeln!(self.output, ";\n")?;
        }

        Ok(())
    }

    fn print_stmt(&mut self, stmt: &Stmt) -> Result<()> {
        for _ in 0..self.indent {
            self.output.push(' ');
        }
        match stmt {
            Stmt::Expr(x) => self.print_expr(x)?,
            Stmt::Local(x) => {
                write!(self.output, "let {} = ", x.name)?;
                self.print_expr(&x.init)?;
            }
            Stmt::Return(x) => match &x.value {
                None => write!(self.output, "return;")?,
                Some(expr) => {
                    write!(self.output, "return ")?;
                    self.print_expr(&expr)?;
                }
            },
        }
        write!(self.output, ";\n")?;

        Ok(())
    }

    fn print_expr(&mut self, expr: &Expr) -> Result<()> {
        match &expr.kind {
            ExprKind::NumberLit(x) => write!(self.output, "{}", x)?,
            ExprKind::BoolLit(x) => write!(self.output, "{}", x)?,
            ExprKind::StringLit(x) => write!(self.output, "\"{}\"", x)?,
            ExprKind::FuncCall(x) => {
                let func = self.ast.func(x.func);
                write!(self.output, "{}(", func.name)?;
                self.print_exprs_with_commas(&x.args)?;
                write!(self.output, ")")?;
            }
        }
        Ok(())
    }

    fn print_exprs_with_commas(&mut self, exprs: &[Expr]) -> Result<()> {
        let mut first = true;
        for i in exprs.iter() {
            if first {
                first = false;
            } else {
                write!(self.output, ", ")?;
            }
            self.print_expr(i)?;
        }
        Ok(())
    }

    fn print_ty(&mut self, ty: TyId) -> Result<()> {
        let ty = self.ast.ty(ty);
        match ty {
            Ty::Builtin(x) => {
                let name = match x {
                    BuiltinTy::Void => "void",
                    BuiltinTy::Bool => "bool",
                    BuiltinTy::I64 => "i64",
                    BuiltinTy::Str => "str",
                };
                write!(self.output, "{}", name)?;
            }
        }

        Ok(())
    }
}

pub fn print_ast(ast: &Ast) -> Result<String> {
    let mut printer = AstPrinter {
        ast,
        output: String::with_capacity(4096),
        indent: 4,
    };
    for i in ast.items.iter() {
        printer.print_item(i)?;
    }

    Ok(printer.output)
}
