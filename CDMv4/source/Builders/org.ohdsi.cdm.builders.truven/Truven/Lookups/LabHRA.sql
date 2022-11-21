﻿SELECT DISTINCT SOURCE_CODE collate Latin1_General_CS_AS, TARGET_CONCEPT_ID
FROM SOURCE_TO_CONCEPT_MAP
WHERE SOURCE_VOCABULARY_ID = 401 
AND TARGET_VOCABULARY_ID IN (0, 6)
union
SELECT DISTINCT LOWER(SOURCE_CODE)  collate Latin1_General_CS_AS, TARGET_CONCEPT_ID
FROM SOURCE_TO_CONCEPT_MAP
WHERE SOURCE_VOCABULARY_ID = 401 
AND TARGET_VOCABULARY_ID IN (0, 6)
