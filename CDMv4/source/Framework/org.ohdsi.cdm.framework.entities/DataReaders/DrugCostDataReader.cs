﻿using System;
using System.Collections.Generic;
using System.Data;
using org.ohdsi.cdm.framework.entities.Builder;
using org.ohdsi.cdm.framework.entities.Omop;
using org.ohdsi.cdm.framework.shared.Extensions;

namespace org.ohdsi.cdm.framework.entities.DataReaders 
{
   public class DrugCostDataReader : IDataReader
   {
      private readonly IEnumerator<DrugCost> enumerator;
      private readonly KeyMasterOffset offset;
      // A custom DataReader is implemented to prevent the need for the HashSet to be transformed to a DataTable for loading by SqlBulkCopy
      public DrugCostDataReader(List<DrugCost> batch, KeyMasterOffset offset)
      {
         enumerator = batch.GetEnumerator();
         this.offset = offset;
      }

      public bool Read()
      {
         return enumerator.MoveNext();
      }
      
      public int FieldCount
      {
         get { return 13; }
      }

      public object GetValue(int i)
      {
         if (enumerator.Current == null) return null;

         switch (i)
         {
            case 0:
               return enumerator.Current.Id + offset.DrugExposureOffset;

            case 1:
               return enumerator.Current.Id + offset.DrugExposureOffset;

            case 2:
               return enumerator.Current.PaidCopay.Round();

            case 3:
               return enumerator.Current.PaidCoinsurance.Round();

            case 4:
               return enumerator.Current.PaidTowardDeductible.Round();

            case 5:
               return enumerator.Current.PaidByPayer.Round();

            case 6:
               return enumerator.Current.PaidByCoordinationBenefits.Round();

            case 7:
               return enumerator.Current.TotalOutOfPocket.Round();

            case 8:
               return enumerator.Current.TotalPaid.Round();

            case 9:
               return enumerator.Current.IngredientCost.Round();

            case 10:
               return enumerator.Current.DispensingFee.Round();

            case 11:
               return enumerator.Current.AverageWholesalePrice.Round();

            case 12:
               return enumerator.Current.PayerPlanPeriodId.HasValue ? enumerator.Current.PayerPlanPeriodId + offset.PayerPlanPeriodOffset : null;

            default:
               throw new NotImplementedException();
         }
      }

      #region implementationn not required for SqlBulkCopy
      public bool NextResult()
      {
         throw new NotImplementedException();
      }

      public void Close()
      {
         throw new NotImplementedException();
      }

      public bool IsClosed
      {
         get { throw new NotImplementedException(); }
      }

      public int Depth
      {
         get { throw new NotImplementedException(); }
      }

      public DataTable GetSchemaTable()
      {
         throw new NotImplementedException();
      }

      public int RecordsAffected
      {
         get { throw new NotImplementedException(); }
      }

      public void Dispose()
      {
         throw new NotImplementedException();
      }

      public bool GetBoolean(int i)
      {
         throw new NotImplementedException();
      }

      public byte GetByte(int i)
      {
         throw new NotImplementedException();
      }

      public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
      {
         throw new NotImplementedException();
      }

      public char GetChar(int i)
      {
         throw new NotImplementedException();
      }

      public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
      {
         throw new NotImplementedException();
      }

      public IDataReader GetData(int i)
      {
         throw new NotImplementedException();
      }

      public string GetDataTypeName(int i)
      {
         throw new NotImplementedException();
      }

      public DateTime GetDateTime(int i)
      {
         throw new NotImplementedException();
      }

      public decimal GetDecimal(int i)
      {
         throw new NotImplementedException();
      }

      public double GetDouble(int i)
      {
         throw new NotImplementedException();
      }

      public Type GetFieldType(int i)
      {
         throw new NotImplementedException();
      }

      public float GetFloat(int i)
      {
         throw new NotImplementedException();
      }

      public Guid GetGuid(int i)
      {
         throw new NotImplementedException();
      }

      public short GetInt16(int i)
      {
         throw new NotImplementedException();
      }

      public int GetInt32(int i)
      {
         throw new NotImplementedException();
      }

      public long GetInt64(int i)
      {
         throw new NotImplementedException();
      }

      public string GetName(int i)
      {
         if (enumerator.Current == null) return null;

         switch (i)
         {
            case 0:
               return "Id";

            case 1:
               return "DrugExposureId";

            case 2:
               return "PaidCopay";

            case 3:
               return "PaidCoinsurance";

            case 4:
               return "PaidTowardDeductible";

            case 5:
               return "PaidByPayer";

            case 6:
               return "PaidByCoordinationBenefits";

            case 7:
               return "TotalOutOfPocket";

            case 8:
               return "TotalPaid";

            case 9:
               return "IngredientCost";

            case 10:
               return "DispensingFee";

            case 11:
               return "AverageWholesalePrice";

            case 12:
               return "PayerPlanPeriodId";

            default:
               throw new NotImplementedException();
         }
      }

      public int GetOrdinal(string name)
      {
         throw new NotImplementedException();
      }

      public string GetString(int i)
      {
         throw new NotImplementedException();
      }

      public int GetValues(object[] values)
      {
         throw new NotImplementedException();
      }

      public bool IsDBNull(int i)
      {
         throw new NotImplementedException();
      }

      public object this[string name]
      {
         get { throw new NotImplementedException(); }
      }

      public object this[int i]
      {
         get { throw new NotImplementedException(); }
      }
      #endregion
   }
}
