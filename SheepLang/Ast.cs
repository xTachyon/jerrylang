using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<(string, AstType)> Arguments { get; }
        public Block Block { get; set; }

        public Function(string name, AstType returnType, List<(string, AstType)> arguments, Block block) {
            Name = name;
            ReturnType = returnType;
            Arguments = arguments;
            Block = block;
        }

        public List<AstType> GetArgumentsTypes() {
            return Arguments.Select(x => x.Item2).ToList();
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
        public static readonly AstType Bool = new BuiltinType(BuiltinTypeKind.Bool);
        public static readonly AstType Number = new BuiltinType(BuiltinTypeKind.Number);
        public static readonly AstType String = new BuiltinType(BuiltinTypeKind.String);

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
        public Function Function { get; }
        public List<Expression> Arguments { get; }

        public FunctionCall(Function function, List<Expression> arguments) {
            Function = function;
            Arguments = arguments;
        }

        public override AstType GetAstType() {
            return Function.ReturnType;
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