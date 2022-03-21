use crate::ast::{ExprKind, Func};
use crate::ast::{Ast, Expr, Item, Local, Stmt};
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

    fn error(msg: &str) -> ! {
        eprintln!("error: {}", msg);
        std::process::exit(1);
    }

    fn peek_kind(&self) -> TokenKind {
        self.tokens[self.offset].kind
    }

    fn match_tok(&mut self, kind: TokenKind) -> Token {
        if self.tokens[self.offset].kind == kind {
            let ret = self.tokens[self.offset];
            self.offset += 1;
            return ret;
        }
        dbg!(kind);
        dbg!(self.offset);
        Parser::error("expected another token");
    }

    fn get_string(&self, token: &Token) -> String {
        self.text[token.loc.start()..token.loc.end()].to_string()
    }

    fn parse_expr(&mut self) -> Expr {
        match self.peek_kind() {
            NumberLit => {
                let number = self.match_tok(NumberLit);
                let number: i64 = self.get_string(&number).parse().unwrap();
                let kind = ExprKind::NumberLit(number);

                Expr::new(kind, Ast::TY_I64)
            }
            _ => unimplemented!(),
        }
    }

    fn parse_local(&mut self) -> Stmt {
        self.match_tok(Let);
        let name = self.match_tok(Ident);
        let name = self.get_string(&name);
        self.match_tok(Equal);
        let init = self.parse_expr();
        self.match_tok(Semi);

        let local = Local { name, init };
        Stmt::Local(local)
    }

    fn parse_stmt(&mut self) -> Stmt {
        let kind = self.peek_kind();
        match kind {
            Let => self.parse_local(),
            _ => unimplemented!("{:?}", kind),
        }
    }

    fn parse_fn(&mut self) -> Func {
        self.match_tok(Fn);
        let name = self.match_tok(Ident);
        let name = self.get_string(&name);
        self.match_tok(OpenParen);
        self.match_tok(ClosedParen);
        self.match_tok(OpenBrace);

        let mut stmts = Vec::new();
        while self.peek_kind() != ClosedBrace {
            stmts.push(self.parse_stmt());
        }
        self.match_tok(ClosedBrace);

        Func { name, stmts }
    }

    pub fn run(mut self) -> Ast {
        let current = self.peek_kind();

        while self.peek_kind() != Eof {
            let item = match current {
                Fn => Item::Func(self.parse_fn()),
                _ => unimplemented!(),
            };
            self.ast.items.push(item);
        }

        self.ast
    }
}
