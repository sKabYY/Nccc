# Nccc
> .Net Compiler Compiler Combinator

一个简单易用的语法生成器。采用Parsec的编程方式实现。语法生成式的设计参考了PEG的思想，但基本上采用的S表达式的结构。

Parsec的意思是Parser Combinators。其思想是基于一些基础parser，使用parser组合子组合成复杂的parser。

这些基础parser都是一些简单的parser，简单到非常容易编程实现。
比如匹配字符串`A`的parser（记为`'A'`），匹配一个数字的parser（记为`<number>`）等。

Parser组合子类似正则表达式中的操作符。
比如讲parser串成序列的序列操作`@..`，或操作`@or`，星号操作（零个或多个）`@*`。

使用基础parser和组合子可以合成较为复杂的parser：

* `(@.. 'A' 'B')`匹配字符串`A`和字符串`B`序列。如`A B`。
* `(@or 'A' 'B')`匹配字符串`A`或字符串`B`。
* `(@? 'A')`匹配空或者字符串`A`。
* `(@* 'A')`匹配零个或多个`A`。如`A`、`A A A A`。
* `(@* (@or 'A' 'B'))`匹配由'A'和'B'组成的序列。如`B B`、`A A`、`A B`、`A B A A`。
* `(@or 'A' <number>)`匹配由'A'或者一个数字。如`A`、`1.1`。
* `(@* (@or 'A' <number>))`匹配由'A'和数字组成的序列。如`A A`、`1.1 2`、`A 23`、`A 1.2 A A`。

# 一个完整的例子： 算术表达式

语法：

```
; 定义add为root parser
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

输出Parsing结果：

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

# Result Type

一个parser（跟parser或者中间的parser）分析的结果类型是`ParseResult`，其属性如下：

## Nodes

Node的List。 一个Node即一个AST。
在语法分析没有出错的情况下，分析的结果包含Node的List。

对于一个完整的语法分析来说，最终返回的Node个数一般是1个。
之所以使用List，是因为中间结果经常会有多个AST。例如`@..`、`@*`等。
并且在分析过程中，需要对中间结果做List拼接操作。

Node的结构为：

* Type：节点类型。类型由带名称的parser定义。
* Value：节点值。只有叶子节点才会有值。
* Children：子节点列表。
* Start & End：节点在文本中的起止位置。

## Success

分析结果是否成功。

## Message & Rest & FailRest

* Message：分析结果错误信息。由于有些parser是依赖错误进行的（比如`@*`、`@or`等），所以分析成功的Result里，Message也可能是有值的。对于成功的结果，忽略Message即可。
* Rest：剩下的未分析的tokens。如果分析成功，Rest里应该只有Eof。
* FailRest：最后一次分析错误的位置。

## Start & End

分析结果在文本中的起止位置

## ParserName

分析这个结果的Parser名称，对于没有名称的Parser，这个值为`Null`

# Grammer Overview

Grammer文件大部分采用了S表达式的语法。

## Scanner

Token类型（TODO)

词法分析参数（TODO）：

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

; 重要的空白符。默认情况下空白符会被忽略，除非在这里设置。例如换行符`'\n'`。
@set-significant-whitespaces

```

## Parser

一个Parser可以是一个只做一个简单匹配的基础parser，或者是由若干个parser组合而成的组合parser。

### Parser参数

`@case-sensitive on|off`
大小写敏感。默认on

`@split-word on|off`
是否将包含空白符的字符串相等匹配转换为序列匹配。例如匹配`'A B'`转换为匹配`('A' 'B')`。默认on

`@left-recur-detection on|off`
左递归检测，会稍微影响性能，但是能避免左递归导致的死循环。默认on

`@use-memorized-parser on|off`
中间结果缓存，能稍微改善@or操作的性能。默认on

### 基础parser

`'string'`：匹配值为`string`的Token。若成功，返回空

`/regex/`：匹配值正则匹配`regex`的Token

`<type>`：匹配类型为`type`的Token

`<*>`：匹配所有Token

### Variable

变量由大小写字母、数字、下划线和横杠组成，并且不能以数字开头。

```
variable = (@err'invalid var' /[a-zA-Z_\-][a-zA-Z0-9_\-]*/)
```

### Combinator

`(@.. p1 p2 p3 ...)`或直接放括号里`(p1 p2 p3 ...)`：按顺序parse

`(@+ p1 p2 p3 ...)`：一个或多个

`(@* p1 p2 p3 ...)`：零个或多个

`(@,+ sep p1 p2 p3 ...)`：以`sep`隔开的token序列

`(@,* sep p1 p2 p3 ...)`：匹配空或者以`sep`隔开的token序列

`(@or p1 p2 p3 ...)`：匹配第一个成功parse的parser

`(@! p1 p2 p3 ...)`：Not操作。若能被`(p1 p2 p3 ...)`成功parse，则失败；否则返回空

`(@? p1 p2 p3 ...)`：匹配空或者`(p1 p2 p3 ...)`

`~p`：若能被`p`成功parse，则返回空

### Named

`name:parser`：带名称的parser，匹配成功时增加一个type为`name`的节点

### Error

`(@err'string' p1 p2 p3 ...)`：匹配`(p1 p2 p3 ...)`。若失败，错误信息为`string`

### Debug

调试用的操作符

`??parser`：打印parser的parse结果

`[p1 p2 p3 ...]`：打印`(p1 p2 p3 ...)`的parse结果

# Travel AST

`Node.Match`

`DigValue`

`DigNode`

TODO
