﻿{base},
Standard as (
SELECT distinct SOURCE_CODE, 1 as TARGET_CONCEPT_ID, TARGET_DOMAIN_ID, SOURCE_VALID_START_DATE, SOURCE_VALID_END_DATE
FROM Source_to_Standard
WHERE SOURCE_VOCABULARY_ID = 'JNJ_DEATH'
)

select distinct Standard.*
from Standard