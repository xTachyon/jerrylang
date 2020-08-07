using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
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

            return new Assignment(variable, expression);
        }

        public override AstElement VisitLiteral([NotNull] JerryParser.LiteralContext context) {
            var number = context.number().GetText();
            return new NumberLiteralExpression(Convert.ToInt64(number));
        }

        public override AstElement VisitBlock([NotNull] JerryParser.BlockContext context) {
            var stmts = context.stmt().Select(x => (Statement)VisitStmt(x)).ToList();
            return new Block(stmts);
        }

        public override AstElement VisitDocument([NotNull] JerryParser.DocumentContext context) {
            var raw_functions = context.function();
            var functions = raw_functions.Select(x => VisitFunction(x)).ToList();
            return null;
        }

        public override AstElement VisitExpression([NotNull] JerryParser.ExpressionContext context) {
            return base.VisitExpression(context);
        }

        public override AstElement VisitFunction([NotNull] JerryParser.FunctionContext context) {
            return base.VisitFunction(context);
        }
    }
}
