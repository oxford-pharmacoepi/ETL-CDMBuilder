﻿using System.Collections.Generic;
using System.Data;
using org.ohdsi.cdm.framework.entities.Builder;
using org.ohdsi.cdm.framework.entities.Omop;
using org.ohdsi.cdm.framework.shared.Extensions;

namespace org.ohdsi.cdm.framework.core.Definitions
{
    public class CohortDefinition : EntityDefinition
    {
        public string StopReason { get; set; }

        public override IEnumerable<IEntity> GetConcepts(Concept concept, IDataReader reader, KeyMasterOffset keyMaster)
        {
            var personId = reader.GetLong(PersonId);
            var startDate = reader.GetDateTime(StartDate);
            var endDate = reader.GetDateTime(EndDate);

            var e = new Entity
            {
                IsUnique = false,
                PersonId = personId.Value,
                SourceValue = " ",
                ConceptId = concept.Fields[0].DefaultConceptId.Value,
                TypeConceptId = concept.Fields[0].DefaultTypeId.Value,
                StartDate = startDate,
                EndDate = endDate
            };

            yield return new Cohort(e)
            {
                Id = reader.GetLong(Id).Value
            };
        }
    }
}
