 
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.PropertyCheck;
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
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("物流快递单单据操作。")]
    public class DeliveryBillPlug : AbstractBillPlugIn
    {


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);


            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    Utils.TYPrint(this.View, Context, orgid, Convert.ToString(this.Model.DataObject[0]));
               
                    Logger.Debug("物流快递单打印完毕----------清除已勾选复选框-----:", $"---------");
                    DynamicObjectCollection entry2 = this.Model.DataObject["SZXY_XYWLKDDEntry"] as DynamicObjectCollection;

                    
                    bool flag29 = entry2 != null;
                    if (flag29)
                    {
                        foreach (DynamicObject item in from ck in entry2
                                                       where Convert.ToString(ck["F_SZXY_Print"]).EqualsIgnoreCase("true")
                                                       select ck)
                        {
                            item["F_SZXY_Print"] = "false";
                        }


                    }
                    this.View.UpdateView();
                }
            }



            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGetReBackInfo"))
            {
                DynamicObject BillDO = this.Model.DataObject;
                if (BillDO["F_SZXY_OrgId"] is DynamicObject OrdDO)
                {
                    string OrgId= OrdDO["ID"].ToString();

                    if (BillDO["SZXY_XYWLKDDEntry"] is DynamicObjectCollection entry)
                    {
                        foreach (DynamicObject Row in entry)
                        {
                            string MoNo = Row["F_SZXY_PTNo"].ToString();
                            string MoLineNo = Row["F_SZXY_MOSEQ"].ToString();
                            if (!MoNo.IsNullOrEmptyOrWhiteSpace()&& !MoLineNo.IsNullOrEmptyOrWhiteSpace())
                            {
                                string SelSql = "/*dialect*/select count(T1.F_SZXY_BoxNo), isnull(sum(T1.F_SZXY_volume),0) ,ISNULL( sum(T1.F_SZXY_Area),0) ,T2.FBILLNO " +
                                                "from SZXY_t_KHHQDEntry T1 " +
                                                "left  join SZXY_t_KHHQD T2 on T1.FID = T2.FID " +
                                                $"where T1.F_SZXY_MoNOE = '{MoNo}' and T1.F_SZXY_MOLINENOE = '{MoLineNo}' and T2.F_SZXY_ORGID = '{OrgId}'" +
                                                $" group by T2.FBILLNO ";

                                using (DataSet fillData = DBServiceHelper.ExecuteDataSet(this.Context, SelSql))
                                {
                                    if (fillData != null && fillData.Tables.Count > 0 && fillData.Tables[0].Rows.Count > 0)
                                    {
                                        Row["F_SZXY_XS"] = fillData.Tables[0].Rows[0][0];
                                        Row["F_SZXY_JS"] = fillData.Tables[0].Rows[0][1];
                                        Row["F_SZXY_MJ"] = fillData.Tables[0].Rows[0][2];
                                        Row["F_SZXY_SIGNATURENO"] = fillData.Tables[0].Rows[0][3];
                                    }
                                };
 
                             }
                        }
                    }
                    this.View.UpdateView("F_SZXY_XYWLKDDEntity");
                }
               
            }
        }

 
 

    }
}
