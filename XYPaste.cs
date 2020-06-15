using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
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
    [Description("浆料单据操作。")]
    public class XYPaste : AbstractBillPlugIn
    {


        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            this.Model.ClearNoDataRow();
        }
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && !formID.IsNullOrEmptyOrWhiteSpace())
            {
                if (!Convert.ToString(this.Model.GetValue("F_SZXY_JLNo")).IsNullOrEmptyOrWhiteSpace()) throw new Exception("编号不允许重复生成！");
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);


                DynamicObjectCollection entry = billobj["SZXY_JLDEntry"] as DynamicObjectCollection;//单据体

                if (billobj["F_SZXY_Mac"] is DynamicObject MacObj && billobj["F_SZXY_OrgId1"] is DynamicObject orgobj)
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    string MacId = MacObj["ID"].ToString();
                    string MacNumber = MacObj["Number"].ToString();
                    string F_SZXY_PTNOH = Convert.ToString(this.Model.GetValue("F_SZXY_PTNOH"));//生产订单号
                    string F_SZXY_POLineNum = Convert.ToString(this.Model.GetValue("F_SZXY_POLineNum"));//生产订单行号
                    if (F_SZXY_PTNOH.IsNullOrEmptyOrWhiteSpace() || F_SZXY_POLineNum.IsNullOrEmptyOrWhiteSpace()) throw new Exception("生产订单号和生产订单行号不能为空");
                    //string SelMoSql = $"/*dialect*/select FPLANSTARTDATE, from T_PRD_MO T1 Left join T_PRD_MOENTRY T2 on T1.Fid=T2.Fid " +
                    //                 $"where T1.FBILLNO ='{F_SZXY_PTNOH}' and T2.FSeq={F_SZXY_POLineNum}  and T1.FPRDORGID={orgid}";
                    //DataSet SelMoDS = Utils.CBData(SelMoSql, Context);
                    //if (SelMoDS != null && SelMoDS.Tables.Count > 0 && SelMoDS.Tables[0].Rows.Count > 0)
                    //{
                    //    DateNo= Convert.ToDateTime(SelMoDS.Tables[0].Rows[0][0]).ToString("yyMMdd");
                    //}
                    //else throw new Exception("没有找到生产订单信息");
                    int index = Model.GetEntryRowCount("F_SZXY_JLD");
                    string OrgName = orgobj["Name"].ToString();
                    string No1 = "";
                    if (OrgName.Contains("深圳"))
                    {
                        No1 = "S";
                    }
                    else if (OrgName.Contains("合肥"))
                    {
                        No1 = "H";
                    }
                    else if (OrgName.Contains("江苏"))
                    {
                        No1 = "J";
                    }
                    string PF = "", BZ = "";
                    if (this.Model.GetValue("F_SZXY_FormulaH1") is DynamicObject PFObj) PF = PFObj["FDataValue"].ToString();
                    string MacName = MacObj["Name"].ToString();
                    MacName = System.Text.RegularExpressions.Regex.Replace(MacName, @"[^0-9]+", "");

                    if (this.Model.GetValue("F_SZXY_TeamGroup") is DynamicObject Teamobj) BZ = Teamobj["Name"].ToString();
                    DateTime DateNo1 = Convert.ToDateTime(this.Model.GetValue("FDate"));
                    string DateNo = DateNo1.ToString("yyMMdd");

                    if (PF.IsNullOrEmptyOrWhiteSpace())
                    {
                        this.View.ShowWarnningMessage("请检查配方是否输入！"); return;
                    }

                    if (BZ.IsNullOrEmptyOrWhiteSpace())
                    {
                        this.View.ShowWarnningMessage("请检查班组是否输入！"); return;
                    }
                    //浆料编号
                    if (MacName != "" && PF != "" && BZ != "" && DateNo != "")
                    {
                        string LSH = Utils.GetLSH(Context,"JL", orgid, DateNo1.ToString("yyyyMMdd"),MacObj);

                        string JLNo = $"{No1}J{PF}{MacName}{DateNo}{BZ}{LSH}";
                        this.Model.SetValue("F_SZXY_JLNo", JLNo);
                        this.View.UpdateView("F_SZXY_JLNo");
                        //调用保存
                        this.Model.ClearNoDataRow();
                        billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                        Utils.Save(View, new DynamicObject[] { billobj }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                    }
                    else
                    {
                        throw new Exception("生成编码失败，请检查录入数据的完整性!");
                    }

                }
                else { this.View.ShowWarnningMessage("请检查机台和组织！"); return; }
 
            }

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {

                if (this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    Utils.TYPrint(this.View, Context, orgid);


                    if (this.Model.DataObject["SZXY_JLDEntry"] is DynamicObjectCollection entry2)
                    {
                        foreach (DynamicObject item in from ck in entry2
                                                       where Convert.ToString(ck["F_SZXY_PRINT"]).EqualsIgnoreCase("true")
                                                       select ck)
                        {
                            item["F_SZXY_PRINT"] = "false";
                        }
                        base.View.UpdateView("F_SZXY_XYTFEntity");
                    //    billobj["FFormId"] = View.BusinessInfo.GetForm().Id;
                    //    IOperationResult a = Utils.Save(View, new DynamicObject[]
                    //    {
                    //billobj
                    //    }, OperateOption.Create(), Context);
                    }
                }
            }
        }


        //public override void DataChanged(DataChangedEventArgs e)
        //{
        //    base.DataChanged(e);

        //    //  计算 产出重量=（面积* 面密度/ 1000）F_SZXY_OutWeight  F_SZXY_Area F_SZXY_ArealDensity
        //    if (e.Field.Key.EqualsIgnoreCase("F_SZXY_Area") || e.Field.Key.EqualsIgnoreCase("F_SZXY_ArealDensity"))
        //    {
        //        int m = e.Row;
        //        decimal F_SZXY_Area = Convert.ToDecimal(this.Model.GetValue("F_SZXY_Area", m));
        //        decimal F_SZXY_ArealDensity = Convert.ToDecimal(this.Model.GetValue("F_SZXY_ArealDensity", m));
        //        if (F_SZXY_ArealDensity != 0)
        //        {
        //            decimal F_SZXY_OutWeight = (F_SZXY_Area * F_SZXY_ArealDensity) / 1000;
        //            this.Model.SetValue("F_SZXY_OutWeight", F_SZXY_OutWeight, m);
        //        }
        //    }

        //}


    }
}
