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
        // PATCH-001B.1R
        // Shared lexical state for structural scanning.
        private enum LexicalState
        {
            Normal,
            String,
            Char,
            LineComment,
            BlockComment
        }

        private static readonly Regex RequireRegex = new(@"RequireComponent\s*\(\s*typeof\s*\(\s*(?<name>[A-Za-z_][\w.]*)\s*\)\s*\)", RegexOptions.Compiled);
        
        private static readonly string[] LifecycleMethods = { "Awake", "OnEnable", "Start", "Update", "FixedUpdate", "LateUpdate", "OnDisable", "OnDestroy", "OnTriggerEnter", "OnCollisionEnter" };
        private static readonly Regex[] LifecycleRegexes = LifecycleMethods.Select(m => new Regex($@"\b{Regex.Escape(m)}\s*\(", RegexOptions.Compiled)).ToArray();

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
                diagnostics.Add(new Diagnostic("CSHARP-TYPE-001", DiagnosticSeverity.Warning, "No top-level type declaration was detected by the lexical scanner.", relative));
                return new CSharpScriptModel(EntityId.FromStableKey(relative), relative, null, types, diagnostics);
            }
            
            if (t_byteBuffer == null) t_byteBuffer = new byte[1024 * 512]; // up to 512KB files
            if (t_charBuffer == null) t_charBuffer = new char[1024 * 512];

            int charsRead = 0;
            using (var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan))
            {
                int bytesRead = RandomAccess.Read(handle, t_byteBuffer, 0);
                charsRead = System.Text.Encoding.UTF8.GetChars(t_byteBuffer.AsSpan(0, bytesRead), t_charBuffer);
            }
            
            ReadOnlySpan<char> textSpan = t_charBuffer.AsSpan(0, charsRead);
            
            int nsIdx = textSpan.IndexOf("namespace ", StringComparison.Ordinal);
            if (nsIdx >= 0)
            {
                int start = nsIdx + 10;
                while (start < textSpan.Length && char.IsWhiteSpace(textSpan[start])) start++;
                int end = start;
                while (end < textSpan.Length && (char.IsLetterOrDigit(textSpan[end]) || textSpan[end] == '_' || textSpan[end] == '.')) end++;
                if (end > start) ns = textSpan.Slice(start, end - start).ToString();
            }

            
            int i = 0;
            while (i < textSpan.Length)
            {
                
                
                if (textSpan[i] == 'c' || textSpan[i] == 's' || textSpan[i] == 'i' || textSpan[i] == 'e')
                {
                    bool isBoundary = (i == 0 || !char.IsLetterOrDigit(textSpan[i - 1]));
                    if (isBoundary)
                    {
                        CSharpTypeKind kind = CSharpTypeKind.Unknown;
                        int kindLen = 0;
                        var rem = textSpan.Slice(i);
                        
                        if (rem.StartsWith("class ", StringComparison.Ordinal) || rem.StartsWith("class\r", StringComparison.Ordinal) || rem.StartsWith("class\n", StringComparison.Ordinal) || rem.StartsWith("class\t", StringComparison.Ordinal)) { kind = CSharpTypeKind.Class; kindLen = 5; }
                        else if (rem.StartsWith("struct ", StringComparison.Ordinal) || rem.StartsWith("struct\r", StringComparison.Ordinal) || rem.StartsWith("struct\n", StringComparison.Ordinal) || rem.StartsWith("struct\t", StringComparison.Ordinal)) { kind = CSharpTypeKind.Struct; kindLen = 6; }
                        else if (rem.StartsWith("interface ", StringComparison.Ordinal) || rem.StartsWith("interface\r", StringComparison.Ordinal) || rem.StartsWith("interface\n", StringComparison.Ordinal) || rem.StartsWith("interface\t", StringComparison.Ordinal)) { kind = CSharpTypeKind.Interface; kindLen = 9; }
                        else if (rem.StartsWith("enum ", StringComparison.Ordinal) || rem.StartsWith("enum\r", StringComparison.Ordinal) || rem.StartsWith("enum\n", StringComparison.Ordinal) || rem.StartsWith("enum\t", StringComparison.Ordinal)) { kind = CSharpTypeKind.Enum; kindLen = 4; }
                        
                        if (kind != CSharpTypeKind.Unknown)
                        {
                            int attrSearchStart = i - 1;
                            while (attrSearchStart >= 0 && textSpan[attrSearchStart] != '}' && textSpan[attrSearchStart] != '{') attrSearchStart--;
                            attrSearchStart++;
                            
                            var attrs = new List<string>();
                            var required = new List<string>();
                            int bStart = -1;
                            for (int a = attrSearchStart; a < i; a++)
                            {
                                if (textSpan[a] == '[') bStart = a + 1;
                                else if (textSpan[a] == ']' && bStart >= 0)
                                {
                                    var attrContent = textSpan.Slice(bStart, a - bStart).ToString().Trim();
                                    attrs.Add(attrContent);
                                    var reqMatch = RequireRegex.Match(attrContent);
                                    if (reqMatch.Success) required.Add(reqMatch.Groups["name"].Value);
                                    bStart = -1;
                                }
                            }
                            
                            var distinctAttrs = attrs.Distinct(StringComparer.Ordinal).ToArray();
                            var distinctRequired = required.Distinct(StringComparer.Ordinal).ToArray();
                            
                            int nameStart = i + kindLen;
                            while (nameStart < textSpan.Length && char.IsWhiteSpace(textSpan[nameStart])) nameStart++;
                            int nameEnd = nameStart;
                            while (nameEnd < textSpan.Length && (char.IsLetterOrDigit(textSpan[nameEnd]) || textSpan[nameEnd] == '_')) nameEnd++;
                            
                            if (nameEnd > nameStart)
                            {
                                var name = textSpan.Slice(nameStart, nameEnd - nameStart).ToString();
                                
                                int colonIdx = -1;
                                int braceIdx = -1;
                                for (int scan = nameEnd; scan < textSpan.Length; scan++)
                                {
                                    if (textSpan[scan] == ':') { colonIdx = scan; }
                                    if (textSpan[scan] == '{') { braceIdx = scan; break; }
                                }
                                
                                string? baseText = null;
                                if (colonIdx >= 0 && colonIdx > nameEnd && (braceIdx == -1 || colonIdx < braceIdx))
                                {
                                    int bEnd = braceIdx >= 0 ? braceIdx : textSpan.Length;
                                    var baseSpan = textSpan.Slice(colonIdx + 1, bEnd - colonIdx - 1);
                                    
                                    int nl = baseSpan.IndexOf('\n');
                                    if (nl >= 0) baseSpan = baseSpan.Slice(0, nl);
                                    
                                    baseText = baseSpan.ToString().Trim();
                                    if (baseText.Length == 0) baseText = null;
                                }
                                
                                int line = 1;
                                for (int ln = 0; ln < i; ln++) if (t_charBuffer[ln] == '\n') line++;
                                
                                if (distinctAttrs.Length > 0)
                                {
                                    int firstAttrIdx = textSpan.Slice(attrSearchStart, i - attrSearchStart).IndexOf('[');
                                    if (firstAttrIdx >= 0)
                                    {
                                        int actualStart = attrSearchStart + firstAttrIdx;
                                        while (actualStart > 0 && char.IsWhiteSpace(textSpan[actualStart - 1])) actualStart--;
                                        line = 1;
                                        for (int ln = 0; ln < actualStart; ln++) if (t_charBuffer[ln] == '\n') line++;
                                    }
                                }
                                
                                var lifecycle = new List<string>();
                                var serialized = new List<SerializedFieldModel>();
                                
                                if (braceIdx >= 0)
                                {
                                    int bodyEnd = FindMatchingBrace(textSpan, braceIdx);
                                    if (bodyEnd > braceIdx)
                                    {
                                        var bodySpan = textSpan.Slice(braceIdx, bodyEnd - braceIdx + 1);
                                        for (int m = 0; m < LifecycleMethods.Length; m++)
                                        {
                                            if (LifecycleRegexes[m].EnumerateMatches(bodySpan).MoveNext())
                                                lifecycle.Add(LifecycleMethods[m]);
                                        }
                                        
                                        int b = 0;
                                        while (b < bodySpan.Length)
                                        {
                                            if (bodySpan[b] == ';')
                                            {
                                                int lineStart = b;
                                                while (lineStart > 0 && bodySpan[lineStart - 1] != '\n' && bodySpan[lineStart - 1] != '{' && bodySpan[lineStart - 1] != '}') lineStart--;
                                                var lineSpan = bodySpan.Slice(lineStart, b - lineStart + 1);
                                                
                                                bool isPublic = lineSpan.Contains("public ", StringComparison.Ordinal);
                                                bool isPrivate = lineSpan.Contains("private ", StringComparison.Ordinal);
                                                bool hasSerializeField = lineSpan.Contains("[SerializeField]", StringComparison.Ordinal) || lineSpan.Contains("[UnityEngine.SerializeField]", StringComparison.Ordinal);
                                                bool hasSerializeRef = lineSpan.Contains("[SerializeReference]", StringComparison.Ordinal) || lineSpan.Contains("[UnityEngine.SerializeReference]", StringComparison.Ordinal);
                                                
                                                if (isPublic || hasSerializeField || hasSerializeRef)
                                                {
                                                    int eqIdx = lineSpan.IndexOf('=');
                                                    int end = eqIdx >= 0 ? eqIdx : lineSpan.Length - 1; 
                                                    while (end > 0 && char.IsWhiteSpace(lineSpan[end - 1])) end--;
                                                    
                                                    int nStart = end;
                                                    while (nStart > 0 && (char.IsLetterOrDigit(lineSpan[nStart - 1]) || lineSpan[nStart - 1] == '_')) nStart--;
                                                    if (nStart < end && nStart > 0)
                                                    {
                                                        var fName = lineSpan.Slice(nStart, end - nStart).ToString();
                                                        
                                                        int tEnd = nStart;
                                                        while (tEnd > 0 && char.IsWhiteSpace(lineSpan[tEnd - 1])) tEnd--;
                                                        int tStart = tEnd;
                                                        while (tStart > 0 && !char.IsWhiteSpace(lineSpan[tStart - 1]) && lineSpan[tStart - 1] != ']') tStart--;
                                                        
                                                        if (tStart < tEnd)
                                                        {
                                                            var fType = lineSpan.Slice(tStart, tEnd - tStart).ToString();
                                                            serialized.Add(new SerializedFieldModel(fName, fType, isPrivate, relative, line));
                                                            if (serialized.Count >= 200) break;
                                                        }
                                                    }
                                                }
                                            }
                                            b++;
                                        }
                                        
                                        i = braceIdx + bodySpan.Length;
                                        types.Add(new CSharpTypeModel(EntityId.FromStableKey($"{relative}:{name}"), name, kind, ns, baseText, distinctAttrs, lifecycle.ToArray(), distinctRequired, serialized.ToArray(), relative, line));
                                        continue;
                                    }
                                }
                                
                                types.Add(new CSharpTypeModel(EntityId.FromStableKey($"{relative}:{name}"), name, kind, ns, baseText, distinctAttrs, lifecycle.ToArray(), distinctRequired, serialized.ToArray(), relative, line));
                            }
                        }
                    }
                }
                i++;
            }

            if (types.Count == 0)
                diagnostics.Add(new Diagnostic(
                    "CSHARP-TYPE-001", DiagnosticSeverity.Warning,
                    "No top-level type declaration was detected by the lexical scanner.", relative));

            return new CSharpScriptModel(EntityId.FromStableKey(relative), relative, ns, types, diagnostics);
        }

        private static int FindMatchingBrace(ReadOnlySpan<char> text, int start)
        {
            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];
                if (inString) { if (escaped) escaped = false; else if (c == '\\') escaped = true; else if (c == '"') inString = false; continue; }
                if (c == '"') { inString = true; continue; }
                if (c == '{') depth++;
                if (c == '}' && --depth == 0) return i;
            }
            return -1;
        }
    }
}
