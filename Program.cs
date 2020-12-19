using System;
using System.IO;
using System.Collections.Generic;

using Shark.CodeParser;
using Shark.SharkVirtualMachine;

namespace Shark
{
    class Program
    {
        static void Main(string[] args)
        {


            //以下代码终于生成IL代码
            // Tuple<string[],string> codes = SharkScriptLexer.HandleSourceCode(SharkScriptUtil.ReadSharkScript("./sample.txt"));
            // SharkScriptUtil.OutputArray(codes.key);
            // Console.WriteLine(codes.value);

            //以下是测试代码
            string[] sample = SharkScriptUtil.ReadSharkScript("./codesample.sk");
            SharkScriptLexer lexer = new SharkScriptLexer();
            Console.WriteLine("---以下是中间代码---");
            Queue<string> O = lexer.GenerateILCode(sample,true);
            
            //以下代码用于执行中间代码 
            Console.WriteLine("---以下是执行结果---");
            SharkAPICollection collection = new SharkAPICollection();
            collection.AddFunction("print",new __shark_function(Print));
            collection.AddFunction("T",new __shark_function(Test));
            //初始化一个集合，添加一个print函数进去
            
            SharkInterpreter interpreter = new SharkInterpreter();
            interpreter.LoadAPI("test",collection);
            interpreter.RunILCode(O);
            //执行Shark脚本
        }
        public static SharkObject Print(__shark_list list){

            Console.WriteLine(SharkAPI.SHARKAPI_SkStr2Str(list.Index(0)));
            return SharkObject.None;
        }
        public static SharkObject Test(__shark_list list){

            list = list.Index(0).GetValue<__shark_list>();       //解包参数
            ;
            int value = SharkAPI.SHARKAPI_SkInt2Int(list.Index(0).GetValue<__shark_list>().Index(0));
            Console.WriteLine(value);
            return SharkObject.None;
        }
    }
}
