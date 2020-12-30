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

            string[] Test = new string[]{
                "Shark测试代码/test1.sk",
            };
            SharkInterpreter interpreter = new SharkInterpreter();
            for(int i = 0;i < Test.Length;i ++){
                SharkScript script_test = SharkScript.ReadScript(Test[i]);
                script_test.SetVariable("print",new SkObject(new CSFunc(SharkPrint)));

                Console.WriteLine("-----------中间代码开始-----------");
                script_test.ShowCommands();
                Console.WriteLine("-----------中间代码结束-----------");
                Console.WriteLine("-----------脚本执行结果开始-----------");
                script_test.RunScript(interpreter);
                Console.WriteLine("-----------脚本执行结果结束-----------");
            }



        }
        public struct TestS{
            
            public TestMachine a;
            public TestS(int a){
                this.a = new TestMachine();
                this.a.a = a;
            }
        }
        public static void Test(int a){
            Console.WriteLine(a);
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
        public static SkObject SharkPrint(SkList args){

            SkObject Obj = args.Index(0);
            if(Obj.IsVal){
                Console.WriteLine(Obj.GetValue<SkVal>().ToString()); 
            }else{
                Console.WriteLine(Obj.ToString());
            }
            return SkObject.None;
        }
    }

    public class TestMachine{

        public int a = 1;
    }
}
