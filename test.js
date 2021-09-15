x = 100
while(x > 0){
	if(x % 2 == 0){
		print(x)
	}
	x -= 1
}




//TODO: 什么时候会产生闭包？这是核心要解决的问题
//TODO: 目前看起来比较正确的做法应该是, 当调用一个Shark函数的时候, 才开始闭包的检测。
//TODO: 当调用一个Shark函数时遇到了SET_LOCAL对象, 如果SET_LOCAL的值是一个SharkFunction的话,
//TODO: 则认为需要进行闭包的检测 
//TODO: 因为全局域中的函数定义时不存在闭包的概念

//TODO: Return关键字
//TODO: 有RETURN关键字的话，有两种情况
// 1.只有RETURN, 向堆栈中插入一个空值
// 2.RETURN一个表达式, 向堆栈中插入该表达式

//TODO: 如果没有RETURN关键字, 执行到函数的末尾, 则自动向堆栈中插入一个表达式
//TODO: 所以函数的末尾必须增加一个RET命令