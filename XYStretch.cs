using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("拉伸单据操作。")]
    public class XYStretch : AbstractBillPlugIn
    {
        private List<LsCx> LsCxList;
        public string CurrFHNO { get; private set; }
 
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            //在XY输入机台、时间，系统根据机台、时间匹配日计划记录，自动将相同的日计划记录携带到XY复合单上
            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && billobj[GetFieldsKey(formID)["拉伸机台H"]] != null)
            {
                if (!Convert.ToString(this.Model.GetValue("F_SZXY_RecNo")).IsNullOrEmptyOrWhiteSpace()) throw new Exception("拉伸编号不允许重复生成！");
                DynamicObject MacObj = billobj[GetFieldsKey(formID)["拉伸机台H"]] as DynamicObject;
                string MacId = MacObj["ID"].ToString();

                DynamicObjectCollection entry = billobj[GetFieldsKey(formID)["Entry"]] as DynamicObjectCollection;//单据体
                if (MacObj != null && this.Model.GetValue("F_SZXY_OrgId") is DynamicObject orgobj)
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
                        string Sql = $"/*dialect*/select T1.FDOCUMENTSTATUS, T2.F_SZXY_PTNo '生产订单号',T2.F_SZXY_MOID '生产订单内码'," +
                                     $"T2.F_SZXY_POLineNo '生产订单行号', T2.F_SZXY_SMark '特殊标志',T2.F_SZXY_Team '班组'," +
                                     $"T2.F_SZXY_Class '班次', T2.F_SZXY_RECIPE '配方',T2.F_SZXY_PLY '厚度',T2.F_SZXY_CJ '车间'," +
                                       $"  T2.F_SZXY_WIDTH '宽度',T2.F_SZXY_LEN '长度',T2.F_SZXY_MATERIAL '产品料号old', T11.FMATERIALID '产品料号', " +
                                       $"  T2.F_SZXY_AREA '面积',T2.F_SZXY_OPERATOR '操作员',T2.F_SZXY_Implicate '卷序'" +
                                       $",T2.F_SZXY_Layer '层数',T2.F_SZXY_HTSpeed '热处理速比',T2.F_SZXY_StretchSpeed '拉伸速比', " +
                                       $"   T2.F_SZXY_FilmType '大膜类型',T2.F_SZXY_M6 'M6', T2.F_SZXY_M7 'M7',T2.F_SZXY_M8 'M8'," +
                                       $"T2.F_SZXY_M9 'M9',T2.F_SZXY_OGR  '总速比', " +
                                       $"   T2.F_SZXY_OT '烘箱温度',T2.F_SZXY_FDT '定型温度', T2.F_SZXY_SDATE '开始时间',T2.F_SZXY_ENDDATE '结束时间'," +
                                       $"T2.F_SZXY_ProductionSpeed '生产速度',T2.F_SZXY_T4 '热处理T4', T2.F_SZXY_EndDate 'Etime',T2.F_SZXY_SDate 'Stime'," +
                                       $"T2.F_SZXY_T5 '热处理T5', T2.FEntryID,T2.Fid " +
                                       $" from {GetFieldsKey(formID)["THead"]} T1 " +
                                       $"Left join {GetFieldsKey(formID)["TEntry"]} T2 on T1.Fid=T2.Fid  " +
                                       $" left join T_BD_MATERIAL t10 on t10.FMATERIALID=T2.F_SZXY_MATERIAL " +
                                       $" left join   T_BD_MATERIAL T11 on T10.FMASTERID =T11.FMASTERID  and T11.FUSEORGID='{orgid}'" +
                                       $" where T2.F_SZXY_Machine='{MacId}' " +
                                       $" and T1.F_SZXY_OrgId={orgid} " +
                                       $"and CONVERT(datetime ,'{Stime}', 120) between CONVERT(datetime ,T2.F_SZXY_SDATE, 120)  and  CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                       $" and T1.FDOCUMENTSTATUS in  ('C')";

                        DataSet DpDs = DBServiceHelper.ExecuteDataSet(Context, Sql);
                        if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                        {
                            string BMType = Convert.ToString(DpDs.Tables[0].Rows[0]["大膜类型"]);
                            string Layer = Convert.ToString(DpDs.Tables[0].Rows[0]["层数"]);

                            int m = this.Model.GetEntryRowCount("F_SZXY_XYLSEntity");

                            for (int i = 0; i < DpDs.Tables[0].Rows.Count; i++)
                            {
                                string lsh = "";
                                string Rollstr = "";
                                int index = m + i;
                                string F_SZXY_StretchNo = "";

                                #region 单据头赋值
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(DpDs.Tables[0].Rows[i]["生产订单内码"]));
                                this.Model.SetValue("F_SZXY_FIDH", Convert.ToString(DpDs.Tables[0].Rows[i]["Fid"]));
                                this.Model.SetValue("F_SZXY_FEntryIDH", Convert.ToString(DpDs.Tables[0].Rows[i]["FEntryID"]));
                                this.Model.SetValue("F_SZXY_PTNoH", Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单号"]));
                                this.Model.SetValue("F_SZXY_POLineNum", Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单行号"]));

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMarkH", Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(DpDs.Tables[0].Rows[i]["班组"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(DpDs.Tables[0].Rows[i]["班次"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["配方"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Formula", Convert.ToString(DpDs.Tables[0].Rows[i]["配方"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品料号"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MaterialID", Convert.ToInt64(DpDs.Tables[0].Rows[i]["产品料号"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));
                               
                                this.Model.SetValue(GetFieldsKey(formID)["厚度H"], Convert.ToDecimal(DpDs.Tables[0].Rows[i]["厚度"]));
                                this.Model.SetValue(GetFieldsKey(formID)["宽度H"], Convert.ToDecimal(DpDs.Tables[0].Rows[i]["宽度"]));
                                this.Model.SetValue(GetFieldsKey(formID)["长度H"], Convert.ToDecimal(DpDs.Tables[0].Rows[i]["长度"]));
       
                                this.Model.SetValue("F_SZXY_StoptimeH", DateTime.Now.AddHours(1));
                                this.Model.SetValue("F_SZXY_Stime", DateTime.Now);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue(GetFieldsKey(formID)["操作员H"], Convert.ToInt64(DpDs.Tables[0].Rows[i]["操作员"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["卷序"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_VSNO", Convert.ToString(DpDs.Tables[0].Rows[i]["卷序"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["层数"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Layer", Convert.ToString(DpDs.Tables[0].Rows[i]["层数"]));
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["大膜类型"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_BFType", Convert.ToString(DpDs.Tables[0].Rows[i]["大膜类型"]));
                            
                                this.Model.SetValue("F_SZXY_HTR", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["热处理速比"]));
                                this.Model.SetValue("F_SZXY_SR", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["拉伸速比"]));
                                this.Model.SetValue("F_SZXY_M6", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["M6"]));
                                this.Model.SetValue("F_SZXY_M7", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["M7"]));
                                this.Model.SetValue("F_SZXY_M8", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["M8"]));
                                this.Model.SetValue("F_SZXY_M9", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["M9"]));
                                this.Model.SetValue("F_SZXY_OGR", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["总速比"]));
                                this.Model.SetValue("F_SZXY_OvenOC1", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["烘箱温度"]));
                                this.Model.SetValue("F_SZXY_FDT", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["定型温度"]));
                                this.Model.SetValue("F_SZXY_Decimal", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["生产速度"]));
                                this.Model.SetValue("F_SZXY_T4HT", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["热处理T4"]));
                                this.Model.SetValue("F_SZXY_T5HT", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["热处理T5"]));

                                //生成拉伸编号
                                DynamicObject FormulaObj = billobj["F_SZXY_Formula"] as DynamicObject;
                                DynamicObject TeamGroupObj = billobj["F_SZXY_TeamGroup"] as DynamicObject;

                                DynamicObject SpecialMarkObejct = this.Model.GetValue("F_SZXY_SpecialMarkH") as DynamicObject;
                                DateTime DateNo = Convert.ToDateTime(this.Model.GetValue("FDate"));
                                string date = DateNo.ToString("yyyyMMdd");
                                if (DateNo != DateTime.MinValue && FormulaObj != null && SpecialMarkObejct != null && TeamGroupObj != null && MacObj != null && orgobj != null && formID != "")
                                {

                                    Rollstr = Utils.GenRandrom(3);

                                    lsh = Utils.GetLSH(Context,"LS", orgid, date,MacObj);

                                    this.Model.SetValue("F_SZXY_Seq", lsh);
                                    this.Model.SetValue("F_SZXY_RollNoH", Rollstr);

                                    F_SZXY_StretchNo = Utils.GenNo(SpecialMarkObejct, orgobj, formID, FormulaObj, MacObj, DateNo, TeamGroupObj, lsh, Rollstr);

                                    if (F_SZXY_StretchNo!="")
                                    {
                                        this.Model.SetValue("F_SZXY_RecNo", F_SZXY_StretchNo);
                                    } 


                                    if (this.Model.DataObject["SZXY_XYLSEntry"] is DynamicObjectCollection LSentry)
                                    {
                                        foreach (var item in LSentry.Where(op=>!Convert.ToString( op["F_SZXY_StretchNo"]).IsNullOrEmptyOrWhiteSpace()))
                                        {
                                            item["F_SZXY_StretchNo"] = F_SZXY_StretchNo;
                                        }
                                    }

                                    if (this.Model.DataObject["SZXY_XYLSEntry2"] is DynamicObjectCollection LYentry)
                                    {
                                        foreach (var item in LYentry.Where(op => !Convert.ToString(op["F_SZXY_StretchNo12"]).IsNullOrEmptyOrWhiteSpace()))
                                        {
                                            item["F_SZXY_StretchNo12"] = F_SZXY_StretchNo;
                                        }
                                    }


                                }
                                else throw new Exception("生成编码失败，请检查录入数据的完整性!");

                                #endregion

                            }
                            //绑定单据头数据后给复合明细里增加一行

                            //调用保存
                            this.Model.ClearNoDataRow();
                            billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            Utils.Save(View, new DynamicObject[] { billobj }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                  
                            this.View.UpdateView();
                            this.View.GetControl("FDate").Enabled = false;
                        }
                        else
                        {
                            this.View.ShowMessage("没有匹配到日计划");return;
                        }
                    }
                }


            }

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

            
 
                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    #region
                    Utils.TYPrint(this.View, Context, orgid);
           
                    #endregion
                }
            }
        }

        //public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        //{
        //    base.AfterEntryBarItemClick(e);

        //    //【计算复合用量】——复合用量为：[（复合长度-固定损耗）*总速比/拉伸长度] 取整
        //    if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGetComQty"))
        //    {
        //        DynamicObject Billobj = this.Model.DataObject;
        //        DynamicObjectCollection Entry = Billobj["SZXY_XYLSEntry"] as DynamicObjectCollection;
        //        DynamicObjectCollection Entry1 = Billobj["SZXY_XYLSEntry1"] as DynamicObjectCollection;
        //        Decimal OGR = Convert.ToDecimal(this.Model.GetValue("F_SZXY_OGR"));//总速比
        //        Decimal ComQty;
        //        foreach (var row in Entry)
        //        {

        //            foreach (var row1 in Entry1)
        //            {
        //                if (row["F_SZXY_FHNo"].ToString() == row1["F_SZXY_PlasticNo1"].ToString())
        //                {
        //                    Decimal FHLen = Convert.ToDecimal(row["F_SZXY_FHLen"]);//复合长度
        //                    Decimal LSLen = Convert.ToDecimal(row["F_SZXY_LSLen"]);//拉伸长度
        //                    Decimal FixedLoss = Convert.ToDecimal(row1["F_SZXY_FixedLoss"]);//固定损耗 
        //                    ComQty = (FHLen - FixedLoss) * OGR / LSLen;

        //                }
        //            }
        //        }
        //    }
        //}


        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="FhDs"></param>
        /// <param name="Lyds"></param>
        /// <param name="m"></param>
        /// <param name="LyNo"></param>


        private void SetValue(DataSet Lyds, int m, string LyNo, DataChangedEventArgs e,long orgid)
        {
            DynamicObject Bwtypobj = this.Model.GetValue("F_SZXY_BFType") as DynamicObject;//单据头层序
            int Censhu = Convert.ToInt32(this.Model.GetValue("F_SZXY_Layer"));//单据头层数

            if (Lyds != null && Lyds.Tables.Count > 0 && Lyds.Tables[0].Rows.Count > 0)
            {
                int count = Lyds.Tables[0].Rows.Count;
                int layerH = Convert.ToInt32(this.Model.GetValue("F_SZXY_LAYER"));//获取单据头层数

                int n = this.Model.GetEntryRowCount("F_SZXY_XYLSEntity2");
       
                for (int i = 0; i < Lyds.Tables[0].Rows.Count; i++)
                {
                    //string NoState = Convert.ToString(Lyds.Tables[0].Rows[0]["流转状态"]);
                    //bool flag2 = NoState.EqualsIgnoreCase("0");

                    string AreaCount = "0";
                    //扫描复合编号时检查所有单据复合编号耗用面积大于复合面积时警告提示；
                    string SelAreaCount = "select Sum(T1.F_SZXY_FHAREA) from SZXY_t_XYLSEntry1 T1 left join  SZXY_t_XYLS T2 on T1.Fid=T2.Fid" +
                                           $" where T1.F_SZXY_PLASTICNO1='{LyNo}' ";
                    DataSet SelAreaDs = DBServiceHelper.ExecuteDataSet(Context, SelAreaCount);

                    if (SelAreaDs != null && SelAreaDs.Tables.Count > 0 && SelAreaDs.Tables[0].Rows.Count > 0)
                    {
                        if (!Convert.ToString(SelAreaDs.Tables[0].Rows[0][0]).IsNullOrEmptyOrWhiteSpace()&& Convert.ToString(SelAreaDs.Tables[0].Rows[0][0])!="")
                        {
                            AreaCount = Convert.ToString(SelAreaDs.Tables[0].Rows[0][0]);
                        }
                    
                    }
                    decimal AreaCount1 = Convert.ToDecimal(AreaCount);
                    if (!(AreaCount1 > Convert.ToDecimal(Lyds.Tables[0].Rows[i]["产出面积"])))
                    {
                        if (i == 0)
                        {
                            //int F_SZXY_AddQty= Convert.ToInt32( this.Model.GetValue("F_SZXY_AddQty"));
                            //this.Model.SetValue("F_SZXY_AddQty", Lyds.Tables[0].Rows.Count + F_SZXY_AddQty);
                            //if (Lyds.Tables[0].Rows.Count + F_SZXY_AddQty > layerH)
                            //{
                            //    throw new Exception("总层数已超过计划层数！");      
                            //}
                            this.Model.CreateNewEntryRow("F_SZXY_XYLSEntity1");
                       
                            int FHIndex = m + i;
                            this.Model.SetValue("F_SZXY_PlasticNo1", LyNo, FHIndex);
                            //var PlasticNo1 = this.View.BusinessInfo.GetField("F_SZXY_PlasticNo1");
                            this.Model.SetValue("F_SZXY_Length1", Convert.ToString(Lyds.Tables[0].Rows[i]["复合长度"]), FHIndex);
                            this.Model.SetValue("F_SZXY_Width1", Convert.ToString(Lyds.Tables[0].Rows[i]["复合宽度"]), FHIndex);
                            this.Model.SetValue("F_SZXY_Area2", Convert.ToString(Lyds.Tables[0].Rows[i]["产出面积"]), FHIndex);

                            if (!Convert.ToString(Lyds.Tables[0].Rows[i]["复合机台"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_ComplexMac", Convert.ToString(Lyds.Tables[0].Rows[i]["复合机台"]));//单据头
                            if (!Convert.ToString(Lyds.Tables[0].Rows[i]["物料"]).IsNullOrEmptyOrWhiteSpace())
                            {
                               string RMat= Utils.GetRootMatId(Convert.ToString(Lyds.Tables[0].Rows[i]["物料"]), orgid.ToString(), Context);
                                this.Model.SetValue("F_SZXY_FHMATERIALID", RMat, FHIndex);
                            }
                            this.Model.SetValue("F_SZXY_FHCOUNTS", Convert.ToInt32(Lyds.Tables[0].Rows.Count), FHIndex);
                            //耗损长度= 单据头长度/总数比  F_SZXY_costLen= F_SZXY_LenH/F_SZXY_OGR
                            //耗损面积=好用长度*但单据体宽度  =F_SZXY_costLen*F_SZXY_WidthH/1000
                            string F_SZXY_LenH = Convert.ToString(this.Model.GetValue("F_SZXY_LenH"));
                            string F_SZXY_OGR = Convert.ToString(this.Model.GetValue("F_SZXY_OGR"));

                            decimal F_SZXY_costLen = 0;
                            if (Convert.ToDecimal(F_SZXY_OGR) != Convert.ToDecimal(0))
                            {
                                 F_SZXY_costLen = Convert.ToDecimal(F_SZXY_LenH) / Convert.ToDecimal(F_SZXY_OGR);
                                this.Model.SetValue("F_SZXY_costLen", F_SZXY_costLen, FHIndex);
                            }
                            string F_SZXY_WidthH = Convert.ToString(Lyds.Tables[0].Rows[i]["复合宽度"]);
                            decimal WidthH = 0;
                            if (!F_SZXY_WidthH.IsNullOrEmptyOrWhiteSpace())
                            {
                                WidthH = Convert.ToDecimal(F_SZXY_WidthH);
                            }

                            //耗用面积
                            decimal F_SZXY_FHAREA = (F_SZXY_costLen * WidthH) / 1000;
                            this.Model.SetValue("F_SZXY_FHAREA", F_SZXY_FHAREA, FHIndex);
                            
                            this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                            this.View.UpdateView("F_SZXY_XYLSEntity1");
                            this.View.SetEntityFocusRow("F_SZXY_XYLSEntity1", FHIndex+ 1);
                            //this.View.InvokeFormOperation("SAVE");
                        }



                        if (!Convert.ToString(Lyds.Tables[0].Rows[i]["流延机台"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_plasticMac", Convert.ToString(Lyds.Tables[0].Rows[i]["流延机台"]));
                        this.Model.CreateNewEntryRow("F_SZXY_XYLSEntity2");//创建流延行获取流延行数  流延单工时
                        int LYIndex = n + i;
                        this.Model.SetValue("F_SZXY_StretchNo2", Convert.ToString(Lyds.Tables[0].Rows[i]["流延编号"]), LYIndex);
                        if (!Convert.ToString(Lyds.Tables[0].Rows[i]["流延单工时"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            this.Model.SetValue("F_SZXY_ManHour1", Convert.ToDecimal(Lyds.Tables[0].Rows[i]["流延单工时"]), LYIndex);
                        }
                     
                        this.Model.SetValue("F_SZXY_Ply2", Convert.ToString(Lyds.Tables[0].Rows[i]["流延厚度"]), LYIndex);
                        this.Model.SetValue("F_SZXY_Width2", Convert.ToString(Lyds.Tables[0].Rows[i]["流延宽度"]), LYIndex);
                        this.Model.SetValue("F_SZXY_Length2", Convert.ToString(Lyds.Tables[0].Rows[i]["流延长度"]), LYIndex);
                        this.Model.SetValue("F_SZXY_LyArea", Convert.ToString(Lyds.Tables[0].Rows[i]["复合面积"]), LYIndex);
                        this.Model.SetValue("F_SZXY_CompositeSeq", Convert.ToString(Lyds.Tables[0].Rows[i]["层序"]), LYIndex);
                        this.Model.SetValue("F_SZXY_OC", Convert.ToString(Lyds.Tables[0].Rows[i]["温度"]), LYIndex);
                        this.Model.SetValue("F_SZXY_HTTime", Convert.ToString(Lyds.Tables[0].Rows[i]["热处理时长"]), LYIndex);
                        if (!Convert.ToString(Lyds.Tables[0].Rows[i]["流延机台"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_StretchMachine2", Convert.ToString(Lyds.Tables[0].Rows[i]["流延机台"]), LYIndex);
                        this.Model.SetValue("F_SZXY_CompositeLen", Convert.ToString(Lyds.Tables[0].Rows[i]["复合长度"]), LYIndex);
                        if (!Convert.ToString(Lyds.Tables[0].Rows[i]["复合机台"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MFP2", Convert.ToString(Lyds.Tables[0].Rows[i]["复合机台"]), LYIndex);
            
                        if (!Convert.ToString(Lyds.Tables[0].Rows[i]["复合等级"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_XNDJ1", Convert.ToString(Lyds.Tables[0].Rows[i]["复合等级"]), LYIndex);
                        //
                        if (!Convert.ToString(Lyds.Tables[0].Rows[i]["原料批号"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_RawMaterial2", Convert.ToString(Lyds.Tables[0].Rows[i]["原料批号"]), LYIndex);
                        this.Model.SetValue("F_SZXY_StretchNo12", Convert.ToString(this.Model.GetValue("F_SZXY_RecNo")), LYIndex);
                        this.Model.SetValue("F_SZXY_CompositeNo2", LyNo, LYIndex);//

                    }
                    else
                    {
                        this.View.ShowWarnningMessage("复合编号耗用面积大于复合面积！");
                
                        return;
                        //this.View.Model.DeleteEntryRow("F_SZXY_XYLSEntity1", e.Row);
                        //this.Model.CreateNewEntryRow("F_SZXY_XYLSEntity1");
                        //this.View.SetEntityFocusRow("F_SZXY_XYLSEntity1", e.Row);
                    }

                   
                }
             } 
            else
            {
                int FHIndex = e.Row;
                this.Model.SetValue("F_SZXY_PlasticNo1", "", FHIndex);
                //var PlasticNo1 = this.View.BusinessInfo.GetField("F_SZXY_PlasticNo1");
                this.Model.SetValue("F_SZXY_Length1", 0, FHIndex);
                this.Model.SetValue("F_SZXY_Width1", 0, FHIndex);
                this.Model.SetValue("F_SZXY_Area2", 0, FHIndex);

                this.Model.SetValue("F_SZXY_ComplexMac", 0);//单据头

                this.Model.SetValue("F_SZXY_FHMATERIALID", 0, FHIndex);

                this.Model.SetValue("F_SZXY_FHCOUNTS", 0, FHIndex);

                this.Model.SetValue("F_SZXY_costLen", 0, FHIndex);

                this.Model.SetValue("F_SZXY_FHAREA", 0, FHIndex);
                this.View.UpdateView("F_SZXY_XYLSEntity1", e.Row);

                this.View.ShowWarnningMessage("录入的编号不存在！");
                this.View.SetEntityFocusRow("F_SZXY_XYLSEntity1", e.Row);
                return;
            }
        }
        int renzenQty = 0; //物料认证层数设置 
        bool flag = false;



        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string formID = this.View.BillBusinessInfo.GetForm().Id;
                    //  面积 = 宽度 * 长度 * 记录条数（记录条数 = 层数 - 大膜类型）
                    //例如：大膜类型：单层、双层、三层（减2层）
                    //大膜类型：上单下双、上双下单（减3层）
                    //大膜类型：上双下双（减4层）
            //if (e.Field.Key.EqualsIgnoreCase("F_SZXY_BFType") || e.Field.Key.EqualsIgnoreCase("F_SZXY_Layer"))
            //{
            //    int a = e.Row;

            //    if (this.Model.GetValue("F_SZXY_BFType") is DynamicObject BFTypeobj)
            //    {
            //        string LayerStr = Convert.ToString(this.Model.GetValue("F_SZXY_Layer"));
            //        string BMTypeStr = BFTypeobj["FNumber"].ToString();
            //        if (!LayerStr.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(LayerStr) > 0 && !BMTypeStr.IsNullOrEmptyOrWhiteSpace())
            //        {
            //            int qty = 0;
            //            switch (BMTypeStr)
            //            {
            //                case "01":  //单层
            //                    qty = 2;
            //                    break;
            //                case "02":  //单层认证层数
            //                    qty = 2;
            //                    break;
            //                case "03":  //双层
            //                    qty = 2;
            //                    break;
            //                case "04":   //双层上双下单
            //                    qty = 3;
            //                    break;
            //                case "05":  //双层上单下双
            //                    qty = 3;
            //                    break;
            //                case "06":   //双层上双下双
            //                    qty = 4;
            //                    break;
            //                case "07":  //多层
            //                    qty = 2;
            //                    break;
            //                case "08":   //多层上双下单
            //                    qty = 3;
            //                    break;
            //                case "09":  //多层上单下双
            //                    qty = 3;
            //                    break;
            //                case "10":  //多层上双下双
            //                    qty = 4;
            //                    break;
            //            }
            //            int RecordQty = Convert.ToInt32(LayerStr) - qty;
            //            this.Model.SetValue("F_SZXY_Record", RecordQty);

            //            // 宽度* 长度*记录条数
            //            string F_SZXY_LenH = Convert.ToString(this.Model.GetValue("F_SZXY_LenH"));
            //            string F_SZXY_WidthH = Convert.ToString(this.Model.GetValue("F_SZXY_WidthH"));
            //            decimal Area = Convert.ToDecimal(F_SZXY_LenH) * Convert.ToDecimal(F_SZXY_WidthH)/1000 * RecordQty;
            //            this.Model.SetValue("F_SZXY_AreaH", Area);
            //            this.View.UpdateView("F_SZXY_AreaH");
            //        }
            //    }

            //}


            #region  计算工时
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_Stime") || e.Field.Key.EqualsIgnoreCase("F_SZXY_STOPTIMEH"))
            {

                if (Convert.ToDateTime(this.Model.GetValue("F_SZXY_Stime")) != DateTime.MinValue && Convert.ToDateTime(this.Model.GetValue("F_SZXY_StoptimeH")) != DateTime.MinValue)
                {
                    DateTime Stime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_Stime"));
                    DateTime Etime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StoptimeH"));
                    TimeSpan time = Etime - Stime;
                    this.Model.SetValue("F_SZXY_ManHour", time.TotalMinutes);
                    this.View.UpdateView("F_SZXY_ManHour");
                }
            }

            #endregion

            //int m = this.Model.GetEntryCurrentRowIndex("F_SZXY_XYLSEntity2");

            //F_SZXY_RecNo
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_RecNo"))
            {
                string LSBH = Convert.ToString(this.Model.DataObject["F_SZXY_RecNo"]);
                if (!LSBH.IsNullOrEmptyOrWhiteSpace())
                {
                    this.View.GetControl("FDate").Enabled = false;
                }
                else
                {
                    this.View.GetControl("FDate").Enabled = true;
                }
            }


            string BMType = "";
           
            #region 在XY拉伸单明细信息扫描复合编号时，能自动将XY复合单上的复合记录和流延记录携带到XY拉伸单上 


            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_PlasticNo1"))
            {
                //if (e.OldValue.IsNullOrEmptyOrWhiteSpace())
                if (true)
 
                {
                    if (this.Model.GetValue("F_SZXY_MaterialID") is DynamicObject Material)
                    {
                        if (!Convert.ToString(Material["F_SZXY_renzenQTY"]).IsNullOrEmptyOrWhiteSpace()&& Convert.ToInt32(Material["F_SZXY_renzenQTY"])>0)
                        {
                            renzenQty = Convert.ToInt32(Material["F_SZXY_renzenQTY"]);
                            flag = true;
                        }
                    }
                    int m = e.Row;
                    string FHNo = this.Model.GetValue("F_SZXY_PlasticNo1", m).ToString();
                    CurrFHNO =Convert.ToString( FHNo);
                    DynamicObject Bwtypobj12 = this.Model.GetValue("F_SZXY_BFType") as DynamicObject;//单据头层序
                    int Censhu = Convert.ToInt32(this.Model.GetValue("F_SZXY_Layer"));//单据头层数
                    DynamicObject orgobj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;

                    if (FHNo != "" && Bwtypobj12 != null && orgobj != null)
                    {

                        //string NoKey = "F_SZXY_PlasticNo1";
                        //string Entry = "SZXY_XYLSEntry1";
                       // XYComposite.CheckNoIsCur(this.View,this.Model.DataObject, FHNo, Entry, NoKey,e.Row, "F_SZXY_XYLSEntity1");

                        BMType = Bwtypobj12["FNumber"].ToString();

                        int rowcount = this.Model.GetEntryRowCount("F_SZXY_XYLSEntity2");
                        int Layerqty = Convert.ToInt32(this.Model.GetValue("F_SZXY_LAYER"));//层数H
                        if (Layerqty > 2)
                        {
                            if (BMType == "01" || flag)

                            {
                                this.Model.SetValue("F_SZXY_sequence", "2");//层序H

                            }
                            else if (BMType == "03" || BMType == "05")
                            {
                                this.Model.SetValue("F_SZXY_sequence", "2-3");//层序H

                            }
                            else if (BMType == "07" || BMType == "09")
                            {
                                this.Model.SetValue("F_SZXY_sequence", "2-4");//层序H

                            }
                            else if (BMType == "04" || BMType == "06")
                            {
                                this.Model.SetValue("F_SZXY_sequence", "3-4");//层序H
                            }
                            else if (BMType == "08" || BMType == "10")
                            {
                                this.Model.SetValue("F_SZXY_sequence", "3-5");//层序H
                            }
                            this.View.UpdateView("F_SZXY_sequence");
                        }
                        else
                        {
                            throw new Exception("层数不能小于3");
                        }


                        long orgid = Convert.ToInt64(orgobj["Id"]);


                        //正序倒序
                        int F_SZXY_CKB = Convert.ToInt32(this.Model.GetValue("F_SZXY_CKB"));
                        string strOrderby = string.Empty;
                        if (F_SZXY_CKB == 1)
                        {
                            strOrderby = " order by T2.F_SZXY_RollNo desc ";
                        }
                        else
                        {
                            strOrderby = " order by T2.F_SZXY_RollNo asc ";
                        }
 


                        string sql = $"/*dialect*/select T3.F_SZXY_ManHour '流延单工时', T1.FBILLNO '单据编号', T1.F_SZXY_MACHINEH '复合机台'," +
                                     $"T2.F_SZXY_RawMaterial '原料批号',T2.F_SZXY_SpecialMark '特殊标志',T1.F_SZXY_Integer '流转状态'," +
                                     " T1.F_SZXY_PlyH '复合厚度',T1.F_SZXY_WidthH '复合宽度',T1.F_SZXY_LENH '复合长度'," +
                                     "T1.F_SZXY_PRODAREA '复合面积',T1.F_SZXY_DiscardReason '报废原因',T1.F_SZXY_MaterialID '物料', " +
                                     " T1.F_SZXY_FORMULAH '复合配方',T2.F_SZXY_RollNo '层序',T2.F_SZXY_Level '复合等级'," +
                                     " T2.F_SZXY_PLY '流延厚度',T2.F_SZXY_PlasticNo '流延编号'," +
                                     "T2.F_SZXY_FEntryID ,T2.F_SZXY_FID ,T1.F_SZXY_LAYER '层数', " +
                                     " T2.F_SZXY_oC '温度',T2.F_SZXY_Decimal4 '热处理时长',T2.F_SZXY_MACHINE '流延机台'," +
                                     "T2.F_SZXY_LEN '流延长度',T2.F_SZXY_WIDTH '流延宽度',T1.F_SZXY_PRODAREA '产出面积' " +
                                     $" from SZXY_t_XYFH T1 " +
                                     $" left join  SZXY_t_XYFHEntry T2 on T1.FID = T2.FID " +
                                     $" left join SZXY_t_XYLYEntry T3 on T2.F_SZXY_PlasticNo =T3.F_SZXY_PlasticNo " +
                                     $" where T1.F_SZXY_RecNo='{FHNo}'" +
                                     $" and T1.F_SZXY_OrgId={orgid} " +
                                     $" and T2.F_SZXY_PlasticNo!='' " +
                                     $" {strOrderby}";

                        DataSet Lyds = Utils.CBData(sql, Context);

                        //赋值
                        this.SetValue(Lyds, m, FHNo,e, orgid);



                        //循环单据体修改拉伸层序
                        int q1 = 2;//大膜类型03  正常上单下双 07 09
                                   //int q2 = 2;//多层
                        for (int i = 1; i <= this.View.Model.GetEntryRowCount("F_SZXY_XYLSEntity2"); i++)
                        {
                            #region
                            if (BMType == "01" && flag==false)
                            {
                                this.Model.SetValue("F_SZXY_StretchSeq2", i, i - 1);
                            }
                            string FirstRow = "0";
                            string SecRow = "0";
                            string LastRow = "0";
                            if (i == 1)//第一行
                            {
                                FirstRow = "1";
                            }
                            else //第二行
                            if (i == 2 && (BMType == "03" || BMType == "05" || BMType == "07" || BMType == "09"))//第2行
                            {
                                SecRow = this.Model.GetValue("F_SZXY_sequence").ToString();
                            }
                            else
                            if (i == 2 && (BMType == "04" || BMType == "06" || BMType == "08" || BMType == "10"))
                            {
                                this.Model.SetValue("F_SZXY_StretchSeq2", 2, 1);
                            }

                            else
                            if (i == rowcount && Layerqty == rowcount)//最后一行
                            {
                                if (BMType == "01" || BMType == "03" || BMType == "04" ||
                                    BMType == "07" || BMType == "08" || BMType == "09"
                                    || BMType == "10" || BMType == "06") LastRow = rowcount.ToString();

                                if (BMType == "05")
                                {
                                    int a1 = Layerqty;
                                    int a2 = Layerqty - 1;
                                    string a3 = $"{Layerqty - 1}-{Layerqty}";
                                    LastRow = a3.ToString();
                                }
                                if (BMType == "07")
                                {
                                    this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 2);
                                }
                                if (BMType == "09")
                                {
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty - 1, i - 2);
                                }
                                if (BMType == "06")
                                {
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty - 1, i - 2);
                                }
                                if (BMType == "10")//修正
                                {
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty, i - 1);
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty - 1, i - 2);
                                }

                            }
                            else
                            if (i > 2 && i < Layerqty && (BMType == "03" || BMType == "05"))
                            {
                                string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                                this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                                this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_StretchSeq2", 1), 2);
                                if ((i - 1) > 2 && i % 2 != 0 && i != Layerqty)
                                //   if ((i == 4 || i == 6 || i == 8 || i == 10 || i == 12 || i == 14 || i == 16 || i == 18 || i == 20 || i == 22 || i == 24 || i == 26) && i != Layerqty)
                                {
                                    this.Model.SetValue("F_SZXY_sequence", Cenx);
                                    q1++;
                                }
                            }
                            else if (i > 2 && i < (Layerqty) && (BMType == "07" || BMType == "09"))
                            {
                                this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 1);
                                if ((i == 5 || i == 8 || i == 11 || i == 14 || i == 17 || i == 20) && i != Layerqty)
                                {
                                    string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                                    this.Model.SetValue("F_SZXY_sequence", Cenx);

                                }

                            }
                            else if (i >= 3 && (BMType == "04" || BMType == "06"))
                            {

                                this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 1);
                                if ((i == 5 || i == 7 || i == 9 || i == 11 || i == 13 || i == 15 || i == 17 || i == 19 || i == 21) && i != Layerqty)
                                {
                                    string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                                    this.Model.SetValue("F_SZXY_sequence", Cenx);
                                }
                            }
                            else if (i >= 3 && (BMType == "08" || BMType == "10"))
                            {
                                this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 1);
                                if ((i == 6 || i == 9 || i == 12 || i == 15 || i == 18 || i == 21))
                                {
                                    string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                                    this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                                    this.Model.SetValue("F_SZXY_sequence", Cenx);

                                }

                            }

                            if (FirstRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", FirstRow, 0);
                            if (SecRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", SecRow, 1);
                            if (LastRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", LastRow, rowcount - 1);
                            if (BMType == "08") this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty, rowcount);
                            if (BMType == "04" && LastRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", LastRow, rowcount);

                            #endregion



                        }



                        //02  物料认证层数生成序号
                        if (flag)
                        {
                            int a1 = rowcount - (renzenQty - 1);//有a1行特殊层序
                            int a2 = Convert.ToInt32(a1) + Convert.ToInt32(1);
                            // this.View.ShowErrMessage($"a1:{a1}a3:{a3}LastRow:{LastRow}");
                            int a3 = 2;
                            for (int i = 1; i <= rowcount; i++)
                            {
                                if (i < renzenQty)
                                {
                                    this.Model.SetValue("F_SZXY_StretchSeq2", i, i - 1);
                                }
                                else if (i >= renzenQty)
                                {

                                    string b3 = $"0{a3}";
                                    this.Model.SetValue("F_SZXY_StretchSeq2", b3, i - 1);
                                    a3++;
                                }
                            }
                        }
                        DynamicObject billobj1 = this.Model.DataObject;
                        DynamicObjectCollection entrys = billobj1["SZXY_XYLSEntry2"] as DynamicObjectCollection;
                        //bool qw = false;
                        if (Layerqty == this.View.Model.GetEntryRowCount("F_SZXY_XYLSEntity2"))
                        {

                            UpdateLsCX();
                        }

                        this.View.UpdateView("F_SZXY_StretchSeq2");
                        this.View.UpdateView("F_SZXY_sequence");
                    }


                    string orgName = Convert.ToString(orgobj["Name"]);
                    if (Convert.ToInt32(this.Model.GetValue("F_SZXY_LAYER")) == this.View.Model.GetEntryRowCount("F_SZXY_XYLSEntity2"))
                    {
                        int layqty = Convert.ToInt32(this.Model.GetValue("F_SZXY_LAYER"));
                        //string formID = this.View.BusinessInfo.GetForm().Id;
                        DynamicObject billobj = this.Model.DataObject;
                        DynamicObjectCollection Entrys = billobj["SZXY_XYLSEntry2"] as DynamicObjectCollection;
                        if (Entrys != null)
                        {
                            LsCxList = new List<LsCx>();
                            LsCxList.Clear();

                            int pp = 1;
                            foreach (var row in Entrys)
                            {

                                string lscx = Convert.ToString(row["F_SZXY_StretchSeq2"]);

                                if (LsCxList.Count == 0) LsCxList.Add(new LsCx { ID = pp, LSCx = lscx });
                                bool iscur = true;
                                if (LsCxList != null && LsCxList.Count > 0)
                                {

                                    for (int i = 0; i < LsCxList.Count; i++)
                                    {
                                        if (Convert.ToString(LsCxList[i].LSCx) == lscx)
                                        {

                                            iscur = false; break;

                                        }

                                    }

                                }
                                if (iscur)
                                {

                                    LsCxList.Add(new LsCx { ID = 1, LSCx = lscx });
                                }

                            }
                        }


                        string B1 = $"{Convert.ToInt32(layqty - 1)}-{layqty}";


                        //作废标记
                        if (LsCxList != null && this.Model.GetEntryRowCount("F_SZXY_XYLSEntity2") == layqty)// && !orgName.Contains("江苏")
                        {
                            for (int i = 0; i < LsCxList.Count; i++)
                            {
                                if (i == 0)
                                {
                                    LsCxList[i].IsZF = "Y";

                                }
                                if ((BMType == "04" || BMType == "06" || BMType == "08" || BMType == "10") && LsCxList[i].LSCx == "2")
                                {
                                    LsCxList[i].IsZF = "Y";
                                }
                                if (i == LsCxList.Count - 1)
                                {
                                    LsCxList[i].IsZF = "Y";
                                    if (BMType == "10" && i == LsCxList.Count - 1)
                                    {
                                        LsCxList[i - 1].IsZF = "Y";
                                    }


                                }
                                if ((BMType == "06" || BMType == "09" || BMType == "10") && i ==( LsCxList.Count -2))
                                {
                                    //if (BMType == "06" || BMType == "09")
                                    //{
                                    //    LsCxList[i - 1].IsZF = "Y";
                                    //}
                                    LsCxList[i].IsZF = "Y";
                                }


                            }
                        }

                        if (LsCxList != null && LsCxList.Count > 0)
                        {
                            for (int i = 0; i < LsCxList.Count; i++)
                            {
                                int index = i;
                                #region 单据体赋值
                                this.Model.CreateNewEntryRow("F_SZXY_XYLSEntity");

                                if (!CurrFHNO.IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_FHNOL", CurrFHNO.ToString(), index);
                                } 

                                if (this.Model.GetValue("F_SZXY_TeamGroup") != null) this.Model.SetValue("F_SZXY_TeamGroup1", this.Model.GetValue("F_SZXY_TeamGroup"), index);
                                if (this.Model.GetValue("F_SZXY_Classes") != null) this.Model.SetValue("F_SZXY_Classes1", this.Model.GetValue("F_SZXY_Classes"), index);
                                if (this.Model.GetValue("F_SZXY_MaterialID") != null) this.Model.SetValue("F_SZXY_Material", this.Model.GetValue("F_SZXY_MaterialID"), index);
                             
                                if (LsCxList[i].IsZF.EqualsIgnoreCase("Y")) this.Model.SetValue("F_SZXY_IsZF", "作废", index);
                                if (this.Model.GetValue("F_SZXY_plasticMac") != null) this.Model.SetValue("F_SZXY_CasMachine2", this.Model.GetValue("F_SZXY_plasticMac"), index);
                                if (this.Model.GetValue("F_SZXY_ComplexMac") != null) this.Model.SetValue("F_SZXY_MFMac", this.Model.GetValue("F_SZXY_ComplexMac"), index);
                                this.Model.SetValue("F_SZXY_Layer1", Convert.ToDecimal(this.Model.GetValue("F_SZXY_LAYER")), index);
                                this.Model.SetValue("F_SZXY_Stretch", Convert.ToDecimal(this.Model.GetValue("F_SZXY_SR")), index);
                                this.Model.SetValue("F_SZXY_PLy", Convert.ToDecimal(this.Model.GetValue("F_SZXY_PlyH")), index);
                                this.Model.SetValue("F_SZXY_Width", Convert.ToDecimal(this.Model.GetValue("F_SZXY_WidthH")), index);
                                this.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(this.Model.GetValue("F_SZXY_LenH")), index);
                                //this.Model.SetValue("F_SZXY_Area2", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["复合产出面积"]), index); 
                                this.Model.SetValue("F_SZXY_AreaE", (Convert.ToDecimal(this.Model.GetValue("F_SZXY_WidthH")) * Convert.ToDecimal(this.Model.GetValue("F_SZXY_LenH"))) / 1000, index);
                                DynamicObject Orgobj = billobj["F_SZXY_OrgId"] as DynamicObject;
                                string F_SZXY_StretchNo = Convert.ToString(this.Model.GetValue("F_SZXY_RecNo"));

                                if (!F_SZXY_StretchNo.IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_StretchNo", F_SZXY_StretchNo, index);
                                }
                                this.Model.SetValue("F_SZXY_RollNo", Convert.ToString(LsCxList[i].LSCx), index);

                                #endregion
                            }

                        }
                    }
                }
                //else if(!e.OldValue.IsNullOrEmptyOrWhiteSpace() && e.NewValue.IsNullOrEmptyOrWhiteSpace())
                //{
            


                //        // View.Model.DeleteEntryRow("F_SZXY_XYLSEntity1", e.Row);
                //        //this.Model.CreateNewEntryRow("F_SZXY_XYLSEntity1");
                //        //View.SetEntityFocusRow("F_SZXY_XYLSEntity1", e.Row);
                //}
                #endregion
                this.View.UpdateView("F_SZXY_XYLSEntity");
                this.View.UpdateView("F_SZXY_XYLSEntity2");
            }

        }

        /// <summary>
        /// 生成拉伸层序
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public string Getcx(string seq)
        {
            string st3 = "";
            switch (seq)
            {
                //双层上单取
                case "2-3":
                    st3 = "4-5";
                    break;
                case "4-5":
                    st3 = "6-7";
                    break;
                case "6-7":
                    st3 = "8-9";
                    break;
                case "8-9":
                    st3 = "10-11";
                    break;
                case "10-11":
                    st3 = "12-13";
                    break;
                case "12-13":
                    st3 = "14-15";
                    break;
                case "14-15":
                    st3 = "16-17";
                    break;
                case "16-17":
                    st3 = "18-19";
                    break;
                case "18-19":
                    st3 = "20-21";
                    break;
                case "20-21":
                    st3 = "22-23";
                    break;
                case "22-23":
                    st3 = "24-25";
                    break;
                case "24-25":
                    st3 = "26-27";
                    break;
                case "26-27":
                    st3 = "28-29";
                    break;


                case "2":
                    st3 = "3";
                    break;
                case "3":
                    st3 = "4";
                    break;
                case "4":
                    st3 = "5";
                    break;
                case "5":
                    st3 = "6";
                    break;

                case "6":
                    st3 = "7";
                    break;
                case "7":
                    st3 = "8";
                    break;
                case "8":
                    st3 = "9";
                    break;
                case "9":
                    st3 = "10";
                    break;
                case "10":
                    st3 = "11";
                    break;
                case "11":
                    st3 = "12";
                    break;
                case "12":
                    st3 = "13";
                    break;
                case "13":
                    st3 = "14";
                    break;
                case "14":
                    st3 = "15";
                    break;
                case "15":
                    st3 = "16";
                    break;
                case "16":
                    st3 = "17";
                    break;
                case "17":
                    st3 = "18";
                    break;
                case "18":
                    st3 = "19";
                    break;



                //双层上双取数
                case "3-4":
                    st3 = "5-6";
                    break;
                case "5-6":
                    st3 = "7-8";
                    break;
                case "7-8":
                    st3 = "9-10";
                    break;
                case "9-10":
                    st3 = "11-12";
                    break;
                case "11-12":
                    st3 = "13-14";
                    break;
                case "13-14":
                    st3 = "15-16";
                    break;
                case "15-16":
                    st3 = "17-18";
                    break;
                case "17-18":
                    st3 = "19-20";
                    break;

                case "19-20":
                    st3 = "21-22";
                    break;
                case "21-22":
                    st3 = "23-24";
                    break;
                case "23-24":
                    st3 = "25-26";
                    break;
                case "25-26":
                    st3 = "27-28";
                    break;






                ///多层 3-5开始  取数
                case "3-5":
                    st3 = "6-8";
                    break;
                case "6-8":
                    st3 = "9-11";
                    break;
                case "9-11":
                    st3 = "12-14";
                    break;
                case "12-14":
                    st3 = "15-17";
                    break;
                case "15-17":
                    st3 = "18-20";
                    break;
                case "18-20":
                    st3 = "21-23";
                    break;
                case "21-23":
                    st3 = "24-26";
                    break;
                case "24-26":
                    st3 = "27-29";
                    break;
                case "27-29":
                    st3 = "30-32";
                    break;


                ///多层 2-4开始  取数
                case "2-4":
                    st3 = "5-7";
                    break;
                case "5-7":
                    st3 = "8-10";
                    break;
                case "8-10":
                    st3 = "11-13";
                    break;
                case "11-13":
                    st3 = "14-16";
                    break;
                case "14-16":
                    st3 = "17-19";
                    break;
                case "17-19":
                    st3 = "20-22";
                    break;
                case "20-22":
                    st3 = "23-25";
                    break;
                case "23-25":
                    st3 = "26-28";
                    break;
                case "26-28":
                    st3 = "29-31";
                    break;
                case "29-31":
                    st3 = "32-34";
                    break;


            }
            return st3;
        }

        /// <summary>
        /// 修正拉伸层序
        /// </summary>
        public void UpdateLsCX()
        {
            //单据头层序

            int Layerqty = Convert.ToInt32(this.Model.GetValue("F_SZXY_LAYER"));//层数H
            int rowcount = this.Model.GetEntryRowCount("F_SZXY_XYLSEntity2");
            //int renzenQty = 12; //物料认证层数设置

            if (this.Model.GetValue("F_SZXY_BFType") is DynamicObject Bwtypobj && rowcount != 0 )
            {
                string BMType = Bwtypobj["FNumber"].ToString();
                if (Bwtypobj != null)
                {

                    if (BMType == "01" || flag)

                    {
                        this.Model.SetValue("F_SZXY_sequence", "2");//层序H

                    }
                    else if (BMType == "03" || BMType == "05")
                    {
                        this.Model.SetValue("F_SZXY_sequence", "2-3");//层序H

                    }
                    else if (BMType == "07" || BMType == "09")
                    {
                        this.Model.SetValue("F_SZXY_sequence", "2-4");//层序H

                    }
                    else if (BMType == "04" || BMType == "06")
                    {
                        this.Model.SetValue("F_SZXY_sequence", "3-4");//层序H
                    }
                    else if (BMType == "08" || BMType == "10")
                    {
                        this.Model.SetValue("F_SZXY_sequence", "3-5");//层序H
                    }


                }

                //循环单据体修改拉伸层序
                int q1 = 2;//大膜类型03  正常上单下双 07 09
                //int q2 = 2;//多层
                for (int i = 1; i <= rowcount; i++)
                {
                    if (BMType == "01"&&flag==false)
                    {
                        this.Model.SetValue("F_SZXY_StretchSeq2", i, i - 1);
                    }
                    string FirstRow = "0";
                    string SecRow = "0";
                    string LastRow = "0";
                    if (i == 1)//第一行
                    {
                        FirstRow = "1";
                    }
                    else //第二行
                    if (i == 2 && (BMType == "03" || BMType == "05" || BMType == "07" || BMType == "09"))//第2行
                    {
                        SecRow = this.Model.GetValue("F_SZXY_sequence").ToString();
                    }
                    else
                    if (i == 2 && (BMType == "04" || BMType == "06" || BMType == "08" || BMType == "10"))
                    {
                        this.Model.SetValue("F_SZXY_StretchSeq2", 2, 1);
                    }

                    else
                    if (i == rowcount && Layerqty == rowcount)//最后一行
                    {
                        if (BMType == "01" || BMType == "03" || BMType == "04" ||
                            BMType == "07" || BMType == "08" || BMType == "09"
                            || BMType == "10" || BMType == "06") LastRow = Layerqty.ToString();

                        if (BMType == "05")
                        {
                            int a1 = Layerqty;
                            int a2 = Layerqty - 1;
                            string a3 = $"{Layerqty - 1}-{Layerqty}";
                            LastRow = a3.ToString();
                        }
                        if (BMType == "07")
                        {
                            this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 2);
                        }
                        if (BMType == "09")
                        {
                            this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty - 1, i - 2);
                        }
                        if (BMType == "06")
                        {
                            this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty - 1, i - 2);
                        }
                        if (BMType == "10")//修正
                        {
                            this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty, i - 1);
                            this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty - 1, i - 2);
                        }


                    }
                    else
                    if (i > 2 && i < (Layerqty) && (BMType == "03" || BMType == "05"))
                    {
                        this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_StretchSeq2", 1), 2);
                        if ((i - 1) > 2 && i % 2 != 0)
                        {
                            string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                            this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);

                            this.Model.SetValue("F_SZXY_sequence", Cenx);
                            q1++;
                        }
                    }
                    else if (i > 2 && i < (Layerqty) && (BMType == "07" || BMType == "09"))
                    {
                        this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 1);
                        if ((i == 5 || i == 8 || i == 11 || i == 14 || i == 17 || i == 20 || i == 23 || i == 26 || i == 29) && i != Layerqty)
                        {
                            string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                            this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                            this.Model.SetValue("F_SZXY_sequence", Cenx);

                        }

                    }
                    else if (i >= 3 && (BMType == "04" || BMType == "06"))
                    {
                        this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 1);
                        if ((i == 5 || i == 7 || i == 9 || i == 11 || i == 13 || i == 15 || i == 17 || i == 19 || i == 21 || i == 23 || i == 25) && i != Layerqty)
                        {
                            string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                            this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                            this.Model.SetValue("F_SZXY_sequence", Cenx);
                        }
                    }
                    else if (i >= 3 && (BMType == "08" || BMType == "10"))
                    {
                        this.Model.SetValue("F_SZXY_StretchSeq2", this.Model.GetValue("F_SZXY_sequence").ToString(), i - 1);
                        if ((i == 6 || i == 9 || i == 12 || i == 15 || i == 18 || i == 21 || i == 24 || i == 27))
                        {
                            string Cenx = Getcx(this.Model.GetValue("F_SZXY_sequence").ToString());
                            this.Model.SetValue("F_SZXY_StretchSeq2", Cenx, i - 1);
                            this.Model.SetValue("F_SZXY_sequence", Cenx);

                        }

                    }
                    if (BMType == "05")
                    {
                        int a1 = Layerqty;
                        int a2 = Layerqty - 1;
                        string a3 = $"{Layerqty - 1}-{Layerqty}";
                        LastRow = a3.ToString();
                    }
                    if (FirstRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", FirstRow, 0);
                    if (SecRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", SecRow, 1);
                    if (LastRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", LastRow, rowcount - 1);
                    if (BMType == "08") this.Model.SetValue("F_SZXY_StretchSeq2", Layerqty, rowcount);
                    if (BMType == "05" && LastRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", LastRow, rowcount - 2);
                    if (BMType == "04" && LastRow != "0") this.Model.SetValue("F_SZXY_StretchSeq2", LastRow, rowcount);
                    // if (BMType == "03") this.Model.SetValue("F_SZXY_StretchSeq2", Convert.ToString( this.Model.GetValue("F_SZXY_sequence")), rowcount-2);

                }
                //02生成序号
                if (flag)
                {
                    int a1 = rowcount - (renzenQty - 1);//有a1行特殊层序
                    int a2 = Convert.ToInt32(a1) + Convert.ToInt32(1);
                    // this.View.ShowErrMessage($"a1:{a1}a3:{a3}LastRow:{LastRow}");
                    int a3 = 2;
                    for (int i = 1; i <= rowcount; i++)
                    {
                        if (i < renzenQty)
                        {
                            this.Model.SetValue("F_SZXY_StretchSeq2", i, i - 1);
                        }
                        else if (i >= renzenQty)
                        {

                            string b3 = $"0{a3}";
                            this.Model.SetValue("F_SZXY_StretchSeq2", b3, i - 1);
                            a3++;
                        }
                    }
                }
            }
        }

      


        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            this.Model.ClearNoDataRow();

            if (this.Model.DataObject["SZXY_XYLSEntry1"] is DynamicObjectCollection Entry)
            {
                List<DynamicObject> ListRow = new List<DynamicObject>() { };

                for (int i = 0; i < Entry.Count; i++)
                {

                    if (Convert.ToString(Entry[i]["F_SZXY_PlasticNo1"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        ListRow.Add((DynamicObject)Entry[i]);
                    }
                }

                ListRow.Reverse();
                foreach (var item in ListRow)
                {
                    Entry.Remove(item);
                }
                
            }

        }

        /// <summary>
        /// 删除行
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            bool flag = e.EntityKey.EqualsIgnoreCase("F_SZXY_XYLSEntity1");
            if (flag)
            {
                string FhNo = Convert.ToString(this.Model.GetValue("F_SZXY_PlasticNo1", e.Row));
                //改变流转状态
                bool flag2 = !FhNo.IsNullOrEmptyOrWhiteSpace();
                if (flag2)
                {
                    string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYFH set  F_SZXY_Integer={0}   where F_SZXY_RecNo='{1}' ", 0, FhNo);
                    int res = DBServiceHelper.Execute(Context, UpdateStateSql);
          
                    this.Model.CreateNewEntryRow("F_SZXY_XYLSEntity1");
                    this.View.SetEntityFocusRow("F_SZXY_XYLSEntity1", e.Row);
                    this.View.GetControl("F_SZXY_PlasticNo1").SetFocus();
                }
                #region  
                //删除拉伸信息单据体
                if (this.Model.DataObject["SZXY_XYLSEntry"] is DynamicObjectCollection entrys &&!Convert.ToString(FhNo).IsNullOrEmptyOrWhiteSpace())
                {
                    List<int> ListRow = new List<int>() { };
                    for (int i = 0; i < entrys.Count; i++)
                    {
                        if (!Convert.ToString(entrys[i]["F_SZXY_FHNOL"]).IsNullOrEmptyOrWhiteSpace() )
                        {
                            if (Convert.ToString( entrys[i]["F_SZXY_FHNOL"]).EqualsIgnoreCase(FhNo.ToString()))
                            {
                                ListRow.Add(i);
                            }
                      
                        }
                    }
                    ListRow.Reverse();
                    foreach (var item in ListRow)
                    {
                        this.Model.DeleteEntryRow("F_SZXY_XYLSEntity", item);
                    }
                    this.View.UpdateView("F_SZXY_XYLSEntity");
                }

                //删除流延信息单据体
                if (this.Model.DataObject["SZXY_XYLSEntry2"] is DynamicObjectCollection entrys2 && !Convert.ToString(FhNo).IsNullOrEmptyOrWhiteSpace())
                {
                    List<int> ListRow = new List<int>() { };
                    for (int i = 0; i < entrys2.Count; i++)
                    {
                        if (!Convert.ToString(entrys2[i]["F_SZXY_CompositeNo2"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            if (Convert.ToString(entrys2[i]["F_SZXY_CompositeNo2"]).EqualsIgnoreCase(FhNo.ToString()))
                            {
                                ListRow.Add(i);
                            }

                        }
                    }
                    ListRow.Reverse();
                    foreach (var item in ListRow)
                    {
                        this.Model.DeleteEntryRow("F_SZXY_XYLSEntity2", item);
                    }
                    this.View.UpdateView("F_SZXY_XYLSEntity2");
                }
                #endregion 删除复合明细行连带删除
            }
        }

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            DynamicObject BillHead = this.Model.DataObject;
            bool flag = BillHead != null && (e.Operation.FormOperation.Id.EqualsIgnoreCase("save") || e.Operation.FormOperation.Id.EqualsIgnoreCase("submit"));
            if (flag)
            {
                this.Model.ClearNoDataRow();
                #region 将复合膜的耗用长度回写到XY复合单上，计算出复合膜的剩余长度。
                DynamicObject billobj = this.Model.DataObject;
                //复合页签
                DynamicObject orgobj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;
                List<string> listSql = new List<string>() { };
                listSql.Clear();

                if (billobj["SZXY_XYLSEntry1"] is DynamicObjectCollection Entry)
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    decimal InArea = 0;
                    foreach (var Row in Entry)
                    {
                        string FHNo = Convert.ToString(Row["F_SZXY_PlasticNo1"]);//复合编号
                        string costLen = Convert.ToString(Row["F_SZXY_costLen"]);//耗损长度
                        if (FHNo != "")
                        {
                            string sql = $"/*dialect*/update  SZXY_t_XYFH set F_SZXY_Decimal={costLen}  where  SZXY_t_XYFH.F_SZXY_RecNo='{FHNo}'" +
                           $" and SZXY_t_XYFH.F_SZXY_OrgId={orgid} ";
                            //DBServiceHelper.Execute(Context, sql); 
                            decimal F_SZXY_CompositeLen = Convert.ToDecimal(Row["F_SZXY_Length1"]);
                            decimal F_SZXY_FixedLoss = Convert.ToDecimal(Row["F_SZXY_FixedLoss"]);
                            decimal F_SZXY_OGR = Convert.ToDecimal(billobj["F_SZXY_OGR"]);
                            decimal F_SZXY_LenH = Convert.ToDecimal(billobj["F_SZXY_LenH"]);
                            decimal FHQty = (F_SZXY_CompositeLen - F_SZXY_FixedLoss) * F_SZXY_OGR / F_SZXY_LenH;
                            string sql1 = $"/*dialect*/update  SZXY_t_XYFH set F_SZXY_FHQty={FHQty}    where  SZXY_t_XYFH.F_SZXY_RecNo='{FHNo}'" +
                                $" and  SZXY_t_XYFH.F_SZXY_OrgId={orgid} ";
                            listSql.Add(sql1);
 
                            InArea+= Convert.ToDecimal(Row["F_SZXY_FHAREA"]);
                        }
                    }
                    this.Model.SetValue("F_SZXY_InArea", InArea);
                    DBServiceHelper.ExecuteBatch(Context, listSql);

                  

                }
                #endregion

             //  面积 = 宽度 * 长度 * 记录条数（记录条数 = 层数 - 大膜类型）

                if (billobj["SZXY_XYLSEntry"] is DynamicObjectCollection entry2)
                {
                    decimal BillArea = 0;
                    foreach (var item in entry2.Where(op=>!Convert.ToString( op["F_SZXY_StretchNo"]).IsNullOrEmptyOrWhiteSpace()&&!Convert.ToString( op["F_SZXY_IsZF"]).EqualsIgnoreCase("作废")))
                    {
                        BillArea+= Convert.ToDecimal(item["F_SZXY_AreaE"]);
                    }
                    this.Model.SetValue("F_SZXY_AreaH", BillArea);
                }


                    #region 日期检查
                    string F_SZXY_MANHOUR = Convert.ToString(this.Model.GetValue("F_SZXY_MANHOUR"));
                
                if (this.Model.GetValue("F_SZXY_Stime").IsNullOrEmptyOrWhiteSpace() && this.Model.GetValue("F_SZXY_StoptimeH").IsNullOrEmptyOrWhiteSpace())
                {
                    throw new Exception("请检查日期时间是否输入有误");
                }
                string etime = Convert.ToDateTime(this.Model.DataObject["F_SZXY_StoptimeH"]).ToString("yyyyMMdd");

              
                if (this.Model.DataObject["F_SZXY_StoptimeH"] == null || Convert.ToDateTime(this.Model.DataObject["F_SZXY_StoptimeH"]) == DateTime.MinValue || etime.EqualsIgnoreCase("1991-01-01"))
                {
                    throw new Exception("单据头结束时间未设置");
                }
                DateTime Stime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_Stime"));
                DateTime Etime = Convert.ToDateTime(this.Model.GetValue("F_SZXY_StoptimeH"));
                int S = DateTime.Compare(Stime, Etime);
                bool flag4 = S > 0;
                if (flag4)
                {
                    throw new Exception("开始时间不能大于结束时间！");
                }
                #endregion



                #region  改写流转状态
                if (this.Model.DataObject["SZXY_XYLSEntry1"] is DynamicObjectCollection Entrys)
                {
                    foreach (var item in Entrys.Where(op => !Convert.ToString(op["F_SZXY_PlasticNo1"]).IsNullOrEmptyOrWhiteSpace()))
                    {
                        string LyNo = Convert.ToString(item["F_SZXY_PlasticNo1"]);

                        if (!LyNo.IsNullOrEmptyOrWhiteSpace())
                        {
                            //改写流转状态
                            string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYFH set  F_SZXY_Integer={0}   where F_SZXY_RecNo='{1}' ", 1, Convert.ToString(LyNo));
                            int res = DBServiceHelper.Execute(Context, UpdateStateSql);
                        }
                    }
                }
                #endregion


            }
        }

        public class LsCx
        {
            public int ID { get; set; }
            public string LSCx { get; set; }

            public string IsZF { get; set; }

        }
        private Dictionary<string, string> GetFieldsKey(string formID)
        {
            Dictionary<string, string> FieldsKey = new Dictionary<string, string>();
            try
            {
                switch (formID.ToUpper())
                {
                    case "SZXY_XYLS"://拉伸单Formid
                        FieldsKey.Add("Entity", "F_SZXY_XYLSEntity");//下有单据体key
                        FieldsKey.Add("Entry", "SZXY_XYLSEntry");//下有单据表体实体属性
                        FieldsKey.Add("THead", "SZXY_t_LSJTRSCJH");//上游单据表头表名
                        FieldsKey.Add("TEntry", "SZXY_t_LSJTRSCJHEntry");//上游单据表体表名
                        FieldsKey.Add("拉伸机台H", "F_SZXY_StretchMac");

                        FieldsKey.Add("生产订单号H", "F_SZXY_PTNoH");
                        FieldsKey.Add("生产订单行号H", "F_SZXY_POLineNum");
                        FieldsKey.Add("特殊标志H", "F_SZXY_SpecialMarkH");
                        FieldsKey.Add("班组H", "F_SZXY_TeamGroup");
                        FieldsKey.Add("班次H", "F_SZXY_Classes");
                        FieldsKey.Add("产品型号H", "F_SZXY_MaterialID");
                        FieldsKey.Add("配方H", "F_SZXY_Formula");
                        FieldsKey.Add("厚度H", "F_SZXY_PlyH");
                        FieldsKey.Add("宽度H", "F_SZXY_WidthH");
                        FieldsKey.Add("长度H", "F_SZXY_LenH");
                        FieldsKey.Add("面积H", "F_SZXY_AreaH");
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



        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
           
            this.Model.BatchCreateNewEntryRow("F_SZXY_XYLSEntity1", 2);
            this.View.UpdateView("F_SZXY_XYLSEntity1");
            this.View.GetControl<EntryGrid>("F_SZXY_XYLSEntity1").SetEnterMoveNextColumnCell(true);
        }
 
    }
}

