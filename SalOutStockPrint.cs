 

using Kingdee.BOS;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("销售出库单单据操作。")]
    public class SalOutStockPrint : AbstractBillPlugIn
    {

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

 
            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;
 
            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                if (billobj["SaleOrgId"] is DynamicObject org && billobj["CustomerID"] is DynamicObject CustDo)
                {
                    string Custid = Convert.ToString(CustDo["ID"]);

                    this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                
                    Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                    string Fid = "";
                    string DyModel = "";
                    int V = 0;
                    DataSet PrintModelDS = null;

                    if (billobj["SAL_OUTSTOCKENTRY"] is DynamicObjectCollection entry)
                    {
                        foreach (var item in entry.Where(m=> !Convert.ToString(m["MaterialID_Id"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToString(m["F_SZXY_CheckBox"]).EqualsIgnoreCase("true")))
                        {
                            Fid = this.Model.DataObject[0].ToString();
                        
                           if (this.Model.DataObject["F_SZXY_DYMB"] is DynamicObject DymodelDo)
                            {
                                DyModel= DymodelDo["Id"].ToString();
                            } 
                            
                            break;
                        }
                    }

                    if (!DyModel.IsNullOrEmptyOrWhiteSpace()&& DyModel!="")
                    {
                        DyModel = $" and T1.Fid={DyModel} ";
                    }
                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);

                    //Func < this.View,Context,DyModel,org["Id"].ToString(),MacInfo,Custid,PrintModelDS > ();
                     PrintModelDS = getPrintModel(this.View, Context, DyModel, org["Id"].ToString(), MacInfo, Custid, ref  V);

                    if (PrintModelDS!=null)
                    {
                        Print(PrintModelDS, V, Context, this.View, Fid);
                    }
                    
                   // Utils.TYPrint(this.View, Context, Convert.ToInt64(org["Id"]),billobj[0].ToString());
                    
                }
 
            }
        }

        private void Print(DataSet printModelDS, int v, Context context, IBillView view, string fid)
        {
            DataSet DS = printModelDS;
            Logger.Debug("打印---", "------------BEGIN------------------");

            Logger.Debug("---", $"------------打印条码或生产订单号Fid为：{fid}------------------");
            List<dynamic> listData = new List<dynamic>();

            listData.Clear();
            if (DS != null && DS.Tables.Count > 0 && DS.Tables[0].Rows.Count > 0)
            {

                foreach (DataRow Row in DS.Tables[0].Rows)
                {
                    string FListSQL = Convert.ToString(Row["F_SZXY_ListSQL"]);
                    if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                    if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                    string QSQL = "";
                    QSQL = $"{Convert.ToString(Row[6])} ";

             
                    QSQL= QSQL.Replace("-SqlFid-", fid);
                    QSQL = $"{QSQL} {FListSQL}";

                    Logger.Debug("替换拼接sql:", QSQL);
  
                    var ReportModel = new
                    {
                        FID = Convert.ToString(Row[0]),
                        report = Convert.ToString(Row[1]),
                        PrintAddress = Convert.ToString(Row[2]),
                        PrintQty = Convert.ToString(Row[3]),
                        ConnString = Convert.ToString(Row[5]),
                        QuerySQL = QSQL
                    };
                    if (QSQL != "")
                    {
                        DataSet SelNullDS = DBServiceHelper.ExecuteDataSet(Context, $"/*dialect*/{QSQL}");

                        if (SelNullDS != null && SelNullDS.Tables.Count > 0 && SelNullDS.Tables[0].Rows.Count > 0)
                        {
                            listData.Add(ReportModel);
                        }
                    }
                    Logger.Debug("最终打印查询SQL:", QSQL);
                }
            }
            string strJson = "";
            if (listData.Count > 0 && listData != null)
            {
                strJson = Newtonsoft.Json.JsonConvert.SerializeObject(listData);
            }

            if (strJson != "")
            {
                //调用打印
                string SQL = "/*dialect*/select F_SZXY_EXTERNALCONADS from SZXY_t_ClientExtern ";
                DataSet ids = DBServiceHelper.ExecuteDataSet(Context, SQL);

                if (ids != null && ids.Tables.Count > 0 && ids.Tables[0].Rows.Count > 0)
                {
                    string linkUrl = Convert.ToString(ids.Tables[0].Rows[0][0]).Replace("\\", "\\\\");// @"C:\Users\Administrator\Desktop\Grid++Report 6old\Grid++Report 6\Samples\CSharp\8.PrintInForm\bin\Debug\PrintReport.exe";
                    if (v == 0)
                    {
                        if (!strJson.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);
                        else View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                    }
                    else
                    {
                        if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Preview " + strJson);
 
                        else View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                    }
                }
                else
                {
                    Logger.Debug("客户端外接配置查询返回为空", "不调打印");
                }
                View.SendDynamicFormAction(View);
            }

            Logger.Debug("打印---", "---------------END------------------");

        }

        /// <summary>
        /// 分切打印
        /// </summary>
        /// <param name="View"></param>
        /// <param name="Context"></param>
        /// <param name="指定标签模板"></param>
        /// <param name="orgid"></param>
        /// <param name="MacInfo"></param>
        /// <param name="客户"></param>
        /// <param name="物料名"></param>
        /// <param name="条码或者生产订单号+行号的值"></param>
        /// <param name="条码或者生产订单号+行号的类型"></param>
        /// <param name=""></param>
        public static DataSet getPrintModel(IBillView View, Context Context, string ZDPrintModel, string orgid, string MacInfo, string F_SZXY_CUSTID, ref int V)
        {
            DataSet RESDS = null;
             
            string SelCust = $"/*dialect*/select T6.FNAME '客户名' from T_BD_CUSTOMER T5    left join T_BD_CUSTOMER_L t6 on t6.FCUSTID=T5.FCUSTID where t6.FCUSTID='{F_SZXY_CUSTID}'";
            DataSet SelCustDS = DBServiceHelper.ExecuteDataSet(Context, SelCust);
            if (SelCustDS != null && SelCustDS.Tables.Count > 0 && SelCustDS.Tables[0].Rows.Count > 0)
            {
                Logger.Debug($"客户:{Convert.ToString(SelCustDS.Tables[0].Rows[0][0])}",$"指定标签模板:{ZDPrintModel},MAC地址为{MacInfo}");
            }

            #region
            string SQL12 = "/*dialect*/select T1.FID,T1.F_SZXY_REPORT,T1.F_SZXY_PRINTMAC,T1.F_SZXY_PRINTQTY,T1.F_SZXY_LPRINT,T1.F_SZXY_CONNSTRING,T1.F_SZXY_QUERYSQL," +
                "T1.F_SZXY_ListSQL,T1.F_SZXY_CustID ,T1.F_SZXY_Model '产品型号', T3.FNAME, T1.F_SZXY_CHECKBOX 'CKB',T1.F_SZXY_Remark,T1.FNUMBER '标签' from SZXY_t_BillTemplatePrint T1" +
                " left join   T_BD_MATERIAL T2  on T2.FMATERIALID=T1.F_SZXY_Model " +
                " left join   T_BD_MATERIAL_L T3 on t2.FMATERIALID=T3.FMATERIALID   where" +
                "  T1.F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and T1.FUSEORGID='" + orgid + "'" +
                " and T1.F_SZXY_TYPESELECT='1'   and T1.FDOCUMENTSTATUS='C'  " + ZDPrintModel + " ";
            DataSet DS = null;
            string WhereSql = "";
 
                if (F_SZXY_CUSTID.IsNullOrEmptyOrWhiteSpace()|| F_SZXY_CUSTID=="")
                {
                    F_SZXY_CUSTID = "0";
                }
                //如果不为空 客户
                WhereSql = $" {SQL12}  and T1.F_SZXY_CustID={F_SZXY_CUSTID}   and F_SZXY_Remark='{MacInfo}'";
                DS = DBServiceHelper.ExecuteDataSet(Context, WhereSql);
 
                if (DS != null && DS.Tables.Count > 0 && DS.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow DR in DS.Tables[0].Rows)
                    {
                        V = 0;
                        if (!Convert.ToString(DR["CKB"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            V = Convert.ToInt32(DR["CKB"]);
                        }

                        RESDS = DS;
                    }
                }
                 
               else
               {
                  WhereSql = $" {SQL12}  and T1.F_SZXY_CustID=0   and F_SZXY_Remark='{MacInfo}'  ";
                  DS = DBServiceHelper.ExecuteDataSet(Context, WhereSql);

                if (DS != null && DS.Tables.Count > 0 && DS.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow DR in DS.Tables[0].Rows)
                    {
                        V = 0;
                        if (!Convert.ToString(DR["CKB"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            V = Convert.ToInt32(DR["CKB"]);
                        }
                        RESDS = DS;
                    }
                }
                else
                {
                    WhereSql = $" {SQL12}  and T1.F_SZXY_CustID={F_SZXY_CUSTID}   and F_SZXY_Remark='' ";
                    DS = DBServiceHelper.ExecuteDataSet(Context, WhereSql);

                    if (DS != null && DS.Tables.Count > 0 && DS.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow DR in DS.Tables[0].Rows)
                        {
                            V = 0;
                            if (!Convert.ToString(DR["CKB"]).IsNullOrEmptyOrWhiteSpace())
                            {
                                V = Convert.ToInt32(DR["CKB"]);
                            }
                            RESDS = DS;
                        }
                    }
           
                        else
                        {
                            WhereSql = $" {SQL12}  and T1.F_SZXY_CustID=0 and F_SZXY_Remark='' ";
                            DS = DBServiceHelper.ExecuteDataSet(Context, WhereSql);

                            if (DS != null && DS.Tables.Count > 0 && DS.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow DR in DS.Tables[0].Rows)
                                {
                                    V = 0;
                                    if (!Convert.ToString(DR["CKB"]).IsNullOrEmptyOrWhiteSpace())
                                    {
                                        V = Convert.ToInt32(DR["CKB"]);
                                    }
                                    RESDS = DS;
                                }
                            }
                            else
                            {
                                View.ShowWarnningMessage("没有匹配到模板！");
                            }
                        }
                    }
                 }
 
            Logger.Debug("匹配单据套打模板编码Sql", WhereSql);
            if (RESDS != null && RESDS.Tables.Count > 0 && RESDS.Tables[0].Rows.Count > 0)
            {
                string BQ = Convert.ToString(RESDS.Tables[0].Rows[0]["标签"]);
                Logger.Debug("匹配单据套打模板编码", BQ);
            }
            return RESDS;
            #endregion
        }
     }
}
