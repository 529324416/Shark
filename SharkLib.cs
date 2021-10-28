using System;
using System.Collections.Generic;

using Shark;
using Shark.SharkCore;
using Shark.SharkVirtualMachine;
using System.Threading;


namespace Shark{
    namespace SharkLib{

        public class SkPack{

            public int id;
            public string symbol;
            public SkObject value;

            public SkPack(int id, string symbol, SkObject value){

                this.id = id;
                this.symbol = symbol;
                this.value = value;
            }
        }

        public static class SharkListLib{

            public static SharkVM VM;
        }

        public static class SharkCommonLib{

            public static SharkVM VM;
            public static List<SkPack> commonLib = GetCommonLib();
            public static List<SkPack> GetCommonLib(){

                List<SkPack> O = new List<SkPack>();
                O.Add(new SkPack(0, "print", new NativeFunction(Print)));
                O.Add(new SkPack(1, "sleep", new NativeFunction(__sleep)));
                O.Add(new SkPack(2, "dice", new NativeFunction(__dice)));
                O.Add(new SkPack(3, "randint", new NativeFunction(__random_int)));
                O.Add(new SkPack(4, "random", new NativeFunction(__random)));
                return O;
            }

            public static SkObject Print(SkTuple values){

                for(int i = 0; i < values.Size; i ++){
                    Console.Write(values.__index(i).ToString() + " ");
                }
                Console.WriteLine();
                return SkNull.NULL;
            }
            public static SkObject Pow(SkTuple values){

                if(values.Size != 2){
                    throw new SharkArgumentError($"invalid argument for \"pow\" at line {VM.currentLine.ToString()}");
                }
                try{

                    float baseValue = ((SkValue)values.__index(0)).getFloat();
                    float power = ((SkValue)values.__index(1)).getFloat();
                    return new SkFloat(MathF.Pow(baseValue, power));
                }catch(Exception){

                    throw new SharkTypeError($"invalid cast from SkObject to SkInt at line {VM.currentLine.ToString()}");
                }
            }
            public static SkObject FunctionDebug(SkTuple args){

                SkObject func = args.__index(0);
                if(func is SharkFunction){
                    SharkFunction _func = (SharkFunction)func;
                    SharkFunction parent = (SharkFunction)(_func.Parent);
                }else{
                    Console.WriteLine("不支持调试的对象");
                }
                return SkNull.NULL;
            }
            public static SkObject __hashcode(SkTuple args){

                Console.WriteLine(args.__index(0).GetHashCode());
                return SkNull.NULL;
            }
            public static SkObject __random(SkTuple args){

                Random a = new Random();
                return new SkFloat((float)a.NextDouble());
            }
            public static SkObject __random_int(SkTuple args){

                Random a = new Random();
                SkValue left = (SkValue)args.__index(0);
                SkValue right = (SkValue)args.__index(1);
                return new SkInt(a.Next(left.getInt(), right.getInt()));
            }
            public static SkObject __sleep(SkTuple args){

                float time = ((SkValue)args.__index(0)).getFloat() * 1000;
                Thread.Sleep((int)time);
                return SkNull.NULL;
            }
            public static SkObject __dice(SkTuple args){

                Random a = new Random();
                SkValue value = (SkValue)args.__index(0);
                return new SkBool(a.NextDouble() < value.getFloat());
            }
        }
    }
}