using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("分切单反审核反写日计划")]
    public class FQDUnAuditOpt : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("F_SZXY_MAC");// 
            e.FieldKeys.Add("F_SZXY_RJHEntryID");//  RJHROWID
            e.FieldKeys.Add("F_SZXY_Material");// 
            e.FieldKeys.Add("F_SZXY_OrgId1");//  
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
        ///累加日计划面积
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            IViewService Services = ServiceHelper.GetService<IViewService>();
            if (selectedRows != null && selectedRows.Count() != 0)
            {
                IMetaDataService metadataService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
                //string SoureFormId = "SZXY_FHJTRSCJH";
                //获取单据元数据
                //FormMetadata BilMetada = metadataService.Load(Context, SoureFormId) as FormMetadata;

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
                    decimal Area = 0;
                    string RJHFid = string.Empty;
                    string RJHRowId = string.Empty;
                    //RJHFid = Convert.ToString(dy["F_SZXY_FIDH"]);
                    //Area = Convert.ToDecimal(dy["F_SZXY_Area"]);
                    //RJHRowId = Convert.ToString(dy["F_SZXY_RJHEntryID"]);


                    if (dy["SZXY_XYFQEntry"] is DynamicObjectCollection Entry)
                    {
                        foreach (var item in Entry)
                        {
                            Area = Convert.ToDecimal(item["F_SZXY_AREA"]);
                            //反写日计划实际完成面积   
                            RJHRowId = Convert.ToString(item["F_SZXY_RJHEntryID"]);
                            if (!RJHRowId.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(RJHRowId) > 0)
                            {
                                string sql1 = $"/*dialect*/update SZXY_t_FQJTRSCJHEntry  set F_SZXY_ProductionArea=F_SZXY_ProductionArea-{Area}," +
                                  $"  where  FEntryID={RJHRowId}";

                                DBServiceHelper.Execute(Context, sql1);
                            }
                        }
                    }

                    //string SelSql = $"/*dialect*/select * from SZXY_t_XYFQEntry where SZXY_t_XYFQEntry.Fid={Id} ";
                    //DataSet SelDS= DBServiceHelper.ExecuteDataSet(Context, SelSql);

                    //if (SelDS != null && SelDS.Tables.Count > 0 && SelDS.Tables[0].Rows.Count > 0)
                    //{

                    //}

                    string SelSql = $"/*dialect*/insert into SZXY_t_XYFQEntry select  * from SZXY_t_XYFQEntryCopy where SZXY_t_XYFQEntryCopy.Fid={Id} ";
                    DBServiceHelper.Execute(Context, SelSql);
                    string DelSql = $"/*dialect*/delete from SZXY_t_XYFQEntryCopy  where SZXY_t_XYFQEntryCopy.Fid ={Id} ";
                    DBServiceHelper.Execute(Context, DelSql);
                }
            }

        }


    }
}
