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
    [Description("复合单审核反写日计划")]
    public class XYFHAuditOpt : AbstractOperationServicePlugIn
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
                    DynamicObject F_SZXY_MotherVolume =  dy["F_SZXY_MotherVolume"] as  DynamicObject;
                    DynamicObject F_SZXY_ligature = dy["F_SZXY_ligature"] as DynamicObject;


                    if (Area != 0 && !RJHFid.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(RJHFid) > 0 && Convert.ToInt32(RJHRowId) > 0)
                    {

                        //单据头反写复合日计划实际完成面积  
            
                        //DynamicObject RJHObejct = Utils.LoadFIDBillObject(this.Context, SoureFormId, RJHFid);
                        //if (RJHObejct["SZXY_FHJTRSCJHEntry"] is DynamicObjectCollection SoureEntry)
                        //{
                            //var Row = from row in SoureEntry
                            //          where Convert.ToString(row["id"]).EqualsIgnoreCase(RJHRowId)
                            //          select row;
                            //foreach (DynamicObject SoureRow in SoureEntry.Where(p => Convert.ToString(p["id"]).EqualsIgnoreCase(RJHRowId)))
                            //{
                                string MotherVolume = "";
                                string ligature = "";
                                if (F_SZXY_MotherVolume != null) MotherVolume = F_SZXY_MotherVolume["Id"].ToString();
                              if ( F_SZXY_ligature != null) ligature = F_SZXY_ligature["Id"].ToString();
                           
                                string sql1 = $"/*dialect*/update SZXY_t_FHJTRSCJHEntry  set F_SZXY_ProductionArea=F_SZXY_ProductionArea+{Area}," +
                                             $" F_SZXY_WIRED='{ligature}', F_SZXY_MVolume='{MotherVolume}'" +
                                             $"  where Fid={RJHFid}" +
                                             $" and FEntryID={RJHRowId} ";
                                DBServiceHelper.Execute(Context, sql1);
                            //}

                        //}
                        //var saveResult = BusinessDataServiceHelper.Save(Context, BilMetada.BusinessInfo, RJHObejct);
                    }


                }
            }

        }


    }
}
