using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Newtonsoft.Json.Linq;

using SharkTestLib;

using Shark.SharkLexer;
using Shark.SharkParser;
using Shark.SharkVirtualMachine;
using Shark.SharkCore;

namespace Shark
{

    class Program
    {
        static void Main(string[] args)
        {
            // 执行中间代码
            SharkVM vm = new SharkVM();
            SkLexer lexer = new SkLexer(SharkUtils.ReadFile("./test.js"));
            SkParser parser = new SkParser();

            try{
                parser.loadLexerSouce(lexer);                               // 初始化语法解析器
                parser.parseScript();                                       // 语法解析
            }catch(SharkSyntaxError e){
                Console.WriteLine(e.Message);
            }



            SharkScript script = parser.generateScript(false);          // 生成脚本
            vm.LoadScript(script);
            script.DEBUG_MODEL = false;
            
            parser.DEBUG_INFOS.Add("--- 脚本中间代码 ---");
            script.SaveToList(parser.DEBUG_INFOS);
            WriteToFile("./internal_code.txt", parser.DEBUG_INFOS);
        

            Console.WriteLine("--- 执行结果 ---");
            try{
                script.runScript();
            }catch(SharkError e){
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.Green;
            }

            // vm.currentRunable.showCodes();


            // Console.WriteLine(script.getVariable("_"));
            // vm.RunScript(script);

            // Console.WriteLine(script.getVariable("x"));
            // Console.WriteLine(vm.stack.Pop());
            // 执行中间代码结束


            // 其他测试
            // SkTable a = new SkTable();
            // a.Setter(new SkInt(), "b");
            // SkObject b = a.Getter("b");
            // ((SkInt)b).data = 11;

            // Console.WriteLine(((SkInt)a.Getter("b")).data);
            // 其他测试结束

        }
        static void TEST_Lexer(string filepath){

            string source = SharkUtils.ReadFile(filepath);
            SkLexer lexer = new SkLexer();
            Queue<SharkToken> queue = lexer.Parse(source);
            
            while(queue.Count > 0){
                Console.WriteLine(queue.Dequeue());
            }
            Console.WriteLine("符号表:");
            for(int i = 0;i < lexer.SymbolTable.Count; i ++){
                Console.WriteLine(lexer.SymbolTable[i]);
            }
            Console.WriteLine("常量表:");
            for(int i = 0;i < lexer.constTable.Count; i ++){
                Console.WriteLine(lexer.constTable[i]);
            }
        }
        static void TEST_Parser(string filepath){

            string source = SharkUtils.ReadFile(filepath);
            SkLexer lexer = new SkLexer(source);
            DEBUG_SkParser parser = new DEBUG_SkParser();
            // parser.LoadTokens(lexer.Parse());
            SkAST ast = parser.nextMathBinOp_Low();

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

        public static void WriteToFile(string filepath, List<string> contents){

            if(File.Exists(filepath)){
                File.Delete(filepath);
            }

            FileStream file = File.OpenWrite(filepath);
            StreamWriter writer = new StreamWriter(file);
            foreach(string line in contents){
                writer.WriteLine(line);
            }
            writer.Close();
            file.Close();
        }
    }
}
