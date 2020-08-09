using System;
using System.Collections.Generic;

namespace JerryLang {

    [System.Serializable]
    public class CompilerErrorException : System.Exception {
        public CompilerErrorException() { }
        public CompilerErrorException(string message) : base(message) { }
        public CompilerErrorException(string message, System.Exception inner) : base(message, inner) { }
        protected CompilerErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    abstract class AstElement {
    }

    abstract class Item : AstElement {

    }

    class Function : Item {
        public string Name { get; }
        public AstType ReturnType { get; }
        public Block Block { get; }

        public Function(string name, AstType returnType, Block block) {
            Name = name;
            ReturnType = returnType;
            Block = block;
        }
    }

    class TranslationUnit : AstElement {
        public List<Function> Functions { get; }

        public TranslationUnit(List<Function> functions) {
            Functions = functions;
        }
    }

    class Something<T> : AstElement {
        public T Value { get; }

        public Something(T value) {
            Value = value;
        }
    }

    class Variable : AstElement {
        public string Name { get; }
        public AstType Type { get; }
        public Variable(string name, AstType type) {
            Name = name;
            Type = type;
        }
    }

    class Block : Statement {
        public List<Statement> Statements { get; }

        public Block(List<Statement> statements) {
            Statements = statements;
        }
    }

    abstract class AstType : AstElement {
        public static readonly AstType Number = new BuiltinType(BuiltinTypeKind.Number);

        public virtual bool IsUnit() {
            return false;
        }

        public virtual bool IsNumber() {
            return false;
        }
    }

    enum BuiltinTypeKind {
        Unit,
        Bool,
        Number,
        String
    }

    class BuiltinType : AstType {
        public BuiltinTypeKind Kind { get; set; }

        public BuiltinType(BuiltinTypeKind kind) {
            Kind = kind;
        }

        public override bool IsUnit() {
            return Kind == BuiltinTypeKind.Unit;
        }

        public override bool IsNumber() {
            return Kind == BuiltinTypeKind.Number;

        }
    }

    abstract class Statement : AstElement {
    }

    class Assignment : Statement {
        public Variable Variable { get; }
        public Expression Expression { get; }
        public bool IsDeclaration { get; }

        public Assignment(Variable variable, Expression expression, bool isDeclaration) {
            Variable = variable;
            Expression = expression;
            IsDeclaration = isDeclaration;
        }
    }

    abstract class Expression : Statement {
        public abstract AstType GetAstType();
    }

    class FunctionCall : Expression {
        public override AstType GetAstType() {
            throw new NotImplementedException();
        }
    }

    class VariableReference : Expression {
        public Variable Variable { get; }

        public VariableReference(Variable variable) {
            Variable = variable;
        }

        public override AstType GetAstType() {
            return Variable.Type;
        }
    }

    enum BinaryOperationKind {
        Plus,
        Multiply
    }

    class BinaryOperation : Expression {
        public Expression Left { get; }
        public BinaryOperationKind Operation { get; }

        public Expression Right { get; }
        public AstType ResultType { get; }

        public BinaryOperation(Expression left, BinaryOperationKind operation, Expression right, AstType resultType) {
            Left = left;
            Operation = operation;
            Right = right;
            ResultType = resultType;
        }

        public override AstType GetAstType() {
            return ResultType;
        }
    }

    abstract class LiteralExpression : Expression {
    }

    class NumberLiteralExpression : LiteralExpression {
        public long Number { get; }

        public NumberLiteralExpression(long number) {
            Number = number;
        }

        public override AstType GetAstType() {
            return AstType.Number;
        }
    }
}