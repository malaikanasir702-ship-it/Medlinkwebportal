-- Add ConsultationTranscripts table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConsultationTranscripts')
BEGIN
    CREATE TABLE [dbo].[ConsultationTranscripts] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AppointmentId] INT NOT NULL,
        [SpeakerId] NVARCHAR(MAX) NOT NULL,
        [SpeakerName] NVARCHAR(100) NOT NULL,
        [SpeakerRole] NVARCHAR(20) NOT NULL,
        [OriginalText] NVARCHAR(MAX) NOT NULL,
        [EnglishTranslation] NVARCHAR(MAX) NOT NULL,
        [UrduTranslation] NVARCHAR(MAX) NOT NULL,
        [DetectedLanguage] NVARCHAR(50) NULL,
        [Timestamp] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ConsultationTranscripts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConsultationTranscripts_Appointments_AppointmentId] 
            FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ConsultationTranscripts_AppointmentId] 
        ON [dbo].[ConsultationTranscripts] ([AppointmentId]);
        
    PRINT 'ConsultationTranscripts table created successfully';
END
ELSE
BEGIN
    PRINT 'ConsultationTranscripts table already exists';
END
GO
