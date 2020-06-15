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
    [Description("成品重检反写分切单和分切外观检验单")]
    public class ReCheckAudit : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_SZXY_BarCode");// 
            e.FieldKeys.Add("F_SZXY_Culprit");//  
            e.FieldKeys.Add("F_SZXY_XNDJ");
            e.FieldKeys.Add("F_SZXY_HD");
            e.FieldKeys.Add("F_SZXY_BLDS");
            e.FieldKeys.Add("F_SZXY_OrgId");//
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
 

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
                    decimal Area = 0;

                    string RJHRowId = string.Empty;
       
                    if (dy["F_SZXY_OrgId"] is DynamicObject OrgDO)
                    {
                        string OrgId = Convert.ToString(OrgDO["Id"]);

          

                        if (dy["SZXY_XYCPJYEntry"] is DynamicObjectCollection Entry)
                        {
                            foreach (var item in Entry)
                            {
                                
                            
                                string FQNO =Convert.ToString(item["F_SZXY_BarCode"]);
                                string HD = Convert.ToString(item["F_SZXY_HD"]);
                                string BLDS = Convert.ToString(item["F_SZXY_BLDS"]);
                                DynamicObject Culprit = item["F_SZXY_Culprit"] as DynamicObject;
                                DynamicObject XNDJ = item["F_SZXY_XNDJ"] as DynamicObject;
                                //反写等级
                                if (XNDJ!=null)
                                {
                                   long DJId=Convert.ToInt64(XNDJ[0]);
                                    if (DJId>0)
                                    {
                                        string sql1 = $"/*dialect*/update SZXY_t_XYFQEntry  set F_SZXY_BLEVEL='{DJId}' " +
                                                             $"  where F_SZXY_BARCODEE= '{FQNO}' ";
                                        DBServiceHelper.Execute(Context, sql1);
                                        string sql2 = $"/*dialect*/update SZXY_t_FQJYDEntry  set F_SZXY_XNDJ='{DJId}' " +
                                                             $"  where F_SZXY_BARCODE= '{FQNO}' ";
                                        DBServiceHelper.Execute(Context, sql2);
                                    }

                                }    
                                //反写 不良原因
                                if (Culprit != null)
                                {
                                    string BLYYId = Convert.ToString(Culprit[0]);
                                    if (!BLYYId.IsNullOrEmptyOrWhiteSpace())
                                    {
                                        string sql1 = $"/*dialect*/update SZXY_t_XYFQEntry  set F_SZXY_BLYY='{BLYYId}' " +
                                                             $"  where F_SZXY_BARCODEE= '{FQNO}' ";
                                        DBServiceHelper.Execute(Context, sql1);
                                        string sql2 = $"/*dialect*/update SZXY_t_FQJYDEntry  set F_SZXY_BLYY='{BLYYId}' " +
                                                       $"  where F_SZXY_BARCODE= '{FQNO}' ";
                                        DBServiceHelper.Execute(Context, sql2);
                                    }
                                }


                                //反写 弧度
                                if (!HD.IsNullOrEmptyOrWhiteSpace())
                                {
                                    string sql1 = $"/*dialect*/update SZXY_t_FQJYDEntry  set F_SZXY_ARC='{HD}' " +
                                                            $"  where F_SZXY_BARCODE= '{FQNO}' ";
                                    DBServiceHelper.Execute(Context, sql1);
                                }

                                //反写 波浪读数
                                if (!BLDS.IsNullOrEmptyOrWhiteSpace())
                                {
                                    string sql1 = $"/*dialect*/update SZXY_t_FQJYDEntry  set F_SZXY_BLDS='{BLDS}' " +
                                                            $"  where F_SZXY_BARCODE= '{FQNO}' ";
                                    DBServiceHelper.Execute(Context, sql1);
                                }
                            }
                        }
         

             
                    }
   
                }
            }

        }


    }
}
