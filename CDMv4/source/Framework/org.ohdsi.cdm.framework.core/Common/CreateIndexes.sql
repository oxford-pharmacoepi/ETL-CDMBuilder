

---- create dimension constraints

ALTER TABLE [dbo].[VOCABULARY] ADD CONSTRAINT [PK_VOCABULARY] PRIMARY KEY CLUSTERED 
(
    [VOCABULARY_ID]
)
WITH (DATA_COMPRESSION = PAGE);
GO

--

ALTER TABLE [dbo].[RELATIONSHIP] ADD CONSTRAINT [PK_RELATIONSHIP] PRIMARY KEY CLUSTERED 
(
    [RELATIONSHIP_ID]
)
WITH (DATA_COMPRESSION = PAGE);
GO

--

ALTER TABLE [dbo].[CONCEPT] ADD CONSTRAINT [PK_CONCEPT] PRIMARY KEY CLUSTERED 
(
	[CONCEPT_ID] 
)
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [XCOC_ID_LEV] ON [dbo].[CONCEPT] 
(
	[CONCEPT_ID] ASC,
	[CONCEPT_LEVEL] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [XCON_2CD] ON [dbo].[CONCEPT] 
(
	[CONCEPT_CLASS] ASC,
	[CONCEPT_ID] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [XCON_2ID] ON [dbo].[CONCEPT] 
(
	[VOCABULARY_ID] ASC,
	[CONCEPT_ID] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [XCON_3CD] ON [dbo].[CONCEPT] 
(
	[VOCABULARY_ID] ASC,
	[CONCEPT_CLASS] ASC,
	[CONCEPT_ID] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

--

ALTER TABLE [dbo].[CONCEPT_ANCESTOR] ADD CONSTRAINT [PK_CONCEPT_ANCESTOR] PRIMARY KEY CLUSTERED 
(
    [ANCESTOR_CONCEPT_ID],
    [DESCENDANT_CONCEPT_ID]
)
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [X_AN_AN] ON [dbo].[CONCEPT_ANCESTOR] 
(
	[ANCESTOR_CONCEPT_ID] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [X_AN_D] ON [dbo].[CONCEPT_ANCESTOR] 
(
	[DESCENDANT_CONCEPT_ID] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[CONCEPT_ANCESTOR] WITH CHECK ADD CONSTRAINT [CONCEPT_ANCESTOR_FK] FOREIGN KEY([ANCESTOR_CONCEPT_ID])
REFERENCES [dbo].[CONCEPT] ([CONCEPT_ID])
GO

ALTER TABLE [dbo].[CONCEPT_ANCESTOR] WITH CHECK ADD CONSTRAINT [CONCEPT_DESCENDANT_FK] FOREIGN KEY([DESCENDANT_CONCEPT_ID])
REFERENCES [dbo].[CONCEPT] ([CONCEPT_ID])
GO

--

ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] ADD CONSTRAINT [PK_CONCEPT_RELATIONSHIP] PRIMARY KEY CLUSTERED 
(
    [CONCEPT_ID_1],
    [CONCEPT_ID_2],
    [RELATIONSHIP_ID]
)
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [XID12] ON [dbo].[CONCEPT_RELATIONSHIP] 
(
	[CONCEPT_ID_1] ASC,
	[CONCEPT_ID_2] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [XID21] ON [dbo].[CONCEPT_RELATIONSHIP] 
(
	[CONCEPT_ID_2] ASC,
	[CONCEPT_ID_1] ASC
) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] WITH CHECK ADD CONSTRAINT [CONCEPT_REL_PARENT_FK] FOREIGN KEY([CONCEPT_ID_1])
REFERENCES [dbo].[CONCEPT] ([CONCEPT_ID])
GO
ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] CHECK CONSTRAINT [CONCEPT_REL_PARENT_FK]
GO

ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] WITH CHECK ADD CONSTRAINT [CONCEPT_REL_CHILD_FK] FOREIGN KEY([CONCEPT_ID_2])
REFERENCES [dbo].[CONCEPT] ([CONCEPT_ID])
GO
ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] CHECK CONSTRAINT [CONCEPT_REL_CHILD_FK]
GO

ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] WITH CHECK ADD CONSTRAINT [CONCEPT_REL_REL_TYPE_FK] FOREIGN KEY([RELATIONSHIP_ID])
REFERENCES [dbo].[RELATIONSHIP] ([RELATIONSHIP_ID])
GO
ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] CHECK CONSTRAINT [CONCEPT_REL_REL_TYPE_FK]
GO

ALTER TABLE [dbo].[CONCEPT_RELATIONSHIP] ADD DEFAULT ('12/31/2099') FOR [VALID_END_DATE]
GO

--

ALTER TABLE [dbo].[CONCEPT_SYNONYM] ADD CONSTRAINT [PK_CONCEPT_SYNONYM] PRIMARY KEY CLUSTERED 
(
    [CONCEPT_SYNONYM_ID]
)
WITH (DATA_COMPRESSION = PAGE);
GO


ALTER TABLE [dbo].[CONCEPT_SYNONYM] WITH CHECK ADD CONSTRAINT [CONCEPT_SYNONYM_CONCEPT_FK] FOREIGN KEY([CONCEPT_ID])
REFERENCES [dbo].[CONCEPT] ([CONCEPT_ID])
GO
ALTER TABLE [dbo].[CONCEPT_SYNONYM] CHECK CONSTRAINT [CONCEPT_SYNONYM_CONCEPT_FK]
GO

--

ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] ADD CONSTRAINT [PK_SOURCE_TO_CONCEPT_MAP] PRIMARY KEY CLUSTERED 
(
    [SOURCE_TO_CONCEPT_MAP_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_TARGET_CONCEPT_ID] ON [dbo].[SOURCE_TO_CONCEPT_MAP]
(
	[TARGET_CONCEPT_ID] 
) 
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX IX_VOCAB_IDS ON [dbo].[SOURCE_TO_CONCEPT_MAP] 
(
    [TARGET_VOCABULARY_ID], [SOURCE_VOCABULARY_ID]
)
INCLUDE ([SOURCE_CODE], [TARGET_CONCEPT_ID])
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX IX_SOURCECODE_TARGET_SOURCE ON [dbo].[SOURCE_TO_CONCEPT_MAP] 
(
    [SOURCE_CODE], [TARGET_VOCABULARY_ID], [SOURCE_VOCABULARY_ID]
)
INCLUDE ([TARGET_CONCEPT_ID])
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] WITH CHECK ADD CONSTRAINT [SOURCE_TO_CONCEPT_CONCEPT] FOREIGN KEY([TARGET_CONCEPT_ID])
REFERENCES [dbo].[CONCEPT] ([CONCEPT_ID])
GO
ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] CHECK CONSTRAINT [SOURCE_TO_CONCEPT_CONCEPT]
GO

ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] WITH CHECK ADD CONSTRAINT [SOURCE_TO_CONCEPT_SOURCE_VOCAB] FOREIGN KEY([SOURCE_VOCABULARY_ID])
REFERENCES [dbo].[VOCABULARY] ([VOCABULARY_ID])
GO
ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] CHECK CONSTRAINT [SOURCE_TO_CONCEPT_SOURCE_VOCAB]
GO

ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] WITH CHECK ADD CONSTRAINT [SOURCE_TO_CONCEPT_TARGET_VOCAB] FOREIGN KEY([TARGET_VOCABULARY_ID])
REFERENCES [dbo].[VOCABULARY] ([VOCABULARY_ID])
GO
ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] CHECK CONSTRAINT [SOURCE_TO_CONCEPT_TARGET_VOCAB]
GO

ALTER TABLE [dbo].[SOURCE_TO_CONCEPT_MAP] ADD DEFAULT ('12/31/2099') FOR [VALID_END_DATE]
GO

---------- fact constraints

ALTER TABLE [dbo].[LOCATION] ADD CONSTRAINT [PK_LOCATION] PRIMARY KEY CLUSTERED 
(
    [LOCATION_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [LOCATION_SOURCE_VALUE_IDX] ON [dbo].[LOCATION]
(
	[LOCATION_SOURCE_VALUE] ASC
)	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

--

ALTER TABLE [dbo].[PROVIDER] ADD CONSTRAINT [PROVIDER_PK] PRIMARY KEY CLUSTERED 
(
    [PROVIDER_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO


ALTER TABLE [dbo].[PERSON] ADD CONSTRAINT [PK_PERSON] PRIMARY KEY CLUSTERED 
(
    [PERSON_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[PERSON]  WITH CHECK ADD  CONSTRAINT [PERSON_LOCATION_FK] FOREIGN KEY([LOCATION_ID])
REFERENCES [dbo].[LOCATION] ([LOCATION_ID])
GO

ALTER TABLE [dbo].[PERSON]  WITH CHECK ADD  CONSTRAINT [PERSON_PROVIDER_FK] FOREIGN KEY([PROVIDER_ID])
REFERENCES [dbo].[PROVIDER] ([PROVIDER_ID])
GO


ALTER TABLE [dbo].[VISIT_OCCURRENCE]  WITH CHECK ADD  CONSTRAINT [VISIT_OCCURRENCE_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_VISIT_OCCURRENCE_PERSON_ID] ON [dbo].[VISIT_OCCURRENCE]
(
	[PERSON_ID] ASC,
	[VISIT_OCCURRENCE_ID] ASC
)	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_VISIT_OCCURRENCE_VISIT_OCCURRENCE_ID] ON [dbo].[VISIT_OCCURRENCE]
(
	[VISIT_OCCURRENCE_ID] ASC
)
INCLUDE ( 	[PLACE_OF_SERVICE_CONCEPT_ID],
	[VISIT_START_DATE],
	[VISIT_END_DATE]) 	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[PROCEDURE_OCCURRENCE] ADD CONSTRAINT [PK_PROCEDURE_OCCURRENCE] PRIMARY KEY NONCLUSTERED 
(
    [PROCEDURE_OCCURRENCE_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[PROCEDURE_OCCURRENCE]  WITH CHECK ADD  CONSTRAINT [PROCEDURE_OCCURRENCE_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_PROCEDURE_OCCURRENCE_PERSON_ID] ON [dbo].[PROCEDURE_OCCURRENCE]
(
	[PERSON_ID] ASC,
	[PROCEDURE_OCCURRENCE_ID] ASC
)	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_PROCEDURE_OCCURRENCE_PROCEDURE_CONCEPT_ID] ON [dbo].[PROCEDURE_OCCURRENCE]
(
	[PROCEDURE_CONCEPT_ID] ASC
)
INCLUDE ( [PROCEDURE_DATE] )
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[OBSERVATION]  WITH CHECK ADD  CONSTRAINT [OBSERVATION_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_OBSERVATION_PERIOD_PERSON_ID] ON [dbo].[OBSERVATION_PERIOD]
(
	[PERSON_ID] ASC,
	[OBSERVATION_PERIOD_START_DATE] ASC,
	[OBSERVATION_PERIOD_END_DATE] ASC
)	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE UNIQUE CLUSTERED INDEX [IX_OBSERVATION_PERSON_ID] ON [dbo].[OBSERVATION]
(
	[PERSON_ID] ASC,
	[OBSERVATION_ID] ASC
)	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_OBSERVATION_OBSERVATION_CONCEPT_ID] ON [dbo].[OBSERVATION]
(
	[OBSERVATION_CONCEPT_ID] ASC
)
INCLUDE ( [OBSERVATION_DATE],
	[VALUE_AS_NUMBER],
	[VALUE_AS_STRING],
	[RANGE_LOW],
	[RANGE_HIGH]) 	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO
--

ALTER TABLE [dbo].[DRUG_EXPOSURE] ADD CONSTRAINT [PK_DRUG_EXPOSURE] PRIMARY KEY NONCLUSTERED 
(
    [DRUG_EXPOSURE_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[DRUG_EXPOSURE]  WITH CHECK ADD  CONSTRAINT [DRUG_EXPOSURE_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_DRUG_EXPOSURE_PERSON_ID] ON [dbo].[DRUG_EXPOSURE]
(
	[PERSON_ID] ASC,
	[DRUG_EXPOSURE_ID] ASC
)	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_DRUG_EXPOSURE_DRUG_CONCEPT_ID] ON [dbo].[DRUG_EXPOSURE]
(
	[DRUG_CONCEPT_ID] ASC
)
INCLUDE ( 	[DRUG_EXPOSURE_START_DATE],
	[DRUG_EXPOSURE_END_DATE],
	[REFILLS],
	[QUANTITY],
	[DAYS_SUPPLY]) 	
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[CONDITION_OCCURRENCE]  WITH CHECK ADD  CONSTRAINT [CONDITION_OCCURRENCE_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_CONDITION_OCCURRENCE_PERSON_ID] ON [dbo].[CONDITION_OCCURRENCE]
(
	[PERSON_ID] ASC,
	[CONDITION_OCCURRENCE_ID] ASC
)
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX [IX_CONDITION_OCCURRENCE_CONCEPT_ID] ON [dbo].[CONDITION_OCCURRENCE]
(
	[CONDITION_CONCEPT_ID] ASC
)
INCLUDE ( [CONDITION_START_DATE], [CONDITION_END_DATE] )
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);


ALTER TABLE [dbo].[CONDITION_ERA]  WITH CHECK ADD  CONSTRAINT [CONDITION_ERA_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_CONDITION_ERA_PERSON_ID] ON [dbo].[CONDITION_ERA]
(
	[PERSON_ID] ASC,
	[CONDITION_ERA_ID] ASC
)
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);

CREATE NONCLUSTERED INDEX [IX_CONDITION_ERA_CONDITION_CONCEPT_ID] ON [dbo].[CONDITION_ERA]
(
	[CONDITION_CONCEPT_ID] ASC
)
INCLUDE ( [CONDITION_ERA_START_DATE], [CONDITION_ERA_END_DATE] )
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[DEATH]  WITH CHECK ADD  CONSTRAINT [DEATH_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE CLUSTERED INDEX [PK_DEATH] ON [dbo].[DEATH]
(
	[PERSON_ID] ASC,
	[CAUSE_OF_DEATH_CONCEPT_ID] ASC
)
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[DRUG_COST] ADD CONSTRAINT [PK_DRUG_COST] PRIMARY KEY CLUSTERED 
(
    [DRUG_EXPOSURE_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

ALTER TABLE [dbo].[DRUG_COST]  WITH CHECK ADD  CONSTRAINT [DRUG_COST_DRUG_EXPOSURE_FK] FOREIGN KEY([DRUG_EXPOSURE_ID])
REFERENCES [dbo].[DRUG_EXPOSURE] ([DRUG_EXPOSURE_ID])
GO

ALTER TABLE [dbo].[DRUG_ERA]  WITH CHECK ADD  CONSTRAINT [DRUG_ERA_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE UNIQUE CLUSTERED INDEX [IX_DRUG_ERA_PERSON_ID] ON [dbo].[DRUG_ERA]
(
	[PERSON_ID] ASC,
	[DRUG_ERA_ID] ASC
)
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_DRUG_ERA_DRUG_CONCEPT_ID] ON [dbo].[DRUG_ERA]
(
	[DRUG_CONCEPT_ID] ASC
)
INCLUDE ( [DRUG_ERA_START_DATE], [DRUG_ERA_END_DATE] ) 
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE CLUSTERED INDEX [pk_procedure_cost] ON [dbo].[PROCEDURE_COST]
(
	[PROCEDURE_OCCURRENCE_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]


ALTER TABLE [dbo].[PROCEDURE_COST]  WITH CHECK ADD  CONSTRAINT [PROCEDURE_COST_PROCEDURE_FK] FOREIGN KEY([PROCEDURE_OCCURRENCE_ID])
REFERENCES [dbo].[PROCEDURE_OCCURRENCE] ([PROCEDURE_OCCURRENCE_ID])
GO
--

ALTER TABLE [dbo].[CARE_SITE] ADD CONSTRAINT [PK_CARE_SITE] PRIMARY KEY CLUSTERED 
(
    [CARE_SITE_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

CREATE INDEX [COHORT_CONCEPT_ID] ON [dbo].[COHORT] 
( [COHORT_CONCEPT_ID] )
INCLUDE ([COHORT_START_DATE], [COHORT_END_DATE], [SUBJECT_ID])
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO

CREATE CLUSTERED INDEX [IX_COHORT_COHORT_CONCEPT_ID] ON [dbo].[COHORT]
(
	[COHORT_CONCEPT_ID] ASC,
	[SUBJECT_ID] ASC
)
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO


ALTER TABLE [dbo].[ORGANIZATION] ADD CONSTRAINT [PK_ORGANIZATION] PRIMARY KEY CLUSTERED 
(
    [ORGANIZATION_ID] 
)	
WITH (DATA_COMPRESSION = PAGE);
GO

--ALTER TABLE [dbo].[ORGANIZATION]  WITH CHECK ADD  CONSTRAINT [ORGANIZATION_LOCATION_FK] FOREIGN KEY([LOCATION_ID])
--REFERENCES [dbo].[LOCATION] ([LOCATION_ID])
--GO

CREATE CLUSTERED INDEX [pk_payer_plan_period] ON [dbo].[PAYER_PLAN_PERIOD]
(
	[PERSON_ID] ASC
)
WITH (DATA_COMPRESSION = PAGE);

ALTER TABLE [dbo].[PAYER_PLAN_PERIOD]  WITH CHECK ADD  CONSTRAINT [PAYER_PLAN_PERIOD_PERSON_FK] FOREIGN KEY([PERSON_ID])
REFERENCES [dbo].[PERSON] ([PERSON_ID])
GO

CREATE INDEX IX_PERSON_ID_START ON [dbo].[PAYER_PLAN_PERIOD]
( 
    PERSON_ID 
)
INCLUDE ( PAYER_PLAN_PERIOD_START_DATE )
WITH (SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
GO


CREATE TABLE dbo.SNOMED_TO_MEDDRA
	(
	SNOMED_CONCEPT_ID int NOT NULL,
	MEDDRA_CONCEPT_ID int NOT NULL
	)  ON [PRIMARY]
GO

INSERT INTO dbo.SNOMED_TO_MEDDRA
select distinct c1.concept_id as snomed_concept_id, c2.concept_id as meddra_concept_id
from concept c1
	inner join concept_ancestor ca1 on c1.concept_id = ca1.descendant_concept_id
	inner join concept c2 on ca1.ancestor_concept_id = c2.concept_id
where c1.vocabulary_id = 1
	and c2.vocabulary_id = 15
	and c2.concept_level in (2, 3, 4)

GO

CREATE UNIQUE CLUSTERED INDEX IX_SNOMED_TO_MEDDRA ON dbo.SNOMED_TO_MEDDRA
	(
	SNOMED_CONCEPT_ID,
	MEDDRA_CONCEPT_ID
	) WITH( STATISTICS_NORECOMPUTE = OFF, data_compression = page, sort_in_tempdb = on, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE TABLE dbo.RXNORM_TO_ATC
	(
	RXNORM_CONCEPT_ID int NOT NULL,
	ATC_CONCEPT_ID int NOT NULL
	)  ON [PRIMARY]
GO

INSERT INTO dbo.RXNORM_TO_ATC
select distinct c1.concept_id as rxnorm_concept_id,
	c2.concept_id as atc_concept_id
from concept c1
inner join concept_ancestor ca1 on c1.concept_id = ca1.descendant_concept_id
	and c1.vocabulary_id IN (8,22)
inner join concept c2 on ca1.ancestor_concept_id = c2.concept_id
where (c2.vocabulary_id = 21 and len(c2.concept_code) in (1, 3, 5)) 
	or (c2.vocabulary_id = 8 and c2.concept_level = 2)
GO

CREATE UNIQUE CLUSTERED INDEX IX_RXNORM_TO_ATC ON dbo.RXNORM_TO_ATC
(
	RXNORM_CONCEPT_ID,
	ATC_CONCEPT_ID
) WITH( STATISTICS_NORECOMPUTE = OFF, data_compression = page, sort_in_tempdb = on, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE TABLE dbo.PROCEDURE_TO_SNOMED
	(
	PROCEDURE_CONCEPT_ID int NOT NULL,
	SNOMED_CONCEPT_ID int NOT NULL
	)  ON [PRIMARY]
GO

INSERT INTO dbo.PROCEDURE_TO_SNOMED
select distinct c1.concept_id as procedure_concept_id,
	c2.concept_id as snomed_concept_id
from concept c1
inner join concept_ancestor ca1 on c1.concept_id = ca1.descendant_concept_id
inner join concept c2 on ca1.ancestor_concept_id = c2.concept_id
where (c2.vocabulary_id = 1 and c2.concept_level = 2)
GO

CREATE UNIQUE CLUSTERED INDEX IX_PROCEDURE_TO_SNOMED ON dbo.PROCEDURE_TO_SNOMED
(
	PROCEDURE_CONCEPT_ID,
	SNOMED_CONCEPT_ID
) WITH( STATISTICS_NORECOMPUTE = OFF, data_compression = page, sort_in_tempdb = on, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO