using System;
using System.IO;
using System.Text;

namespace LLVMnt {
    class Program {
        static void Main(string[] args) {
            var data = File.ReadAllText(args[0]);
            var lexer = new Lexer(data);
            lexer.Run();
        }
    }
}
