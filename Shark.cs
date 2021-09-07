using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Shark.SharkVirtualMachine;
using Shark.SharkLexer;
using Shark.SharkCore;

namespace Shark{

    public class SharkError: Exception{
        public SharkError(string errInfo):base(errInfo){}
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
            public SharkTokenConstant(Dictionary<int, SkObject> constTable, SkObject con, int lineNo, string content, SkTokenType type):
            base(lineNo, content, type, TokenModel.MODEL_TOKEN_IS_CONSTANT){

                constant = con;
                constID = constTable.Count;
                constTable.Add(constID, con);
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
            public override string ToString()
            {
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
                
                new SharkToken(SkTokenType.TOKEN_FUNC, "funciton", TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS),
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

            public Dictionary<int, SkObject> constTable;
            public Dictionary<int, string> SymbolTable;
            public Dictionary<string, int> ReverseSymbolTable;

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

                    return new SharkTokenConstant(constTable, SkObject.SkNull, lineNo, content, SkTokenType.TOKEN_NULL);
                }else if(content == "true"){

                    return new SharkTokenConstant(constTable, SkBool.SkTRUE, lineNo, content, SkTokenType.TOKEN_TRUE);
                }else if(content == "false"){

                    return new SharkTokenConstant(constTable, SkBool.SkFALSE, lineNo, content, SkTokenType.TOKEN_FALSE);
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
                ReverseSymbolTable = new Dictionary<string, int>();
                SymbolTable = new Dictionary<int, string>();
                constTable = new Dictionary<int, SkObject>();
            }
            public SkLexer(string sourceCode){
                LoadSourceCode(sourceCode);
                ReverseSymbolTable = new Dictionary<string, int>();
                SymbolTable = new Dictionary<int, string>();
                constTable = new Dictionary<int, SkObject>();
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
                return new SharkScript(SymbolTable, constTable);
            }
            private SharkToken makeToken(SkTokenType type){
                return new SharkTokenUniversal(currentLine, null, type);
            }
            private SharkToken makeToken(SkTokenType type, string content){
                return new SharkTokenUniversal(currentLine, content, type);
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
                        return makeToken(SkTokenType.TOKEN_INT, parseSpecNumber(SharkLexerCore.isHexChar, "invalid hex number"));
                    }else if(matchChar('o')){
                        return makeToken(SkTokenType.TOKEN_INT, parseSpecNumber(SharkLexerCore.isOctChar, "invalid oct number"));
                    }else if(matchChar('b')){
                        return makeToken(SkTokenType.TOKEN_INT, parseSpecNumber(SharkLexerCore.isBinChar, "invalid bin number"));
                    }else{
                        return makeToken(SkTokenType.TOKEN_INT, C.ToString());
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
            public int currentLine;

            public SkParser(){

                emitter = new SharkCodeEmitter();
                currentPos = 0;
                currentLine = 1;
            }
            public SharkScript generateScript(){

                SharkILStream ilstream = emitter.EmitDone();
                currentScript.loadCommands(ilstream);
                return currentScript;
            }

            public SharkToken currentToken{
                get => tokens[currentPos];
            }

            public void moveToNext(){

                currentPos ++;
                if(currentToken.tokenModel != TokenModel.MODEL_TOKEN_IS_SYSTEMWORDS){
                    currentLine = ((SharkTokenUniversal)currentToken).line;
                }
            }
            public bool matchToken(SkTokenType type){

                if(currentToken == type){
                    moveToNext();
                    return true;
                }
                return false;
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
            }

            public void nextFactor(){
                // 找到下一个因子

                SharkToken token = currentToken;
                switch(token.tokenType){

                    case SkTokenType.TOKEN_INT:
                    emitter.pushConst(((SharkTokenConstant)token).constID);
                    break;

                    case SkTokenType.TOKEN_FLOAT:
                    emitter.pushConst(((SharkTokenConstant)token).constID);
                    break;

                    case SkTokenType.TOKEN_FALSE:
                    emitter.pushFalse();
                    break;
                }
                moveToNext();
            }

            public void nextNegative(){

                if(matchToken(SkTokenType.TOKEN_SUB)){
                    nextFactor();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_NEG);
                }else{
                    nextFactor();
                }
            }
            public void nextMulDiv(){

                nextNegative();
                if(matchToken(SkTokenType.TOKEN_MUL)){

                    nextNegative();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_MUL);
                }else if(matchToken(SkTokenType.TOKEN_DIV)){

                    nextNegative();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_DIV);
                }
            }
            public void nextAddSub(){

                nextMulDiv();
                if(matchToken(SkTokenType.TOKEN_ADD)){

                    nextMulDiv();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_ADD);
                }else if(matchToken(SkTokenType.TOKEN_SUB)){
                    
                    nextMulDiv();
                    emitter.pushOperatorNoParam(SharkOpcodeType.OP_SUB);
                }
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

            // 从堆栈中取出两个值进行运算
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

            // 从堆栈中取出一个值进行运算
            OP_NOT,
            OP_NEG,
            OP_BIT_NOT,

            // 型号2, PARAM_32BIT
            OP_PUSH_VAR,           // 向堆栈中压入一个变量
            OP_PUSH_CONST,         // 向堆栈中压入一个常量
            OP_PUSH_INT,           // 向堆栈中压入一个整型值
            OP_LOAD_VAR,           // 从堆栈顶端弹出一个变量并存入目标变量
            OP_PUSH_FLOAT,         // 向堆栈中压入一个单精度值


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
            public readonly SharkOpcodeType opType;
            public readonly SharkOpcodeModel opModel;
            public SharkInternalCodeBase(SharkOpcodeType type, SharkOpcodeModel model){
                
                opType = type;
                opModel = model;
            }
            public override string ToString()
            {
                return $"{opType.ToString()}";
            }
        }
        public class SharkInternalCodeNoParam: SharkInternalCodeBase{
            public SharkInternalCodeNoParam(SharkOpcodeType type):base(type, SharkOpcodeModel.MODEL_NO_PARAM){}
            public override string ToString()
            {
                return base.ToString();
            }
        }
        public class SharkInternalCode_1_32BIT: SharkInternalCodeBase{
            private DATA_1_32BIT __data;
            public int IntValue{
                get => __data.value_int;
            }
            public float FloatValue{
                get => __data.value_float;
            }
            public SharkInternalCode_1_32BIT(SharkOpcodeType type, int constID):
            base(type, SharkOpcodeModel.MODEL_PARAM_1_32BIT){
                __data = new DATA_1_32BIT();
                __data.value_int = constID;
            }
            public SharkInternalCode_1_32BIT(SharkOpcodeType type, float value):
            base(type, SharkOpcodeModel.MODEL_PARAM_1_32BIT){
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
                return $"{opType.ToString()} {__data.value_int.ToString()}"; 
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
        }

        public class SharkCodeEmitter{
            public List<SharkInternalCodeBase> buffer;

            public void showBuffer(){
                foreach(SharkInternalCodeBase code in buffer){
                    Console.WriteLine(code);
                }
            }

            public SharkCodeEmitter(){
                buffer = new List<SharkInternalCodeBase>();
            }
            public void Emit(SharkInternalCodeBase code){
                buffer.Add(code);
            }
            public void EmitDone(SharkILStream ilstream){
                ilstream.WriteCodes(buffer);
                buffer.Clear();
            }
            public SharkILStream EmitDone(){
                SharkILStream iLStream = new SharkILStream();
                iLStream.WriteCodes(buffer);
                buffer.Clear();
                return iLStream;
            }
            public void pushConst(int constID){
                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_PUSH_CONST, constID));
            }
            public void pushVar(int varID){
                buffer.Add(new SharkInternalCode_1_32BIT(SharkOpcodeType.OP_PUSH_VAR, varID));
            }
            public void pushFalse(){
                buffer.Add(new SharkInternalCodeNoParam(SharkOpcodeType.OP_PUSH_FALSE));
            }
            public void pushTrue(){
                buffer.Add(new SharkInternalCodeNoParam(SharkOpcodeType.OP_PUSH_TRUE));
            }
            public void pushOperatorNoParam(SharkOpcodeType type){
                buffer.Add(new SharkInternalCodeNoParam(type));
            }
        }
        
        public class SharkRunableHeader{
            // 可执行单元头

            private SharkScript globalRunable;
            private SharkRunable parent;
            public SharkRunableHeader(SharkRunable parent, SharkScript globalRunable){

                this.parent = parent;
                this.globalRunable = globalRunable;
            }
            public string __get_symbol(int id){
                return globalRunable.__get_symbol(id);
            }
            public SkObject  __get_variable(int id){

                if(parent != null){
                    return parent.getVariable(id);
                }
                throw new SharkNameError($"name {__get_symbol(id)} is undefined");
            }
        }

        public class SharkRunable{

            protected SharkILStream codes;
            protected SharkRunableHeader header;
            protected Dictionary<int, SkObject> THIS;
            private int pointer;
            public SharkInternalCodeBase currentCode{
                get => codes.getOpcode(pointer);
            }
            public bool hasProgramDone{
                get => pointer >= codes.Size;
            }
            public void moveToNext(){
                pointer ++;
            }

            public SharkRunable(){
                THIS = new Dictionary<int, SkObject>();
            }
            public void setHeader(SharkRunableHeader header){
                this.header = header;
            }
            public void loadCommands(SharkILStream codes){
                this.codes = codes;
            }
            public SkObject getVariable(int id){

                if(THIS.ContainsKey(id)){
                    return THIS[id];
                }
                return header.__get_variable(id);
            }
            public void setVariable(SkObject variable, int id){

                THIS[id] = variable;
            }
        }
        public class SharkScript : SharkRunable{

            public Dictionary<int, SkObject> cons;
            public Dictionary<int, string> symbols;
            public SharkScript(Dictionary<int, string> symbolTable, Dictionary<int, SkObject> constantTable):base(){

                this.symbols = symbolTable;
                this.cons = constantTable;
            }
            public string __get_symbol(int id){
                return symbols[id];
            }
            public SkObject __get_const(int id){
                return cons[id];
            }
        }

        public delegate void opfunc_no_param();
        public delegate void opfunc_1_32bit(int ID);
        public delegate void opfunc_1_64bit(long ID);

        public class SharkVM{

            public Stack<SkObject> stack;
            public SharkScript currentScript;
            public SharkRunable currentRunable;

            public Dictionary<SharkOpcodeType, opfunc_no_param> op_search_table_no_param;
            public Dictionary<SharkOpcodeType, opfunc_1_32bit> op_search_table_1_32bit;
            public Dictionary<SharkOpcodeType, opfunc_1_64bit> op_search_table_1_64bit;

            public SharkVM(){

                stack = new Stack<SkObject>();
                op_search_table_no_param = new Dictionary<SharkOpcodeType, opfunc_no_param>();
                op_search_table_no_param.Add(SharkOpcodeType.OP_PUSH_FALSE, OP_PUSH_FALSE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_PUSH_TRUE, OP_PUSH_TRUE);
                op_search_table_no_param.Add(SharkOpcodeType.OP_ADD, OP_ADD);
                
                op_search_table_1_32bit = new Dictionary<SharkOpcodeType, opfunc_1_32bit>();
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_PUSH_VAR, OP_PUSH_VAR);
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_LOAD_VAR, OP_LOAD_VAR);
                op_search_table_1_32bit.Add(SharkOpcodeType.OP_PUSH_CONST, OP_PUSH_CONST);
            }

            public void RunScript(SharkScript script){

                currentScript = script;
                currentRunable = script;

                while(!currentRunable.hasProgramDone){
                    SharkInternalCodeBase code = currentRunable.currentCode;
                    switch(code.opModel){
                        case SharkOpcodeModel.MODEL_NO_PARAM:
                        op_search_table_no_param[code.opType]();
                        break;

                        case SharkOpcodeModel.MODEL_PARAM_1_32BIT:
                        int value = ((SharkInternalCode_1_32BIT)code).IntValue;
                        op_search_table_1_32bit[code.opType](value);
                        break;
                    }
                    currentRunable.moveToNext();
                }
            }


            public void OP_PUSH_TRUE(){

                stack.Push(SkBool.SkTRUE);
            }
            public void OP_PUSH_FALSE(){

                stack.Push(SkBool.SkFALSE);
            }
            public void OP_PUSH_CONST(int id){

                stack.Push(currentScript.cons[id]);
            }
            public void OP_PUSH_VAR(int id){
                
                stack.Push(currentRunable.getVariable(id));
            }
            public void OP_LOAD_VAR(int id){

                currentRunable.setVariable(stack.Pop(), id);
            }
            public void OP_ADD(){

                SkObject tmp = stack.Pop();
                stack.Push(SharkAPI.Add(stack.Pop(), tmp));
            }
        }





        /// <summary>
        /// Shark脚本的模块系统
        /// </summary>
        public class SharkModule{


        }
    }

    namespace SharkCore{


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

            OT_NULL,
            OT_FUNCTION,
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

            public static SkObject Add(SkObject left, SkObject right){

                if(left.BaseType == right.BaseType){
                    if(left.BaseType == SkObjectType.OT_VALUE){
                        SkValue value1 = (SkValue)left;
                        SkValue value2 = (SkValue)right;

                        if(value1.ValueType == value2.ValueType && value1.ValueType == SkValueType.VT_INT){
                            return (SkInt)value1 + (SkInt)value2;
                        }
                    }
                }
                throw new SharkError("不支持的加法操作");
            }
        }


        /// <summary>
        /// Shark数据基类
        /// </summary>
        public class SkObject{

            public static SkObject SkNull = new SkObject(SkObjectType.OT_NULL);

            protected SkObjectType baseType;
            public SkObjectType BaseType{
                get => baseType;
            }
            public SkObject(){
                baseType = SkObjectType.OT_NULL;
            }
            public SkObject(SkObjectType type){
                baseType = type;
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

        public class SkValue: SkObject{

            protected SkValueType valueType;
            public SkValueType ValueType{
                get => valueType;
            }
            public SkValue(SkValueType type):base(SkObjectType.OT_VALUE){
                valueType = type;
            }
        }

        public class SkInt: SkValue{
            public int data;
            public SkInt():base(SkValueType.VT_INT){
                data = 0;
            }
            public SkInt(int value):base(SkValueType.VT_INT){
                data = value;
            }
            public override string ToString()
            {
                return data.ToString();
            }
            public static SkInt operator+(SkInt left, SkInt right){
                return new SkInt(left.data + right.data);
            }
            public static SkInt operator-(SkInt left, SkInt right){
                return new SkInt(left.data - right.data);
            }
            public static SkInt operator*(SkInt left, SkInt right){
                return new SkInt(left.data * right.data);
            }
            public static SkInt operator/(SkInt left, SkInt right){
                return new SkInt(left.data / right.data);
            }
            public static SkInt operator%(SkInt left, SkInt right){
                return new SkInt(left.data % right.data);
            }
        }

        public class SkFloat: SkValue{

            public float value;
            public SkFloat():base(SkValueType.VT_FLOAT){
                value = 0;
            }
            public SkFloat(float value):base(SkValueType.VT_FLOAT){
                this.value = value;
            }
            public override string ToString()
            {
                return value.ToString();
            }
        }

        public class SkBool: SkValue{

            public static SkBool SkTRUE = new SkBool(true);
            public static SkBool SkFALSE = new SkBool(false);

            public readonly bool value;
            public SkBool():base(SkValueType.VT_BOOLEAN){
                value = false;
            }
            public SkBool(bool value):base(SkValueType.VT_BOOLEAN){
                this.value = value;
            }
        }

        public class SkTuple: SkObject{

            public SkObject[] tuple;
            public SkTuple(int length):base(SkObjectType.OT_TUPLE){
                tuple = new SkObject[length];
            }
            public SkObject Index(int index){
                return tuple[index];
            }
        }
    }
}