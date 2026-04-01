using System;
using System.Collections.Generic;
using System.IO;

namespace BizSqNotifier.Services
{
    /// <summary>HTML 템플릿 로드 및 플레이스홀더 치환.</summary>
    public sealed class TemplateEngine
    {
        private readonly string _templatesRoot;

        public TemplateEngine(string templatesRoot = null)
        {
            _templatesRoot = templatesRoot ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        }

        public string LoadAndRender(string fileName, IDictionary<string, string> tokens)
        {
            var path = Path.Combine(_templatesRoot, fileName);
            if (!File.Exists(path))
            {
                AppLog.Warn("템플릿 없음: " + path);
                return string.Empty;
            }

            var html = File.ReadAllText(path);
            if (tokens == null) return html;

            foreach (var kv in tokens)
            {
                var key = "{{" + kv.Key + "}}";
                html = html.Replace(key, kv.Value ?? string.Empty);
            }

            return html;
        }
    }
}
