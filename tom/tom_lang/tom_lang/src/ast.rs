use std::collections::HashMap;

#[derive(Debug)]
pub struct Ast {
    pub items: Vec<Item>,
    pub tys: Vec<Ty>,
    pub symbols: HashMap<String, ItemId>,
}

impl Ast {
    pub const TY_VOID: TyId = TyId(0);
    pub const TY_I64: TyId = TyId(1);
    pub const TY_STR: TyId = TyId(2);

    pub fn new() -> Ast {
        let mut tys = Vec::with_capacity(16);
        tys.push(Ty::Builtin(BuiltinTy::Void)); // 0
        tys.push(Ty::Builtin(BuiltinTy::I64)); // 1
        tys.push(Ty::Builtin(BuiltinTy::Str)); // 2

        Ast {
            items: Vec::new(),
            symbols: HashMap::new(),
            tys,
        }
    }

    pub fn ty(&self, ty: TyId) -> &Ty {
        &self.tys[ty.0 as usize]
    }

    pub fn func(&self, item: ItemId) -> &Func {
        match &self.items[item.0 as usize] {
            Item::Func(x) => x,
        }
    }

    pub fn push(&mut self, item: Item) -> ItemId {
        let id = ItemId(self.items.len() as u32);
        self.items.push(item);
        id
    }
}

#[derive(Debug)]
pub enum Item {
    Func(Func),
}

#[derive(Debug)]
pub struct Func {
    pub name: String,
    pub return_ty: TyId,
    pub args: Vec<(String, TyId)>,
    pub stmts: Option<Vec<Stmt>>,
}

#[derive(Debug)]
pub enum Stmt {
    Expr(Expr),
    Local(Local),
    Return(Return)
}

#[derive(Debug)]
pub struct Return {
    pub value: Option<Expr>
}

#[derive(Debug)]
pub struct Local {
    pub name: String,
    pub init: Expr,
}

#[derive(Debug)]
pub struct FuncCall {
    pub func: ItemId,
    pub args: Vec<Expr>,
}

#[derive(Debug)]
pub enum ExprKind {
    NumberLit(i64),
    StringLit(String),
    FuncCall(FuncCall),
}

#[derive(Debug)]
pub struct Expr {
    pub kind: ExprKind,
    pub ty: TyId,
}

impl Expr {
    pub fn new(kind: ExprKind, ty: TyId) -> Expr {
        Expr { kind, ty }
    }
}

#[derive(Debug)]
pub enum BuiltinTy {
    Void,
    I64,
    Str,
}

#[derive(Debug)]
pub enum Ty {
    Builtin(BuiltinTy),
}

#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub struct TyId(u32);

#[derive(Debug, Eq, PartialEq, Hash, Copy, Clone)]
pub struct ItemId(pub u32);
