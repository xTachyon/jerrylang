using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace JerryLang {
    class AstCreator {
        private List<Variable> Variables { get; } = new List<Variable>();
        private List<Function> Functions { get; } = new List<Function>();
        private List<(Struct Item, StructType Type)> Structs { get; } = new List<(Struct, StructType)>();
        private SourceFile File { get; }

        public AstCreator(SourceFile file) {
            File = file;
        }

        private Variable FindVariable(string name) {
            return Variables.Select(x => x)
                            .Reverse()
                            .Where(x => x.Name == name)
                            .FirstOrDefault();
        }

        private VariableReference VisitVariableReference(ITerminalNode context) {
            var name = context.GetText();
            var variable = FindVariable(name);
            if (variable == null) {
                throw new CompilerErrorException("variable not found " + name);
            }
            return new VariableReference(GetSourceLocation(context), variable);
        }

        private BinaryOperationKind VisitBinaryOperationKind([NotNull] JerryParser.ExpressionContext context) {
            if (context.PLUS() != null) {
                return BinaryOperationKind.Plus;
            }
            if (context.MINUS() != null) {
                return BinaryOperationKind.Minus;
            }
            if (context.MULTIPLY() != null) {
                return BinaryOperationKind.Multiply;
            }

            throw new CompilerErrorException("unknown binary operation kind");
        }

        private bool IsNumberBinaryOperator(BinaryOperationKind kind) {
            switch (kind) {
                case BinaryOperationKind.Plus:
                case BinaryOperationKind.Minus:
                case BinaryOperationKind.Multiply:
                    return true;
                default:
                    return false;
            }
        }

        private BinaryOperation VisitBinary([NotNull] JerryParser.ExpressionContext context) {
            var left = VisitExpression(context.left);
            var right = VisitExpression(context.right);
            var operation = VisitBinaryOperationKind(context);

            var leftType = left.GetAstType();
            var rightType = right.GetAstType();

            if (IsNumberBinaryOperator(operation) && leftType.IsNumber() && leftType == rightType) {
                return new BinaryOperation(GetSourceLocation(context), left, operation, right, leftType);
            }

            throw new CompilerErrorException("unknown binary operation");
        }

        private Function FindFunction(string name, List<AstType> args) {
            return Functions.Where(x => x.Name == name && x.GetArgumentsTypes().SequenceEqual(args)).First();
        }

        public FunctionCall Visit([NotNull] JerryParser.Function_callContext context) {
            var name = context.name.Text;
            var args = VisitExpressions(context.expression());
            var argsTypes = args.Select(x => x.GetAstType()).ToList();

            var function = FindFunction(name, argsTypes);

            return new FunctionCall(GetSourceLocation(context), function, args);
        }

        public StructInit Visit([NotNull] JerryParser.Struct_initContext context) {
            var name = context.name.Text;
            var exprMap = new Dictionary<string, Expression>();

            foreach (var i in context.struct_init_field()) {
                var fieldName = i.name.Text;
                var expression = VisitExpression(i.value);
                exprMap[fieldName] = expression;
            }

            var @struct = Structs.Where(pair => pair.Item.Name == name).FirstOrDefault();
            if (@struct.Item == null) {
                throw new CompilerErrorException("no struct with that name");
            }

            var expressions = new List<Expression>();
            foreach (var i in @struct.Item.Fields) {
                if (!exprMap.ContainsKey(i.Name)) {
                    throw new CompilerErrorException("unknown field");
                }
                var expression = exprMap[i.Name];
                if (expression.GetAstType() != i.Type) {
                    throw new CompilerErrorException("bad expression type");
                }
                expressions.Add(expression);
            }

            if (@struct.Item.Fields.Count != expressions.Count) {
                throw new CompilerErrorException("not enough args");
            }

            for (int i = 0; i < expressions.Count; ++i) {
                if (@struct.Item.Fields[i].Type != expressions[i].GetAstType()) {
                    throw new CompilerErrorException("invalid type");
                }
            }

            return new StructInit(GetSourceLocation(context), @struct.Type, expressions);
        }

        public List<Expression> VisitExpressions([NotNull] IEnumerable<JerryParser.ExpressionContext> expressions) {
            return expressions.Select(x => VisitExpression(x)).ToList();
        }

        public Expression VisitExpression([NotNull] JerryParser.ExpressionContext context) {
            var literal = context.literal();
            if (literal != null) {
                return VisitLiteral(literal);
            }
            var identifier = context.IDENTIFIER();
            if (identifier != null) {
                return VisitVariableReference(identifier);
            }
            if (context.binary_op != null) {
                return VisitBinary(context);
            }
            var functionCall = context.function_call();
            if (functionCall != null) {
                return Visit(functionCall);
            }
            var structInit = context.struct_init();
            if (structInit != null) {
                return Visit(structInit);
            }
            throw new CompilerErrorException("unknown expression");
        }

        public AstElement VisitAssignment([NotNull] JerryParser.AssignmentContext context) {
            var isNew = context.LET() != null;
            var name = context.name.Text;
            var expression = VisitExpression(context.expression()) as Expression;

            var variable = FindVariable(name);
            if (isNew && variable != null) {
                throw new CompilerErrorException($"variable {name} already declared");
            }
            if (!isNew && variable == null) {
                throw new CompilerErrorException($"variable {name} not declared in this scope :thinking:");
            }
            if (isNew) {
                variable = new Variable(name, expression.GetAstType());
                Variables.Add(variable);
            }

            var sourceLocation = GetSourceLocation(context);
            var assignment = new Assignment(sourceLocation, variable, expression);
            if (isNew) {
                return new VariableDeclaration(sourceLocation, variable, assignment);
            }
            return assignment;
        }

        public LiteralExpression VisitLiteral([NotNull] JerryParser.LiteralContext context) {
            var number = context.number();
            if (number != null) {
                return new NumberLiteralExpression(GetSourceLocation(context), Convert.ToInt64(number.GetText()));
            }
            var str = context.STRING();
            if (str != null) {
                var value = str.GetText();
                value = value[1..^1];
                return new StringLiteralExpression(GetSourceLocation(context), value);
            }
            var boolean = context.boolean();
            if (boolean != null) {
                var value = boolean.TRUE() != null;
                return new BoolLiteralExpression(GetSourceLocation(context), value);
            }
            throw new CompilerErrorException("unknown literal");
        }

        public AstElement VisitStmt([NotNull] JerryParser.StmtContext context) {
            var assignment = context.assignment();
            if (assignment != null) {
                return VisitAssignment(assignment);
            }

            var block = context.block();
            if (block != null) {
                return VisitBlock(block);
            }

            var expression = context.expression();
            if (expression != null) {
                return VisitExpression(expression);
            }

            throw new CompilerErrorException("unknown stmt");
        }

        public AstElement VisitBlock([NotNull] JerryParser.BlockContext context) {
            var stmts = context.stmt().Select(x => (Statement)VisitStmt(x)).ToList();
            return new Block(GetSourceLocation(context), stmts);
        }

        public Struct VisitStruct([NotNull] JerryParser.StructContext context) {
            var name = context.name.Text;
            var fields = new List<StructField>();

            foreach (var i in context.struct_field()) {
                var fieldName = i.name.Text;
                var type = VisitType(i.type.Text);

                fields.Add(new StructField(fieldName, type));
            }

            var item = new Struct(GetSourceLocation(context), name, fields);
            var structType = new StructType(item);

            Structs.Add((item, structType));

            return item;
        }

        public Item VisitItem([NotNull] JerryParser.ItemContext context) {
            var function = context.function();
            if (function != null) {
                return VisitFunction(function);
            }
            var @struct = context.@struct();
            if (@struct != null) {
                return VisitStruct(@struct);
            }

            throw new CompilerErrorException("unknown item");
        }

        public TranslationUnit VisitDocument([NotNull] JerryParser.DocumentContext context) {
            var items = context.item().Select(x => VisitItem(x)).ToList();
            return new TranslationUnit(items);
        }

        private AstType VisitType(string type) {
            return type switch
            {
                "bool" => AstType.Bool,
                "number" => AstType.Number,
                "string" => AstType.String,
                _ => throw new CompilerErrorException("unknown type"),
            };
        }

        public Function VisitFunction([NotNull] JerryParser.FunctionContext context) {
            var name = context.function_name.Text;
            var returnType = new BuiltinType(BuiltinTypeKind.Unit);

            var args = context.argument().Select(x => (x.name.Text, VisitType(x.type.Text))).ToList();

            Block block = null;
            SourceLocation lastBrace = null;
            if (context.block() != null) {
                block = (Block)VisitBlock(context.block());
                lastBrace = GetSourceLocation(context.block().CLOSED_BRACE());
            }

            var result = new Function(GetSourceLocation(context), name, returnType, args, block, lastBrace);
            Functions.Add(result);
            return result;
        }

        private SourceLocation GetSourceLocation(ITerminalNode node) {
            return GetSourceLocation(node.Symbol, node.Symbol);
        }

        private SourceLocation GetSourceLocation(ParserRuleContext parser) {
            return GetSourceLocation(parser.Start, parser.Stop);
        }

        private SourceLocation GetSourceLocation(IToken start, IToken stop) {
            var startIndex = start.StartIndex;
            var stopIndex = stop.StopIndex;
            var line = start.Line;
            var column = start.Column;

            return new SourceLocation(startIndex, stopIndex - startIndex, line, column, File);
        }
    }
}
