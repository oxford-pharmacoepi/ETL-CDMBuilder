CREATE INDEX IF NOT EXISTS idx_procedure_person_id  ON procedure_occurrence  (person_id ASC);
CLUSTER procedure_occurrence  USING idx_procedure_person_id ;
CREATE INDEX IF NOT EXISTS idx_procedure_concept_id ON procedure_occurrence (procedure_concept_id ASC);
CREATE INDEX IF NOT EXISTS idx_procedure_visit_id ON procedure_occurrence (visit_occurrence_id ASC);