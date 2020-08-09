using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LLVMSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;

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
            var visitor = new JerryVisitor();

            Document = (TranslationUnit)visitor.VisitDocument(document);
        }

        public void Compile() {
            var codegen = new CodeGenerator(Document, Filename);
            Exception e = null;
            try {
                codegen.Generate();
            } catch (Exception ex) {
                e = ex;
            } finally {
                var rawStr = LLVM.PrintModuleToString(codegen.Module);
                var str = Marshal.PtrToStringUTF8(rawStr);
                Console.WriteLine(str);
                if (e != null) {
                    throw e;
                }
            }
        }
    }
}
