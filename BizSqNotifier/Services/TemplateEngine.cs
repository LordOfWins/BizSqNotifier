using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// HTML 템플릿 로드 및 {변수} 치환 엔진.
    /// - 템플릿 파일: Templates 폴더 내 .html 파일
    /// - 치환 키: {변수명} 형식 (단일 중괄호, 한글 키)
    /// - 제목(Subject) 문자열도 동일한 치환 로직 적용
    /// </summary>
    public sealed class TemplateEngine
    {
        private readonly string _templatesRoot;

        /// <summary>
        /// 템플릿 엔진을 초기화합니다.
        /// </summary>
        /// <param name="templatesRoot">
        /// 템플릿 폴더 경로. null이면 실행 파일 기준 Templates 폴더를 사용합니다.
        /// </param>
        public TemplateEngine(string templatesRoot = null)
        {
            _templatesRoot = templatesRoot
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        }

        #region 본문 템플릿 로드 + 치환

        /// <summary>
        /// HTML 템플릿 파일을 로드하고 {변수} 토큰을 치환합니다.
        /// </summary>
        /// <param name="fileName">템플릿 파일명 (예: MoveIn.html)</param>
        /// <param name="tokens">치환 딕셔너리 — key는 중괄호 없이 (예: "회사명")</param>
        /// <returns>치환 완료된 HTML 문자열</returns>
        public string LoadAndRender(string fileName, IDictionary<string, string> tokens)
        {
            var path = Path.Combine(_templatesRoot, fileName);

            if (!File.Exists(path))
            {
                AppLog.Warn("템플릿 파일 없음: " + path);
                return string.Empty;
            }

            var html = File.ReadAllText(path, Encoding.UTF8);
            return ReplaceTokens(html, tokens);
        }

        #endregion

        #region 제목(Subject) 치환

        /// <summary>
        /// 메일 제목 문자열에서 {변수}를 치환합니다.
        /// </summary>
        /// <param name="subjectTemplate">제목 템플릿 문자열</param>
        /// <param name="tokens">치환 딕셔너리</param>
        /// <returns>치환 완료된 제목 문자열</returns>
        public string RenderSubject(string subjectTemplate, IDictionary<string, string> tokens)
        {
            if (string.IsNullOrEmpty(subjectTemplate))
                return string.Empty;

            return ReplaceTokens(subjectTemplate, tokens);
        }

        #endregion

        #region 메일 유형별 제목 템플릿

        /// <summary>
        /// 메일 유형(mail_type)에 해당하는 기본 제목 템플릿을 반환합니다.
        /// 관리자 설정에서 커스텀 제목을 지정하지 않았을 때 사용합니다.
        /// </summary>
        public static string GetDefaultSubject(string mailType)
        {
            switch (mailType)
            {
                case MailTypes.MoveIn:
                    return "[모아] 회원이 되어주셔서 진심으로 감사드리며, 서비스 이용에 도움을 드리기 위하여 안내 드립니다.";

                case MailTypes.Unpaid1st:
                    return "[모아] {회사명} 회원이용료 미납에 따른 납부 안내 드립니다.";

                case MailTypes.Unpaid2nd:
                    return "[모아] {회사명} 회원이용료 미납, 연체에 따른 납부 재안내 드립니다";

                case MailTypes.UnpaidFinal:
                    return "[모아] {회사명} 회원이용료 미납, 연체에 따른 납부 최종 안내 드립니다";

                case MailTypes.RenewalManual:
                    return "[모아] {회사명} 회원이용계약 갱신 관련 안내 드립니다";

                case MailTypes.RenewalAuto:
                    return "[모아] {회사명} 회원이용계약 갱신 관련 안내드립니다";

                case MailTypes.MoveOut:
                    return "[모아] {회사명} 회원이용계약 종료에 따른 퇴실 절차 및 정산관련 안내드립니다.";

                default:
                    AppLog.Warn("알 수 없는 mail_type: " + mailType);
                    return "[모아] 안내 메일";
            }
        }

        #endregion

        #region 토큰 치환 핵심 로직

        /// <summary>
        /// 문자열 내 {키} 패턴을 딕셔너리 값으로 치환합니다.
        /// 매칭되지 않는 {키}는 그대로 남겨둡니다 (디버그 시 누락 변수 식별 용이).
        /// </summary>
        private static string ReplaceTokens(string template, IDictionary<string, string> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return template;

            var sb = new StringBuilder(template);
            foreach (var kv in tokens)
            {
                // {변수명} 형식으로 치환
                var placeholder = "{" + kv.Key + "}";
                sb.Replace(placeholder, kv.Value ?? string.Empty);
            }

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// 메일 유형 상수 — tb_mail_log.mail_type 값과 일치.
    /// </summary>
    public static class MailTypes
    {
        public const string MoveIn = "입주";
        public const string Unpaid1st = "미납1차";
        public const string Unpaid2nd = "미납2차";
        public const string UnpaidFinal = "미납최종";
        public const string RenewalManual = "갱신수동";
        public const string RenewalAuto = "갱신자동";
        public const string MoveOut = "퇴실";
    }

    /// <summary>
    /// 템플릿 파일명 상수.
    /// </summary>
    public static class TemplateFiles
    {
        public const string MoveIn = "MoveIn.html";
        public const string Unpaid1st = "Unpaid1st.html";
        public const string Unpaid2nd = "Unpaid2nd.html";
        public const string UnpaidFinal = "UnpaidFinal.html";
        public const string RenewalOffice = "RenewalOffice.html";
        public const string RenewalAuto = "RenewalAuto.html";
        public const string MoveOut = "MoveOut.html";
    }
}
