using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier.Data
{
    /// <summary>tb_branch 조회 — 지점별 SMTP + 계좌 정보.</summary>
    public sealed class BranchRepository
    {
        private const string SelectByCodeSql = @"
SELECT br_code, br_name, smtp, smtp_email, smtp_pwd, bank_accnt, bank_holder
FROM dbo.tb_branch
WHERE br_code = @br_code;";

        private const string SelectAllSql = @"
SELECT br_code, br_name, smtp, smtp_email, smtp_pwd, bank_accnt, bank_holder
FROM dbo.tb_branch
ORDER BY br_name;";

        public BranchSmtpInfo GetByBranchCode(string branchCode)
        {
            if (string.IsNullOrEmpty(branchCode)) return null;
            var list = DbManager.ExecuteReader(SelectByCodeSql, MapRow,
                new SqlParameter("@br_code", branchCode));
            return list.Count > 0 ? list[0] : null;
        }

        public List<BranchSmtpInfo> GetAll()
        {
            return DbManager.ExecuteReader(SelectAllSql, MapRow);
        }

        private static BranchSmtpInfo MapRow(SqlDataReader reader)
        {
            return new BranchSmtpInfo
            {
                BranchCode   = DbManager.GetSafeString(reader, "br_code"),
                BranchName   = DbManager.GetSafeString(reader, "br_name"),
                SmtpHost     = DbManager.GetSafeString(reader, "smtp"),
                SmtpEmail    = DbManager.GetSafeString(reader, "smtp_email"),
                SmtpPassword = DbManager.GetSafeString(reader, "smtp_pwd"),
                BankAccount  = DbManager.GetSafeString(reader, "bank_accnt"),
                BankHolder   = DbManager.GetSafeString(reader, "bank_holder")
            };
        }
    }
}
