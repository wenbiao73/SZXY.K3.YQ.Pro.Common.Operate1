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
    [Description("流延单反审核反写日计划")]
    public class XYLYUAuditOpt : AbstractOperationServicePlugIn
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
            e.FieldKeys.Add("F_SZXY_FID");// RJHFID
            e.FieldKeys.Add("F_SZXY_FEntryID");//  RJHROWID
            e.FieldKeys.Add("F_SZXY_Material");// 
            e.FieldKeys.Add("F_SZXY_PTNo");// 
            e.FieldKeys.Add("F_SZXY_PlasticNo");// 
            e.FieldKeys.Add("F_SZXY_MachineH");// 
            e.FieldKeys.Add("F_SZXY_OrgId");// 
            e.FieldKeys.Add("F_SZXY_Area");
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

        /// <summary>
        ///扣减日计划面积
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            if (selectedRows != null && selectedRows.Count() != 0)
            {
                IMetaDataService metadataService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();

                //获取单据元数据
                FormMetadata BilMetada = metadataService.Load(Context, "SZXY_LYJTRSCJH") as FormMetadata;

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
                    string FormId = "SZXY_LYJTRSCJH";
                    if (dy["SZXY_XYLYEntry"] is DynamicObjectCollection entry)
                    {
                        decimal Area = 0;
                        string RJHFid = string.Empty;
                        string RJHRowId = string.Empty;
                        foreach (var item in entry.Where(m => !Convert.ToString(m["F_SZXY_PlasticNo"]).IsNullOrEmptyOrWhiteSpace()))
                        {
                            RJHFid = Convert.ToString(item["F_SZXY_FID"]);
                            Area = Convert.ToDecimal(item["F_SZXY_Area"]);
                            RJHRowId = Convert.ToString(item["F_SZXY_FEntryID"]);

                            if (Area != 0 && !RJHFid.IsNullOrEmptyOrWhiteSpace())
                            {
                                DynamicObject RJHObejct = Utils.LoadFIDBillObject(this.Context, FormId, RJHFid);
                                if (RJHObejct["SZXY_LYJTRSCJHEntry"] is DynamicObjectCollection SoureEntry)
                                {
                                    var Row = from row in SoureEntry
                                              where Convert.ToString(row["id"]).EqualsIgnoreCase(RJHRowId)
                                              select row;
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
    }
}
