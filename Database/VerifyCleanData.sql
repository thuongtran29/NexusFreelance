-- Read-only content quality checks for the curated dataset and later additions.
SET NOCOUNT ON;
IF DB_NAME()<>'NexusFreelanceMvp' THROW 52050,'Choose the website database NexusFreelanceMvp.',1;
IF EXISTS(SELECT 1 FROM dbo.Users WHERE Email LIKE 'v3-%' OR Email LIKE 'repair-%' OR Email LIKE 'contractv2-%')
    THROW 52051,'Old automated fixture accounts remain.',1;
IF EXISTS(SELECT 1 FROM dbo.Projects WHERE Title LIKE N'QA %' OR Title LIKE N'Kiểm thử giao diện%' OR Title LIKE N'Dự án B2B Platform %')
    THROW 52052,'Old generated fixture projects remain.',1;
IF EXISTS(SELECT 1 FROM dbo.Projects WHERE LEN(LTRIM(RTRIM(Description)))<100 OR Budget<=0 OR DeadlineUtc<CreatedAtUtc OR Industry IS NULL)
    THROW 52053,'A project lacks usable scope, budget, industry or dates.',1;
IF EXISTS(SELECT 1 FROM dbo.FreelancerProfiles WHERE IsPublic=1 AND (LEN(LTRIM(RTRIM(Bio)))<80 OR LEN(LTRIM(RTRIM(CoreRole)))<3 OR Industry IS NULL OR Location IS NULL))
    THROW 52054,'A public freelancer profile is incomplete.',1;
IF EXISTS(SELECT 1 FROM dbo.ContractVersions WHERE HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONVERT(varchar(max),SnapshotJson COLLATE Latin1_General_100_CI_AS_SC_UTF8)))<>CONVERT(varbinary(32),Hash,2))
    THROW 52055,'A contract snapshot hash differs from its UTF-8 content.',1;
IF EXISTS(SELECT 1 FROM dbo.WorkspaceFileVersions WHERE HASHBYTES('SHA2_256',Data)<>CONVERT(varbinary(32),Sha256,2) OR DATALENGTH(Data)<>ByteLength)
    THROW 52056,'A demonstration handover file is incomplete or has the wrong checksum.',1;
IF EXISTS(SELECT 1 FROM dbo.SkillTracks WHERE Code LIKE 'V3%' OR Code LIKE 'QA%' OR Code LIKE 'TEST%')
    THROW 52057,'Old fixture skill tracks remain.',1;
SELECT (SELECT COUNT(*) FROM dbo.Users) AS Accounts,
       (SELECT COUNT(*) FROM dbo.Projects) AS Projects,
       (SELECT COUNT(*) FROM dbo.Contracts) AS Contracts,
       (SELECT COUNT(*) FROM dbo.FreelancerGroups) AS Groups,
       (SELECT COUNT(*) FROM dbo.PortfolioItems) AS PortfolioItems,
       (SELECT COUNT(*) FROM dbo.Tasks) AS Tasks,
       (SELECT COUNT(*) FROM dbo.WorkspaceFileVersions) AS HandoverFiles,
       (SELECT COUNT(*) FROM dbo.SkillTracks) AS SkillTracks,
       (SELECT COUNT(*) FROM dbo.SkillExamQuestions) AS IntroQuestions;
PRINT 'CLEAN_DATA_QUALITY_PASSED';
