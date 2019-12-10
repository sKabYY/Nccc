# Nccc

> .Net Compiler Compiler Combinator
>
> Make Making Parser Easy

一个简单易用的语法生成器。采用Parsec的编程方式实现。语法生成式的设计参考了[王垠的设计](http://www.yinwang.org/blog-cn/2013/04/21/ydiff-%E7%BB%93%E6%9E%84%E5%8C%96%E7%9A%84%E7%A8%8B%E5%BA%8F%E6%AF%94%E8%BE%83)和PEG的思想，基本上采用的S表达式的结构。

**自版本2.0.0起，Nccc使用字符级别的语法分析方法。**
一段待解析的文本首先会被转换为一个Token（记号）流。每个Token包含一个字符和该字符所在的位置。
然后语法分析器会对这个Token流进行解析。

Parsec的意思是Parser Combinators。其思想是基于一些基础parser，使用parser组合子组合成复杂的parser。

这些基础parser都是一些简单的parser，简单到非常容易编程实现。
比如匹配字符串`A`的parser（记为`A`），匹配一个空白字符的parser（记为`\space`）等。

Parser组合子类似正则表达式中的操作符。
比如将若干个parser串成序列的序列操作`@..`，或操作`@or`，星号操作（零个或多个）`@*`。

使用基础parser和组合子可以合成较为复杂的parser：

* `(@.. 'A' 'B')`匹配字符串`A`和字符串`B`序列。如`A B`。
* `(@or 'A' 'B')`匹配字符串`A`或字符串`B`。
* `(@? 'A')`匹配空或者字符串`A`。
* `(@* 'A')`匹配零个或多个`A`。如`A`、`A A A A`。
* `(@* (@or 'A' 'B'))`匹配由'A'和'B'组成的序列。如`B B`、`A A`、`A B`、`A B A A`。
* `(@or 'A' number)`匹配由'A'或者一个数字。如`A`、`1.1`。
* `(@* (@or 'A' number))`匹配由'A'和数字组成的序列。如`A A`、`1.1 2`、`A 23`、`A 1.2 A A`。

# 一个例子： 算术表达式

语法：

```
; 定义add为root parser，即最终生成的parser
:: add

; 导入内置parser
@include-builtin

; 指定词间需要过滤的空白字符
@lex-ignore spacing

;;; 词法分析

; 匹配空白字符
spacing = ~(@* \space)

; 匹配浮点数，每个词末尾需要跟上spacing来过滤空白字符。
; buitlin中已有匹配数字的parser，这里出于演示的目的另外定义了float。
float = (@? #\+ #\-) (@+ digit) (@? #\. (@+ digit)) spacing

;;; 语法分析 - 组合parser

add = (@or add:(mul '+' add)
           sub:(mul '-' add)
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
               num:float)
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

# 更多资料

[文档](https://github.com/sKabYY/Nccc)