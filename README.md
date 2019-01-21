# Nccc
.Net Compiler Compiler Combinator

# Example

一个算术表达式的例子：

```
:: add

@set-delims '(' ')' '[' ']' '{' '}' '+' '-' '*' '/' '^'

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

TODO

# Grammer Overview

Grammer文件大部分采用了S表达式的语法。

## Scanner

词法分析参数（TODO）：

```
@set-delims '(' ')' '[' ']' '{' '}' '<' '>' '`' '::' '=' ':' '~' '??'
@set-line-comment ';'
@set-comment-start '#|'
@set-comment-end '|#'
@set-operators
@set-quotation-marks '\''
@set-regex-marks '/'
@set-lisp-char
@set-significant-whitespaces

@case-sensitive on
@split-word on
```

## Parser

### Options

`@left-recur-detection on|off`
左递归检测，会稍微影响性能，但是能避免左递归导致的死循环

`@use-memorized-parser on|off`
中间结果缓存，能稍微改善Or操作的性能

### Basic Parser

`'string'`：匹配值为`string`的Token。若成功，返回空

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

`(@* p1 p2 p3 ...)`：一个或零个

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