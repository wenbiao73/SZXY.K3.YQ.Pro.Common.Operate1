using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.PreInsertData.NetWorkCtrl;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
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
    [Description("分层单据操作。")]
    public class XYLayered : AbstractBillPlugIn
    {

        string MaterID = "";
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            //在XY分层单输入机台、时间，系统根据机台、时间匹配日计划记录，自动将相同的日计划记录携带到Xy分层单上

            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    #region 打印
                    Utils.TYPrint(this.View, Context, orgid);

                    DynamicObjectCollection entry = this.Model.DataObject["SZXY_XYFCEntry"] as DynamicObjectCollection;
                    bool flag5 = entry != null;
                    if (flag5)
                    {
                        foreach (DynamicObject item in from ck in entry
                                                       where Convert.ToString(ck["F_SZXY_PRINT"]).EqualsIgnoreCase("true")
                                                       select ck)
                        {
                            item["F_SZXY_PRINT"] = "false";
                        }
                        base.View.UpdateView("F_SZXY_XYFCEntity");
                        //billobj["FFormId"] = base.View.BusinessInfo.GetForm().Id;
                        //IOperationResult a = Utils.Save(base.View, new DynamicObject[]
                        //{
                        //    billobj
                        //}, OperateOption.Create(), base.Context);
                    }
                    #endregion
                }
            }





            //输入产品编号，同步拉伸明细数据
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbSyncLsData"))
            {
                string Sync = Convert.ToString(this.Model.GetValue("F_SZXY_Sync"));
                if (!Sync.EqualsIgnoreCase("True"))
                {
                    this.View.ShowWarnningMessage("请先勾选同步复选框！");
                    return;
                }

                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    string LSNO = Convert.ToString(this.Model.GetValue("F_SZXY_ProductNo"));

                    string MacId = "";
                    if (this.Model.GetValue("F_SZXY_Mac") is DynamicObject MacObj)
                    {
                        MacId = MacObj["Id"].ToString();
                    }

                    DateTime Stime = Convert.ToDateTime(billobj["F_SZXY_DatetimeL"]);
                    if (Stime == DateTime.MinValue || billobj["F_SZXY_DatetimeL"] == null)
                    {
                        Stime = DateTime.Now;
                        //this.View.Model.SetValue("F_SZXY_DatetimeL", Stime);
                    }
                    if (LSNO != "" && !LSNO.IsNullOrEmptyOrWhiteSpace() && !MacId.IsNullOrEmptyOrWhiteSpace())
                    {
                        string Sel = $"/*dialect*/select T5.FNAME '拉伸产品型号N',T6.FNAME '分层日计划产品型号N'," +
                               $"T2.F_SZXY_AreaE,T2.F_SZXY_RollNo,T2.F_SZXY_PLy,T2.F_SZXY_WIDTH,T2.F_SZXY_LEN,T2.F_SZXY_TEAMGROUP1," +
                               $" T2.F_SZXY_CLASSES1,T2.F_SZXY_LAYER1,T2.F_SZXY_SCRAPAREAE,T2.F_SZXY_AREAE,T2.F_SZXY_ISZF,T2.F_SZXY_STRETCHNO," +
                               $" T2.F_SZXY_WASTEREASON,T2.F_SZXY_XNDJ,T2.F_SZXY_BLYY,T2.F_SZXY_CASMACHINE2,T2.F_SZXY_MFMAC," +
                               $"T1.F_SZXY_FORMULA,T1.F_SZXY_VSNO,T1.F_SZXY_LENH,T1.F_SZXY_PlyH '拉伸单厚度' ,T1.F_SZXY_BFType '拉伸单大膜类型'," +
                               $"T1.F_SZXY_WIDTHH,T1.F_SZXY_LENH, " +//拉伸明细信息

                               $"  T3.F_SZXY_SMARK '特殊标志',T3.F_SZXY_Material '产品型号',T3.F_SZXY_MOID '生产订单内码', " +
                               $"  T3.F_SZXY_PTNO '生产订单单号',T3.F_SZXY_MOLINENO '生产订单行号',T3.F_SZXY_operator '操作员' " +
                               $"  ,T3.F_SZXY_LossLen '损耗长度',T3.F_SZXY_MACHINESTATE '机台状态',T3.FEntryID,T3.Fid,T3.F_SZXY_CJ '车间', " +
                               $" T3.F_SZXY_Team '班组',T3.F_SZXY_Class '班次'  " +//分层日计划信息

                               $" from  SZXY_t_XYLSEntry T2  " +
                               $" Left join SZXY_t_XYLS T1 on T2.Fid=T1.Fid " +
 
                               $" left join SZXY_t_FCJTRSCJHEntry T3 on T3.F_SZXY_MACHINE='{MacId}'  " +
                               $" left join SZXY_t_FCJTRSCJH T4 on T3.FID=T4.FID " +
                               $" left join T_BD_MATERIAL_L T5 on T5.FMATERIALID=T1.F_SZXY_MaterialID " +
                               $" left join T_BD_MATERIAL_L T6 on T6.FMATERIALID=T3.F_SZXY_MATERIAL " +

 
                               $" where  T2.F_SZXY_ISZF!='作废'" +
                               $" and T5.FNAME= T6.FNAME " +
                               $" and  T2.F_SZXY_STRETCHNO='{LSNO}'" +

                               $" and  T3.F_SZXY_Machine='{MacId}' " +
                               $" and T4.F_SZXY_OrgId={orgid} " +
                               $" and T4.FDOCUMENTSTATUS  in ('C')  " +
                               $"  and CONVERT(datetime ,'{Stime}', 120) between CONVERT(datetime ,T3.F_SZXY_SDATE, 120)  and  CONVERT(datetime ,T3.F_SZXY_ENDDATE, 120)  " +
                               $" order by T2.FSeq";

                        Logger.Debug("同步拉伸明细数据Sql", Sel);
                        DataSet ds = DBServiceHelper.ExecuteDataSet(Context, Sel);

                        DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYFCEntry"] as DynamicObjectCollection;

                        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            IViewService Services = ServiceHelper.GetService<IViewService>();
                            int Rowcount = this.Model.GetEntryRowCount("F_SZXY_XYFCEntity");
                            this.Model.DeleteEntryData("F_SZXY_XYFCEntity");

                            this.Model.BatchCreateNewEntryRow("F_SZXY_XYFCEntity", ds.Tables[0].Rows.Count);
                            this.View.UpdateView("F_SZXY_XYFCEntity");



                            #region

                            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                            {


                                int index = i;
                                if (i == 0)
                                {
                                    //单据头赋值
                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(ds.Tables[0].Rows[0]["生产订单内码"]));
                                    this.Model.SetValue("F_SZXY_PTNoH", ds.Tables[0].Rows[0]["生产订单单号"].ToString());
                                    this.Model.SetValue("F_SZXY_POLineNum", ds.Tables[0].Rows[0]["生产订单行号"].ToString());
                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(ds.Tables[0].Rows[0]["班组"]));
                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(ds.Tables[0].Rows[0]["班次"]));
                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["操作员"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_operatorH", Convert.ToInt64(ds.Tables[0].Rows[0]["操作员"]));
                                    if (!Convert.ToString(ds.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(ds.Tables[0].Rows[i]["车间"]));
                                }

                                //单据体赋值 F_SZXY_POLineNo F_SZXY_PTNO1
                                this.Model.SetValue("F_SZXY_RJHEntryID", Convert.ToString(ds.Tables[0].Rows[0]["FEntryID"]), index);
                                this.Model.SetValue("F_SZXY_PTNO1", ds.Tables[0].Rows[0]["生产订单单号"].ToString(), index);
                                this.Model.SetValue("F_SZXY_POLineNo", ds.Tables[0].Rows[0]["生产订单行号"].ToString(), index);
                                if (!Convert.ToString(ds.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", Convert.ToString(ds.Tables[0].Rows[0]["特殊标志"]), index);
                                if (!Convert.ToString(ds.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    //  PudMaterial = Convert.ToString(ds.Tables[0].Rows[0]["产品型号名称"]);
                                    string RMat = Utils.GetRootMatId(Convert.ToString(ds.Tables[0].Rows[0]["产品型号"]), orgid.ToString(), Context);
                                    this.Model.SetValue("F_SZXY_Material", RMat, index);


                                }


                                decimal SumArea = Convert.ToInt32(ds.Tables[0].Rows[i]["F_SZXY_AreaE"]);
                                this.Model.SetValue("F_SZXY_Area", SumArea, i);
                                this.Model.SetValue("F_SZXY_InArea1", SumArea, i);

                                this.Model.SetValue("F_SZXY_Layer", Convert.ToInt32(ds.Tables[0].Rows[i]["F_SZXY_LAYER1"]));


                                if (!Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_FORMULA"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_Formula", Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_FORMULA"]), i);
                                }

                                if (!Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_CasMachine2"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_CastMac", Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_CasMachine2"]), i);
                                }

                                this.Model.SetValue("F_SZXY_VSNo", Convert.ToInt32(ds.Tables[0].Rows[i]["F_SZXY_VSNO"]), i);
                                if (!Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_STRETCHNO"]).IsNullOrEmptyOrWhiteSpace() && !Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_ROLLNO"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_SEQUENCE", Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_ROLLNO"]), i);
                                    this.Model.SetValue("F_SZXY_StretchNo", Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_STRETCHNO"]), i);

                                    string FCNo = $"{Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_STRETCHNO"])}{ Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_ROLLNO"])}";

                                    this.Model.SetValue("F_SZXY_LayerNo", FCNo, i);

                                    //拉伸追溯
                                    SetLSTraceInfo(Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_STRETCHNO"]), Convert.ToString(ds.Tables[0].Rows[i]["F_SZXY_ROLLNO"]), i, Context, this.View);
                                }

                                this.Model.SetValue("F_SZXY_InLen", Convert.ToDecimal(ds.Tables[0].Rows[i]["F_SZXY_LENH"]), i);
                                this.Model.SetValue("F_SZXY_OutWidth", Convert.ToDecimal(ds.Tables[0].Rows[i]["F_SZXY_WIDTHH"]), i);
                                this.Model.SetValue("F_SZXY_InWidth", Convert.ToDecimal(ds.Tables[0].Rows[i]["F_SZXY_WIDTHH"]), i);

                                this.Model.SetValue("F_SZXY_InPly", Convert.ToDecimal(ds.Tables[0].Rows[i]["拉伸单厚度"]), i);
                                this.Model.SetValue("F_SZXY_OutPLy", Convert.ToDecimal(ds.Tables[0].Rows[i]["拉伸单厚度"]), i);


                                if (!Convert.ToString(ds.Tables[0].Rows[i]["拉伸单大膜类型"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_FILMTYPE", Convert.ToString(ds.Tables[0].Rows[i]["拉伸单大膜类型"]), i);
                                //产出长度 = XY拉伸单的长度 - 日计划损耗长度F_SZXY_LossLen
                                string FEntryIDRJH = Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID", i));


                                decimal lossLen = 0;
                                lossLen = Convert.ToDecimal(this.Model.GetValue("F_SZXY_LOSSWIDTH"));
                                decimal LSLen = Convert.ToDecimal(Convert.ToDecimal(ds.Tables[0].Rows[i]["F_SZXY_LENH"]));

                                decimal F_SZXY_OutLen = LSLen - lossLen;
                                this.Model.SetValue("F_SZXY_OutLen", F_SZXY_OutLen, i);

                                //产出面积=产出宽度*产出长度  
                                decimal OUTAREA = (Convert.ToDecimal(ds.Tables[0].Rows[i]["F_SZXY_WIDTHH"]) * F_SZXY_OutLen) / 1000;

                                if (Convert.ToString( this.Model.GetValue("F_SZXY_sequence",i))=="1")
                                {
                                    OUTAREA = 0;
                                }
                                this.Model.SetValue("F_SZXY_OUTAREA", OUTAREA, i);
                                this.Model.SetValue("F_SZXY_ZGXMJ", OUTAREA, i);

                                //判断层序在拉伸是否为作废，作废将面积设置为0  F_SZXY_Area F_SZXY_InArea1

                                string FCCX = Convert.ToString(this.Model.GetValue("F_SZXY_sequence"));
                                bool IsZFRes = false;

                                CheckCXIsZF(Context, LSNO, FCCX, ref IsZFRes);
                                if (IsZFRes)
                                {
                                    this.Model.SetValue("F_SZXY_OUTAREA", Convert.ToDecimal(0), i);
                                    this.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(0), i);
                                    this.Model.SetValue("F_SZXY_InArea1", Convert.ToDecimal(0), i);
                                    this.Model.SetValue("F_SZXY_ZGXMJ", Convert.ToDecimal(0), i);
                                }

                            
                                //成品
                                this.Model.SetValue("F_SZXY_DelaminationC", true, i);


                                #endregion
                            }
                            this.View.InvokeFormOperation("SAVE");
                            this.View.UpdateView("F_SZXY_XYFCEntity");
                            //this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            //Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                        }
                        else
                        {
                            this.View.ShowWarnningMessage("没有匹配到数据！"); return;
                        }

                    }
                    else
                    {
                        this.View.ShowWarnningMessage("请录入产品编号，机台！"); return;
                    }
                }
            }
        }


        public string PudMaterial { get; private set; }
        public string BFTypeName { get; private set; }
        public int renzenQTY { get; private set; }


        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);

            //分层成品 与XY拉伸单层次一致时默认打勾（分层的最终产品默认打勾）


            if (this.Model.DataObject["SZXY_XYFCEntry"] is DynamicObjectCollection entrys)
            {
                int index = 0;
                foreach (var row in entrys.Where(m => !Convert.ToString(m["F_SZXY_LayerNo"]).IsNullOrEmptyOrWhiteSpace()))
                {
                    string FCCX = Convert.ToString(row["F_SZXY_sequence"]);
                    string FCNO = Convert.ToString(row["F_SZXY_LayerNo"]);
                    string DFDRowId = Convert.ToString(row["F_SZXY_DFDEntryID"]);


                    if (DFDRowId != "" && !DFDRowId.IsNullOrEmptyOrWhiteSpace())
                    {
                        string Sql = $"/*dialect*/select T3.F_SZXY_ROLLNO from SZXY_t_DFDEntry T1  " +
                            $" join SZXY_t_DFD  T2  on T1.FID=T2.FID " +
                            $"   join SZXY_t_XYLSEntry T3 on T3.F_SZXY_StretchNO=T1.F_SZXY_StretchNO " +
                            $" join SZXY_t_XYLS T4 on T3.Fid=T4.Fid " +
                            $"  where T1.FEntryID= {DFDRowId} and T3.F_SZXY_ISZF!='作废' ";// and T4.F_SZXY_OrgId ={Convert.ToString(this.Model.DataObject["F_SZXY_OrgId_Id"])} ";
                        DataSet Ds1 = Utils.CBData(Sql, Context);
                        if (Ds1 != null && Ds1.Tables.Count > 0 && Ds1.Tables[0].Rows.Count > 0)
                        {
                            bool iscur = Ds1.Tables[0].Rows.Cast<DataRow>().Any(x => (x["F_SZXY_ROLLNO"].ToString() == FCCX));
                            if (iscur)
                            {
                                this.Model.SetValue("F_SZXY_DelaminationC", true, (index));
                            }
                        }
                    }



                    index++;
                }

            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

 
            int Rowcount = this.Model.GetEntryRowCount("F_SZXY_XYFCEntity");
            if (Rowcount != 4)
            {
                string Sync = Convert.ToString(this.Model.GetValue("F_SZXY_Sync"));
                if (!Sync.EqualsIgnoreCase("true"))
                {
                    this.Model.BatchCreateNewEntryRow("F_SZXY_XYFCEntity", 4 - Rowcount);
                    this.View.UpdateView("F_SZXY_XYFCEntity");
                }

            }
           

        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);


            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_ProductNo"))
            {
                string Sync = Convert.ToString(this.Model.GetValue("F_SZXY_Sync"));
                if (Sync.EqualsIgnoreCase("true"))
                {
                    return;
                }
                DynamicObject orgobj1 = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;
                if (e.OldValue.IsNullOrEmptyOrWhiteSpace() && !e.NewValue.IsNullOrEmptyOrWhiteSpace() ||
                    (!e.OldValue.IsNullOrEmptyOrWhiteSpace() && !e.NewValue.IsNullOrEmptyOrWhiteSpace() && e.OldValue != e.NewValue))
                {

                    #region
                    DynamicObject billobj = this.Model.DataObject;

                    string F_SZXY_FcType = Convert.ToString(this.Model.GetValue("F_SZXY_FcType"));

                    if (billobj["F_SZXY_Mac"] is DynamicObject MacObj && Convert.ToString(this.Model.GetValue("F_SZXY_FcType")) != "0")
                    {

                        DynamicObjectCollection entry = billobj["SZXY_XYFCEntry"] as DynamicObjectCollection;//单据体

                        string MacId = MacObj["Id"].ToString();
                        long orgid = Convert.ToInt64(orgobj1["Id"]);
                        DateTime Stime = Convert.ToDateTime(billobj["F_SZXY_DatetimeL"]);
                        if (Stime == DateTime.MinValue)
                        {
                          
                            Stime = DateTime.Now;
                           // this.View.Model.SetValue("F_SZXY_DatetimeL", Stime);
                        }

                        if (Stime != DateTime.MinValue)
                        {
                            string CodeMatName = "";
                            string SelCodeMatName = "/*dialect*/select top 1 T5.FName '产品型号名称' " +
                                                "from SZXY_t_DFD T1 Left " +
                                                "join SZXY_t_DFDEntry T2 on T1.FID = T2.FID " +
                                                "left " +
                                                "join SZXY_t_XYLSEntry T3 on T3.F_SZXY_StretchNo = T2.F_SZXY_StretchNO " +
                                                "left " +
                                                "join SZXY_t_XYLS T4 on T3.FID = T4.FID " +
                                                "left " +
                                                "join T_BD_MATERIAL_L T5  on T5.FMATERIALID = T4.F_SZXY_MaterialID " +
                                                $"where T2.F_SZXY_BIPARTITIONNO = '{e.NewValue.ToString()}' " +
                                                "union " +
                                                "select top 1 T6.FName '产品型号名称' " +
                                                "from SZXY_t_XYFCEntry T2 " +
                                                "left Join SZXY_t_XYFC T1 on T1.Fid = T2.Fid " +
                                                "left join SZXY_t_XYLS T5 on T5.F_SZXY_RECNO = T2.F_SZXY_STRETCHNO " +
                                                "left join T_BD_MATERIAL_L T6  on T6.FMATERIALID = T5.F_SZXY_MaterialID " +
                                                $"where T2.F_SZXY_LAYERNO = '{e.NewValue.ToString()}' ";
 
                            DataSet SelCodeMatNameDs = DBServiceHelper.ExecuteDataSet(this.Context, SelCodeMatName);
                            if (SelCodeMatNameDs != null && SelCodeMatNameDs.Tables.Count > 0 && SelCodeMatNameDs.Tables[0].Rows.Count > 0)
                            {
                                CodeMatName= SelCodeMatNameDs.Tables[0].Rows[0][0].ToString();
                            }

                                string Sql = $"/*dialect*/select T2.F_SZXY_SMARK '特殊标志',T2.F_SZXY_Material '产品型号',T2.F_SZXY_MOID '生产订单内码',T4.FName '产品型号名称'," +
                                           $"T2.F_SZXY_PTNO '生产订单单号',T2.F_SZXY_MOLINENO '生产订单行号',T2.F_SZXY_operator '操作员'" +
                                           $",T2.F_SZXY_LossLen '损耗长度',T2.F_SZXY_MACHINESTATE '机台状态',T2.FEntryID,T2.Fid,T2.F_SZXY_CJ '车间'," +
                                           $"T2.F_SZXY_Team '班组',T2.F_SZXY_Class '班次',T1.FDOCUMENTSTATUS  " +
                                           $" from SZXY_t_FCJTRSCJH T1" +
                                           $" Left join SZXY_t_FCJTRSCJHEntry T2  on T1.Fid=T2.Fid  " +
                                           $" left join T_BD_MATERIAL T3  on T3.FMATERIALID=T2.F_SZXY_Material  " +
                                           $" left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                                           $" where T2.F_SZXY_Machine='{MacId}'" +
                                           $" and T1.F_SZXY_OrgId={orgid} " +
                                           $" and T4.FName='{CodeMatName}' " +
                                           $" and CONVERT(datetime ,'{Stime}', 120) between CONVERT(datetime ,T2.F_SZXY_SDATE, 120)  and  CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                           $" and T1.FDOCUMENTSTATUS  in ('C')";

                            Logger.Debug("日计划匹配SQL", Sql);
                            DataSet DpDs = DBServiceHelper.ExecuteDataSet(Context, Sql);
                            if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                            {
                                int index;
                                int m = this.Model.GetEntryRowCount("F_SZXY_XYFCEntity");
                                for (int i = 0; i < m; i++)
                                {
                                    index = i;
                                    if (i == 0)
                                    {
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(DpDs.Tables[0].Rows[0]["生产订单内码"]));
                                        this.Model.SetValue("F_SZXY_PTNoH", DpDs.Tables[0].Rows[0]["生产订单单号"].ToString());
                                        this.Model.SetValue("F_SZXY_POLineNum", DpDs.Tables[0].Rows[0]["生产订单行号"].ToString());
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(DpDs.Tables[0].Rows[0]["班组"]));
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(DpDs.Tables[0].Rows[0]["班次"]));
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["操作员"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_operatorH", Convert.ToInt64(DpDs.Tables[0].Rows[0]["操作员"]));
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));
                                    }
                                    int F_SZXY_FcQty = Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", i));
                                    if (F_SZXY_FcQty > 0)
                                    {
                                        //单据体赋值 F_SZXY_POLineNo F_SZXY_PTNO1
                                        this.Model.SetValue("F_SZXY_RJHEntryID", Convert.ToString(DpDs.Tables[0].Rows[0]["FEntryID"]), index);
                                        this.Model.SetValue("F_SZXY_PTNO1", DpDs.Tables[0].Rows[0]["生产订单单号"].ToString(), index);
                                        this.Model.SetValue("F_SZXY_POLineNo", DpDs.Tables[0].Rows[0]["生产订单行号"].ToString(), index);
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", Convert.ToString(DpDs.Tables[0].Rows[0]["特殊标志"]), index);
                                        if (!Convert.ToString(DpDs.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                                        {
                                            PudMaterial = Convert.ToString(DpDs.Tables[0].Rows[0]["产品型号名称"]);
                                            string RMat = Utils.GetRootMatId(Convert.ToString(DpDs.Tables[0].Rows[0]["产品型号"]), orgid.ToString(), Context);
                                            this.Model.SetValue("F_SZXY_Material", RMat, index);
                                        }

                                    }

                                }
                                this.Model.GetEntryRowCount("F_SZXY_XYFCEntity");

                                billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                                Utils.Save(View, new DynamicObject[] { billobj }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                                this.View.UpdateView("F_SZXY_XYFCEntity");
                            }
                            else { this.View.ShowMessage("没有匹配到对应的日计划！"); return; }
                        }

                    }
                    else
                    { this.View.ShowMessage("请先录入机台和分层类型！"); return; }

                    #endregion

                    string DFNo = this.Model.GetValue("F_SZXY_ProductNo").ToString();//产品编号 F_SZXY_RJHEntryID

                    if (DFNo != "" && this.Model.GetValue("F_SZXY_OrgId") is DynamicObject orgobj)
                    {
                        long orgid = Convert.ToInt64(orgobj["Id"]);

                        string SelSql = "/*Dialect*/select top 1 " +
                                            "null  F_SZXY_StretchNO,null F_SZXY_bipartitionNO, T2.F_SZXY_bipartition '对分单层数'," +
                                            " T1.F_SZXY_FormulaH '对分单配方', T2.F_SZXY_bipartitionArea '对分单面积', " +
                                            " T2.F_SZXY_bipartitionWidth '对分单宽度', T2.F_SZXY_StretchNO '拉伸编号', T4.F_SZXY_MaterialID '拉伸产品型号'," +
                                            "T2.F_SZXY_BIPARTITIONNO , T4.F_SZXY_Formula '拉伸单配方',T4.F_SZXY_BFType '拉伸单大膜类型', " +
                                            " T2.F_SZXY_StretchC '对分单层次',T3.FSeq '流延seq',T3.F_SZXY_RollNo '拉伸单层序'," +
                                            "T3.F_SZXY_AreaE '拉伸单面积',T2.FEntryID '对分RowID'" +
                                            "  ,T3.F_SZXY_CasMachine2 '流延机' ,T4.F_SZXY_VSNO '卷序' ,T4.F_SZXY_LenH '拉伸单长度'," +
                                            "T4.F_SZXY_PlyH '拉伸单厚度'" +
                                            " from SZXY_t_DFD T1 Left join SZXY_t_DFDEntry T2 on T1.FID = T2.FID " +
                                            " left join SZXY_t_XYLSEntry T3 on T3.F_SZXY_StretchNo = T2.F_SZXY_StretchNO " +
                                            " left join SZXY_t_XYLS T4 on T3.FID = T4.FID " +
                                            $" where T2.F_SZXY_BIPARTITIONNO = '{DFNo}' " +
                                            // $" and  T1.F_SZXY_OrgId ={orgid} " +

                                            $"  union  " +
                                            $" select top 1 T3.F_SZXY_StretchNO,T3.F_SZXY_bipartitionNO, " +
                                            $" T3.F_SZXY_bipartition '对分单层数', T6.F_SZXY_FormulaH '对分单配方', T3.F_SZXY_bipartitionArea '对分单面积',  " +
                                            $"  T3.F_SZXY_bipartitionWidth '对分单宽度', T3.F_SZXY_StretchNO '拉伸编号', T5.F_SZXY_MaterialID '拉伸产品型号', " +
                                            $"T3.F_SZXY_BIPARTITIONNO ,T5.F_SZXY_Formula '拉伸单配方', T5.F_SZXY_BFType '拉伸单大膜类型'," +
                                            $"  T3.F_SZXY_StretchC '对分单层次',T4.FSeq '流延seq',T4.F_SZXY_RollNo '拉伸单层序',T4.F_SZXY_AreaE '拉伸单面积',T3.FEntryID '对分RowID' " +
                                            $"  ,T4.F_SZXY_CasMachine2 '流延机' ,T5.F_SZXY_VSNO '卷序' ,T5.F_SZXY_LenH '拉伸单长度',T5.F_SZXY_PlyH '拉伸单厚度' " +
                                            $"  from SZXY_t_XYFCEntry T2  " +
                                            $"  left Join SZXY_t_XYFC T1 on T1.Fid=T2.Fid    " +
                                            $" left join SZXY_t_DFDEntry T3  on  T3.FEntryID=T2.F_SZXY_DFDENTRYID " +//T3.F_SZXY_bipartitionNO=T1.F_SZXY_ProductNo " +
                                            $" left Join SZXY_t_DFD T6 on T6.Fid=T3.Fid  " +
                                            $" left join SZXY_t_XYLSEntry T4 on T4.F_SZXY_StretchNo=T3.F_SZXY_StretchNO " +
                                            $" left join SZXY_t_XYLS T5 on T4.Fid=T5.FID " +
                                            $" where  T2.F_SZXY_LAYERNO='{DFNo}'";
                        //$" and  T1.F_SZXY_OrgId ={orgid}  ";

                        Logger.Debug("匹配分层或者对分的SQL", SelSql);
                        DataSet ds = Utils.CBData(SelSql, Context);

                        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            string F_SZXY_StretchNO = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_StretchNO"]);
                            if (F_SZXY_StretchNO.IsNullOrEmptyOrWhiteSpace())
                            {
                                //第一次 扫对分
                                FristSetValue(ds, DFNo, orgid);

                                this.View.UpdateView("F_SZXY_XYFCEntity");
                            }
                            else
                            {

                                //多次分层 扫分层
                                //string SqlSelLS = $"/*Dialect*/select T3.F_SZXY_StretchNO,T3.F_SZXY_bipartitionNO from SZXY_t_XYFCEntry T2 " +
                                //                     "  left Join SZXY_t_XYFC T1 on T1.Fid = T2.Fid " +
                                //                     " left join SZXY_t_DFDEntry T3  on T3.F_SZXY_bipartitionNO = T1.F_SZXY_ProductNo " +
                                //                     $"  where T2.F_SZXY_LAYERNO = '{DFNo}'" +

                                //DataSet DsN = Utils.CBData(SqlSelLS, Context);
                                //if (DsN == null) throw new Exception("该编码没有匹配到对应的数据");
                                string StretchNO = "";//对应的拉伸编号
                                string DFBH = "";//对应的对分编号
                                StretchNO = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_StretchNO"]);
                                DFBH = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_bipartitionNO"]);


                                string SqlSelLS = $"select T1.F_SZXY_STRETCHNO,T4.FDATAVALUE,T5.F_SZXY_renzenQTY " +
                                               $"from SZXY_t_DFDEntry T1 left join SZXY_t_DFD T2 on T1.Fid=T2.Fid" +
                                               $" left join SZXY_t_XYLS T3 on T1.F_SZXY_STRETCHNO=T3.F_SZXY_RECNO " +
                                               $" left join t_bas_assistantdataentry_l T4 on T3.F_SZXY_BFTYPE=T4.Fentryid " +
                                               $" left join T_BD_MATERIAL T5 on T5.FMATERIALID= T3.F_SZXY_MATERIALID " +

                                               $" where T1.F_SZXY_BIPARTITIONNO='{DFBH}'";
                                DataSet Ds = Utils.CBData(SqlSelLS, Context);

                                if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
                                {
                                    StretchNO = Convert.ToString(Ds.Tables[0].Rows[0][0]);
                                    BFTypeName = Convert.ToString(Ds.Tables[0].Rows[0]["FDATAVALUE"]);
                                    renzenQTY = Convert.ToInt32(Ds.Tables[0].Rows[0]["F_SZXY_renzenQTY"]);
                                }

                                string FCCX1 = "";
                                if (DFNo != "" && StretchNO != "") FCCX1 = DFNo.Replace(StretchNO, string.Empty);
                                if (!FCCX1.Contains('-') || DFNo.Length <= 3)
                                {
                                    this.View.ShowWarnningMessage("输入的编码有误！"); return;
                                }
                                string[] strArray = FCCX1.Split('-');
                                int MinCX = Convert.ToInt32(strArray[0]);
                                int MaxCX = Convert.ToInt32(strArray[1]);

                                int countFcQty = 0;
                                if (this.Model.DataObject["SZXY_XYFCEntry"] is DynamicObjectCollection entrys)
                                {
                                    List<int> ListRow = new List<int>() { };

                                    for (int i = 0; i < entrys.Count; i++)
                                    {
                                        countFcQty += Convert.ToInt32(entrys[i]["F_SZXY_FcQty"]);

                                        if (Convert.ToString(entrys[i]["F_SZXY_FcQty"]).IsNullOrEmptyOrWhiteSpace() || Convert.ToInt32(entrys[i]["F_SZXY_FcQty"]) == 0)
                                        {
                                            ListRow.Add(i);
                                        }
                                    }

                                    if (ListRow.Count > 0)
                                    {
                                        ListRow.Reverse();
                                        foreach (var item in ListRow)
                                        {
                                            this.Model.DeleteEntryRow("F_SZXY_XYFCEntity", item);
                                        }
                                    }

                                }


                                if (!Convert.ToString(BFTypeName).EqualsIgnoreCase("单层") && renzenQTY <= 0)
                                {
                                    //其他大膜类型
                                    if (countFcQty != (MaxCX - MinCX + 1))
                                    {
                                        throw new Exception("单据体分层总数不能大于或小于此编号分层数");
                                    }
                                }
                                else
                                {
                                    //单层加认证层数判断
                                    string MinCX1 = Convert.ToString(strArray[0]);
                                    string MaxCX1 = Convert.ToString(strArray[1]);

                                    string selMinMaxFSeq = $"/*Dialect*/select FSeq from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  F_SZXY_ROLLNO='{MinCX1}' " +
                                                           $" union all" +
                                                           $" select FSeq from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  F_SZXY_ROLLNO='{MaxCX1}' ";

                                    DataSet selMinMaxFSeqDs = DBServiceHelper.ExecuteDataSet(Context, selMinMaxFSeq);
                                    if (selMinMaxFSeqDs != null && selMinMaxFSeqDs.Tables.Count > 0 && selMinMaxFSeqDs.Tables[0].Rows.Count > 0)
                                    {
                                        if (selMinMaxFSeqDs.Tables[0].Rows.Count == 2)
                                        {
                                            MinCX = Convert.ToInt32(selMinMaxFSeqDs.Tables[0].Rows[0][0]);
                                            MaxCX = Convert.ToInt32(selMinMaxFSeqDs.Tables[0].Rows[1][0]);
                                            if (countFcQty != (MaxCX - MinCX + 1))
                                            {
                                                throw new Exception("单据体分层总数不能大于或小于此编号分层数");
                                            }
                                        }

                                    }

                                }
                                string F_SZXY_UW = Convert.ToString(this.Model.GetValue("F_SZXY_UW"));//0 正放，1 反放
                                int FcType = Convert.ToInt32(this.Model.GetValue("F_SZXY_FcType"));
                                if (this.Model.GetEntryRowCount("F_SZXY_XYFCEntity") > 1 && this.Model.GetEntryRowCount("F_SZXY_XYFCEntity") < 5)
                                {
                                    List<dynamic> ListData = new List<dynamic>();
                                    List<dynamic> ListDataRZ = new List<dynamic>();

                                    for (int i = 0; i < this.Model.GetEntryRowCount("F_SZXY_XYFCEntity"); i++)
                                    {

                                        int FcQty = Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", i));
                                        if (countFcQty == 0 || F_SZXY_UW.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            throw new Exception("请先录入明细里的条件分层数！");
                                        }

                                        string FCCX = "";

                                        if (F_SZXY_UW == "0")
                                        {  //正放 
                                            if (i == 0) FCCX = $"{MinCX}-{ MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}";
                                            if (i == 1) FCCX = $"{MinCX + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}-{ MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}";
                                            if (i == 2) FCCX = $"{MinCX + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}-{MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2))}";
                                            if (i == 3) FCCX = $"{MinCX + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2))}-{ MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 3))}";
                                        }
                                        else if (F_SZXY_UW == "1")
                                        {  //反放  
                                            if (i == 0) FCCX = $"{MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}-{ MaxCX}";
                                            if (i == 1) FCCX = $"{MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}-{ MaxCX - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}";
                                            if (i == 2) FCCX = $"{ MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2))}-{MaxCX - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}";
                                            if (i == 3) FCCX = $"{MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 3))}-{ MaxCX - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2)) }";
                                        }

                                        if (!Convert.ToString(BFTypeName).EqualsIgnoreCase("单层") && renzenQTY <= 0)
                                        {
                                            this.Model.SetValue("F_SZXY_sequence", FCCX, i);
                                            string[] FCCXArray = FCCX.Split('-');
                                            int A = Convert.ToInt32(FCCXArray[0]);
                                            int B = Convert.ToInt32(FCCXArray[1]);
                                            if (A == B) this.Model.SetValue("F_SZXY_sequence", A, i);
                                        }
                                        else
                                        {
                                            ///大模类型为 单层加物料层数认证时

                                            string[] FCCXArray = FCCX.Split('-');
                                            string A = Convert.ToString(FCCXArray[0]);
                                            string B = Convert.ToString(FCCXArray[1]);
                                            var FCInfo1 = new { FcQty, FCCX };
                                            ListDataRZ.Add(FCInfo1);

                                            string selMinMaxFSeq = "/*Dialect*/select * from ( " +
                                                  $"select F_SZXY_ROLLNO,FSeq  from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  FSeq='{A}' " +
                                                  $" union all " +
                                                  $" select F_SZXY_ROLLNO,FSeq  from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  FSeq='{B}') T0" +
                                                  $" order by FSeq";

                                            DataSet selMinMaxFSeqDs = DBServiceHelper.ExecuteDataSet(Context, selMinMaxFSeq);

                                            if (selMinMaxFSeqDs != null && selMinMaxFSeqDs.Tables.Count > 0 && selMinMaxFSeqDs.Tables[0].Rows.Count > 0)
                                            {
                                                if (selMinMaxFSeqDs.Tables[0].Rows.Count == 2)
                                                {
                                                    string MinCX1 = Convert.ToString(selMinMaxFSeqDs.Tables[0].Rows[0][0]);
                                                    string MaxCX1 = Convert.ToString(selMinMaxFSeqDs.Tables[0].Rows[1][0]);
                                                    FCCX = $"{MinCX1}-{MaxCX1}";
                                                    this.Model.SetValue("F_SZXY_sequence", $"{MinCX1}-{MaxCX1}", i);
                                                    if (MinCX1 == MaxCX1) this.Model.SetValue("F_SZXY_sequence", MaxCX1, i);

                                                }

                                            }

                                        }


                                        var FCInfo = new { FcQty, FCCX };
                                        ListData.Add(FCInfo);
                                        if (FcQty != 0)
                                        {
                                            ListData.Add(FCInfo);
                                        }
                                    }
                                    this.View.UpdateView("F_SZXY_sequence");

                                    //获取汇总面积
                                    // GetSummaryArea(ListData, orgid, Context, this.View, StretchNO, DFBH);

                                    if (ListDataRZ.Count > 0 && ListDataRZ != null)
                                    {
                                        //大模类型为单层认证层数匹配获取汇总面积
                                        GetSummaryAreaForRenZen(ListDataRZ, orgid, DFBH, Context, this.View);
                                    }
                                    else if (ListData.Count > 0)
                                    {
                                        GetSummaryArea(ListData, orgid, Context, this.View, StretchNO, DFBH);
                                    }


                                    if (ListData != null && ListData.Count > 0)
                                    {
                                        for (int a = 0; a < ListData.Count; a++)
                                        {
                                            //分层编号
                                            string FCCX = Convert.ToString(this.Model.GetValue("F_SZXY_sequence", a));
                                            if (!FCCX.IsNullOrEmptyOrWhiteSpace() && !StretchNO.IsNullOrEmptyOrWhiteSpace())
                                            {
                                                string F_SZXY_LayerNo = $"{StretchNO}{FCCX}";
                                                if (F_SZXY_LayerNo != "") this.Model.SetValue("F_SZXY_LayerNo", F_SZXY_LayerNo, a);

                                                //拉伸追溯
                                                SetLSTraceInfo(StretchNO, FCCX, a, Context, this.View);

                                                IViewService Services = ServiceHelper.GetService<IViewService>();
                                                DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYFCEntry"] as DynamicObjectCollection;
                                                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                                {
                                                    if (MaterID != "")
                                                    {

                                                        DynamicObject F_SZXY_Material = this.Model.GetValue("F_SZXY_Material", a) as DynamicObject;
                                                        Utils.SetBaseDataValue(Services, entry1[a], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material"), Convert.ToInt64(MaterID)
                                                            , ref F_SZXY_Material, Context);
                                                    }

                                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["拉伸产品型号"]).IsNullOrEmptyOrWhiteSpace())
                                                    {
                                                        string RMat = Utils.GetRootMatId(Convert.ToString(ds.Tables[0].Rows[0]["拉伸产品型号"]), orgid.ToString(), Context);
                                                        this.Model.SetValue("F_SZXY_LSMATERIALID", RMat, a);
                                                        DynamicObject F_SZXY_LSMATERIALID = this.Model.GetValue("F_SZXY_LSMATERIALID", a) as DynamicObject;
                                                        if (F_SZXY_LSMATERIALID != null)
                                                        {
                                                            if (F_SZXY_LSMATERIALID["Name"].ToString() != PudMaterial)
                                                            {
                                                                this.View.ShowWarnningMessage("拉伸单产品型号和分层日计划的产品型号的名称不一致！"); return;
                                                            }
                                                        }
                                                        //
                                                    }
                                                    this.Model.SetValue("F_SZXY_Layer", Convert.ToInt32(ds.Tables[0].Rows[0]["对分单层数"]));
                                                    this.Model.SetValue("F_SZXY_DFDEntryID", Convert.ToString(ds.Tables[0].Rows[0]["对分RowID"]), a);
                                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["拉伸单配方"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Formula", Convert.ToString(ds.Tables[0].Rows[0]["拉伸单配方"]), a);
                                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["流延机"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CastMac", Convert.ToString(ds.Tables[0].Rows[0]["流延机"]), a);
                                                    this.Model.SetValue("F_SZXY_VSNo", Convert.ToInt32(ds.Tables[0].Rows[0]["卷序"]), a);
                                                    this.Model.SetValue("F_SZXY_StretchNo", Convert.ToString(ds.Tables[0].Rows[0]["拉伸编号"]), a);
                                                    this.Model.SetValue("F_SZXY_InLen", Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单长度"]), a);
                                                    this.Model.SetValue("F_SZXY_OutWidth", Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"]), a);
                                                    this.Model.SetValue("F_SZXY_InWidth", Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"]), a);
                                                    this.Model.SetValue("F_SZXY_InPly", Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单厚度"]), a);
                                                    this.Model.SetValue("F_SZXY_OutPLy", Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单厚度"]), a);
                                                    if (!Convert.ToString(ds.Tables[0].Rows[0]["拉伸单大膜类型"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_FILMTYPE", Convert.ToString(ds.Tables[0].Rows[0]["拉伸单大膜类型"]), a);

                                                    //产出长度 = XY拉伸单的长度 - 日计划损耗长度F_SZXY_LossLen  拉伸单大膜类型
                                                    string FEntryIDRJH = Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID", a));
                                                    if (!FEntryIDRJH.IsNullOrEmptyOrWhiteSpace())
                                                    {
                                                        //string LenSql = $"select F_SZXY_LossLen from SZXY_t_FCJTRSCJHEntry where FEntryID={FEntryIDRJH}";
                                                        //DataSet Lends = Utils.CBData(LenSql, Context);

                                                        decimal lossLen = 0;
                                                        decimal LSLen = Convert.ToDecimal(Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单长度"]));
                                                        //if (Lends != null && Lends.Tables.Count > 0 && Lends.Tables[0].Rows.Count > 0) lossLen = Convert.ToDecimal(Lends.Tables[0].Rows[0]["F_SZXY_LossLen"]);
                                                        //this.Model.SetValue("F_SZXY_LOSSWIDTH", lossLen);

                                                        lossLen = Convert.ToDecimal(this.Model.GetValue("F_SZXY_LOSSWIDTH"));
                                                        decimal F_SZXY_OutLen = LSLen - lossLen;

                                                        this.Model.SetValue("F_SZXY_OutLen", F_SZXY_OutLen, a);
                                                        //产出面积= 产出宽度 * 产出长度  

                                                        string YMDJ = "";
                                                        if (!StretchNO.IsNullOrEmptyOrWhiteSpace())
                                                        {
                                                            string SelLev = $"select F_SZXY_XNDJ from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO='{StretchNO}'";
                                                            DataSet SelLevDs = Utils.CBData(SelLev, Context);
                                                            if (SelLevDs != null && SelLevDs.Tables.Count > 0 && SelLevDs.Tables[0].Rows.Count > 0)
                                                            {
                                                                YMDJ = Convert.ToString(SelLevDs.Tables[0].Rows[0][0]);// 等级
                                                            }
                                                        }

                                                        if (!YMDJ.IsNullOrEmptyOrWhiteSpace())
                                                        {
                                                            this.Model.SetValue("F_SZXY_YMDJ", YMDJ, a);
                                                        }

                                                        decimal OUTAREA = (Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"]) * F_SZXY_OutLen) / 1000;

                                                        if (Convert.ToString(this.Model.GetValue("F_SZXY_sequence",a)) == "1")
                                                        {
                                                            OUTAREA = 0;
                                                        }

                                                        this.Model.SetValue("F_SZXY_OUTAREA", OUTAREA, a);
                                                        this.Model.SetValue("F_SZXY_ZGXMJ", OUTAREA, a);

                                                        //判断层序在拉伸是否为作废，作废将面积设置为0  F_SZXY_Area F_SZXY_InArea1
                                                        bool IsZFRes = false;
                                                        CheckCXIsZF(Context, StretchNO, FCCX, ref IsZFRes);
                                                        if (IsZFRes)
                                                        {
                                                            this.Model.SetValue("F_SZXY_OUTAREA", Convert.ToDecimal(0), a);
                                                            this.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(0), a);
                                                            this.Model.SetValue("F_SZXY_InArea1", Convert.ToDecimal(0), a);
                                                            this.Model.SetValue("F_SZXY_ZGXMJ", Convert.ToDecimal(0), a);
                                                        }

                                                       
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }
                            }

                        }
                        else
                        {
                            this.View.ShowMessage("此条码没有找到相关记录信息！"); return;
                        }

                        this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                        Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                    }
                    this.View.UpdateView("F_SZXY_XYFCEntity");

                }

                else if (!e.OldValue.IsNullOrEmptyOrWhiteSpace() && e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {
                    //this.Model.DeleteEntryData("F_SZXY_XYFCEntity");
                    //5.30
                    //int Rowcount=   this.Model.GetEntryRowCount("F_SZXY_XYFCEntity");
                    //if (this.Model.DataObject["SZXY_XYFCEntry"] is DynamicObjectCollection entrys)
                    //{
                    //    List<int> ListRow = new List<int>() { };
                    //    for (int i = 0; i < Rowcount; i++)
                    //    {
                    //        if (i > 3)
                    //        {
                    //            ListRow.Add(i);
                    //        }
                    //    }

                    //    ListRow.Reverse();
                    //    foreach (var item in ListRow)
                    //    {
                    //        this.Model.DeleteEntryRow("F_SZXY_XYFCEntity", item);
                    //    }
                    //    this.View.UpdateView("F_SZXY_XYFCEntity");
                    //}


 

                }
            }


            // 产出报废面积 = 报废宽度* 产出长度/ 1000
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_ScrapWidth") || e.Field.Key.EqualsIgnoreCase("F_SZXY_OutLen"))
            {
                int index = e.Row - 1;
                decimal ScrapWidth = Convert.ToDecimal(this.Model.GetValue("F_SZXY_ScrapWidth", index));
                decimal OutLen = Convert.ToDecimal(this.Model.GetValue("F_SZXY_OutLen", index));
                if (ScrapWidth != 0 && OutLen != 0) this.Model.SetValue("F_SZXY_OutScrapArea1", Convert.ToDecimal((ScrapWidth * OutLen) / Convert.ToDecimal(1000)), index);
            }
        }


        /// <summary>
        /// 第一次分层赋值
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="dFNo"></param>
        /// <param name="Orgid"></param>
        private void FristSetValue(DataSet ds, string dFNo, long Orgid)
        {
            string formID = this.View.BillBusinessInfo.GetForm().Id;
            string SqlSelLS = $"select T1.F_SZXY_STRETCHNO,T4.FDATAVALUE,T5.F_SZXY_renzenQTY " +
                              $"from SZXY_t_DFDEntry T1 " +
                              $"left join SZXY_t_DFD T2 on T1.Fid=T2.Fid" +

                              $" left join SZXY_t_XYLS T3 on T1.F_SZXY_STRETCHNO=T3.F_SZXY_RECNO " +
                              $" left join t_bas_assistantdataentry_l T4 on T3.F_SZXY_BFTYPE=T4.Fentryid " +
                              $" left join T_BD_MATERIAL T5 on T5.FMATERIALID= T3.F_SZXY_MATERIALID " +
                              $" where T1.F_SZXY_BIPARTITIONNO='{dFNo}'";
    

            DataSet Ds = Utils.CBData(SqlSelLS, Context);
            string StretchNO = "";
            string DFCX = "";

          
            if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
            {

                StretchNO = Convert.ToString(Ds.Tables[0].Rows[0][0]);//拉伸编号
                BFTypeName = Convert.ToString(Ds.Tables[0].Rows[0]["FDATAVALUE"]);//大膜类型名称
                renzenQTY = Convert.ToInt32(Ds.Tables[0].Rows[0]["F_SZXY_renzenQTY"]);//认证层数

                if (dFNo != "") DFCX = dFNo.Replace(StretchNO, string.Empty);
            }

            string YMDJ = "";
            if (!StretchNO.IsNullOrEmptyOrWhiteSpace())
            {
                string SelLev = $"select F_SZXY_XNDJ from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO='{StretchNO}'";
                DataSet SelLevDs = Utils.CBData(SelLev, Context);
                if (SelLevDs != null && SelLevDs.Tables.Count > 0 && SelLevDs.Tables[0].Rows.Count > 0)
                {
                    YMDJ = Convert.ToString(SelLevDs.Tables[0].Rows[0][0]);// 等级
                }
            }


            if (!DFCX.Contains('-') || dFNo.Length <= 3)
            {
                this.View.ShowWarnningMessage("输入的编码有误！"); return;
            }


            string[] strArray = DFCX.Split('-');
            int MinCX = Convert.ToInt32(strArray[0]);
            int MaxCX = Convert.ToInt32(strArray[1]);



            int countFcQty = 0;
            if (this.Model.DataObject["SZXY_XYFCEntry"] is DynamicObjectCollection entrys)
            {
                List<int> ListRow = new List<int>() { };
                for (int i = 0; i < entrys.Count; i++)
                {
                    countFcQty += Convert.ToInt32(entrys[i]["F_SZXY_FcQty"]);
                    if (Convert.ToString(entrys[i]["F_SZXY_FcQty"]).IsNullOrEmptyOrWhiteSpace() || Convert.ToInt32(entrys[i]["F_SZXY_FcQty"]) == 0)
                    {
                        ListRow.Add(i);
                        //this.Model.DeleteEntryRow("F_SZXY_XYFCEntity", i);
                    }
                }
                ListRow.Reverse();
                foreach (var item in ListRow)
                {
                    this.Model.DeleteEntryRow("F_SZXY_XYFCEntity", item);
                }


            }

            if (!Convert.ToString(BFTypeName).EqualsIgnoreCase("单层") && renzenQTY <= 0)
            {

                if (countFcQty != (MaxCX - MinCX + 1))
                {
                    throw new Exception("单据体分层总数不能大于或小于此编号分层数");
                }
            }
            else
            {

                string MinCX1 = Convert.ToString(strArray[0]);
                string MaxCX1 = Convert.ToString(strArray[1]);
                //单层加认证层数判断
                string selMinMaxFSeq = $"/*Dialect*/select FSeq from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  F_SZXY_ROLLNO='{MinCX1}' " +
                                    $" union all " +
                                    $" select FSeq from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  F_SZXY_ROLLNO='{MaxCX1}' ";
                DataSet selMinMaxFSeqDs = DBServiceHelper.ExecuteDataSet(Context, selMinMaxFSeq);
                if (selMinMaxFSeqDs != null && selMinMaxFSeqDs.Tables.Count > 0 && selMinMaxFSeqDs.Tables[0].Rows.Count > 0)
                {
                    if (selMinMaxFSeqDs.Tables[0].Rows.Count == 2)
                    {
                        MinCX = Convert.ToInt32(selMinMaxFSeqDs.Tables[0].Rows[0][0]);
                        MaxCX = Convert.ToInt32(selMinMaxFSeqDs.Tables[0].Rows[1][0]);
                        if (countFcQty != (MaxCX - MinCX + 1))
                        {
                            throw new Exception("单据体分层总数不能大于或小于此编号分层数");
                        }
                    }

                }
            }


            string F_SZXY_UW = Convert.ToString(this.Model.GetValue("F_SZXY_UW"));//0 正放，1 反放

            int FcType = Convert.ToInt32(this.Model.GetValue("F_SZXY_FcType"));


            if (this.Model.GetEntryRowCount("F_SZXY_XYFCEntity") > 1 && this.Model.GetEntryRowCount("F_SZXY_XYFCEntity") < 5)
            {
                List<dynamic> ListData = new List<dynamic>();
                List<dynamic> ListDataRZ = new List<dynamic>();

                for (int i = 0; i < this.Model.GetEntryRowCount("F_SZXY_XYFCEntity"); i++)
                {
                    int FcQty = Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", i));
                    if (F_SZXY_UW == "" || countFcQty == 0)
                    {
                        //this.Model.SetValue("F_SZXY_ProductNo", "");
                        throw new Exception("请先录入单据体分层条件：分层数");
                    }
                    string FCCX = "";

                    if (F_SZXY_UW == "0")
                    {  //正放// 
                        if (i == 0) FCCX = $"{MinCX}-{ MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}";
                        if (i == 1) FCCX = $"{MinCX + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}-{ MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}";
                        if (i == 2) FCCX = $"{MinCX + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}-{MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2))}";
                        if (i == 3) FCCX = $"{MinCX + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2))}-{ MinCX - 1 + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2)) + Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 3))}";
                    }
                    else if (F_SZXY_UW == "1")
                    {   //反放  
                        if (i == 0) FCCX = $"{MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}-{ MaxCX}";
                        if (i == 1) FCCX = $"{MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}-{ MaxCX - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0))}";
                        if (i == 2) FCCX = $"{ MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2))}-{MaxCX - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1))}";
                        if (i == 3) FCCX = $"{MaxCX + 1 - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 3))}-{ MaxCX - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 0)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 1)) - Convert.ToInt32(this.Model.GetValue("F_SZXY_FcQty", 2)) }";
                    }
                    if (!Convert.ToString(BFTypeName).EqualsIgnoreCase("单层") && renzenQTY <= 0)
                    {
                        this.Model.SetValue("F_SZXY_sequence", FCCX, i);
                        string[] FCCXArray = FCCX.Split('-');
                        int A = Convert.ToInt32(FCCXArray[0]);
                        int B = Convert.ToInt32(FCCXArray[1]);
                        if (A == B) this.Model.SetValue("F_SZXY_sequence", A, i);

                    }
                    else
                    {

                        string[] FCCXArray = FCCX.Split('-');
                        string A = Convert.ToString(FCCXArray[0]);
                        string B = Convert.ToString(FCCXArray[1]);
                        var FCInfo1 = new { FcQty, FCCX };
                        ListDataRZ.Add(FCInfo1);

                        string selMinMaxFSeq = "/*Dialect*/select * from ( " +
                              $"select F_SZXY_ROLLNO,FSeq  from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  FSeq='{A}' " +
                              $" union all " +
                              $" select F_SZXY_ROLLNO,FSeq  from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO ='{StretchNO}' and  FSeq='{B}') T0" +
                              $" order by FSeq";
                        DataSet selMinMaxFSeqDs = DBServiceHelper.ExecuteDataSet(Context, selMinMaxFSeq);
                        if (selMinMaxFSeqDs != null && selMinMaxFSeqDs.Tables.Count > 0 && selMinMaxFSeqDs.Tables[0].Rows.Count > 0)
                        {
                            if (selMinMaxFSeqDs.Tables[0].Rows.Count == 2)
                            {
                                string MinCX1 = Convert.ToString(selMinMaxFSeqDs.Tables[0].Rows[0][0]);
                                string MaxCX1 = Convert.ToString(selMinMaxFSeqDs.Tables[0].Rows[1][0]);
                                FCCX = $"{MinCX1}-{MaxCX1}";
                                this.Model.SetValue("F_SZXY_sequence", $"{MinCX1}-{MaxCX1}", i);
                                if (MinCX1 == MaxCX1) this.Model.SetValue("F_SZXY_sequence", MaxCX1, i);

                            }

                        }

                    }

                    var FCInfo = new { FcQty, FCCX };
                    if (FcQty != 0)
                    {
                        ListData.Add(FCInfo);

                    }

                }
                this.View.UpdateView("F_SZXY_sequence");
                //面积汇总

                if (ListDataRZ.Count > 0 && ListDataRZ != null)
                {
                    //认证层数匹配
                    GetSummaryAreaForRenZen(ListDataRZ, Orgid, dFNo, Context, this.View);
                }
                else if (ListData.Count > 0)
                {
                    GetSummaryArea(ListData, Orgid, dFNo, Context, this.View);
                }




                if (ListData != null)
                {
                    for (int a = 0; a < ListData.Count; a++)
                    {
                        //分层编号
                        string FCCX = Convert.ToString(this.Model.GetValue("F_SZXY_sequence", a));
                        if (!FCCX.IsNullOrEmptyOrWhiteSpace() && !StretchNO.IsNullOrEmptyOrWhiteSpace())
                        {
                            string F_SZXY_LayerNo = $"{StretchNO}{FCCX}";
                            if (StretchNO != "" && FCCX != "")
                            {
                                this.Model.SetValue("F_SZXY_LayerNo", F_SZXY_LayerNo, a);
                                //获取拉伸追溯信息

                                SetLSTraceInfo(StretchNO, FCCX, a, Context, this.View);

                                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                                {
                                    string F_SZXY_RJHEntryID = Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID"));
                                    if (!F_SZXY_RJHEntryID.IsNullOrEmptyOrWhiteSpace())
                                    {

                                        #region
                                        if (MaterID != "")
                                        {
                                            IViewService Services = ServiceHelper.GetService<IViewService>();
                                            DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYFCEntry"] as DynamicObjectCollection;
                                            DynamicObject F_SZXY_Material = this.Model.GetValue("F_SZXY_Material", a) as DynamicObject;
                                            Utils.SetBaseDataValue(Services, entry1[a], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material"), Convert.ToInt64(MaterID)
                                                , ref F_SZXY_Material, Context);
                                        }
                                        if (!Convert.ToString(ds.Tables[0].Rows[0]["拉伸产品型号"]).IsNullOrEmptyOrWhiteSpace())
                                        {
                                            string RMat = Utils.GetRootMatId(Convert.ToString(ds.Tables[0].Rows[0]["拉伸产品型号"]), Orgid.ToString(), Context);
                                            this.Model.SetValue("F_SZXY_LSMATERIALID", RMat, a);

                                            DynamicObject F_SZXY_LSMATERIALID = this.Model.GetValue("F_SZXY_LSMATERIALID", a) as DynamicObject;

                                            if (F_SZXY_LSMATERIALID != null)
                                            {
                                                if (F_SZXY_LSMATERIALID["Name"].ToString() != PudMaterial)
                                                {
                                                    this.View.ShowWarnningMessage("拉伸单产品型号和分层日计划的产品型号的名称不一致！"); return;
                                                }
                                            }
                                        }
                                        this.Model.SetValue("F_SZXY_Layer", Convert.ToInt32(ds.Tables[0].Rows[0]["对分单层数"]));
                                        this.Model.SetValue("F_SZXY_DFDEntryID", Convert.ToString(ds.Tables[0].Rows[0]["对分RowID"]), a);
                                        if (!Convert.ToString(ds.Tables[0].Rows[0]["拉伸单配方"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Formula", Convert.ToString(ds.Tables[0].Rows[0]["拉伸单配方"]), a);
                                        if (!Convert.ToString(ds.Tables[0].Rows[0]["流延机"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CastMac", Convert.ToString(ds.Tables[0].Rows[0]["流延机"]), a);
                                        this.Model.SetValue("F_SZXY_VSNo", Convert.ToInt32(ds.Tables[0].Rows[0]["卷序"]), a);
                                        this.Model.SetValue("F_SZXY_StretchNo", Convert.ToString(ds.Tables[0].Rows[0]["拉伸编号"]), a);
                                        this.Model.SetValue("F_SZXY_InLen", Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单长度"]), a);
                                        this.Model.SetValue("F_SZXY_OutWidth", Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"]), a);
                                        this.Model.SetValue("F_SZXY_InWidth", Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"]), a);
                                        this.Model.SetValue("F_SZXY_InPly", Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单厚度"]), a);
                                        this.Model.SetValue("F_SZXY_OutPLy", Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单厚度"]), a);

                                        if (!Convert.ToString(ds.Tables[0].Rows[0]["拉伸单大膜类型"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_FILMTYPE", Convert.ToString(ds.Tables[0].Rows[0]["拉伸单大膜类型"]), a);
                                        //产出长度 = XY拉伸单的长度 - 日计划损耗长度F_SZXY_LossLen
                                        string FEntryIDRJH = Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID", a));

                                        if (!YMDJ.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            this.Model.SetValue("F_SZXY_YMDJ", YMDJ, a);
                                        }
                                        
                                        decimal lossLen = 0;
                                        decimal LSLen = Convert.ToDecimal(Convert.ToDecimal(ds.Tables[0].Rows[0]["拉伸单长度"]));

                                        lossLen = Convert.ToDecimal(this.Model.GetValue("F_SZXY_LOSSWIDTH"));
                                        decimal F_SZXY_OutLen = LSLen - lossLen;
                                        this.Model.SetValue("F_SZXY_OutLen", F_SZXY_OutLen, a);

                                        //产出面积=产出宽度*产出长度  F_SZXY_OUTAREA=Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"])*F_SZXY_OutLen
                                        decimal OUTAREA = (Convert.ToDecimal(ds.Tables[0].Rows[0]["对分单宽度"]) * F_SZXY_OutLen) / 1000;

                                        if (Convert.ToString(this.Model.GetValue("F_SZXY_sequence",a)) == "1")
                                        {
                                            OUTAREA = 0;
                                        }

                                        this.Model.SetValue("F_SZXY_OUTAREA", OUTAREA, a);
                                        this.Model.SetValue("F_SZXY_ZGXMJ", OUTAREA, a);
                                        //判断层序在拉伸是否为作废，作废将面积设置为0  F_SZXY_Area F_SZXY_InArea1
                                        bool IsZFRes = false;

                                        CheckCXIsZF(Context, StretchNO, FCCX, ref IsZFRes);
                                        if (IsZFRes)
                                        {
                                            this.Model.SetValue("F_SZXY_OUTAREA", Convert.ToDecimal(0), a);
                                            this.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(0), a);
                                            this.Model.SetValue("F_SZXY_InArea1", Convert.ToDecimal(0), a);
                                            this.Model.SetValue("F_SZXY_ZGXMJ", Convert.ToDecimal(0), a);
                                        }
                     

                                        #endregion
                                    }

                                }
                            }

                        }

                    }
                }



            }


        }


        /// <summary>
        ///  //判断层序在拉伸是否为作废，作废返回true
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stretchNO"></param>
        /// <param name="fCCX"></param>
        /// <param name="isZFRes"></param>
        private void CheckCXIsZF(Context context, string stretchNO, string fCCX, ref bool isZFRes)
        {
            if (!stretchNO.IsNullOrEmptyOrWhiteSpace()&& !fCCX.IsNullOrEmptyOrWhiteSpace())
            {
                string SelISZFSql = $"/*dialect*/select F_SZXY_ISZF from SZXY_t_XYLSEntry where F_SZXY_StretchNO='{stretchNO}' and F_SZXY_ROLLNO='{fCCX}'";
                DataSet SelISZFSqlDS = DBServiceHelper.ExecuteDataSet(Context, SelISZFSql);

                if (SelISZFSqlDS != null && SelISZFSqlDS.Tables.Count > 0 && SelISZFSqlDS.Tables[0].Rows.Count > 0)
                {
                    string iszf = Convert.ToString(SelISZFSqlDS.Tables[0].Rows[0][0]);
                    if (iszf.EqualsIgnoreCase("作废"))
                    {
                        isZFRes = true;
                    }
                }
            }
 
        }

        /// <summary>
        /// 设置拉伸追溯信息
        /// </summary>
        /// <param name="拉伸编号"></param>
        /// <param name="拉伸层序"></param>
        /// <param name="当前行"></param>
        /// <param name="context"></param>
        /// <param name="view"></param>
        private void SetLSTraceInfo(string stretchNO, string fCCX, int a, Context context, IBillView view)
        {
            string SelLSsql = $"/*dialect*/select" +
                                     $" T6.F_SZXY_PLY13 '透气率',T3.F_SZXY_POROSITYAVG '孔隙率',T7.F_SZXY_PUTSAVG '穿刺强度' " +
                                     $",T2.F_SZXY_ZHDJ '综合等级',T2.F_SZXY_APPDESCRIPTION '外观描述',T4.F_SZXY_90OCMDAVG '热收缩90'," +
                                     $" T4.F_SZXY_105MDAVG '热收缩105' ,T4.F_SZXY_120MDAVG '热收缩120',T7.F_SZXY_MPAMDAVG '拉伸强度' " +
                                     $" from  SZXY_t_LSTraceDEntry T2  " + //拉伸明细
                                     $" left join SZXY_t_LSTraceAdenDEntry T3 on T2.F_SZXY_STRETCHSEQ=T3.F_SZXY_STRETCHSEQ3 and T2.F_SZXY_STRETCHNO=T3.F_SZXY_PLASTICNO1   " + //--面密度 
                                     $" left join SZXY_t_LSTraceTemDEntry T4 on  T2.F_SZXY_STRETCHSEQ= T4.F_SZXY_STRETCHSEQ5 and T2.F_SZXY_STRETCHNO=T4.F_SZXY_PLASTICNO1 " +// --85℃-150℃判定结果 
                                     $" left join SZXY_t_LSTraceFilmDEntry T5 on  T2.F_SZXY_STRETCHSEQ= T5.F_SZXY_STRETCHSEQ1 and T2.F_SZXY_STRETCHNO=T5.F_SZXY_PLASTICNO   " +// --大膜厚度
                                     $" left join SZXY_t_LSTraceLayDEntry T6 on  T2.F_SZXY_STRETCHSEQ=T6.F_SZXY_STRETCHSEQ2 and T2.F_SZXY_STRETCHNO=T6.F_SZXY_PLASTICNO   " +// --单层厚度判定结果
                                     $" left join SZXY_t_LSTraceLayTenDEntry T7 on  T2.F_SZXY_STRETCHSEQ=T7.F_SZXY_STRETCHSEQ4 and T2.F_SZXY_STRETCHNO=T7.F_SZXY_PLASTICNO1   " +// --拉伸强度
                                     $" where  T2.F_SZXY_STRETCHNO='{stretchNO}' and t2.F_SZXY_STRETCHSEQ='{fCCX}' " +
                                    $"  union " +
                                    $" select t1.透气率平均 as 透气率,t1.孔隙率平均值 as 孔隙率,t1.穿刺强度平均值 as 穿刺强度," +
                                   $"    t2.fid as 综合等级,t1.外观描述 as 外观描述,t1.热收缩90平均 as 热收缩90,t1.热收缩105平均值 as 热收缩105,t1.热收缩120平均值 as 热收缩120,t1.拉伸强度平均值 '拉伸强度' " +
                                   $"  from [192.168.10.103].[AIS20140603092603].[dbo].[v_XYFC_LS] as t1 " +
                                   $"   left join(select b.fid, b.fname, a.f_szxy_process from SZXY_t_Cust100015 a join SZXY_t_Cust100015_L b on a.fid= b.FID and a.f_szxy_process= 'SZXY_XYLS') t2 on t1.综合判定 = t2.fname " +
                                   $"  where 拉伸编号 = '{stretchNO}' and 层序 = '{fCCX}' ";


            //string SelLSsql = $"/*dialect*/select" +
            //                         $" T6.F_SZXY_PLY13 '透气率',T3.F_SZXY_POROSITYAVG '孔隙率',T7.F_SZXY_PUTSAVG '穿刺强度' " +
            //                         $",T2.F_SZXY_ZHDJ '综合等级',T2.F_SZXY_APPDESCRIPTION '外观描述',T4.F_SZXY_90OCMDAVG '热收缩90'," +
            //                         $" T4.F_SZXY_105MDAVG '热收缩105' ,T4.F_SZXY_120MDAVG '热收缩120',T7.F_SZXY_MPAMDAVG '拉伸强度' " +
            //                         $" from  SZXY_t_LSTraceDEntry T2  " + //拉伸明细
            //                         $" left join SZXY_t_LSTraceAdenDEntry T3 on T2.F_SZXY_STRETCHSEQ=T3.F_SZXY_STRETCHSEQ3 and T2.F_SZXY_STRETCHNO=T3.F_SZXY_PLASTICNO1   " + //--面密度 
            //                         $" left join SZXY_t_LSTraceTemDEntry T4 on  T2.F_SZXY_STRETCHSEQ= T4.F_SZXY_STRETCHSEQ5 and T2.F_SZXY_STRETCHNO=T4.F_SZXY_PLASTICNO1 " +// --85℃-150℃判定结果 
            //                         $" left join SZXY_t_LSTraceFilmDEntry T5 on  T2.F_SZXY_STRETCHSEQ= T5.F_SZXY_STRETCHSEQ1 and T2.F_SZXY_STRETCHNO=T5.F_SZXY_PLASTICNO   " +// --大膜厚度
            //                         $" left join SZXY_t_LSTraceLayDEntry T6 on  T2.F_SZXY_STRETCHSEQ=T6.F_SZXY_STRETCHSEQ2 and T2.F_SZXY_STRETCHNO=T6.F_SZXY_PLASTICNO   " +// --单层厚度判定结果
            //                         $" left join SZXY_t_LSTraceLayTenDEntry T7 on  T2.F_SZXY_STRETCHSEQ=T7.F_SZXY_STRETCHSEQ4 and T2.F_SZXY_STRETCHNO=T7.F_SZXY_PLASTICNO1   " +// --拉伸强度
            //                         $" where  T2.F_SZXY_STRETCHNO='{stretchNO}' and t2.F_SZXY_STRETCHSEQ='{fCCX}' ";

            Logger.Debug("匹配分层物理性能的SQL", SelLSsql);
            DataSet LSTraceDs = DBServiceHelper.ExecuteDataSet(Context, SelLSsql);

            if (LSTraceDs != null && LSTraceDs.Tables.Count > 0 && LSTraceDs.Tables[0].Rows.Count > 0)
            {
                this.Model.SetValue("F_SZXY_Permeability", Convert.ToInt32(LSTraceDs.Tables[0].Rows[0]["透气率"]), a);
                this.Model.SetValue("F_SZXY_Poriness1", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["孔隙率"]), a);
                this.Model.SetValue("F_SZXY_HTR1", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["穿刺强度"]), a);
                if (!Convert.ToString(LSTraceDs.Tables[0].Rows[0]["综合等级"]).IsNullOrEmptyOrWhiteSpace())
                    this.Model.SetValue("F_SZXY_ZHDJ", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["综合等级"]), a);

                this.Model.SetValue("F_SZXY_DOA", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["外观描述"]), a);
                if (!Convert.ToString(LSTraceDs.Tables[0].Rows[0]["热收缩90"]).IsNullOrEmptyOrWhiteSpace())
                    this.Model.SetValue("F_SZXY_HS901", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["热收缩90"]), a);

                if (!Convert.ToString(LSTraceDs.Tables[0].Rows[0]["热收缩105"]).IsNullOrEmptyOrWhiteSpace())
                    this.Model.SetValue("F_SZXY_HS1051", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["热收缩105"]), a);

                if (!Convert.ToString(LSTraceDs.Tables[0].Rows[0]["热收缩120"]).IsNullOrEmptyOrWhiteSpace())
                    this.Model.SetValue("F_SZXY_HS1201", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["热收缩120"]), a);


                if (!Convert.ToString(LSTraceDs.Tables[0].Rows[0]["拉伸强度"]).IsNullOrEmptyOrWhiteSpace())
                    this.Model.SetValue("F_SZXY_SR1", Convert.ToString(LSTraceDs.Tables[0].Rows[0]["拉伸强度"]), a);
            }

        }


        /// <summary>
        /// 分层取面积汇总
        /// </summary>
        /// <param name="ListData"></param>
        /// <param name="Orgid"></param>
        /// <param name="dFNo"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static void GetSummaryArea(List<dynamic> ListData, long Orgid, string dFNo, Context Context, IBillView view)
        {
            Dictionary<string, List<string>> AreaSum = new Dictionary<string, List<string>>();
            AreaSum.Clear();
            if (ListData != null)
            {
                for (int a = 0; a < ListData.Count; a++)
                {
                    PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(ListData[a]);
                    PropertyDescriptor pdFCCX = pdc.Find("FCCX", true);
                    string FCCX = pdFCCX.GetValue(ListData[a]).ToString();
                    string[] FCCXArray = FCCX.Split('-');
                    int Before = Convert.ToInt32(FCCXArray[0]);
                    int After = Convert.ToInt32(FCCXArray[1]);
                    string pjSql = " ";
                    bool flag = true;
                    Logger.Debug($"层序==={pdFCCX}", $"当前{Before}-{After}================ListData.Count:{ListData.Count}");
                    if (Before <= After)
                    {
                        Logger.Debug($"--", $"当前{Before}-{After}");
                        for (int k = Before; k < After + 1; k++)
                        {
                            flag = false;
                            Logger.Debug($"{Convert.ToString(Before)}-{Convert.ToString(After)}", $"当前i={k},Before={Before}");
                            if (k == Before)
                            {
                                pjSql = " ( " +
                                $"  cast(left(T3.F_SZXY_RollNo,case when(charindex('-', T3.F_SZXY_RollNo) - 1) < 1 then 1 else charindex('-', T3.F_SZXY_RollNo) - 1 end ) as int)<= {k} " +
                                "   and " +
                                $"  {k} <= cast(right(T3.F_SZXY_RollNo, len(T3.F_SZXY_RollNo) - charindex('-', T3.F_SZXY_RollNo)) as int) " +
                                "   )";
                            }
                            else
                            {
                                pjSql += " or ( " +
                               $"  cast(left(T3.F_SZXY_RollNo,case when(charindex('-', T3.F_SZXY_RollNo) - 1) < 1 then 1 else charindex('-', T3.F_SZXY_RollNo) - 1 end ) as int)<= {k} " +
                               "   and " +
                               $"  {k} <= cast(right(T3.F_SZXY_RollNo, len(T3.F_SZXY_RollNo) - charindex('-', T3.F_SZXY_RollNo)) as int) " +
                               "   )";
                            }
                        }

                        string SelAreaSql = "/*dialect*/select " +
                                          " sum(  " +
                                          " case when  " +
                                          " charindex('-', T3.F_SZXY_RollNo) > 0  " +
                                          "  and " +
                                          "  ( " +
                                          $" {pjSql} " +
                                          "   )   " +
                                          "  then T3.F_SZXY_AreaE " +
                                          "  when " +
                                          "    charindex('-', T3.F_SZXY_RollNo) = 0 " +
                                          "    and " +
                                          $"   cast(T3.F_SZXY_RollNo as int) >= {Before} " +
                                          "   and " +
                                          $"  cast(T3.F_SZXY_RollNo as int) <= {After} " +
                                          "   then T3.F_SZXY_AreaE " +
                                          "   else 0  end) '拉伸单汇总面积' " +
                                          "   from SZXY_t_DFD T1 Left " +
                                          "   join SZXY_t_DFDEntry T2 on T1.FID = T2.FID " +
                                          "   left  " +
                                          "   join SZXY_t_XYLSEntry T3 on T3.F_SZXY_StretchNo = T2.F_SZXY_StretchNO " +
                                          "   left " +
                                         $"   join SZXY_t_XYLS T4 on T3.FID = T4.FID where T2.F_SZXY_BIPARTITIONNO = '{dFNo}'";
                        //$" and T1.F_SZXY_OrgId={Orgid}     ";
                        Logger.Debug("汇总面积", SelAreaSql);
                        DataSet FCDS = Utils.CBData(SelAreaSql, Context);
                        decimal SumArea = 0;
                        if (FCDS != null && flag == false) SumArea = Convert.ToDecimal(FCDS.Tables[0].Rows[0]["拉伸单汇总面积"]);

 
                        view.Model.SetValue("F_SZXY_Area", SumArea, a);
                        view.Model.SetValue("F_SZXY_InArea1", SumArea, a);
                    }

                }

            }
            view.UpdateView("F_SZXY_sequence");
        }

        public static void GetSummaryArea(List<dynamic> ListData, long Orgid, Context Context, IBillView view, string LSNO, string dFNo = "")
        {
            Dictionary<string, List<string>> AreaSum = new Dictionary<string, List<string>>();
            AreaSum.Clear();
            if (ListData != null)
            {
                for (int a = 0; a < ListData.Count; a++)
                {

                    PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(ListData[a]);
                    PropertyDescriptor pdFCCX = pdc.Find("FCCX", true);
                    string FCCX = pdFCCX.GetValue(ListData[a]).ToString();
                    string[] FCCXArray = FCCX.Split('-');
                    int Before = Convert.ToInt32(FCCXArray[0]);
                    int After = Convert.ToInt32(FCCXArray[1]);
                    string pjSql = " ";
                    bool flag = true;
                    Logger.Debug($"测试层序==={pdFCCX}", $"当前{Before}-{After}================ListData.Count:{ListData.Count}");
                    if (Before <= After)
                    {
                        for (int k = Before; k < After + 1; k++)
                        {
                            flag = false;
                            Logger.Debug($"{Convert.ToString(Before)}-{Convert.ToString(After)}", $"当前i={k},Before={Before}");
                            if (k == Before)
                            {
                                pjSql = " ( " +
                                $"  cast(left(T3.F_SZXY_RollNo,case when(charindex('-', T3.F_SZXY_RollNo) - 1) < 1 then 1 else charindex('-', T3.F_SZXY_RollNo) - 1 end ) as int)<= {k} " +
                                "   and " +
                                $"  {k} <= cast(right(T3.F_SZXY_RollNo, len(T3.F_SZXY_RollNo) - charindex('-', T3.F_SZXY_RollNo)) as int) " +
                                "   )";
                            }
                            else
                            {
                                pjSql += " or ( " +
                               $"  cast(left(T3.F_SZXY_RollNo,case when(charindex('-', T3.F_SZXY_RollNo) - 1) < 1 then 1 else charindex('-', T3.F_SZXY_RollNo) - 1 end ) as int)<= {k} " +
                               "   and " +
                               $"  {k} <= cast(right(T3.F_SZXY_RollNo, len(T3.F_SZXY_RollNo) - charindex('-', T3.F_SZXY_RollNo)) as int) " +
                               "   )";
                            }
                        }

                        string SelAreaSql = "/*dialect*/select " +
                                          " sum(  " +
                                          " case when  " +
                                          " charindex('-', T3.F_SZXY_RollNo) > 0  " +
                                          "  and " +
                                          "  ( " +
                                          $" {pjSql} " +
                                          "   )   " +
                                          "  then T3.F_SZXY_AreaE " +
                                          "  when " +
                                          "    charindex('-', T3.F_SZXY_RollNo) = 0 " +
                                          "    and " +
                                          $"   cast(T3.F_SZXY_RollNo as int) >= {Before} " +
                                          "   and " +
                                          $"  cast(T3.F_SZXY_RollNo as int) <= {After} " +
                                          "   then T3.F_SZXY_AreaE " +
                                          "   else 0  end) '拉伸单汇总面积' " +
                                          "   from SZXY_t_DFD T1 Left " +
                                          "   join SZXY_t_DFDEntry T2 on T1.FID = T2.FID " +
                                          "   left  " +
                                          "   join SZXY_t_XYLSEntry T3 on T3.F_SZXY_StretchNo = T2.F_SZXY_StretchNO " +
                                          "   left " +
                                         $"   join SZXY_t_XYLS T4 on T3.FID = T4.FID where T2.F_SZXY_BIPARTITIONNO = '{dFNo}'";
                        //$" and T1.F_SZXY_OrgId={Orgid}     ";

                        Logger.Debug("汇总面积", SelAreaSql);
                        DataSet FCDS = Utils.CBData(SelAreaSql, Context);
                        decimal SumArea = 0;
                        if (FCDS != null && flag == false) SumArea = Convert.ToDecimal(FCDS.Tables[0].Rows[0]["拉伸单汇总面积"]);

                        view.Model.SetValue("F_SZXY_Area", SumArea, a);
                        view.Model.SetValue("F_SZXY_InArea1", SumArea, a);
                    }
                }

            }
            view.UpdateView("F_SZXY_sequence");
        }


        public static void GetSummaryAreaForRenZen(List<dynamic> ListData, long Orgid, string dFNo, Context Context, IBillView view)
        {
            Dictionary<string, List<string>> AreaSum = new Dictionary<string, List<string>>();
            AreaSum.Clear();
            if (ListData != null)
            {
                for (int a = 0; a < ListData.Count; a++)
                {
                    PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(ListData[a]);
                    PropertyDescriptor pdFCCX = pdc.Find("FCCX", true);
                    string FCCX = pdFCCX.GetValue(ListData[a]).ToString();
                    string[] FCCXArray = FCCX.Split('-');
                    int Before = Convert.ToInt32(FCCXArray[0]);
                    int After = Convert.ToInt32(FCCXArray[1]);

                    Logger.Debug($"测试层序==={pdFCCX}", $"当前{Before}-{After}================ListData.Count:{ListData.Count}");
                    if (Before <= After)
                    {
                        Logger.Debug($"测试", $"当前{Before}-{After}");


                        string SelAreaSql = $"/*dialect*/select    sum(T3.F_SZXY_AreaE) '拉伸单汇总面积' " +
                                          " from SZXY_t_DFD T1 Left  join SZXY_t_DFDEntry T2 on T1.FID = T2.FID " +

                                           $"  left   join SZXY_t_XYLSEntry T3 on T3.F_SZXY_StretchNo = T2.F_SZXY_StretchNO " +

                                           $"  left  join SZXY_t_XYLS T4 on T3.FID = T4.FID " +

                                           $"   where T2.F_SZXY_BIPARTITIONNO = '{dFNo}' " +
                                           $" and T3.FSeq between {Before} and {After}";
                        //$" and T1.F_SZXY_OrgId={Orgid}     ";
                        Logger.Debug("汇总面积", SelAreaSql);
                        DataSet FCDS = Utils.CBData(SelAreaSql, Context);
                        decimal SumArea = 0;
                        if (FCDS != null) SumArea = Convert.ToDecimal(FCDS.Tables[0].Rows[0]["拉伸单汇总面积"]);

                        view.Model.SetValue("F_SZXY_Area", SumArea, a);
                        view.Model.SetValue("F_SZXY_InArea1", SumArea, a);
                    }

                }

            }
            view.UpdateView("F_SZXY_sequence");
        }


     

        

    }
}

