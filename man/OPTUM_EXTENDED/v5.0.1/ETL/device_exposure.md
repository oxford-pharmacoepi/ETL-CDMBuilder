*Common Data Model ETL Mapping Specification for Optum Extended SES & Extended DOD* 
<br>*CDM Version = 5.0.1, Clinformatics Version = v7.0*
<br>*Authors: Qianli Ma; Erica Voss, Chris Knoll, Ajit Londhe, Clair Blacketer (Janssen)*

[Back to README](README.md)

---

# CDM Table: DEVICE_EXPOSURE

The *DEVICE_EXPOSURE* table will be populated with records from
**TEMP_MEDICAL,** where the PROC code was mapped to a standard concept
with a DOMAIN_ID of 'Device'.

<a name="table-mappings-device-exposure"></a>

**Destination Field**|**Source Field**|**Applied Rule**|**Comment**
:-----:|:-----:|:-----:|:-----:
DEVICE_EXPOSURE_ID|-|System Generated| 
PERSON_ID|TEMP_MEDICAL:<br>PATID / PAT_PLANID| | 
DEVICE_CONCEPT_ID|TEMP_MEDICAL:<br>PROC1-PROC25, PROC_CD, DIAG1-DIAG25|DIAG1-DIAG25:<br>Use [Source to Standard Terminology](code_snippets.md#source-to-standard-terminology) and filter with [Concept Ids from Medical Claims Diagnosis Fields](code_snippets.md#from-medical-claims-diagnosis-fields)<br><br>PROC1-PROC25, PROC_CD:<br>Use [Source to Standard Terminology](code_snippets.md#source-to-standard-terminology) and filter with [Concept Ids from Medical Claims Procedure Fields](code_snippets.md#from-medical-claims-procedure-fields)<br>| 
DEVICE_EXPOSURE_START_DATE|VISIT_OCCURRENCE VISIT_END_DATE| | 
DEVICE_EXPOSURE_END_DATE|-| | 
DEVICE_TYPE_CONCEPT_ID|-|If the record is coming from another table, like the PROCEDURE_OCCURENCE, keep the types that would have been assigned in that table.| 
UNIQUE_DEVICE_ID|-| | 
QUANTITY|0| | 
PROVIDER_ID|NEW_PROV<br>NEW_PROVCAT|Map NEW_PROV<br>to PROVIDER_SOURCE_VALUE and NEW_PROVCAT to<br>SPECIALTY_SOURCE_VALUE in Provider table to extract associated Provider ID.| 
VISIT_OCCURRENCE_ID|VISIT_OCCURRENCE –VISIT_OCCURRENCE_ID|Refer to logic in building VISIT_OCCURRENCE table for linking with VISIT_OCCURRENCE_ID| 
DEVICE_SOURCE_VALUE|TEMP_MEDICAL:<br>PROC1-PROC25, PROC_CD, DIAG1-DIAG25| | 
DEVICE_SOURCE_CONCEPT_ID|TEMP_MEDICAL:<br>PROC1-PROC25, PROC_CD, DIAG1-DIAG25|TEMP_MEDICAL (PROC1-PROC25 & PROC_CD):<br>Use [Source to Source](code_snippets.md#source-to-source) and filter with [Source Concept Ids from Medical Claims Procedure Fields](code_snippets.md#from-medical-claims-procedure-fields-2)<br><br>TEMP_MEDICAL (DIAG1-DIAG25):<br>Use [Source to Source](code_snippets.md#source-to-source) and filter with [Source Concept Ids from Medical Claims Diagnosis Fields](code_snippets.md#from-medical-claims-diagnosis-fields-2)<br><br><br>|Use the code in [Source to Source](code_snippets.md#source-to-source)
