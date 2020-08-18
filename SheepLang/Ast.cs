using System;
using System.Collections.Generic;
using System.Linq;

namespace JerryLang {

    [Serializable]
    public class CompilerErrorException : Exception {
        public CompilerErrorException() { }
        public CompilerErrorException(string message) : base(message) { }
        public CompilerErrorException(string message, Exception inner) : base(message, inner) { }
        protected CompilerErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class SourceFile {
        public string Path { get; }

        public SourceFile(string path) {
            Path = path;
        }
    }

    class SourceLocation {
        public int StartOffset { get; }
        public int Size { get; }
        public int Line { get; }
        public int Column { get; }
        public SourceFile File { get; }

        public SourceLocation(int startOffset, int size, int line, int column, SourceFile file) {
            StartOffset = startOffset;
            Size = size;
            Line = line;
            Column = column;
            File = file;
        }
    }

    abstract class AstElement {
        public virtual IEnumerable<AstElement> GetElements() {
            yield break;
        }
    }

    abstract class Item : AstElement {
    }

    class Function : Item {
        public SourceLocation SourceLocation { get; }
        public string Name { get; }
        public AstType ReturnType { get; }
        public List<(string, AstType)> Arguments { get; }
        public Block Block { get; set; }
        public SourceLocation ClosedBrace { get; }

        public Function(SourceLocation sourceLocation, string name, AstType returnType, List<(string, AstType)> arguments, Block block, SourceLocation closedBrace) {
            SourceLocation = sourceLocation;
            Name = name;
            ReturnType = returnType;
            Arguments = arguments;
            Block = block;
            ClosedBrace = closedBrace;
        }

        public List<AstType> GetArgumentsTypes() {
            return Arguments.Select(x => x.Item2).ToList();
        }

        public override IEnumerable<AstElement> GetElements() {
            return Block.GetElements();
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
        public abstract SourceLocation Location { get; }
    }

    class Block : Statement {
        public override SourceLocation Location { get; }
        public List<Statement> Statements { get; }

        public Block(SourceLocation location, List<Statement> statements) {
            Location = location;
            Statements = statements;
        }

        public override IEnumerable<AstElement> GetElements() {
            foreach (var stmt in Statements) {
                yield return stmt;
                foreach (var i in stmt.GetElements()) {
                    yield return i;
                }
            }
        }
    }

    class VariableDeclaration : Statement {
        public override SourceLocation Location { get; }
        public Variable Variable { get; }
        public Assignment Assignment { get; }

        public VariableDeclaration(SourceLocation sourceLocation, Variable variable, Assignment assignment) {
            Location = sourceLocation;
            Variable = variable;
            Assignment = assignment;
        }
    }

    class Assignment : Statement {
        public override SourceLocation Location { get; }
        public Variable Variable { get; }
        public Expression Expression { get; }

        public Assignment(SourceLocation sourceLocation, Variable variable, Expression expression) {
            Location = sourceLocation;
            Variable = variable;
            Expression = expression;
        }
    }

    abstract class Expression : Statement {
        public abstract AstType GetAstType();
    }

    class FunctionCall : Expression {
        public override SourceLocation Location { get; }
        public Function Function { get; }
        public List<Expression> Arguments { get; }

        public FunctionCall(SourceLocation sourceLocation, Function function, List<Expression> arguments) {
            Location = sourceLocation;
            Function = function;
            Arguments = arguments;
        }

        public override AstType GetAstType() {
            return Function.ReturnType;
        }
    }

    class VariableReference : Expression {
        public override SourceLocation Location { get; }
        public Variable Variable { get; }

        public VariableReference(SourceLocation sourceLocation, Variable variable) {
            Location = sourceLocation;
            Variable = variable;
        }

        public override AstType GetAstType() {
            return Variable.Type;
        }
    }

    enum BinaryOperationKind {
        Plus,
        Minus,
        Multiply
    }

    class BinaryOperation : Expression {
        public override SourceLocation Location { get; }
        public Expression Left { get; }
        public BinaryOperationKind Operation { get; }

        public Expression Right { get; }
        public AstType ResultType { get; }

        public BinaryOperation(SourceLocation sourceLocation, Expression left, BinaryOperationKind operation, Expression right, AstType resultType) {
            Location = sourceLocation;
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
        public override SourceLocation Location { get; }
        public long Number { get; }

        public NumberLiteralExpression(SourceLocation location, long number) {
            Location = location;
            Number = number;
        }

        public override AstType GetAstType() {
            return AstType.Number;
        }
    }
}