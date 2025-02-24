IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatSessions]') AND type in (N'U'))
	DROP TABLE [dbo].[ChatSessions]
GO

CREATE TABLE [dbo].[ChatSessions](
	[SessionId] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime2](7) NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ChatSessions] ADD PRIMARY KEY CLUSTERED 
(
	[SessionId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ChatSessions] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO