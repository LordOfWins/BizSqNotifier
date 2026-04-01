using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier.Data
{
    /// <summary>
    /// tb_branch 조회 — 지점별 SMTP 설정 및 지점 정보 로드.
    /// [ESTIMATED] tb_branch의 SMTP 관련 컬럼명은 기존 MOS DB 기준 추정값입니다.
    ///            실제 DB 스키마 확인 후 컬럼명을 조정해야 할 수 있습니다.
    /// </summary>
    public sealed class BranchRepository
    {
        // [ESTIMATED] tb_branch SMTP 컬럼명 — 실제 DB 확인 필요
        // 기존 MOS VB.NET 소스에서 사용하는 컬럼명 기준으로 추정:
        //   smtp_host, smtp_port, smtp_ssl, smtp_user, smtp_pwd, smtp_from, smtp_name
        //   bank_accnt, bank_holder, br_name
        private const string SelectByIdSql = @"
SELECT
    br_id,
    br_name,
    smtp_host,
    smtp_port,
    smtp_ssl,
    smtp_user,
    smtp_pwd,
    smtp_from,
    smtp_name,
    bank_accnt,
    bank_holder
FROM dbo.tb_branch
WHERE br_id = @br_id;";

        private const string SelectAllSql = @"
SELECT
    br_id,
    br_name,
    smtp_host,
    smtp_port,
    smtp_ssl,
    smtp_user,
    smtp_pwd,
    smtp_from,
    smtp_name,
    bank_accnt,
    bank_holder
FROM dbo.tb_branch
ORDER BY br_name;";

        /// <summary>
        /// 지점 ID로 SMTP 설정을 조회합니다.
        /// </summary>
        public BranchSmtpInfo GetByBranchId(int branchId)
        {
            var list = DbManager.ExecuteReader(
                SelectByIdSql,
                MapRow,
                new SqlParameter("@br_id", branchId));
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// 전체 지점의 SMTP 설정 목록을 조회합니다.
        /// </summary>
        public List<BranchSmtpInfo> GetAll()
        {
            return DbManager.ExecuteReader(SelectAllSql, MapRow);
        }

        /// <summary>
        /// SqlDataReader → BranchSmtpInfo 매핑.
        /// </summary>
        private static BranchSmtpInfo MapRow(SqlDataReader reader)
        {
            return new BranchSmtpInfo
            {
                BranchId = DbManager.GetSafeInt(reader, "br_id"),
                BranchName = DbManager.GetSafeString(reader, "br_name"),
                SmtpHost = DbManager.GetSafeString(reader, "smtp_host"),
                SmtpPort = DbManager.GetSafeInt(reader, "smtp_port", 587),
                EnableSsl = DbManager.GetSafeInt(reader, "smtp_ssl", 1) == 1,
                SmtpUser = DbManager.GetSafeString(reader, "smtp_user"),
                SmtpPassword = DbManager.GetSafeString(reader, "smtp_pwd"),
                FromAddress = DbManager.GetSafeString(reader, "smtp_from"),
                FromDisplayName = DbManager.GetSafeString(reader, "smtp_name"),
                BankAccount = DbManager.GetSafeString(reader, "bank_accnt"),
                BankHolder = DbManager.GetSafeString(reader, "bank_holder")
            };
        }
    }
}
