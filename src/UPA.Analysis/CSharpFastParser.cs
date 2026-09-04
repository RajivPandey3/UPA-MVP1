using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UPA.Core;
using System.Buffers;
using Microsoft.Win32.SafeHandles;

namespace UPA.Analysis
{
    public static class CSharpFastParser
    {
        private enum LexicalState
        {
            Normal,
            String,
            Char,
            VerbatimString,
            LineComment,
            BlockComment
        }

        private static readonly Regex RequireRegex =
            new(@"RequireComponent\s*\(\s*typeof\s*\(\s*(?<name>[A-Za-z_][\w.]*)\s*\)\s*\)",
                RegexOptions.Compiled);

        private static readonly string[] LifecycleMethods =
        {
            "Awake", "OnEnable", "Start", "Update", "FixedUpdate",
            "LateUpdate", "OnDisable", "OnDestroy",
            "OnTriggerEnter", "OnCollisionEnter"
        };

        private static readonly Regex[] LifecycleRegexes =
            LifecycleMethods
                .Select(m => new Regex(
                    $@"\b{Regex.Escape(m)}\s*\(",
                    RegexOptions.Compiled))
                .ToArray();

        [ThreadStatic] private static byte[]? t_byteBuffer;
        [ThreadStatic] private static char[]? t_charBuffer;

        public static CSharpScriptModel ParseFile(string root, string path)
        {
            var relative = path.Substring(root.Length + 1).Replace('\\', '/');
            var diagnostics = new List<Diagnostic>();
            var types = new List<CSharpTypeModel>();

            string? ns = null;

            var info = new FileInfo(path);

            if (info.Length == 0)
            {
                diagnostics.Add(new Diagnostic(
                    "CSHARP-TYPE-001",
                    DiagnosticSeverity.Warning,
                    "No top-level type declaration was detected by the lexical scanner.",
                    relative));

                return new CSharpScriptModel(
                    EntityId.FromStableKey(relative),
                    relative,
                    null,
                    types,
                    diagnostics);
            }

            if (t_byteBuffer == null)
                t_byteBuffer = new byte[1024 * 512];

            if (t_charBuffer == null)
                t_charBuffer = new char[1024 * 512];

            int charsRead;

            using (var handle = File.OpenHandle(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileOptions.SequentialScan))
            {
                int bytesRead =
                    RandomAccess.Read(handle, t_byteBuffer, 0);

                charsRead =
                    System.Text.Encoding.UTF8.GetChars(
                        t_byteBuffer.AsSpan(0, bytesRead),
                        t_charBuffer);
            }

            ReadOnlySpan<char> text =
                t_charBuffer.AsSpan(0, charsRead);

            var code = BuildCodeMask(text);

            ns = FindNamespace(text, code);

            ScanTypes(
                text,
                code,
                0,
                text.Length,
                relative,
                ns,
                types,
                null);

            if (types.Count == 0)
            {
                diagnostics.Add(new Diagnostic(
                    "CSHARP-TYPE-001",
                    DiagnosticSeverity.Warning,
                    "No top-level type declaration was detected by the lexical scanner.",
                    relative));
            }

            return new CSharpScriptModel(
                EntityId.FromStableKey(relative),
                relative,
                ns,
                types,
                diagnostics);
        }

        private static bool[] BuildCodeMask(ReadOnlySpan<char> text)
        {
            var code = new bool[text.Length];

            LexicalState state = LexicalState.Normal;
            bool escaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                switch (state)
                {
                    case LexicalState.Normal:
                        if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                        {
                            state = LexicalState.LineComment;
                            i++;
                            continue;
                        }

                        if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                        {
                            state = LexicalState.BlockComment;
                            i++;
                            continue;
                        }

                        if (c == '@' && i + 1 < text.Length && text[i + 1] == '"')
                        {
                            state = LexicalState.VerbatimString;
                            i++;
                            continue;
                        }

                        if (c == '"')
                        {
                            state = LexicalState.String;
                            escaped = false;
                            continue;
                        }

                        if (c == '\'')
                        {
                            state = LexicalState.Char;
                            escaped = false;
                            continue;
                        }

                        code[i] = true;
                        break;

                    case LexicalState.String:
                        if (escaped)
                        {
                            escaped = false;
                            continue;
                        }

                        if (c == '\\')
                        {
                            escaped = true;
                            continue;
                        }

                        if (c == '"')
                            state = LexicalState.Normal;

                        break;

                    case LexicalState.Char:
                        if (escaped)
                        {
                            escaped = false;
                            continue;
                        }

                        if (c == '\\')
                        {
                            escaped = true;
                            continue;
                        }

                        if (c == '\'')
                            state = LexicalState.Normal;

                        break;

                    case LexicalState.VerbatimString:
                        if (c == '"' &&
                            i + 1 < text.Length &&
                            text[i + 1] == '"')
                        {
                            i++;
                            continue;
                        }

                        if (c == '"')
                            state = LexicalState.Normal;

                        break;

                    case LexicalState.LineComment:
                        if (c == '\n')
                            state = LexicalState.Normal;

                        break;

                    case LexicalState.BlockComment:
                        if (c == '*' &&
                            i + 1 < text.Length &&
                            text[i + 1] == '/')
                        {
                            state = LexicalState.Normal;
                            i++;
                        }

                        break;
                }
            }

            return code;
        }

        private static string? FindNamespace(
            ReadOnlySpan<char> text,
            bool[] code)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!code[i] ||
                    !IsIdentifierStart(text[i]))
                    continue;

                if (!MatchesWord(text, code, i, "namespace"))
                    continue;

                int p = i + "namespace".Length;

                while (p < text.Length &&
                       (!code[p] || char.IsWhiteSpace(text[p])))
                    p++;

                int start = p;

                while (p < text.Length &&
                       code[p] &&
                       (char.IsLetterOrDigit(text[p]) ||
                        text[p] == '_' ||
                        text[p] == '.'))
                {
                    p++;
                }

                if (p > start)
                    return text.Slice(start, p - start).ToString();

                return null;
            }

            return null;
        }

        private static void ScanTypes(
            ReadOnlySpan<char> text,
            bool[] code,
            int start,
            int end,
            string relative,
            string? ns,
            List<CSharpTypeModel> types,
            string? containingType)
        {
            int i = start;

            while (i < end)
            {
                if (!code[i])
                {
                    i++;
                    continue;
                }

                if (!IsIdentifierStart(text[i]))
                {
                    i++;
                    continue;
                }

                CSharpTypeKind kind;
                int keywordLength;

                if (MatchesWord(text, code, i, "class"))
                {
                    kind = CSharpTypeKind.Class;
                    keywordLength = 5;
                }
                else if (MatchesWord(text, code, i, "struct"))
                {
                    kind = CSharpTypeKind.Struct;
                    keywordLength = 6;
                }
                else if (MatchesWord(text, code, i, "interface"))
                {
                    kind = CSharpTypeKind.Interface;
                    keywordLength = 9;
                }
                else if (MatchesWord(text, code, i, "enum"))
                {
                    kind = CSharpTypeKind.Enum;
                    keywordLength = 4;
                }
                else
                {
                    i++;
                    continue;
                }

                int nameStart = i + keywordLength;

                while (nameStart < end &&
                       (!code[nameStart] ||
                        char.IsWhiteSpace(text[nameStart])))
                    nameStart++;

                if (nameStart >= end ||
                    !IsIdentifierStart(text[nameStart]))
                {
                    i += keywordLength;
                    continue;
                }

                int nameEnd = nameStart + 1;

                while (nameEnd < end &&
                       code[nameEnd] &&
                       IsIdentifierPart(text[nameEnd]))
                {
                    nameEnd++;
                }

                string name =
                    text.Slice(
                        nameStart,
                        nameEnd - nameStart).ToString();

                int brace =
                    FindNextCodeChar(
                        text,
                        code,
                        nameEnd,
                        end,
                        '{');

                if (brace < 0)
                {
                    i = nameEnd;
                    continue;
                }

                int bodyEnd =
                    FindMatchingBrace(
                        text,
                        code,
                        brace,
                        end);

                if (bodyEnd < 0)
                {
                    i = nameEnd;
                    continue;
                }

                int headerStart =
                    FindHeaderStart(
                        text,
                        code,
                        start,
                        i);

                var attributes =
                    ExtractAttributes(
                        text,
                        code,
                        headerStart,
                        i);

                var distinctAttributes =
                    attributes
                        .Select(x => x.Attribute)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                var required =
                    attributes
                        .SelectMany(x => x.RequiredComponents)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                string? baseText =
                    ExtractBaseType(
                        text,
                        code,
                        nameEnd,
                        brace);

                int line =
                    GetLineNumber(
                        text,
                        headerStart);

                var body =
                    text.Slice(
                        brace,
                        bodyEnd - brace + 1);

                var lifecycle =
                    new List<string>();

                for (int m = 0;
                     m < LifecycleMethods.Length;
                     m++)
                {
                    if (ContainsLifecycle(
                        body,
                        code,
                        brace,
                        LifecycleMethods[m]))
                    {
                        lifecycle.Add(
                            LifecycleMethods[m]);
                    }
                }

                var serialized =
                    ExtractSerializedFields(
                        text,
                        code,
                        brace + 1,
                        bodyEnd,
                        relative,
                        line);

                var model =
                    new CSharpTypeModel(
                        EntityId.FromStableKey(
                            containingType == null
                                ? $"{relative}:{name}"
                                : $"{relative}:{containingType}.{name}"),
                        name,
                        kind,
                        ns,
                        baseText,
                        distinctAttributes,
                        lifecycle.ToArray(),
                        required,
                        serialized.ToArray(),
                        relative,
                        line);

                types.Add(model);

                // Recursive nested-type discovery.
                ScanTypes(
                    text,
                    code,
                    brace + 1,
                    bodyEnd,
                    relative,
                    ns,
                    types,
                    containingType == null
                        ? name
                        : $"{containingType}.{name}");

                i = bodyEnd + 1;
            }
        }

        private static int FindHeaderStart(
            ReadOnlySpan<char> text,
            bool[] code,
            int regionStart,
            int typeKeyword)
        {
            int p = typeKeyword - 1;

            while (p >= regionStart)
            {
                if (!code[p])
                {
                    p--;
                    continue;
                }

                if (text[p] == '}' ||
                    text[p] == '{' ||
                    text[p] == ';')
                {
                    return p + 1;
                }

                p--;
            }

            return regionStart;
        }

        private static List<(string Attribute, List<string> RequiredComponents)>
            ExtractAttributes(
                ReadOnlySpan<char> text,
                bool[] code,
                int start,
                int end)
        {
            var result =
                new List<(string, List<string>)>();

            int p = start;

            while (p < end)
            {
                if (!code[p])
                {
                    p++;
                    continue;
                }

                if (text[p] != '[')
                {
                    p++;
                    continue;
                }

                int close =
                    FindNextCodeChar(
                        text,
                        code,
                        p + 1,
                        end,
                        ']');

                if (close < 0)
                    break;

                string content =
                    text.Slice(
                        p + 1,
                        close - p - 1).ToString().Trim();

                var req =
                    new List<string>();

                var match =
                    RequireRegex.Match(content);

                if (match.Success)
                    req.Add(
                        match.Groups["name"].Value);

                result.Add(
                    (content, req));

                p = close + 1;
            }

            return result;
        }

        private static string? ExtractBaseType(
            ReadOnlySpan<char> text,
            bool[] code,
            int nameEnd,
            int brace)
        {
            int colon = -1;

            for (int i = nameEnd; i < brace; i++)
            {
                if (code[i] && text[i] == ':')
                {
                    colon = i;
                    break;
                }
            }

            if (colon < 0)
                return null;

            var span =
                text.Slice(
                    colon + 1,
                    brace - colon - 1);

            var chars =
                new char[span.Length];

            int n = 0;

            for (int i = 0; i < span.Length; i++)
            {
                if (code[colon + 1 + i])
                    chars[n++] = span[i];
            }

            var value =
                new string(chars, 0, n).Trim();

            if (value.Length == 0)
                return null;

            int newline = value.IndexOf('\n');

            if (newline >= 0)
                value = value.Substring(0, newline).Trim();

            return value.Length == 0 ? null : value;
        }

        private static List<SerializedFieldModel>
            ExtractSerializedFields(
                ReadOnlySpan<char> text,
                bool[] code,
                int start,
                int end,
                string relative,
                int typeLine)
        {
            var result =
                new List<SerializedFieldModel>();

            int depth = 0;
            int statementStart = start;

            for (int i = start; i < end; i++)
            {
                if (!code[i])
                    continue;

                char c = text[i];

                if (c == '{')
                {
                    depth++;
                    continue;
                }

                if (c == '}')
                {
                    if (depth > 0)
                        depth--;

                    continue;
                }

                if (c != ';' || depth != 0)
                    continue;

                var statement =
                    text.Slice(
                        statementStart,
                        i - statementStart + 1);

                string line =
                    statement.ToString();

                bool serialized =
                    line.Contains(
                        "[SerializeField]",
                        StringComparison.Ordinal) ||
                    line.Contains(
                        "[UnityEngine.SerializeField]",
                        StringComparison.Ordinal) ||
                    line.Contains(
                        "[SerializeReference]",
                        StringComparison.Ordinal) ||
                    line.Contains(
                        "[UnityEngine.SerializeReference]",
                        StringComparison.Ordinal);

                bool isPublic =
                    Regex.IsMatch(
                        line,
                        @"\bpublic\s+");

                if (serialized || isPublic)
                {
                    var match =
                        Regex.Match(
                            line,
                            @"(?<type>[A-Za-z_][\w.<>,?\[\]]*(?:\s*\.\s*[A-Za-z_][\w]*)*)\s+(?<name>[A-Za-z_][\w]*)\s*(?:=[^;]*)?;$",
                            RegexOptions.Singleline);

                    if (match.Success)
                    {
                        string fieldName =
                            match.Groups["name"].Value;

                        string fieldType =
                            match.Groups["type"].Value.Trim();

                        bool isPrivate =
                            Regex.IsMatch(
                                line,
                                @"\bprivate\s+");

                        result.Add(
                            new SerializedFieldModel(
                                fieldName,
                                fieldType,
                                isPrivate,
                                relative,
                                typeLine));

                        if (result.Count >= 200)
                            break;
                    }
                }

                statementStart = i + 1;
            }

            return result;
        }

        private static bool ContainsLifecycle(
            ReadOnlySpan<char> body,
            bool[] code,
            int absoluteStart,
            string method)
        {
            for (int i = 0; i < body.Length; i++)
            {
                int absolute = absoluteStart + i;

                if (absolute < 0 ||
                    absolute >= code.Length ||
                    !code[absolute])
                {
                    continue;
                }

                if (i + method.Length > body.Length)
                    continue;

                bool matches = true;

                for (int j = 0; j < method.Length; j++)
                {
                    int sourceIndex = absolute + j;

                    if (sourceIndex >= code.Length ||
                        !code[sourceIndex] ||
                        body[i + j] != method[j])
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                    continue;

                int before = absolute - 1;
                int after = absolute + method.Length;

                if (before >= absoluteStart &&
                    IsIdentifierPart(body[i - 1]))
                {
                    continue;
                }

                if (after < absoluteStart + body.Length &&
                    IsIdentifierPart(body[i + method.Length]))
                {
                    continue;
                }

                int p = i + method.Length;

                while (p < body.Length &&
                       char.IsWhiteSpace(body[p]))
                {
                    p++;
                }

                if (p < body.Length &&
                    body[p] == '(')
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindMatchingBrace(
            ReadOnlySpan<char> text,
            bool[] code,
            int start,
            int end)
        {
            int depth = 0;

            for (int i = start; i < end; i++)
            {
                if (!code[i])
                    continue;

                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;

                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static int FindNextCodeChar(
            ReadOnlySpan<char> text,
            bool[] code,
            int start,
            int end,
            char target)
        {
            for (int i = start; i < end; i++)
            {
                if (code[i] && text[i] == target)
                    return i;
            }

            return -1;
        }

        private static bool MatchesWord(
            ReadOnlySpan<char> text,
            bool[] code,
            int index,
            string word)
        {
            if (index + word.Length > text.Length)
                return false;

            for (int i = 0; i < word.Length; i++)
            {
                if (!code[index + i] ||
                    text[index + i] != word[i])
                    return false;
            }

            int before = index - 1;
            int after = index + word.Length;

            if (before >= 0 &&
                IsIdentifierPart(text[before]))
                return false;

            if (after < text.Length &&
                IsIdentifierPart(text[after]))
                return false;

            return true;
        }

        private static bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static int GetLineNumber(
            ReadOnlySpan<char> text,
            int index)
        {
            int line = 1;

            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    line++;
            }

            return line;
        }
    }
}

