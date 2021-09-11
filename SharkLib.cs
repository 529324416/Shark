using System;
using System.Collections.Generic;

using Shark;
using Shark.SharkCore;
using Shark.SharkVirtualMachine;


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

        public static class SharkCommonLib{

            public static SharkVM VM;
            public static List<SkPack> commonLib = GetCommonLib();
            public static List<SkPack> GetCommonLib(){

                List<SkPack> O = new List<SkPack>();
                O.Add(new SkPack(0, "print", new NativeFunction(Print)));
                O.Add(new SkPack(1, "pow", new NativeFunction(Pow)));
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
        }
    }
}