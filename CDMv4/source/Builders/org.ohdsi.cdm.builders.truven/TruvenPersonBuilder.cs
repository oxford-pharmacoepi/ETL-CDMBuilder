﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using org.ohdsi.cdm.framework.core.Base;
using org.ohdsi.cdm.framework.entities.Omop;
using org.ohdsi.cdm.framework.shared.Extensions;
using org.ohdsi.cdm.framework.shared.Validators;

namespace org.ohdsi.cdm.builders.truven
{
   /// <summary>
   ///  Implementation of PersonBuilder for Truven CCAE/MDCR/MDCD, based on CDM Build spec
   /// </summary>
   public class TruvenPersonBuilder : PersonBuilder
   {
      private readonly Dictionary<Guid, VisitOccurrence> rawVisits = new Dictionary<Guid, VisitOccurrence>();
      private readonly Dictionary<Guid, List<IEntity>> medicalRecords = new Dictionary<Guid, List<IEntity>>();

      private readonly Dictionary<string, HashSet<VisitOccurrence>> fachidVisits = new Dictionary<string, HashSet<VisitOccurrence>>();
      private readonly Dictionary<string, HashSet<VisitOccurrence>> caseidVisits = new Dictionary<string, HashSet<VisitOccurrence>>();

     
      public TruvenPersonBuilder(ChunkBuilder chunkBuilder)
         : base(chunkBuilder)
      {
      }

      /// <summary>
      /// Build person entity and all person related entities like: DrugExposures, ConditionOccurrences, ProcedureOccurrences... from raw data sets 
      /// </summary>
      public override void Build()
      {
         var person = BuildPerson(personRecords.ToList());
         if (person == null) return;

         var observationPeriods = BuildObservationPeriods(person.ObservationPeriodGap, observationPeriodsRaw.ToArray()).ToArray();

         if (Excluded(person, observationPeriods)) return;

         var payerPlanPeriods = BuildPayerPlanPeriods(payerPlanPeriodsRaw.ToArray(), null).ToArray();
         var visitOccurrences = new Dictionary<long, VisitOccurrence>();

         foreach (var visitOccurrence in BuildVisitOccurrences(visitOccurrencesRaw.ToArray(), observationPeriods))
         {
            if (visitOccurrence.IdUndefined)
            {
               visitOccurrence.Id = chunkBuilder.Chunk.KeyMasterOffset.VisitOccurrenceId;
               visitOccurrence.IdUndefined = false;
            }
            visitOccurrences.Add(visitOccurrence.Id, visitOccurrence);
         }

         var conditionOccurrences = BuildConditionOccurrences(conditionOccurrencesRaw.ToArray(), visitOccurrences, observationPeriods).ToArray();
         var procedureOccurrences = BuildProcedureOccurrences(procedureOccurrencesRaw.ToArray(), visitOccurrences, observationPeriods).ToArray();
         foreach (var procedureOccurrence in procedureOccurrences)
         {
            procedureOccurrence.Id = chunkBuilder.Chunk.KeyMasterOffset.ProcedureOccurrenceId;
         }

         var observations = BuildObservations(observationsRaw.ToArray(), visitOccurrences, observationPeriods).ToArray();
         var drugExposures = BuildDrugExposure(drugExposuresRaw, procedureOccurrences, visitOccurrences, observationPeriods).ToArray();

         // set corresponding PlanPeriodIds to drug exposure entities and procedure occurrence entities
         SetPayerPlanPeriodId(payerPlanPeriods, drugExposures, procedureOccurrences);

         // set corresponding ProviderIds
         SetProviderIds(drugExposures);
         SetProviderIds(conditionOccurrences);
         SetProviderIds(procedureOccurrences);
         SetProviderIds(observations);

         var drugEra = BuildDrugEra(drugExposures).ToArray();
         var conditionEra = BuildConditionEra(conditionOccurrences).ToArray();

         var death = BuildDeath(deathRecords.ToArray(), visitOccurrences, observationPeriods);
         var drugCosts = BuildDrugCosts(drugExposures).ToArray();
         var procedureCosts = BuildProcedureCosts(procedureOccurrences).ToArray();

         // push built entities to ChunkBuilder for further save to CDM database
         AddToChunk(person, death, observationPeriods, payerPlanPeriods, drugExposures, drugEra, drugCosts,
            conditionOccurrences, conditionEra, procedureOccurrences, procedureCosts, observations, visitOccurrences.Values.ToArray(), new Cohort[0]);
      }

      /// <summary>
      /// 	Projects Enumeration of drug exposure from the raw set of drug exposure & procedure occurrence entities. 
      /// 	During build:
      ///	overide TypeConceptId per CDM Mapping spec. 
      /// </summary>
      /// <param name="drugExposure">raw set of drug exposures entities</param>
      /// <param name="procedureOccurrence">set of procedure occurrence entities</param>
      /// <param name="visitOccurrences">the visit occurrences entities for current person</param>
      /// <param name="observationPeriods">the observation periods entities for current person</param>
      /// <returns>Enumeration of drug exposure entities</returns>
      private IEnumerable<DrugExposure> BuildDrugExposure(IEnumerable<DrugExposure> drugExposure, IEnumerable<ProcedureOccurrence> procedureOccurrence, Dictionary<long, VisitOccurrence> visitOccurrences, ObservationPeriod[] observationPeriods)
      {
         var drugs = new Dictionary<Guid, List<DrugExposure>>();
         var drugClaims = new Dictionary<Guid, List<DrugExposure>>();

         foreach (var de in drugExposure)
         {
            // 38000175 (Prescription dispensed in pharmacy
            // 38000176 (Prescription dispensed through mail order)
            if (de.TypeConceptId == 38000175 || de.TypeConceptId == 38000176)
            {
               if (!drugClaims.ContainsKey(de.SourceRecordGuid))
                  drugClaims.Add(de.SourceRecordGuid, new List<DrugExposure>());

               drugClaims[de.SourceRecordGuid].Add(de);
               continue;
            }

            if (!drugs.ContainsKey(de.SourceRecordGuid))
               drugs.Add(de.SourceRecordGuid, new List<DrugExposure>());

            drugs[de.SourceRecordGuid].Add(de);
         }

         // Create drug exposure entities from procedure occurrence
         foreach (var po in procedureOccurrence)
         {
            if (!drugs.ContainsKey(po.SourceRecordGuid)) continue;

            var de = drugs[po.SourceRecordGuid].FirstOrDefault(d => d.SourceValue == po.SourceValue);
            if (de == null) continue;
            if (de.ConceptId > 0)
            {
               de.StartDate = po.StartDate;
               de.EndDate = null;
               de.ProviderKey = po.ProviderKey;
               de.VisitOccurrenceId = po.VisitOccurrenceId;
               de.RelevantConditionConceptId = po.ReleventConditionConceptId;

               de.Refills = null;

               // overide procedure occurrence TypeConceptId to the corresponding drug exposures TypeConceptId per CDM Mapping spec
               if (po.TypeConceptId >= 38000249 && po.TypeConceptId <= 38000265)
                  de.TypeConceptId = 38000180; // Inpatient administration
               else
                  de.TypeConceptId = 38000179; // Physician administered drug (identified as procedure)

               yield return de;
            }
         }

         // Remove duplicate drug claim records as well as eliminate drug claims that have been administratively backed 
         // out with negative values and apply base logic for drug claims records
         foreach (var de in BuildDrugExposures(FilteroutDrugClaims(drugClaims).ToArray(), visitOccurrences, observationPeriods))
         {
            yield return de;
         }
      }

      /// <summary>
      /// Projects Enumeration of payerPlanPeriod from the raw set of payerPlanPeriod entities.
      /// </summary>
      /// <param name="payerPlanPeriods">raw set of payerPlanPeriod entities</param>
      /// <param name="visitOccurrences">the visit occurrence entities for current person</param>
      /// <returns>Enumeration of payerPlanPeriod entities</returns>
      public override IEnumerable<PayerPlanPeriod> BuildPayerPlanPeriods(PayerPlanPeriod[] payerPlanPeriods, Dictionary<long, VisitOccurrence> visitOccurrences)
      {
         return base.BuildPayerPlanPeriods(CleanPayerPlanPeriods(payerPlanPeriods).ToArray(), visitOccurrences);
      }

      /// <summary>
      /// Projects death entity from the raw set of death entities.
      /// During build:
      /// override the death's start date using the end date of the corresponding visit.
      /// </summary>
      /// <param name="deathRaw">raw set of death entities</param>
      /// <param name="visitOccurrences">the visit occurrence entities for current person</param>
      /// <param name="observationPeriods">the observation period entities for current person</param>
      /// <returns>death entity</returns>
      public override Death BuildDeath(Death[] deathRaw, Dictionary<long, VisitOccurrence> visitOccurrences, ObservationPeriod[] observationPeriods)
      {
         // Filter out death records without visit occurrence and override the death date using the end date of the corresponding visit.
         var death = FilterDeathRecords(deathRaw, visitOccurrences).ToList();

         // Data sources might contain multiple records of death at different dates. 
         // It is the task of the ETL to pick the most plausible or most accurate record to be stored to this table.
         if (death.Any())
         {
            var maxStartDate = death.Max(d => d.StartDate);

            // Only one record will be kept for each patient. If multiple lines of death records are captured, the death captured by discharge status 
            // has higher priority than those captured by ICD 9 diagnosis codes and the record with latest date of death will be kept.
            var result = death.Where(d => d.StartDate == maxStartDate).OrderByDescending(d => d.Primary).First();
            
            result.CauseConceptId = 0;

            var maxVisitStartDate = visitOccurrences.Values.Max(vo => vo.StartDate);
            
            // If there are health care visits after 32 days of death date, delete this record
            if (maxVisitStartDate < result.StartDate.AddDays(32))
            {
               return result;
            }
         }

         return null;
      }

      /// <summary>
      /// Projects Enumeration of procedure cost from the set of procedure occurrence entities.
      /// During build:
      /// override the procedure cost Id to the corresponding procedure occurrence Id
      /// calculate TotalOutOfPocket & TotalPaid per CDM Mapping spec. 
      /// </summary>
      /// <param name="procedureOccurrences">set of procedure occurrence entities</param>
      /// <returns>set of procedure cost entities</returns>
      public override IEnumerable<ProcedureCost> BuildProcedureCosts(ProcedureOccurrence[] procedureOccurrences)
      {
         foreach (var procedureOccurrence in procedureOccurrences)
         {
            if(procedureOccurrence.ProcedureCosts == null) continue;

            foreach (var procedureCost in procedureOccurrence.ProcedureCosts)
            {
               procedureCost.Id = procedureOccurrence.Id;
               //TOTAL_OUT_OF_POCKET = PAID_COPAY + PAID_COINSURANCE + PAID_TOWARDS_DEDUCTIBLE 
               procedureCost.TotalOutOfPocket = procedureCost.PaidCopay + procedureCost.PaidCoinsurance + procedureCost.PaidTowardDeductible;

               //TOTAL_PAID = TOTAL_OUT_OF_POCKET + NETPAY + COB
               procedureCost.TotalPaid = procedureCost.TotalOutOfPocket + procedureCost.PaidByPayer + procedureCost.PaidByCoordinationBenefits;

               yield return procedureCost;
            }
         }
      }

      /// <summary>
      /// Projects Enumeration of drug cost from the set of drug exposure entities.
      /// During build:
      /// calculate TotalOutOfPocket & TotalPaid per CDM Mapping spec. 
      /// </summary>
      /// <param name="drugExposures">set of drug exposure entities</param>
      /// <returns>set of drug cost entities</returns>
      public override IEnumerable<DrugCost> BuildDrugCosts(DrugExposure[] drugExposures)
      {
         foreach (var drugExposure in drugExposures)
         {
            if (drugExposure.DrugCost == null) continue;

            //TOTAL_OUT_OF_POCKET = PAID_COPAY + PAID_COINSURANCE + PAID_TOWARDS_DEDUCTIBLE 
             drugExposure.DrugCost.TotalOutOfPocket = drugExposure.DrugCost.PaidCopay +
                                                      drugExposure.DrugCost.PaidCoinsurance +
                                                      drugExposure.DrugCost.PaidTowardDeductible;

            //TOTAL_PAID = TOTAL_OUT_OF_POCKET + PAID_BY_PAYER + COB
             drugExposure.DrugCost.TotalPaid = drugExposure.DrugCost.TotalOutOfPocket + drugExposure.DrugCost.PaidByPayer + drugExposure.DrugCost.PaidByCoordinationBenefits;

            yield return drugExposure.DrugCost;
         }
      }

      /// <summary>
      /// Projects person etity from the raw set of persons entities. 
      /// </summary>
      /// <param name="records">raw set of Person entities</param>
      /// <returns>Person entity</returns>
      public override Person BuildPerson(List<Person> records)
      {
         if (records == null || records.Count == 0) return null;

         var ordered = records.OrderByDescending(p => p.StartDate);
         var person = ordered.Take(1).First();
         person.StartDate = ordered.Take(1).Last().StartDate;

         return person;
      }

      /// <summary>
      /// Projects Enumeration of ProcedureOccurrence from the raw set of ProcedureOccurence entities.
      /// During build:
      /// override the procedure's start date using the end date of the corresponding visit.
      /// overide TypeConceptId per CDM Mapping spec.
      /// truncate procedure's dates to the corresponding observation period dates
      /// </summary>
      /// <param name="procedureOccurrences">raw set of procedure occurrence entities</param>
      /// <param name="vo">the visit occurrence entities for current person</param>
      /// <param name="observationPeriods">the observation period entities for current person</param>
      /// <returns>Enumeration of procedure occurrence entities</returns>
      public override IEnumerable<ProcedureOccurrence> BuildProcedureOccurrences(ProcedureOccurrence[] procedureOccurrences, Dictionary<long, VisitOccurrence> vo, ObservationPeriod[] observationPeriods)
      {
         var result = new HashSet<ProcedureOccurrence>();
         var procedures = new List<ProcedureOccurrence>();

         var proc = JoinVisitOccurrences(procedureOccurrences).ToList();

         foreach (var po in proc)
         {
            var visitOccurrence = vo[po.VisitOccurrenceId.Value];

            if (po.AdditionalFields["priority"] != "1")
            {
               if (string.IsNullOrEmpty(po.SourceValue)) continue;

               po.StartDate = visitOccurrence.EndDate.Value;
            }
            else
            {
               TruncateDatesToObservationPeriod(observationPeriods, po);
            }

            po.VisitOccurrenceId = visitOccurrence.Id;

            procedures.Add(po);
         }

         foreach (var sameVisit in procedures.GroupBy(c => c.VisitOccurrenceId))
         {
            foreach (var sameStartDate in sameVisit.GroupBy(c => c.StartDate))
            {
               foreach (var sameSource in sameStartDate.GroupBy(c => c.SourceValue))
               {
                  foreach (var sameConcept in sameSource.GroupBy(c => c.ConceptId))
                  {
                     var grouped = sameConcept.OrderBy(c => c.AdditionalFields["priority"]).ThenBy(c => c.TypeConceptId);
                     var po = grouped.First();

                     var visitOccurrence = vo[po.VisitOccurrenceId.Value];


                     // provid in CCAE/MDCR is int & in MDCD is string
                     var isCCAE = visitOccurrence.AdditionalFields["source"] != "mdcd";


                     if (po.AdditionalFields["priority"] == "1")
                     {
                        //DX1, PROVID and STDPROV
                        ProcedureOccurrence first;

                        if (isCCAE)
                        {
                           first = grouped.Where(e => e.AdditionalFields["priority"] == "1")
                                          .OrderBy(e => e.AdditionalFields["dx1"])
                                          .ThenBy(
                                             e =>
                                             string.IsNullOrEmpty(e.AdditionalFields["provid"])
                                                ? 0
                                                : int.Parse(e.AdditionalFields["provid"]))
                                          .ThenBy(
                                             e =>
                                             string.IsNullOrEmpty(e.AdditionalFields["stdprov"])
                                                ? 0
                                                : int.Parse(e.AdditionalFields["stdprov"])).First();
                        }
                        else
                        {
                           first = grouped.Where(e => e.AdditionalFields["priority"] == "1")
                                          .OrderBy(e => e.AdditionalFields["dx1"])
                                          .ThenBy(e => e.AdditionalFields["provid"].ToLower())
                                          .ThenBy(
                                             e =>
                                             string.IsNullOrEmpty(e.AdditionalFields["stdprov"])
                                                ? 0
                                                : int.Parse(e.AdditionalFields["stdprov"])).First();
                        }

                        po.ProviderKey = first.ProviderKey;
                        po.ReleventConditionConceptId = first.ReleventConditionConceptId;

                        var costs = new List<ProcedureCost>();
                        foreach (
                           var procedureOccurrence in
                              grouped.Where(procedureOccurrence => procedureOccurrence.ProcedureCosts != null))
                        {
                           costs.AddRange(procedureOccurrence.ProcedureCosts);
                        }
                        po.ProcedureCosts = costs;
                     }
                     else
                     {
                        if (po.AdditionalFields["priority"] == "2")
                        {
                           po.ReleventConditionConceptId =
                              grouped.Where(
                                 e => e.AdditionalFields["priority"] == "2" && e.TypeConceptId == po.TypeConceptId)
                                     .OrderBy(e => e.AdditionalFields["pdx"])
                                     .First()
                                     .ReleventConditionConceptId;

                        }
                        else
                        {
                           if (isCCAE)
                           {
                              po.ReleventConditionConceptId =
                                 grouped.Where(
                                    e => e.AdditionalFields["priority"] == "3" && e.TypeConceptId == po.TypeConceptId)
                                        .OrderBy(e => e.AdditionalFields["dx1"])
                                        .ThenBy(
                                           e =>
                                           string.IsNullOrEmpty(e.AdditionalFields["provid"])
                                              ? 0
                                              : int.Parse(e.AdditionalFields["provid"]))
                                        .ThenBy(
                                           e =>
                                           string.IsNullOrEmpty(e.AdditionalFields["stdprov"])
                                              ? 0
                                              : int.Parse(e.AdditionalFields["stdprov"]))
                                        .First()
                                        .ReleventConditionConceptId;
                           }
                           else
                           {
                              po.ReleventConditionConceptId =
                                 grouped.Where(
                                    e => e.AdditionalFields["priority"] == "3" && e.TypeConceptId == po.TypeConceptId)
                                        .OrderBy(e => e.AdditionalFields["dx1"])
                                        .ThenBy(e => e.AdditionalFields["provid"].ToLower())
                                        .ThenBy(
                                           e =>
                                           string.IsNullOrEmpty(e.AdditionalFields["stdprov"])
                                              ? 0
                                              : int.Parse(e.AdditionalFields["stdprov"]))
                                        .First()
                                        .ReleventConditionConceptId;

                           }
                        }

                        po.ProviderKey = GetProviderKey(visitOccurrence);
                     }

                     if (po.ReleventConditionConceptId == null)
                        po.ReleventConditionConceptId = 0;

                     po.EndDate = null;
                     po.TypeConceptId = GetProcedureTypeConceptId(po, visitOccurrence.ConceptId);
                     result.Add(po);
                  }
               }
            }
         }

         return base.BuildProcedureOccurrences(result.ToArray(), vo, observationPeriods);
      }

      /// <summary>
      /// Projects Enumeration of ConditionOccurrence from the raw set of ConditionOccurrence entities. 
      /// 	During build:
      /// 	override the condition's start date using the start date of the corresponding visit.
      ///   overide TypeConceptId per CDM Mapping spec. 
      /// </summary>
      /// <param name="conditionOccurrences">raw set of condition occurrence entities</param>
      /// <param name="vo">the visit occurrence entities for current person</param>
      /// <param name="op">the observation period entities for current person</param>
      /// <returns>Enumeration of condition occurrence entities</returns>
      public override IEnumerable<ConditionOccurrence> BuildConditionOccurrences(ConditionOccurrence[] conditionOccurrences, Dictionary<long, VisitOccurrence> vo, ObservationPeriod[] op)
      {
         var result = new HashSet<ConditionOccurrence>();
         foreach (var sameVisit in JoinVisitOccurrences(conditionOccurrences.Where(co => CodeValidator.IsValidIcd9(co.SourceValue))).GroupBy(c => c.VisitOccurrenceId))
         {
            foreach (var sameSource in sameVisit.GroupBy(c => c.SourceValue))
            {
               foreach (var sameConcept in sameSource.GroupBy(c => c.ConceptId))
               {
                  var co = sameConcept.OrderBy(c => c.AdditionalFields["priority"])
                                     .ThenBy(c => c.TypeConceptId)
                                     .First();

                  var visitOccurrence = vo[co.VisitOccurrenceId.Value];

                  co.EndDate = null;
                  co.StartDate = visitOccurrence.StartDate;

                  co.TypeConceptId = GetConditionTypeConceptId(co, visitOccurrence.ConceptId);
                  co.ProviderKey = GetProviderKey(visitOccurrence);

                  result.Add(co);
               }
            }
         }

         return base.BuildConditionOccurrences(result.ToArray(), vo, op);
      }

      /// <summary>
      /// Projects Enumeration of Visit Occurrence from the raw set of Visit Occurrence entities. 
      ///  </summary>
      /// <param name="rawVisitOccurrences">raw set of Visit Occurrence entities</param>
      /// <param name="observationPeriods">the observation periods entities for current person</param>
      /// <returns>Enumeration of Visit Occurrence</returns>
      public override IEnumerable<VisitOccurrence> BuildVisitOccurrences(VisitOccurrence[] rawVisitOccurrences, ObservationPeriod[] observationPeriods)
      {
         var visitOccurrences = CleanVisitOccurrences(rawVisitOccurrences, observationPeriods).ToList();

         var ipVisits = CollapseVisits(visitOccurrences.Where(vo => vo.ConceptId == 9201)).ToList();
         var erVisits = new List<VisitOccurrence>();
         var opVisits = new List<VisitOccurrence>();

         var ltcpVisits = CollapseLTCPVisits(visitOccurrences.Where(vo => vo.ConceptId == 42898160)).ToList();

         foreach (var visitOccurrence in visitOccurrences.Where(visitOccurrence => visitOccurrence.ConceptId != 9201 && visitOccurrence.ConceptId != 42898160))
         {
            var ip = ipVisits.FirstOrDefault(v => visitOccurrence.StartDate.Between(v.StartDate, v.EndDate.Value));

            if (visitOccurrence.ConceptId == 9203)
            {
               if (ip == null || (visitOccurrence.StartDate == ip.StartDate && visitOccurrence.EndDate == ip.StartDate))
               //ER - 9203
               {
                  erVisits.Add(visitOccurrence);
               }
               else
               {
                  AddRawVisitOccurrence(visitOccurrence, ip);
               }
            }
            else if (ip == null)
            {
               opVisits.Add(visitOccurrence);
            }
            else
            {
               AddRawVisitOccurrence(visitOccurrence, ip);
            }
         }

         foreach (var ipVisit in ipVisits)
         {
            yield return ipVisit;
         }

         foreach (var ltcpVisit in ltcpVisits)
         {
            TruncateDatesToObservationPeriod(observationPeriods, ltcpVisit);
            yield return ltcpVisit;
         }

         foreach (var erGroup in erVisits.GroupBy(v => v.StartDate))
         {
            var visit = erGroup.First();
            visit.EndDate = erGroup.Max(v => v.EndDate);
            foreach (var visitOccurrence in erGroup)
            {
               AddRawVisitOccurrence(visitOccurrence, visit);
            }

            yield return visit;
         }

         foreach (var opGroup in opVisits.GroupBy(v => v.StartDate))
         {
            foreach (var opGroup1 in opGroup.GroupBy(v => v.AdditionalFields["provid"].ToLower()))
            {
               var visit = opGroup1.First();
               visit.EndDate = opGroup1.Max(v => v.EndDate);
               foreach (var visitOccurrence in opGroup1)
               {
                  AddRawVisitOccurrence(visitOccurrence, visit);
               }

               yield return visit;
            }
         }
      }

      private static void TruncateDatesToObservationPeriod(IEnumerable<ObservationPeriod> observationPeriods, IEntity entity)
      {
         if (!entity.EndDate.HasValue)
            entity.EndDate = entity.StartDate;
         else if (entity.StartDate > entity.EndDate)
            entity.EndDate = entity.StartDate;

         var observationPeriod = observationPeriods.FirstOrDefault(op => op.StartDate.Between(entity.StartDate, entity.EndDate.Value) || op.EndDate.Between(entity.StartDate, entity.EndDate.Value));
         if (observationPeriod == null) return;

         if (entity.StartDate < observationPeriod.StartDate)
            entity.StartDate = observationPeriod.StartDate;

         if (entity.StartDate > observationPeriod.EndDate)
            entity.StartDate = observationPeriod.EndDate;

         if (entity.EndDate > observationPeriod.EndDate)
            entity.EndDate = observationPeriod.EndDate;
      }

      private static string GetFachdidKey(IEntity entity)
      {
         if (!entity.AdditionalFields.ContainsKey("fachdid") || string.IsNullOrEmpty(entity.AdditionalFields["fachdid"]))
            return null;

         return string.Format("{0};{1}", entity.AdditionalFields["fachdid"], entity.AdditionalFields["year"]);
      }

      private static string GetCaseidKey(IEntity entity)
      {
         return !entity.AdditionalFields.ContainsKey("caseid") ? null : string.Format("{0};{1}", entity.AdditionalFields["caseid"], entity.AdditionalFields["year"]);
      }

      private string GetProviderKey(VisitOccurrence vo)
      {
         var isCCAE = vo.AdditionalFields["source"] != "mdcd";

         IEntity result;
         if (vo.ConceptId == 9202)
         {
            if (isCCAE)
            {
               result = medicalRecords[vo.SourceRecordGuid].Where(e => e.ConceptId == vo.ConceptId)
                                                        .OrderBy(e => DateTime.Parse(e.AdditionalFields["start"]))
                                                        .ThenBy(e => string.IsNullOrEmpty(e.AdditionalFields["provid"]) ? 0 : int.Parse(e.AdditionalFields["provid"]))
                                                        .ThenBy(e => DateTime.Parse(e.AdditionalFields["end"]))
                                                        .ThenBy(e => string.IsNullOrEmpty(e.AdditionalFields["stdprov"]) ? 0 : int.Parse(e.AdditionalFields["stdprov"]))
                                                        .First();
            }
            else
            {
               result = medicalRecords[vo.SourceRecordGuid].Where(e => e.ConceptId == vo.ConceptId)
                                                        .OrderBy(e => DateTime.Parse(e.AdditionalFields["start"]))
                                                        .ThenBy(e => e.AdditionalFields["provid"].ToLower())
                                                        .ThenBy(e => DateTime.Parse(e.AdditionalFields["end"]))
                                                        .ThenBy(e => string.IsNullOrEmpty(e.AdditionalFields["stdprov"]) ? 0 : int.Parse(e.AdditionalFields["stdprov"]))
                                                        .First();
            }
         }
         else
         {
            if (isCCAE)
            {
               result = medicalRecords[vo.SourceRecordGuid].Where(e => e.ConceptId == vo.ConceptId)
                                                        .OrderBy(e => DateTime.Parse(e.AdditionalFields["start"]))
                                                        .ThenBy(e => DateTime.Parse(e.AdditionalFields["end"]))
                                                        .ThenBy(e => string.IsNullOrEmpty(e.AdditionalFields["provid"]) ? 0 : int.Parse(e.AdditionalFields["provid"]))
                                                        .ThenBy(e => string.IsNullOrEmpty(e.AdditionalFields["stdprov"]) ? 0 : int.Parse(e.AdditionalFields["stdprov"]))
                                                        .First();
            }
            else
            {
               result = medicalRecords[vo.SourceRecordGuid].Where(e => e.ConceptId == vo.ConceptId)
                                                        .OrderBy(e => DateTime.Parse(e.AdditionalFields["start"]))
                                                        .ThenBy(e => DateTime.Parse(e.AdditionalFields["end"]))
                                                        .ThenBy(e => e.AdditionalFields["provid"].ToLower())
                                                        .ThenBy(e => string.IsNullOrEmpty(e.AdditionalFields["stdprov"]) ? 0 : int.Parse(e.AdditionalFields["stdprov"]))
                                                        .First();
            }
         }

         return result.ProviderKey;
      }

      private static int GetConditionTypeConceptId(IEntity e, long visitTypeId)
      {
         if (e.AdditionalFields["priority"] == "1") //IS OS
         {
            if (visitTypeId == 9201)
            {
               switch (e.TypeConceptId)
               {
                  case 1:
                     return 38000183;

                  case 2:
                     return 38000184;

                  case 3:
                     return 38000185;

                  case 4:
                     return 38000186;

                  case 5:
                     return 38000187;

                  default:
                     return 0;
               }
            }

            switch (e.TypeConceptId)
            {
               case 1:
                  return 38000215;

               case 2:
                  return 38000216;

               case 3:
                  return 38000217;

               case 4:
                  return 38000218;

               case 5:
                  return 38000219;

               default:
                  return 0;
            }
         }

         if (visitTypeId == 9201)
         {
            switch (e.TypeConceptId)
            {
               case 1:
                  return 38000199;

               case 2:
                  return 38000200;

               case 3:
                  return 38000201;

               case 4:
                  return 38000202;

               case 5:
                  return 38000203;

               case 6:
                  return 38000204;

               case 7:
                  return 38000205;

               case 8:
                  return 38000206;

               case 9:
                  return 38000207;

               case 10:
                  return 38000208;

               case 11:
                  return 38000209;

               case 12:
                  return 38000210;

               case 13:
                  return 38000211;

               case 14:
                  return 38000212;

               case 15:
                  return 38000213;

               case 16:
                  return 38000214;

               default:
                  return 0;
            }
         }

         switch (e.TypeConceptId)
         {
            case 2:
               return 38000230;

            case 3:
               return 38000231;

            case 4:
               return 38000232;

            case 5:
               return 38000233;

            case 6:
               return 38000234;

            case 7:
               return 38000235;

            case 8:
               return 38000236;

            case 9:
               return 38000237;

            case 10:
               return 38000238;

            default:
               return 0;
         }
      }

      private static int GetProcedureTypeConceptId(IEntity e, long visitTypeId)
      {
         if (e.AdditionalFields["priority"] == "1") // IS OS
         {
            return visitTypeId == 9201 ? 38000249 : 38000267;
         }

         if (visitTypeId == 9201)
         {
            switch (e.TypeConceptId)
            {
               case 1:
                  return 38000250;

               case 2:
                  return 38000251;

               case 3:
                  return 38000252;

               case 4:
                  return 38000253;

               case 5:
                  return 38000254;

               case 6:
                  return 38000255;

               case 7:
                  return 38000256;

               case 8:
                  return 38000257;

               case 9:
                  return 38000258;

               case 10:
                  return 38000259;

               case 11:
                  return 38000260;

               case 12:
                  return 38000261;

               case 13:
                  return 38000262;

               case 14:
                  return 38000263;

               case 15:
                  return 38000264;

               case 16:
                  return 38000265;

               default:
                  return 0;
            }
         }

         switch (e.TypeConceptId)
         {
            case 2:
               return 38000269;

            case 3:
               return 38000270;

            case 4:
               return 38000271;

            case 5:
               return 38000272;

            case 6:
               return 38000273;

            case 7:
               return 38000274;

            default:
               return 0;
         }
      }

      private IEnumerable<VisitOccurrence> CleanVisitOccurrences(IEnumerable<VisitOccurrence> visitOccurrences, ObservationPeriod[] observationPeriods)
      {
         foreach (var visitOccurrence in visitOccurrences)
         {
            if (visitOccurrence.ConceptId == 42898160)
            {
               var period =
                  observationPeriods.Where(
                     op => visitOccurrence.StartDate.Between(op.StartDate, op.EndDate) || visitOccurrence.EndDate.Value.Between(op.StartDate, op.EndDate))
                                    .OrderBy(op => op.StartDate)
                                    .ThenBy(op => op.EndDate)
                                    .FirstOrDefault();

               if (period == null) continue;

               visitOccurrence.AdditionalFields.Add("start", visitOccurrence.StartDate.ToString(CultureInfo.InvariantCulture));
               visitOccurrence.AdditionalFields.Add("end", visitOccurrence.EndDate.Value.ToString(CultureInfo.InvariantCulture));
               yield return visitOccurrence;
               continue;
            }

            if (!visitOccurrence.EndDate.HasValue || visitOccurrence.EndDate < visitOccurrence.StartDate)
               visitOccurrence.EndDate = visitOccurrence.StartDate;

            var observationPeriod =
               observationPeriods.Where(
                  op =>
                  visitOccurrence.StartDate.Between(op.StartDate, op.EndDate) ||
                  visitOccurrence.EndDate.Value.Between(op.StartDate, op.EndDate) ||
                  (visitOccurrence.StartDate < op.StartDate && visitOccurrence.EndDate > op.EndDate))
                                 .OrderBy(op => op.StartDate)
                                 .ThenBy(op => op.EndDate)
                                 .FirstOrDefault();

            if (observationPeriod == null) continue;

            if (visitOccurrence.StartDate < observationPeriod.StartDate)
               visitOccurrence.StartDate = observationPeriod.StartDate;

            if (visitOccurrence.EndDate > observationPeriod.EndDate)
               visitOccurrence.EndDate = observationPeriod.EndDate;

            visitOccurrence.AdditionalFields.Add("start", visitOccurrence.StartDate.ToString(CultureInfo.InvariantCulture));
            visitOccurrence.AdditionalFields.Add("end", visitOccurrence.EndDate.Value.ToString(CultureInfo.InvariantCulture));

            yield return visitOccurrence;
         }
      }

      private IEnumerable<VisitOccurrence> CollapseLTCPVisits(IEnumerable<VisitOccurrence> visitOccurrences)
      {
         var step1 = CollapseVisits(visitOccurrences).ToList();

         foreach (var vo in step1.Where(vo => vo.StartDate == vo.EndDate))
         {
            vo.StartDate = new DateTime(vo.StartDate.Year, vo.StartDate.Month, 1);
            vo.EndDate = new DateTime(vo.StartDate.Year, vo.StartDate.Month,
                                      DateTime.DaysInMonth(vo.StartDate.Year, vo.StartDate.Month));
         }

         return CollapseVisits(step1);
      }

      private IEnumerable<VisitOccurrence> CollapseVisits(IEnumerable<VisitOccurrence> visitOccurrences)
      {
         var visits = new List<VisitOccurrence>();

         foreach (var claim in visitOccurrences.OrderBy(vo => vo.StartDate).ThenBy(vo => vo.EndDate)) //IP - 9201
         {
            if (visits.Count > 0)
            {
               var previousClaim = visits.Last();
               if (claim.StartDate <= previousClaim.EndDate.Value.AddDays(1))
               {
                  if (claim.EndDate >= previousClaim.EndDate)
                  {
                     previousClaim.EndDate = claim.EndDate;
                  }

                  AddRawVisitOccurrence(claim, previousClaim);
                  continue;
               }
            }

            AddRawVisitOccurrence(claim, claim);

            visits.Add(claim);
         }
         return visits;
      }

      private void AddRawVisitOccurrence(VisitOccurrence rawVisit, VisitOccurrence finalVisit)
      {
         if (!rawVisits.ContainsKey(rawVisit.SourceRecordGuid))
            rawVisits.Add(rawVisit.SourceRecordGuid, finalVisit);
         else
            rawVisits[rawVisit.SourceRecordGuid] = finalVisit;

         if (!medicalRecords.ContainsKey(finalVisit.SourceRecordGuid))
            medicalRecords.Add(finalVisit.SourceRecordGuid, new List<IEntity>());

         medicalRecords[finalVisit.SourceRecordGuid].Add(rawVisit);

         var caseidKey = GetCaseidKey(rawVisit);
         var fachidKey = GetFachdidKey(rawVisit);

         if (!string.IsNullOrEmpty(caseidKey) && finalVisit.ConceptId == 9201)
         {
            if (!caseidVisits.ContainsKey(caseidKey))
               caseidVisits.Add(caseidKey, new HashSet<VisitOccurrence>());

            caseidVisits[caseidKey].Add(finalVisit);
         }

         if (!string.IsNullOrEmpty(fachidKey))
         {
            if (!fachidVisits.ContainsKey(fachidKey))
               fachidVisits.Add(fachidKey, new HashSet<VisitOccurrence>());

            fachidVisits[fachidKey].Add(finalVisit);
         }
      }

      /// <summary>
      /// Remove duplicate drug claim records as well as eliminate drug claims that have been administratively backed out with negative values
      /// </summary>
      /// <param name="drugClaims">set of drug exposure entities</param>
      /// <returns>Enumeration of filtired drug exposure entities</returns>
      private static IEnumerable<DrugExposure> FilteroutDrugClaims(Dictionary<Guid, List<DrugExposure>> drugClaims)
      {
         foreach (var similarDrugs in drugClaims.SelectMany(drugs => drugs.Value.GroupBy(d => d.SourceValue.Length >= 9 ? d.SourceValue.Substring(0, 9) : d.SourceValue)))
         {
            if (similarDrugs.Count(d => d.ConceptId > 0) > 0)
            {
               yield return similarDrugs.Where(d => d.ConceptId > 0).OrderBy(d => d.SourceValue.Length).Last();
               continue;
            }

            yield return similarDrugs.OrderBy(d => d.SourceValue.Length).Last();
         }
      }

      public override bool CanPayerPlanPeriodBeCombined(PayerPlanPeriod current, PayerPlanPeriod other)
      {
         return current.PlanSourceValue == other.PlanSourceValue && current.PayerSourceValue == other.PayerSourceValue;
      }

      private static IEnumerable<PayerPlanPeriod> CleanPayerPlanPeriods(IList<PayerPlanPeriod> payerPlanPeriods)
      {
         payerPlanPeriods = payerPlanPeriods.OrderBy(p => p.StartDate)
                                            .ThenBy(p => p.EndDate).ToList();

         for (var i = 0; i < payerPlanPeriods.Count - 1; i++)
         {
            if (payerPlanPeriods[i].EndDate >= payerPlanPeriods[i + 1].StartDate)
            {
               payerPlanPeriods[i].EndDate = payerPlanPeriods[i + 1].StartDate.AddDays(-1);

               if (payerPlanPeriods[i].EndDate < payerPlanPeriods[i].StartDate)
                  continue;
            }

            yield return payerPlanPeriods[i];
         }

         yield return payerPlanPeriods.Last();
      }

      // Filter out death records without visit occurrence and override the death date using the end date of the corresponding visit.
      private IEnumerable<Death> FilterDeathRecords(IEnumerable<Death> death, Dictionary<long, VisitOccurrence> visitOccurrences)
      {
         foreach (var d in JoinVisitOccurrences(death))
         {
            if (!d.VisitOccurrenceId.HasValue || !visitOccurrences.ContainsKey(d.VisitOccurrenceId.Value)) continue;

            var visitOccurrence = visitOccurrences[d.VisitOccurrenceId.Value];

            d.StartDate = visitOccurrence.EndDate.Value;
            yield return d;
         }
      }

      // Get corresponding visit occurrence for entity
      private VisitOccurrence GetVisitOccurrence(IEntity ent)
      {
         if (rawVisits.ContainsKey(ent.SourceRecordGuid))
         {
            var vo = rawVisits[ent.SourceRecordGuid];
            if (vo.Id == 0 && rawVisits.ContainsKey(vo.SourceRecordGuid) && rawVisits[vo.SourceRecordGuid].SourceRecordGuid != ent.SourceRecordGuid)
            {
               vo = rawVisits[vo.SourceRecordGuid];
            }

            return vo;
         }

         return null;
      }

      // Find and assign VisitOccurrenceId to set of entities
      private IEnumerable<T> JoinVisitOccurrences<T>(IEnumerable<T> entities) where T : class, IEntity
      {
         var otherEntities = new List<IEntity>();
         foreach (var entity in entities)
         {
            // IS & OS
            if (entity.AdditionalFields["priority"] == "1")
            {
               var visitOccurrence = GetVisitOccurrence(entity);
               if (visitOccurrence == null) continue;

               entity.VisitOccurrenceId = visitOccurrence.Id;
               yield return (T)Activator.CreateInstance(typeof(T), entity);
            }
            else // IA & FH
            {
               otherEntities.Add(entity);
            }
         }

         foreach (var entity in otherEntities)
         {
            var caseidKey = GetCaseidKey(entity);
            var fachidKey = GetFachdidKey(entity);

            if (caseidKey != null) // IA
            {
               if (!caseidVisits.ContainsKey(caseidKey)) continue;

               var visitOccurrence =
                  caseidVisits[caseidKey].OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault();

               entity.VisitOccurrenceId = visitOccurrence.Id;
               yield return (T)Activator.CreateInstance(typeof(T), entity);
            }
            else if (fachidKey != null)
            {
               if (!fachidVisits.ContainsKey(fachidKey)) continue;

               var visitOccurrence = (fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 9201 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault() ??
                                      fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 9203 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault()) ??
                                     fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 9202 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault() ??
                                     fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 42898160 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault();

               if (visitOccurrence == null)
               {

                  visitOccurrence = (fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 9201).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault() ??
                                      fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 9203).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault()) ??
                                     fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 9202).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault() ??
                                     fachidVisits[fachidKey].Where(fhV => fhV.ConceptId == 42898160).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault();

                  var key = GetFachdidKey(visitOccurrence);

                  if (fachidVisits.ContainsKey(key))
                  {
                     visitOccurrence = (fachidVisits[key].Where(fhV => fhV.ConceptId == 9201 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault() ??
                                      fachidVisits[key].Where(fhV => fhV.ConceptId == 9203 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault()) ??
                                     fachidVisits[key].Where(fhV => fhV.ConceptId == 9202 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault() ??
                                     fachidVisits[key].Where(fhV => fhV.ConceptId == 42898160 && fhV.Id > 0).OrderBy(v => v.StartDate).ThenBy(v => v.EndDate).FirstOrDefault();
                  }

               }

               entity.VisitOccurrenceId = visitOccurrence.Id;
               yield return (T)Activator.CreateInstance(typeof(T), entity);
            }
         }
      }
   }
}
