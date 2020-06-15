using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("复合单反审核反写日计划")]
    public class XYFHUAuditOpt : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("F_SZXY_Machine");// 
            e.FieldKeys.Add("F_SZXY_FIDH");//   RJHFID
            e.FieldKeys.Add("F_SZXY_FEntryIDH");//  RJHROWID
            e.FieldKeys.Add("F_SZXY_MATERIALID");// 
            e.FieldKeys.Add("F_SZXY_ligature");// 
            e.FieldKeys.Add("F_SZXY_MotherVolume");//  
            e.FieldKeys.Add("F_SZXY_OrgId");//  
            e.FieldKeys.Add("F_SZXY_InArea");

        }

        /// <summary>
        /// 开始获取选中单据信息
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);

            selectedRows = e.SelectedRows.Select(s => s.DataEntity);

        }

 
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            if (selectedRows != null && selectedRows.Count() != 0)
            {
                IMetaDataService metadataService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
                string SoureFormId = "SZXY_FHJTRSCJH";
                //获取单据元数据
                FormMetadata BilMetada = metadataService.Load(Context, SoureFormId) as FormMetadata;

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
                    decimal Area = 0;
                    string RJHFid = string.Empty;
                    string RJHRowId = string.Empty;
                    RJHFid = Convert.ToString(dy["F_SZXY_FIDH"]);
                    Area = Convert.ToDecimal(dy["F_SZXY_InArea"]);
                    RJHRowId = Convert.ToString(dy["F_SZXY_FEntryIDH"]);
                    string F_SZXY_MotherVolume = Convert.ToString(dy["F_SZXY_MotherVolume"]);
                    string F_SZXY_ligature = Convert.ToString(dy["F_SZXY_ligature"]);


                    if (Area != 0 && !RJHFid.IsNullOrEmptyOrWhiteSpace())
                    {
                        DynamicObject RJHObejct = Utils.LoadFIDBillObject(this.Context, SoureFormId, RJHFid);
                        if (RJHObejct["SZXY_FHJTRSCJHEntry"] is DynamicObjectCollection SoureEntry)
                        {
                            //var Row = from row in SoureEntry
                            //          where Convert.ToString(row["id"]).EqualsIgnoreCase(RJHRowId)
                            //          select row;
                            foreach (DynamicObject SoureRow in SoureEntry.Where(p => Convert.ToString(p["id"]).EqualsIgnoreCase(RJHRowId)))
                            {
                                decimal ResArea = Convert.ToDecimal(SoureRow["F_SZXY_ProductionArea"])- Area;
                                SoureRow["F_SZXY_ProductionArea"] = ResArea;
                            }

                        }
                        var saveResult = BusinessDataServiceHelper.Save(Context, BilMetada.BusinessInfo, RJHObejct);
                    }


                }
            }

        }


    }
}
