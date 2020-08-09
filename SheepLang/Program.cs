using JerryLang;
using System.IO;

namespace JerryLang {
    class Program {
        static void Main(string[] args) {
            var compiler = new Compiler("input.jerry");
            compiler.Compile();
        }
    }
}