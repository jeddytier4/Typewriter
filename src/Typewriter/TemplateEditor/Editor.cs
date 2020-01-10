using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Typewriter.CodeModel;
using Typewriter.TemplateEditor.Lexing;
using Typewriter.TemplateEditor.Lexing.Roslyn;
using Typewriter.VisualStudio;
using Document = EnvDTE.Document;

namespace Typewriter.TemplateEditor
{
    public class Editor
    {
        public static Editor Instance { get; } = new Editor();

        private readonly ShadowClass shadowClass;
        private readonly Contexts contexts;
        private readonly CodeLexer _codeLexerTst;
        private readonly TstXCodeLexer _codeLexerTstX;
        private readonly TemplateLexer _templateLexerTst;
        private readonly TstXTemplateLexer _templateLexerTstX;

        private ITextSnapshot currentSnapshot;
        private ISemanticModel semanticModelCache;

        private Editor()
        {
            shadowClass = new ShadowClass();
            contexts = new Contexts(shadowClass);
            _codeLexerTst = new CodeLexer(contexts);
            _codeLexerTstX = new TstXCodeLexer(contexts);
            _templateLexerTst = new TemplateLexer(contexts);
            _templateLexerTstX = new TstXTemplateLexer(contexts);
        }

        private ISemanticModel GetSemanticModel(ITextBuffer buffer)
        {
            if (currentSnapshot == buffer.CurrentSnapshot)
                return semanticModelCache;

            currentSnapshot = buffer.CurrentSnapshot;
            

            var code = currentSnapshot.GetText();

            if (buffer.ContentType.TypeName == Constants.TstContentType)
            {
                semanticModelCache = new SemanticModel(shadowClass);
                _codeLexerTst.Tokenize(semanticModelCache, code, GetProjectItem(buffer));
                _templateLexerTst.Tokenize(semanticModelCache, code, buffer.ContentType);
            }
            else
            {
                semanticModelCache = new TstXSemanticModel(shadowClass);
                _codeLexerTstX.Tokenize(semanticModelCache, code, GetProjectItem(buffer));
                _templateLexerTstX.Tokenize(semanticModelCache, code, buffer.ContentType);
            }

            return semanticModelCache;
        }

        private static ProjectItem GetProjectItem(ITextBuffer textBuffer)
        {
            var ret = GetProjectItemFastAndHacky(textBuffer);
            if (ret == null)
            {
                Log.Debug("Unable to find the ProjectItem the fast way.");

                //Fallback
                string templatePath = GetFilePath(textBuffer);
                ret = ExtensionPackage.Instance.Dte.Solution.FindProjectItem(templatePath);
            }

            return ret;
        }

        private static ProjectItem GetProjectItemFastAndHacky(ITextBuffer textBuffer)
        {
            if (!textBuffer.Properties.TryGetProperty<IVsTextBuffer>(typeof(IVsTextBuffer), out var vstb))
                return null;

            //HACK: reflection on internal property of VsTextBufferAdapter. 
            var adapterType = vstb.GetType();
            var prop = adapterType.BaseType?.GetProperty("DTEDocument", BindingFlags.Instance | BindingFlags.NonPublic);
            var doc = prop?.GetValue(vstb) as Document;
            return doc?.ProjectItem;
        }

        private static string GetFilePath(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var doc) ? doc.FilePath : null;
        }

        // Outlining
        public IEnumerable<SnapshotSpan> GetCodeBlocks(ITextBuffer buffer)
        {
            var tokens = GetSemanticModel(buffer);
            var contextSpans = tokens.GetContextSpans(ContextType.CodeBlock);

            return contextSpans.Select(s => new SnapshotSpan(buffer.CurrentSnapshot, s.Start, s.End - s.Start));
        }

        // Brace matching
        public IEnumerable<ITagSpan<TextMarkerTag>> GetBraceTags(ITextBuffer buffer, SnapshotPoint point)
        {
            var tokens = GetSemanticModel(buffer);
            var token = tokens.GetToken(point.Position - 1);
            var tag = new TextMarkerTag(Classifications.BraceHighlight);

            if (token?.MatchingToken != null && token.IsOpen == false)
            {
                yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(point.Snapshot, token.Start, 1), tag);
                yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(point.Snapshot, token.MatchingToken.Start, 1), tag);
            }
            else
            {
                token = tokens.GetToken(point.Position);

                if (token?.MatchingToken != null && token.IsOpen)
                {
                    yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(point.Snapshot, token.Start, 1), tag);
                    yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(point.Snapshot, token.MatchingToken.Start, 1), tag);
                }
            }
        }

        // Classification
        public IEnumerable<ClassificationSpan> GetClassificationSpans(ITextBuffer buffer, SnapshotSpan span, IClassificationTypeRegistryService classificationRegistry)
        {
            var semanticModel = GetSemanticModel(buffer);
            var tokens = semanticModel.GetTokens(span.Span);

            foreach (var token in tokens)
            {
                if (token.Classification != null)
                {
                    var classificationType = classificationRegistry.GetClassificationType(token.Classification);
                    yield return new ClassificationSpan(new SnapshotSpan(span.Snapshot, token.Start, token.Length), classificationType);
                }
            }
        }

        // Statement completion
        public bool EnableFullIntelliSense(ITextBuffer buffer, SnapshotPoint point)
        {
            var tokens = GetSemanticModel(buffer);
            var type = tokens.GetContextSpan(point).Type;

            return type == ContextType.CodeBlock || type == ContextType.Lambda;
        }

        public IEnumerable<Completion> GetCompletions(ITextBuffer buffer, SnapshotSpan span, IGlyphService glyphService)
        {
            var semanticModel = GetSemanticModel(buffer);
            var identifiers = semanticModel.GetIdentifiers(span.Start);
            var contextSpan = semanticModel.GetContextSpan(span.Start);
            var prefix = contextSpan.Type == ContextType.Template ? "$" : "";

            var completions = identifiers.Select(i =>
            {
                var imageSource = glyphService.GetGlyph(i.Glyph, StandardGlyphItem.GlyphItemPublic);
                var quickInfo = i.IsParent ? i.QuickInfo.Replace("Item", contextSpan.ParentContext?.Name) : i.QuickInfo;

                return new Completion(prefix + i.Name, prefix + i.Name, quickInfo, imageSource, null);
            });

            if (contextSpan.Type == ContextType.Template && contextSpan.Context.Name == ((buffer.ContentType.TypeName  == Constants.TstContentType) ? nameof(File) : nameof(RootContext)))
            {
                var imageSource = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                var codeBlock = new[]
                {
                    new Completion("#reference", "#reference", "Add a reference to an external assembly.", imageSource, null),
                    new Completion("${ }", "${\r\n}", "Insert a custom C# code block.", imageSource, null)
                };

                return codeBlock.Concat(completions);
            }

            return completions;
        }

        // Syntax errors
        public IEnumerable<ITagSpan<ErrorTag>> GetSyntaxErrorTags(ITextBuffer buffer, SnapshotSpan span)
        {
            var semanticModel = GetSemanticModel(buffer);
            var tokens = semanticModel.GetErrorTokens(span.Span);

            foreach (var token in tokens)
            {
                var tag = new ErrorTag(token.IsError ? Classifications.SyntaxError : Classifications.Warning, token.QuickInfo);
                yield return new TagSpan<ErrorTag>(new SnapshotSpan(span.Snapshot, token.Start, token.Length), tag);
            }
        }

        // Quick info
        public string GetQuickInfo(ITextBuffer buffer, SnapshotSpan span)
        {
            var semanticModel = GetSemanticModel(buffer);
            return semanticModel.GetQuickInfo(span.Start);
        }

        public void FormatDocument(ITextView textView)
        {
            var buffer = textView.TextBuffer;
            var text = buffer.CurrentSnapshot.GetText();
            var model = GetSemanticModel(buffer);
            var codeBlock = model.GetContextSpans(ContextType.CodeBlock).FirstOrDefault();
            var codeBlockText = text.Substring(codeBlock.Start, codeBlock.End- codeBlock.Start);

            
            var tree = CSharpSyntaxTree.ParseText(codeBlockText);
            var formattedNode = Formatter.Format(tree.GetRoot(), new Microsoft.CodeAnalysis.AdhocWorkspace());
            var formatted = formattedNode.ToFullString();
            formatted = string.Join("\r\n", formatted.Split(new string[] {"\r\n"}, StringSplitOptions.None).Select(s => s.Length > 0 ? $"\t{s}" : ""));
          //  formatted = $"\r\n{formatted}\r\n";

            //var pos = textView.Caret.ContainingTextViewLine;
            //var x = textView.Caret.Left;

            using (var edit = buffer.CreateEdit())
            {
                edit.Replace(codeBlock.Start, codeBlock.End- codeBlock.Start, formatted);
                edit.Apply();
            }

           // textView.Caret.MoveTo(pos, x);
        }
    }
}