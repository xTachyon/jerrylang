using JerryLang.obj.Debug.netcoreapp3._1;
using System.IO;

namespace JerryLang {
    class Program {
        static void Main(string[] args) {
            var text = File.ReadAllText("input.jerry");
            var compiler = new Compiler(text);
        }
    }
}