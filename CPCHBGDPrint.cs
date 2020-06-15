using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{

    [HotUpdate]
    [Description("产品出货报告单打印操作。")]
    public class CPCHBGDPrint : AbstractBillPlugIn
    {

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);


            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                #region
                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    //是否指定标签模板
                    string PJSQL = " ";

                    //DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_LabelPrint") as DynamicObject;

                    //if (PrintTemp != null)
                    //{
                    //    string PId = Convert.ToString(PrintTemp["Id"]);

                    //    if (!PId.IsNullOrEmptyOrWhiteSpace())
                    //    {
                    //        PJSQL = $" and T1.Fid={PId} ";
                    //    }
                    //}


                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);
                    string F_SZXY_CustId = "";
                    string material = "";
                    string CustName = "";

                    DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_CPCHBGDEntry"] as DynamicObjectCollection;

                    if (entry1 != null)
                    {
                        StringBuilder STR = new StringBuilder();
                     
                        DataSet PrintModelDS = null;
                        int ckb = 0;

                        foreach (var item in entry1.Where(m => !Convert.ToString(m["F_SZXY_Barcode"]).IsNullOrEmptyOrWhiteSpace()))
                        {
                       
                            string BarNo = Convert.ToString(item["F_SZXY_Barcode"]);

                            if (item["F_SZXY_CustId"] is DynamicObject cust)
                            {
                                F_SZXY_CustId = Convert.ToString(cust["Id"]);
                                CustName = Convert.ToString(cust["Name"]);
                            }
                            if (item["F_SZXY_Material"] is DynamicObject Mat)
                            {
                                material = Convert.ToString(Mat["Name"]);
                            }

                            if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                            {
                               Logger.Debug("调用匹配模板前客户为", CustName);
                               PrintModelDS =XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CustId, material, BarNo, "BarNo", ref ckb);

                                if (PrintModelDS != null)
                                {

                                    XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{BarNo}'", "BarNo");
                                }

                            }
                        }
                      

                    }

                    #endregion


                }
            }
        }
    }
}
