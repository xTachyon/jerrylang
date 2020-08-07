using Antlr4.Runtime;

namespace JerryLang.obj.Debug.netcoreapp3._1 {
    class Compiler {
        private Document Document { get; }
        public Compiler(string text) {
            var inputStream = new AntlrInputStream(text);
            var SheepLexer = new JerryLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(SheepLexer);
            var sheepParser = new JerryParser(commonTokenStream);
            var document = sheepParser.document();
            var visitor = new JerryVisitor();

            Document = (Document)visitor.VisitDocument(document);
        }
    }
}
