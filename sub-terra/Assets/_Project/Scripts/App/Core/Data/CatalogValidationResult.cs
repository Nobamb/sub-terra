using System;
using System.Collections.Generic;
using System.Text;

namespace SubTerra.App.Core.Data
{
    public enum CatalogIssueSeverity
    {
        Error = 0,
        Warning = 1
    }

    /// <summary>단일 검증 이슈. 에셋 경로·필드명을 포함해 Editor/로그에서 위치를 바로 찾을 수 있게 한다.</summary>
    public sealed class CatalogValidationIssue
    {
        public CatalogIssueSeverity Severity { get; }
        public string AssetPath { get; }
        public string FieldName { get; }
        public string Message { get; }

        public CatalogValidationIssue(
            CatalogIssueSeverity severity,
            string assetPath,
            string fieldName,
            string message)
        {
            Severity = severity;
            AssetPath = assetPath ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            var path = string.IsNullOrEmpty(AssetPath) ? "(catalog)" : AssetPath;
            var field = string.IsNullOrEmpty(FieldName) ? "-" : FieldName;
            return $"[{Severity}] {path} :: {field} — {Message}";
        }
    }

    /// <summary>
    /// 검증 결과 집합. 첫 오류에서 중단하지 않고 이슈를 모은다.
    /// 중복 ID가 있으면 DictionaryInitialized는 false다.
    /// </summary>
    public sealed class CatalogValidationResult
    {
        private readonly List<CatalogValidationIssue> issues = new List<CatalogValidationIssue>();

        public bool DictionaryInitialized { get; private set; }

        public IReadOnlyList<CatalogValidationIssue> Issues => issues;

        public int ErrorCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < issues.Count; i++)
                {
                    if (issues[i].Severity == CatalogIssueSeverity.Error)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool IsValid => ErrorCount == 0 && DictionaryInitialized;

        public void Add(CatalogValidationIssue issue)
        {
            if (issue != null)
            {
                issues.Add(issue);
            }
        }

        public void AddError(string assetPath, string fieldName, string message)
        {
            Add(new CatalogValidationIssue(CatalogIssueSeverity.Error, assetPath, fieldName, message));
        }

        public void AddWarning(string assetPath, string fieldName, string message)
        {
            Add(new CatalogValidationIssue(CatalogIssueSeverity.Warning, assetPath, fieldName, message));
        }

        public void SetDictionaryInitialized(bool initialized)
        {
            DictionaryInitialized = initialized;
        }

        /// <summary>Bootstrap 포트용 단일 사유 문자열. 민감 경로·세이브 원문은 넣지 않는다.</summary>
        public string ToFailureReason()
        {
            if (IsValid)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.Append("catalog validation failed (").Append(ErrorCount).Append(" error(s))");
            var reported = 0;
            for (var i = 0; i < issues.Count && reported < 3; i++)
            {
                if (issues[i].Severity != CatalogIssueSeverity.Error)
                {
                    continue;
                }

                sb.Append("; ").Append(issues[i].Message);
                reported++;
            }

            return sb.ToString();
        }

        public string FormatAll()
        {
            if (issues.Count == 0)
            {
                return "No issues.";
            }

            var sb = new StringBuilder();
            for (var i = 0; i < issues.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine();
                }

                sb.Append(issues[i]);
            }

            return sb.ToString();
        }
    }
}
