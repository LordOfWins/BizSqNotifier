using System;
using System.Data.SqlClient;
using BizSqNotifier.Models;

namespace BizSqNotifier.Data
{
    /// <summary>tb_mail_log CRUD (발송 로그).</summary>
    public sealed class MailLogRepository
    {
        private const string InsertSql = @"
INSERT INTO dbo.tb_mail_log
    (mail_type, movein_id, invoice_id, cust_name, email, branch_id, send_date, send_time, status, error_msg)
VALUES
    (@mail_type, @movein_id, @invoice_id, @cust_name, @email, @branch_id, @send_date, @send_time, @status, @error_msg);";

        public void Insert(MailLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            using (var conn = DbManager.CreateConnection())
            using (var cmd = new SqlCommand(InsertSql, conn))
            {
                cmd.Parameters.AddWithValue("@mail_type", entry.MailType);
                cmd.Parameters.AddWithValue("@movein_id", entry.MoveInId);
                cmd.Parameters.AddWithValue("@invoice_id", (object)entry.InvoiceId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cust_name", entry.CustName);
                cmd.Parameters.AddWithValue("@email", (object)entry.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@branch_id", (object)entry.BranchId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@send_date", entry.SendDate);
                cmd.Parameters.AddWithValue("@send_time", entry.SendTime);
                cmd.Parameters.AddWithValue("@status", entry.Status);
                cmd.Parameters.AddWithValue("@error_msg", (object)entry.ErrorMessage ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
