grammar Jerry;

WHITESPACE: [ \n\t\r]+ -> channel(1);

FN: 'fn';
LET: 'let';

EQUAL: '=';
PLUS: '+';

SEMI: ';';

OPEN_PAREN: '(';
CLOSED_PAREN: ')';
OPEN_BRACE: '{';
CLOSED_BRACE: '}';

IDENTIFIER: [a-zA-Z] [a-zA-Z0-9]*;
NUMBER_SIMPLE: [0-9]+;

number: NUMBER_SIMPLE;

literal: number;

expression: literal | expression PLUS expression;

assignment: LET? name = IDENTIFIER EQUAL expression;

stmt: assignment SEMI | block;

block: OPEN_BRACE stmt* CLOSED_BRACE;

function:
	FN function_name = IDENTIFIER OPEN_PAREN CLOSED_PAREN block;

document: function*; 