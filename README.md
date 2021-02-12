# Shark
## 1.关于Shark的简单FAQ

### 1.什么是Shark？

Shark是一门用C#编写的十分简易的脚本语言，拥有最基本的逻辑处理功能。

### 2.Shark依赖什么？

除了.NET的标准库，Shark没有任何依赖，兼容Core2.0和Framework4.x

### 3.Shark主要用于做什么？

Shark只是提供了一种文本文件和宿主程序交流的方式。因而严格来说没有具体的用途，通常用于作为插件，或者一些辅助性功能。

### 4.如何使用Shark

Shark所有的逻辑性代码都在Shark.cs文件中，整个Shark仅有3500行代码，直接将这个文件作为源码插入到目标项目中即可。如果有必要，可以将源码编译成托管dll，具体看情况。具体代码上如何对Shark进行控制，请继续浏览下面的文档。

## 2.Shark脚本的语法

Shark没有太多特殊的语法，和JS有点类似，但是它并不支持面向对象，也不支持点操作符。主要用于完成一些基础的逻辑处理功能。

### 2.1 关于变量

在Shark中定义一个变量类似于Python，不需要定义类型。

```
x = "hello world";		//始终有分号
```

Shark的变量类型包括下面这些。

| 类型     | 对应的C#类型 | 解释                     |
| -------- | ------------ | ------------------------ |
| SkObject | Object       | 对象类型                 |
| SkList   | List         | 链表类型                 |
| SkFunc   | 无对应类型   | 函数类型                 |
| CSFunc   | Delegate     | 函数类型（C#的委托类型） |
| SkVal    | 无对应类型   | 值类型                   |
| SkNull   | 无对应类型   | 空类型                   |

Shark可以使用的值类型包括，整型，浮点型，布尔型和字符串型。对浮点型的判断暂时不支持使用科学计数法表示。即不支持在数字后用e表示数量级。下面是创建变量的一些基础操作。

```
x = "hello world";
y = 10;
z = 20;
w = y + z;
```

任何在全局域创建的变量都被视为一个全局变量。

### 2.2 关于运算符

Shark的运算符差不多和C#一致，以下是它拥有的所有的运算符。

#### 2.2.1 数学运算符

| 符号 | 作用                           | 备注 |
| ---- | ------------------------------ | ---- |
| +    | 加法运算                       |      |
| -    | 减法运算（也可以理解为负运算） |      |
| *    | 乘法运算                       |      |
| /    | 除法运算                       |      |
| ^    | 幂运算                         |      |
| %    | 取余运算                       |      |

#### 2.2.2 比较运算符

和C#一致

| 符号 | 作用     | 备注 |
| ---- | -------- | ---- |
| >    | 大于     |      |
| <    | 小于     |      |
| >=   | 大于等于 |      |
| <=   | 小于等于 |      |
| !=   | 不等于   |      |
| ==   | 等于     |      |

#### 2.2.3 逻辑运算符

Shark提供了标准的逻辑运算，（三种）

| 符号 | 含义   | 备注 |
| ---- | ------ | ---- |
| &&   | 且运算 |      |
| \|\| | 或运算 |      |
| !    | 非运算 |      |

### 2.3 关于函数

函数对象在Shark中有两种不同的类型，由于Shark的所有功能都来源于宿主程序，所以有一部分函数是宿主提供的函数，另外一种则是在Shark中定义的函数。一般来说，它们会被Shark解释器一视同仁，除非尝试一些不存在的操作，你可以查看Shark定义的函数的中间代码，但是无法查看宿主程序的中间代码，因为它们直属于C#。

#### 2.3.1 定义函数

函数有两种定义的方式，一种是直接定义，一种是匿名定义。

**直接定义**

直接定义同JS差不多，在函数定义的末尾处并不需要加分号。

```
function MyFunc(){}
```

**匿名定义**

```
MyFunc = function(){};
```

上面两段代码是等价的，它们都会定义一个新的函数。注意当使用匿名定义的时候，后面需要加分号。Shark没有任何的系统函数（暂时没有，随着升级和维护，会慢慢增加）

#### 2.3.2 关于@操作符

@是一个特殊的操作符，其作用类似于Python的Global关键字，用于在局部引入全局变量的值。

```
x = 10;
function test(){

    x = 0;
}
print(x);//结果为10
```

上面的这个在test函数中的操作，会被认为是在函数中定义一个局部变量，而非覆写全局变量x的值。那么为了改写全局变量的值，我们需要通过@操作符将其引入局部。

```
x = 10;
function test(){
    @x;
    x = 0;
}
print(x);//结果为0
```

## 3.Shark如何与C#沟通

Shark和C#的沟通有几个主要的关键字，创建虚拟机，读取脚本，类型转换，逆向绑定API。下面的案例用于解释如何进行这些操作。

### 3.1 创建Shark虚拟机

Shark虚拟机是用于解释Shark脚本的对象，当宿主程序启动的时候，应当创建一个Shark虚拟机作为全局对象。

```c#
using System;
using Shark.SharkCodeLexer;
using Shark.SharkIL;
using Shark.SharkVirtualMachine;

namespace Program{
	class Program{
        
        static void Main(string[] args){
            
            SharkInterpreter interpreter = new SharkInterpreter();
            //初始化Shark虚拟机
        }
    }
}
```

### 3.2 Shark脚本

Shark提供了一个SharkScript对象，该对象拥有两个主要的API，ReadFile和ReadText，通过它们可以来读取Shark脚本。它们会根据你给定的Shark脚本内容来返回一个SharkScript对象，之后可以直接通过SharkInterpreter的RunScript函数来执行脚本。如下所示，非常简单。

```c#
using System;
using Shark.SharkCodeLexer;
using Shark.SharkIL;
using Shark.SharkVirtualMachine;

namespace Program{
	class Program{
        
        static void Main(string[] args){
            
            SharkInterpreter interpreter = new SharkInterpreter();
            //初始化Shark虚拟机
            
            SharkScript MyScript = SharkScript.ReadText("x = 10;print(x);");
            //读取源代码，并返回一个Script对象
            
            interpreter.RunScript(MyScript);
            //执行Shark脚本
        }
    }
}
```

### 3.3 如何为Shark提供API

说白了，Shark本身没有任何功能，如何用C#编写一些Shark可以访问的API呢？此时就用到了CSFunc，在变量类型中，SkFunc和CSFunc都是可执行对象，但是一者是用C#定义的，一者是Shark自定义的。

另外就是，CSFunc也属于一个变量，所以直接将其赋值给脚本对象，就可以为其增加这个API。所以我们只需要知道怎样创建一个CSFunc即可。

```c#
public delegate SkObject __csfunc(SkList args);
```

这就是CSFunc的原型，它的返回值是一个SkObject类型，它的参数列表是由SkList来存储的。所以只要编写一个符合该条件的C#函数就可以被Shark执行。

```c#
using System;
using Shark.SharkCodeLexer;
using Shark.SharkIL;
using Shark.SharkVirtualMachine;

namespace Program{
	class Program{
        static void Main(string[] args){
            
            SharkInterpreter interpreter = new SharkInterpreter();
            //初始化Shark虚拟机
            
            SharkScript MyScript = SharkScript.ReadText("x = 10;print(x);");
            //读取源代码，并返回一个Script对象
            
            SkObject testFunc = new SkObject(new CSFunc(SharkPrint));
            MyScript.SetVariable("print",testFunc);
            //将我们编写的函数转换为一个SkObject对象，然后赋值给脚本的print变量
            //此后print就是代表该函数，直到print关键字被重新赋值。
        }
        public static SkObject SkPrint(SkList args){

            SkObject Obj = args.Index(0);
            if(Obj.IsVal){
                Console.WriteLine(Obj.GetValue<SkVal>().ToString()); 
            }else{
                Console.WriteLine(Obj.ToString());
            }
            return SkObject.None;
        }

    }
}
```

于是我们编写了一个静态函数，（当然，不一定要是静态的）。接着我们可以将SkPrint。观察这个函数，args是一个SkList类型，我们可以通过它的Index函数来访问第N个元素。当然，我们自己要事先知道这个API的参数规范。比如SkPrint是这样一个函数，输出参数列表的第一个对象。

```c#
using System;
using Shark.SharkCodeLexer;
using Shark.SharkIL;
using Shark.SharkVirtualMachine;

namespace Program{
	class Program{
        static void Main(string[] args){
            
            SharkInterpreter interpreter = new SharkInterpreter();
            //初始化Shark虚拟机
            
            SharkScript MyScript = SharkScript.ReadText("x = 10;print(x);");
            //读取源代码，并返回一个Script对象
            
            SkObject testFunc = new SkObject(new CSFunc(SharkPrint));
            MyScript.SetVariable("print",testFunc);
            //将我们编写的函数转换为一个SkObject对象，然后赋值给脚本的print变量
            //此后print变量就是该函数，直到print关键字被重新赋值。
            
            interpreter.RunScript(MyScript);
            //执行Shark脚本，
            //输出10
        }
        public static SkObject SkPrint(SkList args){

            SkObject Obj = args.Index(0);
            if(Obj.IsVal){
                Console.WriteLine(Obj.GetValue<SkVal>().ToString()); 
            }else{
                Console.WriteLine(Obj.ToString());
            }
            return SkObject.None;
        }

    }
}
```

### 3.4 类型转换

在Shark和C#的沟通中，主要就是如何进行C#和Shark变量类型的转换。那么Shark提供了一些简单的API，用于在两者之间交替。

| 函数                 | 参数类型 | 返回值类型    | 作用                                                         |
| -------------------- | -------- | ------------- | ------------------------------------------------------------ |
| SharkAPI.GetCBool    | SkObject | bool          | 将目标一个SkObject对象（即一个Shark变量）转换为C#的逻辑类型，如果对象类型错误，那么该函数会报错。 |
| SharkAPI.GetCInt     | SkObject | System.Int32  | 将Shark变量转换为C#的Int类型值                               |
| SharkAPI.GetCFloat   | SkObject | System.Single | 将Shark变量转换为C#的Float类型值                             |
| SharkAPI.GetCString  | SkObject | System.String | 将Shark变量转换为C#的string类型                              |
| SharkAPI.GetSkBool   | bool     | SkObject      | 将C#的bool类型转换为SkObject类型                             |
| SharkAPI.GetSkInt    | int      | SkObject      | 将C#的int类型转换为SkObject类型                              |
| SharkAPI.GetSkFloat  | float    | SkObject      | 将C#的float类型转换为SkObject类型                            |
| SharkAPI.GetSkString | string   | SkObject      | 将C#的string类型转换为SkObject类型                           |

有了以上函数之后，我们就可以在编写Shark可使用的API时，将Shark脚本中的变量转换为C#类型，然后进行处理。