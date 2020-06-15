 
 
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
    [Description("包装客户对应表审核反写包装单")]
    public class PackCustInfoAuditOpt : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);


            e.FieldKeys.Add("F_SZXY_TeamGroup");// 
            e.FieldKeys.Add("F_SZXY_OrgId");// 
            e.FieldKeys.Add("F_SZXY_operator");
            e.FieldKeys.Add("F_SZXY_Invalid");// 作废标示
            e.FieldKeys.Add("F_SZXY_OrderQty");// 订单数量
            e.FieldKeys.Add("F_SZXY_CustCode");//客户
            e.FieldKeys.Add("F_SZXY_CustBacth");// 客户批次
            e.FieldKeys.Add("F_SZXY_BC");// 
            e.FieldKeys.Add("F_SZXY_NO");//箱号
            e.FieldKeys.Add("F_SZXY_volume");// 卷数
            e.FieldKeys.Add("F_SZXY_NewDate");// 
            e.FieldKeys.Add("F_SZXY_CustCartonNo");// 客户箱号
            e.FieldKeys.Add("F_SZXY_ProductionTask1");// 生产任务单号
            e.FieldKeys.Add("F_SZXY_MoLineNo");
            e.FieldKeys.Add("F_SZXY_Model");// 产品型号
            e.FieldKeys.Add("F_SZXY_PLy");// 
            e.FieldKeys.Add("F_SZXY_Width");
            e.FieldKeys.Add("F_SZXY_Len");
            e.FieldKeys.Add("F_SZXY_Area");
            e.FieldKeys.Add("F_SZXY_OriginalDate");//原日期
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

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
                    if (!Convert.ToString(dy["F_SZXY_Invalid"]).EqualsIgnoreCase("True"))
                    {

                        string BZ = "";
                        string Orgid = "";
                        string Operator = "";
                        decimal OrderQty = 0;
                        string Cust = "";
                        string CustBacth = "";
                        string BC = "";
                        if (dy["F_SZXY_TeamGroup"] is DynamicObject BZDO)
                        {
                           BZ= BZDO["Id"].ToString();
                        }
                        
                       
                        if (dy["F_SZXY_operator"] is DynamicObject OperatorDo)
                        {
                            Operator = OperatorDo["Id"].ToString();
                        }
                        if (dy["F_SZXY_CustCode"] is DynamicObject CustDo)
                        {
                            Cust = CustDo["Id"].ToString();
                        }
                        if (dy["F_SZXY_BC"] is DynamicObject BCDo)
                        {
                            BC = BCDo["Id"].ToString();
                        }
                        OrderQty =Convert.ToDecimal( dy["F_SZXY_OrderQty"]);
                        CustBacth = dy["F_SZXY_CustBacth"].ToString();

                        if (dy["SZXY_BZKHXXDYBEntry"] is DynamicObjectCollection Entry&& dy["F_SZXY_OrgId"] is DynamicObject OrgDo)
                        {
                            Orgid = OrgDo["Id"].ToString();
                            foreach (DynamicObject item in Entry)
                            {
                
                                string F_SZXY_NO = item["F_SZXY_NO"].ToString();
                                string F_SZXY_NewDate = item["F_SZXY_NewDate"].ToString();
                                string F_SZXY_OriginalDate = item["F_SZXY_OriginalDate"].ToString();
                                string F_SZXY_CustCartonNo = item["F_SZXY_CustCartonNo"].ToString();//新箱号
                                string MoNo = item["F_SZXY_ProductionTask1"].ToString();
                                string MoLineNo = item["F_SZXY_MoLineNo"].ToString();
                                string Mat = item["F_SZXY_Model_Id"].ToString();
                                decimal F_SZXY_PLy =Convert.ToDecimal( item["F_SZXY_PLy"].ToString());
                                decimal F_SZXY_Width = Convert.ToDecimal(item["F_SZXY_Width"].ToString());
                                decimal F_SZXY_Len = Convert.ToDecimal(item["F_SZXY_Len"].ToString());
                                decimal F_SZXY_Area = Convert.ToDecimal(item["F_SZXY_Area"].ToString());
                                decimal F_SZXY_volume = Convert.ToDecimal(item["F_SZXY_volume"].ToString());
                                string SQLPJ = "";
                                if (!F_SZXY_CustCartonNo.IsNullOrEmptyOrWhiteSpace())
                                {
                                    SQLPJ =$"  F_SZXY_CTNNO = '{F_SZXY_CustCartonNo}', ";
                                }

                                if (!F_SZXY_NO.IsNullOrEmptyOrWhiteSpace())
                                {
                                    long MoId = 0;
                                    long FENTRYID = 0;
                                    if (!MoLineNo.IsNullOrEmptyOrWhiteSpace() && !MoNo.IsNullOrEmptyOrWhiteSpace())
                                    {
                                        string sql = $"/*dialect*/select T2.fid,T2.FENTRYID  from " +
                                                     $"T_PRD_MO T1 " +
                                                     $"inner join " +
                                                     $" T_PRD_MOENTRY T2 on T1.fid = T2.fid " +
                                                     $"where T1.FPRDORGID = '{Orgid}' and T1.FBILLNO='{MoNo}' and T2.FSEQ='{MoLineNo}'";

                                        DataSet Selds = DBServiceHelper.ExecuteDataSet(Context, sql);
                                        if (Selds != null && Selds.Tables.Count > 0 && Selds.Tables[0].Rows.Count > 0)
                                        {
                                            MoId = Convert.ToInt64(Selds.Tables[0].Rows[0]["fid"].ToString());
                                            FENTRYID = Convert.ToInt64(Selds.Tables[0].Rows[0]["FENTRYID"].ToString());

                                            string updatesql1 = $"/*dialect*/update   SZXY_t_BZDHEntry " +
                                                $"set " +
                                                $"F_SZXY_DATE = '{F_SZXY_NewDate}', " +
                                                $" F_SZXY_MATERIAL = '{Mat}', " +
                                                $" F_SZXY_PLY = '{F_SZXY_PLy}', " +
                                                $" F_SZXY_WIDTH = '{F_SZXY_Width}', " +
                                                $" F_SZXY = '{F_SZXY_Len}', " +
                                                $" F_SZXY_AREA1 = '{F_SZXY_Area}', " +
                                                $" F_SZXY_TEAMGROUP1 = '{BZ}', " +
                                                $" F_SZXY_CLASSES1 = '{BC}', " +
                                                SQLPJ +
                                                $" F_SZXY_OPERATOR = '{Operator}'," +
                                                $" F_SZXY_CUSTBACTH = '{CustBacth}'," +
                                                $" F_SZXY_BACTHQTY = '{OrderQty}'," +
                                                $"  F_SZXY_PUDNO = '{MoNo}'," +
                                                $"  F_SZXY_JQTY = '{F_SZXY_volume}'," +
                                                $" F_SZXY_PUDLINENO = '{MoLineNo}' " +
                                                $" where F_SZXY_CTNNO = '{F_SZXY_NO}'";
                                            DBServiceHelper.Execute(Context, updatesql1);

                                            //string updatesql2 = $"/*dialect*/update a   " +
                                            //     $"set a.F_SZXY_MOID={MoId} " +
                                            //     $" from  SZXY_t_BZD a,SZXY_t_BZDHEntry b  " +
                                            //     $" where a.Fid=B.Fid and  b.F_SZXY_CTNNO = '{F_SZXY_NO}'";
                                            //DBServiceHelper.Execute(Context, updatesql2);
                                        }
                                       
                                     
                                    }

                                }
                            }

                    }
                }
            }
 
            }

        }


    }
}
