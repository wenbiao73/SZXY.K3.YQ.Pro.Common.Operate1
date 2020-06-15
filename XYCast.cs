using Kingdee.BOS;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.SystemParameter.PlugIn.Args;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("流延单据操作")]
    public class XYCast : AbstractBillPlugIn
    {
 
        public override void BeforeSave(Kingdee.BOS.Core.Bill.PlugIn.Args.BeforeSaveEventArgs e)
        {
            DynamicObject BillHead = this.Model.DataObject;
            this.Model.ClearNoDataRow();
            if (BillHead["SZXY_XYLYEntry"] is DynamicObjectCollection entry)
            {
                int index = 0;
                foreach (var item in entry.Where(m=>!Convert.ToString( m["F_SZXY_PlasticNo"]).IsNullOrEmptyOrWhiteSpace()))
                {
                    DateTime Stime = Convert.ToDateTime(item["F_SZXY_StartTime"]);
                    DateTime Etime = Convert.ToDateTime(item["F_SZXY_StopTime"]);
                  
                    int S = DateTime.Compare(Stime, Etime);
                    if (S > 0)
                    {
                        throw new Exception($"开始时间不能大于结束时间！");
                    }
                    int E = DateTime.Compare(Etime, Stime);
                    if (E < 0)
                    {
                        throw new Exception($"结束时间不能大于开始时间！");
                    }
                    index++;
                }
            }
          
            base.BeforeSave(e);

        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            //工时计算
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_StartTime") || e.Field.Key.EqualsIgnoreCase("F_SZXY_StopTime"))
            {
                int m = e.Row;
                if (Convert.ToString(this.Model.GetValue("F_SZXY_StartTime", m)) != "" && Convert.ToString(this.Model.GetValue("F_SZXY_StopTime", m)) != "")
                {
                    DateTime Stime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StartTime", m));
                    DateTime Etime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StopTime", m));
                    TimeSpan time = Etime - Stime;
                  
                    this.Model.SetValue("F_SZXY_ManHour", time.TotalMinutes, m);
                    this.View.UpdateView("F_SZXY_ManHour", m);
                }
            }



            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_YL"))
            {
                if (!e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {
                    string YLKey = "F_SZXY_YL";
                
                    string MoNOKey = "F_SZXY_PTNoH";
                    string MoLineNOKey = "F_SZXY_POLineNo";
                    Utils.CheckRawMaterialIsTrue(this.View,Context, YLKey, MoNOKey, MoLineNOKey,e.Row);
                  
               }
            }
        }

      

        //public override void AfterDoOperation(AfterDoOperationEventArgs e)
        //{
        //    base.AfterDoOperation(e);
        //    DynamicObject dataInfo = this.Model.DataObject as DynamicObject;
        //    long fid = Convert.ToInt64(dataInfo["Id"]);
        //    string billNo = Convert.ToString(dataInfo["FBillNo"]);
        //    if (billNo.IsEmpty())
        //    {
        //        return;
        //    }
        //    if (e.OperationResult != null)
        //    {
        //        string operation = e.Operation.Operation.ToUpperInvariant();
        //        string formID = this.View.BillBusinessInfo.GetForm().Id;
        //        if (operation.Equals("SAVE"))
        //        {
        //            DynamicObjectCollection Entry = dataInfo["SZXY_XYLYEntry"] as DynamicObjectCollection;
        //            foreach (var row in Entry)
        //            {
        //                string Area = row["F_SZXY_Area"].ToString();
        //                DynamicObject RawMateriallobj = row["F_SZXY_RawMateriall"] as DynamicObject;
        //                if (RawMateriallobj != null)
        //                {
        //                    int RawMateriall = Convert.ToInt32(RawMateriallobj["Id"]);

        //                }
        //            }
        //        }
        //    }
        //}
        //若流延单的面积和大于流延日计划的面积，则系统弹出预警提示“流延面积大于日计划的面积！”
        //public override void AfterSave(Kingdee.BOS.Core.Bill.PlugIn.Args.AfterSaveEventArgs e)
        //{
        //    base.AfterSave(e);//F_SZXY_Area
        //    DynamicObject bill= this.Model.DataObject;
        //    DynamicObjectCollection Entry= bill["SZXY_XYLYEntry"] as DynamicObjectCollection;
        //    foreach (var row in Entry)
        //    {
        //        Decimal LyArea = Convert.ToDecimal(row["F_SZXY_Area"]);
        //        string  SourseBillno = Convert.ToString(row["F_SZXY_SourceBillNo"]);
        //        DynamicObject Material = row["F_SZXY_Material"] as DynamicObject;
        //        string MaterialId = Convert.ToString(Material["Id"]);
        //        string sql = $"/*dialect*/ select T2.F_SZXY_Area from  SZXY_t_LYJTRSCJH  T1 left join SZXY_t_LYJTRSCJHEntry T2   where T2.F_SZXY_Material= {MaterialId} and T1.FBillNo={SourseBillno}";

        //    }
        //}


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            //输入机台、时间，系统根据机台、时间匹配日计划记录，自动将相同的日计划记录携带到XY复合单上
            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;
            DateTime Stime = Convert.ToDateTime(billobj["F_SZXY_Datetime"]);
            string Stimestr = Convert.ToString(billobj["F_SZXY_Datetime"]);
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && billobj["F_SZXY_MachineH"] != null)
            {
                int m = this.View.Model.GetEntryRowCount("F_SZXY_XYLYEntity");
                DynamicObject MacObj = billobj["F_SZXY_MachineH"] as DynamicObject;
                string MacId = MacObj["ID"].ToString();
                //string Mac = MacObj["FNumber"].ToString();

                DynamicObjectCollection entry = billobj["SZXY_XYLYEntry"] as DynamicObjectCollection;//单据体
                DynamicObject orgobj = billobj["F_SZXY_OrgId"] as DynamicObject;

                if (MacObj != null && orgobj != null)
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    if (Stime == DateTime.MinValue || Stimestr == "")
                    {
                        Stime = DateTime.Now;
                        //this.View.Model.SetValue("F_SZXY_Datetime", Stime);
                    }
                    //DateTime time = Convert.ToDateTime(billobj["F_SZXY_Datetime"]);
                    if (Stime != DateTime.MinValue)
                    {
                        string Sql = $"/*dialect*/select T2.F_SZXY_PTNo '生产订单单号',T2.F_SZXY_POLineNo '生产订单行号',T2.F_SZXY_MOID '生产订单内码'," +
                                      $"T2.F_SZXY_Team '班组',T2.F_SZXY_Class '班次',T2.F_SZXY_StretchSpeed '流延速度',T2.F_SZXY_CJ '车间'," +
                                      $"T2.F_SZXY_Tension '张力',T2.F_SZXY_Taper '锥度',T2.F_SZXY_SwingSpeed '摆幅速度'," +
                                      $"T2.F_SZXY_DieLip '模唇开度',T2.F_SZXY_dieOC '模头温度',T2.F_SZXY_C1Rollers 'C1辊温'," +
                                      $"T2.F_SZXY_recipe '配方',T2.F_SZXY_Material '产品型号Old',T2.F_SZXY_RawMaterial '原料批次号',T11.FMATERIALID '产品型号'," +
                                      $"T2.F_SZXY_PLy '厚度',T2.F_SZXY_Width '宽度',T2.F_SZXY_Len '长度',T2.F_SZXY_SMark '特殊标志'," +
                                      $"T2.F_SZXY_Area '面积',T2.F_SZXY_operator '操作员', T2.F_SZXY_PudUnitID '产品面积单位', " +
                                      $"  T2.F_SZXY_PudWeightUnitID '产品重量单位',T2.FEntryID,T2.Fid " +
                                      $" from SZXY_t_LYJTRSCJH T1 " +
                                      $"Left join SZXY_t_LYJTRSCJHEntry T2 on T1.Fid=T2.Fid " +
                                      $" left join T_BD_MATERIAL t10 on t10.FMATERIALID=T2.F_SZXY_Material " +
                                      $" left join   T_BD_MATERIAL T11 on T10.FMASTERID =T11.FMASTERID  and T11.FUSEORGID='{orgid}'" +
                                      $" where T2.F_SZXY_Machine='{MacId}' " +
                                      $" and T1.F_SZXY_OrgId={orgid} " +
                                      $" and T1.FDOCUMENTSTATUS in  ('C') " +
                                      $"and CONVERT(datetime  ,'{Stime}', 120) between  CONVERT(datetime  ,T2.F_SZXY_SDATE, 120)  and CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120)  ";

                        DataSet DpDs = DBServiceHelper.ExecuteDataSet(Context, Sql);

                        if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                        {

                            for (int i = 0; i < DpDs.Tables[0].Rows.Count; i++)
                            {
                                //获取生成编号按钮点击次数

                                if (Convert.ToString(this.Model.GetValue("F_SZXY_ClickQty")) != "")
                                {
                                    int clickqty = Convert.ToInt32(this.Model.GetValue("F_SZXY_ClickQty"));
                                    this.Model.SetValue("F_SZXY_ClickQty", clickqty + 1);
                                }
 
                                string Rollstr = Utils.GenRandrom(3);
                                int index = m + i;

                                #region   给单据头赋值
                               if (!Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(DpDs.Tables[0].Rows[i]["生产订单内码"]));
                                this.Model.SetValue("F_SZXY_PTNoH", Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单单号"]));
                                this.Model.SetValue("F_SZXY_POLineNo", Convert.ToInt32(DpDs.Tables[0].Rows[i]["生产订单行号"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMarkH", DpDs.Tables[0].Rows[i]["特殊标志"].ToString());
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(DpDs.Tables[0].Rows[i]["班组"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(DpDs.Tables[0].Rows[i]["班次"]));
                                this.Model.SetValue("F_SZXY_SFS", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["流延速度"]));
                                this.Model.SetValue("F_SZXY_TDLO", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["模唇开度"]));
                                this.Model.SetValue("F_SZXY_DieT", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["模头温度"]));
                                this.Model.SetValue("F_SZXY_Temperature", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["C1辊温"]));
                                this.Model.SetValue("F_SZXY_Strain", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["张力"]));
                                this.Model.SetValue("F_SZXY_Taper", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["锥度"]));
                                this.Model.SetValue("F_SZXY_SwSpeed", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["摆幅速度"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_KeyBoarder", Convert.ToInt64(DpDs.Tables[0].Rows[i]["操作员"]));
                                #endregion
                                #region 单据体赋值
                                this.Model.CreateNewEntryRow("F_SZXY_XYLYEntity");
                                this.Model.SetValue("F_SZXY_FEntryID", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["FEntryID"]), index);
                                this.Model.SetValue("F_SZXY_FID", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["FID"]), index);

                                this.Model.SetValue("F_SZXY_MoLineE", Convert.ToInt64(DpDs.Tables[0].Rows[i]["生产订单行号"]), index);
                                this.Model.SetValue("F_SZXY_PTNo", Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单单号"]), index);
                                this.Model.SetValue("F_SZXY_Machine", MacId, index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", DpDs.Tables[0].Rows[i]["特殊标志"].ToString(), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品型号"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Material", Convert.ToInt64(DpDs.Tables[0].Rows[i]["产品型号"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["配方"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Formula", DpDs.Tables[0].Rows[i]["配方"].ToString(), index);
                                // F_SZXY_Seq F_SZXY_RollNo F_SZXY_PlasticNo   F_SZXY_InPutQty F_SZXY_Output 
                                // if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品型号"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_RawMateriall", Convert.ToInt32(DpDs.Tables[0].Rows[i]["产品型号"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Team", Convert.ToInt64(DpDs.Tables[0].Rows[i]["班组"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Class", Convert.ToInt64(DpDs.Tables[0].Rows[i]["班次"]), index);

                                if (Convert.ToString(DpDs.Tables[0].Rows[i]["原料批次号"]) != "") this.Model.SetValue("F_SZXY_RawMaterial", Convert.ToString(DpDs.Tables[0].Rows[i]["原料批次号"]), index);

                                this.Model.SetValue("F_SZXY_PLy", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["厚度"]), index);
                                this.Model.SetValue("F_SZXY_Width", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["宽度"]), index);
                                this.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["长度"]), index);
                                this.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["面积"]), index);

                                //投入重量
                                decimal InPutQty = Convert.ToDecimal(this.View.Model.GetValue("F_SZXY_PLy", index)) * (Convert.ToDecimal(this.View.Model.GetValue("F_SZXY_Width", index)) * Convert.ToDecimal(this.View.Model.GetValue("F_SZXY_Len", index))) / Convert.ToDecimal(1000) * Convert.ToDecimal(0.91) / Convert.ToDecimal(1000) / Convert.ToDecimal(0.83);
                                this.Model.SetValue("F_SZXY_InPutQty", InPutQty, index);
                                //产出重量
                                decimal Output = (Convert.ToDecimal(this.View.Model.GetValue("F_SZXY_PLy", index)) * Convert.ToDecimal(this.View.Model.GetValue("F_SZXY_Width", index)) * Convert.ToDecimal(this.View.Model.GetValue("F_SZXY_Len", index))) / Convert.ToDecimal(1000) * Convert.ToDecimal(0.91) / Convert.ToDecimal(1000);
                                this.Model.SetValue("F_SZXY_Output", Output, index);

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品面积单位"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_PudAreaUnitID", DpDs.Tables[0].Rows[i]["产品面积单位"], index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品重量单位"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_PudWeightUnitID", DpDs.Tables[0].Rows[i]["产品重量单位"], index);
                                string   F_SZXY_Datetime  =Convert.ToString( this.Model.GetValue("F_SZXY_Datetime"));

                                this.Model.SetValue("F_SZXY_StartTime", DateTime.Now, index);
                                if (index != 0) this.Model.SetValue("F_SZXY_StopTime", DateTime.Now, index - 1);
                                if (!F_SZXY_Datetime.IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_StartTime",Convert.ToDateTime( F_SZXY_Datetime), index);
                                    if (index != 0) this.Model.SetValue("F_SZXY_StopTime", Convert.ToDateTime(F_SZXY_Datetime), index - 1);
                                }
                


                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_KeyBoarder", Convert.ToInt32(DpDs.Tables[0].Rows[i]["操作员"]), index);
                                this.Model.SetValue("F_SZXY_Date", Convert.ToDateTime(this.Model.GetValue("FDate")), index);
                                string Date = Convert.ToString(this.Model.GetValue("F_SZXY_Date", index));
                                DynamicObject formulaObejct = this.Model.GetValue("F_SZXY_Formula", index) as DynamicObject;
                                DynamicObject machineObejct = this.Model.GetValue("F_SZXY_Machine", index) as DynamicObject;
                                DynamicObject SpecialMarkObejct = this.Model.GetValue("F_SZXY_SpecialMark", index) as DynamicObject;
                                DynamicObject TeamObejct = this.Model.GetValue("F_SZXY_Team", index) as DynamicObject;
   
                                this.Model.SetValue("F_SZXY_RollNo", Rollstr, index);

                                if (!Date.IsNullOrEmptyOrWhiteSpace() && formulaObejct != null && SpecialMarkObejct != null && TeamObejct != null && machineObejct != null && !Rollstr.IsNullOrEmptyOrWhiteSpace())
                                {
                                    string Formid = this.View.BusinessInfo.GetForm().Id;

                                    string PudDate= Convert.ToDateTime(this.Model.GetValue("FDate")).ToString("yyyyMMdd");

                                    string  LSH = Utils.GetLSH(Context, "LY", orgid, PudDate,MacObj);

                                    if (LSH != "")
                                    {
                                        Logger.Debug("LSH=========================", LSH);
                                        this.Model.SetValue("F_SZXY_Seq", LSH, index);

                                        string value = Utils.GenNo(SpecialMarkObejct, orgobj, Formid, formulaObejct, machineObejct, Convert.ToDateTime(this.Model.GetValue("FDate")), TeamObejct, LSH, Rollstr);
                                        this.Model.SetValue("F_SZXY_PlasticNo", value, index);
                                    }
                            
                                  
                                    //单据日期
  
                            

                                    this.View.UpdateView("F_SZXY_PlasticNo", index);
                                    this.View.UpdateView("F_SZXY_XYLYEntity", index);
                                }
                                else throw new Exception("生成编码失败，请检查录入数据的完整性!");

                                #endregion
                            }
                            DynamicObject[] BillObjs = { billobj };
                            //调用保存
                            this.Model.ClearNoDataRow();
                            billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            Utils.Save(View, new DynamicObject[] { billobj }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                            this.View.UpdateView();
                        }
                        else
                        {
                            this.View.ShowMessage("没有匹配到日计划");
                        }

                    }
                }
             
            }


            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    Utils.TYPrint(this.View,Context,orgid,Convert.ToString(this.Model.DataObject[0]));
                    //#region
                    //string MacInfo = Utils.GetMacAddress();
                    //if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                    //{

                    //    string SQL12 = "/*dialect*/select FID,F_SZXY_REPORT,F_SZXY_PRINTMAC,F_SZXY_PRINTQTY,F_SZXY_LPRINT,F_SZXY_CONNSTRING,F_SZXY_QUERYSQL,F_SZXY_ListSQL,F_SZXY_CustID ,F_SZXY_Model '产品型号',F_SZXY_Remark 'MAC' from SZXY_t_BillTemplatePrint where  F_SZXY_BILLIDENTIFI='" + this.View.BusinessInfo.GetForm().Id + "' and FUSEORGID='" + orgid + "' and F_SZXY_TYPESELECT='1'  and FDOCUMENTSTATUS='C' and F_SZXY_Remark='" + MacInfo + "' ";
                    //    DataSet ds12 = DBServiceHelper.ExecuteDataSet(this.Context, SQL12);
                    //    if (ds12 != null && ds12.Tables.Count > 0 && ds12.Tables[0].Rows.Count > 0)
                    //    {
                    //        XYCast.Print(ds12, 0, Context, this.View, MacInfo);
                    //    }
                    //    else
                    //    {

                    //        string SQL = "/*dialect*/select FID,F_SZXY_REPORT,F_SZXY_PRINTMAC,F_SZXY_PRINTQTY,F_SZXY_LPRINT,F_SZXY_CONNSTRING,F_SZXY_QUERYSQL,F_SZXY_ListSQL,F_SZXY_CustID ,F_SZXY_Model '产品型号',F_SZXY_CHECKBOX 'CKB'  from SZXY_t_BillTemplatePrint where  F_SZXY_BILLIDENTIFI='" + this.View.BusinessInfo.GetForm().Id + "' and FUSEORGID='" + orgid + "' and F_SZXY_TYPESELECT='1' and FDOCUMENTSTATUS='C' and F_SZXY_Remark='' ";
                    //        DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, SQL);
                    //        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    //        {
                    //            int V = 0;
                    //            if (!Convert.ToString(ds.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                    //            {
                    //                V = Convert.ToInt32(ds.Tables[0].Rows[0]["CKB"]);
                    //            }
                    //            Print(ds, V, Context, this.View);
                    //        }
                    //        else
                    //        {
                    //            Logger.Debug("根据客户物料没有找到匹配模板，打通用模板", "");

                    //            DataSet SelNullModelDS = XYPack.GetNullCustPrintModel(Context, this.View, orgid);
                    //            if (SelNullModelDS != null && SelNullModelDS.Tables.Count > 0 && SelNullModelDS.Tables[0].Rows.Count > 0)
                    //            {
                    //                int V = 0;
                    //                if (!Convert.ToString(SelNullModelDS.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                    //                {
                    //                    V = Convert.ToInt32(SelNullModelDS.Tables[0].Rows[0]["CKB"]);
                    //                }
                    //                XYCast.Print(SelNullModelDS, V, Context, this.View);
                    //            }
                    //            else
                    //            {
                    //                this.View.ShowMessage("没有找到匹配的模板！");
                    //            }
                    //        }
                    //    }
                    //}
                    //#endregion
                    Logger.Debug("所有打印完毕----------清除已勾选复选框-----:", $"---------");
                    DynamicObjectCollection entry2 = this.Model.DataObject["SZXY_XYLYEntry"] as DynamicObjectCollection;
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

        }

        /// <summary>
        /// 调用打印
        /// </summary>
        /// <param name="套打模板dataset"></param>
        /// <param name="0 OR 1 0代表打印，1代表预览打印"></param>
        /// <param name="Context"></param>
        /// <param name="View"></param>
        /// <param name="Mac地址"></param>
        /// <param name="Fid"></param>
        public static void Print(DataSet ds, int v, Context Context, IBillView View,string MacInfo="",string Fid="")
        {
            Logger.Debug("打印---", "------------BEGIN------------------");
            string PKFid = "";
            if (!Convert.ToString(View.Model.GetPKValue()).IsNullOrEmptyOrWhiteSpace())
            {
                PKFid = Convert.ToString(View.Model.GetPKValue());
            }
            else
            {
                PKFid = Fid;
            }
                Logger.Debug("打印Fid---", $"-------{PKFid}-----");
            #region
            if (PKFid != "")
            {

                List<dynamic> listData = new List<dynamic>();
                listData.Clear();
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {

                    string FListSQL = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_ListSQL"]);

                    foreach (DataRow Row in ds.Tables[0].Rows)
                    {
                        string QSQL = Convert.ToString(Row[6]);
                        //System.Text.RegularExpressions.Regex.Replace(Convert.ToString(Row[6]), @"[^0-9]+", "");
                        if (View.BusinessInfo.GetForm().Id== "SAL_OUTSTOCK")
                        {
                            QSQL= Convert.ToString(Row[6]).Replace("@Fid@", $" XYFID={PKFid} {FListSQL} ");
                        }
                        else
                        {
                            QSQL = $"{Convert.ToString(Row[6])}{PKFid} {FListSQL}";
                        }
                      
                        // Kingdee.BOS.Log.Logger.Debug("QuerySQL", $"/*Dialect*/{Convert.ToString(Row[6])} {View.Model.GetPKValue()}");
                        if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                        if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                        var ReportModel = new
                        {
                            FID = Convert.ToString(Row[0]),
                            report = Convert.ToString(Row[1]),
                            PrintAddress = Convert.ToString(Row[2]),
                            PrintQty = Convert.ToString(Row[3]),
                        
                            ConnString = Convert.ToString(Row[5]),
                            // QuerySQL = $"/*dialect*/{Convert.ToString(Row[6])}{PKFid} {FListSQL}"
                            QuerySQL = QSQL

                        };
                        //string SelIsNull = $"/*dialect*/{Convert.ToString(Row[6])}{PKFid} {FListSQL}";
                        Logger.Debug("查询是否存在数据SQL:", QSQL);
                        DataSet SelNullDS = DBServiceHelper.ExecuteDataSet(Context, $"/*dialect*/{Convert.ToString(Row[6])}{PKFid} {FListSQL}");

                        if (SelNullDS != null && SelNullDS.Tables.Count > 0 && SelNullDS.Tables[0].Rows.Count > 0)
                        {
                            listData.Add(ReportModel);
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
                            //if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

                            else View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                        }
                    }
                    else
                    {
                        Logger.Debug("客户端外接配置查询返回为空", "不调打印");
                    }
                    View.SendDynamicFormAction(View);

                }
            


                //else
                //{
                //    List<dynamic> listData = new List<dynamic>();
                //    listData.Clear();
                //    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                //    {
                //        string FListSQL = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_ListSQL"]);
                //        foreach (DataRow Row in ds.Tables[0].Rows)
                //        {

                //            //Kingdee.BOS.Log.Logger.Debug("QuerySQL", $"/*Dialect*/{Convert.ToString(Row[6])} {View.Model.GetPKValue()}");
                //            if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                //            if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                //            var ReportModel = new
                //            {
                //                FID = Convert.ToString(Row[0]),
                //                report = Convert.ToString(Row[1]),
                //                PrintAddress = Convert.ToString(Row[2]),
                //                PrintQty = Convert.ToString(Row[3]),
                //                //Label = Convert.ToString(Row[4]),
                //                ConnString = Convert.ToString(Row[5]),
                //                QuerySQL = $"{Convert.ToString(Row[6])}{PKFid} {FListSQL}"
                //                //QuerySQL = $"{Convert.ToString(Row[6])}{View.Model.GetPKValue()} {FListSQL}"

                //            };
                //            string SelIsNull = $"{Convert.ToString(Row[6])}{PKFid} {FListSQL}";
                //            Logger.Debug("查询是否存在数据SQL:", $"{Convert.ToString(Row[6])} {PKFid} {FListSQL}");
                //            DataSet SelNullDS = DBServiceHelper.ExecuteDataSet(Context, SelIsNull);

                //            if (SelNullDS != null && SelNullDS.Tables.Count > 0 && SelNullDS.Tables[0].Rows.Count > 0)
                //            {
                //                listData.Add(ReportModel);
                //            }
                //            Logger.Debug("最终预览打印查询SQL:", $"{Convert.ToString(Row[6])} {PKFid} {FListSQL}");
                //        }
                //    }
                //    string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(listData);

                //    Logger.Debug("配置JSON", strJson);
                //    if (!strJson.IsNullOrEmptyOrWhiteSpace())
                //    {
                //        //调用打印
                //        string SQL = "/*dialect*/select F_SZXY_EXTERNALCONADS from SZXY_t_ClientExtern ";
                //        DataSet ids = DBServiceHelper.ExecuteDataSet(Context, SQL);
                //        if (ids != null && ids.Tables.Count > 0 && ids.Tables[0].Rows.Count > 0)
                //        {
                //            string linkUrl = Convert.ToString(ids.Tables[0].Rows[0][0]).Replace("\\", "\\\\");// @"C:\Users\Administrator\Desktop\Grid++Report 6old\Grid++Report 6\Samples\CSharp\8.PrintInForm\bin\Debug\PrintReport.exe";
                //            if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Preview " + strJson);
                //            if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

                //            else View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                //        }
                //        else
                //        {
                //            Logger.Debug("客户端外接配置查询返回为空", "不调打印");
                //        }
                //        View.SendDynamicFormAction(View);
                //    }

                //}
            }

            #endregion
            Logger.Debug("打印---", "---------------END------------------");
        }


        /// <summary>
        /// 弹出动态表单，录入大晶点，返回值
        /// </summary>
        /// <param name="e"></param>
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGelPosition"))//F_SZXY_BigCrystalLoc
            {
                int m = this.Model.GetEntryCurrentRowIndex("F_SZXY_XYCPJYEntity");
                string crystalLoc = Convert.ToString(this.Model.GetValue("F_SZXY_BigCrystalLoc", m));
                if (crystalLoc.IsNullOrEmptyOrWhiteSpace()) crystalLoc = "";
                DynamicFormShowParameter showParam = new DynamicFormShowParameter
                {
                    FormId = "SZXY_GelData",//晶点
                    ShowMaxButton = false,
                    HiddenCloseButton = true
                };
                showParam.CustomParams.Add("value", crystalLoc);
                this.View.ShowForm(showParam,
                new Action<FormResult>((returnData) =>
                {
                    // 获取列表返回的数据
                    if (returnData == null || returnData.ReturnData == null)
                    {
                        return;
                    }
                    // AScanReturnInfo listData =(AScanReturnInfo)returnData.ReturnData;//
                    //if (!listData.Value.IsNullOrEmptyOrWhiteSpace())
                    //{
                    //    this.Model.SetValue("F_SZXY_BigCrystalLoc", listData.Value, m);
                    //    this.View.UpdateView("F_SZXY_XYCPJYEntity");
                    //}
                    if (!returnData.ReturnData.ToString().IsNullOrEmptyOrWhiteSpace())
                    {
                        this.Model.SetValue("F_SZXY_BigCrystalLoc", returnData.ReturnData.ToString(), m);
                        this.View.UpdateView("F_SZXY_XYCPJYEntity");
                    }
                }));
            }
        }

        //public  override void AfterSave(Kingdee.BOS.Core.Bill.PlugIn.Args.AfterSaveEventArgs e)
        //{
        //    base.AfterSave(e);
        //    //保存后生成领料和入库单
        //    #region
        //    // 获取源单与目标单直接的转换规则，如果规则未启用，则返回为空，注意容错
        //    // 假设：上游单据FormId为sourceFormId，下游单据FormId为targetFormId
        //    string sourceFormId = "SZXY_XYLY";
        //    string targetFormId = "PRD_PickMtrl";
        //    var rules = ConvertServiceHelper.GetConvertRules(this.View.Context, sourceFormId, targetFormId);
        //    var rule = rules.FirstOrDefault(t => t.IsDefault);
        //    // 获取在列表上当前选择需下推的行
        //    //  ListSelectedRow[] selectedRows = ((IListView)this.View).SelectedRowsInfo.ToArray();

        //    // 如下代码为单据上获取当前当前选择行
        //    string primaryKeyValue = ((IBillView)this.View).Model.GetPKValue().ToString();
        //    ListSelectedRow row = new ListSelectedRow(primaryKeyValue, string.Empty, 0, this.View.BillBusinessInfo.GetForm().Id);
        //    ListSelectedRow[] selectedRows = new ListSelectedRow[] { row };

        //    // 调用下推服务，生成下游单据数据包
        //    ConvertOperationResult operationResult = null;
        //    Dictionary<string, object> custParams = new Dictionary<string, object>();
        //    try
        //    {
        //        PushArgs pushArgs = new PushArgs(rule, selectedRows)
        //        {
        //            TargetBillTypeId = "",                 // 请设定目标单据单据类型。如无单据类型，可以空字符
        //            TargetOrgId = 0,                        // 请设定目标单据主业务组织。如无主业务组织，可以为0
        //            CustomParams = custParams,  // 可以传递额外附加的参数给单据转换插件，如无此需求，可以忽略
        //        };
        //        //执行下推操作，并获取下推结果
        //        operationResult = ConvertServiceHelper.Push(this.View.Context, pushArgs, OperateOption.Create());
        //    }
        //    catch (KDExceptionValidate ex)
        //    {
        //        this.View.ShowErrMessage(ex.Message, ex.ValidateString);
        //        //return false;
        //    }
        //    catch (KDException ex)
        //    {
        //        this.View.ShowErrMessage(ex.Message);
        //        //return false;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //    // 获取生成的目标单据数据包
        //    DynamicObject[] objs = (from p in operationResult.TargetDataEntities
        //                            select p.DataEntity).ToArray();
        //    // 读取目标单据元数据
        //    var targetBillMeta = MetaDataServiceHelper.Load(this.View.Context, targetFormId) as FormMetadata;
        //    OperateOption saveOption = OperateOption.Create();
        //    // 忽略全部需要交互性质的提示，直接保存；
        //    saveOption.SetIgnoreWarning(true); // 提交数据库保存，并获取保存结果
        //    var saveResult = BusinessDataServiceHelper.Save(this.View.Context, targetBillMeta.BusinessInfo, objs, saveOption, "Save");

        //    #endregion
        //}

    

    }
}
