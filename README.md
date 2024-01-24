The .Net CDM Builder was initially developed by Janssen Research & Development as a tool to transform its observational databases into the OMOP Common Data Model.
You can find the original code in https://github.com/OHDSI/ETL-CDMBuilder

The program is used to convert CPRD GOLD into OMOP CDM. 

v.3.0
=============
* Support CDM v.5.4 (+)
    - (backward compatible to CDM v.5.3)
* Map route in drugs (+)
* Bug fixed the incorrect observation_end_date
* [Tentative] No vaccinations in drug_era (sue to the CVX issues)
* Applied to **GOLD 202307 release**
  
v.2.0
=============
* Expand COVID-19 Vaccination brand infomation (+)
* Only map events within observation period (+)
    - For Death, the observation period = observation_start_date to observation_end_date + 3 months)
* Data Cleaning function (-)
* Applied to **GOLD 202301 release**

v.1.0
=============
* SQL tunning
* Autogenerated ids in OMOP CDM tables (+)
* Data Cleaning function (+)
* Applied to **GOLD 202207 release**

v.0.0
=============
Clone from https://github.com/OHDSI/ETL-CDMBuilder

Backlogs
=============
- [x] Support CDM v.5.4
- [ ] Map Ethnicities
- testing
