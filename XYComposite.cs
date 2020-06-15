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
    [Description("复合单据操作。")]
    public class XYComposite : AbstractBillPlugIn
    {
 
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {


            base.BeforeDoOperation(e);
            this.View.UpdateView("F_SZXY_XYFHEntity");

            DynamicObject BillHead = this.Model.DataObject as DynamicObject;


            //复合明细的面积汇总写入单据头投入面积

            if (BillHead != null && (e.Operation.FormOperation.Id.EqualsIgnoreCase("save") || e.Operation.FormOperation.Id.EqualsIgnoreCase("submit")))
            {


                if (this.Model.DataObject["SZXY_XYFHEntry"] is DynamicObjectCollection Entry)
                {
                    List<DynamicObject> ListRow = new List<DynamicObject>() { };

                    for (int i = 0; i < Entry.Count; i++)
                    {

                        if (Convert.ToString(Entry[i]["F_SZXY_PlasticNo"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            ListRow.Add((DynamicObject)Entry[i]);
                        }
                    }

                    ListRow.Reverse();
                    if (ListRow!=null&& ListRow.Count>0)
                    {
                        foreach (var item in ListRow)
                        {
                            //this.Model.DeleteEntryRow("F_SZXY_XYFCEntity", Convert.ToInt32(item));
                            Entry.Remove(item);
                        }
                    }
                  
                    this.View.UpdateView("F_SZXY_XYFHEntity");
                }

                string F_SZXY_MANHOUR = Convert.ToString(this.Model.GetValue("F_SZXY_MANHOUR"));
                if (!F_SZXY_MANHOUR.IsNullOrEmptyOrWhiteSpace())
                {
                    if (Convert.ToDecimal(F_SZXY_MANHOUR) <= 0)
                    {
                        //this.View.ShowWarnningMessage("请检查输入的日期时间");
                        //return;
                        throw new Exception("请检查输入的日期时间");
                    }
                }

                string F_SZXY_StoptimeH = Convert.ToString(this.Model.GetValue("F_SZXY_StoptimeH"));
                bool flag4 = F_SZXY_StoptimeH.IsNullOrEmptyOrWhiteSpace() || Convert.ToDateTime(F_SZXY_StoptimeH) == DateTime.MinValue;
                if (flag4)
                {
                    //this.View.ShowWarnningMessage("请检查输入的日期时间");
                    throw new Exception("请检查输入的日期时间");
                    //return;
                }
                decimal totalArea = 0;

                if (this.Model.DataObject["SZXY_XYFHEntry"] is DynamicObjectCollection Entrys)
                {
                    foreach (var item in Entrys.Where(op => !Convert.ToString(op["F_SZXY_PlasticNo"]).IsNullOrEmptyOrWhiteSpace()))
                    {
                        string LyNo = Convert.ToString(item["F_SZXY_PlasticNo"]);
                        decimal Area = Convert.ToDecimal(item["F_SZXY_Area"]);
                        if (!LyNo.IsNullOrEmptyOrWhiteSpace() && !Area.IsNullOrEmptyOrWhiteSpace())
                        {
                            totalArea += Convert.ToDecimal(Area);
                        }
                        //改写流转状态
                        string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYLYEntry set  F_SZXY_CirculationState={0}   where F_SZXY_PlasticNo='{1}' ", 1, LyNo);
                        int res = DBServiceHelper.Execute(Context, UpdateStateSql);
                    }
                    if (totalArea != 0) this.Model.SetValue("F_SZXY_InArea", totalArea);
                    this.View.UpdateView("F_SZXY_InArea");
                }


            }

        }


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            //在XY复合单输入机台、时间，系统根据机台、时间匹配日计划记录，自动将相同的日计划记录携带到XY复合单上
            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && billobj[GetFieldsKey(formID)["机台H"]] != null && !formID.IsNullOrEmptyOrWhiteSpace())
            {

                if (!Convert.ToString(this.Model.GetValue("F_SZXY_RecNo")).IsNullOrEmptyOrWhiteSpace()) throw new Exception("复合编号不允许重复生成！");
                DynamicObject MacObj = billobj[GetFieldsKey(formID)["机台H"]] as DynamicObject;
                string MacId = MacObj["ID"].ToString();
                DynamicObjectCollection entry = billobj[GetFieldsKey(formID)["Entry"]] as DynamicObjectCollection;//单据体
                DynamicObject orgobj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;
                if (MacObj != null && orgobj != null)
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    DateTime Stime = Convert.ToDateTime(billobj["F_SZXY_DatetimeL"]);
                    if (Stime == DateTime.MinValue)
                    {
                        Stime = DateTime.Now;
                       // this.View.Model.SetValue("F_SZXY_DatetimeL", Stime);
                    }
                    if (Stime != DateTime.MinValue)
                    {
                        string Sql = $"/*dialect*/select T2.F_SZXY_PONO,T2.F_SZXY_MoLineNo, F_SZXY_SMARK,T2.F_SZXY_TeamGroup,T2.F_SZXY_Classes," +
                                       $"  T2.F_SZXY_RECIPE,T2.F_SZXY_PLY,T2.F_SZXY_WIDTH,T2.F_SZXY_LEN,T2.F_SZXY_MATERIAL,T2.F_SZXY_MOID '生产订单内码'," +
                                       $"T11.FMATERIALID '产品型号'," +
                                       $"  T2.F_SZXY_AREA,T2.F_SZXY_OPERATOR ,F_SZXY_WIRED,F_SZXY_MVolume,T1.FBILLNO ,T2.F_SZXY_CJ '车间'," +
                                       $" T2.F_SZXY_SDate 'Stime', T2.F_SZXY_EndDate 'Etime',T2.FEntryID,T2.Fid " +
                                       $" from {GetFieldsKey(formID)["THead"]} T1 " +
                                       $" left join  {GetFieldsKey(formID)["TEntry"]} T2 on T1.Fid=T2.Fid  " +
                                       $" left join T_BD_MATERIAL t10 on t10.FMATERIALID=T2.F_SZXY_MATERIAL " +
                                       $" left join   T_BD_MATERIAL T11 on T10.FMASTERID =T11.FMASTERID  and T11.FUSEORGID='{orgid}'" +
                                       $" where T2.F_SZXY_Machine='{MacId}' " +
                                       $" and T1.F_SZXY_OrgId={orgid} " +
                                       $" and CONVERT(datetime ,'{Stime}', 120) between CONVERT(datetime ,T2.F_SZXY_SDATE, 120)  and  CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                       $" and T1.FDOCUMENTSTATUS in  ('C')";

                        DataSet DpDs = DBServiceHelper.ExecuteDataSet(Context, Sql);

                        if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                        {
                            int n = this.Model.GetEntryRowCount("F_SZXY_XYFHEntity");

                            DynamicObject TeamGroupObj;
                            string Recipe;

                            string RecNo = "";

                            for (int i = 0; i < DpDs.Tables[0].Rows.Count; i++)
                            {

                                string Rollstr = Utils.GenRandrom(3);

                                int index = n + i;

                                //给单据头赋值
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(DpDs.Tables[0].Rows[i]["生产订单内码"]));
                                this.Model.SetValue("F_SZXY_FIDH", Convert.ToString(DpDs.Tables[0].Rows[i]["Fid"]));
                                this.Model.SetValue("F_SZXY_FEntryIDH", Convert.ToString(DpDs.Tables[0].Rows[i]["FEntryID"]));
                                billobj[GetFieldsKey(formID)["生产订单号H"]] = DpDs.Tables[0].Rows[i]["F_SZXY_PONO"].ToString();
                                billobj[GetFieldsKey(formID)["生产订单行号H"]] = DpDs.Tables[0].Rows[i]["F_SZXY_MoLineNo"].ToString();
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_SMARK"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["特殊标志H"], DpDs.Tables[0].Rows[i]["F_SZXY_SMARK"].ToString());
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_TeamGroup"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["班组H"], Convert.ToInt64(DpDs.Tables[0].Rows[i]["F_SZXY_TeamGroup"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_Classes"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["班次H"], Convert.ToInt64(DpDs.Tables[0].Rows[i]["F_SZXY_Classes"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品型号"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["产品型号H"], Convert.ToInt64(DpDs.Tables[0].Rows[i]["产品型号"]));
                                this.Model.SetValue(GetFieldsKey(formID)["配方H"], DpDs.Tables[0].Rows[i]["F_SZXY_RECIPE"].ToString());
                                this.Model.SetValue(GetFieldsKey(formID)["厚度H"], Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_PLY"]));
                                this.Model.SetValue(GetFieldsKey(formID)["宽度H"], Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_WIDTH"]));
                                this.Model.SetValue(GetFieldsKey(formID)["长度H"], Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_LEN"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_OPERATOR"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["操作员H"], Convert.ToInt64(DpDs.Tables[0].Rows[i]["F_SZXY_OPERATOR"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_WIRED"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["连线H"], Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_WIRED"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_MVolume"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["母卷数H"], Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_MVolume"]));

                                this.Model.SetValue("F_SZXY_Stime", DateTime.Now);
                                // this.Model.SetValue("F_SZXY_StoptimeH",DateTime.MinValue);

                                Recipe = Utils.GetFZZL(DpDs.Tables[0].Rows[i]["F_SZXY_RECIPE"].ToString(), Context);
                                TeamGroupObj = billobj[GetFieldsKey(formID)["班组H"]] as DynamicObject;
                                this.Model.SetValue("F_SZXY_RollNoH", Rollstr);


                                DynamicObject formulaObejct = this.Model.GetValue("F_SZXY_FormulaH") as DynamicObject;
                                DynamicObject SpecialMarkObejct = this.Model.GetValue("F_SZXY_SpecialMarkH") as DynamicObject;
                                DynamicObject TeamObejct = this.Model.GetValue("F_SZXY_TeamGroup") as DynamicObject;
                                DynamicObject Material = this.Model.GetValue("F_SZXY_MaterialID") as DynamicObject;

                                if (SpecialMarkObejct != null && orgobj != null && !formID.IsNullOrEmptyOrWhiteSpace()
                                    && formulaObejct != null && !Rollstr.IsNullOrEmptyOrWhiteSpace() && TeamObejct != null)
                                {
                                    DateTime DateNo = Convert.ToDateTime(this.Model.GetValue("FDate"));
                                    string Date = DateNo.ToString("yyyyMMdd");

                                    string lsh = Utils.GetLSH(Context, "FH", orgid, Date, MacObj);
                                    this.Model.SetValue("F_SZXY_SeqH", lsh);
                                    RecNo = Utils.GenNo(SpecialMarkObejct, orgobj, formID, formulaObejct, MacObj, DateNo, TeamObejct, lsh, Rollstr, Material);
                                    if (RecNo != "") this.Model.SetValue("F_SZXY_RecNo", RecNo);
                                    else throw new Exception("生成复合编号失败，请检查录入数据完整性");
                                }

                                #region 
                                //this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity3");
                                //this.Model.SetValue("F_SZXY_Text", DpDs.Tables[0].Rows[i]["F_SZXY_PONO"].ToString(), index);
                                //this.Model.SetValue("F_SZXY_Text1", DpDs.Tables[0].Rows[i]["F_SZXY_MoLineNo"].ToString(), index);
                                //this.Model.SetValue("F_SZXY_Assistant1", DpDs.Tables[0].Rows[i]["F_SZXY_SMARK"].ToString(), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_TeamGroup"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Base", Convert.ToInt32(DpDs.Tables[0].Rows[i]["F_SZXY_TeamGroup"]), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_Classes"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Base1", Convert.ToInt32(DpDs.Tables[0].Rows[i]["F_SZXY_Classes"]), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_MATERIAL"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Base2", Convert.ToInt32(DpDs.Tables[0].Rows[i]["F_SZXY_MATERIAL"]), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_RECIPE"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Assistant2", DpDs.Tables[0].Rows[i]["F_SZXY_RECIPE"].ToString(), index);
                                //this.Model.SetValue("F_SZXY_Decimal1", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_PLY"]), index);
                                //this.Model.SetValue("F_SZXY_Decimal2", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_WIDTH"]), index);
                                //this.Model.SetValue("F_SZXY_Decimal3", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_LEN"]), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_OPERATOR"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Base3", Convert.ToInt32(DpDs.Tables[0].Rows[i]["F_SZXY_OPERATOR"]), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_WIRED"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Assistant3", Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_WIRED"]), index);
                                //if (!Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_MVolume"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Assistant4", Convert.ToString(DpDs.Tables[0].Rows[i]["F_SZXY_MVolume"]), index);

                                //this.Model.SetValue("F_SZXY_Qty", PudArea, index);

                                //if (!Rollstr.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Text3", Rollstr, index);
                                //if (!RecNo.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Text4", RecNo, index);

                                //this.Model.SetValue("F_SZXY_Assistant", MacId, index);
                                //this.Model.SetValue("F_SZXY_Datetime", Convert.ToDateTime(DpDs.Tables[0].Rows[i]["Stime"]), index);
                                //this.Model.SetValue("F_SZXY_Datetime1", Convert.ToDateTime(DpDs.Tables[0].Rows[i]["Etime"]), index); //
                                //this.Model.SetValue("F_SZXY_SourceBillNo1", Convert.ToString(DpDs.Tables[0].Rows[i]["FBILLNO"]), index); //FBILLNO
                                #endregion

                            }

                            //调用保存
                            this.View.UpdateView();
                            billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            IOperationResult a = Utils.Save(this.View, new DynamicObject[] { billobj }, OperateOption.Create(), Context);
                            //  this.View.UpdateView();
                        }
                        else
                        {
                            this.View.ShowMessage("没有匹配到日计划"); return;
                        }
                    }
                }
                else { throw new Exception("机台和业务组织不许为空！"); }

            }


            #region
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNoByList"))
            {
                this.View.UpdateView();
                // int m   =this.Model.GetEntryRowCount("F_SZXY_XYFHEntity");
                int m = this.View.Model.GetEntryRowCount("F_SZXY_XYFHEntity");
                DynamicObject MaterialObejct = this.Model.GetValue("F_SZXY_MaterialId") as DynamicObject;
                DynamicObject machineObejct = this.Model.GetValue("F_SZXY_MachineH") as DynamicObject;
                string formid = this.View.BusinessInfo.GetForm().Id;
                Dictionary<string, DynamicObject> dir = new Dictionary<string, DynamicObject> { };
                dir.Clear();
                if (MaterialObejct != null && machineObejct != null)
                {
                    dir.Add("物料", MaterialObejct);
                    dir.Add("机台", machineObejct);
                    DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                    //showParam.OpenStyle.ShowType = ShowType.MainNewTabPage;
                    showParam.FormId = "SZXY_JTRJHFILTER";//机台日计划过滤 SZXY_SpiltDP SZXY_JTRJHFILTER
                    showParam.ShowMaxButton = false;
                    showParam.HiddenCloseButton = true;
                    showParam.CustomParams.Add("index", m.ToString());
                    showParam.CustomComplexParams.Add("FilterInfo", dir);
                    //this.View.ShowForm(showParam);
                    this.View.ShowForm(showParam, new Action<FormResult>((returnData) =>
                    {
                        // 获取列表返回的数据
                        if (returnData == null || returnData.ReturnData == null)
                        {
                            return;
                        }
                        ScanReturnInfo listDataSet = (ScanReturnInfo)returnData.ReturnData;//
                        int Mateial = Convert.ToInt32(listDataSet.Mateial);
                        string Mac = Convert.ToString(listDataSet.Mac);
                        if (Mateial > 0 && !Mac.IsNullOrEmptyOrWhiteSpace())
                        {
                            ListSelBillShowParameter showParamorder = new ListSelBillShowParameter();
                            showParamorder.FormId = "SZXY_FHJTRSCJH";
                            showParamorder.ParentPageId = this.View.PageId;
                            showParamorder.IsLookUp = true;
                            showParamorder.MultiSelect = false;

                            showParamorder.IsShowApproved = true;

                            string filter = " F_SZXY_MACHINE='" + Mac + "' and F_SZXY_MATERIAL=" + Mateial + "";
                            string filterorder = showParamorder.ListFilterParameter.Filter;
                            if (!filterorder.IsNullOrEmptyOrWhiteSpace()) showParamorder.ListFilterParameter.Filter = " and " + filter;
                            else showParamorder.ListFilterParameter.Filter = filter;
                            this.View.ShowForm(showParamorder,
                              new Action<FormResult>((formResult) =>
                              {

                                  if (formResult != null)
                                  {
                                      ListSelectedRowCollection ReslistData = formResult.ReturnData as ListSelectedRowCollection;
                                      int entrycount = this.View.Model.GetEntryRowCount("F_SZXY_XYFHEntity3");
                                      if (ReslistData != null && ReslistData.Count() > 0)
                                      {
                                          int i = 0;
                                          foreach (ListSelectedRow listData in ReslistData)
                                          {

                                              //this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity");
                                              var dy = listData.DataRow;
                                              DynamicObject F_SZXY_TEAMGROUP = dy["F_SZXY_TEAMGROUP_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_Classes = dy["F_SZXY_Classes_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_OPERATOR = dy["F_SZXY_OPERATOR_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_Material = dy["F_SZXY_Material_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_SMark = dy["F_SZXY_SMark_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_recipe = dy["F_SZXY_recipe_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_WIRED = dy["F_SZXY_WIRED_Ref"] as DynamicObject;
                                              DynamicObject F_SZXY_MVolume = dy["F_SZXY_MVolume_Ref"] as DynamicObject;
                                              if (F_SZXY_TEAMGROUP != null) this.View.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt32(F_SZXY_TEAMGROUP["Id"]));
                                              if (F_SZXY_Classes != null) this.View.Model.SetValue("F_SZXY_Classes", Convert.ToInt32(F_SZXY_Classes["Id"]));
                                              if (F_SZXY_OPERATOR != null) this.View.Model.SetValue("F_SZXY_Recorder", Convert.ToInt32(F_SZXY_OPERATOR["Id"]));
                                              if (F_SZXY_Material != null) this.View.Model.SetValue("F_SZXY_Material", Convert.ToInt32(F_SZXY_Material["Id"]));
                                              if (F_SZXY_WIRED != null) this.View.Model.SetValue("F_SZXY_ligature", F_SZXY_WIRED["Id"].ToString());
                                              if (F_SZXY_MVolume != null) this.View.Model.SetValue("F_SZXY_MotherVolume", F_SZXY_MVolume["Id"].ToString());
                                              this.View.Model.SetValue("F_SZXY_PTNoH", dy["F_SZXY_PONO"]);
                                              this.View.Model.SetValue("F_SZXY_POLineNum", dy["F_SZXY_MoLineNo"]);
                                              if (F_SZXY_SMark != null) this.View.Model.SetValue("F_SZXY_SpecialMarkH", F_SZXY_SMark["Id"].ToString());
                                              if (F_SZXY_recipe != null) this.View.Model.SetValue("F_SZXY_FormulaH", F_SZXY_recipe["Id"].ToString());
                                              this.View.Model.SetValue("F_SZXY_PLyH", dy["F_SZXY_PLY"]);
                                              this.View.Model.SetValue("F_SZXY_WidthH", dy["F_SZXY_WIDTH"]);
                                              this.View.Model.SetValue("F_SZXY_LenH", dy["F_SZXY_Len"]);
                                              this.View.Model.SetValue("F_SZXY_Stime", dy["F_SZXY_SDate"]);
                                              this.View.Model.SetValue("F_SZXY_StoptimeH", dy["F_SZXY_EndDate"]);
                                              string RollNoH = Convert.ToString(this.Model.GetValue("F_SZXY_RollNoH"));
                                              //产出面积 公式：宽度* 长度*层数 / 1000
                                              decimal PudArea = Convert.ToDecimal(dy["F_SZXY_Len"]) * Convert.ToDecimal(dy["F_SZXY_WIDTH"]) / 1000;
                                              this.View.Model.SetValue("F_SZXY_ProdArea", PudArea);
                                              //F_SZXY_RecNo NumberF_SZXY_FormulaH

                                              DynamicObject orgobj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;
                                              DynamicObject formulaObejct = this.Model.GetValue("F_SZXY_FormulaH") as DynamicObject;
                                              DynamicObject SpecialMarkObejct = this.Model.GetValue("F_SZXY_SpecialMarkH") as DynamicObject;
                                              DynamicObject TeamObejct = this.Model.GetValue("F_SZXY_TeamGroup") as DynamicObject;

                                              DateTime DateNo = Convert.ToDateTime(this.Model.GetValue("FDate"));
                                              string Rollstr = Utils.GenRandrom(3);

                                              this.Model.SetValue("F_SZXY_RollNoH", Rollstr);
                                              string lhs = "";
                                              string RecNo = "";
                                              if (!Rollstr.IsNullOrEmptyOrWhiteSpace() && SpecialMarkObejct != null && orgobj != null && formid != ""
                                              && formulaObejct != null && TeamObejct != null && machineObejct != null && DateNo != DateTime.MinValue)
                                              {
                                                  //lhs = Utils.GetLSH(Context, "FH", OrgID, DateNo.ToString("yyyyMMdd"));
                                                  if (!lhs.IsNullOrEmptyOrWhiteSpace())
                                                  {
                                                      this.Model.SetValue("F_SZXY_SeqH", lhs);
                                                  }
                                                  RecNo = Utils.GenNo(SpecialMarkObejct, orgobj, formid, formulaObejct, machineObejct, DateNo, TeamObejct, lhs, Rollstr);
                                                  this.Model.SetValue("F_SZXY_RecNo", RecNo);
                                              }
                                              else throw new Exception("生成编码失败，请检查录入数据的完整性!");


                                              #region
                                              //单据体
                                              this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity3");
                                              int index = entrycount + i;
                                              this.Model.SetValue("F_SZXY_Text", dy["F_SZXY_PONO"], index);
                                              this.Model.SetValue("F_SZXY_Text1", dy["F_SZXY_MoLineNo"], index);
                                              if (F_SZXY_SMark != null) this.Model.SetValue("F_SZXY_Assistant1", F_SZXY_SMark["Id"].ToString(), index);
                                              if (F_SZXY_TEAMGROUP != null) this.Model.SetValue("F_SZXY_Base", Convert.ToInt32(F_SZXY_TEAMGROUP["Id"]), index);
                                              if (F_SZXY_Classes != null) this.Model.SetValue("F_SZXY_Base1", Convert.ToInt32(F_SZXY_Classes["Id"]), index);
                                              if (F_SZXY_Material != null) this.Model.SetValue("F_SZXY_Base2", Convert.ToInt32(F_SZXY_Material["Id"]), index);
                                              if (F_SZXY_recipe != null) this.Model.SetValue("F_SZXY_Assistant2", F_SZXY_recipe["Id"].ToString(), index);
                                              this.Model.SetValue("F_SZXY_Decimal1", Convert.ToDecimal(dy["F_SZXY_PLY"]), index);
                                              this.Model.SetValue("F_SZXY_Decimal2", Convert.ToDecimal(dy["F_SZXY_WIDTH"]), index);
                                              this.Model.SetValue("F_SZXY_Decimal3", Convert.ToDecimal(dy["F_SZXY_Len"]), index);
                                              if (F_SZXY_OPERATOR != null) this.Model.SetValue("F_SZXY_Base3", Convert.ToInt32(F_SZXY_OPERATOR["Id"]), index);
                                              if (F_SZXY_WIRED != null) this.Model.SetValue("F_SZXY_Assistant3", Convert.ToString(F_SZXY_WIRED["Id"]), index);
                                              if (F_SZXY_MVolume != null) this.Model.SetValue("F_SZXY_Assistant4", Convert.ToString(F_SZXY_MVolume["Id"]), index);
                                              //产出面积 公式：宽度* 长度*层数 / 1000
                                              //PudArea = Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_LEN"]) * Convert.ToDecimal(DpDs.Tables[0].Rows[i]["F_SZXY_WIDTH"]) / 1000;
                                              this.Model.SetValue("F_SZXY_Qty", PudArea, index);
                                              //F_SZXY_RecNo Number

                                              // TeamGroupObj = this.Model.GetValue("F_SZXY_Base") as DynamicObject;

                                              this.Model.SetValue("F_SZXY_Text3", Rollstr);
                                              if (!lhs.IsNullOrEmptyOrWhiteSpace())
                                              {
                                                  this.Model.SetValue("F_SZXY_Text2", lhs);
                                              }
                                              if (RecNo != "")
                                              {
                                                  this.Model.SetValue("F_SZXY_RecNo", RecNo);
                                              }
                                              this.Model.SetValue("F_SZXY_Assistant", machineObejct, index);
                                              this.Model.SetValue("F_SZXY_Datetime", dy["F_SZXY_SDate"], index);
                                              this.Model.SetValue("F_SZXY_Datetime1", dy["F_SZXY_EndDate"], index); //

                                              #endregion
                                              //this.Model.SetValue("F_SZXY_SourceBillNo1", Convert.ToString(DpDs.Tables[0].Rows[i]["FBILLNO"]), index); //FBILLNO
                                              i++;
                                          }

                                      }

                                  }

                              }
                              ));
                        }
                        this.View.UpdateView();
                    }));

                }
            }
            #endregion

            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                if (billobj["F_SZXY_OrgId"] is DynamicObject org)
                {
                    this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                    Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                    Utils.TYPrint(this.View, Context, Convert.ToInt64(org["Id"]));
                }


            }
        }
   

        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);

            //获取晶点位置
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGelPosition"))//F_SZXY_BigCrystalLoc
            {
                int m = this.Model.GetEntryCurrentRowIndex("F_SZXY_XYCPJYEntity");
                string crystalLoc = Convert.ToString(this.Model.GetValue("F_SZXY_BigCrystalLoc", m));
                if (crystalLoc.IsNullOrEmptyOrWhiteSpace()) crystalLoc = "";
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.FormId = "SZXY_GelData";//晶点
                showParam.ShowMaxButton = false;
                showParam.HiddenCloseButton = true;
                showParam.CustomParams.Add("value", crystalLoc);
                this.View.ShowForm(showParam,
                new Action<FormResult>((returnData) =>
                {
                    // 获取列表返回的数据
                    if (returnData == null || returnData.ReturnData == null)
                    {
                        return;
                    }
                    AScanReturnInfo listData = (AScanReturnInfo)returnData.ReturnData;//
                    if (!listData.Value.IsNullOrEmptyOrWhiteSpace())
                    {
                        this.Model.SetValue("F_SZXY_BigCrystalLoc", listData.Value, m);
                        this.View.UpdateView("F_SZXY_XYCPJYEntity");
                    }
                }));
            }


        }

 
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.Model.BatchCreateNewEntryRow("F_SZXY_XYFHEntity", 2);

            this.View.GetControl<EntryGrid>("F_SZXY_XYFHEntity").SetEnterMoveNextColumnCell(true);
            this.View.UpdateView("F_SZXY_XYFHEntity");
        }


        /// <summary>
        ///XY复合单新增，选择机台、产品料号，自动按机台、产品料号过滤并弹出对应的日计划记录，选择数据，返回生成
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            #region  计算工时
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_Stime") || e.Field.Key.EqualsIgnoreCase("F_SZXY_StoptimeH"))
            {
                int m = e.Row;
                if (Convert.ToDateTime(this.Model.GetValue("F_SZXY_Stime")) != DateTime.MinValue && Convert.ToDateTime(this.Model.GetValue("F_SZXY_StoptimeH")) != DateTime.MinValue)
                {
                    DateTime Stime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_Stime"));
                    DateTime Etime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StoptimeH"));
                    TimeSpan time = Etime - Stime;
                    this.Model.SetValue("F_SZXY_ManHour", time.TotalMinutes);

                    this.View.UpdateView("F_SZXY_ManHour");
                }
            }
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_StartTime") || e.Field.Key.EqualsIgnoreCase("F_SZXY_StopTime"))
            {
                //单据体工时
                int rowindex = e.Row;
                if (Convert.ToDateTime(this.Model.GetValue("F_SZXY_StartTime", rowindex)) != DateTime.MinValue && Convert.ToDateTime(this.Model.GetValue("F_SZXY_StopTime", rowindex)) != DateTime.MinValue)
                {
                    DateTime Stime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StartTime", rowindex));
                    DateTime Etime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StopTime", rowindex));
                    TimeSpan time = Etime - Stime;
                    this.Model.SetValue("F_SZXY_Decimal4", time.TotalMinutes, rowindex);
                    this.View.UpdateView("F_SZXY_Decimal4", rowindex);
                }
            }
            #endregion

            //XY复合单明细信息扫描流延编号时，能自动将XY流延单上的流延记录携带到XY复合单上

            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_PlasticNo"))
            {
                if (e.OldValue.IsNullOrEmptyOrWhiteSpace() ||
                    (!e.OldValue.IsNullOrEmptyOrWhiteSpace() && !e.NewValue.IsNullOrEmptyOrWhiteSpace() && e.OldValue != e.NewValue))
                {
                    int j = e.Row;

                    DynamicObject orgobj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;
                    string LyNo = Convert.ToString(this.Model.GetValue("F_SZXY_PlasticNo", j));
                    string NoKey = "F_SZXY_PlasticNo";
                    string Entry = "SZXY_XYFHEntry";
                    //  CheckNoIsCur(this.View,this.Model.DataObject,LyNo, Entry, NoKey,e.Row, "F_SZXY_XYFHEntity");

                    bool flag7 = LyNo != "" && orgobj != null;
                    if (flag7)
                    {
                        long orgid = Convert.ToInt64(orgobj["Id"]);
                        //string sql = string.Concat(new string[]
                        //{
                        //    "/*dialect*/select T2.F_SZXY_Machine, T2.F_SZXY_PudAreaUnitID '产品面积单位', T2.F_SZXY_CirculationState '流转状态'," +
                        //    "T2.F_SZXY_RawMaterial,T2.F_SZXY_InPutQty, T2.F_SZXY_PLy,T2.F_SZXY_Width ,T2.F_SZXY_Len ,T2.F_SZXY_Area ," +
                        //    "T2.F_SZXY_Remark1,T1.F_SZXY_DieT '模头温度',  T2.F_SZXY_Formula ,T2.F_SZXY_XNDJ ,T2.F_SZXY_ManHour ,T2.F_SZXY_StopTime ," +
                        //    "T2.F_SZXY_STARTTIME,T2.F_SZXY_Material, T2.FEntryID,T2.Fid  " +
                        //    "from SZXY_t_XYLY T1 " +
                        //    "left join  SZXY_t_XYLYEntry T2 on T1.FID = T2.FID " +
                        //    //" left join T_BD_MATERIAL t10 on t10.FMATERIALID=T2.F_SZXY_Material" +
                        //    //$" left join   T_BD_MATERIAL T11 on T10.FMASTERID =T11.FMASTERID  and T11.FUSEORGID='{orgid}'" +
                        //    "where T2.F_SZXY_PlasticNo='",
                        //    LyNo,
                        //    "'",
                        // string.Format(" and T1.F_SZXY_OrgId={0} ", orgid),   " "   });
                        string sql = "/*dialect*/select T2.F_SZXY_Machine, T2.F_SZXY_PudAreaUnitID '产品面积单位', T2.F_SZXY_CirculationState '流转状态'," +
                                    "T2.F_SZXY_RawMaterial,T2.F_SZXY_InPutQty, T2.F_SZXY_PLy,T2.F_SZXY_Width ,T2.F_SZXY_Len ,T2.F_SZXY_Area ," +
                                    "T2.F_SZXY_Remark1,T1.F_SZXY_DieT '模头温度',  T2.F_SZXY_Formula ,T2.F_SZXY_XNDJ ,T2.F_SZXY_ManHour ,T2.F_SZXY_StopTime ," +
                                    "T2.F_SZXY_STARTTIME,T2.F_SZXY_Material, T2.FEntryID,T2.Fid  " +
                                    "from SZXY_t_XYLY T1 " +
                                   "left join  SZXY_t_XYLYEntry T2 on T1.FID = T2.FID " +
                                    $"where T2.F_SZXY_PlasticNo='{LyNo}' " +
                                    $"  and T1.F_SZXY_OrgId ={orgid} ";
                        Logger.Debug("复合扫码", sql);
                        DataSet ds = Utils.CBData(sql, Context);
                        this.SetBillValue(ds, j, LyNo, orgid.ToString());
                    }
                }
                else if (!e.OldValue.IsNullOrEmptyOrWhiteSpace() && e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {

                    //string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYLYEntry set  F_SZXY_CirculationState={0}   where F_SZXY_PlasticNo='{1}' ", 0, Convert.ToString(e.OldValue));
                    //int res = DBServiceHelper.Execute(Context, UpdateStateSql);
                    View.Model.DeleteEntryRow("F_SZXY_XYFHEntity", e.Row);
                    this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity");
                    View.SetEntityFocusRow("F_SZXY_XYFHEntity", e.Row);

                }

                if (this.Model.DataObject["SZXY_XYFHEntry"] is DynamicObjectCollection entry)
                {
                    int rowcount = 0;
                    foreach (var item in entry.Where(h => !Convert.ToString(h["F_SZXY_PlasticNo"]).IsNullOrEmptyOrWhiteSpace()))
                    {
                        rowcount++;
                    }
                    // 单据体扫描层数反写到单据体
                    //int rowcount = this.View.Model.GetEntryRowCount("F_SZXY_XYFHEntity");
                    this.Model.SetValue("F_SZXY_LAYER", rowcount);
                    this.View.UpdateView("F_SZXY_LAYER");
                }

            }


            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_LenH") || e.Field.Key.EqualsIgnoreCase("F_SZXY_WidthH") || e.Field.Key.EqualsIgnoreCase("F_SZXY_Layer"))
            {
                bool flag11 = !Convert.ToString(this.Model.GetValue("F_SZXY_LenH")).IsNullOrEmptyOrWhiteSpace() && !Convert.ToString(this.Model.GetValue("F_SZXY_Layer")).IsNullOrEmptyOrWhiteSpace() && !Convert.ToString(this.Model.GetValue("F_SZXY_WidthH")).IsNullOrEmptyOrWhiteSpace();
                if (flag11)
                {
                    bool flag12 = Convert.ToDecimal(this.Model.GetValue("F_SZXY_LenH")) > 0m && Convert.ToDecimal(this.Model.GetValue("F_SZXY_Layer")) > 0m && Convert.ToDecimal(this.Model.GetValue("F_SZXY_WidthH")) > 0m;
                    if (flag12)
                    {
                        decimal PudArea = Convert.ToDecimal(Convert.ToDecimal(this.Model.GetValue("F_SZXY_LenH")) * Convert.ToDecimal(this.Model.GetValue("F_SZXY_WidthH")) * Convert.ToDecimal(this.Model.GetValue("F_SZXY_Layer")) / 1000m);
                        this.Model.SetValue("F_SZXY_ProdArea", PudArea);
                    }
                }
            }


        }

        /// <summary>
        /// 检查编码是否在单据
        /// </summary>
        /// <param name="View"></param>
        /// <param name="billobj"></param>
        /// <param name="lyNo">要检验的条码号</param>
        /// <param name="entry">entry实体名</param>
        /// <param name="noKey">要检验的条码号Key</param>
        /// <param name="CurRow">行idenx</param>
        /// <param name="EntryKey">校验的单据体Key</param>
        /// <param name="formId"></param>
        /// <param name="con"></param>
        public static void CheckNoIsCur(IBillView View, DynamicObject billobj, string lyNo, string entry, string noKey, int CurRow, string EntryKey, string formId = "", Context con = null)
        {
            if (billobj[entry] is DynamicObjectCollection entry1)
            {
                foreach (var item in entry1.Where(op => !Convert.ToString(op[noKey]).IsNullOrEmptyOrWhiteSpace()))
                {
                    if (Convert.ToString(item[noKey]).EqualsIgnoreCase(lyNo) && Convert.ToInt32(item["Seq"]) != CurRow + 1)
                    {
                        View.Model.DeleteEntryRow(EntryKey, CurRow);
                        View.SetEntityFocusRow(EntryKey, CurRow);
                        View.GetControl(noKey).SetFocus();
                        throw new Exception("此编码在此单已存在！");
                    }
                }

            }

            if (formId == "BZ" && billobj["F_SZXY_OrgId1"] is DynamicObject org && con != null)
            {
                string orgid = Convert.ToString(org["ID"]);
                if (!orgid.EqualsIgnoreCase("0") && !orgid.IsNullOrEmptyOrWhiteSpace())
                {
                    string IScurrSql = $"select 1 from SZXY_t_BZDHEntry T1 left join SZXY_t_BZD T2 on T1.fid=T2.fid " +
                    $"where  T1.F_SZXY_BARCODE='{lyNo}' " +
                    $"and T2.F_SZXY_ORGID1={orgid} " +
                    $" and T1.F_SZXY_ISUNBOX!=1";
                    DataSet ISCurrDS = Utils.CBData(IScurrSql, con);
                    if (ISCurrDS != null && ISCurrDS.Tables.Count > 0 && ISCurrDS.Tables[0].Rows.Count > 0)
                    {
                        throw new Exception("编码已被扫过！");
                    }
                }

            }
        }


        public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            bool flag = e.EntityKey.EqualsIgnoreCase("F_SZXY_XYFHEntity");
            if (flag)
            {
                string FhNo = Convert.ToString(this.Model.GetValue("F_SZXY_PlasticNo", e.Row));
                bool flag2 = !FhNo.IsNullOrEmptyOrWhiteSpace();
                if (flag2)
                {
                    string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYLYEntry set  F_SZXY_CirculationState={0}   where F_SZXY_PlasticNo='{1}' ", 0, FhNo);
                    int res = DBServiceHelper.Execute(Context, UpdateStateSql);
                    this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity");
                    View.SetEntityFocusRow("F_SZXY_XYFHEntity", e.Row);
                    View.GetControl("F_SZXY_PlasticNo").SetFocus();
                }
            }
        }

        private void SetBillValue(DataSet Ds, int m, string LyNo, string orgid)
        {
            if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
            {

                //DynamicObjectCollection Entrys = this.Model.DataObject["SZXY_Entry"] as DynamicObjectCollection;
                //Entrys.Clear();      
                int index = 0;
                for (int i = 0; i < Ds.Tables[0].Rows.Count; i++)
                {
                    index = m + i;
                    string NoState = Convert.ToString(Ds.Tables[0].Rows[i]["流转状态"]);
                    bool flag2 = NoState.EqualsIgnoreCase("0");
                    if (true)
                    {
                        this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity");

                        this.View.GetControl<EntryGrid>("F_SZXY_XYFHEntity").SetEnterMoveNextColumnCell(true);
                        this.View.SetEntityFocusRow("F_SZXY_XYFHEntity", index + 1);
                        this.Model.SetValue("F_SZXY_FID", Convert.ToString(Ds.Tables[0].Rows[i]["Fid"]), m + i);
                        this.Model.SetValue("F_SZXY_FEntryID", Convert.ToString(Ds.Tables[0].Rows[i]["FEntryID"]), m + i);

                        this.Model.SetValue("F_SZXY_LyWeight", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_InPutQty"]), m + i);
                        this.Model.SetValue("F_SZXY_Remark1", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Remark1"]), m + i);
                        this.Model.SetValue("F_SZXY_LEN", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_LEN"]), m + i);
                        this.Model.SetValue("F_SZXY_WIDTH", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_WIDTH"]), m + i);


                        if (!Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_XNDJ"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            this.Model.SetValue("F_SZXY_Level", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_XNDJ"]), m + i);
                        }

                        this.Model.SetValue("F_SZXY_oC", Convert.ToDecimal(Ds.Tables[0].Rows[i]["模头温度"]), m + i);

                        //this.Model.SetItemValueByNumber("F_SZXY_Level", "SZXY_Firsts", m + i);//默认值

                        this.Model.SetValue("F_SZXY_PudAreaUnitID", Ds.Tables[0].Rows[i]["产品面积单位"], m + i);

                        if (!Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Machine"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Machine", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Machine"]), m + i);
                        this.Model.SetValue("F_SZXY_PLy", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_PLY"]), m + i);
                        this.Model.SetValue("F_SZXY_PickArea", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Area"]), m + i);
                        this.Model.SetValue("F_SZXY_Area", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Area"]), m + i);
                        this.Model.SetValue("F_SZXY_ManHour", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_ManHour"]), m + i);
                        this.Model.SetValue("F_SZXY_StartTime", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_STARTTIME"]), m + i);
                        this.Model.SetValue("F_SZXY_StopTime", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_StopTime"]), m + i);


                        if (!Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_RawMaterial"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            //string RMat = Utils.GetRootMatId(Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_RawMaterial"]), orgid.ToString(), Context);
                            this.Model.SetValue("F_SZXY_RawMaterial", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_RawMaterial"]), m + i);
                        }

                        if (!Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Material"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            string RMat = Utils.GetRootMatId(Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Material"]), orgid.ToString(), Context);
                            this.Model.SetValue("F_SZXY_Material", RMat, m + i);
                        }


                        if (!Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Formula"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Formula", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_Formula"]), m + i);

                        this.Model.SetValue("F_SZXY_RollNo", m + i + 1, m + i);
                        if (m + i + 1 == 0)
                        {
                            this.Model.SetValue("F_SZXY_RollNo", 1, m + i);
                        }
                    }
                    else
                    {
                        View.ShowWarnningMessage("录入的编号已被使用！", "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
                        View.Model.DeleteEntryRow("F_SZXY_XYFHEntity", m);
                        this.Model.CreateNewEntryRow("F_SZXY_XYFHEntity");
                        View.SetEntityFocusRow("F_SZXY_XYFHEntity", m);

                    }
                }
                //this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                //Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                //this.View.UpdateView("F_SZXY_XYFHEntity");


            }
            else
            {
                this.View.ShowWarnningMessage("录入的编号不存在!"); return;
            }

        }


        private Dictionary<string, string> GetFieldsKey(string formID)
        {
            Dictionary<string, string> FieldsKey = new Dictionary<string, string>();
            try
            {
                switch (formID.ToUpper())
                {
                    case "SZXY_XYFH"://复合单Formid
                        FieldsKey.Add("Entity", "F_SZXY_XYFHEntity");//下有单据体key
                        FieldsKey.Add("Entry", "SZXY_XYFHEntry");//下有单据表体实体属性
                        FieldsKey.Add("THead", "SZXY_t_FHJTRSCJH");//上游单据表头表名
                        FieldsKey.Add("TEntry", "SZXY_t_FHJTRSCJHEntry");//上游单据表体表名
                        FieldsKey.Add("机台H", "F_SZXY_MachineH");
                        FieldsKey.Add("生产订单号H", "F_SZXY_PTNoH");
                        FieldsKey.Add("生产订单行号H", "F_SZXY_POLineNum");
                        FieldsKey.Add("特殊标志H", "F_SZXY_SpecialMarkH");
                        FieldsKey.Add("班组H", "F_SZXY_TeamGroup");
                        FieldsKey.Add("班次H", "F_SZXY_Classes");
                        FieldsKey.Add("产品型号H", "F_SZXY_MaterialID");
                        FieldsKey.Add("配方H", "F_SZXY_FormulaH");
                        FieldsKey.Add("厚度H", "F_SZXY_PlyH");
                        FieldsKey.Add("宽度H", "F_SZXY_WidthH");
                        FieldsKey.Add("长度H", "F_SZXY_LenH");
                        // FieldsKey.Add("面积H", "F_SZXY_Area");
                        FieldsKey.Add("操作员H", "F_SZXY_Recorder");
                        FieldsKey.Add("开始时间H", "F_SZXY_Stime");
                        FieldsKey.Add("结束时间H", "F_SZXY_StoptimeH");
                        FieldsKey.Add("连线H", "F_SZXY_ligature");
                        FieldsKey.Add("母卷数H", "F_SZXY_MotherVolume");
                        FieldsKey.Add("上游开始时间", "F_SZXY_SDATE");//上游时间
                        FieldsKey.Add("上游结束时间", "F_SZXY_EndDate");
                        //FieldsKey.Add("产品型号", "F_SZXY_MaterialId");
                        //FieldsKey.Add("原料", "F_SZXY_RawMateriall");
                        break;

                }
            }
            catch (Exception) { }

            return FieldsKey;
        }
    }
}
