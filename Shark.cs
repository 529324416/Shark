using System;
using System.Collections.Generic;
using System.IO;

using Shark.CodeParser;
using Shark.SharkException;
using Shark.SharkVirtualMachine;

namespace Shark{
    /* a simple script used to controll the unity */

    public class Tuple<T1,T2>{
        /* a simple data structure contained two value */

        public T1 key;
        public T2 value;

        public Tuple(T1 key,T2 value){

            this.key = key;
            this.value = value;
        }
    }

    public class SharkScript{
        /* the string object contained two parts,
        the pragma and logical code */

        private string[] __pragmas;
        /* the pragmas will be run first, it defined that, 
        which API would be used in target script */

        private string[] __scripts;
        /* you can use the API collections and  */

        public string[] Pragmas{get{return __pragmas;}}
        public string[] Scripts{get{return __scripts;}}

        public SharkScript(string[] pragmas,string[] scripts){
            /* load data */

            this.__pragmas = pragmas;
            this.__scripts = scripts;
        }
        public SharkScript(LinkedList<string> pragmas,LinkedList<string> scripts){
            /* convert to array type */

            __pragmas = new string[pragmas.Count];
            pragmas.CopyTo(__pragmas,0);
            __scripts = new string[scripts.Count];
            scripts.CopyTo(__scripts,0);
        }
    }

    namespace SharkException{


        public class SharkBaseException:Exception{
            /* the base exception of shark */

            private const string __err_info = "shark error(Shark脚本异常)";
            public SharkBaseException():base(__err_info){}
            public SharkBaseException(string message):base(message){}
            public SharkBaseException(string message,Exception inner):base(message,inner){}
        }

        public class SharkScriptInvalidSyntax:SharkBaseException{
            /* if shark script has occured a syntax error 
            then this exception will be thrown */

            public static SharkScriptInvalidSyntax defaultErr = new SharkScriptInvalidSyntax();

            public const string __err_info = "invalid shark syntax(不正确的Shark语法)";
            public SharkScriptInvalidSyntax():base(__err_info){}
            public SharkScriptInvalidSyntax(string message):base(message){}
            public SharkScriptInvalidSyntax(string message,Exception inner):base(message,inner){}
        }
        public class SharkScriptTypeError:SharkBaseException{

            public static SharkScriptTypeError defaultErr = new SharkScriptTypeError();

            public const string __err_info = "invalid shark type(错误的Shark类型)";
            public SharkScriptTypeError():base(__err_info){}
            public SharkScriptTypeError(string message):base(message){}
            public SharkScriptTypeError(string message,Exception inner):base(message,inner){}
        }
        public class SharkScriptIndexError:SharkBaseException{

            public static SharkScriptIndexError defaultErr = new SharkScriptIndexError();

            public const string __err_info = "invalid index(索引异常)";
            public SharkScriptIndexError():base(__err_info){}
            public SharkScriptIndexError(string message):base(message){}
            public SharkScriptIndexError(string message,Exception inner):base(message,inner){}
        }
        public class SharkScriptNameError:SharkBaseException{

            public static SharkScriptNameError defaultErr = new SharkScriptNameError();

            public const string __err_info = "invalid variable name(变量名异常)";
            public SharkScriptNameError():base(__err_info){}
            public SharkScriptNameError(string message):base(message){}
            public SharkScriptNameError(string message,Exception inner):base(message,inner){}
        }
        public class SharkScriptConflictError:SharkBaseException{

            public static SharkScriptConflictError defaultErr = new SharkScriptConflictError();

            public const string __err_info = "confilict error(变量名冲突)";
            public SharkScriptConflictError():base(__err_info){}
            public SharkScriptConflictError(string message):base(message){}
            public SharkScriptConflictError(string message,Exception inner):base(message,inner){}
        }
    }

    namespace CodeParser{
        /* contain the code of shark script parser */

        public class SharkScriptUtil{
            /* some basic function tools*/

            public static char[] WHITE_SPACE = new char[]{' '};

            public static void OutputQueue(Queue<char> codes){

                int size = codes.Count;
                for(int i = 0;i < size; i++){
                    Console.Write(codes.Dequeue());
                }
            }
            public static void OutputArray(string[] arr){

                foreach(string line in arr){
                    Console.WriteLine(line);
                }
            }
            public static void ExtendList<T>(List<T> dest,List<T> source){
                /* 把两个列表的内容加到一起 */

                foreach(T t in source){
                    dest.Add(t);
                }
            }
            public static T ExtractObjectFromDict<T>(string key,Dictionary<string,T> dict){
                /* 从字典中提取一个元素 */

                T buf;
                if(dict.TryGetValue(key,out buf)){
                    return buf;
                };
                return default(T);
            }
            public static string[] SplitStringWithWhiteSpace(string cnt,int count){

                return cnt.Split(WHITE_SPACE,count);
            }
            public static LinkedList<string> HandleSharkScript(string[] lines){
                /*read all script and remove \n && // in 
                all code */

                LinkedList<string> buf = new LinkedList<string>();
                foreach(string line in lines){
                    string tmp = line.Trim();
                    if(tmp.Length > 0 && !tmp.StartsWith(SharkScriptLexer.SIGN_COMMENT)){
                        buf.AddLast(tmp);
                    }
                }
                return buf;
            }
            public static string[] ReadFile(string filepath){
                /* read all text in target file and load into a string array
                @filepath: the filepath of target shark script */

                if(File.Exists(filepath)){
                    string line;
                    LinkedList<String> buf = new LinkedList<string>();
                    StreamReader reader = new StreamReader(filepath);
                    while((line = reader.ReadLine()) != null){
                        buf.AddLast(line);
                    }
                    string[] arr = new string[buf.Count];
                    buf.CopyTo(arr,0);
                    return arr;
                }
                return null;
            }
            public static string[] RemoveComment(LinkedList<string> lines){
                /* remove the comment in the shark code */

                LinkedList<string> script = new LinkedList<string>();
                foreach(string line in lines){
                    script.AddLast(RemoveCommentSingleLine(line));
                }
                string[] buf = new string[script.Count];
                script.CopyTo(buf,0);
                return buf;
            }
            private static string RemoveCommentSingleLine(string line){
                /* if has comment sign in line then remove it , otherwise return line */

                int idx = line.IndexOf(SharkScriptLexer.SIGN_COMMENT);
                if(idx == -1){
                    return line;
                }
                return line.Substring(0,idx);
            }
            public static string[] ReadSharkScript(string filepath){
                /* read shark string from file */

                string[] script = ReadFile(filepath);
                if(script != null){
                    return RemoveComment(HandleSharkScript(script));
                }
                return null;
            }
            public static SharkScript SelectCodes(string[] lines){
                /* choose all pragma in target code */

                LinkedList<string> pragmas = new LinkedList<string>();
                LinkedList<string> scripts = new LinkedList<string>();
                foreach(string line in lines){
                    if(line.StartsWith(SharkScriptLexer.SIGN_PRAGMA.ToString())){
                        pragmas.AddLast(line);
                    }else{
                        scripts.AddLast(line);
                    }
                }
                return new SharkScript(pragmas,scripts);
            }
            public static string CombineStringList(LinkedList<string> list){
                /* link all string value in target list into one string and return */

                string buf = string.Empty;
                foreach(string line in list){
                    buf += line;
                }
                return buf;
            }
            public static Queue<char> GenerateCode(string script){
                /* generate a char queue from given string value */

                bool hasSpace = false;
                Queue<char> O = new Queue<char>();
                foreach(char c in script.Trim()){
                    if(char.IsWhiteSpace(c)){
                        if(!hasSpace){
                            hasSpace = true;
                            O.Enqueue(c);
                        }
                    }else{
                        if(hasSpace){
                            hasSpace = false;
                        }
                        O.Enqueue(c);
                    }
                }
                return O;
            }
        }

        public enum PDA_NUMBER_TYPE{
            /* flaot or int */

            INT,
            FLOAT
        }
        public enum RESULT_LETTER{

            VAR,
            FUNC
        }

        public class PDA{
            /* when entry a new part, then code parser will generate a new pda
            the pda will parse all character until entry code format error or */

            public delegate bool BoolGetterREQString(char nextChar);

            public static void PDASafeCheck(Queue<char> codes){
                /* until the code has parse done, otherwise the codes shouldn't end */

                if(codes.Count == 0){
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public static string NextString(Queue<char> codes,string tmp){
                /* get next string value */

                char nextChar = codes.Peek();
                if(nextChar == SharkScriptLexer.VAR_STRING){
                    return tmp + codes.Dequeue().ToString();
                }
                return NextString(codes,tmp + codes.Dequeue().ToString());
            }
            public static string NextString(Queue<char> codes){
                /* 获取下一个字符串对象 */

                codes.Dequeue();        //移除第一个引号
                return __NextString(codes,string.Empty);
            }
            private static string __NextString(Queue<char> codes,string tmp){

                char nextChar = codes.Peek();
                if(nextChar == SharkScriptLexer.VAR_STRING){
                    codes.Dequeue();    //移除第二个引号
                    return tmp;
                }
                codes.Dequeue();
                return __NextString(codes,tmp + nextChar.ToString());
            }
            public static string NextLine(Queue<char> codes){
                /* do nothing about first character */

                return NextLine(codes,string.Empty);
            }
            public static string NextLine(Queue<char> codes,string tmp){
                /* get next line until a ; occurred, this function will 
                do some basic check before get the whole line */

                char nextChar = codes.Peek();
                if(nextChar == SharkScriptLexer.VAR_STRING){
                    string __tmp = codes.Dequeue().ToString();
                    tmp += NextString(codes,__tmp);
                    return NextLine(codes,tmp);
                }else if(nextChar == SharkScriptLexer.CODE_SEP){
                    return tmp;
                }else{
                    tmp += codes.Dequeue().ToString();
                    return NextLine(codes,tmp);
                }
            }
            public static Tuple<string,PDA_NUMBER_TYPE> NextNumber(Queue<char> codes,string tmp){
                /* get next number value, and this function would have chance to jump to next 
                function NextFloat only if one '.' has been found, note that the end of 
                this PDA has more than one characters, including ')' ',' and ';' */

                char nextChar = codes.Peek();
                if(char.IsDigit(nextChar)){
                    tmp += codes.Dequeue().ToString();
                    return NextNumber(codes,tmp);
                }else if(nextChar == SharkScriptLexer.VAR_FLOAT){
                    tmp += codes.Dequeue().ToString();
                    return NextFlaot(codes,tmp);
                }else if(IsNumberEnd(nextChar)){
                    return new Tuple<string, PDA_NUMBER_TYPE>(tmp,PDA_NUMBER_TYPE.INT);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public static Tuple<string,PDA_NUMBER_TYPE> NextFlaot(Queue<char> codes,string tmp){
                /* get next float value */

                char nextChar = codes.Peek();
                if(char.IsDigit(nextChar)){
                    tmp += codes.Dequeue().ToString();
                    return NextFlaot(codes,tmp);
                }else if(IsNumberEnd(nextChar)){
                    return new Tuple<string, PDA_NUMBER_TYPE>(tmp,PDA_NUMBER_TYPE.FLOAT);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public static Tuple<string,PDA_NUMBER_TYPE> NextNumberWithCustomEnding(
                Queue<char> codes,string tmp,BoolGetterREQString conditionSignal){
                /* get next number value, and this function would have chance to jump to next 
                function NextFloat only if one '.' has been found, note that the end of 
                this PDA has more than one characters, including ')' ',' and ';' */

                char nextChar = codes.Peek();
                if(char.IsDigit(nextChar)){
                    codes.Dequeue();
                    return NextNumberWithCustomEnding(codes,tmp + nextChar.ToString(),conditionSignal);
                }else if(nextChar == SharkScriptLexer.VAR_FLOAT){
                    codes.Dequeue();
                    return NextFloatWithCustomEnding(codes,tmp + nextChar.ToString(),conditionSignal);
                }else if(conditionSignal(nextChar)){
                    return new Tuple<string, PDA_NUMBER_TYPE>(tmp,PDA_NUMBER_TYPE.INT);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public static Tuple<string,PDA_NUMBER_TYPE> NextFloatWithCustomEnding(
                Queue<char> codes,string tmp,BoolGetterREQString conditionSignal){
                /* get next float value */

                char nextChar = codes.Peek();
                if(char.IsDigit(nextChar)){
                    codes.Dequeue();
                    return NextFloatWithCustomEnding(codes,tmp + nextChar.ToString(),conditionSignal);
                }else if(conditionSignal(nextChar)){
                    return new Tuple<string, PDA_NUMBER_TYPE>(tmp,PDA_NUMBER_TYPE.FLOAT);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public static Tuple<string,RESULT_LETTER> NextGuessLetterInList(Queue<char> codes){
                /* 找到下一个由字母或者下划线开头的对象，如果是一个变量，REUSLT_LETTER为VAR
                否则为FUNC */

                string head = codes.Dequeue().ToString();
                return __NextGuessLetterInList(codes,head);
            }
            private static Tuple<string,RESULT_LETTER> __NextGuessLetterInList(Queue<char> codes,string head){
                /* 除了第一个字符之外，其他的字符可以包括数字 */

                char nextChar = codes.Peek();
                if(char.IsLetterOrDigit(nextChar) || nextChar == SharkScriptLexer.UNDERLINE){
                    string _ = codes.Dequeue().ToString();
                    return __NextGuessLetterInList(codes,head + _);
                }else if(IsListEnd(nextChar)){
                    return new Tuple<string, RESULT_LETTER>(head,RESULT_LETTER.VAR);
                }else if(nextChar == SharkScriptLexer.FUNCTION_PARAMS_START){
                    return new Tuple<string, RESULT_LETTER>(head,RESULT_LETTER.FUNC);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public static Tuple<string,RESULT_LETTER> NextGuessLetter(Queue<char> codes,char stop){
                /* 找到下一个由字母或者下划线开头的对象，如果是一个变量，RESULT_LETTER为VAR否则为FUNC 
                @_：表示该变量是第一阶段还是第二阶段，如果是第一阶段，终止符可以是赋值号，否则不可以 */

                string head = codes.Dequeue().ToString();
                return __NextGuessLetter(codes,head,stop);
            }
            public static Tuple<string,RESULT_LETTER> __NextGuessLetter(Queue<char> codes,string head,char stop){
                /* 除了第一个字母外，其他的字母可以是数字下划线或者字母,
                和__NextGuessLetterInList不同的是，这个函数的终止符是;*/

                char nextChar = codes.Peek();
                if(char.IsLetterOrDigit(nextChar) || nextChar == SharkScriptLexer.UNDERLINE){
                    /* 用下推状态机滚动到停止符号 */

                    string c = codes.Dequeue().ToString();
                    return __NextGuessLetter(codes,head + c,stop);
                }else if(nextChar == stop){
                    /* 遇到函数终止符，仅用于给别的变量赋值时才能进行的操作 */

                    return new Tuple<string, RESULT_LETTER>(head,RESULT_LETTER.VAR);
                }else if(nextChar == SharkScriptLexer.FUNCTION_PARAMS_START){
                    /* 该变量是一个函数调用 */

                    return new Tuple<string, RESULT_LETTER>(head,RESULT_LETTER.FUNC);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }

            public static bool IsNumberEnd(char nextChar){
                /* check out if should exit next_number function now */

                return nextChar == SharkScriptLexer.CODE_SEP || 
                nextChar == SharkScriptLexer.FUNCTION_PARAMS_END || 
                nextChar == SharkScriptLexer.FUNCTION_PARAMS_SEP;
            }
            public static bool IsNumberEndArgs(char nextChar){

                return nextChar == SharkScriptLexer.FUNCTION_PARAMS_SEP || 
                nextChar == SharkScriptLexer.FUNCTION_BODY_START;
            }
            public static bool IsNumberEndLine(char nextChar){

                return nextChar == SharkScriptLexer.CODE_SEP;
            }
            public static bool IsListEnd(char nextChar){
                /* 检查是否一个列表或者参数列表元素已经结束了 */

                return nextChar == SharkScriptLexer.FUNCTION_PARAMS_SEP || 
                nextChar == SharkScriptLexer.LIST_END ||
                nextChar == SharkScriptLexer.FUNCTION_PARAMS_END;
            }
        }

        public class SharkScriptLexer{
            /* used to analysis the shark script */

            public const string SIGN_COMMENT = "//";
            public const char SIGN_FUNCTION = '@';
            public const char SIGN_PRAGMA = '#';
            public const char LIST_START = '[';
            public const char LIST_END = ']';
            public const char FUNCTION_BODY_START = '{';
            public const char FUNCTION_BODY_END = '}';
            public const char FUNCTION_PARAMS_START = '(';
            public const char FUNCTION_PARAMS_END = ')';
            public const char FUNCTION_PARAMS_SEP = ',';
            public const char VAR_STRING = '\"';
            public const char VAR_FLOAT = '.';
            public const char OP_ASSIGN = '=';
            public const char CODE_SEP = ';';
            public const string PRAGMA_API = "#api";
            public const string PRAGMA_BIND = "#bind";
            public const char SPACE = ' ';
            public const char EOL = '\n';
            public const string ENTRY_POINT = "Main";

            public const char UNDERLINE = '_';


            private Queue<char> characters;
            private Queue<string> ILCodes;

            private PDA.BoolGetterREQString listEnding = ListElementEnding;
            private PDA.BoolGetterREQString codeSepEnding = CodeSepEnding;
            public SharkScriptLexer(){
                /* intialize the characters object and ready to load shark script */

                characters = new Queue<char>();
            }
            public void LoadScript(string script){
                /* load the script into characters 
                注意这里的script是预处理处理过的第二部分 */

                characters.Clear();
                foreach(char c in script){
                    characters.Enqueue(c);
                }
            }
            public Queue<string> GenerateILCode(string[] lines,bool showILCode){
                /* 生成中间代码 */

                ILCodes = new Queue<string>();
                Tuple<string[],string> combinations = HandleSourceCode(lines);
                foreach(string pragma in combinations.key){ILCodes.Enqueue(pragma);}
                LoadScript(combinations.value);

                while(characters.Count > 0){
                    if(characters.Peek() == SIGN_FUNCTION){
                        List<string> list = NextFunctionDefination();
                        foreach(string line in list){
                            if(showILCode){
                                Console.WriteLine(line);
                            }
                            ILCodes.Enqueue(line);
                        }
                    }else{
                        throw SharkScriptInvalidSyntax.defaultErr;
                    }
                }
                return ILCodes;
            }
            public string NextNumberInList(){
                /* 解析下一个数字型值，并生成针对列表项的IL代码 */

                string tmp = characters.Dequeue().ToString();
                Tuple<string,PDA_NUMBER_TYPE> result = PDA.NextNumberWithCustomEnding(characters,tmp,listEnding);
                if(result.value == PDA_NUMBER_TYPE.FLOAT){
                    return $"1PUSH_FLOAT {result.key}";
                }
                return $"1PUSH_INT {result.key}";
            }
            public string NextNumberFillVariable(string varName){
                /* 解析下一个数字类型，并生成对应变量值的IL代码 */

                string tmp = characters.Dequeue().ToString();
                Tuple<string,PDA_NUMBER_TYPE> result = PDA.NextNumberWithCustomEnding(characters,tmp,CodeSepEnding);
                if(result.value == PDA_NUMBER_TYPE.FLOAT){
                    return $"2FILL_FLOAT {varName} {result.key}";
                }
                return $"2FILL_INT {varName} {result.key}";
            }
            public string NextStringInList(){
                /* 解析下一个字符串型值，并生成针对列表项的IL代码 */

                return $"1PUSH_STRING {PDA.NextString(characters)}";
            }
            public string NextStringFillVariable(string varName){
                /* 解析下一个字符串型值，并生成针对变量值的IL代码 */

                return $"2FILL_STRING {varName} {PDA.NextString(characters)}";
            }
            public Tuple<object,RESULT_LETTER> NextVariableInList(){
                /* 当检测到开头的字母是下划线或者,当结尾并不是,或者]而是一个(时，则认为
                该对象是一个函数，并连带之前的内容跳转到函数调用的解析策略中 */

                Tuple<string,RESULT_LETTER> R = PDA.NextGuessLetterInList(characters);
                if(R.value == RESULT_LETTER.VAR){
                    return new Tuple<object, RESULT_LETTER>($"1PUSH_VAR {R.key}",R.value);
                }
                List<string> O = new List<string>();
                O.Add($"1FIND_FUNC {R.key}");
                characters.Dequeue();   //移除左括号
                SharkScriptUtil.ExtendList<string>(O,NextParamList());
                O.Add($"0DOCALL_FILL_PARAM");
                return new Tuple<object, RESULT_LETTER>(O,RESULT_LETTER.FUNC);
            }
            public Tuple<object,RESULT_LETTER> NextVariable(){
                /* 当检测到开头的字母是下划线或者,当结尾并不是,或者]而是一个(时，则认为
                该对象是一个函数，并连带之前的内容跳转到函数调用的解析策略中,
                该函数针对的是一个不在列表的元素 */

                Tuple<string,RESULT_LETTER> R = PDA.NextGuessLetter(characters,OP_ASSIGN);       //函数可以赋值号作为状态机终止符
                if(R.value == RESULT_LETTER.VAR){
                    return new Tuple<object, RESULT_LETTER>(R.key,R.value);
                }
                List<string> O = new List<string>();
                O.Add($"1FIND_FUNC {R.key}");
                characters.Dequeue();   //移除左括号
                SharkScriptUtil.ExtendList<string>(O,NextParamList());
                O.Add($"0DOCALL");
                return new Tuple<object, RESULT_LETTER>(O,RESULT_LETTER.FUNC);
            }
            public Tuple<object,RESULT_LETTER> NextVariable(string varName){
                /* 当检测到开头的字母是下划线或者,当结尾并不是,或者]而是一个(时，则认为
                该对象是一个函数，并连带之前的内容跳转到函数调用的解析策略中,
                该函数针对的是一个不在列表的元素 */

                Tuple<string,RESULT_LETTER> R = PDA.NextGuessLetter(characters,CODE_SEP);      //赋值符号不再成为终止符
                if(R.value == RESULT_LETTER.VAR){
                    return new Tuple<object, RESULT_LETTER>($"2FILL_VAR {varName} {R.key}",R.value);
                }
                List<string> O = new List<string>();
                O.Add($"1FIND_FUNC {R.key}");
                characters.Dequeue();   //移除左括号
                SharkScriptUtil.ExtendList<string>(O,NextParamList());
                O.Add($"1DOCALL_FILL {varName}");
                return new Tuple<object, RESULT_LETTER>(O,RESULT_LETTER.FUNC);
            }
            public List<string> NextParamList(){
                /* 找到下一个完整的参数列表 */

                List<string> O = new List<string>();
                O.Add("0MAKE_LIST");
                char nextChar = characters.Peek();
                if(nextChar == FUNCTION_PARAMS_END){
                    /*参数的末尾了*/

                    characters.Dequeue();   //移除右括号
                    return O;
                }
                return __NextParamList(O);
            }
            public List<string> __NextParamList(List<string> O){
                /* PDA函数 */

                char nextChar = characters.Peek();
                if(nextChar == FUNCTION_PARAMS_END){
                    /* 参数列表已经搜索完毕 */

                    characters.Dequeue();
                    return O;
                }else if(char.IsDigit(nextChar)){
                    /* 数字型参数 */

                    O.Add(NextNumberInList());
                    return __NextParamList(O);
                }else if(nextChar == VAR_STRING){
                    /* 字符串型参数 */

                    O.Add(NextStringInList());
                    return __NextParamList(O);
                }else if(char.IsLetter(nextChar) || nextChar == UNDERLINE){
                    /* 变量型参数 */

                    Tuple<object,RESULT_LETTER> R = NextVariableInList();
                    if(R.value == RESULT_LETTER.FUNC){
                        SharkScriptUtil.ExtendList<string>(O,(List<string>)R.key);
                    }else{
                        O.Add((string)R.key);
                    }
                    return __NextParamList(O);
                }else if(nextChar == LIST_START){
                    /* 列表型数据 */

                    characters.Dequeue();
                    SharkScriptUtil.ExtendList<string>(O,NextList());
                    return __NextParamList(O);
                }else if(nextChar == FUNCTION_PARAMS_SEP){
                    /*遇到分隔符，则移除该分隔符重新选取下一个符号 */

                    characters.Dequeue();
                    return __NextParamList(O);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public List<string> NextList(){
                /* 解析下一个完整的列表,如果遇到异常则报代码结构错误,进入该函数则默认上一个出现的字符是 [ 
                @callbackCommand: 该字符串表示当列表解析完成后，该对列表做怎样的操作 */

                List<string> O = new List<string>();
                O.Add("0MAKE_LIST");
                char nextChar = characters.Peek();
                if(nextChar == SharkScriptLexer.LIST_END){
                    return O;
                }
                return __NextList(O);
            }
            private List<string> __NextList(List<string> O){
                /* PDA函数 */

                char nextChar = characters.Peek();
                if(nextChar == LIST_END){

                    characters.Dequeue();
                    return O;
                }else if(char.IsDigit(nextChar)){
                    /* 数字型参数 */

                    O.Add(NextNumberInList());
                    return __NextList(O);
                }else if(nextChar == VAR_STRING){
                    /* 字符串型参数 */

                    O.Add(NextStringInList());
                    return __NextList(O);
                }else if(char.IsLetter(nextChar) || nextChar == UNDERLINE){
                    /* 变量型参数 */

                    Tuple<object,RESULT_LETTER> R = NextVariableInList();
                    if(R.value == RESULT_LETTER.FUNC){
                        SharkScriptUtil.ExtendList<string>(O,(List<string>)R.key);
                    }else{
                        O.Add((string)R.key);
                    }
                    return __NextList(O);
                }else if(nextChar == LIST_START){
                    /* 列表型数据 */

                    characters.Dequeue();
                    SharkScriptUtil.ExtendList<string>(O,NextList());
                    O.Add("0FILL_PARAM");
                    return __NextList(O);
                }else if(nextChar == SIGN_FUNCTION){
                    /* 定义一个函数 */

                    SharkScriptUtil.ExtendList<string>(O,NextFunctionDefination());
                    O.Add("0END_DEFINE_FUNC_INLIST");
                    return __NextList(O);
                }else if(nextChar == FUNCTION_PARAMS_SEP){
                    /*遇到分隔符后忽略，并从下一个字符开始 */

                    characters.Dequeue();
                    return __NextList(O);
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public List<string> NextFunctionDefination(){
                /* 找到下一个完整的函数定义,当首次找到的标签为一个@时，进入该函数 */

                characters.Dequeue();       //移除符号@
                List<string> O = new List<string>();
                string funcName = NextFunctionDefineName();
                characters.Dequeue();   //移除左花括号
                if(funcName == null){
                    O.Add("0DEFINE_FUNC_NULLNAME");
                    SharkScriptUtil.ExtendList<string>(O,AttachDefineFunc(NextFunctionDefineBody()));
                }else{
                    if(funcName == ENTRY_POINT){
                        SharkScriptUtil.ExtendList<string>(O,NextFunctionDefineBody());
                    }else{
                        O.Add($"0DEFINE_FUNC {funcName}");
                        SharkScriptUtil.ExtendList<string>(O,AttachDefineFunc(NextFunctionDefineBody()));
                    }
                }
                characters.Dequeue();   //移除右花括号
                return O;
            }
            public List<string> AttachDefineFunc(List<string> O){

                List<string> _O = new List<string>();
                foreach(string line in O){
                    if(line.IndexOf("PUSH_VAR") > 0){
                        _O.Add($"1ADDILCODE_PUSHVAR {line}");
                    }else{
                        _O.Add($"1ADDILCODE {line}");
                    }
                }
                return _O;
            }
            public string NextFunctionDefineName(){
                /* 找到下一个函数的完整的函数名,支持匿名函数,但匿名函数必须赋值给一个变量
                或者塞入参数列表或者作为列表项目 */

                char nextChar = characters.Peek();
                if(nextChar == FUNCTION_BODY_START){
                    return null;
                }else if(IsVarHead(nextChar)){

                    characters.Dequeue();   //remove the first character;
                    return __NextFunctionDefineName(nextChar.ToString());
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public string __NextFunctionDefineName(string buf){
                /* PDA函数,从这里开始，函数就不再是一个匿名函数*/

                char nextChar = characters.Peek();
                if(nextChar == FUNCTION_BODY_START){
                    return buf;
                }else if(char.IsLetterOrDigit(nextChar)||nextChar == UNDERLINE){

                    characters.Dequeue();
                    return __NextFunctionDefineName(buf + nextChar.ToString());
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public List<string> NextFunctionDefineBody(){
                /* 找到下一个完整的函数体，并生成中间代码 */

                List<string> O = new List<string>();
                char nextChar = characters.Peek();
                if(IsVarHead(nextChar)){
                    /* 是一个变量并且没有结束 */

                    return __NextFunctionDefineBody(O);
                }else if(nextChar == FUNCTION_BODY_END){

                    return O;
                }else{
                    throw SharkScriptInvalidSyntax.defaultErr;
                }
            }
            public List<string> __NextFunctionDefineBody(List<string> O){
                /* 找到下一个完整的函数体，并生成中间代码 */

                Tuple<object,RESULT_LETTER> R = NextVariable();
                if(R.value == RESULT_LETTER.FUNC){
                    SharkScriptUtil.ExtendList<string>(O,(List<string>)R.key);
                }else{
                    string varName = (string)R.key;
                    O.Add($"1DIM {varName}");
                    characters.Dequeue();   //移除赋值号
                    char c = characters.Peek(); //获取下一个字符
                    if(IsVarHead(c)){
                        Tuple<object,RESULT_LETTER> R2 = NextVariable(varName);
                        if(R2.value == RESULT_LETTER.FUNC){
                            SharkScriptUtil.ExtendList<string>(O,(List<string>)R2.key);
                        }else{
                            O.Add((string)R.key);
                        }
                    }else if(char.IsDigit(c)){
                        /* 数字型变量 */

                        O.Add(NextNumberFillVariable(varName));
                    }else if(c == LIST_START){
                        /* list type value */

                        characters.Dequeue();   //remove the sign '['
                        SharkScriptUtil.ExtendList<string>(O,NextList());
                        O.Add($"1FILL_LIST {varName}");

                    }else if(c == VAR_STRING){
                        /*string type value*/

                        O.Add(NextStringFillVariable(varName));
                    }else if(c == SIGN_FUNCTION){

                        SharkScriptUtil.ExtendList<string>(O,NextFunctionDefination());
                        O.Add($"1END_DEFINE_FUNC_FILLVAR {varName}");
                    }else{
                        /* the invalid syntax */

                        throw SharkScriptInvalidSyntax.defaultErr;
                    }
                }
                char nextChar = characters.Dequeue();
                if(nextChar != CODE_SEP){
                    throw new SharkScriptInvalidSyntax("Shark语法错误，是否忘记写分号？");
                }
                if(characters.Peek() != FUNCTION_BODY_END){
                    return __NextFunctionDefineBody(O);
                }
                return O;
            }
            private bool IsVarHead(char _){
                return char.IsLetter(_) || _ == UNDERLINE;
            }
            public static bool ListElementEnding(char nextChar){
                /* 列表元素结尾 */

                return nextChar == LIST_END || nextChar == FUNCTION_PARAMS_SEP;
            }
            public static bool CodeSepEnding(char nextChar){

                return nextChar == CODE_SEP;
            }
            public static Tuple<string[],string> HandleSourceCode(string[] lines){
                /* 代码预处理器，将代码拆分为编译指令和控制代码两块，编译指令将直接在这里被转换为中间代码，
                其余的代码将会生成一个新的string对象(移除不必要的空格) */

                List<string> _IL = new List<string>();
                string buf = string.Empty;
                foreach(string line in lines){
                    if(line.StartsWith("#")){
                        _IL.Add(ParsePragma(line));
                    }else{
                        buf += HandleSingleLine(line.Trim());
                    }
                }
                return new Tuple<string[], string>(_IL.ToArray(),buf);
            }
            public static string ParsePragma(string line){
                /* 解析所有的编译指令并生成对应的中间代码 */

                string[] parts = SharkScriptUtil.SplitStringWithWhiteSpace(line,2);
                switch(parts[0].Trim()){
                    case PRAGMA_API:
                    return $"1LOAD_API {parts[1].Trim()}";
                    case PRAGMA_BIND:
                    return $"1BIND {parts[1].Trim()}";
                }
                throw new SharkBaseException("unknown pargma command");
            }
            public static string HandleSingleLine(string line){
                /*移除所有的换行符与非字符串空格*/

                string buf = string.Empty;
                bool inString = false;
                foreach(char c in line){
                    if(inString || (c != SPACE && c != EOL)){
                        buf += c.ToString();
                        if(c == VAR_STRING){
                            inString = !inString;
                        }
                    }
                }
                return buf;
            }
        }
    }

    namespace SharkVirtualMachine{
        /* the code parser would convert the source code into internal code */

        public class SharkAPI{
            /* the core function of shark */

            public static __shark_list SHARKAPI_MakeArgs(){
                /* create an empty list */

                return new __shark_list();
            }
            public static void SHARKAPI_PushArgs(__shark_list args,SharkObject O){
                /* push a parameters into target args */

                args.Append(O);
            }
            public static SharkObject SHARKAPI_MakeList(){
                /* create en empty list and pack it into a SharkObject */

                return new SharkObject(new __shark_list());
            }
            public static SharkObject SHARKAPI_Str2SkStr(string cnt){
                /* convert string type value to shark string */

                return new SharkObject(new __shark_value(cnt));
            }
            public static string SHARKAPI_SkStr2Str(SharkObject O){
                /* get the string type value from shark object */

                if(O.type != SharkType.VALUE){
                    throw SharkScriptTypeError.defaultErr;
                }
                __shark_value V = O.GetValue<__shark_value>();
                if(V.type != SharkValueType.STRING){
                    throw SharkScriptTypeError.defaultErr;
                }
                return V.GetValue<string>();
            }
            public static SharkObject SHARKAPI_Float2SkFloat(float value){
                /* convert float type value to shark float */

                return new SharkObject(new __shark_value(value));
            }
            public static float SHARKAPI_SkFloat2Float(SharkObject O){
                /* get the float type value from shark object */

                if(O.type != SharkType.VALUE){
                    throw SharkScriptTypeError.defaultErr;
                }
                __shark_value V = O.GetValue<__shark_value>();
                if(V.type != SharkValueType.FLOAT){
                    throw SharkScriptTypeError.defaultErr;
                }
                return V.GetValue<float>();
            }
            public static SharkObject SHARKAPI_Int2SkInt(int value){
                /* convert float type value to shark float */

                return new SharkObject(new __shark_value(value));
            }
            public static int SHARKAPI_SkInt2Int(SharkObject O){
                /* get the int type value from shark object */

                if(O.type != SharkType.VALUE){
                    throw SharkScriptTypeError.defaultErr;
                }
                __shark_value V = O.GetValue<__shark_value>();
                if(V.type != SharkValueType.INT){
                    throw SharkScriptTypeError.defaultErr;
                }
                return V.GetValue<int>();
            }
            public static SharkObject SHARKAPI_Obj2SkObj(object data){
                /* convert object type value to shark object */

                return new SharkObject(new __shark_value(data));
            }
            public static object SHARKAPI_SkObj2Obj(SharkObject O){
                if(O.type != SharkType.OBJ){
                    throw SharkScriptTypeError.defaultErr;
                }
                return O.GetValue<object>();
            }
            public static T SHARKAPI_SkObj2Obj<T>(SharkObject O){
                if(O.type != SharkType.OBJ){
                    throw SharkScriptTypeError.defaultErr;
                }
                return (T)O.GetValue<object>();
            }

        }

        public class SharkAPICollection{
            /*  this object will maintain a dictionary, it would bind cs function from the top level 
            to shark_function and expose them to shark script, this collection can be sured by pragma
            when FIND_FUNC operation command has been run, then the interpreter would search target 
            function in all SharkAPICollection loaded in interpreter, if no function has been found, 
            then NameError will be thrown */

            private Dictionary<string,__shark_function> apis;
            public SharkAPICollection(){
                apis = new Dictionary<string, __shark_function>();
            }
            public bool AddFunction(string name,__shark_function func){
                /* add a new shark function to here, if there is a function with same name
                then if will failed */

                if(apis.ContainsKey(name)){
                    return false;
                }
                apis.Add(name,func);
                return true;
            }
            public __shark_function Search(string name){
                /* try to get a function, if failed, then return null */

                __shark_function buf;
                if(apis.TryGetValue(name,out buf)){
                    return buf;
                }
                return null;
            }
        }

        public class SharkAssembly{
            /* this function will maintain a dictionary, and each element will save a SharkAPICollection 
            it will be initialized as a component of SharkInterpreter, and it can load a new SharkAPICollection 
            or remove it */

            private Dictionary<string,SharkAPICollection> __scope;
            public SharkAssembly(){
                /* intialize the __scope */

                __scope = new Dictionary<string, SharkAPICollection>();
                SharkAPICollection Global = new SharkAPICollection();
                __scope.Add("Global",Global);
            }
            public SharkAPICollection GetGlobalCollection(){
                //return __scope.GetValueOrDefault("Global");

                return GetCollection("Global");
            }
            public SharkAPICollection GetCollection(string apiName){
                /*找到指定的API集合*/

                SharkAPICollection apis;
                if(__scope.TryGetValue(apiName,out apis)){
                    return apis;
                }
                return null;
            }
            public void AddGlobalFunction(string name,__shark_function func){

                SharkAPICollection global = GetGlobalCollection();
                global.AddFunction(name,func);
            }
            public void LoadAPICollection(string apiName,SharkAPICollection collection){

                __scope.Add(apiName,collection);
            }
            public bool Contains(string collectionName){
                
                return __scope.ContainsKey(collectionName);
            }
            public __shark_function SearchFunction(string functionName){
                /* iterate all collection and search target function */

                foreach(string name in __scope.Keys){
                    __shark_function func = GetCollection(name).Search(functionName);
                    if(func != null){
                        return func;
                    }
                }
                return null;
            }
            public void Clear(){

                __scope.Clear();
            }
        }

        public class SharkScope{
            /* contained a dictionary used to save shark datas */

            Dictionary<string,SharkObject> __scope;

            public SharkScope(){

                __scope = new Dictionary<string, SharkObject>();
            }
            public SharkObject FindObject(string key){
                /*find target object according to a string key*/
                
                SharkObject buf;
                if(__scope.TryGetValue(key,out buf)){
                    return buf;
                };
                return null;
            }
            public void Remove(string key){
                /* remove target variable */

                __scope.Remove(key);
            }
            public void Save(string key,SharkObject value){

                __scope.Add(key,value);
            }
            public void Save(Tuple<string,SharkObject> tuple){

                __scope.Add(tuple.key,tuple.value);
            }
            public bool ContainsKey(string varName){
                /* check out if target name has been used */

                return __scope.ContainsKey(varName);
            }
            public void Clear(){
                /* clear all datas */

                __scope.Clear();
            }
        }

        public class SharkInterpreter{
            /* SharkInterpreter is a virtual machine used to run the IL code */

            public delegate SharkAPICollection GetSharkAPICollectionWithName(string name);

            private Stack<__shark_list> paramStack;
            private Stack<__shark_function> functionStack;
            private SharkAssembly assembly;
            private SharkScope field;
            private __shark_function functionDefination;
            private string functionName;
            private IL il;
            private SharkScriptLexer lexer;

            private string bindName;
            private bool isBinded;
            private GetSharkAPICollectionWithName findAPI;

            public SharkInterpreter(){
                /* initialize the shark_interpreter */

                paramStack = new Stack<__shark_list>();
                functionStack = new Stack<__shark_function>();
                assembly = new SharkAssembly();
                field = new SharkScope();
                lexer = new SharkScriptLexer();
                InitializeIL();
            }
            public void Run(string[] script){
                /* 从文件中刚读取的完整的脚本 */

                script = SharkScriptUtil.RemoveComment(SharkScriptUtil.HandleSharkScript(script));
                RunILCode(lexer.GenerateILCode(script,false));
            }
            public void Erase(){
                /* clear all the datas */

                field.Clear();
            }
            public void RunILCode(Queue<string> lines){
                /* run the il code from il list */

                int size = lines.Count;
                for(int i = 0;i < size;i ++){
                    string line = lines.Dequeue();
                    il.RunILCode(line);
                }
            }
            public void SetAPIFinder(GetSharkAPICollectionWithName getter){
                /* 设置新的API搜寻器 */

                findAPI = getter;
            }
            private void InitializeIL(){
                /* load all the IL code of current interpreter */

                il = new IL();

                /* load all functions with no args */
                IL.OP_NOARGS buf0;
                buf0 = OP_MAKE_ARGSLIST;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"MAKE_LIST",(Delegate)buf0.Clone());
                buf0 = OP_DOCALL;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"DOCALL",(Delegate)buf0.Clone());
                buf0 = OP_DOCALL_FILL_PARAM;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"DOCALL_FILL_PARAM",(Delegate)buf0.Clone());
                buf0 = OP_END_DEFINE_FUNC;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"END_DEFINE_FUNC",(Delegate)buf0.Clone());
                buf0 = OP_END_DEFINE_FUNC_INLIST;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"END_DEFINE_FUNC_INLIST",(Delegate)buf0.Clone());
                buf0 = OP_FILL_PARAM;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"FILL_PARAM",(Delegate)buf0.Clone());
                buf0 = OP_ERASE;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"CLR",(Delegate)buf0.Clone());
                buf0 = OP_DEFINE_FUNC_NULLNAME;
                il.AddFunction(IL.OP_TYPE._SIGN_NOARGS,"DEFINE_FUNC_NULLNAME",(Delegate)buf0.Clone());


                /* load all functions with one args */
                IL.OP_ONEARGS buf1;
                buf1 = OP_FIND_FUNC;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"FIND_FUNC",(Delegate)buf1.Clone());
                buf1 = OP_PUSH_STRING;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"PUSH_STRING",(Delegate)buf1.Clone());
                buf1 = OP_PUSH_INT;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"PUSH_INT",(Delegate)buf1.Clone());
                buf1 = OP_PUSH_FLOAT;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"PUSH_FLOAT",(Delegate)buf1.Clone());
                buf1 = OP_PUSH_VAR;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"PUSH_VAR",(Delegate)buf1.Clone());
                buf1 = OP_DOCALL_FILL;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"DOCALL_FILL",(Delegate)buf1.Clone());
                buf1 = OP_DIM;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"DIM",(Delegate)buf1.Clone());
                buf1 = OP_DEFINE_FUNC;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"DEFINE_FUNC",(Delegate)buf1.Clone());
                buf1 = OP_ADDILCODE;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"ADDILCODE",(Delegate)buf1.Clone());
                buf1 = OP_ADDILCODE_PUSHVAR;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"ADDILCODE_PUSHVAR",(Delegate)buf1.Clone());
                buf1 = OP_END_DEFINE_FUNC_FILLVAR;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"END_DEFINE_FUNC_FILLVAR",(Delegate)buf1.Clone());
                buf1 = OP_FILL_LIST;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"FILL_LIST",(Delegate)buf1.Clone());
                buf1 = OP_LOADAPI;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"LOAD_API",(Delegate)buf1.Clone());
                buf1 = OP_BIND;
                il.AddFunction(IL.OP_TYPE._SIGN_ONEARGS,"BIND",(Delegate)buf1.Clone());

                IL.OP_TWOARGS buf2;
                buf2 = OP_FILL_STRING;
                il.AddFunction(IL.OP_TYPE._SIGN_TWOARGS,"FILL_STRING",(Delegate)buf2.Clone());
                buf2 = OP_FILL_FLOAT;
                il.AddFunction(IL.OP_TYPE._SIGN_TWOARGS,"FILL_FLOAT",(Delegate)buf2.Clone());
                buf2 = OP_FILL_INT;
                il.AddFunction(IL.OP_TYPE._SIGN_TWOARGS,"FILL_INT",(Delegate)buf2.Clone());
                buf2 = OP_FILL_VAR;
                il.AddFunction(IL.OP_TYPE._SIGN_TWOARGS,"FILL_VAR",(Delegate)buf2.Clone());
            }
            public SharkAPICollection GetCurrentAPICollection(){
                /* 找到当前的API集合，用于存放在当前脚本中存放的所有的函数 */

                if(isBinded){
                    return assembly.GetCollection(bindName);
                }
                return assembly.GetGlobalCollection();
            }
            public void LoadAPI(string name,SharkAPICollection collection){
                /* load a new collection */

                assembly.LoadAPICollection(name,collection);
            }
            public void OP_LOADAPI(string apiName){
                /* 在assembly中加载新的作用域 */

                if(!assembly.Contains(apiName)){
                    assembly.LoadAPICollection(apiName,findAPI(apiName));
                }
            }
            public void OP_BIND(string bindName){
                /* 将当前的函数声明绑定到指定的绑定名当中,如果绑定名错误，则指定绑定名错误 */

                if(assembly.Contains(bindName)){
                    throw new SharkBaseException("绑定名已经被使用");
                }
                SharkAPICollection apis = new SharkAPICollection();
                assembly.LoadAPICollection(bindName,apis);
                this.bindName = bindName;
                isBinded = true;
            }
            public void OP_ERASE(){
                /* 清空栈区和作用域 */
                
                Erase();
            }
            public void OP_DEFINE_FUNC(string functionName){
                /* define a new function */

                functionDefination = new __shark_function();
                this.functionName = functionName;
            }
            public void OP_DEFINE_FUNC_NULLNAME(){
                /*定义一个匿名函数，该函数只能赋值给一个变量或者填入列表 */

                functionDefination = new __shark_function();
                this.functionName = null;
            }
            public void OP_ADDILCODE(string line){

                functionDefination.AddILCode(line);
            }
            public void OP_ADDILCODE_PUSHVAR(string line){
                /* 定义一个函数时的特殊命令，如果命令中包含PUSHVAR命令，那么
                被push的变量必须要复制到目标函数自己的作用域中 */

                string varName = IL.GetPushVarName(line);
                if(field.ContainsKey(varName)){
                    if(!functionDefination.HasFunctionField()){
                        functionDefination.InitializeField();
                    }
                    functionDefination.AddVariable(varName,field.FindObject(varName));
                    functionDefination.AddILCode(line);
                    return;
                }
                throw SharkScriptNameError.defaultErr;
            }
            public void OP_END_DEFINE_FUNC(){
                /* 结束函数的定义，目标函数将保存在全局assembly中 */

                GetCurrentAPICollection().AddFunction(this.functionName,functionDefination.Clone());
                functionDefination = null;
            }
            public void OP_END_DEFINE_FUNC_INLIST(){
                /* 匿名函数对象将作为一个对象传递给目标列表,目标列表有可能是
                参数列表或者普通列表 */

                SharkObject func = new SharkObject(functionDefination.Clone());
                paramStack.Peek().Append(func);
                functionDefination = null;
            }
            public void OP_END_DEFINE_FUNC_FILLVAR(string varName){
                /* 匿名函数对象将作为一个对象保存在目标变量中 */

                SharkObject func = new SharkObject(functionDefination.Clone());
                field.FindObject(varName).Rewrite(func);
                functionDefination = null;
            }
            public void OP_FIND_FUNC(string functionName){
                /* FIND_FUNC is a IL operation, when you call it, it will search 
                the function in global api group and other api group */

                __shark_function func = assembly.SearchFunction(functionName);
                if(func == null){
                    throw SharkScriptNameError.defaultErr;
                }
                functionStack.Push(func);
            }
            public void OP_MAKE_ARGSLIST(){
                /* MAKE_ARGSLIST is a IL opreation, it will make a new args list 
                and saved into paramStack */

                __shark_list list = new __shark_list();
                paramStack.Push(list);
            }
            public void OP_PUSH_STRING(string strValue){
                /* push a string value into target paramlist */

                paramStack.Peek().Append(SharkAPI.SHARKAPI_Str2SkStr(strValue));
            }
            public void OP_PUSH_FLOAT(string floatValue){
                /* push a float value into target paramlist */

                float _ = float.Parse(floatValue);
                paramStack.Peek().Append(SharkAPI.SHARKAPI_Float2SkFloat(_));
            }
            public void OP_PUSH_INT(string intValue){
                /* push a int value into target paramList */

                int _ = int.Parse(intValue);
                paramStack.Peek().Append(SharkAPI.SHARKAPI_Int2SkInt(_));
            }
            public void OP_DOCALL(){
                /* take a function and target list from stack and call it */

                __shark_function func = functionStack.Peek();
                __shark_list parameters = paramStack.Pop();
                if(func.type == __shark_function.FuncType.CS){
                    func.Call(parameters);
                }else{
                    func.Call(il);
                }
                functionStack.Pop();
            }
            public void OP_DOCALL_FILL_PARAM(){
                /* call target function and push the result into paramList */

                __shark_function func = functionStack.Peek();
                __shark_list parameters = paramStack.Pop();
                if(func.type == __shark_function.FuncType.CS){
                    paramStack.Peek().Append(func.Call(parameters));
                }else{
                    paramStack.Peek().Append(func.Call(il));
                }
                functionStack.Pop();
            }
            public void OP_DIM(string varName){
                /* declear a new variable */

                if(!field.ContainsKey(varName)){
                    SharkObject O = SharkObject.None;
                    field.Save(varName,O);   
                }
            }
            public void OP_DOCALL_FILL(string varName){
                /* fill the result of target function into target variable */

                __shark_function func = functionStack.Peek();
                __shark_list parameters = paramStack.Pop();
                if(func.type == __shark_function.FuncType.CS){
                    field.FindObject(varName).Rewrite(func.Call(parameters));
                }else{
                    field.FindObject(varName).Rewrite(func.Call(il));
                }
                functionStack.Pop();
            }
            public void OP_PUSH_VAR(string varName){
                /* push a variable of field into current paramList 
                如果在一个函数定义的时候遇到了该指令的话，将该变量复制到
                函数自己的域当中，当执行函数的时候，先在函数的本地域当中搜索
                目标变量，如果搜索失败，则在全局域中搜索。如果均搜索不到，则引发NameError */

                SharkObject O;
                if(!field.ContainsKey(varName)){
                    int funcLvl = functionStack.Count;
                    __shark_function F;
                    Stack<__shark_function> tmpStk = new Stack<__shark_function>();
                    while(funcLvl > 0){
                        F = functionStack.Peek();
                        if(F.HasFunctionField() && F.ContainsVariable(varName)){
                            O = F.FindObject(varName);
                            paramStack.Peek().Append(O);
                            PushFunctions(tmpStk);
                            return;
                        }else{
                            tmpStk.Push(functionStack.Pop());
                            funcLvl -= 1;
                        }
                    }
                    throw SharkScriptNameError.defaultErr;
                }
                O = field.FindObject(varName);
                paramStack.Peek().Append(O);
            }
            public void OP_FILL_PARAM(){
                /* pop a value from stack and save into the parameters 
                note that: this function only used for type __shark_list */

                __shark_list __params = paramStack.Pop();
                paramStack.Peek().Append(new SharkObject(__params));
            }
            public void OP_FILL_LIST(string varName){
                /* pop a value from stack and save into target variable */

                SharkObject O = new SharkObject(paramStack.Pop());
                field.FindObject(varName).Rewrite(O);
            }
            public void OP_FILL_STRING(string varName,string content){
                /* 将一个字符串覆写到变量中 */

                field.FindObject(varName).Rewrite(SharkAPI.SHARKAPI_Str2SkStr(content));
            }
            public void OP_FILL_FLOAT(string varName,string fValue){
                /* 将一个浮点类型值覆写到变量中 */

                float _ = float.Parse(fValue);
                field.FindObject(varName).Rewrite(SharkAPI.SHARKAPI_Float2SkFloat(_));
            }
            public void OP_FILL_INT(string varName,string intValue){

                int _ = int.Parse(intValue);
                field.FindObject(varName).Rewrite(SharkAPI.SHARKAPI_Int2SkInt(_));
            }
            public void OP_FILL_VAR(string dest,string src){

                field.FindObject(dest).Rewrite(field.FindObject(src));
            }
            public bool IsCallFunctionNow(){
                /* check if is calling a function now */

                return functionStack.Count > 0;
            }
            public void PushFunctions(Stack<__shark_function> funcs){
                /* 将取出来的函数再压栈 */

                while(funcs.Count > 0){
                    functionStack.Push(funcs.Pop());
                }
            }

        }

        public class IL{
            /* Shark Internal Code */

            public enum OP_TYPE{
                _SIGN_NOARGS,
                _SIGN_ONEARGS,
                _SIGN_TWOARGS
            }
            public const char ILCODE_SEP = ' ';

            public delegate void OP_NOARGS();
            public delegate void OP_ONEARGS(string parameter);
            public delegate void OP_TWOARGS(string parameter1,string parameter2);

            public Dictionary<string,OP_NOARGS> API_NOARGS;
            public Dictionary<string,OP_ONEARGS> API_ONEARGS;
            public Dictionary<string,OP_TWOARGS> API_TWOARGS;

            public IL(){
                /* initialize all the operation of SharkInterpreter */

                API_NOARGS = new Dictionary<string, OP_NOARGS>();
                API_ONEARGS = new Dictionary<string, OP_ONEARGS>();
                API_TWOARGS = new Dictionary<string, OP_TWOARGS>();
            }
            public void AddFunction(OP_TYPE type,string name,Delegate func){

                switch(type){
                    case OP_TYPE._SIGN_NOARGS:
                    API_NOARGS.Add(name,(OP_NOARGS)func);
                    break;
                    case OP_TYPE._SIGN_ONEARGS:
                    API_ONEARGS.Add(name,(OP_ONEARGS)func);
                    break;
                    case OP_TYPE._SIGN_TWOARGS:
                    API_TWOARGS.Add(name,(OP_TWOARGS)func);
                    break;
                }
            }
            public void RunILCode(string line){
                /* run a piece of IL code */

                OP_TYPE type = (OP_TYPE)int.Parse(line.Substring(0,1));
                string code = line.Substring(1,line.Length - 1);
                switch(type){
                    case OP_TYPE._SIGN_NOARGS:
                    RunILCodeNoArgs(code);
                    break;
                    case OP_TYPE._SIGN_ONEARGS:
                    RunILCodeOneArgs(code);
                    break;
                    case OP_TYPE._SIGN_TWOARGS:
                    RunILCodeTwoArgs(code);
                    break;
                }
            }
            private void RunILCodeNoArgs(string code){
                /* the code has no args, just run it */

                SharkScriptUtil.ExtractObjectFromDict<OP_NOARGS>(code,API_NOARGS)();
            }
            private void RunILCodeOneArgs(string code){
                /* the code has one args, so we split the code into two parts */

                string[] G = SharkScriptUtil.SplitStringWithWhiteSpace(code,2);
                SharkScriptUtil.ExtractObjectFromDict<OP_ONEARGS>(G[0],API_ONEARGS)(G[1]);
            }
            private void RunILCodeTwoArgs(string code){

                string[] G = SharkScriptUtil.SplitStringWithWhiteSpace(code,3);
                SharkScriptUtil.ExtractObjectFromDict<OP_TWOARGS>(G[0],API_TWOARGS)(G[1],G[2]);
            }
            public static string GetPushVarName(string code){
                /* 当目标函数是一个var变量时，获取该var的变量名
                执行该函数前必须要求目标参数是一个PUSH_VAR的有效指令 */

                return SharkScriptUtil.SplitStringWithWhiteSpace(code,2)[1];
            }
        }
    }

    public enum SharkValueType{

        STRING,
        INT,
        FLOAT,
        //BOOL
    }

    public enum SharkType{
        /* the data type of shark */

        VALUE,
        OBJ,
        LIST,
        FUNC,
        NONE
    }
    
    public class SharkObject{
        /* all the value in the Shark would yield from SharkObject */

        public static SharkObject None = new SharkObject();

        SharkType __type;
        object value;

        public SharkType type{
            get{return __type;}
        }
        public SharkObject(){
            /* build up a new shark object */

            __type = SharkType.NONE;
            value = null;
        }
        public SharkObject(__shark_value value){
            __type = SharkType.VALUE;
            this.value = value;
        }
        public SharkObject(__shark_function func){
            __type = SharkType.FUNC;
            this.value = func;
        }
        public SharkObject(__shark_list list){
            __type = SharkType.LIST;
            this.value = list;
        }
        public SharkObject(object data){
            __type = SharkType.OBJ;
            this.value = data;
        }
        public T GetValue<T>(){

            return (T)this.value;
        }
        public void Rewrite(SharkObject O){

            __type = O.type;
            switch(O.type){
                case SharkType.OBJ:
                this.value = O.GetValue<object>();
                break;
                case SharkType.FUNC:
                this.value = O.GetValue<__shark_function>();
                break;
                case SharkType.LIST:
                this.value = O.GetValue<__shark_list>();
                break;
                case SharkType.VALUE:
                this.value = O.GetValue<__shark_value>();
                break;
                case SharkType.NONE:
                this.value = null;
                break;
            }
        }
        public void Rewrite(object data){
            __type = SharkType.OBJ;
            this.value = data;
        }
        public void Rewrite(__shark_list list){
            __type = SharkType.LIST;
            this.value = list;
        }
        public void Rewrite(__shark_function func){
            __type = SharkType.FUNC;
            this.value = func;
        }
        public void Rewrite(__shark_value value){
            __type = SharkType.VALUE;
            this.value = value;
        }
        public void Rewrite(){
            __type = SharkType.NONE;
            this.value = null;
        }
    }

    public delegate SharkObject SharkFunction(__shark_list args);

    public class __shark_function{
        /* define the shark function, the shark function 
        has two type, one is the cs function, it will be saved in the type of delegate
        and another is IL code queue, in this type, function must be called with 
        interpreter */

        public enum FuncType{

            CS,
            SHARK
        }

        private SharkFunction func;
        private Queue<string> ilcodes;
        private SharkScope funcField;
        FuncType __type;
        public __shark_function(SharkFunction func){

            __type = FuncType.CS;
            this.func = func;
            funcField = null;
        }
        public __shark_function(){

            __type = FuncType.SHARK;
            ilcodes = new Queue<string>();
        }
        public __shark_function(string[] codes,SharkScope filed){

            __type = FuncType.SHARK;
            ilcodes = new Queue<string>();
            foreach(string line in codes){
                ilcodes.Enqueue(line);
            }
            this.funcField = filed;
        }
        public FuncType type{

            get{return __type;}
        }
        public __shark_function(string[] codes){

            __type = FuncType.SHARK;
            ilcodes = new Queue<string>();
            foreach(string line in codes){
                ilcodes.Enqueue(line);
            }
        }
        public void InitializeField(){
            /* 初始化函数的变量空间 */

            funcField = new SharkScope();
        }
        public void AddVariable(string name,SharkObject variable){
            /* 当该函数引用了一个内部的变量的时候，将该变量引用到SharkScope中 */

            funcField.Save(name,variable);
        }
        public bool HasFunctionField(){
            /* 检查函数有没有自己的作用域 */

            return funcField != null;
        }
        public SharkObject FindObject(string key){
            /* 找到目标变量 */

            return funcField.FindObject(key);
        }
        public bool ContainsVariable(string key){
            /* 检查是否存在目标变量 */

            return funcField.ContainsKey(key);
        }

        public __shark_function Clone(){

            
            if(HasFunctionField()){
                return new __shark_function(ilcodes.ToArray(),funcField);
            }
            return new __shark_function(ilcodes.ToArray());
        }
        public void AddILCode(string line){

            ilcodes.Enqueue(line);
        }
        public SharkObject Call(__shark_list args){

            return this.func(args);
        }
        public SharkObject Call(){

            return this.func(__shark_list.emptyList);
        }
        public SharkObject Call(IL il){
            /* the function define by */

            while(ilcodes.Count > 0){
                string line = ilcodes.Dequeue();
                il.RunILCode(line);
            }
            return SharkObject.None;
        }
    }

    public class __shark_list{
        /* shark list can contained all type value of shark */
        
        public static __shark_list emptyList = new __shark_list();

        private List<SharkObject> list;
        public __shark_list(){
            list = new List<SharkObject>();
        }
        public int Length{
            get{return list.Count;}
        }
        public void Append(SharkObject item){
            /* append a new object in the last position */

            list.Add(item);
        }
        public void Remove(SharkObject item){
            /* remove target item */

            list.Remove(item);
        }
        public SharkObject Index(int idx){
            /* get value of given index */

            if(idx >= list.Count || idx < 0){
                throw SharkScriptIndexError.defaultErr;
            }
            return list.ToArray()[idx];
        }

        public void Clear(){
            /* remove all datas */

            list.Clear();
        }
    }

    public class __shark_value{
        /* contain a simple value,three types including 
        string,int and float */

        private static SharkScriptTypeError __error = new SharkScriptTypeError("unknown type");
        private static Type[] __type2index = new Type[]{
            typeof(System.String),
            typeof(System.Int32),
            typeof(System.Single)
        };
        private SharkValueType __value_type;
        private object __value;

        public SharkValueType type{
            get{return __value_type;}
        }

        public __shark_value(object data){
            /* initialize __value with given value
            and set the __value_type dynamicly according to given value
            note that target value must be int,float or string type
            otherwise a RuntimeError will be thrown */

            int __type = Array.IndexOf(__type2index,data.GetType());
            if(__type == -1){
                throw __error;
            }else{
                __value_type = (SharkValueType)__type;
                __value = data;
            }
        }
        public T GetValue<T>(){
            /* get the value saved in this variable */

            return (T)__value;
        }
        public override string ToString()
        {
            return $"{__value_type.ToString()}:{__value}";
        }
    }
}