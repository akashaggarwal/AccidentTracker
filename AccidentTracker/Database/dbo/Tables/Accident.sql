CREATE TABLE [dbo].[Accident] (
    [Id]             BIGINT         IDENTITY (1, 1) NOT NULL,
    [ProgramID]      INT            NOT NULL,
    [AccidentID]     VARCHAR (20)   NOT NULL,
    [AccidentDate]   DATE           NOT NULL,
    [Name]           VARCHAR (1000) NOT NULL,
    [InsertDateTime] DATETIME       NOT NULL,
    CONSTRAINT [PK_Accident] PRIMARY KEY CLUSTERED ([Id] ASC), 
    CONSTRAINT [FK_Accident_ToProgram] FOREIGN KEY ([ProgramID]) REFERENCES [AccidentProgram]([Id])
);

