# Nccc
.Net Compiler Compiler Combinator

# Example

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

# Result Type

# Grammer Overview

# Scanner

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

# Parser

### Options

@left-recur-detection on|off
左递归检测，会稍微影响性能，但是能排除

@use-memorized-parser on|off
中间结果缓存，能稍微改善Or操作的性能

### Basic Parser

'string'
<type>
<*>

### Variable
variable = (@err'invalid var' /[a-zA-Z_\-][a-zA-Z0-9_\-]*/)

### Combinator

seq-cmb:'@..'  ()

plus-cmb:'@+'
star-cmb:'@*'
join-cmb:'@,*'
join-plus-cmb:'@,+'
or-cmb:'@or'
not-cmb:'@!'
maybe-cmb:'@?'
glob-exp '~'

### Named

named-exp = variable ':' exp

### Error

err-cmb:'@err

### Debug

dbg-1exp = '??' exp
dbg-exp = '[' (@+ exp) ']'

# Travel AST