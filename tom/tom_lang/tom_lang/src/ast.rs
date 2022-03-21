#[derive(Debug)]
pub struct Ast {
    pub items: Vec<Item>,
    pub tys: Vec<Ty>,
}

impl Ast {
    pub const TY_I64: TyId = TyId(0);
    pub const TY_STR: TyId = TyId(1);

    pub fn new() -> Ast {
        let mut tys = Vec::with_capacity(16);
        tys.push(Ty::Builtin(BuiltinTy::I64)); // 0
        tys.push(Ty::Builtin(BuiltinTy::Str)); // 1

        Ast { items: Vec::new(), tys }
    }

    pub fn get(&self, ty: TyId) -> &Ty {
        &self.tys[ty.0 as usize]
    }

    // pub fn make_type(&mut self, ty: Ty) -> TyId {
    //     let id = self.tys.len();
    //     self.tys.push(ty);
    //     TyId(id as u32)
    // }
}

#[derive(Debug)]
pub enum Item {
    Func(Func),
}

#[derive(Debug)]
pub struct Func {
    pub name: String,
    pub args: Vec<(String, TyId)>,
    pub stmts: Option<Vec<Stmt>>,
}

#[derive(Debug)]
pub enum Stmt {
    Local(Local),
}

#[derive(Debug)]
pub struct Local {
    pub name: String,
    pub init: Expr,
}

#[derive(Debug)]
pub enum ExprKind {
    NumberLit(i64),
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
    I64,
    Str,
}

#[derive(Debug)]
pub enum Ty {
    Builtin(BuiltinTy)
}

#[derive(Debug, Copy, Clone)]
pub struct TyId(u32);