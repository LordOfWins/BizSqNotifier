/*
 * BizSqNotifier — 발송 로그 테이블
 * 기존 MOS DB에 신규 테이블 1개만 추가
 * 실행 환경: SQL Server 2012+
 */

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tb_mail_log]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[tb_mail_log]
    (
        [log_id]      INT IDENTITY(1,1) NOT NULL,           -- PK, 자동 증가
        [mail_type]   VARCHAR(20)       NOT NULL,           -- 입주/미납1차/미납2차/미납최종/갱신자동/갱신수동/퇴실
        [movein_id]   INT               NOT NULL,           -- tb_movein FK (논리적 참조)
        [invoice_id]  INT               NULL,               -- tb_invoice FK (미납 안내 시 사용)
        [cust_name]   NVARCHAR(100)     NOT NULL,           -- 회사명
        [email]       NVARCHAR(200)     NULL,               -- 수신 이메일
        [branch_id]   INT               NULL,               -- 지점 ID (SMTP 발송 계정 식별)
        [send_date]   DATE              NOT NULL,           -- 발송 날짜
        [send_time]   TIME              NOT NULL,           -- 발송 시각
        [status]      VARCHAR(10)       NOT NULL,           -- 성공 / 실패 / SKIP
        [error_msg]   NVARCHAR(500)     NULL,               -- 실패 시 에러 메시지
        [created_at]  DATETIME          NOT NULL DEFAULT GETDATE(),

        CONSTRAINT [PK_tb_mail_log] PRIMARY KEY CLUSTERED ([log_id] ASC)
    );

    -- 중복 발송 방지 및 조회 성능을 위한 인덱스
    CREATE NONCLUSTERED INDEX [IX_tb_mail_log_type_movein]
        ON [dbo].[tb_mail_log] ([mail_type], [movein_id], [send_date])
        INCLUDE ([status]);

    CREATE NONCLUSTERED INDEX [IX_tb_mail_log_invoice]
        ON [dbo].[tb_mail_log] ([mail_type], [invoice_id])
        INCLUDE ([status])
        WHERE [invoice_id] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_tb_mail_log_date]
        ON [dbo].[tb_mail_log] ([send_date] DESC)
        INCLUDE ([mail_type], [cust_name], [status]);

    PRINT 'tb_mail_log 테이블 생성 완료';
END
ELSE
BEGIN
    PRINT 'tb_mail_log 테이블이 이미 존재합니다. 건너뜁니다.';
END
GO
