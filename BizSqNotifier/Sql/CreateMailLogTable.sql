IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tb_mail_log]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[tb_mail_log]
    (
        [log_id]       INT IDENTITY(1,1) NOT NULL,
        [mail_type]    VARCHAR(20)       NOT NULL,
        [movein_id]    INT               NOT NULL,
        [invoice_id]   INT               NULL,
        [cust_name]    NVARCHAR(100)     NOT NULL,
        [email]        NVARCHAR(200)     NULL,
        [branch_code]  VARCHAR(16)       NULL,
        [send_date]    DATE              NOT NULL,
        [send_time]    TIME              NOT NULL,
        [status]       VARCHAR(10)       NOT NULL,
        [error_msg]    NVARCHAR(500)     NULL,
        [created_at]   DATETIME          NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_tb_mail_log] PRIMARY KEY CLUSTERED ([log_id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_mail_log_type_movein]
        ON [dbo].[tb_mail_log] ([mail_type], [movein_id], [send_date]) INCLUDE ([status]);
    CREATE NONCLUSTERED INDEX [IX_mail_log_invoice]
        ON [dbo].[tb_mail_log] ([mail_type], [invoice_id]) INCLUDE ([status]) WHERE [invoice_id] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_mail_log_date]
        ON [dbo].[tb_mail_log] ([send_date] DESC) INCLUDE ([mail_type], [cust_name], [status]);

    PRINT 'tb_mail_log 생성 완료';
END
ELSE
    PRINT 'tb_mail_log 이미 존재';
