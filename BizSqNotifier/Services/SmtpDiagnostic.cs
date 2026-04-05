using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// SMTP 진단 도구.
    /// 실제 메일 발송 없이 SMTP 서버 연결 가능 여부만 확인합니다.
    /// 설정 화면에서 "연결 진단" 버튼으로 호출 가능.
    /// </summary>
    public static class SmtpDiagnostic
    {
        /// <summary>
        /// 모든 지점의 SMTP 서버 포트 연결을 테스트합니다.
        /// </summary>
        public static List<DiagResult> DiagnoseAll()
        {
            var results = new List<DiagResult>();
            var repo = new BranchRepository();
            var branches = repo.GetAll();

            foreach (var br in branches)
            {
                results.Add(Diagnose(br));
            }
            return results;
        }

        /// <summary>
        /// 단일 지점의 SMTP 포트 연결을 테스트합니다.
        /// </summary>
        public static DiagResult Diagnose(BranchSmtpInfo branch)
        {
            var r = new DiagResult
            {
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                SmtpHost = branch.SmtpHost,
                SmtpPort = branch.SmtpPort,
                SmtpEmail = branch.SmtpEmail
            };

            // SMTP 호스트 미설정
            if (string.IsNullOrWhiteSpace(branch.SmtpHost))
            {
                r.Status = "SKIP";
                r.Message = "SMTP 호스트 미설정";
                return r;
            }

            // SMTP 이메일 미설정
            if (string.IsNullOrWhiteSpace(branch.SmtpEmail))
            {
                r.Status = "WARN";
                r.Message = "SMTP 이메일 미설정 (발송 불가)";
                return r;
            }

            // SMTP 비밀번호 미설정
            if (string.IsNullOrWhiteSpace(branch.SmtpPassword))
            {
                r.Status = "WARN";
                r.Message = "SMTP 비밀번호 미설정";
                return r;
            }

            // TCP 포트 연결 테스트 (메일 발송 없이)
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.BeginConnect(branch.SmtpHost, branch.SmtpPort, null, null);
                    var connected = connectTask.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                    if (connected && client.Connected)
                    {
                        r.Status = "OK";
                        r.Message = $"{branch.SmtpHost}:{branch.SmtpPort} 연결 성공";
                    }
                    else
                    {
                        r.Status = "FAIL";
                        r.Message = $"{branch.SmtpHost}:{branch.SmtpPort} 연결 시간 초과 (5초)";
                    }
                }
            }
            catch (Exception ex)
            {
                r.Status = "FAIL";
                r.Message = $"연결 실패: {ex.Message}";
            }

            return r;
        }

        /// <summary>진단 결과를 보기 좋은 텍스트로 포맷합니다.</summary>
        public static string FormatResults(List<DiagResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ SMTP 진단 결과 ═══");
            sb.AppendLine();

            int ok = 0, warn = 0, fail = 0, skip = 0;
            foreach (var r in results)
            {
                var icon = r.Status == "OK" ? "✓" : r.Status == "WARN" ? "△" : r.Status == "SKIP" ? "—" : "✗";
                sb.AppendLine($"  [{icon}] {r.BranchName} ({r.BranchCode})");
                sb.AppendLine($"      호스트: {r.SmtpHost ?? "(없음)"}:{r.SmtpPort}");
                sb.AppendLine($"      계정: {r.SmtpEmail ?? "(없음)"}");
                sb.AppendLine($"      결과: {r.Message}");
                sb.AppendLine();

                switch (r.Status)
                {
                    case "OK": ok++; break;
                    case "WARN": warn++; break;
                    case "FAIL": fail++; break;
                    default: skip++; break;
                }
            }

            sb.AppendLine($"═══ 합계: 정상={ok} / 경고={warn} / 실패={fail} / 스킵={skip} ═══");
            return sb.ToString();
        }
    }

    /// <summary>SMTP 진단 결과 모델.</summary>
    public sealed class DiagResult
    {
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpEmail { get; set; }
        public string Status { get; set; }  // OK / WARN / FAIL / SKIP
        public string Message { get; set; }
    }
}
