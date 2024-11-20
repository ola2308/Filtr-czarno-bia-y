using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Filtr_czarno_biały
{
    internal class Program
    {
        [DllImport(@"C:\Users\olani\source\repos\Filtr czarno-biały\x64\Debug\ASM.dll")]
        static extern int MyProc1(int a, int b);
        static void Main(string[] args)
        {
            int x = 5, y = 3;
            int ret = MyProc1(x, y);
            Console.Write("Moja pierwsza wartość obliczona w asm to:");
            Console.WriteLine(ret);
            Console.ReadLine();
        }
    }
}
