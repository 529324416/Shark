using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Shark.SharkCodeLexer;
using Shark.SharkIL;
using Shark.SharkVirtualMachine;

using Newtonsoft.Json.Linq;

namespace Shark
{

    class Program
    {
        static void Main(string[] args)
        {
            //由于递归调用同一个函数的时候，所有的函数对象共用同一个域的问题
            //导致了bug。

            SharkInterpreter interpreter = new SharkInterpreter();
            SharkScript script = SharkScript.ReadFile("Shark测试代码/test1.sk");


            SkObject func = new SkObject(new CSFunc(SkPrint));
            script.SetVariable("print",func);
            interpreter.RunScript(script);


        }
        public static string ReadFile(string filepath){

            FileStream file = File.OpenRead(filepath);
            StreamReader reader = new StreamReader(file);
            string O = reader.ReadToEnd();
            reader.Close();
            file.Close();
            return O;
        }
        public static void WriteFile(string filepath,string content){
            /* write to file */

            File.Delete(filepath);
            FileStream file = File.OpenWrite(filepath);
            StreamWriter writer = new StreamWriter(file);
            writer.Write(content);
            writer.Close();
            file.Close();
        }
        public static JObject Jsonify(SharkASTree O){

            JObject obj = new JObject();
            if(O == null){
                obj.Add("name","null");
                return obj;
            }else{
                obj.Add("name",O.content.token.ToString());
            }
            if(!O.IsVarOrLiteral){
                JArray arr = new JArray();
                for(int i = 0;i < O.nodes.Length;i++){
                    arr.Add(Jsonify(O.nodes[i]));
                }
                obj.Add("children",arr);
            }
            return obj;
        }
        public static SkObject SkPrint(SkList args){

            SkObject Obj = args.Index(0);
            if(Obj.IsVal){
                Console.WriteLine(Obj.GetValue<SkVal>().ToString()); 
            }else{
                Console.WriteLine(Obj.ToString());
            }
            return SkObject.None;
        }
        public static SkObject GetTime(SkList args){

            Console.WriteLine(DateTime.Now);
            return SkObject.None;
        }
    }
}
