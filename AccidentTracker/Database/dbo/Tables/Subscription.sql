CREATE TABLE [dbo].[Subscription] (
    [Id]           BIGINT IDENTITY (1, 1) NOT NULL,
    [ProgramId]    INT    NOT NULL,
    [SubscriberID] BIGINT NOT NULL,
    CONSTRAINT [PK_Subscription] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Subscription_Program] FOREIGN KEY ([ProgramId]) REFERENCES [dbo].[AccidentProgram] ([Id]),
    CONSTRAINT [FK_Subscription_Subscriber] FOREIGN KEY ([SubscriberID]) REFERENCES [dbo].[Subscriber] ([Id])
);

