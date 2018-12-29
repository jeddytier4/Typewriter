using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using Typewriter.CodeModel;
using Typewriter.Generation.Controllers;
using Typewriter.TemplateEditor.Lexing;
using Typewriter.VisualStudio;
using Type = System.Type;

namespace Typewriter.Generation
{
    public class TstXParser
    {


        private class MultiFileStringBuilder
        {
            private readonly Dictionary<string, StringBuilder> _fileStore;
            private readonly MultiFileStringBuilder _parent;
            private readonly string[] _fileNames;
            private StringBuilder[] _outputs;
            public Dictionary<string, StringBuilder> FileStore { get => _fileStore; }

            public MultiFileStringBuilder(Dictionary<string, StringBuilder> fileStore, params string[] fileNames)
            {
                this._fileStore = fileStore;
                this._fileNames = fileNames;
                InitOutputs();
            }

            public MultiFileStringBuilder(MultiFileStringBuilder parent, params string[] fileNames) : this(parent.FileStore, fileNames)
            {
                this._parent = parent;
            }

            private void InitOutputs()
            {
                List<StringBuilder> outputs = new List<StringBuilder>();

                foreach (var fileName in _fileNames)
                {
                    StringBuilder output;
                    if (!_fileStore.TryGetValue(fileName, out output))
                    {
                        output = new StringBuilder();
                        _fileStore.Add(fileName, output);
                    }
                    outputs.Add(output);
                }

                this._outputs = outputs.ToArray();
            }

            public void Append(char value)
            {
                _parent?.Append(value);
                _outputs?.ForEach(p => p.Append(value));
            }

            public void Append(string value)
            {
                _parent?.Append(value);
                _outputs?.ForEach(p => p.Append(value));
            }
            public void Append(object value)
            {
                _parent?.Append(value);
                _outputs?.ForEach(p => p.Append(value));
            }

            public override string ToString()
            {
                return string.Join("----- END OF FILE ----\r\n", _outputs.Zip(_fileNames, (sb, fileName) => $"----- {fileName} ----\r\n{sb}"));
            }
        }


        public static Dictionary<string, string> Parse(ProjectItem projectItem, string sourcePath, string template, List<Type> extensions, object context, string mainFileName, out bool success)
        {
            var instance = new TstXParser(extensions);
            var outputs = new Dictionary<string, StringBuilder>();


            if (template.StartsWith("\r\n"))
                template = template.Substring(2);
            else if (template.StartsWith("\n"))
                template = template.Substring(1);

            instance.ParseTemplate(projectItem, sourcePath, template, context, new MultiFileStringBuilder(outputs, mainFileName));
            success = instance.hasError == false;

            return outputs.ToDictionary(k => k.Key, v => v.Value.ToString());
        }

        private readonly List<Type> extensions;
        private bool hasError;

        private TstXParser(List<Type> extensions)
        {
            this.extensions = extensions;
        }


        private string ParseTemplate(ProjectItem projectItem, string sourcePath, string template, object context)
        {
            if (string.IsNullOrEmpty(template)) return null;

            var temp = new StringBuilder();
            var output = new MultiFileStringBuilder(new Dictionary<string, StringBuilder>() { { string.Empty, temp } }, string.Empty);

            ParseTemplate(projectItem, sourcePath, template, context, output);
            return temp.ToString();
        }

        /*
        private string ParseTemplate(ProjectItem projectItem, string sourcePath, string template, object context, Dictionary<string, StringBuilder> outputs)
        {
            if (string.IsNullOrEmpty(template)) return null;

            var output = new CascadingStringBuilder();

            ParseTemplate(projectItem, sourcePath, template, context, output, outputs);
            return output.ToString();
        }*/

        private bool ParseTemplate(ProjectItem projectItem, string sourcePath, string template, object context, MultiFileStringBuilder output)
        {
            if (string.IsNullOrEmpty(template)) return false;

            var stream = new Stream(template);


            bool hadOutput = false;

            //stream.SkipWhitespaceUntilNewLine();
            while (true)
            {
                var result = ParseDollar(projectItem, sourcePath, stream, context, output);
                if (result.ProcessedIdentifier)
                {
                    hadOutput |= result.HadOutput;
                    continue;
                }

                if (stream.Current != char.MinValue)
                {
                    output.Append(stream.Current);
                    hadOutput = true;
                }

                if (!stream.Advance()) break;
            } 

            return hadOutput;
        }

        private class ParseResult
        {
            public bool ProcessedIdentifier { get; set; }
            public bool HadOutput { get; set; }

            public ParseResult(bool processedIdentifier, bool hadOutput)
            {
                ProcessedIdentifier = processedIdentifier;
                HadOutput = hadOutput;
            }
            public ParseResult()
            {

            }
        }
        private ParseResult ParseDollar(ProjectItem projectItem, string sourcePath, Stream stream, object context, MultiFileStringBuilder output)
        {
            if (stream.Current == '$')
            {
                var identifier = stream.PeekWord(1);
                object value;

                if (TryGetIdentifier(projectItem, sourcePath, identifier, context, out value))
                {
                    bool hadOutput = false;
                    stream.Advance(identifier.Length + 1);

                    var collection = value as IEnumerable<Item>;
                    if (collection != null)
                    {
                        var filter = ParseBlock(stream, '(', ')');
                        var fileNames = ParseBlock(stream, '<', '>');
                        var block = ParseBlock(stream, '[', ']');
                        var separator = ParseBlock(stream, '[', ']');


                        if (filter == null && block == null && separator == null)
                        {
                            var stringValue = value.ToString();

                            if (stringValue != value.GetType().FullName)
                            {
                                output.Append(stringValue);
                            }
                            else
                            {
                                output.Append("$");
                                output.Append(identifier);
                            }
                            hadOutput = true;
                        }
                        else
                        {
                            Item[] items = null;
                            if (filter != null && filter.StartsWith("$"))
                            {
                                var predicate = filter.Remove(0, 1);
                                if (extensions != null)
                                {
                                    // Lambda filters are always defined in the first extension type
                                    var c = extensions.FirstOrDefault()?.GetMethod(predicate);
                                    if (c != null)
                                    {
                                        try
                                        {
                                            items = collection.Where(x => (bool)c.Invoke(null, new object[] { x })).ToArray();
                                        }
                                        catch (Exception e)
                                        {
                                            hasError = true;

                                            var message = $"Error rendering template. Cannot apply filter to identifier '{identifier}'.";
                                            LogException(e, message, projectItem, sourcePath);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                bool dummy = false;
                                items = ItemFilter.Apply(collection, filter, ref dummy).ToArray();
                            }

                            if (!string.IsNullOrEmpty(separator))
                                separator = ParseTemplate(projectItem, sourcePath, separator, context);

                            if (separator == null)
                            {
                                separator = "\r\n";
                            }
                            if (items != null && items.Length > 0)
                            {

                                for (int i = 0; i < items.Length; i++)
                                {
                                    var item = items[i];

                                    var saveOutput = output;

                                    if (fileNames != null)
                                    {
                                        var fileNamesValue = ParseTemplate(projectItem, sourcePath, fileNames, item);
                                        if (fileNamesValue != null)
                                        {
                                            var fileNamesArr = fileNamesValue.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                            output = new MultiFileStringBuilder(output, fileNamesArr);
                                        }
                                    }

                                 
                                    var itemHadOutput = ParseTemplate(projectItem, sourcePath, block, item, output);
                                    if (itemHadOutput && separator != null && i < items.Length - 1)
                                    {
                                        output.Append(separator);
                                        hadOutput = true;
                                    }

                                    output = saveOutput;
                                }
                            }
                            else
                            {
                                if (stream.PeekLine() == "\r\n")
                                    stream.Advance(2);
                                else if (stream.PeekLine() == "\n")
                                    stream.Advance();
                            }

                            //                      string itemsOutput = string.Join(separator,
                            //                            items.Select(item =>
                            //                                    ParseTemplate(projectItem, sourcePath, block, item, outputs)?.TrimEnd()));
                            //                          output.Append(itemsOutput);
                        }
                    }
                    else if (value is bool)
                    {
                        var trueBlock = ParseBlock(stream, '[', ']');
                        var falseBlock = ParseBlock(stream, '[', ']');

                        hadOutput |= ParseTemplate(projectItem, sourcePath, (bool)value ? trueBlock : falseBlock, context, output);
                    }
                    else
                    {
                        var block = ParseBlock(stream, '[', ']');
                        if (value != null)
                        {
                            if (block != null)
                            {
                                hadOutput |= ParseTemplate(projectItem, sourcePath, block, value, output);
                            }
                            else
                            {
                                output.Append(value);
                                hadOutput = true;
                            }
                        }
                    }

                    return new ParseResult(true, hadOutput);
                }
            }

            return new ParseResult();
        }

        private static string ParseBlock(Stream stream, char open, char close)
        {
            if (stream.Current == open)
            {
                var block = stream.PeekBlock(1, open, close);

                stream.Advance();
                stream.Advance(block.Length);
                stream.Advance();
                //stream.Advance(stream.Peek(2) == close ? 2 : 1);

      
                if (block.StartsWith("\r\n"))
                    block = block.Substring(2);
                else if (block.StartsWith("\n"))
                    block = block.Substring(1);

                if (block.EndsWith("\r\n"))
                    block = block.Substring(0, block.Length - 2);
                else if (block.EndsWith("\n"))
                    block = block.Substring(0, block.Length - 1);
                return block;
            }

            return null;
        }

        private bool TryGetIdentifier(ProjectItem projectItem, string sourcePath, string identifier, object context, out object value)
        {
            value = null;

            if (identifier == null) return false;

            var type = context.GetType();

            try
            {
                var property = type.GetProperty(identifier);
                if (property != null)
                {
                    value = property.GetValue(context);
                    return true;
                }

                var extension = extensions.Select(e => e.GetMethod(identifier, new[] { type })).FirstOrDefault(m => m != null);
                if (extension != null)
                {
                    value = extension.Invoke(null, new[] { context });
                    return true;
                }
            }
            catch (Exception e)
            {
                hasError = true;

                var message = $"Error rendering template. Cannot get identifier '{identifier}'.";
                LogException(e, message, projectItem, sourcePath);
            }

            return false;
        }

        private void LogException(Exception exception, string message, ProjectItem projectItem, string sourcePath)
        {
            // skip the target invokation exception, get the real exception instead.
            if (exception is TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            var studioMessage = $"{message} Error: {exception.Message}. Source path: {sourcePath}. See Typewriter output for more detail.";
            var logMessage = $"{message} Source path: {sourcePath}{Environment.NewLine}{exception}";

            Log.Error(logMessage);
            ErrorList.AddError(projectItem, studioMessage);
            ErrorList.Show();
        }
    }
}
