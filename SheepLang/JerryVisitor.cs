using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JerryLang {
    class JerryVisitor : JerryBaseVisitor<AstElement> {
        private List<Variable> Variables { get; } = new List<Variable>();

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
            return new VariableReference(variable);
        }

        private BinaryOperationKind VisitBinaryOperationKind([NotNull] JerryParser.ExpressionContext context) {
            if (context.PLUS() != null) {
                return BinaryOperationKind.Plus;
            }
            if (context.MULTIPLY() != null) {
                return BinaryOperationKind.Multiply;
            }

            throw new CompilerErrorException("unknown binary operation kind");
        }

        private bool IsNumberBinaryOperator(BinaryOperationKind kind) {
            switch (kind) {
                case BinaryOperationKind.Plus:
                case BinaryOperationKind.Multiply:
                    return true;
                default:
                    return false;
            }
        }

        private Expression VisitBinary([NotNull] JerryParser.ExpressionContext context) {
            var left = (Expression)VisitExpression(context.left);
            var right = (Expression)VisitExpression(context.right);
            var operation = VisitBinaryOperationKind(context);

            var leftType = left.GetAstType();
            var rightType = right.GetAstType();

            if (IsNumberBinaryOperator(operation) && leftType.IsNumber() && leftType == rightType) {
                return new BinaryOperation(left, operation, right, leftType);
            }

            throw new CompilerErrorException("unknown binary operation");
        }

        public AstElement Visit([NotNull] JerryParser.Function_callContext context) {
            return null;
        }

        public override AstElement VisitExpression([NotNull] JerryParser.ExpressionContext context) {
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
            throw new CompilerErrorException("unknown expression");
        }

        public override AstElement VisitAssignment([NotNull] JerryParser.AssignmentContext context) {
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

            return new Assignment(variable, expression, isNew);
        }

        public override AstElement VisitLiteral([NotNull] JerryParser.LiteralContext context) {
            var number = context.number().GetText();
            return new NumberLiteralExpression(Convert.ToInt64(number));
        }

        public override AstElement VisitStmt([NotNull] JerryParser.StmtContext context) {
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

        public override AstElement VisitBlock([NotNull] JerryParser.BlockContext context) {
            var stmts = context.stmt().Select(x => (Statement)VisitStmt(x)).ToList();
            return new Block(stmts);
        }

        public override AstElement VisitDocument([NotNull] JerryParser.DocumentContext context) {
            var raw_functions = context.function();
            var functions = raw_functions.Select(x => (Function)VisitFunction(x)).ToList();
            return new TranslationUnit(functions);
        }

        public override AstElement VisitFunction([NotNull] JerryParser.FunctionContext context) {
            var name = context.function_name.Text;
            var returnType = new BuiltinType(BuiltinTypeKind.Unit);
            var block = (Block)VisitBlock(context.block());

            return new Function(name, returnType, block);
        }
    }
}
