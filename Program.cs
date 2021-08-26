using System;
using System.Text.RegularExpressions;
using Shark.SharkLexer;

namespace Shark
{
    class Program
    {
        static void Main(string[] args)
        {
            
            SkLexer lexer = new SkLexer(SharkUtils.ReadFile("./test.sk"));
            TEST_Parser(lexer);

            // Console.WriteLine(Regex.Unescape("\u4001"));
        }
        static void TEST_Parser(SkLexer lexer){

            SkToken token;
            while(true){
                token = lexer.getNextToken();
                Console.WriteLine(token);
                if(token.tokenType == SkTokenType.TOKEN_EOF)break;
            }
        }
    }
}
