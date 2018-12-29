using System;
using System.IO;

namespace Typewriter
{
    internal static class Constants
    {
        internal static readonly string[]  TemplateExtensions = new string[] { TstTemplateExtension , TstXTemplateExtension  };
        internal const string TstTemplateExtension = ".tst";
        internal const string TstXTemplateExtension = ".tstx";

        internal static readonly string[] ContentTypes = new string[] {TstContentType, TstXContentType };
        internal const string TstContentType = "tst";
        internal const string TstXContentType = "tstx";

        internal const string LanguageName = "TSTXz";

        internal const string CsExtension = ".cs";

        internal const string ExtensionPackageId = "ab103aaa-514a-4650-a0b8-b798c40978d5";
        internal const string LanguageServiceId = "500c4886-937d-4d62-b869-e5bbf4b9e61b";

        internal const string BaseDefinition = "code";
        internal const char NewLine = '\n';

        internal static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "Typewriter");
        internal static readonly string TypewriterDirectory = Path.GetDirectoryName(typeof(Constants).Assembly.Location);
        internal static readonly string ResourcesDirectory = Path.Combine(TypewriterDirectory, "Resources");
        internal static readonly string ReferenceAssembliesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETFramework");

        internal static bool RoslynEnabled = false;
    }

    internal static class Classifications
    {
        public const string BraceHighlight = "MarkerFormatDefinition/HighlightedReference";
        public const string Comment = "Comment";
        public const string Identifier = "Identifier";
        public const string Keyword = "Keyword";
        public const string Number = "Number";
        public const string Operator = "Operator";
        public const string String = "String";
        public const string SyntaxError = "syntax error";
        public const string Directive = "excluded code";
        public const string Warning = "compiler warning";
        public const string ClassSymbol = "Tst/ClassSymbol";
        public const string InterfaceSymbol = "Tst/InterfaceSymbol";
        public const string Property = "Tst/Property";
        public const string AlternalteProperty = "Tst/AlternateProperty";
    }
}
