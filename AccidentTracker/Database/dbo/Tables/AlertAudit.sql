CREATE TABLE [dbo].[AlertAudit] (
    [Id]             BIGINT         IDENTITY (1, 1) NOT NULL,
    [ProgramID]      INT            NOT NULL,
    [SubscriberID]   BIGINT         NOT NULL,
    [message]        VARCHAR (2000) NOT NULL,
    [InsertDateTime] DATETIME       CONSTRAINT [DF_AlertAudit_InsertDateTime] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_AlertAudit] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AlertAudit_Program] FOREIGN KEY ([ProgramID]) REFERENCES [dbo].[AccidentProgram] ([Id]),
    CONSTRAINT [FK_AlertAudit_Subscriber] FOREIGN KEY ([SubscriberID]) REFERENCES [dbo].[Subscriber] ([Id])
);

