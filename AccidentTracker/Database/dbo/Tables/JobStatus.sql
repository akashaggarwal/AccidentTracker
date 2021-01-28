CREATE TABLE [dbo].[JobStatus] (
    [Id]                BIGINT         IDENTITY (1, 1) NOT NULL,
    [ProgramID]         INT            NOT NULL,
    [LastRunDateTime]   DATETIME       NOT NULL,
    [LastRunStatus]     CHAR (1)       NOT NULL,
    [LastRunErrorState] VARCHAR (100)  NOT NULL,
    [LastRunError]      VARCHAR (1600) NOT NULL,
    [NewAccidentCount]  INT            NOT NULL,
    [ExportDateTime]    DATETIME       NULL,
    CONSTRAINT [PK_JobStatus] PRIMARY KEY CLUSTERED ([Id] ASC)
);

