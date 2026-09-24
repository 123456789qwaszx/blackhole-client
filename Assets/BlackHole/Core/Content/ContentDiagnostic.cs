using System.Collections.Generic;

namespace BlackHole.Core
{
    // 콘텐츠 오류 하나. Path가 핵심이다 — "Session.TimeLimit"처럼 고칠 자리를 바로 가리킨다.
    public sealed class ContentDiagnostic
    {
        public string Path { get; }
        public string Message { get; }

        public ContentDiagnostic(string path, string message)
        {
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString() => Path.Length == 0 ? Message : $"{Path}: {Message}";
    }

    // 로드 결과. 오류가 하나라도 있으면 Content는 null이다(부분 통과 금지).
    public sealed class ContentLoadResult
    {
        public GameContent Content { get; }
        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }
        public bool Succeeded => Content != null;

        internal ContentLoadResult(GameContent content, List<ContentDiagnostic> diagnostics)
        {
            Content = content;
            Diagnostics = diagnostics.AsReadOnly();
        }
    }
}
