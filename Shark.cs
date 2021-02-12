using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Shark.SharkExceptions;
using Shark.SharkCodeLexer;
using Shark.SharkIL;
using Shark.SharkVirtualMachine;

namespace Shark
{
    /* all code about shark */

    public class SkTuple<T1, T2>
    {

        public T1 key;
        public T2 value;
        public SkTuple(T1 k, T2 v)
        {

            key = k;
            value = v;
        }
    }

    public class SharkUtil
    {
        /* provide static function tool only this function is reusable*/

        public static bool Contains<T>(T[] array, T value) where T : class
        {
            /* check that if value is contained in given array */

            foreach (T v in array)
            {
                if (value.Equals(v))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool ContainsVal<T>(T[] array, T value) where T : struct
        {

            foreach (T v in array)
            {
                if (value.Equals(v))
                {
                    return true;
                }
            }
            return false;
        }
        public static void ShowQueue<T>(Queue<T> queue) where T : struct
        {

            while (queue.Count > 0)
            {
                Console.WriteLine(queue.Dequeue().ToString());
            }
        }

        public static string ReadSource(string filepath)
        {
            /* read shark source code and remove comment 
            读取Shark原始脚本并移除所有的注释*/

            List<string> O = new List<string>();
            FileStream file = new FileStream(filepath, FileMode.Open);
            StreamReader reader = new StreamReader(file);

            string buf;
            while ((buf = reader.ReadLine()) != null)
            {
                O.Add(buf);
            }
            return RemoveComment(O);
        }
        public static string RemoveComment(List<string> lines)
        {
            /* 移除所有的注释并将代码拼成一个完整的字符串 */

            string O = string.Empty;
            foreach (string line in lines)
            {
                O += RemoveSingleComment(line);
            }
            return O;
        }
        public static string RemoveSingleComment(string line)
        {

            bool isInString = false;
            Queue<char> characters = new Queue<char>();
            string buf = string.Empty;
            foreach (char c in line) { characters.Enqueue(c); }
            while (characters.Count > 0)
            {
                char nextChar = characters.Peek();
                if (isInString)
                {
                    if (nextChar == '\\')
                    {
                        buf += characters.Dequeue().ToString() + characters.Dequeue().ToString();
                    }
                    else if (nextChar == '"')
                    {
                        isInString = false;
                        buf += characters.Dequeue().ToString();
                    }
                    else
                    {
                        buf += characters.Dequeue().ToString();
                    }

                }
                else
                {
                    //还不在一个字符串内，此时主要检测是否存在注释符号.
                    if (nextChar == '/')
                    {
                        characters.Dequeue();
                        if (characters.Peek() == '/')
                        {
                            // is comment after this
                            break;
                        }
                        else
                        {
                            buf += nextChar.ToString();
                        }
                    }
                    else if (nextChar == '"')
                    {
                        isInString = true;
                        buf += characters.Dequeue().ToString();
                    }
                    else
                    {
                        buf += characters.Dequeue().ToString();
                    }
                }
            }
            return buf;
        }
    }

    namespace SharkExceptions
    {
        /* the exception of shark */

        public class SharkException : Exception
        {
            public SharkException() : base("shark exception") { }
            public SharkException(string message) : base(message) { }
            public SharkException(string message, Exception inner) : base(message, inner) { }
        }
        public class SharkTokenException : SharkException
        {

            public static SharkTokenException defaultErr = new SharkTokenException();
            public SharkTokenException() : base("shark token exception") { }
            public SharkTokenException(string message) : base(message) { }
            public SharkTokenException(string message, Exception inner) : base(message, inner) { }
        }
        public class SharkCodeFormatException : SharkException
        {

            public static SharkCodeFormatException defaultErr = new SharkCodeFormatException();
            public SharkCodeFormatException() : base("shark code format exception") { }
            public SharkCodeFormatException(string message) : base(message) { }
            public SharkCodeFormatException(string message, Exception inner) : base(message, inner) { }
        }
        public class SharkTypeError : SharkException
        {

            public static SharkTypeError defaultErr = new SharkTypeError();
            public SharkTypeError() : base("unsupport type in shark") { }
            public SharkTypeError(string message) : base(message) { }
            public SharkTypeError(string message, Exception inner) : base(message, inner) { }
        }
        public class SharkIndexError : SharkException
        {

            public static SharkIndexError defaultErr = new SharkIndexError();
            public SharkIndexError() : base("index out of range") { }
            public SharkIndexError(string message) : base(message) { }
            public SharkIndexError(string message, Exception inner) : base(message, inner) { }
        }
        public class SharkNameError : SharkException
        {

            public static SharkNameError defaultErr = new SharkNameError();
            public SharkNameError() : base("shark name error") { }
            public SharkNameError(string message) : base(message) { }
            public SharkNameError(string message, Exception inner) : base(message, inner) { }
        }
    }

    namespace SharkCodeLexer
    {
        /* the code about code analysis lexer */

        public class TokenSet
        {

            public const char global = '@';
            public const string funcDefination = "function";
            public const char opAssign = '=';
            public const char opAdd = '+';
            public const char opSub = '-';
            public const char opMul = '*';
            public const char opPow = '^';
            public const char opMod = '%';
            public const char opDiv = '/';
            public const char opLt = '<';
            public const char opGt = '>';
            public const string opAssignAdd = "+=";
            public const string opAssignSub = "-=";
            public const string opAssignMul = "*=";
            public const string opAssignDiv = "/=";
            public const string opAssignMod = "%=";
            public const string opLe = "<=";
            public const string opGe = ">=";
            public const string opEq = "==";
            public const string opNe = "!=";
            public const string opAnd = "&&";
            public const string opOr = "||";
            public const char opNot = '!';
            public const string sysReturn = "return";
            public const string sysIf = "if";
            public const string sysWhile = "while";
            public const string sysElse = "else";
            public const string sysElif = "elif";
            public const string sysTrue = "true";
            public const string sysFalse = "false";
            public const string sysNull = "null";
            public const string sysBreak = "break";
            public const char listSeparation = ',';
            public const char codeSeparation = ';';
            public const char varString = '"';
            public const char listLeft = '[';
            public const char listRight = ']';
            public const char blockLeft = '{';
            public const char blockRight = '}';
            public const char paranthesesLeft = '(';
            public const char paranthesesRight = ')';
            public const char underLine = '_';
            public const char point = '.';
            public const char space = ' ';
            public const char cast = '\\';
            public const char EOL = '\n';
            public const char castR = '\r';

            public static string[] sysWords = new string[]{

                TokenSet.funcDefination,
                TokenSet.sysReturn,
                TokenSet.sysWhile,
                TokenSet.sysIf,
                TokenSet.sysElse,
                TokenSet.sysElif,
                TokenSet.sysNull,
                TokenSet.sysBreak,
            };
            public static string[] opAssigns = new string[]{

                opAssign.ToString(),
                opAssignAdd,
                opAssignSub,
                opAssignMul,
                opAssignDiv,
                opAssignMod
            };
            public static string[] opDouble = new string[]{
                opNe,
                opEq,
                opAnd,
                opOr
            };
            public static char[] separations = new char[]{
                listSeparation,
                codeSeparation,
                listLeft,
                listRight,
                blockLeft,
                blockRight,
                paranthesesLeft,
                paranthesesRight
            };
            public static char[] opComputeAssign = new char[]{

                opAdd,
                opSub,
                opMul,
                opDiv,
                opMod
            };
            public static TokenType[] literal = new TokenType[]{

                TokenType.BOOLEAN,
                TokenType.FLOAT,
                TokenType.INT,
                TokenType.STRING
            };
            public static string[] opCompare = new string[]{
                /* compare operator */

                opEq,
                opNe,
                opGe,
                opLe,
                opGt.ToString(),
                opLt.ToString()
            };
            public static string[] opLogic = new string[]{
                /* logical operator */

                opNot.ToString(),
                opAnd,
                opOr,
            };
            public static bool isSysWords(string tmp)
            {
                /* check that if target */

                return SharkUtil.Contains<string>(sysWords, tmp);
            }
        }

        public enum TokenType
        {
            /* type of token, and */

            BOOLEAN,
            INT,
            FLOAT,
            STRING,
            SYSKEYWORD,
            SEPARATION,
            OPERATOR,
            ID,
            VIRTUAL
        }

        public struct Token
        {
            /* contained the type of token, and a string type value to marked 
            the token content */

            public TokenType type;
            public string value;

            public Token(TokenType type, string value)
            {
                this.type = type;
                this.value = value;
            }
            public override string ToString()
            {
                return $"[{this.type} : {this.value}]";
            }
            public bool IsID
            {

                get { return type == TokenType.ID; }
            }
            public bool IsLiteral
            {
                /* check if token is a literal */

                get { return (byte)type <= 3; }
            }
        }

        public class CodeScanner
        {
            /* used to scan the code and get a the array of code */

            private Queue<char> characters;
            public CodeScanner()
            {
                characters = new Queue<char>();
            }
            public void LoadSource(string script)
            {
                /* load source code into characters */

                foreach (char c in script)
                {
                    characters.Enqueue(c);
                }
            }
            public Token NextID()
            {
                /* find next id value, note that, the id can be a system keyword
                when we get a underline or a letter, then we can use this function to get the 
                complete id name */

                return __NextID(characters.Dequeue().ToString());
            }
            private Token __NextID(string tmp)
            {

                char c = characters.Peek();
                if (char.IsLetterOrDigit(c) || c == TokenSet.underLine)
                {
                    return __NextID(tmp + characters.Dequeue().ToString());
                }
                if (TokenSet.isSysWords(tmp))
                {
                    return new Token(TokenType.SYSKEYWORD, tmp);
                }
                else if (tmp == TokenSet.sysTrue || tmp == TokenSet.sysFalse)
                {
                    return new Token(TokenType.BOOLEAN, tmp);
                }
                else
                {
                    return new Token(TokenType.ID, tmp);
                }

            }

            public Token NextString()
            {
                /* when a " find in last peek, then this function will find next complete 
                string type value */

                return __NextString(characters.Dequeue().ToString());
            }
            public Token __NextString(string tmp)
            {

                char c = characters.Peek(); // next string
                if (c == TokenSet.cast)
                {
                    /* the characters has been cast now */

                    return __NextString(tmp + characters.Dequeue().ToString() + characters.Dequeue().ToString());
                }
                else
                {
                    if (c == TokenSet.varString)
                    {
                        return new Token(TokenType.STRING, tmp + characters.Dequeue().ToString());
                    }
                    return __NextString(tmp + characters.Dequeue().ToString());
                }
            }
            public Token NextNumber()
            {
                /* when you got a digital value, then this function can help 
                to find the complete number value, when a . has been found, then
                NextFloat will take the place of NextInt*/

                return __NextNumber(characters.Dequeue().ToString());
            }
            public Token __NextNumber(string tmp)
            {

                char c = characters.Peek();
                if (char.IsDigit(c))
                {
                    return __NextNumber(tmp + characters.Dequeue().ToString());
                }
                else if (c == TokenSet.point)
                {
                    return __NextFloat(tmp + characters.Dequeue().ToString());
                }
                else
                {
                    return new Token(TokenType.INT, tmp);
                }
            }
            public Token __NextFloat(string tmp)
            {

                char c = characters.Peek();
                if (char.IsDigit(c))
                {
                    return __NextFloat(tmp + characters.Dequeue().ToString());
                }
                return new Token(TokenType.FLOAT, tmp);
            }
            public Token NextLogic()
            {

                return __NextLogic(characters.Dequeue().ToString());
            }
            public Token __NextLogic(string tmp)
            {
                /* */

                char c = characters.Peek();
                if (c.ToString() == tmp)
                {
                    return new Token(TokenType.OPERATOR, tmp + characters.Dequeue().ToString());
                }
                throw new SharkException("invalid logical token");
            }
            public Token NextEqual(string tmp)
            {

                char c = characters.Peek();
                if (c == TokenSet.opAssign)
                {
                    return new Token(TokenType.OPERATOR, tmp + characters.Dequeue().ToString());
                }
                return new Token(TokenType.OPERATOR, tmp);
            }
            public Token NextCompare(string tmp)
            {

                char c = characters.Peek();
                if (c == TokenSet.opAssign)
                {
                    return new Token(TokenType.OPERATOR, tmp + characters.Dequeue().ToString());
                }
                return new Token(TokenType.OPERATOR, tmp);
            }
            public Token NextComputeAssign(string tmp)
            {

                char c = characters.Peek();
                if (c == TokenSet.opAssign)
                {
                    return new Token(TokenType.OPERATOR, tmp + characters.Dequeue().ToString());
                }
                return new Token(TokenType.OPERATOR, tmp);
            }
            public Queue<Token> Scan()
            {
                /* scan all characters to get a token list */

                Queue<Token> O = new Queue<Token>();
                while (characters.Count > 0)
                {
                    char c = characters.Peek();
                    if (c == TokenSet.varString)
                    {

                        O.Enqueue(NextString());
                    }
                    else if (char.IsDigit(c))
                    {

                        O.Enqueue(NextNumber());
                    }
                    else if (char.IsLetter(c) || c == TokenSet.underLine)
                    {

                        O.Enqueue(NextID());
                    }
                    else if (c == TokenSet.opNot || c == TokenSet.opPow)
                    {

                        O.Enqueue(new Token(TokenType.OPERATOR, characters.Dequeue().ToString()));
                    }
                    else if (c == '&' || c == '|')
                    {

                        O.Enqueue(NextLogic());
                    }
                    else if (c == '>' || c == '<')
                    {

                        O.Enqueue(NextCompare(characters.Dequeue().ToString()));
                    }
                    else if (SharkUtil.ContainsVal<char>(TokenSet.opComputeAssign, c))
                    {

                        O.Enqueue(NextComputeAssign(characters.Dequeue().ToString()));
                    }
                    else if (c == TokenSet.opAssign)
                    {

                        O.Enqueue(NextEqual(characters.Dequeue().ToString()));
                    }
                    else if (SharkUtil.Contains<string>(TokenSet.opDouble, c.ToString()))
                    {

                        O.Enqueue(new Token(TokenType.OPERATOR, characters.Dequeue().ToString()));
                    }
                    else if (SharkUtil.ContainsVal<char>(TokenSet.separations, c))
                    {

                        O.Enqueue(new Token(TokenType.SEPARATION, characters.Dequeue().ToString()));
                    }
                    else if (c == TokenSet.global)
                    {

                        O.Enqueue(new Token(TokenType.SYSKEYWORD, characters.Dequeue().ToString()));
                    }
                    else if (c == TokenSet.space || c == TokenSet.EOL || c == TokenSet.castR)
                    {

                        characters.Dequeue();
                    }
                    else
                    {
                        throw new SharkTokenException($"unknown token sign {c.ToString()}");
                    }
                }
                return O;
            }
        }
    }

    namespace SharkIL
    {
        /* about the il code generator */

        public enum NodeType
        {
            /* the type of node */

            ASSIGN,
            OP,
            VAR,
            LITERAL,
            FUNC,
            LIST,
            IF,
            SUBIF,
            ELSE,
            LOOP,
            DEF,
            BLOCK,
            ARGS,
            INDEX,
            RETURN,
            BREAK,
            NONE,
            GLOBAL
        }

        public class NodeCnt
        {
            /* NodeCnt contains a value type and a object type data
            it can represent an operator ,a function call ,a literal 
            or a variable. */

            public static NodeCnt AssignNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.ASSIGN.ToString()), NodeType.ASSIGN);
            public static NodeCnt ArgsNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.ARGS.ToString()), NodeType.ARGS);
            public static NodeCnt ListNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.LIST.ToString()), NodeType.LIST);
            public static NodeCnt FuncNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.FUNC.ToString()), NodeType.FUNC);
            public static NodeCnt IfNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.IF.ToString()), NodeType.IF);
            public static NodeCnt SubIfNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.SUBIF.ToString()), NodeType.SUBIF);
            public static NodeCnt ElseNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.ELSE.ToString()), NodeType.ELSE);
            public static NodeCnt WhileNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.LOOP.ToString()), NodeType.LOOP);
            public static NodeCnt DefNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.DEF.ToString()), NodeType.DEF);
            public static NodeCnt BlockNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.BLOCK.ToString()), NodeType.BLOCK);
            public static NodeCnt IndexNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.INDEX.ToString()), NodeType.INDEX);
            public static NodeCnt ReturnNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.RETURN.ToString()), NodeType.RETURN);
            public static NodeCnt BreakNode = new NodeCnt(new Token(TokenType.VIRTUAL, NodeType.BREAK.ToString()), NodeType.BREAK);
            public static NodeCnt GlobalNode = new NodeCnt(new Token(TokenType.SYSKEYWORD, NodeType.GLOBAL.ToString()), NodeType.GLOBAL);

            public NodeType ndtype;
            public Token token;

            public NodeCnt(Token token, NodeType ndtype)
            {
                /* the node content */

                this.token = token;
                this.ndtype = ndtype;
            }
            public override string ToString()
            {
                return $"[{ndtype}:{token}]";
            }
        }

        public class SharkASTree
        {
            /* represent the abstract structure of procedure */

            public SharkASTree[] nodes;
            /* some code has more than one node, so here is a array type value
            to record them, but usually we got two node in ASTree Node */

            public NodeCnt content;
            /* save code source used in this node */

            public SharkASTree(Token token)
            {
                /* ASTree must contained single value at last 
                @token: the function token marked by */

                if (token.type == TokenType.OPERATOR)
                {

                    if (token.value == TokenSet.opAssign.ToString())
                    {
                        content = NodeCnt.AssignNode;
                    }
                    else
                    {
                        content = new NodeCnt(token, NodeType.OP);
                    };
                }
                else if (SharkUtil.ContainsVal<TokenType>(TokenSet.literal, token.type))
                {

                    content = new NodeCnt(token, NodeType.LITERAL);
                }
                else if (token.type == TokenType.ID)
                {

                    content = new NodeCnt(token, NodeType.VAR);
                }
                else
                {

                    throw new SharkException("该token无法单独作为一个节点创建");
                }
                nodes = new SharkASTree[2];
            }
            public SharkASTree()
            {
                /* create empty list */

                content = null;
                nodes = null;
            }
            public NodeType NdType
            {

                get { return content.ndtype; }
            }
            public bool IsOperator
            {
                /*is op node */

                get { return content.ndtype == NodeType.OP; }
            }
            public bool IsVarOrLiteral
            {
                /* literal and anything with ID could be treat as Val*/

                get { return content.ndtype == NodeType.VAR || content.ndtype == NodeType.LITERAL; }
            }
            public bool IsVarOrLiteralOrNone
            {

                get { return content.ndtype == NodeType.VAR || content.ndtype == NodeType.LITERAL || content.ndtype == NodeType.NONE; }
            }
            public bool IsLiteral
            {
                /* check if node is a constant value */

                get { return content.ndtype == NodeType.LITERAL; }
            }
            public bool IsVar
            {
                /* is an id type node */

                get { return content.ndtype == NodeType.VAR; }
            }
            public int BlockSize
            {
                /* get block size */

                get { return nodes.Length; }
            }
            public void AddNode(SharkASTree node)
            {
                /* add a new node to this list */

                SharkASTree[] newArr = new SharkASTree[nodes.Length + 1];
                nodes.CopyTo(newArr, 0);
                newArr[nodes.Length] = node;
                nodes = newArr;
            }
            public void DownExtend(Token value)
            {
                /* extend left node
                LEFT !!!*/

                nodes[0] = new SharkASTree(value);
            }
            public SharkASTree Left
            {
                /* get left value */

                get { return nodes[0]; }
                set { nodes[0] = value; }
            }
            public string Value
            {
                get { return content.token.value; }
            }
            public string LeftValue
            {
                get { return Left.content.token.value; }
            }
            public string RightValue
            {
                get { return Right.content.token.value; }
            }
            public SharkASTree Right
            {
                /* get right child node */

                get { return nodes[1]; }
                set { nodes[1] = value; }
            }
            public override string ToString()
            {
                return content.ToString();
            }
            public static SharkASTree UpExtend(SharkASTree tree, Token value)
            {
                /* extend from this node */

                SharkASTree UpNode = new SharkASTree(value);
                UpNode.Left = tree;
                return UpNode;
            }
            public static SharkASTree UpExtendAtRight(SharkASTree tree, Token value)
            {

                SharkASTree UpNode = new SharkASTree(value);
                UpNode.Right = tree;
                return UpNode;
            }
            public static SharkASTree CreateGlobalNode()
            {
                /* 创建一个全局操作符节点 */

                SharkASTree O = new SharkASTree();
                O.nodes = new SharkASTree[1];
                O.content = NodeCnt.GlobalNode;
                return O;
            }
            public static SharkASTree CreateEmptyList()
            {
                /* create a empty list */

                SharkASTree O = new SharkASTree();
                O.nodes = new SharkASTree[0];
                O.content = NodeCnt.ListNode;
                return O;
            }
            public static SharkASTree CreateArgsList()
            {

                SharkASTree O = new SharkASTree();
                O.nodes = new SharkASTree[0];
                O.content = NodeCnt.ArgsNode;
                return O;
            }
            public static SharkASTree CreateFunctionCall(Token IdToken)
            {
                /* create a function call node */

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.FuncNode;
                O.nodes = new SharkASTree[2];
                O.Left = new SharkASTree(IdToken);
                return O;
            }
            public static SharkASTree CreateFunctionCall(SharkASTree v)
            {

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.FuncNode;
                O.nodes = new SharkASTree[2];
                O.Left = v;
                return O;
            }
            public static SharkASTree CreateFunctionDefination(string ID)
            {
                /* create a function defination */

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.DefNode;
                O.nodes = new SharkASTree[3];
                O.nodes[0] = new SharkASTree(new Token(TokenType.ID, ID));
                O.nodes[1] = SharkASTree.CreateEmptyList();
                O.nodes[2] = SharkASTree.CreateEmptyList();
                return O;
            }
            public static SharkASTree CreateCodeBlock()
            {
                /* create en empty code block */

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.BlockNode;
                O.nodes = new SharkASTree[0];
                return O;
            }
            public static SharkASTree CreateIFBlock()
            {
                /* create en if code block */

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.IfNode;
                O.nodes = new SharkASTree[1];
                return O;
            }
            public static SharkASTree CreateSubIfBlock()
            {
                /* create an sub if block */

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.SubIfNode;
                O.nodes = new SharkASTree[2];
                return O;
            }
            public static SharkASTree CreateWhileBlock()
            {
                /* create an while block */

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.WhileNode;
                O.nodes = new SharkASTree[2];
                return O;
            }
            public static SharkASTree CreateElseBlock()
            {

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.ElseNode;
                O.nodes = new SharkASTree[1];
                return O;
            }
            public static SharkASTree CreateIndex(SharkASTree list)
            {

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.IndexNode;
                O.nodes = new SharkASTree[2];
                O.Left = list;
                return O;
            }
            public static SharkASTree CreateIndex(Token tk)
            {

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.IndexNode;
                O.nodes = new SharkASTree[2];
                O.Left = new SharkASTree(tk);
                return O;
            }
            public static SharkASTree CreateReturnNode()
            {

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.ReturnNode;
                O.nodes = new SharkASTree[1];
                return O;
            }
            public static SharkASTree CreateBreakNode()
            {

                SharkASTree O = new SharkASTree();
                O.content = NodeCnt.BreakNode;
                O.nodes = new SharkASTree[0];
                return O;
            }
            public static SharkASTree CreateNullNode(Token token)
            {

                SharkASTree O = new SharkASTree();
                O.content = new NodeCnt(token, NodeType.NONE);
                O.nodes = new SharkASTree[0];
                return O;
            }
        }

        public class CodeParser
        {
            /* parse code and generate ast tree */

            public Queue<Token> tokens;
            public CodeParser()
            {

                tokens = null;
            }
            public void LoadTokens(Queue<Token> tokens)
            {

                this.tokens = tokens;
            }
            public Token GetNextToken(bool isPop)
            {

                if (isPop)
                {
                    return tokens.Dequeue();
                }
                return tokens.Peek();
            }
            public SharkASTree NextArgs()
            {
                /* find next args list */

                SharkASTree O = SharkASTree.CreateArgsList();
                if (GetNextToken(false).value == TokenSet.paranthesesRight.ToString())
                {
                    GetNextToken(true);
                    return O;
                }
                return __NextArgs(O);
            }
            public SharkASTree __NextArgs(SharkASTree O)
            {
                /* find next args list */

                O.AddNode(NextLogicalExpression());
                Token t = GetNextToken(true);
                if (t.value == TokenSet.paranthesesRight.ToString())
                {
                    return O;
                }
                else if (t.value == TokenSet.listSeparation.ToString())
                {
                    return __NextArgs(O);
                }
                else
                {
                    throw SharkCodeFormatException.defaultErr;
                }
            }
            public SharkASTree NextList(string ending)
            {
                /* find next list type value, when we get a [ 
                    @ending: ) or ]*/

                SharkASTree O;
                if (ending == TokenSet.listRight.ToString())
                {
                    O = SharkASTree.CreateEmptyList();
                }
                else
                {
                    O = SharkASTree.CreateArgsList();
                }
                if (GetNextToken(false).value == ending)
                {
                    GetNextToken(true);
                    Token t = GetNextToken(false);
                    if (t.value == TokenSet.listLeft.ToString())
                    {
                        return NextIndex(O);
                    }
                    else if (t.value == TokenSet.paranthesesLeft.ToString())
                    {
                        return NextCall(O);
                    }
                    return O;
                }
                else
                {
                    return __NextList(O, ending);
                }
            }
            public SharkASTree __NextList(SharkASTree O, string ending)
            {
                /* recurrent to call this function and get all element */

                O.AddNode(NextLogicalExpression());
                if (GetNextToken(false).value == TokenSet.listSeparation.ToString())
                {
                    GetNextToken(true);
                    return __NextList(O, ending);
                }
                else if (GetNextToken(false).value == ending)
                {
                    GetNextToken(true);         //remove ]
                    Token t = GetNextToken(false);
                    if (t.value == TokenSet.listLeft.ToString())
                    {
                        return NextIndex(O);
                    }
                    else if (t.value == TokenSet.paranthesesLeft.ToString())
                    {
                        return NextCall(O);
                    }
                    return O;
                }
                throw SharkCodeFormatException.defaultErr;
            }

            public SharkASTree NextFactor()
            {
                /* find next expression factor 
                factor can be 1.function call, 2.function defination, 3.literal, 4.variable, 5.list and 6.sub expression */

                Token token = GetNextToken(false);
                if (SharkUtil.ContainsVal<TokenType>(TokenSet.literal, token.type))
                {
                    /* literal */

                    return new SharkASTree(GetNextToken(true));
                }
                else if (token.value == TokenSet.listLeft.ToString())
                {
                    /* list */

                    GetNextToken(true);     //remove listLeft
                    SharkASTree O = NextList(TokenSet.listRight.ToString());
                    Token t = GetNextToken(false);
                    if (t.value == TokenSet.listLeft.ToString())
                    {
                        GetNextToken(true);
                        return NextIndex(O);
                    }
                    else if (t.value == TokenSet.paranthesesLeft.ToString())
                    {
                        GetNextToken(true);
                        return NextCall(O);
                    }
                    return O;
                }
                else if (token.value == TokenSet.paranthesesLeft.ToString())
                {
                    /* sub expression */

                    GetNextToken(true);
                    SharkASTree O = NextLogicalExpression();
                    CheckNextToken(TokenSet.paranthesesRight.ToString());
                    if (GetNextToken(false).value == TokenSet.paranthesesLeft.ToString())
                    {
                        return NextCall(O);
                    }
                    else if (GetNextToken(false).value == TokenSet.listLeft.ToString())
                    {
                        return NextIndex(O);
                    }
                    return O;
                }
                else if (token.type == TokenType.ID)
                {
                    /* variable & function call*/

                    Token t = GetNextToken(true);//remove ID
                    if (GetNextToken(false).value == TokenSet.paranthesesLeft.ToString())
                    {
                        return NextCall(t);
                    }
                    if (GetNextToken(false).value == TokenSet.listLeft.ToString())
                    {
                        return NextIndex(t);
                    }
                    return new SharkASTree(t);
                }
                else if (token.value == TokenSet.funcDefination)
                {
                    /* function defination */

                    GetNextToken(true); // remove @
                    return NextDef();
                }
                else if (token.value == TokenSet.opSub.ToString())
                {

                    return NextSub(GetNextToken(true));
                }
                else if (token.value == TokenSet.opNot.ToString())
                {

                    return NextNot(GetNextToken(true));
                }
                else if (token.value == TokenSet.sysNull)
                {

                    return NextNull(GetNextToken(true));
                }
                else
                {
                    throw new SharkCodeFormatException("错误的解析序列");
                }
            }
            public SharkASTree NextNull(Token t)
            {

                return SharkASTree.CreateNullNode(t);
            }
            public SharkASTree NextNot(Token t)
            {
                /* not operator */

                SharkASTree O = new SharkASTree();
                O.content = new NodeCnt(t, NodeType.OP);
                O.nodes = new SharkASTree[1];
                O.Left = NextLogicalExpression();
                return O;
            }
            public SharkASTree NextDef()
            {
                /* find next function defination */

                Token t = GetNextToken(true);
                SharkASTree O;
                if (t.type == TokenType.ID)
                {
                    O = SharkASTree.CreateFunctionDefination(t.value);
                    CheckNextToken(TokenSet.paranthesesLeft.ToString());        //check (
                    O.nodes[1] = NextList(TokenSet.paranthesesRight.ToString());
                    CheckNextToken(TokenSet.blockLeft.ToString());             //check {
                    O.nodes[2] = NextBlock();
                    return O;
                }
                else if (t.value == TokenSet.paranthesesLeft.ToString())
                {
                    O = SharkASTree.CreateFunctionDefination(null);
                    O.nodes[1] = NextList(TokenSet.paranthesesRight.ToString());
                    CheckNextToken(TokenSet.blockLeft.ToString());
                    O.nodes[2] = NextBlock();
                    return O;
                }
                else
                {
                    throw SharkCodeFormatException.defaultErr;
                }
            }
            public SharkASTree NextIndex(Token tk)
            {
                /* find next index block */

                GetNextToken(true);       //remove [
                SharkASTree O = SharkASTree.CreateIndex(tk);
                O.Right = NextMathExpression();
                CheckNextToken(TokenSet.listRight.ToString());// check ]
                Token t = GetNextToken(false);
                if (t.value == TokenSet.paranthesesLeft.ToString())
                {
                    return NextCall(O);
                }
                else if (t.value == TokenSet.listLeft.ToString())
                {
                    return NextIndex(O);
                }
                return O;
            }
            public SharkASTree NextIndex(SharkASTree O)
            {
                /* find next index block */

                GetNextToken(true);       //remove [
                SharkASTree buf = SharkASTree.CreateIndex(O);
                buf.Right = NextMathExpression();
                CheckNextToken(TokenSet.listRight.ToString());      //remove ]
                Token t = GetNextToken(false);
                if (t.value == TokenSet.listLeft.ToString())
                {
                    return NextIndex(buf);
                }
                else if (t.value == TokenSet.paranthesesLeft.ToString())
                {
                    return NextCall(buf);
                }
                return buf;
            }
            public SharkASTree NextCall(Token IdToken)
            {
                /* we get a ( after ID, so it looks like a function call 
                @ID, the ID before ( */

                GetNextToken(true); //remove (
                SharkASTree O = SharkASTree.CreateFunctionCall(IdToken);
                O.Right = NextArgs();
                Token t = GetNextToken(false);
                if (t.value == TokenSet.listLeft.ToString())
                {
                    return NextIndex(O);
                }
                else if (t.value == TokenSet.paranthesesLeft.ToString())
                {
                    return NextCall(O);
                }
                return O;
            }
            public SharkASTree NextCall(SharkASTree func)
            {

                GetNextToken(true);
                SharkASTree O = SharkASTree.CreateFunctionCall(func);
                O.Right = NextArgs();
                Token t = GetNextToken(false);
                if (t.value == TokenSet.listLeft.ToString())
                {
                    return NextIndex(O);
                }
                else if (t.value == TokenSet.paranthesesLeft.ToString())
                {
                    return NextCall(O);
                }
                return O;
            }
            public SharkASTree NextSingle()
            {
                /* search for ^ */

                SharkASTree O = NextFactor();
                while (true)
                {
                    Token next = GetNextToken(false);
                    if (next.value == TokenSet.opPow.ToString())
                    {
                        O = SharkASTree.UpExtend(O, GetNextToken(true));
                        O.Right = NextFactor();
                    }
                    else
                    {
                        break;
                    }
                }
                return O;
            }
            public SharkASTree NextSub(Token token)
            {
                /* search for - */

                SharkASTree O = NextSingle();
                SharkASTree _ = new SharkASTree();
                _.content = new NodeCnt(token, NodeType.OP);
                _.nodes = new SharkASTree[1];
                _.Left = O;
                return _;
            }
            public SharkASTree NextTerm()
            {
                /* search for * and / */

                SharkASTree O = NextSingle();
                while (true)
                {
                    Token next = GetNextToken(false);
                    if (next.value == TokenSet.opDiv.ToString() || next.value == TokenSet.opMul.ToString()
                    || next.value == TokenSet.opMod.ToString())
                    {
                        O = SharkASTree.UpExtend(O, GetNextToken(true));
                        O.Right = NextSingle();
                    }
                    else
                    {
                        break;
                    }
                }
                return O;
            }
            public SharkASTree NextMathExpression()
            {
                /* find next math expression */

                SharkASTree O = NextTerm();
                while (true)
                {
                    Token next = GetNextToken(false);
                    if (next.value == TokenSet.opAdd.ToString() || next.value == TokenSet.opSub.ToString())
                    {
                        O = SharkASTree.UpExtend(O, GetNextToken(true));
                        O.Right = NextTerm();
                    }
                    else
                    {
                        break;
                    }
                }
                return O;
            }
            public SharkASTree NextCompareExpression()
            {
                /* find next compare expression */

                SharkASTree O = NextMathExpression();
                while (true)
                {
                    Token next = GetNextToken(false);
                    if (SharkUtil.Contains<string>(TokenSet.opCompare, next.value))
                    {
                        O = SharkASTree.UpExtend(O, GetNextToken(true));
                        O.Right = NextMathExpression();
                    }
                    else
                    {
                        break;
                    }
                }
                return O;
            }
            public SharkASTree NextLogicalExpression()
            {
                /* find next compare expression */

                SharkASTree O = NextCompareExpression();
                while (true)
                {
                    Token next = GetNextToken(false);
                    if (SharkUtil.Contains<string>(TokenSet.opLogic, next.value))
                    {
                        O = SharkASTree.UpExtend(O, GetNextToken(true));
                        O.Right = NextCompareExpression();
                    }
                    else
                    {
                        break;
                    }
                }
                return O;
            }
            public SharkASTree NextAssignExpression()
            {
                /* find next assign expression */

                SharkASTree O = NextLogicalExpression();
                if (O.IsVarOrLiteral)
                {
                    Token next = GetNextToken(false);
                    if (SharkUtil.Contains<string>(TokenSet.opAssigns, next.value))
                    {
                        O = SharkASTree.UpExtend(O, GetNextToken(true));
                        O.Right = NextLogicalExpression();
                        if (next.value != TokenSet.opAssigns[0])
                        {
                            O.content.token = new Token(TokenType.OPERATOR, O.Value.Substring(0, 1));
                            SharkASTree buf = SharkASTree.UpExtend(O.Left, new Token(TokenType.OPERATOR, TokenSet.opAssign.ToString()));
                            buf.Right = O;
                            O = buf;
                        }
                    }
                    else
                    {
                        throw SharkCodeFormatException.defaultErr;
                    }
                }
                return O;
            }
            public SharkASTree NextOperation()
            {
                /* find next assign until ; 
                operation is the base unit of the block, and block is combined 
                with if block, while block,function defination */

                SharkASTree O = NextAssignExpression();
                Token token = GetNextToken(true);
                if (token.value == TokenSet.codeSeparation.ToString())
                {
                    return O;
                }
                throw new SharkCodeFormatException("语法异常，是否忘记了写分号");
            }
            public SharkASTree NextBlock()
            {
                /* start with { and endwith } */

                SharkASTree O = SharkASTree.CreateCodeBlock();
                if (GetNextToken(false).value == TokenSet.blockRight.ToString())
                {
                    GetNextToken(true);     //remove }
                    return O;
                }
                return __NextBlock(O);
            }
            public SharkASTree __NextBlock(SharkASTree O)
            {
                /* end with } */

                while (true)
                {
                    Token tk = GetNextToken(false);
                    if (tk.value == TokenSet.sysIf)
                    {

                        GetNextToken(true);     //remove sign 'if'
                        O.AddNode(NextIfBlock());
                    }
                    else if (tk.value == TokenSet.sysWhile)
                    {

                        GetNextToken(true);     //remove sign 'while'
                        O.AddNode(NextWhileBlock());
                    }
                    else if (tk.value == TokenSet.blockRight.ToString())
                    {

                        GetNextToken(true);     //remove }
                        break;
                    }
                    else if (tk.value == TokenSet.codeSeparation.ToString())
                    {

                        GetNextToken(true);
                    }
                    else if (tk.type == TokenType.ID)
                    {

                        O.AddNode(NextOperation());
                    }
                    else if (tk.value == TokenSet.sysReturn)
                    {

                        GetNextToken(true);
                        //O.AddNode(NextLogicalExpression());
                        SharkASTree _ = SharkASTree.CreateReturnNode();
                        _.Left = NextLogicalExpression();
                        O.AddNode(_);
                    }
                    else if (tk.value == TokenSet.sysBreak)
                    {

                        GetNextToken(true);
                        SharkASTree _ = SharkASTree.CreateBreakNode();
                        O.AddNode(_);
                    }
                    else if (tk.value == TokenSet.funcDefination)
                    {

                        GetNextToken(true); //remove function
                        O.AddNode(NextDef());
                    }
                    else if (tk.value == TokenSet.global.ToString())
                    {

                        GetNextToken(true);     //remove @
                        SharkASTree _ = SharkASTree.CreateGlobalNode();
                        _.Left = NextFactor();  //a variable name
                        O.AddNode(_);
                    }
                    else
                    {
                        throw SharkCodeFormatException.defaultErr;
                    }
                }
                return O;
            }
            public SharkASTree NextWhileBlock()
            {
                /* find while block */

                SharkASTree O = SharkASTree.CreateWhileBlock();
                // (condition expression)
                CheckNextToken(TokenSet.paranthesesLeft.ToString());
                O.Left = NextLogicalExpression();
                CheckNextToken(TokenSet.paranthesesRight.ToString());

                //{block}
                CheckNextToken(TokenSet.blockLeft.ToString());
                O.Right = NextBlock();
                return O;
            }
            public SharkASTree NextIfBlock()
            {
                /* when we get a 'if', this function will be called 
                to find the complete */

                SharkASTree O = SharkASTree.CreateIFBlock();
                O.Left = NextSubIfBlock();
                while (tokens.Count > 0)
                {
                    /* the block should not check if tokens has run out*/

                    Token tk = GetNextToken(false);
                    if (tk.value == TokenSet.sysElif)
                    {

                        GetNextToken(true);         //remove elif
                        O.AddNode(NextSubIfBlock());
                    }
                    else if (tk.value == TokenSet.sysElse)
                    {

                        GetNextToken(true);         //remove else
                        O.AddNode(NextElseBlock());
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                return O;
            }
            public SharkASTree NextSubIfBlock()
            {
                /* find next subif block */

                SharkASTree O = SharkASTree.CreateSubIfBlock();
                //(condition expression)
                CheckNextToken(TokenSet.paranthesesLeft.ToString());
                O.Left = NextLogicalExpression();
                CheckNextToken(TokenSet.paranthesesRight.ToString());

                //{block}
                CheckNextToken(TokenSet.blockLeft.ToString());
                O.Right = NextBlock();
                return O;
            }
            public SharkASTree NextElseBlock()
            {

                SharkASTree O = SharkASTree.CreateElseBlock();
                CheckNextToken(TokenSet.blockLeft.ToString());
                O.Left = NextBlock();
                return O;
            }

            public SharkASTree GenAST()
            {
                /* generate ast from given tokens */

                SharkASTree O = SharkASTree.CreateCodeBlock();
                while (tokens.Count > 0)
                {
                    Token tk = GetNextToken(false);
                    if (tk.value == TokenSet.sysIf)
                    {
                        GetNextToken(true);
                        O.AddNode(NextIfBlock());
                    }
                    else if (tk.value == TokenSet.sysWhile)
                    {
                        GetNextToken(true);
                        O.AddNode(NextWhileBlock());
                    }
                    else if (tk.value == TokenSet.codeSeparation.ToString())
                    {
                        GetNextToken(true);
                    }
                    else if (tk.type == TokenType.ID)
                    {
                        O.AddNode(NextOperation());
                    }
                    else if (tk.value == TokenSet.funcDefination)
                    {
                        GetNextToken(true);
                        O.AddNode(NextDef());
                    }
                    else if (tk.value == TokenSet.listLeft.ToString())
                    {
                        GetNextToken(true);
                        O.AddNode(NextList(TokenSet.listRight.ToString()));
                    }
                    else
                    {
                        throw SharkCodeFormatException.defaultErr;
                    }
                }
                return O;
            }

            public void CheckNextToken(string sign)
            {

                if (GetNextToken(true).value != sign)
                {
                    throw SharkCodeFormatException.defaultErr;
                }
            }
        }

        public enum OpCode
        {
            move,
            load_const,
            load_var,
            push,
            push_var,
            push_const,
            push_func,
            pusho_func,
            pop_op,
            pop_list,
            pop,
            pop_var,
            index,
            list,
            call,
            _goto,
            jmp,
            assign,
            def,
            enddef,
            add_param,
            add_code,
            ret,
            con,
            load_null,
            push_null,
            glb,
            opneg,
            opadd,
            opsub,
            opmul,
            opdiv,
            oppow,
            opmod,
            opgt,
            oplt,
            opge,
            ople,
            opeq,
            opne,
            opand,
            opor,
            opnot,
        }

        public class opParams
        {
            /* save the parameters needed in target operation */

            public static opParams empty = null;
            public byte position;
            public int constId;
            public string variableId;
            public opParams nextArgs;

            public opParams(byte position, int constId)
            {
                this.position = position;
                this.constId = constId;
                this.variableId = null;
                this.nextArgs = null;
            }
            public opParams(byte position, int constId, string variableId)
            {

                this.position = position;
                this.constId = constId;
                this.variableId = variableId;
                this.nextArgs = null;
            }
            public opParams(byte position, int constId, string variableId, opParams args)
            {

                this.position = position;
                this.constId = constId;
                this.variableId = variableId;
                this.nextArgs = args;
            }
            public SharkCommand ReleaseNextCommand()
            {
                /* release a new command */

                return new SharkCommand(position, nextArgs);
            }
            public override string ToString()
            {
                if (nextArgs == null)
                {
                    return $"{position},{constId},{variableId}";
                }
                return $"{((OpCode)position).ToString()} {nextArgs}";
            }
            public string CvtString(bool isWriteCode)
            {

                if (isWriteCode)
                {
                    if (nextArgs == null)
                    {
                        return $"{((OpCode)position).ToString()}";
                    }
                    return $"{((OpCode)position).ToString()} {nextArgs.CvtString((OpCode)position == OpCode.add_code)}";
                }
                return $"{position},{constId},{variableId}";
            }
        }

        public struct SharkCommand
        {
            /* represent a il command */

            public opParams opArgs;
            public byte opCode;

            public static SharkCommand String2Command_double(string src)
            {

                switch (src)
                {
                    case "+":
                        return SharkCommand.Add;
                    case "-":
                        return SharkCommand.Sub;
                    case "*":
                        return SharkCommand.Mul;
                    case "/":
                        return SharkCommand.Div;
                    case "%":
                        return SharkCommand.Mod;
                    case "^":
                        return SharkCommand.Pow;
                    case ">":
                        return SharkCommand.Gt;
                    case "<":
                        return SharkCommand.Lt;
                    case ">=":
                        return SharkCommand.Ge;
                    case "<=":
                        return SharkCommand.Le;
                    case "!=":
                        return SharkCommand.Ne;
                    case "==":
                        return SharkCommand.Eq;
                    case "&&":
                        return SharkCommand.And;
                    case "||":
                        return SharkCommand.Or;
                    case "!":
                        return SharkCommand.Not;
                }
                throw new SharkException($"未知的符号:{src}");
            }

            public static SharkCommand Push = new SharkCommand((byte)OpCode.push, opParams.empty);
            public static SharkCommand PushOFunc = new SharkCommand((byte)OpCode.pusho_func, opParams.empty);
            public static SharkCommand PopOp = new SharkCommand((byte)OpCode.pop_op, opParams.empty);
            public static SharkCommand PopList = new SharkCommand((byte)OpCode.pop_list, opParams.empty);
            public static SharkCommand Index = new SharkCommand((byte)OpCode.index, opParams.empty);
            public static SharkCommand MakeList = new SharkCommand((byte)OpCode.list, opParams.empty);
            public static SharkCommand Call = new SharkCommand((byte)OpCode.call, opParams.empty);
            public static SharkCommand Def = new SharkCommand((byte)OpCode.def, opParams.empty);
            public static SharkCommand EndDef = new SharkCommand((byte)OpCode.enddef, opParams.empty);
            public static SharkCommand Neg = new SharkCommand((byte)OpCode.opneg, opParams.empty);
            public static SharkCommand Add = new SharkCommand((byte)OpCode.opadd, opParams.empty);
            public static SharkCommand Sub = new SharkCommand((byte)OpCode.opsub, opParams.empty);
            public static SharkCommand Mul = new SharkCommand((byte)OpCode.opmul, opParams.empty);
            public static SharkCommand Div = new SharkCommand((byte)OpCode.opdiv, opParams.empty);
            public static SharkCommand Mod = new SharkCommand((byte)OpCode.opmod, opParams.empty);
            public static SharkCommand Gt = new SharkCommand((byte)OpCode.opgt, opParams.empty);
            public static SharkCommand Lt = new SharkCommand((byte)OpCode.oplt, opParams.empty);
            public static SharkCommand Ge = new SharkCommand((byte)OpCode.opge, opParams.empty);
            public static SharkCommand Le = new SharkCommand((byte)OpCode.ople, opParams.empty);
            public static SharkCommand Eq = new SharkCommand((byte)OpCode.opeq, opParams.empty);
            public static SharkCommand Ne = new SharkCommand((byte)OpCode.opne, opParams.empty);
            public static SharkCommand Pow = new SharkCommand((byte)OpCode.oppow, opParams.empty);
            public static SharkCommand And = new SharkCommand((byte)OpCode.opand, opParams.empty);
            public static SharkCommand Or = new SharkCommand((byte)OpCode.opor, opParams.empty);
            public static SharkCommand Not = new SharkCommand((byte)OpCode.opnot, opParams.empty);
            public static SharkCommand Return = new SharkCommand((byte)OpCode.ret, opParams.empty);
            public static SharkCommand PushNull = new SharkCommand((byte)OpCode.push_null, opParams.empty);

            public SharkCommand(byte code, opParams args)
            {

                opCode = code;
                opArgs = args;
            }
            public opParams ToParams()
            {

                return new opParams(opCode, 0, null, opArgs);
            }
            public static SharkCommand Build(OpCode code, byte pos, int constId, string variableId)
            {

                return new SharkCommand((byte)code, new opParams(pos, constId, variableId));
            }
            public override string ToString()
            {
                if (opArgs == null)
                {
                    return $"[{((OpCode)opCode).ToString()}: ]";
                }
                return $"[{((OpCode)opCode).ToString()}: {opArgs.CvtString(opCode == 21)}]";
            }
        }

        public class SharkILGenerator
        {
            /* used to generate shark il code */

            public List<SharkCommand> commandBuffer;
            public List<SharkCommand> constantList;
            int ifLvl;
            int whileLvl;

            public List<SkFunc> funcList;
            Stack<List<int>> breakPositions;


            public Tuple<List<SharkCommand>, List<SharkCommand>> GenForScript(SharkASTree program)
            {

                commandBuffer = new List<SharkCommand>();
                constantList = new List<SharkCommand>();
                funcList = new List<SkFunc>();
                ifLvl = 0;
                GenForBlock(program);
                return new Tuple<List<SharkCommand>, List<SharkCommand>>(commandBuffer, constantList);
            }
            public SharkILGenerator()
            {

                commandBuffer = new List<SharkCommand>();
                constantList = new List<SharkCommand>();
                breakPositions = new Stack<List<int>>();
                ifLvl = 0;
            }
            public int GetConstantId(Token tk)
            {
                /* search constant in target list */

                for (int i = 0; i < constantList.Count; i++)
                {
                    if (constantList[i].opArgs.variableId == tk.value)
                    {
                        return i;
                    }
                }
                constantList.Add(SharkCommand.Build(OpCode.con, (byte)tk.type, 0, tk.value));
                return constantList.Count - 1;
            }
            public void HandleVal(SharkASTree valNode)
            {
                /* if no position has been sent, then target value 
                will be pushed into list stack */

                if (valNode.content.token.type == TokenType.ID)
                {
                    commandBuffer.Add(SharkCommand.Build(OpCode.push_var, 0, 0, valNode.Value));
                }
                else if (valNode.NdType == NodeType.NONE)
                {
                    commandBuffer.Add(SharkCommand.PushNull);
                }
                else
                {
                    commandBuffer.Add(SharkCommand.Build(OpCode.push_const, 0, GetConstantId(valNode.content.token), null));
                }
            }
            public void HandleVal(SharkASTree valNode, byte pos)
            {
                /* push value into target register */

                if (valNode.content.token.type == TokenType.ID)
                {
                    commandBuffer.Add(SharkCommand.Build(OpCode.load_var, pos, 0, valNode.Value));
                }
                else if (valNode.NdType == NodeType.NONE)
                {
                    commandBuffer.Add(SharkCommand.Build(OpCode.load_null, pos, 0, null));
                }
                else
                {
                    commandBuffer.Add(SharkCommand.Build(OpCode.load_const, pos, GetConstantId(valNode.content.token), null));
                }
            }
            public void HandleOperator(SharkASTree opNode)
            {
                /* generate il code for operator node */

                if (opNode.NdType == NodeType.OP)
                {
                    GenForOperator(opNode);
                }
                else if (opNode.NdType == NodeType.FUNC)
                {
                    GenForFunc(opNode);
                }
                else if (opNode.NdType == NodeType.INDEX)
                {
                    GenForIndex(opNode);
                }
                else
                {
                    throw new SharkCodeFormatException("不支持的运算数");
                }
            }
            public void HandleConditionExpr(SharkASTree O)
            {

                SharkCommand moveResult2Condition = SharkCommand.Build(OpCode.move, 3, 2, null);
                if (O.Left.NdType == NodeType.INDEX)
                {

                    GenForIndex(O.Left);
                    commandBuffer.Add(moveResult2Condition);
                }
                else if (O.Left.NdType == NodeType.OP)
                {

                    GenForOperator(O.Left);
                    commandBuffer.Add(moveResult2Condition);
                }
                else if (O.Left.NdType == NodeType.FUNC)
                {
                    GenForFunc(O.Left);
                    commandBuffer.Add(moveResult2Condition);
                }
                else if (O.Left.IsVarOrLiteral)
                {
                    HandleVal(O.Left, 2);
                }
                else
                {
                    throw new SharkCodeFormatException("不支持的条件表达式");
                }
            }
            public void GenForGlobal(SharkASTree O)
            {
                /* generate il code for global operator */

                commandBuffer.Add(SharkCommand.Build(OpCode.glb, 0, 0, O.LeftValue));
            }
            public void GenForBlock(SharkASTree O)
            {
                /* generate il code for block node */

                SharkASTree subNode;
                for (int i = 0; i < O.BlockSize; i++)
                {
                    /* iter for sub nodes */

                    subNode = O.nodes[i];
                    if (subNode.NdType == NodeType.ASSIGN)
                    {
                        GenForAssign(subNode);
                    }
                    else if (subNode.NdType == NodeType.FUNC)
                    {
                        GenForFunc(subNode);
                    }
                    else if (subNode.NdType == NodeType.IF)
                    {
                        GenForIF(subNode);
                    }
                    else if (subNode.NdType == NodeType.LOOP)
                    {
                        GenForWhile(subNode);
                    }
                    else if (subNode.NdType == NodeType.RETURN)
                    {
                        GenForReturnNode(subNode);
                    }
                    else if (subNode.NdType == NodeType.DEF)
                    {
                        GenForDefination(subNode);
                    }
                    else if (subNode.NdType == NodeType.BREAK)
                    {
                        GenForBreak(subNode);
                    }
                    else if (subNode.NdType == NodeType.GLOBAL)
                    {
                        GenForGlobal(subNode);
                    }
                    else
                    {
                        /* do nothing for other node */
                    }
                }
            }
            public void GenForReturnNode(SharkASTree O)
            {
                /* defination a new function */

                if (O.Left.IsVar)
                {
                    /* 如果函数直接返回一个变量或者值，则该值应该被添加到结果寄存器 */

                    commandBuffer.Add(SharkCommand.Build(OpCode.load_var, 3, 0, O.LeftValue));
                }
                else if (O.Left.IsLiteral)
                {

                    commandBuffer.Add(SharkCommand.Build(OpCode.load_const, 3, GetConstantId(O.Left.content.token), null));
                }
                else
                {

                    if (O.Left.NdType == NodeType.INDEX)
                    {
                        GenForIndex(O.Left);
                    }
                    else if (O.Left.NdType == NodeType.DEF)
                    {
                        GenForDefination(O.Left);
                    }
                    else if (O.Left.NdType == NodeType.FUNC)
                    {
                        GenForFunc(O.Left);
                    }
                    else if (O.Left.NdType == NodeType.LIST)
                    {
                        GenForList(O.Left, false);
                    }
                    else if (O.Left.NdType == NodeType.OP)
                    {
                        GenForOperator(O.Left);
                    }
                    else
                    {
                        throw new SharkException("错误的返回值");
                    }
                }
                /* 其他类型的节点计算的结果都会自动加到结果寄存器中 */
                commandBuffer.Add(SharkCommand.Return);
            }
            public void GenForIF(SharkASTree O)
            {

                ifLvl++;
                int endPoint = O.BlockSize - 1;
                List<int> gotoCmds = new List<int>();
                for (int i = 0; i < O.BlockSize; i++)
                {
                    GenForSubIf(O.nodes[i], O.BlockSize);
                    if (i != endPoint)
                    {
                        gotoCmds.Add(commandBuffer.Count);
                    }
                }
                int addrOut = commandBuffer.Count + gotoCmds.Count + ifLvl - 1;
                for (int i = 0; i < gotoCmds.Count; i++)
                {
                    commandBuffer.Insert(gotoCmds[i] + i, SharkCommand.Build(OpCode._goto, 0, addrOut, null));
                }
                ifLvl--;
            }
            public void GenForSubIf(SharkASTree O, int count)
            {

                SharkCommand moveResult2Condition = SharkCommand.Build(OpCode.move, 3, 2, null);
                if (O.NdType == NodeType.SUBIF)
                {
                    HandleConditionExpr(O);
                    int addrNow = commandBuffer.Count;
                    GenForBlock(O.Right);
                    int offset = commandBuffer.Count - addrNow;
                    commandBuffer.Insert(addrNow, SharkCommand.Build(OpCode.jmp, 0, offset + (count == 1 ? 0 : 1), null));
                }
                else
                {
                    GenForBlock(O.Left);
                }
            }
            public void GenForBreak(SharkASTree O)
            {

                breakPositions.Peek().Add(commandBuffer.Count);
                commandBuffer.Add(SharkCommand.Build(OpCode._goto, 0, 0, null));
            }
            public void GenForWhile(SharkASTree O)
            {

                whileLvl++;
                breakPositions.Push(new List<int>());
                int addrStart = commandBuffer.Count - 1;
                HandleConditionExpr(O);
                int addrBodyStart = commandBuffer.Count;
                GenForBlock(O.Right);
                commandBuffer.Insert(addrBodyStart, SharkCommand.Build(OpCode.jmp, 0, commandBuffer.Count - addrBodyStart + 1, null));
                commandBuffer.Add(SharkCommand.Build(OpCode._goto, 0, addrStart + whileLvl - 1, null));
                List<int> poses = breakPositions.Pop();
                int whileOut = commandBuffer.Count - 1;
                for (int i = 0; i < poses.Count; i++)
                {
                    commandBuffer[poses[i] + 2].opArgs.constId = whileOut + whileLvl - 1;
                }
                whileLvl--;
            }
            public void GenForAssign(SharkASTree O)
            {

                if (O.Left.IsVar)
                {
                    if (O.Right.IsVar)
                    {
                        commandBuffer.Add(SharkCommand.Build(OpCode.load_var, 0, 0, O.LeftValue));
                    }
                    else if (O.Right.IsLiteral)
                    {
                        commandBuffer.Add(SharkCommand.Build(OpCode.assign, 0, GetConstantId(O.Right.content.token), O.LeftValue));
                        return;
                    }
                    else
                    {
                        if (O.Right.NdType == NodeType.OP)
                        {
                            GenForOperator(O.Right);
                        }
                        else if (O.Right.NdType == NodeType.FUNC)
                        {
                            GenForFunc(O.Right);
                        }
                        else if (O.Right.NdType == NodeType.LIST)
                        {
                            GenForList(O.Right, false);
                        }
                        else if (O.Right.NdType == NodeType.DEF)
                        {
                            GenForDefination(O.Right);
                        }
                        else if (O.Right.NdType == NodeType.INDEX)
                        {
                            GenForIndex(O.Right);
                        }
                        else if (O.Right.NdType == NodeType.NONE)
                        {
                            HandleVal(O.Right, 3);
                        }
                        else
                        {
                            throw new SharkCodeFormatException("不支持的操作");
                        }
                    }
                    commandBuffer.Add(SharkCommand.Build(OpCode.pop_var, 0, 0, O.LeftValue));
                    return;
                }
                throw new SharkCodeFormatException("不能给非变量对象赋值");
            }
            public void GenForDefination(SharkASTree O)
            {
                /* define a new function 
                创建一个空的函数对象，然后向其中写入变量和指令代码 */

                commandBuffer.Add(SharkCommand.Def);        //这条指令会让虚拟机在脚本中创建一个函数的原型
                for (int i = 0; i < O.Right.BlockSize; i++)
                {
                    //将参数名写入函数的参数名列表
                    commandBuffer.Add(SharkCommand.Build(OpCode.add_param, 0, 0, O.Right.nodes[i].Value));
                }
                List<SharkCommand> tmp = commandBuffer;
                commandBuffer = new List<SharkCommand>();

                GenForBlock(O.nodes[2]);//为函数体生成指令代码
                foreach (SharkCommand cmd in commandBuffer)
                {
                    tmp.Add(new SharkCommand((byte)OpCode.add_code, cmd.ToParams()));
                }
                tmp.Add(SharkCommand.EndDef);
                if (O.LeftValue != null)
                {
                    tmp.Add(SharkCommand.Build(OpCode.pop_var, 0, 0, O.LeftValue));
                }
                commandBuffer = tmp;
            }
            public void GenForIndex(SharkASTree O)
            {

                if (O.Left.NdType == NodeType.LIST)
                {
                    GenForList(O.Left, false);
                }
                else if (O.Left.NdType == NodeType.FUNC)
                {
                    GenForFunc(O.Left);
                }
                else if (O.Left.NdType == NodeType.INDEX)
                {
                    GenForIndex(O.Left);
                }
                else if (O.Left.IsVar)
                {
                    HandleVal(O.Left, 0);
                }
                else
                {
                    throw new SharkCodeFormatException("不支持的索引对象");
                }
                SharkCommand Pop0 = SharkCommand.Build(OpCode.pop, 0, 0, null);
                if (O.Right.NdType == NodeType.FUNC)
                {
                    commandBuffer.Add(SharkCommand.Push);
                    GenForFunc(O.Right);
                    commandBuffer.Add(Pop0);
                }
                else if (O.Right.NdType == NodeType.INDEX)
                {

                    commandBuffer.Add(SharkCommand.Push);
                    GenForIndex(O.Right);
                    commandBuffer.Add(Pop0);
                }
                else if (O.Right.NdType == NodeType.OP)
                {

                    commandBuffer.Add(SharkCommand.Push);
                    GenForOperator(O.Right);
                    commandBuffer.Add(Pop0);
                }
                else if (O.Right.IsVarOrLiteral)
                {

                    commandBuffer.Add(SharkCommand.Build(OpCode.move, 0, 3, null));
                    HandleVal(O.Right, 1);
                }
                commandBuffer.Add(SharkCommand.Index);
            }
            public void GenForOperator(SharkASTree O)
            {

                if (O.Left.IsVarOrLiteralOrNone && O.Right.IsVarOrLiteralOrNone)
                {
                    HandleVal(O.Left, 0);
                    HandleVal(O.Right, 1);
                }
                else if (O.Left.IsVarOrLiteral)
                {

                    HandleOperator(O.Right);
                    commandBuffer.Add(SharkCommand.Build(OpCode.move, 3, 1, null));
                    HandleVal(O.Left, 0);
                }
                else if (O.Right.IsVarOrLiteral)
                {

                    HandleOperator(O.Left);
                    commandBuffer.Add(SharkCommand.Build(OpCode.move, 3, 1, null));
                    HandleVal(O.Right, 0);
                }
                else
                {
                    HandleOperator(O.Left);
                    commandBuffer.Add(SharkCommand.PopOp);      //压入运算数栈
                    HandleOperator(O.Right);
                    commandBuffer.Add(SharkCommand.Build(OpCode.move, 3, 1, null));
                    commandBuffer.Add(SharkCommand.Build(OpCode.pop, 0, 0, null));

                }
                commandBuffer.Add(SharkCommand.String2Command_double(O.Value));
            }
            public void GenForFunc(SharkASTree O)
            {
                /* generate il code for op node */


                if (O.Left.IsVar)
                {
                    commandBuffer.Add(SharkCommand.Build(OpCode.push_func, 0, 0, O.LeftValue));
                }
                else if (O.Left.NdType == NodeType.FUNC)
                {
                    GenForFunc(O.Left);
                    commandBuffer.Add(SharkCommand.PushOFunc);
                }
                else if (O.Left.NdType == NodeType.INDEX)
                {
                    GenForIndex(O.Left);
                    commandBuffer.Add(SharkCommand.PushOFunc);
                }
                else
                {
                    throw new SharkCodeFormatException("无法执行的对象");
                }
                GenForList(O.Right, true);
                commandBuffer.Add(SharkCommand.Call);
            }
            public void GenForList(SharkASTree O, bool isParamsList)
            {

                commandBuffer.Add(SharkCommand.MakeList);
                SharkASTree subNode;
                for (int i = 0; i < O.BlockSize; i++)
                {
                    subNode = O.nodes[i];
                    if (subNode.IsOperator)
                    {
                        /* 运算符类型的操作数 */

                        GenForOperator(subNode);
                        commandBuffer.Add(SharkCommand.Push);
                    }
                    else if (subNode.IsVarOrLiteral)
                    {

                        HandleVal(subNode);
                    }
                    else if (subNode.NdType == NodeType.INDEX)
                    {

                        GenForIndex(subNode);
                        commandBuffer.Add(SharkCommand.Push);
                    }
                    else if (subNode.NdType == NodeType.FUNC)
                    {

                        GenForFunc(subNode);
                        commandBuffer.Add(SharkCommand.Push);
                    }
                    else if (subNode.NdType == NodeType.DEF)
                    {

                        GenForDefination(subNode);
                        commandBuffer.Add(SharkCommand.Push);
                    }
                    else if (subNode.NdType == NodeType.LIST)
                    {

                        GenForList(subNode, false);
                        commandBuffer.Add(SharkCommand.Push);
                    }
                    else if (subNode.NdType == NodeType.NONE)
                    {

                        commandBuffer.Add(SharkCommand.PushNull);
                    }
                    else
                    {
                        throw new SharkCodeFormatException("不支持的列表元素");
                    }
                }
                if (!isParamsList)
                {
                    commandBuffer.Add(SharkCommand.PopList);
                }
            }
        }
    }

    namespace SharkVirtualMachine
    {
        /* used to run SharkIL code */

        public class SharkAPI
        {
            /* the operation of Shark */

            public static string GetCString(SkObject skData)
            {

                return (string)skData.GetValue<SkVal>().RawData;
            }
            public static SkObject GetSkString(string cData)
            {

                return new SkObject(new SkVal(cData));
            }
            public static int GetCInt(SkObject skData)
            {

                return skData.GetValue<SkVal>().GetInt();
            }
            public static SkObject GetSkInt(int cData)
            {

                return new SkObject(new SkVal(cData));
            }
            public static float GetCFloat(SkObject skData)
            {

                return skData.GetValue<SkVal>().GetFloat();
            }
            public static SkObject GetSkFloat(float cData)
            {

                return new SkObject(new SkVal(cData));
            }
            public static bool GetCBool(SkObject skData)
            {

                return skData.GetValue<SkVal>().GetBool();
            }
            public static SkObject GetSkBool(bool cData)
            {

                return new SkObject(new SkVal(cData));
            }

            public static SkObject Neg(SkObject obj)
            {
                if (obj.IsVal)
                {
                    SkVal val = obj.GetValue<SkVal>();
                    if (val.IsFloat)
                    {
                        return new SkObject(new SkVal(-val.GetFloat()));
                    }
                    else if (val.IsInt)
                    {
                        return new SkObject(new SkVal(-val.GetInt()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }

            public static SkObject Add(SkObject left, SkObject right)
            {
                /* add two value, and note that, only number or string type 
                value can do this operation */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsText && r.IsText)
                    {
                        return new SkObject(new SkVal(l.GetValue<string>() + r.GetValue<string>()));
                    }
                    else if (l.IsInt && r.IsInt)
                    {
                        return new SkObject(new SkVal(l.GetInt() + r.GetInt()));
                    }
                    else if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() + r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Sub(SkObject left, SkObject right)
            {
                /* compute left - right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsInt && r.IsInt)
                    {
                        return new SkObject(new SkVal(l.GetInt() - r.GetInt()));
                    }
                    else if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() - r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Mul(SkObject left, SkObject right)
            {
                /* return left * right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsInt && r.IsInt)
                    {
                        return new SkObject(new SkVal(l.GetInt() * r.GetInt()));
                    }
                    else if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() * r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Div(SkObject left, SkObject right)
            {
                /* return left / right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsInt && r.IsInt)
                    {
                        return new SkObject(new SkVal(l.GetInt() / r.GetInt()));
                    }
                    else if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() / r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Mod(SkObject left, SkObject right)
            {
                /* return left % right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsInt && r.IsInt)
                    {
                        return new SkObject(new SkVal(l.GetInt() % r.GetInt()));
                    }
                    else if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() % r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Pow(SkObject left, SkObject right)
            {
                /* return left % right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(Math.Pow(l.GetFloat(), r.GetFloat())));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Gt(SkObject left, SkObject right)
            {
                /* return left > right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() > r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Lt(SkObject left, SkObject right)
            {
                /* return left < right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() < r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Ge(SkObject left, SkObject right)
            {
                /* return left >= right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() >= r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Le(SkObject left, SkObject right)
            {
                /* return left <= right */

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsNumber && r.IsNumber)
                    {
                        return new SkObject(new SkVal(l.GetFloat() <= r.GetFloat()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject And(SkObject left, SkObject right)
            {

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsBoolean && r.IsBoolean)
                    {
                        return new SkObject(new SkVal(l.GetBool() && r.GetBool()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Or(SkObject left, SkObject right)
            {

                if (left.IsVal && right.IsVal)
                {
                    SkVal l = left.GetValue<SkVal>();
                    SkVal r = right.GetValue<SkVal>();
                    if (l.IsBoolean && r.IsBoolean)
                    {
                        return new SkObject(new SkVal(l.GetBool() || r.GetBool()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
            public static SkObject Not(SkObject obj)
            {
                if (obj.IsVal)
                {
                    SkVal val = obj.GetValue<SkVal>();
                    if (val.IsBoolean)
                    {
                        return new SkObject(new SkVal(!val.GetBool()));
                    }
                }
                throw SharkTypeError.defaultErr;
            }
        }

        public class SharkField
        {
            /* maintain a dictionary and save SkObject according to string */

            private Dictionary<string, SkObject> field;
            public SharkField()
            {

                field = new Dictionary<string, SkObject>();
            }
            public bool Contains(string name)
            {

                return field.ContainsKey(name);
            }
            public void Declear(string ID)
            {
                /* create an empty value 
                if target ID has been used, then this function won't work */

                if (!field.ContainsKey(ID))
                {
                    field.Add(ID, new SkObject());
                }
            }
            public void Rewrite(string ID, SkObject Obj)
            {
                /* rewrite target ID with given value */

                if (!field.ContainsKey(ID))
                {
                    field.Add(ID, Obj);
                }
                field[ID] = Obj;
            }
            public SkObject Get(string ID)
            {
                /* get value with given ID */

                if (!field.ContainsKey(ID))
                {
                    throw SharkNameError.defaultErr;
                }
                return field[ID];
            }
            public void Clear()
            {
                /* remove all values */

                field.Clear();
            }
            public SharkField Clone()
            {

                SharkField _ = new SharkField();
                foreach (string name in field.Keys)
                {
                    _.field.Add(name, field[name]);
                }
                return _;
            }
        }

        public class SharkRegister
        {
            /* object can save single object data */

            private SkObject data;
            /* data saved in register */

            public SkObject Data
            {
                get { return data; }
                set { data = value; }
            }

            public SharkRegister()
            {
                /* initialize data */

                data = new SkObject();
            }
            public T FetchData<T>()
            {

                return data.GetValue<T>();
            }
        }

        public class SharkScript : IJumper
        {
            /* shark script represent one sk file including commands 
            and global field and function list */

            public static CodeScanner scanner = new CodeScanner();
            public static CodeParser parser = new CodeParser();
            public static SharkILGenerator generator = new SharkILGenerator();

            public static SharkScript ReadFile(string filepath)
            {
                /* read source code and create a new shark script object for this */

                string sourceCode = SharkUtil.ReadSource(filepath);
                return ReadText(sourceCode);
            }
            public static SharkScript ReadText(string src){
                /* read string type value and create a new Shark Script for it */

                scanner.LoadSource(src);
                parser.LoadTokens(scanner.Scan());
                Tuple<List<SharkCommand>, List<SharkCommand>> O = generator.GenForScript(parser.GenAST());
                SharkScript _ = new SharkScript(O.Item2, O.Item1);
                return _;
            }

            private SharkField globalField;
            /* the global scope */

            private List<SkObject> constantList;
            /* save all constant */

            private List<SharkField> localField;
            /* the field of target function, when 
            target function is a Shark Function, then this field would 
            be added */

            private List<List<string>> globalVars;

            public int Addr { get; set; }
            public List<SharkCommand> Constants{
                get{return constants;}
            }
            public List<SharkCommand> Codes{
                get{return codes;}
            }
            public SharkCommand NextCommand{
                get{return codes[Addr];}
            }

            private Stack<IJumper> LastObjs;
            private IJumper now;
            private SkFunc definationBuf;
            private List<SharkCommand> constants;
            private List<SharkCommand> codes;
            private int funcLvl;

            public void Global(string name)
            {
                /* 将全局域中的变量引入到局部域中 
                @name: 要引入的变量名 */

                if (IsCallFunction)
                {
                    //if isn't calling a function, then this command will be ignored;
                    globalVars[funcLvl].Add(name);
                }
            }
            public void ShowCommands()
            {
                for (int i = 0; i < codes.Count; i++)
                {
                    Console.WriteLine($"{i}:{codes[i]}");
                }
            }
            public SharkScript(List<SharkCommand> constants, List<SharkCommand> codes)
            {

                globalField = new SharkField();
                constantList = new List<SkObject>();
                localField = new List<SharkField>();
                LastObjs = new Stack<IJumper>();
                globalVars = new List<List<string>>();
                funcLvl = -1;

                now = this;

                this.constants = constants;
                this.codes = codes;
            }
            public void RunScript(SharkInterpreter interpreter)
            {

                Addr = 0;
                interpreter.LoadScript(this);
                foreach (SharkCommand cmd in constants)
                {
                    interpreter.RunCommand(cmd);
                }
                while (IsRunning)
                {
                    interpreter.RunCommand(this.codes[Addr]);
                    Addr++;
                }
            }
            public SkObject RunCallable(SharkInterpreter interpreter, SkFunc function)
            {
                /* 将目标函数作为该脚本中的一个对象来执行
                该函数只运行无参的可调用对象，如果目标有参，则必须附加参数 */

                PushFunction(function);
                function.Call(interpreter, SkList.emptyList);
                return interpreter.GetRegister(3).Data;
            }
            public SkObject RunCallable(SharkInterpreter interpreter, SkFunc function, SkList args)
            {

                PushFunction(function);
                function.Call(interpreter, args);
                return interpreter.GetRegister(3).Data;
            }
            public bool IsRunning
            {
                get { return Addr < this.codes.Count; }
            }
            public bool IsCallFunction
            {
                get { return funcLvl >= 0; }
            }
            public void CallFunction(SkFunc func, SkList args, SharkInterpreter interpreter)
            {
                /* 执行一个Shark风格的函数 */

                PushFunction(func);
                func.Call(interpreter, args);
            }
            public void ReturnLast()
            {
                /* 回到上一个调用的函数 */

                ((ICallable)now).ShutDown();
                PopLocalField();
                now = LastObjs.Count > 0 ? LastObjs.Pop() : this;
            }
            public void opJump(int offset)
            {

                now.Jmp(offset);
            }
            public void opGoto(int addr)
            {

                now.Goto(addr);
            }
            public void opDefination()
            {
                /* 构建一个新的函数原型 */

                definationBuf = new SkFunc();
            }
            public void opAddParams(string argName)
            {
                /* 增加函数的参数 */

                definationBuf.paramsList.Add(argName);
            }
            public SkObject GetFunctionBuf()
            {
                /* 取得当前正在定义的函数对象 */

                return new SkObject(definationBuf);
            }
            public void opWriteCode(opParams args)
            {
                /* 向当前在定义的函数对象中写入指令 */

                SharkCommand cmd = args.ReleaseNextCommand();       //write_code pop_var 0,0,x
                if (IsCallFunction)
                {
                    /* if is calling a function now, then we should record the variable 
                    defined in this function */

                    OpCode opCode = (OpCode)cmd.opCode;
                    if (opCode == OpCode.assign || opCode == OpCode.pop_var)
                    {
                        /* these code can make new variable */

                        definationBuf.scope.Declear(cmd.opArgs.variableId);
                    }
                    else if (opCode == OpCode.load_var || opCode == OpCode.push_var || opCode == OpCode.push_func)
                    {
                        /* these code would visit target function */

                        if (!definationBuf.CanSearchName(cmd.opArgs.variableId))
                        {
                            /* 函数内部搜索不到的值 */

                            SkObject Obj = SearchVariableInUpLvl(cmd.opArgs.variableId);
                            /* 尝试在上级函数的域中搜索(忽略全局域)*/

                            if (Obj != null)
                            {
                                /* 上级函数的域中确实可以搜索到目标变量,则该变量应该复制到该函数的域中 */

                                definationBuf.scope.Rewrite(cmd.opArgs.variableId, Obj);
                            }
                            //否则则默认可以在全局域中搜索到..
                        }
                    }
                }
                definationBuf.WriteOperations(cmd);
            }
            public bool IsFunction()
            {
                return false;
            }
            public void AddConstant(SkObject value)
            {

                constantList.Add(value);
            }
            public void PushFunction(SkFunc func)
            {
                /* add function field to this list */

                funcLvl++;
                localField.Add(func.scope);
                globalVars.Add(new List<string>());
                if (IsCallFunction)
                {
                    LastObjs.Push(now);
                }
                now = func;
            }
            public void PopLocalField()
            {
                /* remove the last unit */

                localField.RemoveAt(funcLvl);
                globalVars.RemoveAt(funcLvl);
                funcLvl--;
            }
            public SkObject GetConstant(int constId)
            {
                /* get constant in target position */

                return constantList[constId].CloneConstant();
            }
            public void SetVariable(string name, SkObject data)
            {
                /* rewrite the data of target name */

                if (IsCallFunction)
                {
                    if (globalVars[funcLvl].Contains(name))
                    {
                        
                        globalField.Rewrite(name, data);
                    }
                    else
                    {
                        localField[funcLvl].Rewrite(name, data);
                    }
                }
                else
                {
                    globalField.Rewrite(name, data);
                }
            }
            public SkObject SearchVariableInUpLvl(string name)
            {

                for (int i = localField.Count - 1; i >= 0; i--)
                {
                    if (globalVars[i].Contains(name))
                    {
                        return globalField.Get(name);
                    }
                    else
                    {
                        if (localField[i].Contains(name))
                        {
                            return localField[i].Get(name);
                        }
                    }
                }
                return null;
            }
            public SkObject SearchVariable(string name)
            {
                /* get target variable */

                for (int i = localField.Count - 1; i >= 0; i--)
                {
                    if (globalVars[i].Contains(name))
                    {
                        return globalField.Get(name);
                    }
                    else
                    {
                        if (localField[i].Contains(name))
                        {
                            return localField[i].Get(name);
                        }
                    }
                }
                return globalField.Get(name);
            }
            public void Jmp(int offset)
            {
                Addr += offset;
            }
            public void Goto(int addr)
            {
                Addr = addr;
            }
        }

        public class SharkInterpreter
        {
            /* shark virtual machine 
            contained four register */

            public delegate void Operation(opParams arsg);

            public static Operation[] ops;

            public const byte REG_0 = 0;
            public const byte REG_1 = 1;
            public const byte REG_COND = 2;
            public const byte REG_RESULT = 3;

            private SharkRegister[] Registers;
            /* Register contained four register the first and second 
            is compute register and the third is condition register, 
            the fourth is result condition */

            private Stack<SkObject> operationStk;
            /* a new skobject will be push into this stack only 
            if left and right node are all operator */

            private Stack<ICallable> funcStk;
            /* when one function has been called, then it will be saved in 
            this stack, and if target function is not found in baseField, then
            a name error will be thrown */

            private Stack<SkList> paramsStk;
            /* when call a function or create a new list, Shark would add 
            a new empty list to this stack */

            private SharkScript script;

            public SharkInterpreter()
            {
                /* initialize base components */

                Registers = new SharkRegister[4];
                for (int i = 0; i < Registers.Length; i++)
                {
                    Registers[i] = new SharkRegister();
                }
                funcStk = new Stack<ICallable>();
                paramsStk = new Stack<SkList>();
                operationStk = new Stack<SkObject>();
                InitializeOps();
            }

            public void InitializeOps()
            {
                /* 初始化虚拟机指令集 */

                ops = new Operation[]{
                    /* list */

                    Move,
                    LoadConst,
                    LoadVar,
                    Push,
                    PushVar,
                    PushConst,
                    PushFunction,
                    PushOFunction,
                    PopOp,
                    PopList,
                    Pop,
                    PopVar,
                    Index,
                    MakeList,
                    Call,
                    Goto,
                    Jmp,
                    Assign,
                    Def,
                    EndDef,
                    AddParam,
                    AddCode,
                    Ret,
                    Constant,
                    LoadNull,
                    PushNull,
                    Global,
                    opNeg,
                    opAdd,
                    opSub,
                    opMul,
                    opDiv,
                    opPow,
                    opMod,
                    opGt,
                    opLt,
                    opGe,
                    opLe,
                    opEq,
                    opNe,
                    opAnd,
                    opOr,
                    opNot
                };
            }
            public void RunCommand(SharkCommand cmd)
            {
                /* run single command */

                //Console.WriteLine(cmd);
                ops[cmd.opCode](cmd.opArgs);
            }
            public void RunScript(SharkScript script){
                /* 执行Shark脚本 */

                script.Addr = 0;
                LoadScript(script);
                foreach(SharkCommand cmd in script.Constants){
                    RunCommand(cmd);
                }
                while(script.IsRunning){
                    RunCommand(script.NextCommand);
                    script.Addr ++;
                }
            }

            public void LoadScript(SharkScript script)
            {
                /* change the current script */

                this.script = script;
            }
            public SharkRegister GetRegister(int idx)
            {

                return Registers[idx];
            }
            public void Move(opParams args)
            {
                /* move the value in register position to constId */

                Registers[args.constId].Data = Registers[args.position].Data;
            }
            public void LoadConst(opParams args)
            {
                /* load a constant value into target register */

                Registers[args.position].Data = script.GetConstant(args.constId);
            }
            public void LoadVar(opParams args)
            {
                /* load target variable into target register */

                Registers[args.position].Data = script.SearchVariable(args.variableId);
            }
            public void Push(opParams args)
            {
                /* push value in result register into current param stack */

                paramsStk.Peek().Add(Registers[REG_RESULT].Data);
            }
            public void PushVar(opParams args)
            {
                /* push variable into param stack */

                paramsStk.Peek().Add(script.SearchVariable(args.variableId));
            }
            public void PushConst(opParams args)
            {
                /* push constant value into param stack */

                paramsStk.Peek().Add(script.GetConstant(args.constId));
            }
            public void PushFunction(opParams args)
            {
                /* push function into param stack 
                if function saved in same way like */

                ICallable callable = script.SearchVariable(args.variableId).GetValue<ICallable>();
                if (callable.IsSharkFunction())
                {
                    funcStk.Push(((SkFunc)callable).Clone());
                }
                else
                {
                    funcStk.Push(callable);
                }
            }
            public void PushOFunction(opParams args)
            {
                /* push function in result register into function stack */

                ICallable callable = Registers[REG_RESULT].Data.GetValue<ICallable>();
                if (callable.IsSharkFunction())
                {
                    funcStk.Push(((SkFunc)callable).Clone());
                }
                else
                {
                    funcStk.Push(callable);
                }
            }
            public void PopOp(opParams args)
            {
                /* push function in result into operation stack */

                operationStk.Push(Registers[REG_RESULT].Data);
            }
            public void PopList(opParams args)
            {
                /* pop a list from paramStack and save into result */

                Registers[REG_RESULT].Data = new SkObject(paramsStk.Pop());
            }
            public void Pop(opParams args)
            {
                /* pop value from operation stack into register */

                Registers[args.position].Data = operationStk.Pop();
            }
            public void PopVar(opParams args)
            {
                /* save the data in result register into target variable */

                script.SetVariable(args.variableId, Registers[REG_RESULT].Data);
            }
            public void Index(opParams args)
            {
                /* index data */

                Registers[REG_RESULT].Data = Registers[REG_0].Data.GetValue<SkList>().Index(Registers[REG_1].Data.GetValue<SkVal>().GetInt());
            }
            public void MakeList(opParams args)
            {
                /* make an empty list */

                paramsStk.Push(new SkList());
            }
            public void Call(opParams args)
            {
                /* call function */

                ICallable callable = funcStk.Pop();
                SkList paramsList = paramsStk.Pop();
                if (callable.IsSharkFunction())
                {
                    /* call like a script */

                    script.CallFunction((SkFunc)callable, paramsList, this);
                }
                else
                {
                    Registers[REG_RESULT].Data = callable.Call(paramsList);
                }
            }
            public void Jmp(opParams args)
            {

                if (!Registers[REG_COND].Data.GetValue<SkVal>().GetBool())
                {
                    script.opJump(args.constId);
                }
            }
            public void Goto(opParams args)
            {
                /* args */

                script.opGoto(args.constId);
            }
            public void Assign(opParams args)
            {
                /* assign target variable with a constant value */

                script.SetVariable(args.variableId, script.GetConstant(args.constId));
            }
            public void Def(opParams args)
            {
                /* defination a new function */

                script.opDefination();
            }
            public void EndDef(opParams args)
            {
                /* move function to result register */

                Registers[REG_RESULT].Data = script.GetFunctionBuf();
            }
            public void AddParam(opParams args)
            {
                /* add new params */

                script.opAddParams(args.variableId);
            }
            public void AddCode(opParams args)
            {
                /* add new code */

                script.opWriteCode(args);
            }
            public void Ret(opParams args)
            {
                /* set return value */

                script.ReturnLast();
            }
            public void Constant(opParams args)
            {
                /* write constant value to script's constant list */

                script.AddConstant(SkVal.ConstFromString(args.position, args.variableId));
            }
            public void LoadNull(opParams args)
            {
                /* 在指定的寄存器中写入一个空值 */

                Registers[args.position].Data = SkObject.None;
            }
            public void PushNull(opParams args)
            {
                /* 将一个空值压入参数栈 */

                paramsStk.Peek().Add(SkObject.None);
            }
            public void Global(opParams args)
            {
                /* make function can change the value from global field */

                script.Global(args.variableId);
            }
            public void opAdd(opParams args)
            {
                /* add the data of register 0 and 1, if string type value, then combined them
                if number value, then add them, otherwise, throw type error */

                Registers[REG_RESULT].Data = SharkAPI.Add(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opSub(opParams args)
            {
                /* reg_result = r0 - r1*/

                Registers[REG_RESULT].Data = SharkAPI.Sub(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opNeg(opParams args)
            {
                /* reg_result = -r1 */

                Registers[REG_RESULT].Data = SharkAPI.Neg(Registers[REG_0].Data);
            }
            public void opMul(opParams args)
            {
                /* reg_result = r1 * r2*/

                Registers[REG_RESULT].Data = SharkAPI.Mul(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opDiv(opParams args)
            {
                /* reg_result = r1 * */

                Registers[REG_RESULT].Data = SharkAPI.Div(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opPow(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Pow(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opMod(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Mod(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opGt(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Gt(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opLt(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Lt(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opGe(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Ge(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opLe(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Le(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opEq(opParams args)
            {

                Registers[REG_RESULT].Data = new SkObject(new SkVal(Registers[REG_0].Data.Equal(Registers[REG_1].Data)));
            }
            public void opNe(opParams args)
            {

                Registers[REG_RESULT].Data = new SkObject(new SkVal(!Registers[REG_0].Data.Equal(Registers[REG_1].Data)));
            }
            public void opAnd(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.And(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opOr(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Or(Registers[REG_0].Data, Registers[REG_1].Data);
            }
            public void opNot(opParams args)
            {

                Registers[REG_RESULT].Data = SharkAPI.Not(Registers[REG_0].Data);
            }
        }
    }

    public enum SkValType
    {
        /* including int,float,string and boolean
        these data is not a object type value */

        BOOLEAN,
        INT,
        FLOAT,
        STRING
    }
    public enum SkDataType
    {
        /* include value,obj,list,func and nil */

        VALUE,
        OBJ,
        LIST,
        FUNC,
        NULL
    }
    public class SkObject
    {
        /* all data in shark would extend from this */

        public static SkObject None = new SkObject();

        public static Type[] __type2idx = new Type[]{
            typeof(SkVal),
            typeof(SkList),
            typeof(ICallable),
            typeof(object)
        };

        private object data;
        private SkDataType type;

        public SkObject(SkVal value)
        {

            type = SkDataType.VALUE;
            data = value;
        }
        public SkObject(SkList value)
        {

            type = SkDataType.LIST;
            data = value;
        }
        public SkObject(object value)
        {

            type = SkDataType.OBJ;
            data = value;
        }
        public SkObject(ICallable value)
        {

            type = SkDataType.FUNC;
            data = value;
        }
        public SkObject()
        {

            type = SkDataType.NULL;
            data = null;
        }
        public SkDataType DataType
        {

            get { return type; }
        }
        public bool Equal(SkObject other)
        {

            if (other.DataType == DataType)
            {
                if (IsNull)
                {
                    return true;
                }
                else if (IsVal)
                {
                    return GetValue<SkVal>().RawData.Equals(other.GetValue<SkVal>().RawData);
                }
                return other.data == data;
            }
            return !IsVal || other.data == data;
        }
        public T GetValue<T>()
        {
            /* cannot be null type */

            return (T)data;
        }
        public override string ToString()
        {
            if (DataType == SkDataType.VALUE)
            {
                return ((SkVal)data).ToString();
            }
            else if (DataType == SkDataType.LIST)
            {
                return ((SkList)data).ToString();
            }
            else if (DataType == SkDataType.OBJ)
            {
                return data.ToString();
            }
            else if (DataType == SkDataType.FUNC)
            {
                return ((SkFunc)data).ToString();
            }
            else
            {
                return "None";
            }
        }
        public void Rewrite<T>(T value)
        {

            int idx = Array.IndexOf(__type2idx, value.GetType());
            if (idx == -1)
            {
                throw SharkTypeError.defaultErr;
            }
            data = value;
            type = (SkDataType)idx;
        }
        public void Clear()
        {

            type = SkDataType.NULL;
            data = null;
        }
        public bool IsVal
        {

            get { return DataType == SkDataType.VALUE; }
        }
        public bool IsFunc
        {
            get { return DataType == SkDataType.FUNC; }
        }
        public bool IsObject
        {
            get { return DataType == SkDataType.OBJ; }
        }
        public bool IsNull
        {
            get { return DataType == SkDataType.NULL; }
        }
        public bool IsList
        {
            get { return DataType == SkDataType.LIST; }
        }
        public bool IsReference
        {
            /* check if target value is reference */

            get { return DataType == SkDataType.LIST || DataType == SkDataType.FUNC || DataType == SkDataType.OBJ || DataType == SkDataType.NULL; }
        }
        public SkObject CloneConstant()
        {
            /* clone a constant value */

            return new SkObject(((SkVal)data).Clone());
        }
    }

    public class SkVal
    {
        /* contains four base type value including int,float,string and boolean */

        private static Type[] __type2idx = new Type[]{
            typeof(System.Boolean),
            typeof(System.Int32),
            typeof(System.Single),
            typeof(System.String)
        };

        private SkValType type;
        private object data;

        public static SkObject ConstFromString(int typeId, string source)
        {

            TokenType tkType = (TokenType)typeId;
            switch (tkType)
            {
                case TokenType.BOOLEAN:
                    return new SkObject(new SkVal(bool.Parse(source)));
                case TokenType.INT:
                    return new SkObject(new SkVal(int.Parse(source)));
                case TokenType.FLOAT:
                    return new SkObject(new SkVal(float.Parse(source)));
                case TokenType.STRING:
                    return new SkObject(new SkVal(Regex.Unescape(source.Trim(TokenSet.varString))));
            }
            throw new SharkTypeError("未知或不支持的字面量类型");
        }

        public SkVal(object value)
        {
            /* type is int */

            int tp = Array.IndexOf(__type2idx, value.GetType());
            if (tp == -1)
            {
                throw SharkTypeError.defaultErr;
            }
            type = (SkValType)tp;
            data = value;
        }
        public object RawData
        {
            get { return data; }
            set { data = value; }
        }
        public SkVal Clone()
        {

            return new SkVal(data);
        }
        public SkValType ValType
        {
            /* type cannot be changed */

            get { return type; }
        }
        public T GetValue<T>()
        {
            /* get value from this variable */

            return (T)data;
        }
        public float GetFloat()
        {
            return Convert.ToSingle(data);
        }
        public int GetInt()
        {
            return Convert.ToInt32(data);
        }
        public bool GetBool()
        {
            return Convert.ToBoolean(data);
        }
        public override string ToString()
        {
            return data.ToString();
        }
        public bool IsText
        {
            get { return type == SkValType.STRING; }
        }
        public bool IsNumber
        {
            get { return type == SkValType.FLOAT || type == SkValType.INT; }
        }
        public bool IsInt
        {
            get { return type == SkValType.INT; }
        }
        public bool IsFloat
        {
            get { return type == SkValType.FLOAT; }
        }
        public bool IsBoolean
        {
            get { return type == SkValType.BOOLEAN; }
        }
    }

    public class SkList
    {
        /* a list type value */

        public static SkList emptyList = new SkList();
        private List<SkObject> list;
        public SkList()
        {

            list = new List<SkObject>();
        }
        public int Size
        {

            get { return list.Count; }
        }
        public void Add(SkObject element)
        {
            /*append a new node to this list*/

            list.Add(element);
        }
        public void Remove(SkObject element)
        {
            /* remove target object */

            list.Remove(element);
        }
        public bool Contains(SkObject element)
        {
            /* check if given item is in this list */

            return list.Contains(element);
        }
        public SkObject Index(int idx)
        {
            /* get the element at target position */

            if (idx >= Size)
            {
                throw new SharkIndexError();
            }
            return list[idx];
        }
        public SkObject Pop()
        {
            /* remove last element of this list */

            int idx = Size - 1;
            SkObject O = list[idx];
            list.RemoveAt(idx);
            return O;
        }
        public override string ToString()
        {
            string buf = string.Empty;
            foreach (SkObject obj in list)
            {
                buf += obj.ToString() + ",";
            }
            return $"[{buf.TrimEnd(',')}]";
        }
    }

    public interface IJumper
    {
        /* 一种跳转器 */

        void Jmp(int offset);
        void Goto(int addr);
        bool IsFunction();
    }

    public delegate SkObject __csfunc(SkList args);
    /* Shark函数的CS形态 */

    public interface ICallable
    {
        /* this object can be called */

        SkObject Call(SkList args);
        void Call(SharkInterpreter interpreter, SkList args);
        bool IsSharkFunction();
        SharkField GetSharkField();
        void ShutDown();
    }
    public struct CSFunc : ICallable
    {
        /* function packed from C# */

        __csfunc func;
        public CSFunc(__csfunc func)
        {
            this.func = func;
        }
        public SkObject Call(SkList args)
        {
            return func(args);
        }
        public void Call(SharkInterpreter interpreter, SkList args)
        {
            throw new SharkTypeError("CS风格的函数不支持使用解释器执行");
        }
        public bool IsSharkFunction()
        {
            return false;
        }
        public SharkField GetSharkField()
        {
            return null;
        }
        public void ShutDown()
        {
            throw new SharkException("CS风格的函数无法在Shark中终止");
        }
    }
    public class SkFunc : ICallable, IJumper
    {
        /* the shark function defination class*/

        public List<string> paramsList;
        public SharkField scope;
        private List<SharkCommand> opCmds;
        public int Addr { get; set; }
        public bool hasShuted;

        public SkFunc()
        {
            /* create a new function */

            paramsList = new List<string>();
            opCmds = new List<SharkCommand>();
            scope = new SharkField();
        }
        public bool IsFunction()
        {
            return true;
        }
        public void Jmp(int offset)
        {
            Addr += offset;
        }
        public void Goto(int addr)
        {
            Addr = addr;
        }
        public void WriteOperations(SharkCommand cmd)
        {
            /* add new commands to this function */

            opCmds.Add(cmd);
        }
        public bool CanSearchName(string Name)
        {
            /*检查该函数是否有目标参数*/

            return scope.Contains(Name);
        }
        public SkObject Call(SkList args)
        {
            /* call this function and return value */

            throw new SharkTypeError("Shark风格的函数不支持直接执行");
        }
        public void WriteParamsList(SkList args)
        {
            /* 向该函数中写入所有的参数 */

            for (int i = 0; i < args.Size; i++)
            {
                scope.Rewrite(paramsList[i], args.Index(i));
            }
        }
        public void Call(SharkInterpreter interpreter, SkList args)
        {
            /* 执行Shark风格的函数 */

            Addr = 0;
            hasShuted = false;
            WriteParamsList(args);
            foreach (SharkCommand cmd in IterCommands())
            {
                interpreter.RunCommand(cmd);
            }
            if (!hasShuted)
            {
                interpreter.GetRegister(3).Data = SkObject.None;
                interpreter.RunCommand(SharkCommand.Return);
            }
        }
        public IEnumerable<SharkCommand> IterCommands()
        {

            while (Addr < opCmds.Count)
            {
                yield return opCmds[Addr];
                Addr++;
            }
        }
        public void ShutDown()
        {
            Addr = opCmds.Count;
            hasShuted = true;
        }
        public bool IsSharkFunction()
        {

            return true;
        }
        public SharkField GetSharkField()
        {

            return scope;
        }
        public SkFunc Clone()
        {

            SkFunc clone = new SkFunc();
            foreach (SharkCommand cmd in opCmds)
            {
                clone.opCmds.Add(cmd);
            }
            clone.scope = scope.Clone();
            clone.paramsList = paramsList;
            return clone;
        }
    }
}