CREATE TABLE [dbo].[Subscriber] (
    [Id]           BIGINT        IDENTITY (1, 1) NOT NULL,
    [FirstName]    VARCHAR (100) NOT NULL,
    [LastName]     VARCHAR (100) NOT NULL,
    [EmailAddress] VARCHAR (100) NULL,
    [Phone]        VARCHAR (50)  NULL,
    [NotifyEmail]  BIT           NOT NULL,
    [NotifySMS]    BIT           NOT NULL,
    CONSTRAINT [PK_Subscribers] PRIMARY KEY CLUSTERED ([Id] ASC)
);

