grammar Jerry;

WHITESPACE: [ \n\t\r]+ -> channel(1);

COMMENT: '#' ~ [\r\n]* -> skip;

FN: 'fn';
LET: 'let';
STRUCT: 'struct';
INIT: 'jerry';

TRUE: 'true';
FALSE: 'false';

EQUAL: '=';
PLUS: '+';
MINUS: '-';
MULTIPLY: '*';

COLON: ':';
SEMI: ';';
COMMA: ',';

OPEN_PAREN: '(';
CLOSED_PAREN: ')';
OPEN_BRACE: '{';
CLOSED_BRACE: '}';

fragment QUOTE: '"';
STRING:
	QUOTE
		[ !'#$%&()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~\\]
		* QUOTE;

IDENTIFIER: [a-zA-Z_] [a-zA-Z0-9_]*;
fragment DIGIT: [0-9];
fragment UNDERSCORE: '_';
INTEGER_NORMAL: '-'? DIGIT (DIGIT | UNDERSCORE)*;

number: INTEGER_NORMAL;

boolean: TRUE | FALSE;

literal: number | STRING | boolean;

function_call:
	name = IDENTIFIER OPEN_PAREN (expression (COMMA expression)*)? COMMA? CLOSED_PAREN;

struct_init_field: name = IDENTIFIER COLON value = expression;

struct_init:
	INIT name = IDENTIFIER OPEN_BRACE (
		struct_init_field (COMMA struct_init_field)
	)? CLOSED_BRACE;

expression:
	literal
	| IDENTIFIER
	| function_call
	| struct_init
	| left = expression binary_op = PLUS right = expression
	| left = expression binary_op = MINUS right = expression
	| left = expression binary_op = MULTIPLY right = expression;

assignment: LET? name = IDENTIFIER EQUAL expression;

stmt: assignment SEMI | expression SEMI | block;

block: OPEN_BRACE stmt* CLOSED_BRACE;

argument: name = IDENTIFIER COLON type = IDENTIFIER;

function:
	FN function_name = IDENTIFIER OPEN_PAREN (
		argument (COMMA argument)*
	)? CLOSED_PAREN (SEMI | body = block);

struct_field: name = IDENTIFIER COLON type = IDENTIFIER;

struct:
	STRUCT name = IDENTIFIER OPEN_BRACE (
		struct_field (COMMA struct_field)*
	)? CLOSED_BRACE;

item: function | struct;

document: item*; 