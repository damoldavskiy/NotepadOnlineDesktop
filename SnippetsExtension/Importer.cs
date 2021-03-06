﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SnippetsExtension
{
    static class Importer
    {
        static readonly string _snippet = "snippet";

        public static Bracket[] LoadBrackets(string path)
        {
            if (!File.Exists(path))
                return new Bracket[0];

            var brackets = new List<Bracket>();

            using (var stream = new StreamReader(path, Encoding.Default))
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    if (line.Length != 2)
                        throw new Exception(Properties.Resources.ExpectedTwo); // Ожидалось 2 символа

                    brackets.Add(new Bracket { Start = line[0], End = line[1] });
                }
            }

            return brackets.ToArray();
        }

        public static Snippet[] LoadSnippets(string path)
        {
            if (!File.Exists(path))
                return new Snippet[0];

            var snippets = new List<Snippet>();

            var state = ImporterState.ReadingTemplate;
            var template = "";
            var headers = "";
            var value = "";

            using (var stream = new StreamReader(path, Encoding.Default))
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();

                    if (state == ImporterState.ReadingTemplate)
                        if (line.StartsWith(_snippet))
                        {
                            line = line.Remove(0, _snippet.Length).Trim();
                            if (line[0] != '\'')
                                throw new Exception(Properties.Resources.ExpectedPattern); // После snippet нету кавычки
                            line = line.Substring(1);

                            int pos = -1;
                            for (int i = 0; i < line.Length - 1; i++)
                                if (line[i] != '\\' && line[i + 1] == '\'')
                                {
                                    pos = i + 1;
                                    break;
                                }

                            if (pos == -1)
                                throw new Exception(Properties.Resources.ExpectedEnd); // Нету символа окончания шаблона

                            template = line.Substring(0, pos);
                            headers = line.Substring(pos + 1).Trim();

                            state = ImporterState.ReadingValue;
                            continue;
                        }
                        else if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                            continue;
                        else
                            throw new Exception(Properties.Resources.ExpectedSnippet); // Ожидалось слово snippet

                    if (state == ImporterState.ReadingValue)
                    {
                        if (line == "")
                        {
                            snippets.Add(DecodeSnippet(template, headers, value));

                            state = ImporterState.ReadingTemplate;
                            template = "";
                            headers = "";
                            value = "";
                        }
                        else if (line == ".")
                            value += "\n";
                        else if (line == "\\.")
                            value += ".";
                        else
                            value += (value.Length > 0 ? "\n" : "") + line;
                        continue;
                    }
                }

                snippets.Add(DecodeSnippet(template, headers, value));
            }
            
            for (int i = 0; i < snippets.Count; i++)
                for (int j = 0; j < snippets.Count; j++)
                    if (snippets[i].Template.Contains(snippets[j].Template))
                        if (i > j)
                        {
                            var m = snippets[i];
                            snippets[i] = snippets[j];
                            snippets[j] = m;
                        }

            return snippets.ToArray();
        }

        static Snippet DecodeSnippet(string template, string headers, string value)
        {
            headers = headers.ToUpper();

            if (headers.Contains("B") && headers.Contains("R"))
                template = @"(?<=^\s*)" + template;
                //template = "^" + template;

            if (headers.Contains("W"))
            {
                if (!headers.Contains("R"))
                    template = Regex.Escape(template);
                template = @"(?<![a-zA-Z])" + template;
                headers += "R";
            }

            var snippet = new Snippet
            {
                Template = template,
                BeginOnly = headers.Contains("B"),
                UsesRegex = headers.Contains("R"),
                SelectTrigger = headers.Contains("S")
            };

            if (!headers.Contains("A") && !headers.Contains("S"))
                snippet.Template += "\t";

            // Python
            var python = false;
            var start = -1;
            var end = -1;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '`' && i > 0 && value[i - 1] == '\\')
                {
                    value = value.Remove(i - 1, 1);
                    continue;
                }
                
                if (value[i] == '`' && !python)
                {
                    if (snippet.ContainsPythonCode)
                        throw new Exception(Properties.Resources.TooMuchPython); // Более одного блока Python
                    snippet.ContainsPythonCode = true;
                    snippet.PythonPosition = start = i;
                    python = true;
                    continue;
                }

                if (value[i] == '`' && python)
                {
                    end = i;
                    python = false;
                    continue;
                }
            }
            
            if (snippet.ContainsPythonCode)
            {
                snippet.PythonCode = value.Substring(start + 1, end - start - 1);
                value = value.Substring(0, start) + value.Substring(end + 1);
            }

            if (Snippet.Index(value, 0) == -1)
                value += "$0";

            snippet.Value = value;

            return snippet;
        }
    }
}
