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
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("分切单审核反写日计划")]
    public class FQDAuditOpt : AbstractOperationServicePlugIn
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
            e.FieldKeys.Add("F_SZXY_discardR");//报废原因
            e.FieldKeys.Add("F_SZXY_discardA");//报废面积
            e.FieldKeys.Add("F_SZXY_Blevel");//性能等级
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
                    //RJHFid = Convert.ToString(dy["F_SZXY_FIDH"]); SZXY_XYFQEntry
                    //Area = Convert.ToDecimal(dy["F_SZXY_AreaH"]);
                    //RJHRowId = Convert.ToString(dy["F_SZXY_RJHEntryID"]);

                    string F_SZXY_discardR = "";//报废原因
                    decimal F_SZXY_discardA = 0;//报废面积
                    
                    string FentryID = "";

                    List<string> InsSqlList = new List<string>();
                    InsSqlList.Clear();
                    if (dy["SZXY_XYFQEntry"] is DynamicObjectCollection Entry)
                    {
                        foreach (DynamicObject item in Entry)
                        {
                            FentryID = item["Id"].ToString();
                            Area = Convert.ToDecimal(item["F_SZXY_AREA"]);
                            //反写日计划实际完成面积   
                            RJHRowId = Convert.ToString(item["F_SZXY_RJHEntryID"]);
                            if (!RJHRowId.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(RJHRowId) > 0)
                            {
                                string sql1 = $"/*dialect*/update SZXY_t_FQJTRSCJHEntry  set F_SZXY_ProductionArea=F_SZXY_ProductionArea+{Area}," +
                                  $"  where  FEntryID={RJHRowId}";

                                DBServiceHelper.Execute(Context, sql1);
                            }

                            F_SZXY_discardR = Convert.ToString(item["F_SZXY_discardR"]);
                            F_SZXY_discardA = Convert.ToDecimal(item["F_SZXY_discardA"]);
                            string F_SZXY_Blevel = "";
                            if (item["F_SZXY_Blevel"] is DynamicObject LevDo)
                            {
                                  F_SZXY_Blevel = LevDo["Id"].ToString();
                            }
                            if (F_SZXY_discardR.IsNullOrEmptyOrWhiteSpace()&& F_SZXY_discardA==0 && F_SZXY_Blevel.IsNullOrEmptyOrWhiteSpace())
                            {
                                string DelSql = $"/*dialect*/insert into SZXY_t_XYFQEntrycopy select  * from SZXY_t_XYFQEntry where SZXY_t_XYFQEntry.Fid={Id} and SZXY_t_XYFQEntry.FEntryID={FentryID} ";
                                InsSqlList.Add(DelSql);

                                string DelSql1 = $"/*dialect*/delete from SZXY_t_XYFQEntry where SZXY_t_XYFQEntry.Fid={Id} and SZXY_t_XYFQEntry.FEntryID={FentryID} ";
                                InsSqlList.Add(DelSql1);
                            }  
                        }

                        if (InsSqlList.Count>0)
                        {
                            DBServiceHelper.ExecuteBatch(Context, InsSqlList);
                        }
                    }
                 }
            }

        }


    }
}
