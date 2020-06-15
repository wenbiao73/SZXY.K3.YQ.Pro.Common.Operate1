using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.FieldElement;
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
    [Description("湿法单据操作。")]
    public class XYWet : AbstractBillPlugIn
    {

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
        }



        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            //在XY单输入机台、时间，系统根据机台、时间匹配日计划记录，自动将相同的日计划记录携带到XY复合单上
            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && billobj["F_SZXY_Mac"] is DynamicObject MacObj && !formID.IsNullOrEmptyOrWhiteSpace())
            {
                string MacId = MacObj["ID"].ToString();
                string MacNumber = MacObj["Number"].ToString();
                DynamicObjectCollection entry = billobj["SZXY_SFDEntry"] as DynamicObjectCollection;//单据体

                if (MacObj != null && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    DateTime Stime = Convert.ToDateTime(billobj["F_SZXY_DatetimeL"]);
                    if (Stime == DateTime.MinValue)
                    {
                        Stime = DateTime.Now;
                        //this.View.Model.SetValue("F_SZXY_DatetimeL", Stime);
                    }
                    if (Stime != DateTime.MinValue)
                    {
                        string Sql = $"/*dialect*/select  FLOOR(T2.F_SZXY_BWIDTH/T2.F_SZXY_WIDTH) '工位',T2.F_SZXY_CJ '车间'," +
                                       $"T2.F_SZXY_PTNo '生成订单号',T2.F_SZXY_MoLineNo '生成订单行号',T2.F_SZXY_MOID '生产订单内码' " +
                                       $" ,T2.F_SZXY_recipe '配方',T2.F_SZXY_Team '班组',T2.F_SZXY_Class '班次',T2.F_SZXY_CwType '卷芯类型' " +
                                       $" ,T2.F_SZXY_PLY '厚度',T2.F_SZXY_WIDTH '宽度',T2.F_SZXY_LEN '长度',T2.F_SZXY_MATERIAL '产品型号',  " +
                                       $"  T2.F_SZXY_AREA '面积',T2.F_SZXY_OPERATOR '操作员',T2.F_SZXY_SMark '特殊标志'," +
                                       $" T2.F_SZXY_SDate 'Stime', T2.F_SZXY_EndDate 'Etime',T2.FEntryID,T2.Fid " +
                                       $" from 	SZXY_t_SFJTRSCJH T1 " +
                                       $"Left join SZXY_t_SFJTRSCJHEntry T2 on T1.Fid=T2.Fid  " +
                                       $" where T2.F_SZXY_Machine='{MacId}' " +
                                       $" and T1.F_SZXY_OrgId={orgid} " +
                                       $" and CONVERT(datetime ,'{Stime}', 120) between CONVERT(datetime ,T2.F_SZXY_SDATE, 120)  and  CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                       $" and T1.FDOCUMENTSTATUS in  ('C')";

                        DataSet DpDs = DBServiceHelper.ExecuteDataSet(Context, Sql);

                        string PudDate = Convert.ToDateTime(View.Model.GetValue("FDate")).ToString("yyyyMMdd");
                        if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                        {
                 
                            string LSH = Utils.GetLSH(Context, "SF", orgid, PudDate,MacObj);
                            int Gw = Convert.ToInt32(DpDs.Tables[0].Rows[0]["工位"]);

                            string F_SZXY_Datetime = Convert.ToString(this.View.Model.GetValue("F_SZXY_DatetimeL"));
                            if (Gw == 0)
                            {
                                throw new Exception("无法生成单据，请检查日计划宽度");
                            }
                            DateTime dt = Stime;
                            string LastGenQty = Convert.ToString(this.Model.GetValue("F_SZXY_LastGenQty"));//上次生成编号
                            if (!LastGenQty.IsNullOrEmptyOrWhiteSpace() && this.Model.DataObject["SZXY_SFDEntry"] is DynamicObjectCollection Entry2)
                            {
                                foreach (var item in Entry2)
                                {
                                    if (Convert.ToString(item["F_SZXY_SFNO"]).Length>3)
                                    {
                                        string LastLsh = Convert.ToString(item["F_SZXY_SFNO"]).Substring(Convert.ToString(item["F_SZXY_SFNO"]).Length - 3);
                                        if (LastLsh.EqualsIgnoreCase(LastGenQty))
                                        {
                                            if (!F_SZXY_Datetime.IsNullOrEmptyOrWhiteSpace())
                                            {

                                                item["F_SZXY_ETime"] = Convert.ToDateTime(F_SZXY_Datetime);

                                            }
                                            else
                                            {
                                                item["F_SZXY_ETime"] = dt;
                                            }
                                        }
                                    }
                          

                                }

                            }
                            int n = this.Model.GetEntryRowCount("F_SZXY_SFDEntity");
                            for (int i = 0; i < Gw; i++)
                            {

                                int index = n + i;
                                //给单据头赋值
                                if (i==0)
                                {
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[0]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(DpDs.Tables[0].Rows[0]["生产订单内码"]));
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));
                                    if (DpDs.Tables[0].Rows[0]["操作员"] != null) this.Model.SetValue("F_SZXY_operator", Convert.ToInt64(DpDs.Tables[0].Rows[0]["操作员"]));

                                }

                                //单据体
                                #region 
                                this.Model.CreateNewEntryRow("F_SZXY_SFDEntity");
                                this.Model.SetValue("F_SZXY_RJHEntryID", Convert.ToString(DpDs.Tables[0].Rows[0]["FEntryID"]), index);
                                this.Model.SetValue("F_SZXY_PuONO", DpDs.Tables[0].Rows[0]["生成订单号"].ToString(), index);
                                this.Model.SetValue("F_SZXY_PuOLineNO", DpDs.Tables[0].Rows[0]["生成订单行号"].ToString(), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", DpDs.Tables[0].Rows[0]["特殊标志"].ToString(), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(DpDs.Tables[0].Rows[0]["班组"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(DpDs.Tables[0].Rows[0]["班次"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    string RMat = Utils.GetRootMatId(Convert.ToString(DpDs.Tables[0].Rows[0]["产品型号"]), orgid.ToString(), Context);
                                    this.Model.SetValue("F_SZXY_MaterialId", RMat, index);
                                } 
                                if (!Convert.ToString(DpDs.Tables[0].Rows[0]["配方"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    IViewService viewService = ServiceHelper.GetService<IViewService>();
                                    DynamicObject F_SZXY_formulaObj = this.Model.GetValue("F_SZXY_Formula", index) as DynamicObject;
                                    Utils.SetFZZLDataValue(viewService, ((DynamicObjectCollection)this.Model.DataObject["SZXY_SFDEntry"])[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Formula"), ref F_SZXY_formulaObj, Context, Convert.ToString(DpDs.Tables[0].Rows[0]["配方"]));
                                }
                                string F_SZXY_station = Convert.ToString(i + 1);

                                View.Model.SetValue("F_SZXY_station", F_SZXY_station, index);

                                this.Model.SetValue("F_SZXY_PLY", Convert.ToDecimal(DpDs.Tables[0].Rows[0]["厚度"]), index);
                                this.Model.SetValue("F_SZXY_Width", Convert.ToDecimal(DpDs.Tables[0].Rows[0]["宽度"]), index);
                                this.Model.SetValue("F_SZXY_LEN", Convert.ToDecimal(DpDs.Tables[0].Rows[0]["长度"]), index);
                                this.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(DpDs.Tables[0].Rows[0]["面积"]), index);
                                //this.Model.SetValue("F_SZXY_PUTWEIGHT", Convert.ToDecimal(DpDs.Tables[0].Rows[0]["投入重量"]), index);
                                
                                if (!F_SZXY_Datetime.IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_STime", Convert.ToDateTime(F_SZXY_Datetime), index);
                                    this.Model.SetValue("F_SZXY_ETime", Convert.ToDateTime(F_SZXY_Datetime), index);
                                }
                                else
                                {                                                

                                    this.Model.SetValue("F_SZXY_STime", dt, index);
                                    this.Model.SetValue("F_SZXY_ETime", dt, index);
                                }

                                if (!Convert.ToString(DpDs.Tables[0].Rows[0]["卷芯类型"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CwType_Id", Convert.ToString(DpDs.Tables[0].Rows[0]["卷芯类型"]), index);
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
                                else if (OrgName.Contains("常州"))
                                {
                                    No1 = "C";
                                }
                                else
                                {
                                    this.View.ShowWarnningMessage("本组织不生产湿法产品！");
                                    return;
                                }
                                string PF = "", BZ = "";
                                if (this.Model.GetValue("F_SZXY_Formula", index) is DynamicObject PFObj) PF = PFObj["FDataValue"].ToString();
                                string MacName = MacObj["Name"].ToString();
                                MacName = System.Text.RegularExpressions.Regex.Replace(MacName, @"[^0-9]+", "");
                                if (MacName.IsNullOrEmptyOrWhiteSpace())
                                {
                                    MacName = "0";
                                }
                                DateTime DateNo1 = Convert.ToDateTime(this.Model.GetValue("FDate"));
                                string DateNo = DateNo1.ToString("yyMMdd");
                                if (this.Model.GetValue("F_SZXY_TeamGroup", index) is DynamicObject Teamobj) BZ = Teamobj["Name"].ToString();
                                //工位号

                                //if (this.Model.GetValue("F_SZXY_CwType", index) is DynamicObject CwObj) CW = CwObj["Number"].ToString();
                                Logger.Debug("SFNo--", $"{PF}+ {BZ}+{DateNo}+{No1}+ {MacName}");
                                //湿法编号
                                if (PF != "" && BZ != "" && DateNo != "" && No1 != "" && MacName != "" && this.View.BusinessInfo.GetForm().Id != "")
                                {
                                    string SFNo = $"{No1}W{PF}{MacName}{DateNo}{BZ}{F_SZXY_station}{LSH}";
                                    this.Model.SetValue("F_SZXY_SFNO", SFNo, index);
                                    if (i == (Gw - 1))
                                    {
                                        this.Model.SetValue("F_SZXY_LastGenQty", LSH);
                                    }
                                }
                                else throw new Exception("生成编码失败，请检查录入数据的完整性!");
                                #endregion
                            }
                            // DynamicObject[] BillObjs = { billobj };
                            //调用保存


                            billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            Utils.Save(View, new DynamicObject[] { billobj }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                            this.View.UpdateView();
                        }
                        else
                        {
                            this.View.ShowMessage("没有匹配到日计划");return;
                        }
                    }
                }

            }

            //打印按钮
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    Utils.TYPrint(this.View, Context, orgid);


                    if (this.Model.DataObject["SZXY_SFDEntry"] is DynamicObjectCollection entry2)
                    {
                        foreach (DynamicObject item in from ck in entry2
                                                       where Convert.ToString(ck["F_SZXY_PRINT"]).EqualsIgnoreCase("true")
                                                       select ck)
                        {
                            item["F_SZXY_PRINT"] = "false";
                        }
                        View.UpdateView("F_SZXY_SFDEntity");
                        //billobj["FFormId"] = View.BusinessInfo.GetForm().Id;
                        //IOperationResult a = Utils.Save(View, new DynamicObject[]
                        //{
                        //    billobj
                        //}, OperateOption.Create(), Context);
                    }
                }
            }


        }


        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            //  计算 产出重量=（面积* 面密度/ 1000）F_SZXY_OutWeight  F_SZXY_Area F_SZXY_ArealDensity
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_Area") || e.Field.Key.EqualsIgnoreCase("F_SZXY_ArealDensity"))
            {
                int m = e.Row;
                decimal F_SZXY_Area = Convert.ToDecimal(this.Model.GetValue("F_SZXY_Area", m));
                decimal F_SZXY_ArealDensity = Convert.ToDecimal(this.Model.GetValue("F_SZXY_ArealDensity", m));
                if (F_SZXY_ArealDensity != 0)
                {
                    decimal F_SZXY_OutWeight = (F_SZXY_Area * F_SZXY_ArealDensity) / 1000;
                    this.Model.SetValue("F_SZXY_OutWeight", F_SZXY_OutWeight, m);
                }
            }
            //工时计算
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_STime") || e.Field.Key.EqualsIgnoreCase("F_SZXY_ETime"))
            {
                int m = e.Row;
                if (Convert.ToString(this.Model.GetValue("F_SZXY_STime", m)) != "" && Convert.ToString(this.Model.GetValue("F_SZXY_ETime", m)) != "")
                {
                    DateTime Stime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_STime", m));
                    DateTime Etime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_ETime", m));
                    TimeSpan time = Etime - Stime;

                    this.Model.SetValue("F_SZXY_MANHOUR", time.TotalMinutes, m);
                    this.View.UpdateView("F_SZXY_MANHOUR", m);
                }
            }


            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_YL"))
            {
                if (!e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {
                    string YLKey = "F_SZXY_YL";
                    string MoNOKey = "F_SZXY_PUONO";
                    string MoLineNOKey = "F_SZXY_PUOLINENO";
                    Utils.CheckRawMaterialIsTrue(this.View, Context, YLKey, MoNOKey, MoLineNOKey, e.Row,"SF");

                }
            }
        }

        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            DynamicObject BillHead = this.Model.DataObject;
            if (BillHead["SZXY_SFDEntry"] is DynamicObjectCollection entry)
            {
                foreach (var item in entry.Where(m => !Convert.ToString(m["F_SZXY_SFNO"]).IsNullOrEmptyOrWhiteSpace()))
                {
                    DateTime Stime = Convert.ToDateTime(item["F_SZXY_STIME"]);
                    DateTime Etime = Convert.ToDateTime(item["F_SZXY_ETIME"]);
                    int S = DateTime.Compare(Stime, Etime);
                    if (S > 0)
                    {
                        throw new Exception($"开始时间不能大于结束时间！！");
                    }
                    int E = DateTime.Compare(Etime, Stime);
                    if (E < 0)
                    {
                        throw new Exception($"结束时间不能大于开始时间！！");
                    }
                }
            }

            base.BeforeSave(e);

        }
    }
}
