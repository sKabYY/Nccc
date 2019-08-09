# Nccc
> .Net Compiler Compiler Combinator

一个简单易用的语法生成器。采用Parsec的编程方式实现。语法生成式的设计参考了[王垠的设计](http://www.yinwang.org/blog-cn/2013/04/21/ydiff-%E7%BB%93%E6%9E%84%E5%8C%96%E7%9A%84%E7%A8%8B%E5%BA%8F%E6%AF%94%E8%BE%83)和PEG的思想，基本上采用的S表达式的结构。

Parsec的意思是Parser Combinators。其思想是基于一些基础parser，使用parser组合子组合成复杂的parser。

这些基础parser都是一些简单的parser，简单到非常容易编程实现。
比如匹配字符串`A`的parser（记为`'A'`），匹配一个数字的parser（记为`<number>`）等。

Parser组合子类似正则表达式中的操作符。
比如将parser串成序列的序列操作`@..`，或操作`@or`，星号操作（零个或多个）`@*`。

使用基础parser和组合子可以合成较为复杂的parser：

* `(@.. 'A' 'B')`匹配字符串`A`和字符串`B`序列。如`A B`。
* `(@or 'A' 'B')`匹配字符串`A`或字符串`B`。
* `(@? 'A')`匹配空或者字符串`A`。
* `(@* 'A')`匹配零个或多个`A`。如`A`、`A A A A`。
* `(@* (@or 'A' 'B'))`匹配由'A'和'B'组成的序列。如`B B`、`A A`、`A B`、`A B A A`。
* `(@or 'A' <number>)`匹配由'A'或者一个数字。如`A`、`1.1`。
* `(@* (@or 'A' <number>))`匹配由'A'和数字组成的序列。如`A A`、`1.1 2`、`A 23`、`A 1.2 A A`。

# 一个例子： 算术表达式

语法：

```
; 定义add为root parser，即最后得到的parser
:: add

; 设置参数
@set-delims '(' ')' '[' ']' '{' '}' '+' '-' '*' '/' '^'

; 下面开始组合

add = (@or add:(mul '+' add)
           sub:(mul'-' add)
           mul)

mul = (@or mul:(pow '*' mul)
           div:(pow '/' mul)
           pow)

pow = (@or pow:(primary '^' pow)
           primary)

primary = (@or par:('(' add ')')
               par:('[' add ']')
               par:('{' add '}')
               neg:('-' pow)
               num:<number>)
```

加载Parser：

```
private NcParser _parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Calculator.calculator.grammer");
```

Parse：

```
var pr = _parser.ScanAndParse("(5.1+2)*3+-2^3^2");
Console.WriteLine(pr.ToSExp().ToPrettyString());
```

输出Parsing结果（打印成S表达式）：

```
(((parser add))
 (success? True)
 (nodes
  ((add[(1,1)-(1,17)]
    (mul[(1,1)-(1,10)]
     (par[(1,1)-(1,8)]
      (add[(1,2)-(1,7)]
       (num[(1,2)-(1,5)] 5.1)
       (num[(1,6)-(1,7)] 2)))
     (num[(1,9)-(1,10)] 3))
    (neg[(1,11)-(1,17)]
     (pow[(1,12)-(1,17)]
      (num[(1,12)-(1,13)] 2)
      (pow[(1,14)-(1,17)]
       (num[(1,14)-(1,15)] 3)
       (num[(1,16)-(1,17)] 2)))))))
```

# ParseResult Type

所有parser（root parser或者中间的parser）分析的结果类型是`ParseResult`。`ParseResult`主要提供三个信息：

1. 分析是否成功

    分析失败也是有意义的。即使最终分析是成功的，中间的parser也可能会有多次分析失败。
    
    比如`@or`操作符要尝试多个parser直至分析成功，`@*`操作符则要重复解析一个文本直到解析失败。

2. 抽象语法数（AST）

    一个parser会返回AST的根节点Node的列表，这个列表可能包含一个或多个Node，也可能为空。

	Node主要属性有1) 节点类型`Type`，2) 节点值`Value`和3) 子节点列表`Children`。

	操作符`@..`、`@*`等组合的parser会拼接被操作parser返回的Node列表，产生多个Node的结果。

	操作符`{name}:`组合的parser则会返回一个`Type`属性值为`{name}`的节点、其子节点为被操作parser返回的Node列表。

	注意，分析成功也可能会返回空列表。比如解析关键字parser（一般是字符串匹配parser）。
	
	另外，操作符`~`可以“吞掉”被操作parser成功返回的AST。

3. 剩余的Token流

	parser需要返回剩余的Token，以供后续的parser继续分析。
	
	有一些特殊的parser不会消耗Token。比如`@!`操作符组合的parser仅做检查，不消耗Token。

	对于root parser，剩余的Token流应该只有Eof。

## `ParseResult`的属性具体说明

### Nodes

Node的列表。 一个Node即一个AST的根节点。
在语法分析没有出错的情况下，分析的结果包含Node的列表。

对于一个完整的语法分析来说，最终返回的Node个数一般是1个。
之所以使用列表，是因为中间结果经常会有一个parser返回多个Node的情况。例如使用操作符`@..`或操作符`@*`组合的parser。
并且在分析过程中，需要对中间结果做列表拼接操作。

Node的结构为：

* `Type`：节点类型。类型由带名称的parser定义。
* `Value`：节点值。只有叶子节点才会有值。
* `Children`：子节点列表。
* `Start` & `End`：节点在文本中的起止位置。

### Success

分析结果是否成功。

### Message & Rest & FailRest

* `Message`：分析结果错误信息。由于有些parser是依赖错误进行的（比如`@*`、`@or`等），所以分析成功的结果里，`Message`也可能是有值的。对于成功的结果，忽略`Message`即可。
* `Rest`：剩下的未分析的Tokens。如果分析成功，Rest里应该只有Eof。
* `FailRest`：最后一次分析错误的位置。同`Message`，分析成功的结果里`FailRest`也可能包含多于Eof的内容。

### Start & End

分析结果在文本中的起止位置

### ParserName

分析这个结果的parser名称，对于没有名称的parser，这个值为`Null`。

# Grammer的写法

Grammer文件大部分采用了S表达式的语法。

Grammer文件分为三个部分（必须按顺序写）：

1. Root parser的声明：`::{parser}`，声明`{parser}`为root parser。`{parser}`的定义写在第三部分。
2. 参数配置[可选]：词法分析参数和语法分析参数。
3. 各个parser的定义：使用基础parser和组合子来定义各种复杂parser。

## 词法分析

不同于大部分教程使用正则表达式的技术实现的词法分析，Nccc直接硬编码实现，因此只能生成固定的几种Token（但也够用）。

后续可能会使用字符级别的语法分析替代词法分析。

### Token类型：

* Eof：结束标记。
* Comment：注释。
* Newline：换行。设置了`@set-significant-whitespace`才会生成这个类型的Token
* Regex：正则。
* Str：字符串。
* Number：数字。
* Token：其他类型，一般是关键字、操作符等。

### 词法分析参数（TODO）：

```
; 分隔符列表。词法分析器遇到这些符号生成一个词。
@set-delims '(' ')' '[' ']' '{' '}' '<' '>' '`' '::' '=' ':' '~' '??'

; 行注释标记。
@set-line-comment ';'

; 块注释开始标记
@set-comment-start '#|'

; 块注释结束标记
@set-comment-end '|#'

; 操作符。目前和delims作用相同
@set-operators

; 字符串开始结束标记字符
@set-quotation-marks '\''

; 正则开始结束标记字符
@set-regex-marks '/'

; 预留参数，暂未用到
@set-lisp-char

; 匹配数字类型的正则表达式
@set-number-regex  '([+-]?\\d+(\\.\\d+)?([Ee]-?\\d+)?)'

; 重要的空白符。默认情况下空白符会被忽略，除非在这里设置。例如换行符`'\n'`。
@set-significant-whitespaces

```

## 语法分析

一个Parser可以是一个只做一个简单匹配的基础parser，或者是由若干个parser组合而成的组合parser。

### 语法分析参数

`@case-sensitive on|off`
大小写敏感。默认on

`@split-word on|off`
是否将包含空白符的字符串相等匹配转换为序列匹配。例如匹配`'A B'`转换为匹配`('A' 'B')`。默认on

`@left-recur-detection on|off`
左递归检测，会稍微影响性能，但是能避免左递归导致的死循环。默认on

`@use-memorized-parser on|off`
中间结果缓存，能稍微改善@or操作的性能。默认on

### 定义parser

`{variable} = parser`：定义变量`{variable}`为parser。

变量由大小写字母、数字、下划线和横杠组成，并且不能以数字开头。

```
variable = (@err'invalid var' /[a-zA-Z_\-][a-zA-Z0-9_\-]*/)
```

变量定义是可递归的，即引用可写在前面，定义写在后面。重复定义会产生不确定的行为。

`{variable} = p1 p2 p3 ...`：`{variable} = (@.. p1 p2 p3 ...)`的缩写。

> 变量定义能力比想象中的重要。可递归的变量定义是语法生成式匹配能力超过正则表达式的根本原因。

### 基础parser

`'{string}'`：匹配值为`{string}`的Token。返回空。

`/{regex}/`：匹配值正则匹配`{regex}`的Token，返回具有Token值的叶子节点。

`<type>`：匹配类型为`type`的Token，返回具有Token值的叶子节点。

`<*>`：匹配所有Token，返回具有Token值的叶子节点

### 组合子

`(@.. p1 p2 p3 ...)`或直接放括号里`(p1 p2 p3 ...)`：按顺序匹配，拼接返回结果（下面没特殊说明的默认都是拼接返回结果）

`(@+ p1 p2 p3 ...)`：一个或多个`(p1 p2 p3 ...)`

`(@* p1 p2 p3 ...)`：零个或多个`(p1 p2 p3 ...)`

`(@,+ sep p1 p2 p3 ...)`：以`sep`隔开的匹配`(p1 p2 p3 ...)`的序列

比如`(@,+ ',' 'A' <number>)`匹配`A 1.1 , A 2`。

`(@,* sep p1 p2 p3 ...)`：匹配空或者以`sep`隔开的匹配`(p1 p2 p3 ...)`的序列

`(@or p1 p2 p3 ...)`：匹配第一个成功匹配的结果

`(@! p1 p2 p3 ...)`：Not操作。若`(p1 p2 p3 ...)`分析成功，则匹配失败；否则匹配成功，返回空并且不消耗任何Token

`(@? p1 p2 p3 ...)`：匹配空或者`(p1 p2 p3 ...)`

`~p`：若能被`p`成功parse，则返回空

### Named

`{name}:parser`：带名称的parser，匹配成功时增加一个Type为`{name}`的节点。

只有这个操作会使AST增长一层。

### Error

`(@err'string' p1 p2 p3 ...)`：自定义错误信息。匹配`(p1 p2 p3 ...)`。若失败，错误信息为`string`

### Debug

调试用的操作符

`??parser`：打印parser的parse结果

`[p1 p2 p3 ...]`：打印`(p1 p2 p3 ...)`的parse结果

# Travel AST

`Node.Match`

`DigValue`

`DigNode`

TODO
