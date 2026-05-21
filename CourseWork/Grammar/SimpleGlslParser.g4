translation_unit
    : external_declaration*
    ;

external_declaration
    : function_definition
    | declaration
    | SEMICOLON
    ;

function_definition
    : fully_specified_type IDENTIFIER LEFT_PAREN function_parameters? RIGHT_PAREN compound_statement
    ;

function_parameters
    : parameter_declaration (COMMA parameter_declaration)*
    ;

parameter_declaration
    : type_qualifier? fully_specified_type (IDENTIFIER array_specifier?)?
    ;

type_qualifier
    : CONST | IN | OUT | INOUT | UNIFORM | BUFFER | READONLY
    ;

fully_specified_type
    : type_specifier
    | type_qualifier+ type_specifier
    ;

type_specifier
    : VOID | BOOL | INT | UINT | FLOAT | DOUBLE
    | VEC2 | VEC3 | VEC4 | DVEC2 | DVEC3 | DVEC4
    | struct_specifier
    | IDENTIFIER
    ;

struct_specifier
    : STRUCT IDENTIFIER? LEFT_BRACE struct_declaration_list RIGHT_BRACE
    ;

struct_declaration_list
    : struct_declaration+
    ;

struct_declaration
    : type_specifier struct_declarator_list SEMICOLON
    ;

struct_declarator_list
    : struct_declarator (COMMA struct_declarator)*
    ;

struct_declarator
    : IDENTIFIER array_specifier?
    ;

array_specifier
    : LEFT_BRACKET constant_expression? RIGHT_BRACKET
    ;

declaration
    : init_declarator_list SEMICOLON
    | fully_specified_type IDENTIFIER LEFT_BRACE struct_declaration_list RIGHT_BRACE (IDENTIFIER array_specifier?)? SEMICOLON
    | type_qualifier+ SEMICOLON
    ;

init_declarator_list
    : single_declaration (COMMA typeless_declaration)*
    ;

single_declaration
    : fully_specified_type typeless_declaration?
    ;

typeless_declaration
    : IDENTIFIER array_specifier? (ASSIGN initializer)?
    ;

initializer
    : assignment_expression
    | LEFT_BRACE initializer_list COMMA? RIGHT_BRACE
    ;

initializer_list
    : initializer (COMMA initializer)*
    ;

expression
    : assignment_expression (COMMA assignment_expression)*
    ;

assignment_expression
    : constant_expression
    | unary_expression assignment_operator assignment_expression
    ;

assignment_operator
    : ASSIGN | ADD_ASSIGN | SUB_ASSIGN | MUL_ASSIGN | DIV_ASSIGN | MOD_ASSIGN
    | LEFT_ASSIGN | RIGHT_ASSIGN | AND_ASSIGN | XOR_ASSIGN | OR_ASSIGN
    ;

constant_expression
    : logical_or_expression (QUESTION expression COLON assignment_expression)?
    ;

logical_or_expression
    : logical_and_expression (OR_OP logical_and_expression)*
    ;
logical_and_expression
    : inclusive_or_expression (AND_OP inclusive_or_expression)*
    ;
inclusive_or_expression
    : exclusive_or_expression (VERTICAL_BAR exclusive_or_expression)*
    ;
exclusive_or_expression
    : and_expression (CARET and_expression)*
    ;
and_expression
    : equality_expression (AMPERSAND equality_expression)*
    ;
equality_expression
    : relational_expression ((EQ_OP | NE_OP) relational_expression)*
    ;
relational_expression
    : shift_expression ((LEFT_ANGLE | RIGHT_ANGLE | LE_OP | GE_OP) shift_expression)*
    ;
shift_expression
    : additive_expression ((LEFT_OP | RIGHT_OP) additive_expression)*
    ;
additive_expression
    : multiplicative_expression ((PLUS | DASH) multiplicative_expression)*
    ;
multiplicative_expression
    : unary_expression ((STAR | SLASH | PERCENT) unary_expression)*
    ;

unary_expression
    : postfix_expression
    | INC_OP unary_expression
    | DEC_OP unary_expression
    | unary_operator unary_expression
    ;

unary_operator
    : PLUS | DASH | BANG | TILDE
    ;

postfix_expression
    : primary_expression
    | postfix_expression LEFT_BRACKET expression RIGHT_BRACKET
    | postfix_expression LEFT_PAREN function_call_parameters? RIGHT_PAREN
    | type_specifier LEFT_PAREN function_call_parameters? RIGHT_PAREN
    | postfix_expression DOT field_selection
    | postfix_expression INC_OP
    | postfix_expression DEC_OP
    ;

field_selection
    : IDENTIFIER
    | function_call
    ;

function_call
    : IDENTIFIER LEFT_PAREN function_call_parameters? RIGHT_PAREN
    ;

function_call_parameters
    : assignment_expression (COMMA assignment_expression)*
    | VOID
    ;

primary_expression
    : IDENTIFIER
    | TRUE | FALSE
    | INT_CONST | UINT_CONST | FLOAT_CONST | DOUBLE_CONST
    | LEFT_PAREN expression RIGHT_PAREN
    ;

statement
    : compound_statement
    | simple_statement
    ;

simple_statement
    : declaration_statement
    | expression_statement
    | selection_statement
    | switch_statement
    | case_label
    | iteration_statement
    | jump_statement
    ;

declaration_statement
    : declaration
    ;

expression_statement
    : SEMICOLON
    | expression SEMICOLON
    ;

compound_statement
    : LEFT_BRACE statement* RIGHT_BRACE
    ;

selection_statement
    : IF LEFT_PAREN condition RIGHT_PAREN statement (ELSE statement)?
    ;

condition
    : expression
    | fully_specified_type IDENTIFIER ASSIGN initializer
    ;

switch_statement
    : SWITCH LEFT_PAREN expression RIGHT_PAREN LEFT_BRACE (statement)* RIGHT_BRACE
    ;

case_label
    : CASE constant_expression COLON
    | DEFAULT COLON
    ;

iteration_statement
    : WHILE LEFT_PAREN condition RIGHT_PAREN statement
    | DO statement WHILE LEFT_PAREN expression RIGHT_PAREN SEMICOLON
    | FOR LEFT_PAREN for_init_statement for_rest_statement RIGHT_PAREN statement
    ;

for_init_statement
    : expression_statement
    | declaration_statement
    ;

for_rest_statement
    : condition? SEMICOLON expression?
    ;

jump_statement
    : BREAK SEMICOLON
    | CONTINUE SEMICOLON
    | RETURN expression? SEMICOLON
    | DISCARD SEMICOLON
    ;