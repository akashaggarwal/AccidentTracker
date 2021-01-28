CREATE TABLE [dbo].[AccidentProgram] (
    [Id]          INT           NOT NULL,
    [ProgramName] VARCHAR (100) NOT NULL,
    [URL] VARCHAR(2000) NOT NULL, 
    [EmailTemplateID] VARCHAR(200) NOT NULL, 
    [ProgramDescription] VARCHAR(200) NULL, 
    [ProgramDataElementDescriptions] VARCHAR(1000) NULL, 
    CONSTRAINT [PK_Program] PRIMARY KEY CLUSTERED ([Id] ASC)
);

