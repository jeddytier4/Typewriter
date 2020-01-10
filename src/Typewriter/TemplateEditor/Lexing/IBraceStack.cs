namespace Typewriter.TemplateEditor.Lexing
{
    public interface IBraceStack
    {
        bool IsBalanced(char brace);
        Token Pop(char brace);
        void Push(Token token, char brace);
    }
}