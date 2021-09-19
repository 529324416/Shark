using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Shark.SharkVirtualMachine;
using Shark.SharkLexer;
using Shark.SharkCore;
using Shark.SharkLib;

namespace Shark{

    public enum SharkErrorType{

        // 语法异常
        SYNTAX_EXPECT_RIGHT_PAREN,                  // ) 缺失
        SYNTAX_EXPECT_RIGHT_BRACKETS,               // ] 缺失
        
        // 操作异常
        OPERATION_INDEX_OUTOF_RANGE,                // 数组范围超出

        // 其他异常
    }
    public delegate void RaiseError(int line);

    public class SharkError: Exception{
        public SharkError(string errInfo):base(errInfo){}

        public static Dictionary<SharkErrorType, RaiseError> errors;
        public static void Initialize(){

            errors = new Dictionary<SharkErrorType, RaiseError>();
        }
        public static void SYNTAX_EXPECT_RIGHT_PAREN(int line){
            throw new SharkSyntaxError($"expect \")\" at line {line.ToString()}");
        }
    }
    public class SharkSyntaxError: SharkError{
        public SharkSyntaxError(string errInfo):base(errInfo){}
        public override string Message => $"SharkSyntaxError: {base.Message}";
    }
    public class SharkNameError: SharkError{
        public SharkNameError(string errInfo):base(errInfo){}
        public override string Message => $"SharkNameError: {base.Message}";
    }
    public class SharkOperationError: SharkError{
        public SharkOperationError(string errInfo):base(errInfo){}
        public override string Message => $"SharkOperationError: {base.Message}";
    }
    public class SharkIndexOutOfArrayError: SharkError{
        public SharkIndexOutOfArrayError(string errInfo):base(errInfo){}
        public override string Message => $"SharkIndexOutOfArrayError: {base.Message}";
    }
    public class SharkArgumentError:SharkError{
        public SharkArgumentError(string errInfo):base(errInfo){}
        public override string Message => $"SharkArgumentError: {base.Message}";
    }
    public class SharkTypeError:SharkError{
        public SharkTypeError(string errInfo):base(errInfo){}
        public override string Message => $"SharkTypeError: {base.Message}";
    }

    public static class SharkUtils{

        ///<summary>
        /// 读取本地的文件，如果文件不存在，则返回null
        ///</summary>
        public static string ReadFile(string filepath){

            string _Ret = null;
            if(File.Exists(filepath)){
                FileStream file = File.OpenRead(filepath);
                StreamReader reader = new StreamReader(file);
                _Ret = reader.ReadToEnd();
                reader.Close();
                file.Close();
            }
            return _Ret;
        }

        ///<summary>
        /// 将文本内容写入本地文件，如果文件不存在，返回false
        ///</summary>
        public static void WriteFile(string filepath, string content){

            FileStream file = File.OpenWrite(filepath);
            StreamWriter writer = new StreamWriter(file);
            writer.Write(content);
            writer.Close();
            file.Close();
        }
    }


    namespace SharkLexer{

        public enum SkTokenType{

            TOKEN_UNKNOWN,
            
            // data types
            TOKEN_ID,
            TOKEN_INT,
            TOKEN_FLOAT,
            TOKEN_STRING,
            TOKEN_TRUE,
            TOKEN_FALSE,

            // system keywords
            TOKEN_FUNC,
            TOKEN_IF,
            TOKEN_ELSE,
            TOKEN_WHILE,
            TOKEN_FOR,
            TOKEN_BREAK,
            TOKEN_CONTINUE,
            TOKEN_RETURN,
            TOKEN_NULL,

            TOKEN_THIS,
            TOKEN_IMPORT,
            TOKEN_GLOBAL,

            // split sign
            TOKEN_COMMA,                    //,
            TOKEN_COLON,                    //:
            TOKEN_SEMIC_COLON,              //;
            TOKEN_LEFT_PAREN,               //(
            TOKEN_RIGHT_PAREN,              //) 
            TOKEN_LEFT_BRACKETS,            //[
            TOKEN_RIGHT_BRACKETS,           //]
            TOKEN_LEFT_BARCE,               //{
            TOKEN_RIGHT_BRACE,              //}
            TOKEN_DOT,                      //.

            // common operators
            TOKEN_ADD,                      // +
            TOKEN_SUB,                      // -
            TOKEN_MUL,                      // *
            TOKEN_DIV,                      // /
            TOKEN_MOD,                      // %
            TOKEN_POW,                      // ^
            TOKEN_ASSIGN,                   // =
            TOKEN_ASSIGN_ADD,               // +=
            TOKEN_ASSIGN_SUB,               // -=
            TOKEN_ASSIGN_MUL,               // *=
            TOKEN_ASSIGN_DIV,               // /=
            TOKEN_ASSIGN_MOD,               // %=

            //bit operators
            TOKEN_BIT_AND,                  // &
            TOKEN_BIT_OR,                   // |
            TOKEN_BIT_NOT,                  // ~
            TOKEN_BIT_SHIFT_RIGHT,          // >>
            TOKEN_BIT_SHIFT_LEFT,           // <<

            // logic operators
            TOKEN_LOGIC_AND,                // &&
            TOKEN_LOGIC_OR,                 // ||
            TOKEN_LOGIC_NOT,                // !

            // compare opeators
            TOKEN_EQUAL,
            TOKEN_NOT_EQUAL,
            TOKEN_GREATER,
            TOKEN_GREATER_EQUAL,
            TOKEN_LESS,
            TOKEN_LESS_EQUAL,

            TOKEN_EOF,
            TOKEN_MARCO,                    // #
        }

        public enum TokenModel{

            MODEL_TOKEN_IS_SYSTEMWORDS,
            MODEL_TOKEN_IS_VARIABLE_NAME,
            MODEL_TOKEN_IS_CONSTANT,
            MODEL_TOKEN_IS_UNIVERSAL,
        }

        public class SharkToken{

            public readonly TokenModel tokenModel;
            public readonly SkTokenType tokenType;
            public readonly string tokenContent;
            public SharkToken(SkTokenType type, string content, TokenModel model){

                tokenType = type;
                tokenContent = content;
                tokenModel = model;
            }
            public override string ToString()
            {
                return $"{tokenContent}({tokenType.ToString()})";
            }
            public static bool operator==(SharkToken left, SkTokenType type){
                return left.tokenType == type;
            }
            public static bool operator!=(SharkToken left, SkTokenType type){
                return left.tokenType != type;
            }
            public override bool Equals(object other)
            {
                return base.Equals(other);
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
        public class SharkTokenUniversal: SharkToken{

            public readonly int line;
            public SharkTokenUniversal(int line, string content, SkTokenType type):base(type, content, TokenModel.MODEL_TOKEN_IS_UNIVERSAL){
                this.line = line;
            }
            public SharkTokenUniversal(int line, string content, SkTokenType type, TokenModel model):base(type, content, model){
                this.line = line;
            }
            public override string ToString()
            {
                return $"{base.ToString()} at line {line.ToString()}";
            }
        }


        public class SharkTokenConstant: SharkTokenUniversal{

            public readonly SkObject constant;       // 常量具体的值
            public int constID;
            public SharkTokenConstant(List<SkObject> constTable, SkObject con, int lineNo, string content, SkTokenType type):
            base(lineNo, content, type, TokenModel.MODEL_TOKEN_IS_CONSTANT){

                constant = con;
                constID = constTable.Count;
                for(int i = 0; i < constTable.Count; i ++){
                    if(SharkAPI.ConstantEquals(constTable[i], con)){
                        constID = i;
                        break; 
                    }
                }
                constTable.Add(con);
            }
            public override string ToString()
            {
                return $"{base.ToString()} at line {line.ToString()}";
            }
        }
        public class SharkTokenVariable: SharkTokenUniversal{

            public readonly string variableName;
            public readonly int variableID;
            public SharkTokenVariable(int ID, int lineNo, string content, SkTokenType type):
            base(lineNo, content, type, TokenModel.MODEL_TOKEN_IS_VARIABLE_NAME){

                variableName = content;
                variableID = ID;
            }
            public override string ToString(){
                return $"{base.ToString()} at line {line.ToString()}";
            }
        }



        // 记录一个Token的具体内容
        public struct SkToken{

            public readonly SkTokenType tokenType;
            public readonly string tokenContent;
            public readonly int line;

            public SkToken(SkTokenType type, string content, int lineNo = 0){

                tokenType = type;
                tokenContent = content;
                line = lineNo;
            }
            public override string ToString()
            {
                return $"{tokenContent}({tokenType.ToString()}) at line {line.ToString()}";
            }
            public static bool operator==(SkToken other, SkTokenType type){
                return other.tokenType == type;
            }
            public static bool operator!=(SkToken other, SkTokenType type){
                return other.tokenType != type;
            }
            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        // 已经被征用的系统关键字
        public static class SharkLexerCore{

            public static char eof = '\0';
            public static char space = ' ';
            public static char tab = '\t';
            public static char newLine = '\n';
            public static char carriageReturn = '\r';

            public delegate bool charChekcer(char value);

            public static bool isSpace(char value){
                return value == space || value == tab || value == carriageReturn;
            }
            public static bool isNewLine(char value){
                return value == newLine;
            }
            public static bool isHexChar(char value){

                if(value >= '0' && value <= '9'){
                    return true;
                }else if(value >= 'a' && value <= 'f'){
                    return true;
                }else if(value >= 'A' && value <= 'F'){
                    return true;
                }
                return false;
            }
            public static bool isOctChar(char value){
                return value >= '0' && value <= '7';
            }
            public static bool isBinChar(char value){
                return value == '0' || value == '1';
            }

            public static SharkToken[] systemKeywords = new SharkToken[]{
                
                new SharkToken(SkTokenType.TOKEN_FUNC, "function", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_IF, "if", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_ELSE, "else", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_WHILE, "while", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_FOR, "for", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_BREAK, "break", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_CONTINUE, "continue", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_RETURN, "return", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_THIS, "this", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_IMPORT, "import", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_GLOBAL, "global", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
                new SharkToken(SkTokenType.TOKEN_UNKNOWN, null, TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
            };

            public static char[] castList = new char[]{
                '0',
                'a',
                'b',
                'f',
                'n',
                'r',
                't',
                'u',
                '\"',
                '\\',
            };


            ///<summary>
            /// 检查目标字符串是ID还是关键字
            ///</summary>

            ///<summary>
            /// 检查目标字符是否是正确的转义字符
            ///</summary>
            public static bool isCastChar(char C){
                foreach(char _C in castList){
                    if(C == _C){
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Shark词法分析器
        /// </summary>
        public class SkLexer{

            public List<SkObject> constTable;
            public Dictionary<int, string> SymbolTable;
            public Dictionary<string, int> ReverseSymbolTable;
            public Dictionary<int, SkObject> variableTable;

            private string sourceCode;
            private char[] sourceCodeCharList;
            private int currentPos;
            private int currentLine;

            private char currentChar{
                get => sourceCode[currentPos];
            }
            private char charAfterCurrent{
                get => sourceCode[currentPos + 1];
            }
            private bool hasMore{
                get => currentPos < sourceCode.Length;
            }

            ///<summary>
            ///获取下一个字符(并将当前位置递增)
            ///</summary>
            private char nextChar{

                get{
                    char _Ret = currentChar;
                    currentPos ++;
                    return _Ret;
                }
            }

            ///<summary>
            ///找到下一个有用的字符(跳过换行符与空格)
            ///</summary>
            private char nextUsefulChar{

                get{
                    char buf;
                    while(hasMore){
                        buf = nextChar;
                        if(SharkLexerCore.isSpace(buf)){
                            continue;
                        }else if(SharkLexerCore.isNewLine(buf)){
                            currentLine ++;
                        }else{
                            return buf;
                        }
                    }
                    return SharkLexerCore.eof;
                }
            }
            
            public SharkToken isIdOrKeyword(string content, int lineNo){
                foreach(SharkToken keyword in SharkLexerCore.systemKeywords){
                    if(content == keyword.tokenContent){
                        return keyword;
                    }
                }
                if(content == "null"){

                    return new SharkTokenConstant(constTable, SkNull.NULL, lineNo, content, SkTokenType.TOKEN_NULL);
                }else if(content == "true"){

                    return new SharkTokenConstant(constTable, SkBool.TRUE, lineNo, content, SkTokenType.TOKEN_TRUE);
                }else if(content == "false"){

                    return new SharkTokenConstant(constTable, SkBool.FALSE, lineNo, content, SkTokenType.TOKEN_FALSE);
                }else{
                    if(ReverseSymbolTable.ContainsKey(content)){
                        //如果符号表中已经存在目标符号了

                        return new SharkTokenVariable(ReverseSymbolTable[content], lineNo, content, SkTokenType.TOKEN_ID);
                    }else{
                        int currentID = SymbolTable.Count;
                        ReverseSymbolTable.Add(content, currentID);
                        SymbolTable.Add(currentID, content);
                        return new SharkTokenVariable(currentID, lineNo, content, SkTokenType.TOKEN_ID);
                    }
                }
            }

            public SkLexer(){
                Initialize();
            }
            public SkLexer(string sourceCode){
                LoadSourceCode(sourceCode);
                Initialize();
            }
            public void Initialize(){

                ReverseSymbolTable = new Dictionary<string, int>();
                SymbolTable = new Dictionary<int, string>();
                constTable = new List<SkObject>();

                foreach(SkPack pack in SharkCommonLib.commonLib){
                    ReverseSymbolTable.Add(pack.symbol, pack.id);
                    SymbolTable.Add(pack.id, pack.symbol);
                }
            }
            

            ///<summary>
            ///加载新的脚本源代码
            ///</summary>
            public void LoadSourceCode(string source){
                sourceCode = source;
                sourceCodeCharList = source.ToCharArray();
                currentPos = 0;
                currentLine = 1;
            }


            ///<summary>
            /// 加载源代码后解析为Token队列
            ///</summary>
            public Queue<SharkToken> Parse(string source){

                LoadSourceCode(source);
                Queue<SharkToken> output = new Queue<SharkToken>();
                SharkToken buf;
                while(true){
                    buf = getNextToken();
                    output.Enqueue(buf);
                    if(buf.tokenType == SkTokenType.TOKEN_EOF){
                        break;
                    }
                }
                return output;
            }
            ///<summary>
            /// 解析现有的代码并生成Token序列
            ///</summary>
            public Queue<SharkToken> Parse(){

                currentPos = 0;
                currentLine = 1;
                Queue<SharkToken> output = new Queue<SharkToken>();
                SharkToken buf;
                while(true){
                    buf = getNextToken();
                    output.Enqueue(buf);
                    if(buf.tokenType == SkTokenType.TOKEN_EOF){
                        break;
                    }
                }
                return output;
            }
            public SharkScript generateScript(){
                Dictionary<int, SkObject> conTable = new Dictionary<int, SkObject>();
                for(int i = 0; i < constTable.Count; i ++){
                    conTable.Add(i, constTable[i]);
                }
                // -- 检查常量表
                // foreach(KeyValuePair<int, string> value in SymbolTable){
                //     Console.WriteLine($"{value.Key.ToString()}: {value.Value}");
                // }
                // -- 检查常量表结束
                return new SharkScript(SymbolTable, conTable);
            }
            private SharkToken makeToken(SkTokenType type){
                return new SharkTokenUniversal(currentLine, null, type);
            }
            private SharkToken makeToken(SkTokenType type, string content){
                return new SharkTokenUniversal(currentLine, content, type);
            }
            private SharkToken makeIntToken(SkTokenType type, string content){

                SkInt con = new SkInt(int.Parse(content));
                return new SharkTokenConstant(constTable, con, currentLine, content, type);
            }
            public SharkToken makeFloatToken(SkTokenType type, string content){

                SkFloat con = new SkFloat(float.Parse(content));
                return new SharkTokenConstant(constTable, con, currentLine, content, type);
            }
            ///<summary>
            ///从源代码的指定位置获取一个子字符串
            ///</summary>
            private string takeString(int from, int to, bool cast = false){
                string _Ret = sourceCode.Substring(from, to - from);
                if(cast)return Regex.Unescape(_Ret);
                return _Ret;
            }

            public SharkToken getNextToken(){
                char C = nextUsefulChar;
                switch(C){

                    case '\0':
                    return makeToken(SkTokenType.TOKEN_EOF);

                    case ',':
                    return makeToken(SkTokenType.TOKEN_COMMA);

                    case ':':
                    return makeToken(SkTokenType.TOKEN_COLON);

                    case ';':
                    return makeToken(SkTokenType.TOKEN_SEMIC_COLON);

                    case '(':
                    return makeToken(SkTokenType.TOKEN_LEFT_PAREN);

                    case ')':
                    return makeToken(SkTokenType.TOKEN_RIGHT_PAREN);

                    case '[':
                    return makeToken(SkTokenType.TOKEN_LEFT_BRACKETS);

                    case ']':
                    return makeToken(SkTokenType.TOKEN_RIGHT_BRACKETS);

                    case '{':
                    return makeToken(SkTokenType.TOKEN_LEFT_BARCE);

                    case '}':
                    return makeToken(SkTokenType.TOKEN_RIGHT_BRACE);

                    case '.':
                    return makeToken(SkTokenType.TOKEN_DOT);

                    case '^':
                    return makeToken(SkTokenType.TOKEN_POW);

                    case '=':
                    return matchChar('=')?makeToken(SkTokenType.TOKEN_EQUAL):makeToken(SkTokenType.TOKEN_ASSIGN);

                    case '+':
                    return matchChar('=')?makeToken(SkTokenType.TOKEN_ASSIGN_ADD):makeToken(SkTokenType.TOKEN_ADD);

                    case '-':
                    return matchChar('=')?makeToken(SkTokenType.TOKEN_ASSIGN_SUB):makeToken(SkTokenType.TOKEN_SUB);

                    case '*':
                    return matchChar('=')?makeToken(SkTokenType.TOKEN_ASSIGN_MUL):makeToken(SkTokenType.TOKEN_MUL);

                    case '/':
                    if(matchChar('=')){
                        return makeToken(SkTokenType.TOKEN_ASSIGN_DIV);
                    }else if(matchChar('/')){
                        skipLineComments();
                        return getNextToken();
                    }else if(matchChar('*')){
                        skipBlockComments();
                        return getNextToken();
                    }else{
                        return makeToken(SkTokenType.TOKEN_DIV);
                    }

                    case '%':
                    return matchChar('=')?makeToken(SkTokenType.TOKEN_ASSIGN_MOD):makeToken(SkTokenType.TOKEN_MOD);

                    case '&':
                    return matchChar('&')?makeToken(SkTokenType.TOKEN_LOGIC_AND):makeToken(SkTokenType.TOKEN_BIT_AND);

                    case '|':
                    return matchChar('|')?makeToken(SkTokenType.TOKEN_LOGIC_OR):makeToken(SkTokenType.TOKEN_BIT_OR);

                    case '~':
                    return makeToken(SkTokenType.TOKEN_BIT_NOT);

                    case '!':
                    return matchChar('=')?makeToken(SkTokenType.TOKEN_NOT_EQUAL):makeToken(SkTokenType.TOKEN_LOGIC_NOT);

                    case '>':
                    if(matchChar('>')){
                        return makeToken(SkTokenType.TOKEN_BIT_SHIFT_RIGHT);
                    }else if(matchChar('=')){
                        return makeToken(SkTokenType.TOKEN_GREATER_EQUAL);
                    }else{
                        return makeToken(SkTokenType.TOKEN_GREATER);
                    }

                    case '<':
                    if(matchChar('<')){
                        return makeToken(SkTokenType.TOKEN_BIT_SHIFT_LEFT);
                    }else if(matchChar('=')){
                        return makeToken(SkTokenType.TOKEN_LESS_EQUAL);
                    }else{
                        return makeToken(SkTokenType.TOKEN_LESS);
                    }

                    case '\"':
                    string content = parseString();
                    SkObject value = new SkString(content);
                    return new SharkTokenConstant(constTable, value, currentLine, content, SkTokenType.TOKEN_STRING);

                    case '0':
                    if(matchChar('x')){
                        return makeIntToken(SkTokenType.TOKEN_INT, parseSpecNumber(SharkLexerCore.isHexChar, "invalid hex number"));
                    }else if(matchChar('o')){
                        return makeIntToken(SkTokenType.TOKEN_INT, parseSpecNumber(SharkLexerCore.isOctChar, "invalid oct number"));
                    }else if(matchChar('b')){
                        return makeIntToken(SkTokenType.TOKEN_INT, parseSpecNumber(SharkLexerCore.isBinChar, "invalid bin number"));
                    }else{
                        return makeIntToken(SkTokenType.TOKEN_INT, C.ToString());
                    }

                    default:
                    /*
                        1.解析标识符(ID和系统关键字)
                        2.解析数字
                    */
                    if(char.IsLetter(C) || C == '_'){
                        return parseID();
                    }else if(char.IsDigit(C)){
                        return parseNumber();
                    }
                
                    return makeToken(SkTokenType.TOKEN_UNKNOWN);
                }
            }

            private bool matchChar(char match){
                if(hasMore && match == currentChar){
                    currentPos ++;
                    return true;
                }
                return false;
            }
            private bool matchCharUnsafe(char match){
                if(match == currentChar){
                    currentPos ++;
                    return true;
                }
                return false;
            }

            
            
            ///<summary>
            /// 跳过行注释或者区块注释
            ///</summary>
            private void skipLineComments(){

                char buf;
                while(hasMore){
                    buf = currentChar;
                    currentPos ++;
                    if(buf == '\n'){
                        currentLine ++;
                        break;
                    }
                }
            }     
            ///<summary>
            /// 跳过区块注释或者区块注释
            ///</summary>
            private void skipBlockComments(){

                char buf;
                while(hasMore){
                    buf = currentChar;
                    currentPos ++;
                    if(buf == '*' && matchChar('/')){
                        break;
                    }else if(buf == '\n'){
                        currentLine ++;
                    }
                }
                throw new SharkSyntaxError($"expect \"*/\" at line {currentLine.ToString()}");
            }
            ///<summary>
            /// 解析十六进制, 八进制或者二进制的数字
            ///</summary>
            private string parseSpecNumber(SharkLexerCore.charChekcer chekcer, string errInfo){

                int positionStart = currentPos - 2;
                int length = 0;
                char buf;
                while(hasMore){
                    buf = nextChar;
                    if(chekcer(buf)){
                        length ++;
                        continue;
                    }
                    currentPos --;
                    break;
                }

                if(length > 0){
                    return takeString(positionStart, currentPos);
                }
                throw new SharkSyntaxError($"{errInfo} at line {currentLine.ToString()}");
            }

            ///<summary>
            /// 解析普通数字
            ///</summary>
            private SharkToken parseNumber(){

                int positionStart = currentPos - 1;

                char buf;
                while(hasMore){
                    buf = nextChar;
                    if(char.IsDigit(buf)){
                        continue;
                    }else if(buf == '.'){
                        return numberParseFloat(positionStart);
                    }else if(buf == 'e'){
                        return numberParserE(positionStart);
                    }else{
                        currentPos --;
                        break;
                    }
                }
                string content = takeString(positionStart, currentPos);
                SkObject value = new SkInt(int.Parse(content));
                return new SharkTokenConstant(constTable, value, currentLine, content, SkTokenType.TOKEN_INT);
            }

            ///<summary>
            /// 解析小数部分
            ///</summary>
            private SharkToken numberParseFloat(int positionStart){

                char buf;
                while(hasMore){
                    buf = nextChar;
                    if(char.IsDigit(buf)){
                        continue;
                    }else if(buf == 'e'){
                        return numberParserE(positionStart);
                    }else{
                        currentPos --;
                        break;
                    }
                }
                // return makeToken(SkTokenType.TOKEN_FLOAT, takeString(positionStart, currentPos));
                string content = takeString(positionStart, currentPos);
                SkObject value = new SkFloat(float.Parse(content));
                return new SharkTokenConstant(constTable, value, currentLine, content, SkTokenType.TOKEN_FLOAT);
            }

            ///<summary>
            /// 解析数字后的科学计数部分
            ///</summary>
            private SharkToken numberParserE(int positionStart){

                char buf;
                int length = 0;
                while(hasMore){
                    buf = nextChar;
                    if(char.IsDigit(buf)){
                        length ++;
                        continue;
                    }
                    currentPos --;
                    break;
                }
                if(length > 0){
                    // return makeToken(SkTokenType.TOKEN_FLOAT, takeString(positionStart, currentPos));
                    string content = takeString(positionStart, currentPos);
                    SkObject value = new SkFloat(float.Parse(content));
                    return new SharkTokenConstant(constTable, value, currentLine, content, SkTokenType.TOKEN_FLOAT);
                }
                throw new SharkSyntaxError($"expect number after \"{takeString(positionStart, currentPos - 1)}\" at line {currentLine.ToString()}");
            }

            ///<summary>
            /// 解析标识符,包括ID和系统关键字
            ///</summary>
            private SharkToken parseID(){

                //当已经发现了是普通的字符时, 第一个字符也需要被包括进来.
                int positionStart = currentPos - 1;

                char buf;
                while(hasMore){
                    buf = nextChar;
                    if(char.IsLetterOrDigit(buf) || buf == '_'){
                        continue;
                    }
                    currentPos --;
                    break;
                }
                string cnt = takeString(positionStart, currentPos);
                return isIdOrKeyword(cnt, currentLine);
            }

            ///<summary>
            /// 解析普通字符串(包含转义字符内容)
            ///</summary>
            private string parseString(){

                int positionStart = currentPos;
                bool shouldCast = false;
                char buf;
                while(hasMore){
                    buf = nextChar;
                    switch(buf){
                        case '\"':
                        return takeString(positionStart, currentPos - 1, shouldCast);

                        case '\n':
                        currentLine ++;
                        continue;

                        case '\\':
                        if(SharkLexerCore.isCastChar(currentChar)){
                            currentPos ++;
                            shouldCast = true;
                        }else{
                            throw new SharkSyntaxError($"unsupported escape \"\\{currentChar}\" at line {currentLine.ToString()}");
                        }
                        continue;
                    }
                }
                throw new SharkSyntaxError($"expect '\"' at line {currentLine.ToString()}");
            }
        }
    }

    namespace SharkParser{

        /// <summary>
        /// Shark语法树（该语法树在Shark1.3版本中仅用于语法分析测试）
        /// </summary>
        public class SkAST{
            
            public readonly string content;
            public SkAST[] children;
            public SkAST LeftNode{
                get => children[0];
                set => children[0] = value;
            }
            public SkAST RightNode{
                get => children[1];
                set => children[1] = value;
            }
            public bool HasChildren{
                get => children != null;
            }
            public SkAST(string NodeName, int count = 0){

                content = NodeName;
                children = count == 0 ? null : new SkAST[count];
            }
            public SkAST GetChildren(int index){
                
                if(index < children.Length){
                    return children[index];
                }
                return null;
            }
            public bool SetChildren(int index, SkAST child){

                if(index < children.Length){
                    children[index] = child;
                    return true;
                }
                return false;
            }
            public override string ToString()
            {
                return content;
            }
            public static SkAST upInsert(SkAST child, string content, int count){

                SkAST O = new SkAST(content, count);
                O.LeftNode = child;
                return O;
            }
            public static SkAST makeSingleOp(SkToken token){

                SkAST ast = new SkAST(token.tokenContent, 1);
                return ast;
            }
        }


        /// <summary>
        /// Shark语法解析器(测试用)
        /// </summary>
        public class DEBUG_SkParser{

            private Queue<SkToken> tokens;
            public DEBUG_SkParser(){}
            public void LoadTokens(Queue<SkToken> tokens){
                this.tokens = tokens;
            }
            public SkToken getNextToken(bool takeOut){
                if(takeOut)return tokens.Dequeue();
                return tokens.Peek();
            }
            public bool safeMatchToken(SkTokenType type){
                if(tokens.Count > 0){
                    if(tokens.Peek().tokenType == type){
                        tokens.Dequeue();
                        return true;
                    }
                }
                return false;
            }
            public bool matchToken(SkTokenType type){
                if(tokens.Peek().tokenType == type){
                    tokens.Dequeue();
                    return true;
                }
                return false;
            }
            /// <summary>
            /// BNF 因子解析器，因子是不可分割的最小单位
            /// factor -> null | true | false
            /// factor -> int | floatc
            /// factor -> string
            /// factor -> name ('[' expression ']' | '(' paramlist ')' | '.' name )
            /// factor -> functiondef
            /// </summary>
            public SkAST nextFactor(){

                SkToken token = getNextToken(true);
                switch(token.tokenType){

                    case SkTokenType.TOKEN_INT:
                    return new SkAST(token.tokenContent);

                    case SkTokenType.TOKEN_FLOAT:
                    return new SkAST(token.tokenContent);

                    case SkTokenType.TOKEN_FALSE:
                    return new SkAST(token.tokenContent);

                    case SkTokenType.TOKEN_TRUE:
                    return new SkAST(token.tokenContent);

                    case SkTokenType.TOKEN_STRING:
                    return nextStringChecker(new SkAST(token.tokenContent));

                    case SkTokenType.TOKEN_NULL:
                    return new SkAST(token.tokenContent);

                    case SkTokenType.TOKEN_ID:
                    return nextIDChcker(new SkAST(token.tokenContent));

                    case SkTokenType.TOKEN_FUNC:
                    return nextAnonyFunctionDef();

                    case SkTokenType.TOKEN_LEFT_PAREN:
                    SkAST exp = nextLogicBinOp();
                    if(safeMatchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                        return nextIDChcker(exp);
                    }else{
                        throw new SharkSyntaxError($"expect \")\" at line {token.line}");
                    }

                    default:
                    throw new SharkSyntaxError($"invalid syntax at line {token.line}");
                }
            }

            public SkAST nextAnonyFunctionDef(){

                SkAST functionDef = new SkAST("FUNCTION_DEF_ANONY", 2);
                functionDef.LeftNode = nextFormalParamlist();
                functionDef.RightNode = nextBody();
                return functionDef;
            }

            /// <summary>
            /// nextID -> . | '[' exp ']' | '(' paramlist ')' 
            /// </summary>
            public SkAST nextDot(SkAST O){
                
                SkToken token = getNextToken(true);
                if(token == SkTokenType.TOKEN_ID){
                    O = SkAST.upInsert(O, SkTokenType.TOKEN_DOT.ToString(), 2);
                    O.RightNode = new SkAST(token.tokenContent, 0);
                }else{
                    throw new SharkSyntaxError($"expect a variable name after \".\" at line {token.line}");
                }
                return O;
            }

            public SkAST nextIndex(SkAST O){

                SkAST exp = nextMathBinOp_Low();
                if(safeMatchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){
                    O = SkAST.upInsert(O, "OP_INDEX", 2);
                    O.RightNode = exp;
                    return O;
                }
                throw new SharkSyntaxError($"expect \"]\" at line {0}");
            }

            public SkAST nextStringChecker(SkAST O){
                if(matchToken(SkTokenType.TOKEN_DOT)){
                    O = SkAST.upInsert(O, "OP_DOT", 2);
                    SkToken token = getNextToken(false);
                    if(token == SkTokenType.TOKEN_ID){
                        tokens.Dequeue();
                        O.RightNode = nextIDChcker(new SkAST(token.tokenContent));
                        return O;
                    }
                    throw new SharkSyntaxError($"expect variable name after \".\" at line {token.line}");        
                }
                return O;
            }

            public SkAST nextFormalParamlist(){

                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    SkToken token;
                    List<SkAST> paramlist = new List<SkAST>();
                    while(true){
                        token = getNextToken(false);
                        if(token == SkTokenType.TOKEN_ID){
                            tokens.Dequeue();
                            paramlist.Add(new SkAST(token.tokenContent));
                            token = getNextToken(false);
                            if(token == SkTokenType.TOKEN_COMMA){
                                tokens.Dequeue();
                                continue;
                            }else if(token == SkTokenType.TOKEN_RIGHT_PAREN){
                                tokens.Dequeue();
                                break;
                            }else{
                                throw new SharkSyntaxError($"形式参数列表中遇到了不符合规范的符号,在第{token.line}行");
                            }
                        }else if(token == SkTokenType.TOKEN_COMMA){
                            throw new SharkSyntaxError($"参数列表不符合规范,在第{token.line}行");
                        }else{
                            throw new SharkSyntaxError($"形式参数列表中遇到了不符合规范的符号,在第{token.line}行");
                        }
                    }
                    SkAST args = new SkAST("DATA_FORMAL_PARAMS", paramlist.Count);
                    for(int i = 0; i < paramlist.Count; i ++){
                        args.SetChildren(i, paramlist[i]);
                    }
                    return args;
                }
                throw new SharkSyntaxError("形式参数列表应该以左括号开始, 在{}行");
            }

            /// <summary>
            /// ID checker -> ID | ID{'(' paramlist ')'} | ID{'.' ID{'(' paramlist ')'}} | ID{'[' exp ']'}
            /// </summary>
            public SkAST nextIDChcker(SkAST variable){

                SkAST O;
                SkToken token = getNextToken(false);
                switch(token.tokenType){
                    case SkTokenType.TOKEN_DOT:
                        tokens.Dequeue();
                        O = nextDot(variable);
                        return nextIDChcker(O);

                    case SkTokenType.TOKEN_LEFT_PAREN:
                        tokens.Dequeue();
                        O = SkAST.upInsert(variable, "OP_CALL", 2);
                        O.RightNode = nextParamlist();
                        return nextIDChcker(O);

                    case SkTokenType.TOKEN_LEFT_BRACKETS:
                        tokens.Dequeue();
                        O = nextIndex(variable);
                        return nextIDChcker(O);

                    default:
                    return variable;
                }
            }

            /// <summary>
            /// 参数列表解析器
            /// paramlist -> '(' (exp {',' exp }|)')'
            /// </summary>
            public SkAST nextParamlist(){

                SkToken token;
                List<SkAST> paramlist = new List<SkAST>();
                while(true){
                    if(paramlist.Count > 0){
                        token = getNextToken(false);
                        if(token == SkTokenType.TOKEN_RIGHT_PAREN){
                            tokens.Dequeue();
                            break;
                        }else if(token == SkTokenType.TOKEN_COMMA){
                            tokens.Dequeue();
                            paramlist.Add(nextLogicBinOp());
                        }else{
                            throw new SharkSyntaxError($"invalid paramlist at line {token.line}");
                        }
                    }else{
                        token = getNextToken(false);
                        if(token == SkTokenType.TOKEN_RIGHT_PAREN){
                            tokens.Dequeue();
                            break;
                        }
                        paramlist.Add(nextLogicBinOp());
                    }
                }
                SkAST ast = new SkAST("PARAMLIST", paramlist.Count);
                for(int i = 0; i < paramlist.Count; i++){
                    ast.SetChildren(i, paramlist[i]);
                }
                return ast;
            }

            
            /// <summary>
            /// 数学单目操作符解析
            /// UnOp -> '-'
            /// </summary>
            public SkAST nextMathUnOp(){

                SkAST O;
                if(safeMatchToken(SkTokenType.TOKEN_SUB)){
                    O = new SkAST(SkTokenType.TOKEN_SUB.ToString(), 1);
                    O.LeftNode = nextFactor();
                    return O;
                }
                return nextFactor();
            }
            public SkAST nextPowOp(){

                SkAST O = nextMathUnOp();
                if(matchToken(SkTokenType.TOKEN_POW)){
                    O = SkAST.upInsert(O, SkTokenType.TOKEN_POW.ToString(), 2);
                    O.RightNode = nextMathUnOp();
                    return O;
                }
                return O;
            }

            /// <summary>
            /// 高优先级_数学双目运算符解析轨道
            /// BinOp = '*' | '/' | '%'
            /// <summary>
            public SkAST nextMathBinOp_High(){

                SkAST O = nextPowOp();
                SkToken token; 
                while(true){

                    token = getNextToken(false);
                    if(token.tokenType == SkTokenType.TOKEN_MUL || 
                    token.tokenType == SkTokenType.TOKEN_DIV || token.tokenType == SkTokenType.TOKEN_MOD){
                        tokens.Dequeue();
                        O = SkAST.upInsert(O, token.tokenContent, 2);
                        O.RightNode = nextPowOp();
                    }else{
                        return O;
                    }
                }
            }
            public SkAST nextMathBinOp_Low(){

                SkAST O = nextMathBinOp_High();
                SkToken token;
                while(true){
                    token = getNextToken(false);
                    if(token == SkTokenType.TOKEN_ADD || token == SkTokenType.TOKEN_SUB){
                        tokens.Dequeue();
                        O = SkAST.upInsert(O, token.tokenContent, 2);
                        O.RightNode = nextMathBinOp_High();
                    }else{
                        return O;
                    }
                }
            }
            public SkAST nextCompare(){

                SkAST O = nextMathBinOp_Low();
                SkToken token = getNextToken(false);
                if(token == SkTokenType.TOKEN_GREATER || token == SkTokenType.TOKEN_GREATER_EQUAL || 
                token == SkTokenType.TOKEN_EQUAL || token == SkTokenType.TOKEN_NOT_EQUAL || 
                token == SkTokenType.TOKEN_LESS || token == SkTokenType.TOKEN_LESS_EQUAL){
                    tokens.Dequeue();
                    O = SkAST.upInsert(O, token.tokenContent, 2);
                    O.RightNode = nextMathBinOp_Low();
                }
                return O;
            }
            public SkAST nextLogicUnOp(){

                SkAST O = nextCompare();
                SkToken token = getNextToken(false);
                if(token == SkTokenType.TOKEN_LOGIC_NOT){
                    tokens.Dequeue();
                    O = SkAST.upInsert(O, token.tokenContent, 1);
                }
                return O;
            }
            public SkAST nextLogicBinOp(){

                SkAST O = nextLogicUnOp();
                SkToken token;
                while(true){
                    token = getNextToken(false);
                    if(token == SkTokenType.TOKEN_LOGIC_AND || token == SkTokenType.TOKEN_LOGIC_OR){
                        tokens.Dequeue();
                        O = SkAST.upInsert(O, token.tokenContent, 2);
                        O.RightNode = nextLogicUnOp();
                        continue;
                    }
                    break;
                }
                return O;
            }

            /// <summary>
            /// statement -> return exp | 
            /// </summary>
            public SkAST nextStatement(){

                SkToken token = getNextToken(false);
                if(token == SkTokenType.TOKEN_RETURN){
                    tokens.Dequeue();
                    SkAST exp = nextLogicBinOp();
                    SkAST O = SkAST.upInsert(exp, "OP_RETURN", 1);
                    return O;
                }else if(token == SkTokenType.TOKEN_ID){
                    tokens.Dequeue();
                    SkAST variable = nextNameChecker(new SkAST(token.tokenContent));
                    if(matchToken(SkTokenType.TOKEN_ASSIGN)){
                        
                        variable = SkAST.upInsert(variable, "OP_ASSIGN", 2);
                        variable.RightNode = nextLogicBinOp();
                        return variable;
                    }else if(matchToken(SkTokenType.TOKEN_ASSIGN_ADD)){
                        
                        variable = SkAST.upInsert(variable, "OP_ASSIGN_ADD", 2);
                        variable.RightNode = nextLogicBinOp();
                        return variable;
                    }else if(matchToken(SkTokenType.TOKEN_ASSIGN_DIV)){

                        variable = SkAST.upInsert(variable, "OP_ASSIGN_DIV", 2);
                        variable.RightNode = nextLogicBinOp();
                        return variable;
                    }else if(matchToken(SkTokenType.TOKEN_ASSIGN_MUL)){

                        variable = SkAST.upInsert(variable, "OP_ASSIGN_MUL", 2);
                        variable.RightNode = nextLogicBinOp();
                        return variable;
                    }else if(matchToken(SkTokenType.TOKEN_ASSIGN_SUB)){

                        variable = SkAST.upInsert(variable, "OP_ASSIGN_SUB", 2);
                        variable.RightNode = nextLogicBinOp();
                        return variable;
                    }else if(matchToken(SkTokenType.TOKEN_ASSIGN_MOD)){
                        variable = SkAST.upInsert(variable, "OP_ASSIGN_MOD", 2);
                        variable.RightNode = nextLogicBinOp();
                        return variable;
                    }else{
                        SkAST stat = nextIDChcker(variable);
                        if(stat.content != "OP_CALL"){
                            throw new SharkSyntaxError($"单独的变量名无法成为语句, 在第{token.line}行");
                        }
                        return stat;
                    }
                }else if(token == SkTokenType.TOKEN_IF){
                    tokens.Dequeue();
                    return nextIf();
                }else if(token == SkTokenType.TOKEN_WHILE){
                    tokens.Dequeue();
                    return nextWhile();
                }else if(token == SkTokenType.TOKEN_FUNC){

                }else if(token == SkTokenType.TOKEN_FOR){

                }else if(token == SkTokenType.TOKEN_BREAK){
                    return new SkAST("OP_BREAK");
                }else if(token == SkTokenType.TOKEN_CONTINUE){
                    return new SkAST("OP_CONTINUE");
                }
                return null;
            }
            public SkAST nextNameChecker(SkAST variable){
                while(true){
                    if(matchToken(SkTokenType.TOKEN_DOT)){
                        variable = SkAST.upInsert(variable, "OP_DOT", 2);
                        SkToken token = getNextToken(false);
                        if(token.tokenType == SkTokenType.TOKEN_ID){
                            tokens.Dequeue();
                            variable.RightNode = new SkAST(token.tokenContent);
                        }else{
                            throw new SharkSyntaxError($"expect variable name after \".\" at line {token.line}");
                        }
                    }else{
                        break;
                    }
                }
                return variable;
            }
            public SkAST nextIf(){

                List<SkAST> branches = new List<SkAST>();
                while(true){
                    SkAST branch = new SkAST("STRUCT_BRANCH", 2);
                    branch.LeftNode = nextCondition();
                    branch.RightNode = nextBody();
                    branches.Add(branch);

                    if(matchToken(SkTokenType.TOKEN_ELSE)){
                        if(matchToken(SkTokenType.TOKEN_IF)){
                            // else if 

                            continue;
                        }else{
                            // else
                            SkAST branchDefault = new SkAST("DEFAULT", 1);
                            branchDefault.LeftNode = nextBody();
                            branches.Add(branchDefault);
                            break;
                        }
                    }else{
                        break;
                    }
                }
                
                SkAST ifblock = new SkAST("STRUCT_IF", branches.Count);
                for(int i = 0; i < branches.Count; i ++){
                    ifblock.SetChildren(i, branches[i]);
                }
                return ifblock;
            }
            public SkAST nextWhile(){

                SkAST whileBlock = new SkAST("STRUCT_WHILE", 2);
                whileBlock.LeftNode = nextCondition();
                whileBlock.RightNode = nextBody();
                return whileBlock;
            }
            public SkAST nextCondition(){

                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    SkAST cond = nextLogicBinOp();
                    if(matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                        return cond;
                    }
                }
                throw new SharkSyntaxError("条件表达式右侧应该有一个括号, 在第{}行");
            }
            public SkAST nextBody(){

                if(matchToken(SkTokenType.TOKEN_LEFT_BARCE)){
                    SkAST body;
                    List<SkAST> statements = new List<SkAST>();
                    while(true){
                        if(matchToken(SkTokenType.TOKEN_RIGHT_BRACE)){
                            break;
                        }
                        statements.Add(nextStatement());
                    }
                    body = new SkAST("BODY", statements.Count);
                    for(int i = 0; i < statements.Count; i ++){
                        body.SetChildren(i, statements[i]);
                    }
                    return body;
                }
                throw new SharkSyntaxError("代码块必须由 \"{\" 开始");
            }
        }
    
        public class SkParser{

            public SharkCodeEmitter emitter;
            public SharkScript currentScript;

            public SharkToken[] tokens;
            public int currentPos;
            public int lastPos;
            public int currentLine;
            public StringBuilder currentStatementInfo;
            public Stack<SharkRunable> functionList;

            public List<string> DEBUG_INFOS;

            public SkParser(){

                emitter = new SharkCodeEmitter();
                currentPos = 0;
                lastPos = 0;
                currentLine = 1;
                currentStatementInfo = new StringBuilder();
                functionList = new Stack<SharkRunable>();

                DEBUG_INFOS = new List<string>();
            }

            public SharkToken currentToken{
                get => tokens[currentPos];
            }
            public SharkToken lastToken{
                get => tokens[lastPos];
            }
            public bool hasMore{
                get => currentPos < tokens.Length;
            }

            public void moveToNext(){

                lastPos = currentPos;
                currentPos ++;
                if(currentToken.tokenModel != TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS){
                    currentLine = ((SharkTokenUniversal)currentToken).line;
                }
            }
            public bool matchToken(SkTokenType type){

                if(currentToken.tokenType == type){
                    moveToNext();
                    return true;
                }
                return false;
            }
            public SharkScript generateScript(bool clearBuffer = true){

                SharkILStream ilstream = emitter.EmitDone(clearBuffer);
                currentScript.loadCommands(ilstream);
                return currentScript;
            }

            /// <summary>
            /// 加载词法分析器的解析的结果
            /// </summary>
            public void loadLexerSouce(SkLexer lexer){

                Queue<SharkToken> tokenQueue = lexer.Parse();
                tokens = new SharkToken[tokenQueue.Count];
                int index = 0;
                while(tokenQueue.Count > 0){

                    tokens[index] = tokenQueue.Dequeue();
                    index ++;
                }
                currentScript = lexer.generateScript();
                SharkInternalCodeBase.currentScript = currentScript;
                // 接受到标记队列时生成目标脚本, 但此时的脚本中间代码为空, 需要解析后写入
            }
            public void nextFactor(){
                // 找到下一个因子

                SharkToken token = currentToken;
                switch(token.tokenType){

                    case SkTokenType.TOKEN_INT:
                    emitter.pushConst(((SharkTokenConstant)token).constID, currentLine);
                    moveToNext();
                    break;

                    case SkTokenType.TOKEN_FLOAT:
                    emitter.pushConst(((SharkTokenConstant)token).constID, currentLine);
                    moveToNext();
                    break;

                    case SkTokenType.TOKEN_FALSE:
                    emitter.pushFalse(currentLine);
                    moveToNext();
                    break;

                    case SkTokenType.TOKEN_TRUE:
                    emitter.pushTrue(currentLine);
                    moveToNext();
                    break;

                    case SkTokenType.TOKEN_STRING:
                    emitter.pushConst(((SharkTokenConstant)token).constID, currentLine);
                    moveToNext();
                    break;

                    case SkTokenType.TOKEN_ID:
                    int id = ((SharkTokenVariable)token).variableID;
                    moveToNext();
                    nextIDFactor(id);
                    break;

                    case SkTokenType.TOKEN_LEFT_BRACKETS:
                    moveToNext();
                    nextList();
                    break;

                    case SkTokenType.TOKEN_FUNC:
                    moveToNext();
                    nextAnonymousFunctionDefine();
                    break;

                    case SkTokenType.TOKEN_THIS:
                    moveToNext();
                    emitter.pushThis(currentLine);
                    nextThisFactor();
                    break;

                    case SkTokenType.TOKEN_LEFT_PAREN:
                    moveToNext();
                    nextLogicExpr();
                    if(!matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                        throw new SharkSyntaxError($"sub expression should end with \")\" at line {currentLine.ToString()}");
                    }
                    break;
                }
            }

            public void nextAnonymousFunctionDefine(){
                /* 匿名函数定义 */

                SharkFunctionCore functionCore = new SharkFunctionCore();
                List<int> paramlist = nextFunctionDefParamlist();
                
                emitter.defineFunction();
                nextBody();
                functionCore.loadFunctionCore(emitter.endDefineFunction(currentLine), paramlist);
                int functionID = currentScript.registerNewFunction(functionCore);

                DEBUG_INFOS.Add($"--- FUNC INFO: 匿名函数_{functionID} ---");
                functionCore.saveToList(DEBUG_INFOS);
                DEBUG_INFOS.Add(string.Empty);
                emitter.pushOperator32Bit(SharkOpcodeType.OP_PUSH_FUNC, functionID, currentLine);
            }

            public void nextList(){

                if(matchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){
                    emitter.pushOperator32Bit(SharkOpcodeType.DEF_LIST, 0, currentLine);
                }else{
                    nextListFactor(1);
                }
            }
            public void nextListFactor(int count){

                nextLogicExpr();
                if(matchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){
                    emitter.pushOperator32Bit(SharkOpcodeType.DEF_LIST, count, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_COMMA)){
                    nextListFactor(count + 1);
                }
            }

            /// <summary>
            /// 当在因子检测器中遇到了ID, 检查ID是索引还是函数调用还是Getter
            /// </summary>
            public void nextIDFactor(int id){

                emitter.pushVar(id, currentLine);                               // 将是函数的目标对象压入栈区
                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    // 函数调用

                    emitter.pushCall(nextParamlist(), currentLine);
                    nextIDFactor();
                }else if(matchToken(SkTokenType.TOKEN_DOT)){
                    // getter
                    if(matchToken(SkTokenType.TOKEN_ID)){
                        emitter.pushGetField(((SharkTokenVariable)lastToken).variableID, currentLine);
                    }else{
                        throw new SharkSyntaxError($"expect variable name after \".\" at line {currentLine.ToString()}");
                    }
                    nextIDFactor();
                }else if(matchToken(SkTokenType.TOKEN_LEFT_BRACKETS)){
                    // 索引

                    nextLogicExpr();
                    if(!matchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){
                        throw new SharkSyntaxError("expect \"]\" after list index operation at line {}");
                    }
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_INDEX, currentLine);
                    nextIDFactor();
                }
            }
            public void nextIDFactor(){
                /* 辅助nextIDFactor(int)函数进行连续后缀检测 */

                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    // 函数调用

                    emitter.pushCall(nextParamlist(), currentLine);
                    nextIDFactor();
                }else if(matchToken(SkTokenType.TOKEN_DOT)){
                    // getter
                    if(matchToken(SkTokenType.TOKEN_ID)){
                        
                        emitter.pushGetField(((SharkTokenVariable)lastToken).variableID, currentLine);
                    }else{
                        throw new SharkSyntaxError($"expect variable name after \".\" at line {currentLine.ToString()}");
                    }
                    nextIDFactor();
                }else if(matchToken(SkTokenType.TOKEN_LEFT_BRACKETS)){
                    // 索引

                    nextLogicExpr();
                    if(!matchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){
                        throw new SharkSyntaxError("expect \"]\" after list index operation at line {}");
                    }
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_INDEX, currentLine);
                    nextIDFactor();
                }
            }
            public void nextThisFactor(){
                /* 在factor中遇到this时，进行this的后缀检测 */

                if(matchToken(SkTokenType.TOKEN_DOT)){
                    // 从this中索引一个值
                    if(matchToken(SkTokenType.TOKEN_ID)){
                        /* 仅支持从this中索引一个符号，其他索引会被认为是语法异常 */
                        emitter.pushGetField(((SharkTokenVariable)lastToken).variableID, currentLine);
                    }else{
                        throw new SharkSyntaxError($"expect variable name after \".\" at line {currentLine.ToString()}");
                    }
                }
            }
            
            public void nextNegative(){

                if(matchToken(SkTokenType.TOKEN_SUB)){
                    nextFactor();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_NEG, currentLine);
                }else{
                    nextFactor();
                }
            }
            public void nextMulDiv(){

                nextNegative();
                while(true){
                    if(matchToken(SkTokenType.TOKEN_MUL)){

                        nextNegative();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_MUL, currentLine);
                    }else if(matchToken(SkTokenType.TOKEN_DIV)){

                        nextNegative();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_DIV, currentLine);
                    }else if(matchToken(SkTokenType.TOKEN_MOD)){

                        nextNegative();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_MOD, currentLine);
                    }else{
                        break;
                    }
                }
            }
            public void nextMathExpr(){

                nextMulDiv();
                while(true){
                    if(matchToken(SkTokenType.TOKEN_ADD)){
                        nextMulDiv();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_ADD, currentLine);
                    }else if(matchToken(SkTokenType.TOKEN_SUB)){
                        nextMulDiv();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_SUB, currentLine);
                    }else{
                        break;
                    }
                }
            }
            public void nextCompareExpr(){

                nextMathExpr();
                if(matchToken(SkTokenType.TOKEN_GREATER)){

                    nextMathExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_GT, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_GREATER_EQUAL)){

                    nextMathExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_GE, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_LESS)){

                    nextMathExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_LT, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_LESS_EQUAL)){

                    nextMathExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_LE, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_NOT_EQUAL)){

                    nextMathExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_NE, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_EQUAL)){

                    nextMathExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_EQ, currentLine);
                }
            }
            public void nextLogicNot(){

                nextCompareExpr();
                if(matchToken(SkTokenType.TOKEN_LOGIC_NOT)){

                    nextCompareExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_NOT, currentLine);
                }
            }
            public void nextLogicExpr(){

                nextCompareExpr();
                while(true){
                    if(matchToken(SkTokenType.TOKEN_LOGIC_AND)){

                        nextCompareExpr();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_AND, currentLine);
                    }else if(matchToken(SkTokenType.TOKEN_LOGIC_OR)){

                        nextCompareExpr();
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_OR, currentLine);
                    }else{
                        break;
                    }
                }
            }
            public void nextMathAssign(int id, SharkOpcodeType type){

                emitter.pushVar(id, currentLine);
                nextLogicExpr();
                emitter.pushOperatorNoParam(type, currentLine);
                emitter.pushSetLocal(id, currentLine);
            }
            public void nextStatementThis(){

                int count = 0;
                SharkInternalCode_1_32BIT opcall = null;    // 上一个函数call命令
                while(true){
                    if(matchToken(SkTokenType.TOKEN_DOT)){
                        if(opcall != null){
                            opcall.opType = SharkOpcodeType.CALL;
                            opcall = null;
                        }
                        nextStatementID_HandleDot();
                    }else if(matchToken(SkTokenType.TOKEN_LEFT_BRACKETS)){
                        if(opcall != null){
                            opcall.opType = SharkOpcodeType.CALL;
                            opcall = null;
                        }
                        nextStatementID_HandleIndex();
                    }else if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                        if(opcall != null){
                            opcall.opType = SharkOpcodeType.CALL;
                            opcall = null;
                        }
                        nextStatementID_HandleCall();
                    }else{

                        break;
                    }
                    count ++;
                }
                if(count == 0){
                    throw new SharkSyntaxError($"keyword \"this\" cannot be rewrite at line {currentLine.ToString()}");
                }else{
                    nextStatementThis_AssignCheck();
                }
                
            }
            public void nextStatementThis_AssignCheck(){

                if(matchToken(SkTokenType.TOKEN_ASSIGN)){
                    nextLogicExpr();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_SET_FIELD, currentLine);
                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_ADD)){
                    
                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_MOD)){

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_DIV)){

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_MUL)){

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_SUB)){

                }
            }
            public void nextStatementID_Face(int ID){

                int count = 0;                              // 循环的次数
                SharkInternalCode_1_32BIT opcall = null;    // 上一个函数call命令
                while(true){

                    if(matchToken(SkTokenType.TOKEN_DOT)){
                        if(opcall != null){
                            opcall.opType = SharkOpcodeType.CALL;
                            opcall = null;
                        }
                        if(count == 0){
                            nextStatementID_HandleDot(ID);
                        }else{
                            nextStatementID_HandleDot();
                        }
                    }else if(matchToken(SkTokenType.TOKEN_LEFT_BRACKETS)){

                        if(opcall != null){
                            opcall.opType = SharkOpcodeType.CALL;
                            opcall = null;
                        }
                        if(count == 0){
                            nextStatementID_HandleIndex(ID);
                        }else{
                            nextStatementID_HandleIndex();
                        }
                    }else if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){

                        if(opcall != null){
                            opcall.opType = SharkOpcodeType.CALL;
                            opcall = null;
                        }

                        if(count == 0){
                            opcall = nextStatementID_HandleCall(ID);
                        }else{
                            opcall = nextStatementID_HandleCall();
                        }
                    }else{

                        break;
                    }
                    count ++;
                }

                nextStatementID_AssignCheck(count, ID, opcall);
            }
            public void nextStatementID_AssignCheck(int count, int ID, SharkInternalCode_1_32BIT opcall){

                if(matchToken(SkTokenType.TOKEN_ASSIGN)){
                    if(opcall != null)opcall.opType = SharkOpcodeType.CALL;
                    nextLogicExpr();
                    if(count == 0){
                        emitter.pushOperator32Bit(SharkOpcodeType.OP_SET_LOCAL, ID, currentLine);
                    }else{
                        emitter.pushOperatorNoParam(SharkOpcodeType.OP_SET_FIELD, currentLine);
                    }
                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_ADD)){
                    if(opcall != null)opcall.opType = SharkOpcodeType.CALL;

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_MOD)){
                    if(opcall != null)opcall.opType = SharkOpcodeType.CALL;

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_DIV)){
                    if(opcall != null)opcall.opType = SharkOpcodeType.CALL;

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_MUL)){
                    if(opcall != null)opcall.opType = SharkOpcodeType.CALL;

                }else if(matchToken(SkTokenType.TOKEN_ASSIGN_SUB)){
                    if(opcall != null)opcall.opType = SharkOpcodeType.CALL;

                }else{
                    
                }
            }
            public void nextStatementID_HandleDot(int ID){

                if(matchToken(SkTokenType.TOKEN_ID)){
                    emitter.pushVar(ID, currentLine);
                    emitter.pushOperator32Bit(SharkOpcodeType.OP_GET_FIELD, ((SharkTokenVariable)lastToken).variableID, currentLine);
                }else{
                    throw new SharkSyntaxError($"missing variable id after \".\"at line {currentLine.ToString()}");
                }
            }
            public void nextStatementID_HandleDot(){
                if(matchToken(SkTokenType.TOKEN_ID)){
                    emitter.pushOperator32Bit(SharkOpcodeType.OP_GET_FIELD, ((SharkTokenVariable)lastToken).variableID, currentLine);
                }else{
                    throw new SharkSyntaxError($"missing variable id after \".\"at line {currentLine.ToString()}");
                }
            }
            public void nextStatementID_HandleIndex(int ID){
                emitter.pushVar(ID, currentLine);
                nextLogicExpr();
                emitter.pushOperatorNoParam(SharkOpcodeType.OP_INDEX, currentLine);
                if(!matchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){ 
                    throw new SharkSyntaxError($"missing \"]\" at line {currentLine.ToString()}");
                }
            }
            public void nextStatementID_HandleIndex(){
                nextLogicExpr();
                emitter.pushOperatorNoParam(SharkOpcodeType.OP_INDEX, currentLine);
                if(!matchToken(SkTokenType.TOKEN_RIGHT_BRACKETS)){ 
                    throw new SharkSyntaxError($"missing \"]\" at line {currentLine.ToString()}");
                }
            }
            public SharkInternalCode_1_32BIT nextStatementID_HandleCall(int ID){
                emitter.pushVar(ID, currentLine);
                SharkInternalCode_1_32BIT opcall = new SharkInternalCode_1_32BIT(SharkOpcodeType.JCALL, nextParamlist(), currentLine, emitter.BufferSize);
                emitter.buffer.Add(opcall);
                return opcall;
            }
            public SharkInternalCode_1_32BIT nextStatementID_HandleCall(){
                SharkInternalCode_1_32BIT opcall = new SharkInternalCode_1_32BIT(SharkOpcodeType.JCALL, nextParamlist(), currentLine, emitter.BufferSize);
                emitter.buffer.Add(opcall);
                return opcall;
            }


            /// <summary>
            /// 将所有的参数压入栈区
            /// </summary>
            public int nextParamlist(){

                int count = 0;
                while(true){
                    if(count > 0){
                        if(matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                            break;
                        }else if(matchToken(SkTokenType.TOKEN_COMMA)){
                            continue;
                        }else{
                            nextLogicExpr();
                            count ++;
                        }
                    }else{
                        if(matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                            break;
                        }else{
                            nextLogicExpr();
                            count ++;
                        }
                    }
                }
                return count;
            }
            public void nextIf(){
                List<SharkInternalCode_1_32BIT> quitCommands = new List<SharkInternalCode_1_32BIT>();

                nextCondition();
                int ifBlockLength = nextBody();
                int branchChooze = emitter.lastRecord;

                SharkInternalCode_1_32BIT quit = new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, -1, currentLine, emitter.BufferSize);
                quitCommands.Add(quit);
                emitter.buffer.Add(quit);
                emitter.insertCode32Bit(branchChooze, SharkOpcodeType.JMP_FALSE, ifBlockLength + 1, currentLine);

                while(true){
                    if(matchToken(SkTokenType.TOKEN_ELSE)){
                        if(matchToken(SkTokenType.TOKEN_IF)){
                            nextCondition();
                            int elseIfBlockLength = nextBody();
                            int elseIfBranchChooze = emitter.lastRecord;
                            quit = new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, -1, currentLine, emitter.BufferSize);
                            quitCommands.Add(quit);
                            emitter.buffer.Add(quit);
                            //emitter.insertCode32Bit(elseIfBranchChooze, SharkOpcodeType.JMP_FALSE, elseIfBlockLength + 1, currentLine);
                        }else{
                            int elseBlockLength = nextBody();
                            emitter.insertCode32Bit(branchChooze, SharkOpcodeType.JMP_FALSE, ifBlockLength + 1, currentLine);
                            break;
                        }
                    }else{
                        break;
                    }
                }
                foreach(SharkInternalCode_1_32BIT code in quitCommands){
                    code.IntValue = emitter.CurrentIfBlockQuitPosition;
                }
            }
            public void nextIfEx(){

                List<SharkInternalCode_1_32BIT> quitCommands = new List<SharkInternalCode_1_32BIT>();

                nextCondition();                            // 写入条件代码
                SharkInternalCode_1_32BIT branch = 
                    new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_FALSE, 0, currentLine, emitter.BufferSize);
                emitter.buffer.Add(branch);
                int bodySize = nextBody();                  // 写入代码块
                SharkInternalCode_1_32BIT quitCmd = 
                    new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, 0, currentLine, emitter.BufferSize);
                quitCommands.Add(quitCmd);


                // 检查是否有后续的else if 或者else
                if(matchToken(SkTokenType.TOKEN_ELSE)){
                    // 遇到else

                    emitter.buffer.Add(quitCmd);
                    branch.IntValue = bodySize + 1;

                    if(matchToken(SkTokenType.TOKEN_IF)){
                        nextIfEx(quitCommands);
                    }else{
                        // 默认分支

                        nextBody();         // 默认分支的代码
                        foreach(SharkInternalCode_1_32BIT opcode in quitCommands){
                            opcode.IntValue = emitter.LastOpcodePosition;
                        }
                    }
                }else{

                    branch.IntValue = bodySize;
                }
            }
            public void nextIfEx(List<SharkInternalCode_1_32BIT> quitCommands){

                nextCondition();
                SharkInternalCode_1_32BIT branch = 
                    new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_FALSE, 0, currentLine, emitter.BufferSize);
                emitter.buffer.Add(branch);
                int bodySize = nextBody();
                SharkInternalCode_1_32BIT quitCmd = 
                    new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, 0, currentLine, emitter.BufferSize);
                quitCommands.Add(quitCmd);

                // 检查是否有后续的else if 或者else
                if(matchToken(SkTokenType.TOKEN_ELSE)){
                    // 遇到else

                    emitter.buffer.Add(quitCmd);
                    branch.IntValue = bodySize + 1;

                    if(matchToken(SkTokenType.TOKEN_IF)){
                        nextIfEx(quitCommands);
                    }else{
                        // 默认分支

                        nextBody();         // 默认分支的代码
                        foreach(SharkInternalCode_1_32BIT opcode in quitCommands){
                            opcode.IntValue = emitter.LastOpcodePosition;
                        }
                    }
                }else{

                    branch.IntValue = bodySize;
                    foreach(SharkInternalCode_1_32BIT opcode in quitCommands){
                        opcode.IntValue = emitter.LastOpcodePosition;
                    }
                }
            }
            public void nextWhile(){

                int position = emitter.LastOpcodePosition;
                emitter.defineWhile();
                nextCondition();
                SharkInternalCode_1_32BIT jumpFalse = 
                    new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_FALSE, 0, currentLine, emitter.BufferSize);
                emitter.buffer.Add(jumpFalse);
                jumpFalse.IntValue = nextBody() + 1;
                emitter.pushOperator32Bit(SharkOpcodeType.JMP_TO, position, currentLine);
                emitter.endDefineWhile();
            }
            public void nextFor(){

                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    // 计数循环条件必须由括号开始

                    nextForAssign();
                    SharkInternalCode_1_32BIT branch = nextForCondition();
                    List<SharkInternalCodeBase> changerCodes = nextForConditionChanger();

                    int size = nextBody();
                    int pos = emitter.BufferSize;
                    foreach(SharkInternalCodeBase code in changerCodes){
                        code.codeNumber += pos;
                        emitter.buffer.Add(code);
                    }
                    size += changerCodes.Count;
                    int entrance = emitter.endDefineFor();
                    emitter.pushOperator32Bit(SharkOpcodeType.JMP_TO, entrance, currentLine);
                    branch.IntValue = size + 1;
                }else{
                    throw new SharkSyntaxError($"for loop condition must start with a \"(\" at line {currentLine.ToString()}");
                }
                
            }
            public void nextForAssign(){
                /* 特殊的赋值查询, 仅用于for循环 */

                if(matchToken(SkTokenType.TOKEN_ID)){
                    int varID = ((SharkTokenVariable)lastToken).variableID;         // variable id
                    if(matchToken(SkTokenType.TOKEN_ASSIGN)){
                        nextMathExpr();
                        emitter.pushSetLocal(varID, currentLine);
                        if(matchToken(SkTokenType.TOKEN_SEMIC_COLON)){
                            emitter.defineFor();
                            return;
                        }
                        throw new SharkSyntaxError($"for condition should split with \";\" at line {currentLine.ToString()}");
                    }
                    throw new SharkSyntaxError($"expect \"=\" after sign {currentScript.getSymbol(varID)} at line {currentLine.ToString()}");
                }
                throw new SharkSyntaxError($"for loop need a loop variable at line {currentLine.ToString()}");
            }
            public SharkInternalCode_1_32BIT nextForCondition(){
                /* 特殊的条件表达式, 仅用于for循环 */

                nextLogicExpr();
                if(!matchToken(SkTokenType.TOKEN_SEMIC_COLON)){
                    throw new SharkSyntaxError($"for condition should split with \";\" at line {currentLine.ToString()}");
                }
                SharkInternalCode_1_32BIT code = new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_FALSE, 0, currentLine, emitter.BufferSize);
                emitter.buffer.Add(code);
                return code;
            }
            public List<SharkInternalCodeBase> nextForConditionChanger(){
                /* 条件表达式状态改变器 */
                
                emitter.holdBranchBuffer();
                nextStatement();
                Console.WriteLine(currentToken);
                if(matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                    return emitter.releaseBranchBuffer();
                }
                throw new SharkSyntaxError($"for condition should end with \")\" at line {currentLine.ToString()}");
            }

            /// <summary>
            /// 解析一个完整的普通函数定义，普通函数的定义必须有一个函数名，
            /// 如果没有函数名则认为是语法错误 
            /// </summary>
            public void nextFunctionDefine(){

                if(matchToken(SkTokenType.TOKEN_ID)){
                    int functionID = ((SharkTokenVariable)lastToken).variableID;            // 获取函数的ID

                    emitter.defineFunction();                   // 定义一个新的函数
                    SharkFunctionCore functionCore = new SharkFunctionCore();
                    // function.bindVM(SharkVM.vm);
                    // function.bindParent(functionList.Count > 0 ? functionList.Peek() : currentScript, currentScript);
                    // functionList.Push(function);
                    
                    List<int> paramlist = nextFunctionDefParamlist();
                    nextBody();

                    functionCore.loadFunctionCore(emitter.endDefineFunction(currentLine), paramlist);
                    // functionList.Pop();

                    DEBUG_INFOS.Add($"--- FUNC INFO: {currentScript.getSymbol(functionID)} ---");
                    functionCore.saveToList(DEBUG_INFOS);

                    int constID = currentScript.registerNewFunction(functionCore);
                    emitter.pushOperator32Bit(SharkOpcodeType.OP_PUSH_FUNC, constID, currentLine);
                    emitter.pushSetLocal(functionID, currentLine);
                }else{
                    throw new SharkSyntaxError($"expect function name after keyword \"function\" at line {currentLine.ToString()}");
                }
            }
            public List<int> nextFunctionDefParamlist(){

                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    // function paramlist should start with (
                    List<int> paramlist = new List<int>();
                    while(true){
                        if(matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                            break;
                        }else if(matchToken(SkTokenType.TOKEN_ID)){
                            paramlist.Add(((SharkTokenVariable)lastToken).variableID);
                            if(matchToken(SkTokenType.TOKEN_COMMA)){
                                continue;
                            }else if(matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                                break;
                            }
                            throw new SharkSyntaxError($"paramlist should split with \",\" at line {currentLine.ToString()}");
                        }
                    }
                    return paramlist;
                }
                throw new SharkSyntaxError($"function paramlist should start with \"(\" at line {currentLine.ToString()}");
                
            }
            public void nextCondition(){

                // 检查左括号
                if(matchToken(SkTokenType.TOKEN_LEFT_PAREN)){
                    nextLogicExpr();

                    // 检查右括号
                    if(!matchToken(SkTokenType.TOKEN_RIGHT_PAREN)){
                        throw new SharkSyntaxError($"expect \")\" end of condition expr at line {currentLine.ToString()}");
                    }
                }else{
                    throw new SharkSyntaxError($"condition expresion should start with \"(\" at line {currentLine.ToString()}");
                }
            }
            /// <summary>
            /// 找到下一个完整的代码块
            /// </summary>
            public int nextBody(){

                if(matchToken(SkTokenType.TOKEN_LEFT_BARCE)){
                    emitter.Record();
                    while(true){
                        if(matchToken(SkTokenType.TOKEN_RIGHT_BRACE)){
                            break;
                        }
                        nextStatement();
                    }
                    return emitter.RecordDone();
                }
                throw new SharkSyntaxError($"code block should start with \"{{\" at line {currentLine.ToString()}");
            }
            public void nextReturn(){

                int start = emitter.BufferSize;
                nextLogicExpr();
                emitter.pushOperatorNoParam(emitter.BufferSize > start ? 
                SharkOpcodeType.RET:SharkOpcodeType.RET_NONE, currentLine);
            }
            /// <summary>
            /// 下一条完整的语句
            /// </summary>
            public void nextStatement(){

                currentStatementInfo.Clear();
                if(matchToken(SkTokenType.TOKEN_ID)){
                    // start with a id

                    SharkTokenVariable variable = (SharkTokenVariable)lastToken;
                    currentStatementInfo.Append((variable.variableName));
                    nextStatementID_Face(variable.variableID);

                }else if(matchToken(SkTokenType.TOKEN_IF)){
                    // 条件判断语句

                    nextIfEx();
                }else if(matchToken(SkTokenType.TOKEN_WHILE)){
                    // 循环语句

                    nextWhile();
                }else if(matchToken(SkTokenType.TOKEN_FUNC)){
                    // 普通函数定义

                    nextFunctionDefine();
                }else if(matchToken(SkTokenType.TOKEN_RETURN)){
                    // RETURN语句

                    nextReturn();
                }else if(matchToken(SkTokenType.TOKEN_FOR)){
                    // 计数循环

                    nextFor();
                }else if(matchToken(SkTokenType.TOKEN_BREAK)){

                    if(emitter.isDefineWhile){
                        emitter.pushWhileBreak(currentLine);
                    }else{
                        throw new SharkSyntaxError($"\"break\" can only used in while or for loop at line {currentLine.ToString()}");
                    }
                }else if(matchToken(SkTokenType.TOKEN_CONTINUE)){

                    if(emitter.isDefineWhile){
                        emitter.pushWhileContinue(currentLine);
                    }else{
                        throw new SharkSyntaxError($"\"continue\" can only used in while or for loop at line {currentLine.ToString()}");
                    }
                }else if(matchToken(SkTokenType.TOKEN_THIS)){

                    emitter.pushThis(currentLine);
                    nextStatementThis();
                }else{
                    throw new SharkSyntaxError($"invalid statement starter \"{currentToken.ToString()}\" at line {currentLine.ToString()}");
                }
            }
            public void parseScript(bool DEBUG = false){
                int count = 0;
                while(true){
                    if(DEBUG){
                        Console.WriteLine($"语句:{count}");
                        Console.WriteLine(currentToken);
                        count ++;
                        if(count >= 1000){
                            break;
                        }
                    }

                    if(currentToken.tokenType == SkTokenType.TOKEN_EOF)break;
                    nextStatement();
                }
                emitter.pushOperatorNoParam(SharkOpcodeType.OP_END, currentLine);
            }
        }
    }

    namespace SharkVirtualMachine{

        public enum SharkOpcodeType{

            // 中间代码
            // 中间代码除了自己的类型之外还拥有一个操作符型号，
            // 这个型号决定了如何解释操作符的数据信息
            
            // 型号1, NO_PARAM
            OP_PUSH_FALSE,         // 向堆栈中压入一个FALSE             
            OP_PUSH_TRUE,          // 向堆栈中压入一个TRUE

            OP_ADD,                
            OP_SUB,
            OP_MUL,
            OP_DIV,
            OP_MOD,
            OP_BIT_AND,
            OP_BIT_OR,
            OP_BIT_LSHIFT,
            OP_BIT_RSHIFT,
            OP_GT,
            OP_GE,
            OP_LT,
            OP_LE,
            OP_EQ,
            OP_NE,
            OP_AND,
            OP_OR,

            OP_NOT,
            OP_NEG,
            OP_BIT_NOT,

            OP_END,
            OP_INDEX,                   /* 从堆栈中取出两个值, 从后者中索引前者, 后者必须是一个列表，前者必须是一个数学表达式 */
            OP_SET_FIELD,

            RET_NONE,
            RET,


            // 型号2, PARAM_32BIT
            OP_PUSH_VAR,           // 向堆栈中压入一个变量
            OP_PUSH_CONST,         // 向堆栈中压入一个常量
            OP_PUSH_INT,           // 向堆栈中压入一个整型值
            OP_PUSH_FLOAT,         // 向堆栈中压入一个单精度值
            OP_SET_LOCAL,          /* 从堆栈中取出一个值，然后将其赋值给全局表中指定的对象 */
            OP_GET_FIELD,          /* 从堆栈中取出一个对象，并从中访问一个指定的字段，再次压入堆栈中 */
            OP_PUSH_FUNC,          /* 从函数表中取出一个函数核心, 然后创建一个函数并压入堆栈中*/
            OP_PUSH_THIS,          /* 向堆栈中压入当前函数的this表 */
            OP_PUSH_GLOBAL,         /* 向堆栈中压入当前的全局表 */
            CALL,
            JCALL,                 // 调用的函数结果并不会压住堆栈中
            

            JMP_FALSE,           // 从栈区中取出一个单位，如果检查为false，则跳过指定数量的操作码
            JMP_TO,              // 无条件直接跳转到指定的位置
            JMP,                 // 无条件直接跳过指定数量的操作码
            DEF_LIST,


            // 型号3, PARAM_64BIT
            OP_PUSH_LONG,          // 向堆栈中压入一个长整型值
            OP_PUSH_DOUBLE,        // 向堆栈中压入一个双精度值

        }

        public enum SharkOpcodeModel{

            MODEL_NO_PARAM,
            MODEL_PARAM_1_32BIT,
            MODEL_PARAM_1_64BIT,

        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DATA_1_32BIT{

            [FieldOffset(0)]
            public float value_float;

            [FieldOffset(0)]
            public int value_int;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DATA_1_64BIT{

            [FieldOffset(0)]
            public double value_double;

            [FieldOffset(0)]
            public long value_long;
        }


        /// <summary>
        /// Shark中间代码, 运行时形态, 从二进制文件中释放成该数据类型
        /// </summary>
        public abstract class SharkInternalCodeBase{

            public static SharkScript currentScript;

            public readonly string contextInfo;
            public readonly int line;
            public readonly SharkOpcodeModel opModel;

            public SharkOpcodeType opType;
            public int codeNumber;
            public SharkInternalCodeBase(SharkOpcodeType type, SharkOpcodeModel model, int lineNo, int codeNo){
                
                opType = type;
                opModel = model;
                line = lineNo;
                codeNumber = codeNo;
            }
            public override string ToString()
            {
                return $"{codeNumber.ToString()}: {opType.ToString()}";
            }
        }
        public class SharkInternalCodeNoParam: SharkInternalCodeBase{
            public SharkInternalCodeNoParam(SharkOpcodeType type, int line, int codeNumber):base(type, SharkOpcodeModel.MODEL_NO_PARAM, line, codeNumber){}
            public override string ToString()
            {
                return base.ToString();
            }
        }
        public class SharkInternalCode_1_32BIT: SharkInternalCodeBase{
            private DATA_1_32BIT __data;
            public int IntValue{
                get => __data.value_int;
                set => __data.value_int = value;
            }
            public float FloatValue{
                get => __data.value_float;
                set => __data.value_float = value;
            }
            public SharkInternalCode_1_32BIT(SharkOpcodeType type, int constID, int line, int codeNumber):
            base(type, SharkOpcodeModel.MODEL_PARAM_1_32BIT, line, codeNumber){
                __data = new DATA_1_32BIT();
                __data.value_int = constID;
            }
            public SharkInternalCode_1_32BIT(SharkOpcodeType type, float value, int line, int codeNumber):
            base(type, SharkOpcodeModel.MODEL_PARAM_1_32BIT, line, codeNumber){
                __data= new DATA_1_32BIT();
                __data.value_float = value;
            }
            public void SetData(int value){
                __data.value_int = value;
            }
            public void SetData(float value){
                __data.value_float = value;
            }
            public override string ToString()
            {
                if(opType == SharkOpcodeType.OP_PUSH_CONST){
                    return $"{codeNumber.ToString()}: {opType.ToString()} {SharkInternalCodeBase.currentScript.__get_const(__data.value_int)}";
                }else if(opType == SharkOpcodeType.OP_PUSH_VAR){
                    return $"{codeNumber.ToString()}: {opType.ToString()} {SharkInternalCodeBase.currentScript.getSymbol(__data.value_int)}";
                }else if(opType == SharkOpcodeType.OP_SET_LOCAL){
                    return $"{codeNumber.ToString()}: {opType.ToString()} {SharkInternalCodeBase.currentScript.getSymbol(__data.value_int)}";
                }else if(opType == SharkOpcodeType.OP_GET_FIELD){
                    return $"{codeNumber.ToString()}: {opType.ToString()} ({__data.value_int.ToString()}:{SharkInternalCodeBase.currentScript.getSymbol(__data.value_int)})";
                }else{
                    return $"{codeNumber.ToString()}: {opType.ToString()} {__data.value_int.ToString()}";
                }
            }

        }

        /// <summary>
        /// 指令流, 任何可执行单位或者模块单位都会有这样的一个指令流
        /// </summary>
        public class SharkILStream{

            private SharkInternalCodeBase[] codes;
            public int Size{
                get => codes.Length;
            }
            public void WriteCodes(List<SharkInternalCodeBase> buffer){
                codes = new SharkInternalCodeBase[buffer.Count];
                for(int i = 0; i < codes.Length; i ++){
                    codes[i] = buffer[i];
                }
            }
            public SharkInternalCodeBase getOpcode(int index){
                return codes[index];
            }
            public void showCodes(){

                foreach(SharkInternalCodeBase code in codes){
                    Console.WriteLine(code);
                }
            }
            public void WriteToList(List<string> list){
                foreach(SharkInternalCodeBase code in codes){
                    list.Add(code.ToString());
                }
            }
        }

        /// <summary>
        /// 变量访问的检查器, 用于检查目标变量是否不属于局部域或者全局域, 即只属于上级局部域, 如果是这样,
        /// 需要进行变量拷贝
        /// </summary>
        public class AccessChecker{

            public List<int> variables;         // 所有已经被写入局部域的变量
            public List<int> accessVariables;   // 所有被访问的变量
            public AccessChecker(){

                variables = new List<int>();
                accessVariables = new List<int>();
            }
            public void addVariable(int varID){

                if(variables.Contains(varID))return;
                variables.Add(varID);
            }
            public void addVariableAccessor(int varID){

                if(accessVariables.Contains(varID))return;
                accessVariables.Add(varID);
            }
        }
        public class SharkCodeEmitter{
            public int BufferSize{
                get => buffer.Count;
            }
            public int LastOpcodePosition{
                get => buffer.Count > 0 ?  buffer.Last().codeNumber : -1;
            }
            public int CurrentIfBlockQuitPosition{
                get => LastOpcodePosition + records.Count;
            }
            public bool isDefineFunctionNow{
                get => bufferStack.Count > 0;
            }

            public Stack<int> records;
            public int lastRecord;
            
            public List<SharkInternalCodeBase> buffer;
            public AccessChecker currentAccessChecker;

            public Stack<List<SharkInternalCodeBase>> bufferStack;
            public Stack<AccessChecker> checkerStack;

            //! 关于while循环的定义
            public bool isDefineWhile;
            public List<SharkInternalCode_1_32BIT> whileBreaks;
            public int whileEntrance;
            //! 关于while循环的定义

            //! 关于for循环的定义
            public bool isDefineFor;
            public List<SharkInternalCode_1_32BIT> forBreaks;
            public Stack<int> forEntrance;
            //! 关于for循环的定义

            public SharkCodeEmitter(){
                buffer = new List<SharkInternalCodeBase>();
                bufferStack = new Stack<List<SharkInternalCodeBase>>();
                records = new Stack<int>();
                checkerStack = new Stack<AccessChecker>();

                whileBreaks = new List<SharkInternalCode_1_32BIT>();
                forBreaks = new List<SharkInternalCode_1_32BIT>();
                forEntrance = new Stack<int>();
            }
            public void showBuffer(){

                for(int i = 0; i < buffer.Count; i ++){
                    Console.WriteLine($"{i.ToString()}: {buffer[i]}");
                }
            }
            public void Record(){
                records.Push(BufferSize);
            }
            public int RecordDone(){
                lastRecord = records.Pop();
                return BufferSize - lastRecord;
            }
            public void defineWhile(){
                isDefineWhile = true;
                whileEntrance = LastOpcodePosition;
            }
            public void endDefineWhile(){
                foreach(SharkInternalCode_1_32BIT opcode in whileBreaks){
                    opcode.IntValue = LastOpcodePosition;
                }
                isDefineWhile = false;
                whileBreaks.Clear();
            }
            public void pushWhileBreak(int line){
                SharkInternalCode_1_32BIT opcode = new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, 0, line, BufferSize);
                buffer.Add(opcode);
                whileBreaks.Add(opcode);
            }
            public void pushWhileContinue(int line){

                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, whileEntrance, line, BufferSize));
            }
            public void defineFor(){

                isDefineFor = true;
                forEntrance.Push(LastOpcodePosition);
            }
            public void holdBranchBuffer(){

                List<SharkInternalCodeBase> _buf = new List<SharkInternalCodeBase>();
                bufferStack.Push(buffer);
                buffer = _buf;
            }
            public List<SharkInternalCodeBase> releaseBranchBuffer(){

                List<SharkInternalCodeBase> _buf = buffer;
                buffer = bufferStack.Pop();
                return _buf;
            }
            public int endDefineFor(){
                int quitPosition = LastOpcodePosition;
                foreach(SharkInternalCode_1_32BIT opcode in forBreaks){
                    opcode.IntValue = quitPosition;
                }
                isDefineWhile = false;
                forBreaks.Clear();
                return forEntrance.Pop();
            }
            public void pushForBreak(int line){

                SharkInternalCode_1_32BIT opcode = new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, 0, line, BufferSize);
                buffer.Add(opcode);
                forBreaks.Add(opcode);
            }
            public void pushForContinue(int line){

                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.JMP_TO, forEntrance.Peek(), line, BufferSize));
            }
            public void defineFunction(){

                if(currentAccessChecker != null){
                    checkerStack.Push(currentAccessChecker);
                }
                currentAccessChecker = new AccessChecker();
                List<SharkInternalCodeBase> _buf = new List<SharkInternalCodeBase>();
                bufferStack.Push(buffer);                   // save parent buffer
                buffer = _buf;
            }
            public Tuple<SharkILStream, AccessChecker> endDefineFunction(int line){

                // 如果最后一个指令不是RETURN的话, 则新增一个RETURN指令
                if(buffer.Count == 0){
                    pushOperatorNoParam(SharkOpcodeType.RET_NONE, line);
                }else{
                    SharkInternalCodeBase codeBase= buffer[buffer.Count - 1];
                    if(codeBase.opType != SharkOpcodeType.RET && codeBase.opType != SharkOpcodeType.RET_NONE){
                        pushOperatorNoParam(SharkOpcodeType.RET_NONE, codeBase.line);
                    }
                }
                SharkILStream iLStream = new SharkILStream();
                iLStream.WriteCodes(buffer);
                buffer = bufferStack.Pop();
                Tuple<SharkILStream, AccessChecker> funcCore = new Tuple<SharkILStream, AccessChecker>(iLStream, currentAccessChecker);
                currentAccessChecker = checkerStack.Count > 0?checkerStack.Pop():null;
                return funcCore;
            }
            public void Emit(SharkInternalCodeBase code){
                buffer.Add(code);
            }
            public void EmitDone(SharkILStream ilstream){
                // ! 等所有的中间代码全部解析完成后写入目标脚本中, 脚本在外部生成

                ilstream.WriteCodes(buffer);
                buffer.Clear();
            }
            public SharkILStream EmitDone(bool clearBuffer){
                SharkILStream iLStream = new SharkILStream();
                iLStream.WriteCodes(buffer);
                if(clearBuffer)buffer.Clear();
                return iLStream;
            }
            public void insertCodeNoParam(int pos, SharkOpcodeType type, int line){
                buffer.Insert(pos, new SharkInternalCodeNoParam(type, line, pos));
                foreach(SharkInternalCodeBase opcode in buffer.GetRange(pos + 1, buffer.Count - pos)){
                    opcode.codeNumber ++;
                }
            }
            public void insertCode32Bit(int pos, SharkOpcodeType type, int value, int line){
                buffer.Insert(pos, new SharkInternalCode_1_32BIT(type, value, line, pos));
                foreach(SharkInternalCodeBase opcode in buffer.GetRange(pos + 1, buffer.Count - pos - 1)){
                    opcode.codeNumber ++;
                }
            }
            public void pushConst(int constID, int line){
                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_PUSH_CONST, constID, line, buffer.Count));
            }
            public void pushVar(int varID, int line){
                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_PUSH_VAR, varID, line, buffer.Count));
                if(currentAccessChecker != null){
                    currentAccessChecker.addVariableAccessor(varID);
                }
            }
            public void pushSetLocal(int varID, int line){

                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_SET_LOCAL, varID, line, buffer.Count));
                if(currentAccessChecker != null){
                    currentAccessChecker.addVariable(varID);
                }
            }
            public void pushFalse(int line){
                buffer.Add(new SharkInternalCodeNoParam(SharkOpcodeType.OP_PUSH_FALSE, line, buffer.Count));
            }
            public void pushTrue(int line){
                buffer.Add(new SharkInternalCodeNoParam(SharkOpcodeType.OP_PUSH_TRUE, line, buffer.Count));
            }
            public void pushOperatorNoParam(SharkOpcodeType type, int line){
                buffer.Add(new SharkInternalCodeNoParam(type, line, buffer.Count));
            }
            public void pushOperator32Bit(SharkOpcodeType type, int value, int line){
                buffer.Add(new SharkInternalCode_1_32BIT(type, value, line, buffer.Count));
            }
            public void pushGetField(int id, int line){
                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_GET_FIELD, id, line, buffer.Count));
            }
            public void pushCall(int count, int line){
                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.CALL, count, line, buffer.Count));
            }
            public void pushFunction(int functionID, int line){

                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_PUSH_FUNC, functionID, line, buffer.Count));
            }
            public void pushThis(int line){

                buffer.Add(new SharkInternalCodeNoParam(SharkOpcodeType.OP_PUSH_THIS, line, BufferSize));
            }
        }
        public interface SharkRunable{

            string getSymbol(int id);
            SkObject getVariable(int id);
            void setVariable(SkObject value, int id);
            bool hasParent();
            void loadCommands(SharkILStream stream);
            SkObject run(SkTuple paramlist);
            void runScript();
            void bindVM(SharkVM vm);
            int getCurrentLine();
            void jumpTo(int position);
            void jumpOffset(int length);
            int getEndPosition();
            void showCodes();
            void jumpToEnd();
            SkTable getThis();
        }
        public class SharkScript : SharkRunable{

            private SharkILStream codes;
            private SkTable thisTable;            // this表
            private Dictionary<int, SkObject> cons;                 // 常量表
            private Dictionary<int, SharkFunctionCore> functions;   // 函数表
            private Dictionary<int, string> symbols;                // 符号表
            private Dictionary<string, int> reverseSymbols;
            private int pointer;

            public bool DEBUG_MODEL = false;

            private SharkVM interpreter;
            private int line;
            public SharkScript(Dictionary<int, string> symbolTable, Dictionary<int, SkObject> constantTable):base(){

                symbols = symbolTable;
                reverseSymbols = new Dictionary<string, int>();
                foreach(KeyValuePair<int, string> value in symbols){
                    reverseSymbols.Add(value.Value, value.Key);
                }

                cons = constantTable;
                thisTable = new SkTable();
                foreach(SkPack pack in SharkCommonLib.commonLib){
                    thisTable.SetField(pack.value, pack.id);
                }
                functions = new Dictionary<int, SharkFunctionCore>();
            }
            public SkObject __get_const(int id){
                return cons[id];
            }
            public int getCurrentLine(){
                return line;
            }
            public string getSymbol(int id){
                return symbols[id];
            }
            public Dictionary<int, string> getSymbolTable(){
                return symbols;
            }
            public SkObject getVariable(int id){
                if(thisTable.containsId(id)){
                    return thisTable.__get_field(id);
                }
                throw new SharkNameError($"name {symbols[id]} is undefined at line {line.ToString()}");
            }
            public SkObject getVariable(string name){

                if(reverseSymbols.ContainsKey(name)){
                    return getVariable(reverseSymbols[name]);
                }
                throw new SharkNameError($"name \"{name}\" is undefined at line {line.ToString()}");
             }
            public void setVariable(SkObject value, int id){

                thisTable.SetField(value, id);
            }
            public bool hasParent(){
                return false;
            }
            public void loadCommands(SharkILStream stream){
                this.codes = stream;
            }

            public SkObject run(SkTuple paramlist){
                throw new SharkError("script cannot run as a funciton");
            }
            public void bindVM(SharkVM vm){
                this.interpreter = vm;
            }
            public void runScript(){

                while(pointer < codes.Size){
                    SharkInternalCodeBase code = codes.getOpcode(pointer);
                    line = code.line;
                    if(DEBUG_MODEL)Console.WriteLine(code);
                    switch(code.opModel){

                        case SharkOpcodeModel.MODEL_NO_PARAM:
                        interpreter.op_search_table_no_param[code.opType]();
                        break;

                        case SharkOpcodeModel.MODEL_PARAM_1_32BIT:
                        interpreter.op_search_table_1_32bit[code.opType](((SharkInternalCode_1_32BIT)code).IntValue);
                        break;
                    }
                    pointer ++;
                }
            }
            public void jumpTo(int position){
                pointer = position;
            }
            public void jumpOffset(int length){
                pointer += length;
            }
            public void jumpToEnd(){
                pointer = codes.Size;
            }
            public int getEndPosition(){
                return codes.Size;
            }
            public void showCodes(){

                codes.showCodes();
            }
            public void SaveToList(List<string> content){

                codes.WriteToList(content);
            }
            public int addNewConst(SkObject constValue){

                int constID = cons.Count;
                cons.Add(cons.Count, constValue);
                return constID;
            }
            public int registerNewFunction(SharkFunctionCore function){

                int id = functions.Count;
                functions.Add(id, function);
                return id;
            }
            public SharkFunctionCore getFunctionCore(int id){

                if(functions.ContainsKey(id)){
                    return functions[id];
                }
                return null;
            }
            public SkTable getThis(){
                return thisTable;
            }
            public void registerSymbol(string symbol){

                symbols.Add(symbols.Count, symbol);
                reverseSymbols.Add(symbol, reverseSymbols.Count);
            }
        }
        public delegate void opfunc_no_param();
        public delegate void opfunc_1_32bit(int ID);
        public delegate void opfunc_1_64bit(long ID);
        public enum FUNCTION_CALL_FLAG{

            JCALL,
            CALL
        }
        public class SharkVM{

            public static SharkVM vm;
            public static int CurrentLine{
                get => vm.currentLine;
            }
            public Stack<SkObject> stack;
            public SharkScript currentScript;
            public SharkRunable currentRunable;
            public List<SharkRunable> runableStack;

            private bool isCallFunctionNow;

            public Dictionary<SharkOpcodeType, opfunc_no_param> op_search_table_no_param;
            public Dictionary<SharkOpcodeType, opfunc_1_32bit> op_search_table_1_32bit;
            public Dictionary<SharkOpcodeType, opfunc_1_64bit> op_search_table_1_64bit;

            public int currentLine{
                get => currentRunable.getCurrentLine();
            }

            public SharkVM(){

                vm = this;
                runableStack = new List<SharkRunable>();
                stack = new Stack<SkObject>();
                op_search_table_no_param = new Dictionary<SharkOpcodeType, opfunc_no_param>();
                op_search_table_no_param.Add(SharkOpcodeType.OP_PUSH_FALSE, OP_PUSH_FALSE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_PUSH_TRUE, OP_PUSH_TRUE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_ADD, OP_ADD);
                op_search_table_no_param.Add(SharkOpcodeType.OP_SUB, OP_SUB);
                op_search_table_no_param.Add(SharkOpcodeType.OP_NEG, OP_NEG);
                op_search_table_no_param.Add(SharkOpcodeType.OP_MUL, OP_MUL);
                op_search_table_no_param.Add(SharkOpcodeType.OP_DIV, OP_DIV);
                op_search_table_no_param.Add(SharkOpcodeType.OP_MOD, OP_MOD);
                op_search_table_no_param.Add(SharkOpcodeType.OP_GT, OP_GT);
                op_search_table_no_param.Add(SharkOpcodeType.OP_GE, OP_GE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_LE, OP_LE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_LT, OP_LT);
                op_search_table_no_param.Add(SharkOpcodeType.OP_EQ, OP_EQ);
                op_search_table_no_param.Add(SharkOpcodeType.OP_NE, OP_NE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_NOT, OP_NOT);
                op_search_table_no_param.Add(SharkOpcodeType.OP_AND, OP_AND);
                op_search_table_no_param.Add(SharkOpcodeType.OP_OR, OP_OR);
                op_search_table_no_param.Add(SharkOpcodeType.OP_SET_FIELD, OP_SET_FIELD);
                op_search_table_no_param.Add(SharkOpcodeType.OP_END, OP_END);
                op_search_table_no_param.Add(SharkOpcodeType.RET, RET);
                op_search_table_no_param.Add(SharkOpcodeType.RET_NONE, RET_NONE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_PUSH_THIS, OP_PUSH_THIS);
                op_search_table_no_param.Add(SharkOpcodeType.OP_INDEX, INDEX);
                
                op_search_table_1_32bit = new Dictionary<SharkOpcodeType, opfunc_1_32bit>();
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_PUSH_VAR, OP_PUSH_VAR);
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_PUSH_CONST, OP_PUSH_CONST);
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_SET_LOCAL, OP_SET_LOCAL);
                op_search_table_1_32bit.Add(SharkOpcodeType.CALL, CALL);
                op_search_table_1_32bit.Add(SharkOpcodeType.JCALL, JCALL);
                op_search_table_1_32bit.Add(SharkOpcodeType.JMP_FALSE, JUMP_FALSE);
                op_search_table_1_32bit.Add(SharkOpcodeType.JMP_TO, JUMP_TO);
                op_search_table_1_32bit.Add(SharkOpcodeType.JMP, JUMP);
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_PUSH_FUNC, OP_PUSH_FUNC);
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_GET_FIELD, OP_GET_FIELD);
                op_search_table_1_32bit.Add(SharkOpcodeType.DEF_LIST, DEFINE_LIST);

                isCallFunctionNow = false;
            }

            public void RunScript(SharkScript script){

                LoadScript(script);
                script.runScript();
            }
            public void LoadScript(SharkScript script){

                currentScript = script;
                currentRunable = script;
                script.bindVM(this);
            }
            public SkTuple loadParamlist(int count){

                SkTuple tuple = new SkTuple(count);
                int endIndex = count - 1;
                for(int i = 0; i < count; i ++){
                    tuple.__set_index(endIndex - i, stack.Pop());
                }
                return tuple;
            }
            public void OP_PUSH_TRUE(){

                stack.Push(SkBool.TRUE);
            }
            public void OP_PUSH_FALSE(){

                stack.Push(SkBool.FALSE);
            }
            public void OP_PUSH_CONST(int id){

                stack.Push(currentScript.__get_const(id));
            }
            public void OP_PUSH_VAR(int id){
                
                stack.Push(currentRunable.getVariable(id));
            }
            public void OP_ADD(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Add(stack.Pop(), tmp));
            }
            public void OP_SUB(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Sub(stack.Pop(), tmp));
            }
            public void OP_NEG(){
                stack.Push(SharkAPI.Neg(stack.Pop()));
            }
            public void OP_MUL(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Mul(stack.Pop(), tmp));
            }
            public void OP_DIV(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Div(stack.Pop(), tmp));
            }
            public void OP_MOD(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Mod(stack.Pop(), tmp));
            }
            public void OP_GT(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Gt(stack.Pop(), tmp));
            }
            public void OP_GE(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Ge(stack.Pop(), tmp));
            }
            public void OP_LE(){

                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Le(stack.Pop(), tmp));
            }
            public void OP_LT(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Lt(stack.Pop(), tmp));
            }
            public void OP_NE(){

                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Ne(stack.Pop(), tmp));
            }
            public void OP_EQ(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Eq(stack.Pop(), tmp));
            }
            public void OP_AND(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.And(stack.Pop(), tmp));
            }
            public void OP_OR(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Or(stack.Pop(), tmp));
            }
            public void OP_NOT(){
                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Not(stack.Pop()));
            }
            public void OP_PUSH_LOCAL(int id){

                SkObject tmp = stack.Pop();
                switch(tmp.BaseType){

                    case SkObjectType.OT_TABLE:
                    stack.Push(((SkTable)tmp).Getter(id));
                    break;
                }
            }
            public void OP_SET_LOCAL(int id){

                SkObject value = stack.Pop();
                // Console.WriteLine($"准备写入值:{value}到变量:{currentScript.getSymbol(id)}, 当前运行时为:{currentRunable.GetHashCode()}");
                if(isCallFunctionNow && value.BaseType == SkObjectType.OT_FUNCTION_CORE){
                    // Console.WriteLine("正在设置一个函数对象");
                    SharkFunctionCore functionCore = (SharkFunctionCore)value;
                    SharkFunction func = new SharkFunction(functionCore);
                    func.bindParent(currentRunable, currentScript);
                    func.bindVM(this);
                    currentRunable.setVariable(func, id);
                }else{
                    currentRunable.setVariable(value, id);
                }
            }
            public void OP_SET_FIELD(){

                SkObject value = stack.Pop();
                if(value is SharkFunctionCore){
                    SharkFunction function = new SharkFunction((SharkFunctionCore)value);
                    function.bindParent(currentRunable, currentScript);
                    function.bindVM(this);
                    stack.Pop().RewriteObject(function);
                }else{
                    stack.Pop().RewriteObject(value);
                }
            }
            public void CALL(int count){

                SkTuple paramlist = loadParamlist(count);
                SkObject _obj = stack.Pop();
                if(_obj is NativeFunction || _obj is NativeMemberFunction){
                    // 目标对象是原生函数

                    SkCallable skCallable = (SkCallable)_obj;
                    skCallable.setFunctionFlag(FUNCTION_CALL_FLAG.CALL);
                    stack.Push(skCallable.call(paramlist));
                }else if(_obj is SharkFunctionCore){
                    // 目标对象是函数原型, 生成一个运行时栈区, 并在结束调用时销毁

                    isCallFunctionNow = true;
                    SharkFunction function = new SharkFunction((SharkFunctionCore)_obj);
                    function.setFunctionFlag(FUNCTION_CALL_FLAG.CALL);
                    function.bindVM(this);
                    function.bindParent(currentRunable, currentScript);
                    runableStack.Add(currentRunable);
                    currentRunable = function;
                    function.run(paramlist);
                    
                }else if(_obj is SharkFunction){
                    // 函数是一个闭包对象, 直接调用, 不处理栈区

                    isCallFunctionNow = true;
                    SharkFunction function = (SharkFunction)_obj;
                    function.setFunctionFlag(FUNCTION_CALL_FLAG.CALL);
                    runableStack.Add(currentRunable);
                    currentRunable = function;
                    function.run(paramlist);
                }else{
                    throw new SharkTypeError($"type \"{_obj.BaseType.ToString()}\" is not callable at line {currentLine.ToString()}");
                }
            }
            public void JCALL(int count){

                SkTuple paramlist = loadParamlist(count);
                SkObject _obj = stack.Pop();
                if(_obj is NativeFunction || _obj is NativeMemberFunction){

                    SkCallable skCallable = (SkCallable)_obj;
                    skCallable.setFunctionFlag(FUNCTION_CALL_FLAG.JCALL);
                    skCallable.call(paramlist);
                }else if(_obj is SharkFunctionCore){

                    isCallFunctionNow = true;
                    SharkFunction function = new SharkFunction((SharkFunctionCore)_obj);
                    function.setFunctionFlag(FUNCTION_CALL_FLAG.JCALL);
                    function.bindVM(this);
                    function.bindParent(currentRunable, currentScript);
                    runableStack.Add(currentRunable);
                    currentRunable = function;
                    function.run(paramlist);
                    
                }else if(_obj is SharkFunction){

                    isCallFunctionNow = true;
                    SharkFunction function = (SharkFunction)_obj;
                    function.setFunctionFlag(FUNCTION_CALL_FLAG.JCALL);
                    runableStack.Add(currentRunable);
                    currentRunable = function;
                    function.run(paramlist);
                }else{
                    throw new SharkTypeError($"type \"{_obj.BaseType.ToString()}\" is not callable at line {currentLine.ToString()}");
                }
            }
            public void INDEX(){

                SkObject index = stack.Pop();
                if(index.BaseType != SkObjectType.OT_VALUE){
                    throw new SharkTypeError($"type \"{index.BaseType.ToString()}\" cannot be index value at line {currentLine.ToString()}");
                }

                //stack.Push(SharkAPI.Index(stack.Pop(), index));

                SkValue _index = (SkValue)index;
                stack.Push(SharkAPI.Index(stack.Pop(), _index.getInt()));
            }
            public void JUMP_FALSE(int length){

                SkBool value = (SkBool)stack.Pop();
                if(value == SkBool.FALSE){
                    currentRunable.jumpOffset(length);
                }
            }

            public void JUMP_TO(int pos){
                currentRunable.jumpTo(pos);
            }
            public void JUMP(int length){
                currentRunable.jumpOffset(length);
            }

            public void OP_END(){
                currentRunable.jumpTo(currentRunable.getEndPosition());
            }
            public void RET(){

                // 如果该值不被需要的话, 会直接在RET时取出
                if(((SkCallable)currentRunable).getFunctionFlag() == 
                FUNCTION_CALL_FLAG.JCALL)stack.Pop();

                // 终止当前的对象
                currentRunable.jumpToEnd();

                // 更换当前的被执行单位
                // Console.WriteLine($"---------更换当前的运行时, from {currentRunable.GetHashCode()} to {runableStack[runableStack.Count - 1].GetHashCode()}");
                currentRunable = runableStack.Last();
                runableStack.Remove(currentRunable);
                isCallFunctionNow = runableStack.Count != 0;
            }
            public void RET_NONE(){

                // RET_NONE, 当该值是被其他单位访问时, 会向其中插入一个NONE
                if(((SkCallable)currentRunable).getFunctionFlag() != 
                FUNCTION_CALL_FLAG.JCALL)stack.Push(SkNull.NULL);

                // 终止当前的对象
                currentRunable.jumpToEnd();

                // 更换当前的被执行单位
                // Console.WriteLine("---------更换当前的运行时");
                currentRunable = runableStack.Last();
                runableStack.Remove(currentRunable);
                isCallFunctionNow = runableStack.Count != 0;
            }
            public void OP_PUSH_FUNC(int id){

                SharkFunctionCore functionCore = currentScript.getFunctionCore(id);
                if(functionCore != null){
                    stack.Push(functionCore);
                }else{
                    throw new SharkNameError($"name {currentScript.getSymbol(id)} is undefined at line {currentLine.ToString()}");
                }
            }
            public void OP_PUSH_THIS(){

                stack.Push(currentRunable.getThis());
            }
            public void OP_GET_FIELD(int id){

                SkObject instance = stack.Pop();
                switch(instance.BaseType){
                    case SkObjectType.OT_TABLE:
                    stack.Push(((SkTable)instance).GetField(id));
                    break;

                    case SkObjectType.OT_LIST:
                    stack.Push(((SkList)instance).getField(id));
                    break;

                    default:
                    throw new SharkTypeError($"cannot get field from type \"{instance.BaseType.ToString()}\" at line {currentLine.ToString()}");
                }

            }

            public void DEFINE_LIST(int count){

                SkList list = new SkList();
                for(int i = 0; i < count; i ++){
                    list.__append(stack.Pop());
                }
                list.Reverse();
                stack.Push(list);
            }
        }

        /// <summary>
        /// Shark脚本的模块系统
        /// </summary>
        public class SharkModule{


        }
    }

    namespace SharkCore{

        public static class SharkCore{

            public static SkNull NULL = new SkNull();
            public static SkBool TRUE = new SkBool(1);
            public static SkBool FALSE = new SkBool(0);
            public static SkTuple emptyList = new SkTuple(0);
        }


        public static class SharkCoreUtils{

            public static void __save_int(byte[] data, int value){

                data[0] = (byte)(value >> 24);
                data[1] = (byte)(value >> 16);
                data[2] = (byte)(value >> 8);
                data[3] = (byte)value;
            }
            public static int __load_int(byte[] data){
                return (int)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
            }
            public static void __save_long(byte[] data, long value){

                data[0] = (byte)(value >> 56);
                data[1] = (byte)(value >> 48);
                data[2] = (byte)(value >> 40);
                data[3] = (byte)(value >> 32);
                data[4] = (byte)(value >> 24);
                data[5] = (byte)(value >> 16);
                data[6] = (byte)(value >> 8);
                data[7] = (byte)value;
            }
            public static long __load_long(byte[] data){

                long o = ((long)data[0] << 56)|
                ((long)data[1] << 48)|
                ((long)data[2] << 40)|
                ((long)data[3] << 32)|
                ((long)data[4] << 24)|
                ((long)data[5] << 16)|
                ((long)data[6] << 8)|
                ((long)data[7]);
                return o;
            }
        }

        /// <summary>
        /// Shark所有的对象类型
        /// </summary>
        public enum SkObjectType{

            OT_UNDEFINED,
            OT_NULL,
            OT_FUNCTION,
            OT_FUNCTION_CORE,
            OT_TUPLE,
            OT_LIST,
            OT_TABLE,
            OT_MODULE,
            OT_STRING,
            OT_VALUE,
        }

        /// <summary>
        /// Shark支持的值类型对象
        /// </summary>
        public enum SkValueType{

            VT_BOOLEAN,
            VT_FLOAT,
            VT_INT,
        }


        /// <summary>
        /// Shark对象的头部信息, 任何一个Shark对象都会拥有一个对象头, 用于索引该对象
        /// 的函数或者字段
        /// </summary>
        public interface SharkInstanceHeader{

            public string InstanceName();
            public SkObject Getter(string name);
            public void Setter(string name, SkObject value);
        }

        public delegate SkObject __shark_method_prototype(SkTuple args);

        public static class SharkAPI{

            public static SkBool And(SkBool left, SkBool right){
                return SkBool.Bools[left.data * right.data];
            }
            public static SkBool Or(SkBool left, SkBool right){
                return SkBool.Bools[Math.Min(1, left.data + right.data)];
            }
            public static SkBool Not(SkBool value){
                return SkBool.Bools[1 - value.data];
            }
            public static SkObject Add(SkObject left, SkObject right){
                if(left.BaseType == right.BaseType){
                    if(left.BaseType == SkObjectType.OT_VALUE){
                        return ((SkValue)left).Add((SkValue)right);
                    }else if(left.BaseType == SkObjectType.OT_STRING){
                        return new SkString(((SkString)left).text + ((SkString)right).text);
                    }
                }
                throw new SharkOperationError($"add operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Sub(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Sub((SkValue)right);
                }
                throw new SharkOperationError($"sub operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Neg(SkObject value){

                if(value.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)value).Neg();
                }
                throw new SharkOperationError($"neg operation is unsupportted on type {value.BaseType}");
            }
            public static SkObject Mul(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Mul(((SkValue)right));
                }
                throw new SharkOperationError($"mul operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Div(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Div(((SkValue)right));
                }
                throw new SharkOperationError($"div operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Mod(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Mod(((SkValue)right));
                }
                throw new SharkOperationError($"mod operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Ge(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Ge(((SkValue)right));
                }
                throw new SharkOperationError($">= operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Gt(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Gt(((SkValue)right));
                }
                throw new SharkOperationError($"> operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Le(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Le(((SkValue)right));
                }
                throw new SharkOperationError($"<= operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Lt(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Lt(((SkValue)right));
                }
                throw new SharkOperationError($"< operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Eq(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType){
                    if(left.BaseType == SkObjectType.OT_VALUE){
                        return ((SkValue)left).Eq(((SkValue)right));
                    }else{
                        return left == right ? SkBool.TRUE : SkBool.FALSE;
                    }
                }else{
                    return SkBool.FALSE;
                }
            }

            /// <summary>
            /// 仅用于检查常量是否相等
            /// </summary>
            public static bool ConstantEquals(SkObject left, SkObject right){
                if(left.BaseType == right.BaseType){
                    switch(left.BaseType){
                        case SkObjectType.OT_STRING:
                        return ((SkString)left).text.Equals(((SkString)right).text);
                        
                        case SkObjectType.OT_VALUE:
                        return ValueEquals((SkValue)left, (SkValue)right);

                        default:
                        return false;
                    }
                }
                return false;
            }
            public static bool ValueEquals(SkValue left, SkValue right){

                return left.getFloat() == right.getFloat();
            }
            public static SkObject Ne(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Ne(((SkValue)right));
                }
                throw new SharkOperationError($"!= operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Not(SkObject value){

                if(value.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)value).Not();
                }
                throw new SharkOperationError($"! operation is unsupportted on {value.BaseType}");
            }
            public static SkObject And(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).And(((SkValue)right));
                }
                throw new SharkOperationError($"&& operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Or(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType && left.BaseType == SkObjectType.OT_VALUE){
                    return ((SkValue)left).Or(((SkValue)right));
                }
                throw new SharkOperationError($"|| operation is unsupportted between {left.BaseType} and {right.BaseType}");
            }
            public static SkObject Index(SkObject list, int index){
                
                if(list.BaseType == SkObjectType.OT_LIST){

                    SkList _list = (SkList)list;
                    if(index >= 0 && index < _list.Size){
                        return _list.__index(index);
                    }
                    throw new SharkIndexOutOfArrayError($"index out of range at line {SharkVM.CurrentLine.ToString()}");
                }
                throw new SharkOperationError($"cannot index data from type {list.BaseType.ToString()} at line {SharkVM.CurrentLine.ToString()}");
            }
        }


        public abstract class SkClass{

            protected Dictionary<int, __c_member_function> sourceApi;          //API的原型, 此时没有赋值给任何单位
            public SkClass(){
                sourceApi = new Dictionary<int, __c_member_function>();
            }
            public abstract __c_member_function searchApi(int ID);
        }
        public class SkListClass: SkClass{

            public static SkListClass defaultClass = new SkListClass(5);

            public SkListClass(int offset):base(){
                sourceApi.Add(offset, __append);
            }

            public static SkObject __append(SkObject instance, SkTuple paramlist){
                ((SkList)instance).__append(paramlist.__index(0));
                return SkNull.NULL;
            }
            public override __c_member_function searchApi(int ID)
            {
                if(sourceApi.ContainsKey(ID)){
                    return sourceApi[ID];
                }
                return null;
            }

        }

        /// <summary>
        /// Shark数据基类
        /// </summary>
        public class SkObject{

            private int id;
            private string symbol;
            protected SkTable header;

            private SkObjectType baseType;

            public int ID{
                get => id;
            }
            public string Symbol{
                get => symbol;
            }
            public SkObjectType BaseType{
                get => baseType;
            }
            public SkObject(SkObjectType type){
                baseType = type;
                this.id = -1;
                this.symbol = null;
            }
            public SkObject(int id, string symbol, SkObjectType type){
                baseType = type;
                this.id = id;
                this.symbol = symbol;
            }
            public void RewriteObject(SkObject value){

                if(header == null){
                    // 如果目标对象没有Header，则说明它不可写入
                    return;
                }
                value.inheritFrom(this);
                header.SetField(value, id);
            }
            public void bindHeader(SkTable header){
                this.header = header;
            }
            public void inheritFrom(SkObject old){

                id = old.ID;
                symbol = old.Symbol;
                bindHeader(old.header);
            }
        }
        public class SkUndefined: SkObject{

            public SkUndefined(int id, string symbol):base(id, symbol, SkObjectType.OT_UNDEFINED){}
            public override string ToString()
            {
                return "undefined";
            }
        }
        public class SkNull: SkObject{
            public static SkNull NULL = new SkNull();
            public SkNull():base(SkObjectType.OT_NULL){}
            public override string ToString()
            {
                return "null";
            }
        }

        public enum SkFunctionType{

            NativeCS,
            NativeCSMem,
            Shark,
        }
        public abstract class SkCallable: SkObject{

            public readonly SkFunctionType callableType;
            public SkCallable(SkFunctionType type):base(SkObjectType.OT_FUNCTION){

                callableType = type;
            }
            public abstract SkObject call(SkTuple paramlist);
            public abstract FUNCTION_CALL_FLAG getFunctionFlag();
            public abstract void setFunctionFlag(FUNCTION_CALL_FLAG value);
        }
        public delegate SkObject __c_function(SkTuple paramlist);
        public delegate SkObject __c_member_function(SkObject instance, SkTuple paramlist);

        public class NativeFunction: SkCallable{

            __c_function __callable;
            private FUNCTION_CALL_FLAG flag;
            public NativeFunction(__c_function target):base(SkFunctionType.NativeCS){
                __callable = target;
            }
            public override SkObject call(SkTuple paramlist){
                return __callable(paramlist);
            }
            public override FUNCTION_CALL_FLAG getFunctionFlag()
            {
                return flag;
            }
            public override void setFunctionFlag(FUNCTION_CALL_FLAG value)
            {
                flag = value;
            }
        }
        public class NativeMemberFunction: SkCallable{

            __c_member_function __callable;
            public SkObject instance;
            private FUNCTION_CALL_FLAG flag;
            public NativeMemberFunction(SkObject instance, __c_member_function target):base(SkFunctionType.NativeCSMem){
                __callable = target;
                this.instance = instance;
            }
            public override SkObject call(SkTuple paramlist){
                return __callable(instance, paramlist);
            }
            public override FUNCTION_CALL_FLAG getFunctionFlag()
            {
                return flag;
            }
            public override void setFunctionFlag(FUNCTION_CALL_FLAG value)
            {
                flag = value;
            }
        }



        public class SharkFunctionCore : SkObject{

            private SharkILStream codes;             // 函数中间代码
            private List<int> accessList;
            private List<int> paramlist;
            private List<int> holderList;
            private List<int> upValuelist;
            
            public SharkFunctionCore():base(SkObjectType.OT_FUNCTION_CORE){
                this.upValuelist = new List<int>();
            }
            public int StreamSize{
                get => codes.Size;
            }
            public int paramCount{
                get => paramlist.Count;
            }
            public SharkILStream Stream{
                get => codes;
            }
            public void loadFunctionCore(Tuple<SharkILStream, AccessChecker> functionCore, List<int> paramlist){

                this.codes = functionCore.Item1;
                this.accessList = functionCore.Item2.accessVariables;
                this.holderList = functionCore.Item2.variables;
                this.paramlist = paramlist;

                this.upValuelist = new List<int>();
                foreach(int varID in accessList){
                    if(!searchVariable(varID))upValuelist.Add(varID);
                }
            }
            public bool searchVariable(int id){
                // 查询一个变量是否存在于参数列表或者持有变量集合中

                foreach(int varID in this.holderList){
                    if(varID == id){
                        return true;
                    }
                }
                foreach(int varID in this.paramlist){
                    if(varID == id){
                        return true;
                    }
                }
                return false;
            }
            public int getParam(int index){
                return paramlist[index];
            }
            public SharkInternalCodeBase getOpcode(int pointer){
                return codes.getOpcode(pointer);
            }
            public void saveToList(List<string> buffer){
                codes.WriteToList(buffer);
            }
        }

        /// <summary>
        /// Shark风格的函数, 应该提供中间代码写入的接口.
        /// </summary>
        public class SharkFunction: SkCallable, SharkRunable{

            // --- 函数基础信息 ---
            private SharkFunctionCore function;
            // private SharkFunctionRuntimeStack runtimeStack;
            private SkTable thisTable;
            private Dictionary<int, SkObject> runtimeStack;
            private SharkRunable parent;
            private SharkScript script;
            private int pointer;

            private SharkVM interpreter;
            private int line;
            private FUNCTION_CALL_FLAG flag;

            public bool ShouldContinue{
                get => pointer < function.StreamSize;
            }
            public SharkRunable Parent{
                get => parent;
            }
            public SharkFunction(SharkFunctionCore core): base(SkFunctionType.Shark){

                this.function = core;
                this.script = null;
                this.parent = null;
                runtimeStack = new Dictionary<int, SkObject>();
                header = new SkTable();
            }
            public void bindParent(SharkRunable parent){
                this.parent = parent;
            }
            public void bindParent(SharkRunable parent, SharkScript script){
                this.script = script;
                this.parent = parent;
            }
            public override SkObject call(SkTuple paramlist)
            {
                throw new NotImplementedException();
            }
            public string getSymbol(int id){
                return script.getSymbol(id);
            }
            public SkObject getVariable(int id){
                if(runtimeStack.ContainsKey(id)){
                    return runtimeStack[id];
                }else{
                    return parent.getVariable(id);
                }
            }
            /// <summary>
            /// 设置函数当前的变量
            /// </summary>
            public void setVariable(SkObject value, int id){
                if(runtimeStack.ContainsKey(id)){
                    runtimeStack[id] = value;
                }else{
                    runtimeStack.Add(id, value);
                }
            }
            public bool hasParent(){
                return parent != null;
            }
            public void loadCommands(SharkILStream stream){}
            /// <summary>
            /// 执行一个Shark风格的函数
            /// </summary>
            public SkObject run(SkTuple args){

                if(function.paramCount != args.Size){
                    throw new SharkArgumentError($"invalid argument list at line {interpreter.currentLine.ToString()}");
                }
                for(int i = 0; i < function.paramCount; i ++){
                    setVariable(args.__index(i), function.getParam(i));
                }
                while(ShouldContinue){
                    SharkInternalCodeBase code = function.getOpcode(pointer);
                    line = code.line;
                    if(script.DEBUG_MODEL)Console.WriteLine(code);
                    switch(code.opModel){
                        case SharkOpcodeModel.MODEL_NO_PARAM:
                        interpreter.op_search_table_no_param[code.opType]();
                        break;

                        case SharkOpcodeModel.MODEL_PARAM_1_32BIT:
                        interpreter.op_search_table_1_32bit[code.opType](((SharkInternalCode_1_32BIT)code).IntValue);
                        break;
                    }
                    pointer ++;
                }
                return null;
            }
            public int getCurrentLine(){

                return line;
            }
            public void runScript(){

                throw new SharkError($"cannot call function as script at line {line.ToString()}");
            }
            public void bindVM(SharkVM vm){
                interpreter = vm;
            }
            public void jumpTo(int position){
                pointer = position;
            }
            public void jumpOffset(int length){
                pointer += length;
            }
            public void jumpToEnd(){
                pointer = function.StreamSize;
            }
            public int getEndPosition(){
                return function.StreamSize;
            }
            public override FUNCTION_CALL_FLAG getFunctionFlag()
            {
                return flag;
            }
            public override void setFunctionFlag(FUNCTION_CALL_FLAG value)
            {
                flag = value;
            }
            public void showCodes(){
                function.Stream.showCodes();
            }
            public void SaveToList(List<string> contents){

                contents.Add("[指令集]");
                function.Stream.WriteToList(contents);
                contents.Add(string.Empty);
                // contents.Add("[访问变量集合]");
                // foreach(int id in accessList){
                //     contents.Add($"{id.ToString()} : {script.getSymbol(id)}");
                // }
                // contents.Add(string.Empty);
                // contents.Add("[持有变量集合]");
                // foreach(int id in holderList){
                //     contents.Add($"{id.ToString()} : {script.getSymbol(id)}");
                // }
                // contents.Add(string.Empty);
                // contents.Add("[上值集合]");
                // foreach(int id in upValuelist){
                //     contents.Add($"{id.ToString()} : {script.getSymbol(id)}");
                // }
                // contents.Add(string.Empty);
            }
            public SkTable getThis(){
                return header;
            }
        }

        /// <summary>
        /// Shark元表类, 会维护一个字典, 用字典来模拟类的实现
        /// </summary>
        public class SkTable: SkObject{

            public Dictionary<int, SkObject> table;
            private readonly bool isFixedTable;
            public SkTable(bool isFixed = false):base(SkObjectType.OT_TABLE){

                table = new Dictionary<int, SkObject>();
                isFixedTable = isFixed;
            }
            public SkObject Getter(int id){

                if(table.ContainsKey(id)){
                    return table[id];
                }
                return SkNull.NULL;
            }
            public void Setter(SkObject value, int id){

                if(isFixedTable)return;
                if(table.ContainsKey(id)){
                    table[id] = value;
                }else{
                    table.Add(id, value);
                }
            }
 
            /// <summary>
            /// dot value from 'this' table
            /// </summary>
            public SkObject GetField(int id){
                
                if(table.ContainsKey(id)){
                    return table[id];
                }
                SkObject value = new SkUndefined(id, SharkVM.vm.currentScript.getSymbol(id));
                value.bindHeader(this);
                table.Add(id, value);
                return value;
            }
            public void SetField(SkObject value, int id){
                if(table.ContainsKey(id)){
                    table[id] = value;
                }else{
                    table.Add(id, value);
                }
            }
            public bool containsId(int id){
                return table.ContainsKey(id);
            }
            public SkObject __get_field(int id){
                return table[id];
            }
        }

        public class SkString: SkObject{

            public string text;
            public SkString():base(SkObjectType.OT_STRING){
                text = string.Empty;
            }
            public SkString(string text):base(SkObjectType.OT_STRING){
                this.text = text;
            }
            public override string ToString()
            {
                return text;
            }
        }

        public abstract class SkValue: SkObject{

            protected SkValueType valueType;
            public SkValueType ValueType{
                get => valueType;
            }
            public SkValue(SkValueType type):base(SkObjectType.OT_VALUE){
                valueType = type;
            }
            public T GetInstance<T>() where T: SkValue{
                return (T)this;
            }
            public abstract int getInt();
            public abstract float getFloat();
            public abstract SkValue Add(SkValue other);
            public abstract SkValue Sub(SkValue other);
            public abstract SkValue Mul(SkValue other);
            public abstract SkValue Div(SkValue other);
            public abstract SkValue Mod(SkValue other);
            public abstract SkValue Neg();
            public abstract SkValue Ge(SkValue other);
            public abstract SkValue Gt(SkValue other);
            public abstract SkValue Le(SkValue other);
            public abstract SkValue Lt(SkValue other);
            public abstract SkValue Eq(SkValue other);
            public abstract SkValue Ne(SkValue other);
            public abstract SkValue And(SkValue other);
            public abstract SkValue Or(SkValue other);
            public abstract SkValue Not();
        }

        public class SkInt: SkValue{
            public int data;
            public SkInt():base(SkValueType.VT_INT){
                data = 0;
            }
            public SkInt(int value):base(SkValueType.VT_INT){
                data = value;
            }
            public SkBool toBool(){
                return data == 0 ? SkBool.FALSE : SkBool.TRUE;
            }
            public override string ToString()
            {
                return data.ToString();
            }
            public override float getFloat()
            {
                return (float)data;
            }
            public override int getInt(){
                return data;
            }
            public override SkValue Add(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkInt(data + other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data + other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkInt(data + other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Sub(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkInt(data - other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data - other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkInt(data - other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Mul(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkInt(data * other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data * other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkInt(data * other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Div(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkInt(data / other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data / other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkInt(data / other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Mod(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkInt(data % other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data % other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkInt(data % other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Neg(){
                return new SkInt(-data);
            }
            public override SkValue Ge(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data >= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data >= other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data >= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Gt(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data > other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data > other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data > other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Le(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data <= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data <= other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data <= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Lt(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data < other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data < other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data < other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Eq(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data == other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data == other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data == other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Ne(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data != other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data != other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data != other.GetInstance<SkBool>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue And(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return SharkAPI.And(GetInstance<SkInt>().toBool(), other.GetInstance<SkInt>().toBool());

                    case SkValueType.VT_FLOAT:
                    return SharkAPI.And(GetInstance<SkFloat>().toBool(), other.GetInstance<SkFloat>().toBool());

                    case SkValueType.VT_BOOLEAN:
                    return SharkAPI.And(GetInstance<SkBool>(), other.GetInstance<SkBool>());

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Or(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return SharkAPI.Or(GetInstance<SkInt>().toBool(), other.GetInstance<SkInt>().toBool());

                    case SkValueType.VT_FLOAT:
                    return SharkAPI.Or(GetInstance<SkFloat>().toBool(), other.GetInstance<SkFloat>().toBool());

                    case SkValueType.VT_BOOLEAN:
                    return SharkAPI.Or(GetInstance<SkBool>(), other.GetInstance<SkBool>());

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Not(){

                switch(ValueType){
                    case SkValueType.VT_INT:
                    return SharkAPI.Not(GetInstance<SkInt>().toBool());

                    case SkValueType.VT_FLOAT:
                    return SharkAPI.Not(GetInstance<SkFloat>().toBool());

                    case SkValueType.VT_BOOLEAN:
                    return SharkAPI.Not(GetInstance<SkBool>());

                    default:
                    // this code would never be run
                    return null;
                }
            }
        }

        public class SkFloat: SkValue{

            public float data;
            public SkFloat():base(SkValueType.VT_FLOAT){
                data = 0;
            }
            public SkFloat(float value):base(SkValueType.VT_FLOAT){
                this.data = value;
            }
            public SkBool toBool(){

                return data == 0 ? SkBool.FALSE : SkBool.TRUE;
            }
            public override string ToString()
            {
                return data.ToString();
            }
            public override float getFloat()
            {
                return data;
            }
            public override int getInt()
            {
                return (int)data;
            }
            public override SkValue Add(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkFloat(data + other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data + other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkFloat(data + other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Sub(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkFloat(data - other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data - other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkFloat(data - other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Mul(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkFloat(data * other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data * other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkFloat(data * other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Div(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkFloat(data / other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data / other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkFloat(data / other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Mod(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return new SkFloat(data % other.GetInstance<SkInt>().data);

                    case SkValueType.VT_FLOAT:
                    return new SkFloat(data % other.GetInstance<SkFloat>().data);

                    case SkValueType.VT_BOOLEAN:
                    return new SkFloat(data % other.GetInstance<SkBool>().data);

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Neg(){
                return new SkFloat(-data);
            }
            public override SkValue Ge(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data >= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data >= other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data >= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Gt(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data > other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data > other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data > other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Le(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data <= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data <= other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data <= other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Lt(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data < other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data < other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data < other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Eq(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data == other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data == other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data == other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Ne(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return data != other.GetInstance<SkInt>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_FLOAT:
                    return data != other.GetInstance<SkFloat>().data ? SkBool.TRUE : SkBool.FALSE;

                    case SkValueType.VT_BOOLEAN:
                    return data != other.GetInstance<SkBool>().data ? SkBool.TRUE : SkBool.FALSE;

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue And(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return SharkAPI.And(GetInstance<SkInt>().toBool(), other.GetInstance<SkInt>().toBool());

                    case SkValueType.VT_FLOAT:
                    return SharkAPI.And(GetInstance<SkFloat>().toBool(), other.GetInstance<SkFloat>().toBool());

                    case SkValueType.VT_BOOLEAN:
                    return SharkAPI.And(GetInstance<SkBool>(), other.GetInstance<SkBool>());

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Or(SkValue other){

                switch(other.ValueType){
                    case SkValueType.VT_INT:
                    return SharkAPI.Or(GetInstance<SkInt>().toBool(), other.GetInstance<SkInt>().toBool());

                    case SkValueType.VT_FLOAT:
                    return SharkAPI.Or(GetInstance<SkFloat>().toBool(), other.GetInstance<SkFloat>().toBool());

                    case SkValueType.VT_BOOLEAN:
                    return SharkAPI.Or(GetInstance<SkBool>(), other.GetInstance<SkBool>());

                    default:
                    // this code would never be run
                    return null;
                }
            }
            public override SkValue Not(){

                switch(ValueType){
                    case SkValueType.VT_INT:
                    return SharkAPI.Not(GetInstance<SkInt>().toBool());

                    case SkValueType.VT_FLOAT:
                    return SharkAPI.Not(GetInstance<SkFloat>().toBool());

                    case SkValueType.VT_BOOLEAN:
                    return SharkAPI.Not(GetInstance<SkBool>());

                    default:
                    // this code would never be run
                    return null;
                }
            }
        }

        public class SkBool: SkInt{

            public static SkBool TRUE = new SkBool(1);
            public static SkBool FALSE = new SkBool(0);
            public static SkBool[] Bools = new SkBool[]{FALSE, TRUE};
            public static string[] BoolStrings = new string[]{"false", "true"};            
            public SkBool(int value):base(value){}
            public override string ToString()
            {
                return BoolStrings[data];
            }
        }

        public class SkList: SkObject{

            private SkListClass metaClass;
            private Dictionary<int, NativeMemberFunction> apis;
            private List<SkObject> list;
            public int Size{
                get => list.Count;
            }
            public SkList():base(SkObjectType.OT_LIST){
                list = new List<SkObject>();
                apis = new Dictionary<int, NativeMemberFunction>();
                metaClass = SkListClass.defaultClass;
            }
            public SkObject getField(int ID){

                if(apis.ContainsKey(ID)){
                    return apis[ID];
                }else{
                    __c_member_function source = metaClass.searchApi(ID);
                    if(source == null){
                        throw new SharkNameError($"list has no attribute called {SharkVM.vm.currentScript.getSymbol(ID)} at line {SharkVM.CurrentLine.ToString()}");
                    }else{
                        NativeMemberFunction function = new NativeMemberFunction(this, source);
                        apis.Add(ID, function);
                        return function;
                    }
                }
            }
            public void __append(SkObject item){
                list.Add(item);
            }
            public SkObject __index(int index){
                return list[index];
            }
            public SkObject Append(SkTuple paramList){
                
                if(paramList.Size == 1){
                    list.Add(paramList.__index(0));
                    return SkNull.NULL;
                }
                throw new SharkArgumentError($"invalid argument length for \"append\" function");
            }
            public void Remove(SkObject item){
                list.Remove(item);
            }
            public SkObject Pop(){
                SkObject o = list.Last();
                list.RemoveAt(list.Count - 1);
                return o;
            }
            public void Reverse(){
                list.Reverse();
            }
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder("[");
                foreach(SkObject item in list){
                    builder.Append(item.ToString() + ",");
                }
                builder.Remove(builder.Length - 1, 1);
                builder.Append("]");
                return builder.ToString();
            }
        }

        public class SkTuple: SkObject{

            public static SkTuple emptyList = new SkTuple(0);

            private SkObject[] tuple;
            public int Size{
                get => tuple.Length;
            }
            public SkTuple(int length):base(SkObjectType.OT_TUPLE){
                tuple = new SkObject[length];
            }
            public SkObject __index(int index){
                return tuple[index];
            }
            public void __set_index(int index, SkObject value){
                tuple[index] = value;
            }
        }
    }
}