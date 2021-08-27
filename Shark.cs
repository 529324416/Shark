using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
    }


    namespace SharkLexer{


        public enum SkTokenType{

            TOKEN_UNKNOWN,
            
            // data types
            TOKEN_ID,
            TOKEN_NUM,
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

            public SkToken(SkTokenType type, string content, int lineNo){

                tokenType = type;
                tokenContent = content;
                line = lineNo;
            }
            public override string ToString()
            {
                return $"{tokenContent}({tokenType.ToString()}) at line {line.ToString()}";
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
                new SkToken(SkTokenType.TOKEN_FUNC, "function", 0),
                new SkToken(SkTokenType.TOKEN_IF, "if", 0),
                new SkToken(SkTokenType.TOKEN_ELSE, "else", 0),
                new SkToken(SkTokenType.TOKEN_WHILE, "while", 0),
                new SkToken(SkTokenType.TOKEN_FOR, "for", 0),
                new SkToken(SkTokenType.TOKEN_BREAK, "break", 0),
                new SkToken(SkTokenType.TOKEN_CONTINUE, "continue", 0),
                new SkToken(SkTokenType.TOKEN_RETURN, "return", 0),
                new SkToken(SkTokenType.TOKEN_NULL, "null", 0),
                new SkToken(SkTokenType.TOKEN_THIS, "this", 0),
                new SkToken(SkTokenType.TOKEN_IMPORT, "import", 0),
                new SkToken(SkTokenType.TOKEN_UNKNOWN, null, 0)
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
            public static SkTokenType isIdOrKeyword(string content){

                foreach(SkToken keyword in systemKeywords){
                    if(content == keyword.tokenContent){
                        return keyword.tokenType;
                    }
                }
                return SkTokenType.TOKEN_ID;
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

            private SkToken makeToken(SkTokenType type){
                return new SkToken(type, null, currentLine);
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
                        return makeToken(SkTokenType.TOKEN_NUM, parseSpecNumber(SharkLexerCore.isHexChar, "invalid hex number"));
                    }else if(matchChar('o')){
                        return makeToken(SkTokenType.TOKEN_NUM, parseSpecNumber(SharkLexerCore.isOctChar, "invalid oct number"));
                    }else if(matchChar('b')){
                        return makeToken(SkTokenType.TOKEN_NUM, parseSpecNumber(SharkLexerCore.isBinChar, "invalid bin number"));
                    }else{
                        return makeToken(SkTokenType.TOKEN_NUM, C.ToString());
                    }

                    default:
                    /*
                        1.解析标识符(ID和系统关键字)
                        2.解析数字
                    */
                    if(char.IsLetter(C) || C == '_'){
                        return parseID();
                    }else if(char.IsDigit(C)){
                        return makeToken(SkTokenType.TOKEN_NUM, parseNumber());
                    }
                    

                    return makeToken(SkTokenType.TOKEN_UNKNOWN);
                }
                // Console.WriteLine($"default区块的未知字符:{(byte)C.ToCharArray()[0]}");
                // return makeToken(SkTokenType.TOKEN_UNKNOWN);
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
            private string parseNumber(){

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
                return takeString(positionStart, currentPos);
            }

            ///<summary>
            /// 解析小数部分
            ///</summary>
            private string numberParseFloat(int positionStart){

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
                return takeString(positionStart, currentPos);
            }

            ///<summary>
            /// 解析数字后的科学计数部分
            ///</summary>
            private string numberParserE(int positionStart){

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
                    return takeString(positionStart, currentPos);
                }
                throw new SharkSyntaxError($"expect number after \"{takeString(positionStart, currentPos - 1)}\"");
            }

            ///<summary>
            /// 解析标识符,包括ID和系统关键字
            ///</summary>
            private SkToken parseID(){

                //当已经发现了是普通的字符时, 第一个字符也需要被包括进来.
                int positionStart = currentPos - 1;
                int offset = 0;

                char buf;
                while(hasMore){
                    buf = nextChar;
                    if(char.IsLetterOrDigit(buf) || buf == '_'){
                        continue;
                    }else{
                        offset = -1;
                        break;
                    }
                }
                string cnt = takeString(positionStart, currentPos + offset);
                return makeToken(SharkLexerCore.isIdOrKeyword(cnt), cnt);
            }

            ///<summary>
            /// 解析普通字符串(包含转义字符内容, 当遇到嵌入表达式时, 转换解析模式)
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
                            throw new SharkSyntaxError($"unsupported escape \"\\{currentChar}\"");
                        }
                        continue;
                    }
                }
                throw new SharkSyntaxError($"expect '\"' at line {currentLine.ToString()}");
            }
        }
    }
}  