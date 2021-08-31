using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Shark.SharkLexer;
using Shark.SharkParser;
using Newtonsoft.Json.Linq;

namespace Shark
{
    class Program
    {
        static void Main(string[] args)
        {
            TEST_Parser("./test.sk");
        }
        static void TEST_Lexer(string filepath){

            string source = SharkUtils.ReadFile(filepath);
            SkLexer lexer = new SkLexer();
            Queue<SkToken> queue = lexer.Parse(source);
            
            while(queue.Count > 0){
                Console.WriteLine(queue.Dequeue());
            }
        }
        static void TEST_Parser(string filepath){

            string source = SharkUtils.ReadFile(filepath);
            SkLexer lexer = new SkLexer(source);
            DEBUG_SkParser parser = new DEBUG_SkParser();
            parser.LoadTokens(lexer.Parse());
            SkAST ast = parser.nextLogicBinOp();

            SaveAsJson(ast, source);
            RenderToHtml();
        }
        static void SaveAsJson(SkAST ast, string title, string filepath="./ast.json"){

            File.Delete(filepath);
            JObject output = new JObject();
            output.Add("title", title);
            output.Add("data", Jsonify(ast));
            SharkUtils.WriteFile(filepath, output.ToString());
        }
        static JObject Jsonify(SkAST O){

            JObject obj = new JObject();
            if(O == null){
                obj.Add("name", "null");
                return obj;
            }else{
                obj.Add("name", O.content);
            }

            if(O.HasChildren){
                JArray arr = new JArray();
                for(int i = 0; i < O.children.Length; i++){
                    arr.Add(Jsonify(O.GetChildren(i)));
                }
                obj.Add("children", arr);
            }
            return obj;
        }
        public static void RenderToHtml(){
            /* 将生成的语法树对象转换为HTML */

            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            p.StandardInput.WriteLine("D:/Code/Python/python.exe c:/Users/MECHREVO/Desktop/Shark/via.py \n &exit");
            p.StandardInput.AutoFlush = true;
            p.WaitForExit();
            p.Close();

        }
    }
}
