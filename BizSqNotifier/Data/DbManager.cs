using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using BizSqNotifier.Config;
using BizSqNotifier.Services;

namespace BizSqNotifier.Data
{
    /// <summary>
    /// SQL Server 연결 팩토리 및 헬퍼 메서드.
    /// 모든 DB 접근은 이 클래스를 통해 SqlConnection을 생성합니다.
    /// </summary>
    public static class DbManager
    {
        // 기본 재시도 횟수 (네트워크 불안정 대비)
        private const int DefaultRetryCount = 3;
        // 재시도 간 대기 시간(ms)
        private const int RetryDelayMs = 1000;

        #region Connection Factory

        /// <summary>
        /// App.config의 연결 문자열로 SqlConnection을 생성합니다. (열리지 않은 상태)
        /// </summary>
        public static SqlConnection CreateConnection()
        {
            var cs = ConfigurationManager.ConnectionStrings[AppSettings.ConnectionStringName];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    $"connectionStrings에 '{AppSettings.ConnectionStringName}' 항목이 없거나 비어 있습니다.");
            }
            return new SqlConnection(cs.ConnectionString);
        }

        /// <summary>
        /// SqlConnection을 생성하고 Open 상태로 반환합니다.
        /// </summary>
        public static SqlConnection CreateOpenConnection()
        {
            var conn = CreateConnection();
            conn.Open();
            return conn;
        }

        #endregion

        #region Connection Test

        /// <summary>
        /// DB 연결 테스트. 성공 시 true, 실패 시 false + out 에러 메시지.
        /// 설정 화면 "연결 테스트" 버튼에서 호출합니다.
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var conn = CreateConnection())
                {
                    conn.Open();
                    // 간단한 쿼리로 실제 통신 확인
                    using (var cmd = new SqlCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                AppLog.Error("DB 연결 테스트 실패", ex);
                return false;
            }
        }

        /// <summary>
        /// 임의의 연결 문자열로 DB 연결 테스트. (설정 변경 전 검증용)
        /// </summary>
        public static bool TestConnection(string connectionString, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT 1", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                AppLog.Error("DB 연결 테스트 실패 (커스텀 연결문자열)", ex);
                return false;
            }
        }

        #endregion

        #region Query Helpers

        /// <summary>
        /// SELECT 쿼리를 실행하고 DataTable로 반환합니다.
        /// parameterized query를 위해 SqlParameter 배열을 전달합니다.
        /// </summary>
        public static DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            var dt = new DataTable();
            ExecuteWithRetry(() =>
            {
                using (var conn = CreateConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value ?? System.DBNull.Value });
                    }
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        conn.Open();
                        adapter.Fill(dt);
                    }
                }
            });
            return dt;
        }

        /// <summary>
        /// INSERT / UPDATE / DELETE 쿼리를 실행하고 영향받은 행 수를 반환합니다.
        /// </summary>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            int result = 0;
            ExecuteWithRetry(() =>
            {
                using (var conn = CreateConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value ?? System.DBNull.Value });
                    }
                    conn.Open();
                    result = cmd.ExecuteNonQuery();
                }
            });
            return result;
        }

        /// <summary>
        /// 단일 값(스칼라)을 반환하는 쿼리를 실행합니다.
        /// </summary>
        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            object result = null;
            ExecuteWithRetry(() =>
            {
                using (var conn = CreateConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value ?? System.DBNull.Value });
                    }
                    conn.Open();
                    result = cmd.ExecuteScalar();
                }
            });
            return result;
        }

        /// <summary>
        /// SqlDataReader 기반 조회. 호출자가 각 행을 매핑하는 Func을 전달합니다.
        /// </summary>
        public static List<T> ExecuteReader<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
        {
            var list = new List<T>();
            ExecuteWithRetry(() =>
            {
                list.Clear();
                using (var conn = CreateConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value ?? DBNull.Value });
                        }
                    }
                    conn.Open();
                    using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            list.Add(mapper(reader));
                        }
                    }
                }
            });
            return list;
        }

        #endregion

        #region Retry Logic

        /// <summary>
        /// 네트워크 일시 장애 대비 재시도 래퍼.
        /// SqlException 발생 시 최대 DefaultRetryCount회 재시도합니다.
        /// </summary>
        private static void ExecuteWithRetry(Action action)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (SqlException ex)
                {
                    attempt++;
                    if (attempt >= DefaultRetryCount)
                    {
                        AppLog.Error($"DB 작업 실패 (재시도 {attempt}회 소진)", ex);
                        throw;
                    }
                    // 일시적 오류(네트워크/타임아웃) 계열만 재시도
                    if (IsTransientError(ex))
                    {
                        AppLog.Warn($"DB 일시 오류 — {RetryDelayMs}ms 후 재시도 ({attempt}/{DefaultRetryCount}): {ex.Message}");
                        System.Threading.Thread.Sleep(RetryDelayMs);
                    }
                    else
                    {
                        throw; // 구문 오류 등은 재시도 불필요
                    }
                }
            }
        }

        /// <summary>
        /// SQL Server 일시적 오류 번호 판별.
        /// </summary>
        private static bool IsTransientError(SqlException ex)
        {
            // 참고: https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues
            // -2: Timeout, 53: Network path not found,
            // 10054: Connection reset, 10060: Connection timeout,
            // 40613: Database unavailable, 40197: Service error
            switch (ex.Number)
            {
                case -2:     // Timeout expired
                case 53:     // Network path not found
                case 121:    // Semaphore timeout
                case 258:    // Wait timeout
                case 10054:  // Connection forcibly closed
                case 10060:  // Connection attempt timed out
                case 40197:  // Service encountered an error
                case 40501:  // Service is busy
                case 40613:  // Database not currently available
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Safe Reader Helpers

        /// <summary>SqlDataReader에서 안전하게 string을 읽습니다.</summary>
        public static string GetSafeString(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        /// <summary>SqlDataReader에서 안전하게 int를 읽습니다.</summary>
        public static int GetSafeInt(SqlDataReader reader, string column, int defaultValue = 0)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
        }

        /// <summary>SqlDataReader에서 안전하게 int?를 읽습니다.</summary>
        public static int? GetSafeNullableInt(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        /// <summary>SqlDataReader에서 안전하게 decimal을 읽습니다.</summary>
        public static decimal GetSafeDecimal(SqlDataReader reader, string column, decimal defaultValue = 0m)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDecimal(ordinal);
        }

        /// <summary>SqlDataReader에서 안전하게 DateTime을 읽습니다.</summary>
        public static DateTime? GetSafeDateTime(SqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        /// <summary>SqlDataReader에서 안전하게 DateTime을 읽습니다 (non-nullable, 기본값 지정).</summary>
        public static DateTime GetSafeDateTimeValue(SqlDataReader reader, string column, DateTime defaultValue)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDateTime(ordinal);
        }

        #endregion
    }
}
