using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Typewriter.TemplateEditor.Lexing.Roslyn;

namespace Typewriter.TemplateEditor.Lexing
{
    public interface ISemanticModel
    {
        ContextSpans ContextSpans { get; }
        ITokens ErrorTokens { get; }
        ShadowClass ShadowClass { get; }
        Identifiers TempIdentifiers { get; }
        ITokens Tokens { get; }

        ContextSpan GetContextSpan(int position);
        IEnumerable<ContextSpan> GetContextSpans(ContextType type);
        IEnumerable<Token> GetErrorTokens(Span span);
        IEnumerable<Identifier> GetIdentifiers(int position);
        string GetQuickInfo(int position);
        Token GetToken(int position);
        IEnumerable<Token> GetTokens(Span span);
        Identifier GetIdentifier(Context context, string name);
    }
}