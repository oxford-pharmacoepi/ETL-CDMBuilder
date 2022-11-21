﻿using System.Collections.Generic;
using System.Data;
using org.ohdsi.cdm.framework.entities.Builder;
using org.ohdsi.cdm.framework.entities.Omop;
using org.ohdsi.cdm.framework.shared.Extensions;

namespace org.ohdsi.cdm.framework.core.Definitions
{
   public class LocationDefinition : EntityDefinition
   {
      public string Zip { get; set; }
      public string Country { get; set; }
      public string State { get; set; }
      public string SourceValue { get; set; }

      public override IEnumerable<IEntity> GetConcepts(Concept concept, IDataReader reader, KeyMasterOffset keyMaster)
      {
         var id = string.IsNullOrEmpty(Id) ? KeyMaster.GetLocationId() : int.Parse(reader[Id].ToString());
         yield return new Location
         {
            Id = id,
            State = reader.GetString(State),
            SourceValue = reader.GetString(SourceValue)
         };
      }
   }
}
