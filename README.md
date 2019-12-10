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


# Parser的返回结果类型：ParseResult

所有parser（root parser或者中间的parser）分析的结果类型是`ParseResult`。`ParseResult`主要提供三个信息：

1. 分析是否成功

    分析失败是有意义的。即使最终分析是成功的，中间的parser也可能会有多次分析失败。
    
    比如`@or`操作符要尝试多个parser直至分析成功，`@*`操作符则要重复解析一个文本直到解析失败。

2. 抽象语法数（AST）

    一个parser会返回AST的根节点Node的列表，这个列表可能包含一个或多个Node，也可能为空。

	Node主要属性有1) 节点类型`Type`，2) 节点值`Value`和3) 子节点列表`Children`。

	操作符`@..`、`@*`等组合的parser会拼接被操作parser返回的Node列表，产生多个Node的结果。

	操作符`{name}:`组合的parser则会返回一个`Type`属性值为`{name}`的节点、其子节点为被操作parser返回的Node列表。

	注意，分析成功也可能会返回空列表。比如解析关键字parser（一般是字符串匹配parser）。
	
	另外，操作符`~`可以“吞掉”被操作parser成功返回的AST。

	解析失败的情况下，AST会尽量返回已解析的部分。不过此时已解析部分仅供调试参考，不保证正确性。

3. Parse过程消耗了多少Token/剩余的Token流

	一次parse过程会消耗零个或多个Token，所以parser需要返回剩余的Token流，作为后续的parser解析的开始位置。
	
	有一些特殊的parser不会消耗Token。比如`@!`操作符组合的parser仅做检查，不消耗Token。

	对于root parser，剩余的Token流应该只有EOF。

## `ParseResult`的属性具体说明

### Nodes

Node的列表。 一个Node即一个AST的根节点。
在语法分析没有出错的情况下，分析的结果包含Node的列表。

对于一个完整的语法分析来说，最终返回的Node个数一般是1个。
之所以使用列表，是因为中间结果经常会有一个parser返回多个Node的情况。例如使用操作符`@..`或操作符`@*`组合的parser。
并且在分析过程中，需要对中间结果做列表拼接操作。

Node的结构为：

* `Type`：节点类型。类型由带名称的parser定义。
* `Value`：节点字符值。只有叶子节点才会有值。
* `Children`：子节点列表。
* `Start` & `End`：节点在文本中的起止位置。

Node的取值方法：

* `string StringValue()`：如果该Node的所有子节点都是叶子节点，那么返回所有叶子节点包含的字符拼接而成的字符串；否则抛出`NodeMethodException`异常。
* `bool TryGetStringValue(out string value)`：`StringValue`方法的无异常版本。
* `public static string ConcatValue(IList<Node> nodes)`：如果`nodes`都是叶子节点，那么返回`nodes`包含的字符拼接而成的字符串；否则抛出`NodeMethodException`异常。

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
2. 参数配置[可选]：语法分析参数和国际化参数。
3. 各个parser的定义：使用基础parser和组合子来定义各种复杂parser。

## Grammer文件中的注释

* 单行注释：';'
* 块注释：'#|'开始，'|#'结束

## 词法分析

Nccc使用了字符级别的语法分析方法。所以词法分析的规则写法与语法分析相同。

一般来说，词法分析需要定义以下匹配规则：

* `comment`：匹配注释。
* `spacing`：匹配语法分析不需要的字符串。通常为空白字符和注释。如`spacing = ~(@* (@or \space comment))`。
* 推荐配置`@lex-ignore spacing`。
* 匹配词的parser应该以`spacing`结尾。如`float = (@? #\+ #\-) (@+ digit) (@? #\. (@+ digit)) spacing`。

## 语法分析

一个parser可以是一个只做一个简单匹配的基础parser，或者是由若干个parser组合而成的组合parser。

### 语法分析参数

`@case-sensitive on|off`
大小写敏感。默认on。

`@split-word on|off`
是否将包含空白符的字符串相等匹配转换为序列匹配。例如匹配`'A B'`转换为匹配`('A' 'B')`。默认on。字符模式下此参数无意义。

`@lex-ignore {variable}`
`{variable}`为后面定义的一个parser的变量名。此配置用于指定在词法分析中需要被忽略的Token，一般是空白字符和注释。

有几个地方会用到这个parser来忽略匹配的Token：
1. 文本开头；
2. 字符串匹配`'{string}'`的结尾；
3. 一些内置的词法级别的parser（如`number`）。

默认为空白字符串。

`@lex-mode`
关掉`@lex-ignore`设定过滤功能。不忽略任何字符。这个选项不能和`@lex-ignore`一起使用，否则会产生不确定的行为。

`@include-builtin`
引入内置的parser。目前内置的parser有：
* `alpha`: 匹配一个大写或者小写字母。
* `digit`: 匹配一个0~9的数字。
* `number`: 匹配一个数字，包括整数、浮点数或科学计数法表示的数字。然后过滤紧接其后，匹配`@lex-ignore`指定的parser的字符。


### 定义parser

**`{variable} = parser`：定义变量`{variable}`为parser。**

变量由大小写字母、数字、下划线和横杠组成，并且不能以数字开头。

用Nccc的语法表示变量命名规则如下：

```
var-start = (@or #\_ #\- alpha)
variable = (@err'invalid var' var-start (@* (@or var-start digit))) spacing
```

变量定义是可递归的，即引用可写在前面，定义写在后面。**注意不支持左递归。**
重复定义会产生不确定的行为。

> 变量定义能力比想象中的重要。可递归的变量定义是语法生成式匹配能力超过正则表达式的根本原因。

**`{variable} = p1 p2 p3 ...`：`{variable} = (@.. p1 p2 p3 ...)`的缩写。**

### 基础parser

`\{char}`：匹配并消耗值为`{char}`的Token。返回空。

`#\{char}`：匹配并消耗值为`{char}`的Token。返回值为`{char}`的叶子节点。

`'{string}'`：匹配并消耗拼接为字符串后值为`{string}`的多个Token，然后过滤紧接其后，匹配`@lex-ignore`指定的parser的字符。返回空。

`#'{string}'`：匹配并消耗拼接为字符串后值为`{string}`的多个Token，然后过滤紧接其后，匹配`@lex-ignore`指定的parser的字符。返回组成这串字符串的叶子节点列表，其中每个节点包含一个字符。

`<*>`：匹配并消耗下一个Token，返回具有该Token值的叶子节点

### 组合子

`(@.. p1 p2 p3 ...)`或直接放括号里`(p1 p2 p3 ...)`：按顺序匹配，拼接返回结果（下面没特殊说明的默认都是拼接返回结果）

`(@+ p1 p2 p3 ...)`：一个或多个`(p1 p2 p3 ...)`

`(@* p1 p2 p3 ...)`：零个或多个`(p1 p2 p3 ...)`

`(@,+ sep p1 p2 p3 ...)`：以`sep`隔开的匹配`(p1 p2 p3 ...)`的序列

比如`(@,+ ',' 'A' number)`匹配`A 1.1 , A 2`。

`(@,* sep p1 p2 p3 ...)`：匹配空或者以`sep`隔开的匹配`(p1 p2 p3 ...)`的序列

`(@or p1 p2 p3 ...)`：匹配第一个成功匹配的结果

`(@! p1 p2 p3 ...)`：Not操作。若`(p1 p2 p3 ...)`分析成功，则匹配失败；否则匹配成功，返回空并且不消耗任何Token

`(@? p1 p2 p3 ...)`：匹配空或者`(p1 p2 p3 ...)`

`~p`：若能被`p`成功parse，则返回空

`[n] p`：连续`n`次匹配`p`

### Named

`{name}:parser`：带名称的parser，匹配成功时增加一个Type为`{name}`的节点，并且parser的匹配结果都作为该节点的子节点。

只有这个操作会使AST增长一层。

### Error

`(@err'string' p1 p2 p3 ...)`：自定义错误信息。匹配`(p1 p2 p3 ...)`。若失败，错误信息为`string`。

例如：
`(@err'不能出现数字' (@! digit) <*>)`

### Debug

调试用的操作符。

`??parser`：打印parser的parse结果

`?[p1 p2 p3 ...]`：打印`(p1 p2 p3 ...)`的parse结果

## 加载grammer

`NcParser.Load`方法使用字符串加载grammer：

```
var parser = NcParser.Load("::root\nroot = 'A'");
```

另外也提供了从嵌入的资源文件加载grammer的方法：

```
var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Calculator.calculator.grammer");
```

## 国际化

语言需要在加载grammer的时候在初始化方法里指定：

```
var parser = NcParser.LoadFromAssembly(Assembly.GetExecutingAssembly(), "Nccc.Tests.Json.json.grammer", settings =>
{
    settings.Locale.Language = "zh-cn";
});
```

Nccc默认配置了`"zh-cn"`语言的内置错误信息。如果需要自定义国际化字符串，可在初始化方法中设置：

```
settings.Locale.Language = "zh-cn";
settings.Locale.Set("zh-cn", new Dictionary<string, string>
{
    { "expect", "盼望着" }
});
```

自定义错误信息默认不做国际化处理。
若需要国际化自定义错误信息，需要使用国际化参数`@set-message-locale-start`和`@set-message-locale-end`来指明`@err`操作符自定义的错误信息中需要国际化的字符串。

例如：

```
public const string grammer = @"
:: root

; 'L{'和'}'包围的字符串会被国际化
@set-message-locale-start 'L{'
@set-message-locale-end '}'

root = (@err'L{expect} A L{or} B' oo:(@or 'A' 'B'))
";
[TestMethod]
public void TestMessageLocale()
{
    var parser = NcParser.Load(grammer, settings =>
    {
        settings.Locale.Language = "zh-cn";
        settings.Locale.Set("zh-cn", new Dictionary<string, string>
        {
            { "expect", "盼望着" },
            { "or", "或" },
        });
    });
    var source = "C";
    var result = parser.ScanAndParse(source);
    Console.WriteLine(result.ToSExp().ToPrettyString());
    Assert.IsFalse(result.IsSuccess());
    Assert.AreEqual("盼望着 A 或 B", result.Message);
}
```

# 遍历/访问AST

直接访问Node的属性非常不方便，Nccc提供了一些方便的方法来帮助获取Node中的数据。

## Match方法

### Node的Match方法

Match方法会根据`node`的类型执行响应的操作（其中`nodes`为`node.Children`）：
```
node.Match(type =>
{
	type(类型1, nodes => stm1 with nodes);
	type(类型2, nodes => stm2 with nodes);
	type(类型3, nodes => stm3 with nodes);
	...
});
```

### 静态Match方法

静态Match方法遍历多个Node：
```
Node.Match<T>(ns, type =>
{
	type(类型1, nodes => stm1 with nodes);
	type(类型2, nodes => stm1 with nodes);
	type(类型3, nodes => stm1 with nodes);
	...
});  // returns value with type T
```

相当于
```
foreach (var n in ns)
{
	n.Match(type =>
	{
		type(类型1, nodes => stm1 with nodes);
		type(类型2, nodes => stm1 with nodes);
		type(类型3, nodes => stm1 with nodes);
		...
	});
}
```

### 带返回值的Match方法

```
node.Match<T>(type =>
{
	type(类型1, nodes => exp1(type T) with nodes);
	type(类型2, nodes => exp2(type T) with nodes);
	type(类型3, nodes => exp3(type T) with nodes);
	...
});  // returns value with type T
```

### 还是计算器的例子

使用Match方法实现计算器的解释器：

```
private double _Calc(string exp)
{
    var pr = _parser.ScanAndParse(exp);
    if (!pr.IsSuccess())
    {
        throw new ArgumentException($"Parsing fail: {pr.ToSExp().ToPrettyString()}");
    }
    return _ValueOf(pr.Nodes.First());
}

private double _ValueOf(Node node)
{
    return node.Match<double>(type =>
    {
        type("par", es => _ValueOf(es[0]));
        type("add", es => _ValueOf(es[0]) + _ValueOf(es[1]));
        type("sub", es => _ValueOf(es[0]) - _ValueOf(es[1]));
        type("mul", es => _ValueOf(es[0]) * _ValueOf(es[1]));
        type("div", es => _ValueOf(es[0]) / _ValueOf(es[1]));
        type("pow", es => Math.Pow(_ValueOf(es[0]), _ValueOf(es[1])));
        type("neg", es => -_ValueOf(es[0]));
        type("num", es => double.Parse(es[0].Value));
    });
}

[TestMethod]
public void TestCalc()
{
    var x = _Calc("(5.1+2)*3+-2^3^2");
    var x0 = (5.1 + 2) * 3 - Math.Pow(2, Math.Pow(3, 2));
    Console.WriteLine(x);
    Assert.AreEqual(x0, x);
}
```

## Dig方法

Dig方法用于按照路径从AST取特定的Node或者值。路径必须指定到唯一一个节点。

`Node.DigNode`：返回路径指定的节点

`Node.DigValue`：如果路径指定的节点的所有子节点是叶子节点，则返回`node.StringValue()`；否则抛出`NodeMethodException`异常。

仍然是计算器的例子，从语法分析的结果中取节点/值：
```
var pr = _parser.ScanAndParse("(5.1+2)*3+-2^3^2");
var node = Node.DigNode(pr.Nodes, "add", "mul");
Assert.AreEqual("mul", node.Type);
var value = Node.DigValue(pr.Nodes, "add", "neg", "pow", "num");
Assert.AreEqual("2", value);
```


# 自举

[Nccc/Bootstrapping/nccc.grammer](Nccc/Bootstrapping/nccc.grammer)描述了Grammer文件的语法。

[Nccc.Tests/Bootstrapping/BootstrappingTests.cs](Nccc.Tests/Bootstrapping/BootstrappingTests.cs)里面的测试用例用上面的Grammer文件的语法生成了自己……
