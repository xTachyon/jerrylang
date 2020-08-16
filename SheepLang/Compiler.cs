using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LLVMSharp.Interop;
using System;
using System.IO;

namespace JerryLang {
    class ErrorListener : IAntlrErrorListener<IToken> {
        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e) {
            throw new NotImplementedException();
        }
    }

    class ErrorListenerInt : IAntlrErrorListener<int> {
        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] int offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e) {
            throw new NotImplementedException();
        }
    }

    class Compiler {
        private TranslationUnit Document { get; }
        private string Filename { get; }
        public Compiler(string file) {
            Filename = file;
            var text = File.ReadAllText(file);

            var inputStream = new AntlrInputStream(text);
            var lexer = new JerryLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new JerryParser(commonTokenStream);

            lexer.AddErrorListener(new ErrorListenerInt());
            parser.AddErrorListener(new ErrorListener());

            var document = parser.document();
            var visitor = new JerryVisitor(new SourceFile(file));

            Document = (TranslationUnit)visitor.VisitDocument(document);
        }

        public void Compile() {
            LLVMModuleRef module;
            using (var codegen = new CodeGenerator(Document, Filename)) {
                module = codegen.Generate();
            }
            module.PrintToFile("code.ll");
            var str = module.PrintToString();
            Console.WriteLine(str);
        }
    }
}
