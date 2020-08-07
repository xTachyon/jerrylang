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
        public Type ReturnType { get; }
    }

    class Document : AstElement {
        public List<Function> Functions { get; }

        public Document(List<Function> functions) {
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
    }

    enum BuiltinTypeKind {
        Number
    }

    class BuiltinType : AstType {
        public BuiltinTypeKind Kind { get; set; }

        public BuiltinType(BuiltinTypeKind kind) {
            Kind = kind;
        }
    }

    abstract class Statement : AstElement {
    }

    class Assignment : Statement {
        public Variable Variable { get; }
        public Expression Expression { get; }

        public Assignment(Variable variable, Expression expression) {
            Variable = variable;
            Expression = expression;
        }
    }

    abstract class Expression : Statement {
        public abstract AstType GetAstType();
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