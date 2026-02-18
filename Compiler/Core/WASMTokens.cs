using CDTk;

public class WASMTokens : TokenSet
{
    public Token LeftParen = new Token(@"\(");
    public Token RightParen = new Token(@"\)");
    public Token Dollar = new Token(@"\$");
    public Token Whitespace = new Token(@"[ \t\r\n]+");
    
    public Token KwFunc = @"\bfunc\b";
    public Token KwParam = @"\bparam\b";
    public Token KwResult = @"\bresult\b";
    public Token KwModule = @"\bmodule\b";
    public Token KwImport = @"\bimport\b";
    public Token KwExport = @"\bexport\b";
    public Token KwMemory = @"\bmemory\b";
    public Token KwGlobal = @"\bglobal\b";
    public Token KwMut = @"\bmut\b";
    public Token KwIf = @"\bif\b";
    public Token KwThen = @"\bthen\b";
    public Token KwElse = @"\belse\b";
    public Token KwLoop = @"\bloop\b";
    public Token KwBlock = @"\bblock\b";
    public Token KwBr = @"\bbr\b";
    public Token KwBrIf = @"\bbr_if\b";
    public Token KwCall = @"\bcall\b";
    public Token KwLocalGet = @"\blocal\.get\b";
    public Token KwLocalSet = @"\blocal\.set\b";
    public Token KwGlobalGet = @"\bglobal\.get\b";
    public Token KwGlobalSet = @"\bglobal\.set\b";
    
    public Token TypeI32 = @"\bi32\b";
    public Token TypeI64 = @"\bi64\b";
    public Token TypeF32 = @"\bf32\b";
    public Token TypeF64 = @"\bf64\b";
    
    public Token I32Const = @"\bi32\.const\b";
    public Token I64Const = @"\bi64\.const\b";
    public Token F32Const = @"\bf32\.const\b";
    public Token F64Const = @"\bf64\.const\b";
    
    public Token I32Add = @"\bi32\.add\b";
    public Token I32Sub = @"\bi32\.sub\b";
    public Token I32Mul = @"\bi32\.mul\b";
    public Token I32Eqz = @"\bi32\.eqz\b";
    
    public Token Identifier = @"[a-zA-Z_][a-zA-Z0-9_]*";
    public Token Number = @"\d+";
}
