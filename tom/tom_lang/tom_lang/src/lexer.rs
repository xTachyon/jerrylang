#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum TokenKind {
    Ident,
    Fn,
    Let,

    OpenParen,
    ClosedParen,
    OpenBrace,
    ClosedBrace,
    OpenBracket,
    ClosedBracket,

    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    Semi,
    Colon,
    Equal,

    NumberLit,
    // StringLit,
    Whitespace,
    Eof,
}

#[derive(Debug, Copy, Clone)]
pub struct Location {
    start_p: u32,
    end_p: u32,
}

impl Location {
    fn new(start: usize, end: usize) -> Location {
        Location {
            start_p: start as u32,
            end_p: end as u32,
        }
    }

    pub fn start(&self) -> usize {
        self.start_p as usize
    }

    pub fn end(&self) -> usize {
        self.end_p as usize
    }
}

#[derive(Debug, Copy, Clone)]
pub struct Token {
    pub kind: TokenKind,
    pub loc: Location,
}

#[derive(Debug)]
pub struct Lexer {
    text: Vec<u8>,
    offset: usize,
    tokens: Vec<Token>,
}

impl Lexer {
    pub fn lex(input: &str) -> Vec<Token> {
        if !input.is_ascii() {
            Lexer::error("input must be ascii");
        }
        if input.len() > u32::MAX as usize {
            Lexer::error("length is too big");
        }
        let input = input
            .as_bytes()
            .into_iter()
            .map(|x| *x)
            .chain(Some(b'\0'))
            .collect();
        let mut lexer = Lexer {
            text: input,
            offset: 0,
            tokens: Vec::new(),
        };
        lexer.run();
        lexer.tokens
    }

    fn peek(&self) -> char {
        assert!(self.offset < self.text.len());
        self.text[self.offset] as char
    }

    fn next(&mut self) -> char {
        assert!(self.offset < self.text.len());
        let ret = self.peek();
        self.offset += 1;
        ret
    }

    fn error(msg: &str) -> ! {
        eprintln!("error: {}", msg);
        std::process::exit(1);
    }

    fn run(&mut self) {
        while self.offset < self.text.len() {
            self.run_one();
        }
    }

    fn is_ident_start(ch: char) -> bool {
        ch.is_alphabetic() || ch == '_'
    }

    fn is_ident_continue(ch: char) -> bool {
        Lexer::is_ident_start(ch) || ch.is_ascii_digit()
    }

    fn get_keyword(input: &[u8]) -> Option<TokenKind> {
        let input = std::str::from_utf8(input).unwrap();
        let ret = match input {
            "fn" => TokenKind::Fn,
            "let" => TokenKind::Let,
            _ => return None,
        };
        Some(ret)
    }

    fn run_one(&mut self) {
        use TokenKind::*;

        let original_offset = self.offset;
        let ch = self.next();

        let kind = match ch {
            '+' => Plus,
            '-' => Minus,
            '*' => Star,
            '/' => Slash,
            '%' => Percent,
            '=' => Equal,
            ';' => Semi,
            ':' => Colon,
            '(' => OpenParen,
            ')' => ClosedParen,
            '[' => OpenBracket,
            ']' => ClosedBracket,
            '{' => OpenBrace,
            '}' => ClosedBrace,
            'a' | 'b' | 'c' | 'd' | 'e' | 'f' | 'g' | 'h' | 'i' | 'j' | 'k' | 'l' | 'm' | 'n'
            | 'o' | 'p' | 'q' | 'r' | 's' | 't' | 'u' | 'v' | 'w' | 'x' | 'y' | 'z' | 'A' | 'B'
            | 'C' | 'D' | 'E' | 'F' | 'G' | 'H' | 'I' | 'J' | 'K' | 'L' | 'M' | 'N' | 'O' | 'P'
            | 'Q' | 'R' | 'S' | 'T' | 'U' | 'V' | 'W' | 'X' | 'Y' | 'Z' | '_' => {
                while Lexer::is_ident_continue(self.peek()) {
                    self.next();
                }
                Lexer::get_keyword(&self.text[original_offset..self.offset]).unwrap_or(Ident)
            }
            '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9' => {
                while self.peek().is_ascii_digit() {
                    self.next();
                }
                TokenKind::NumberLit
            }
            ' ' | '\r' | '\n' | '\t' => {
                while self.peek().is_ascii_whitespace() {
                    self.next();
                }
                TokenKind::Whitespace
            }
            '\0' => TokenKind::Eof,
            _ => Lexer::error("unknown char"),
        };

        if kind == Whitespace {
            return;
        }

        let end = if kind == Eof {
            self.offset - 1
        } else {
            self.offset
        };
        self.tokens.push(Token {
            kind,
            loc: Location::new(original_offset, end),
        });
    }
}
