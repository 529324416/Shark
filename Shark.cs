using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Shark.SharkLexer;

namespace Shark{

    public class SharkError: Exception{
        public SharkError(string errInfo):base(errInfo){}
    }
    public class SharkSyntaxError: SharkError{
        public SharkSyntaxError(string errInfo):base(errInfo){}
        public override string Message => $"SharkSyntaxError: {base.Message}";
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

            public static SkToken[] systemKeywords = new SkToken[]{
                new SkToken(SkTokenType.TOKEN_FUNC, "function"),
                new SkToken(SkTokenType.TOKEN_IF, "if"),
                new SkToken(SkTokenType.TOKEN_ELSE, "else"),
                new SkToken(SkTokenType.TOKEN_WHILE, "while"),
                new SkToken(SkTokenType.TOKEN_FOR, "for"),
                new SkToken(SkTokenType.TOKEN_BREAK, "break"),
                new SkToken(SkTokenType.TOKEN_CONTINUE, "continue"),
                new SkToken(SkTokenType.TOKEN_RETURN, "return"),
                new SkToken(SkTokenType.TOKEN_NULL, "null"),
                new SkToken(SkTokenType.TOKEN_THIS, "this"),
                new SkToken(SkTokenType.TOKEN_IMPORT, "import"),
                new SkToken(SkTokenType.TOKEN_GLOBAL, "global"),
                new SkToken(SkTokenType.TOKEN_UNKNOWN, null)
                
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
            public static SkToken isIdOrKeyword(string content, int lineNo){
                foreach(SkToken keyword in systemKeywords){
                    if(content == keyword.tokenContent){
                        return keyword;
                    }
                }
                return new SkToken(SkTokenType.TOKEN_ID, content, lineNo);
            }
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
            

            public SkLexer(){}
            public SkLexer(string sourceCode){
                LoadSourceCode(sourceCode);
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
            public Queue<SkToken> Parse(string source){

                LoadSourceCode(source);
                Queue<SkToken> output = new Queue<SkToken>();
                SkToken buf;
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
            public Queue<SkToken> Parse(){

                currentPos = 0;
                currentLine = 1;
                Queue<SkToken> output = new Queue<SkToken>();
                SkToken buf;
                while(true){
                    buf = getNextToken();
                    output.Enqueue(buf);
                    if(buf.tokenType == SkTokenType.TOKEN_EOF){
                        break;
                    }
                }
                return output;
            }

            private SkToken makeToken(SkTokenType type){
                return new SkToken(type, type.ToString(), currentLine);
            }
            private SkToken makeToken(SkTokenType type, string content){
                return new SkToken(type, content, currentLine);
            }
            ///<summary>
            ///从源代码的指定位置获取一个子字符串
            ///</summary>
            private string takeString(int from, int to, bool cast = false){
                string _Ret = sourceCode.Substring(from, to - from);
                if(cast)return Regex.Unescape(_Ret);
                return _Ret;
            }

            public SkToken getNextToken(){
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
                    return makeToken(SkTokenType.TOKEN_STRING, parseString());

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
            private SkToken parseNumber(){

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
                return makeToken(SkTokenType.TOKEN_INT, takeString(positionStart, currentPos));
            }

            ///<summary>
            /// 解析小数部分
            ///</summary>
            private SkToken numberParseFloat(int positionStart){

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
                return makeToken(SkTokenType.TOKEN_FLOAT, takeString(positionStart, currentPos));
            }

            ///<summary>
            /// 解析数字后的科学计数部分
            ///</summary>
            private SkToken numberParserE(int positionStart){

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
                    return makeToken(SkTokenType.TOKEN_FLOAT, takeString(positionStart, currentPos));
                }
                throw new SharkSyntaxError($"expect number after \"{takeString(positionStart, currentPos - 1)}\"");
            }

            ///<summary>
            /// 解析标识符,包括ID和系统关键字
            ///</summary>
            private SkToken parseID(){

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
                return SharkLexerCore.isIdOrKeyword(cnt, currentLine);
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
                    // Console.WriteLine(token.tokenType.ToString());
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
    }

    namespace SharkVirtualMachine{

        public enum SharkOpcodeType{

            PUSH_CONSTANT,
            PUSH_VAR,
            PUSH_TRUE,
            PUSH_FALSE,
            PUSH_NULL,

            STORE_VAR,
        }

        public class SharkVM{


        }

        public class SharkILStream{


        }



        /// <summary>
        /// Shark脚本的模块系统
        /// </summary>
        public class SharkModule{


        }
    }

    namespace SharkCore{


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

            public int value;
            public SkInt():base(SkValueType.VT_INT){
                value = 0;
            }
            public SkInt(int value):base(SkValueType.VT_INT){
                this.value = value;
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
        }

        public class SkBool: SkValue{

            public bool value;
            public SkBool():base(SkValueType.VT_BOOLEAN){
                value = false;
            }
            public SkBool(bool value):base(SkValueType.VT_BOOLEAN){
                this.value = value;
            }
        }
    }
}