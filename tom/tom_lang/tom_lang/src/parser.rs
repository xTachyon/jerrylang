use crate::ast::{Ast, Expr, FuncCall, Item, Local, Return, Stmt, TyId};
use crate::ast::{ExprKind, Func};
use crate::lexer::{Token, TokenKind};
use TokenKind::*;

pub struct Parser<'a> {
    tokens: &'a [Token],
    offset: usize,
    text: &'a str,
    ast: Ast,
}

impl<'a> Parser<'a> {
    pub fn new(tokens: &'a [Token], text: &'a str) -> Parser<'a> {
        Parser {
            tokens,
            offset: 0,
            text,
            ast: Ast::new(),
        }
    }

    fn peek_kind(&self) -> TokenKind {
        self.tokens[self.offset].kind
    }

    fn match_tok_string(&mut self, kind: TokenKind) -> String {
        let token = self.match_tok(kind);
        self.get_string(&token)
    }

    fn match_tok(&mut self, kind: TokenKind) -> Token {
        if self.tokens[self.offset].kind == kind {
            let ret = self.tokens[self.offset];
            self.offset += 1;
            return ret;
        }
        unimplemented!(
            "expected {:?}, got {:?}",
            kind,
            self.tokens[self.offset].kind
        );
    }

    fn get_string(&self, token: &Token) -> String {
        self.text[token.loc.start()..token.loc.end()].to_string()
    }

    fn parse_expr(&mut self) -> Expr {
        let kind = self.peek_kind();
        match kind {
            NumberLit => {
                let number = self.match_tok(NumberLit);
                let number: i64 = self.get_string(&number).parse().unwrap();
                let kind = ExprKind::NumberLit(number);

                Expr::new(kind, Ast::TY_I64)
            }
            StringLit => {
                let string = self.match_tok(StringLit);
                let mut string = self.get_string(&string);
                string.pop();
                string.remove(0);
                let kind = ExprKind::StringLit(string);

                Expr::new(kind, Ast::TY_STR)
            }
            Ident => {
                let name = self.match_tok_string(Ident);
                self.match_tok(OpenParen);
                let mut args = Vec::new();
                while self.peek_kind() != ClosedParen {
                    args.push(self.parse_expr());
                    match self.peek_kind() {
                        Comma => {
                            self.match_tok(Comma);
                        }
                        _ => break,
                    }
                }
                self.match_tok(ClosedParen);

                let func_id = *self.ast.symbols.get(&name).unwrap();
                let func = self.ast.func(func_id);
                let kind = ExprKind::FuncCall(FuncCall {
                    func: func_id,
                    args,
                });

                Expr::new(kind, func.return_ty)
            }
            _ => unimplemented!("kind: {:?}", kind),
        }
    }

    fn parse_local(&mut self) -> Stmt {
        self.match_tok(Let);
        let name = self.match_tok(Ident);
        let name = self.get_string(&name);
        self.match_tok(Equal);
        let init = self.parse_expr();

        let local = Local { name, init };
        Stmt::Local(local)
    }

    fn parse_return(&mut self) -> Stmt {
        self.match_tok(Return);
        let value = if self.peek_kind() == Semi {
            None
        } else {
            Some(self.parse_expr())
        };

        Stmt::Return(Return { value })
    }

    fn parse_stmt(&mut self) -> Stmt {
        let kind = self.peek_kind();
        let ret = match kind {
            Let => self.parse_local(),
            Return => self.parse_return(),
            _ => Stmt::Expr(self.parse_expr()),
        };
        self.match_tok(Semi);
        ret
    }

    fn get_type(&self, name: &str) -> TyId {
        match name {
            "str" => Ast::TY_STR,
            "i64" => Ast::TY_I64,
            _ => unimplemented!(),
        }
    }

    fn parse_func(&mut self) -> Func {
        self.match_tok(Fn);
        let name = self.match_tok(Ident);
        let name = self.get_string(&name);
        self.match_tok(OpenParen);
        let mut args = Vec::new();
        while self.peek_kind() != ClosedParen {
            let arg_name = self.match_tok(Ident);
            let arg_name = self.get_string(&arg_name);
            self.match_tok(Colon);
            let ty_name = self.match_tok(Ident);
            let ty_name = self.get_string(&ty_name);
            let ty = self.get_type(&ty_name);
            args.push((arg_name, ty));
        }
        self.match_tok(ClosedParen);

        let return_ty = if self.peek_kind() == Arrow {
            self.match_tok(Arrow);
            let name = self.match_tok_string(Ident);
            self.get_type(&name)
        } else {
            Ast::TY_VOID
        };
        let stmts = if self.peek_kind() == Semi {
            self.match_tok(Semi);
            None
        } else {
            self.match_tok(OpenBrace);

            let mut stmts = Vec::new();
            while self.peek_kind() != ClosedBrace {
                stmts.push(self.parse_stmt());
            }
            self.match_tok(ClosedBrace);
            Some(stmts)
        };

        Func {
            name,
            return_ty,
            args,
            stmts,
        }
    }

    pub fn run(mut self) -> Ast {
        let current = self.peek_kind();

        while self.peek_kind() != Eof {
            let name;
            let item = match current {
                Fn => {
                    let func = self.parse_func();
                    name = func.name.clone();
                    Item::Func(func)
                }
                _ => unimplemented!(),
            };
            let id = self.ast.push(item);
            self.ast.symbols.insert(name, id);
        }

        self.ast
    }
}
