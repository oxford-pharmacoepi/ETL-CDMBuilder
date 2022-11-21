﻿SELECT DISTINCT SOURCE_CODE, TARGET_CONCEPT_ID
FROM SOURCE_TO_CONCEPT_MAP
WHERE SOURCE_VOCABULARY_ID = 3 AND TARGET_VOCABULARY_ID = 3 
	AND PRIMARY_MAP = 'Y'
	AND (INVALID_REASON IS NULL or INVALID_REASON = '')
	AND GETDATE() BETWEEN VALID_START_DATE and VALID_END_DATE
