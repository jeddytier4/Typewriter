using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Typewriter.TemplateEditor.Lexing
{
    public interface ITokens
    {
        IBraceStack BraceStack { get; }

        void Add(string classification, int start, int length = 1, string quickInfo = null);
        void Add(Token token);
        void AddRange(IEnumerable<Token> tokens);
        IEnumerable<Token> FindTokens(int position);
        Token GetToken(int position);
        IEnumerable<Token> GetTokens(Span span);
        void AddBrace(Stream stream, string classification = Classifications.Operator);
    }
}