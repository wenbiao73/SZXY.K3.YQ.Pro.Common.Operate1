using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using System.Diagnostics;
using Kingdee.BOS.Orm.Metadata.DataEntity;
 

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("包装单据操作。")]
    public class XYPack : AbstractBillPlugIn
    {
        AScanReturnInfo info = new AScanReturnInfo();

        int Curr = 0;

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            //打印按钮预览事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbPrintView"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                //是否指定标签模板
                string PJSQL = " ";

                DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_PrintTemp") as DynamicObject;
                if (PrintTemp != null)
                {
                    string PId = Convert.ToString(PrintTemp["Id"]);

                    if (!PId.IsNullOrEmptyOrWhiteSpace())
                    {
                        PJSQL = $" and T1.Fid={PId} ";
                    }
                }
                else
                {
                    return;
                }

                string F_SZXY_ForLabel = Convert.ToString(this.Model.GetValue("F_SZXY_ForLabel"));

                if (this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace())
                {

                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    //如果输入的是箱号
                    string SelSql = $"/*dialect*/select T1.F_SZXY_CUSTID,T4.FNAME  " +
                                    $"from  SZXY_t_BZDHEntry T1 " +
                                    $"left join SZXY_t_BZD T2  on t1.fid=T2.fid " +
                                    $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                                    $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                                    $"where  T1.F_SZXY_CTNNO='{F_SZXY_ForLabel}' and T2.F_SZXY_ORGID1='{orgid}' ";
                    DataSet SelSqlds = DBServiceHelper.ExecuteDataSet(this.Context, SelSql);

                    //如果输入的是生产订单号加行号
                    string SelSql1 = $"/*dialect*/select T1.F_SZXY_CTNNO 'XH',T1.F_SZXY_CUSTID,T4.FNAME from  SZXY_t_BZDHEntry T1 " +
                        $"left join SZXY_t_BZD T2  on t1.fid=T2.fid " +
                        $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                        $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                        $" where  T1.F_SZXY_PUDNO+CAST(T1.F_SZXY_PUDLINENO  as varchar(50))='{F_SZXY_ForLabel}' and T2.F_SZXY_ORGID1='{orgid}'  ";
                    //$"  group by T1.F_SZXY_CTNNO ,F_SZXY_CUSTID,T4.FNAME ";
                    DataSet SelSqlds1 = DBServiceHelper.ExecuteDataSet(this.Context, SelSql1);//T1.F_SZXY_PONO+CAST(T1.F_SZXY_POLINENO as varchar(50))

                    DataSet ds = null;

                    string InType = "";
                    if (SelSqlds.Tables[0].Rows.Count > 0)
                    {
                        InType = "XHNO";
                        Logger.Debug("输入的是箱号", SelSql);
                    }
                    if (SelSqlds1.Tables[0].Rows.Count > 0)
                    {
                        InType = "BillNo";
                        Logger.Debug("输入的是生产订单号", SelSql1);
                    }
                    if (InType == "")
                    {
                        this.View.ShowWarnningMessage("没有匹配到信息，请输入箱号或者生产订单号+行号!"); return;
                    }

                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);
                    StringBuilder STR = new StringBuilder();
                    DataSet PrintModelDS = null;
                    int ckb = 0;


                    //是否有产品型号加客户
                    orgid = Convert.ToInt64(OrgObj["Id"]);

                    string F_SZXY_CUSTID = "";
                    string FNAME = "";
                    if (InType == "XHNO" && SelSqlds.Tables[0].Rows.Count > 0)
                    {

                        for (int i = 0; i < SelSqlds.Tables[0].Rows.Count; i++)
                        {
                            F_SZXY_CUSTID = Convert.ToString(SelSqlds.Tables[0].Rows[i]["F_SZXY_CUSTID"]);
                            FNAME = Convert.ToString(SelSqlds.Tables[0].Rows[i]["FNAME"]);

                        }
                    }

                    if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                    {
                        if (InType == "XHNO")
                        {
                            PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, F_SZXY_ForLabel, "XH", ref ckb);

                            if (PrintModelDS != null)
                            {
                                XYStraddleCut.Print(PrintModelDS, 1, Context, this.View, $"'{F_SZXY_ForLabel}'", "XH");
                            }

                        }

                        //生产订单号加行号
                        if (InType == "BillNo" && SelSqlds1.Tables[0].Rows.Count > 0)
                        {
                            int j = 0;
                            if (SelSqlds1 != null && SelSqlds1.Tables.Count > 0 && SelSqlds1.Tables[0].Rows.Count > 0)
                            {
                                //循环，按客户打印
                                for (int i = 0; i < SelSqlds1.Tables[0].Rows.Count; i++)
                                {
                                    F_SZXY_CUSTID = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["F_SZXY_CUSTID"]);
                                    FNAME = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["FNAME"]);
                                    string XH = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["XH"]);

                                    if (!MacInfo.IsNullOrEmptyOrWhiteSpace() && !XH.IsNullOrEmptyOrWhiteSpace())
                                    {
                                        if (j == 0)
                                        {
                                            j++;
                                            PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, XH, "BillNo", ref ckb);

                                        }
                                        STR.Append($"'{XH}',");
                                    }
                                }
                                if (STR.ToString() != "" && !STR.ToString().IsNullOrEmptyOrWhiteSpace())
                                {
                                    string BarNoStr = STR.ToString();
                                    BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);

                                    if (PrintModelDS != null)
                                    {
                                        XYStraddleCut.Print(PrintModelDS, 1, Context, this.View, $"'{F_SZXY_ForLabel}'", "BillNo");
                                    }

                                }
                            }
                        }


                    }

                }

            }


            //补打
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {

                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);


                string F_SZXY_CTN = Convert.ToString(this.Model.GetValue("F_SZXY_CTN"));//最新箱号
                string F_SZXY_ForLabel = Convert.ToString(this.Model.GetValue("F_SZXY_ForLabel"));


                if (this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace())
                {

                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    //如果输入的是箱号
                    string SelSql = $"/*dialect*/select T1.F_SZXY_CUSTID,T4.FNAME  " +
                                    $"from  SZXY_t_BZDHEntry T1 " +
                                    $"left join SZXY_t_BZD T2  on t1.fid=T2.fid " +
                                    $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                                    $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                                    $"where  T1.F_SZXY_CTNNO='{F_SZXY_ForLabel}' and T2.F_SZXY_ORGID1='{orgid}' ";
                    DataSet SelSqlds = DBServiceHelper.ExecuteDataSet(this.Context, SelSql);

                    //如果输入的是生产订单号加行号
                    string SelSql1 = $"/*dialect*/select T1.F_SZXY_CTNNO 'XH',T1.F_SZXY_CUSTID,T4.FNAME from  SZXY_t_BZDHEntry T1 " +
                        $"left join SZXY_t_BZD T2  on t1.fid=T2.fid " +
                        $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                        $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                        $" where  T1.F_SZXY_PUDNO+CAST(T1.F_SZXY_PUDLINENO  as varchar(50))='{F_SZXY_ForLabel}' and T2.F_SZXY_ORGID1='{orgid}'  ";
                    //$"  group by T1.F_SZXY_CTNNO ,F_SZXY_CUSTID,T4.FNAME ";
                    DataSet SelSqlds1 = DBServiceHelper.ExecuteDataSet(this.Context, SelSql1);//T1.F_SZXY_PONO+CAST(T1.F_SZXY_POLINENO as varchar(50))

                    DataSet ds = null;

                    string InType = "";
                    if (SelSqlds.Tables[0].Rows.Count > 0)
                    {
                        InType = "XHNO";
                        Logger.Debug("输入的是箱号", SelSql);
                    }
                    if (SelSqlds1.Tables[0].Rows.Count > 0)
                    {
                        InType = "BillNo";
                        Logger.Debug("输入的是生产订单号", SelSql1);
                    }
                    if (InType == "")
                    {
                        this.View.ShowWarnningMessage("没有匹配到信息，请输入箱号或者生产订单号+行号!"); return;
                    }

                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);
                    StringBuilder STR = new StringBuilder();
                    DataSet PrintModelDS = null;
                    int ckb = 0;


                    //是否有产品型号加客户
                    orgid = Convert.ToInt64(OrgObj["Id"]);
                    string F_SZXY_CustIdH = "";
                    string F_SZXY_CUSTID = "";
                    string FNAME = "";
                    if (InType == "XHNO" && SelSqlds.Tables[0].Rows.Count > 0)
                    {

                        for (int i = 0; i < SelSqlds.Tables[0].Rows.Count; i++)
                        {
                            F_SZXY_CUSTID = Convert.ToString(SelSqlds.Tables[0].Rows[i]["F_SZXY_CUSTID"]);
                            FNAME = Convert.ToString(SelSqlds.Tables[0].Rows[i]["FNAME"]);
                            if (!F_SZXY_CUSTID.EqualsIgnoreCase("0") && !FNAME.IsNullOrEmptyOrWhiteSpace() && !F_SZXY_CUSTID.IsNullOrEmptyOrWhiteSpace())
                            {

                                F_SZXY_CustIdH = $" and T3.FNAME='{FNAME}' and T1.F_SZXY_CustID={F_SZXY_CUSTID}  "; break;
                            }
                        }
                    }

                    //是否指定标签模板
                    string PJSQL = " ";

                    DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_PrintTemp") as DynamicObject;
                    if (PrintTemp != null)
                    {
                        string PId = Convert.ToString(PrintTemp["Id"]);

                        if (!PId.IsNullOrEmptyOrWhiteSpace())
                        {
                            PJSQL = $" and T1.Fid={PId} ";
                        }
                    }




                    if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                    {
                        if (InType == "XHNO")
                        {
                            PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, F_SZXY_ForLabel, "XH", ref ckb);

                            if (PrintModelDS != null)
                            {
                                XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "XH");
                            }

                        }

                        //生产订单号加行号
                        if (InType == "BillNo" && SelSqlds1.Tables[0].Rows.Count > 0)
                        {
                            int j = 0;
                            if (SelSqlds1 != null && SelSqlds1.Tables.Count > 0 && SelSqlds1.Tables[0].Rows.Count > 0)
                            {
                                //循环，按客户打印
                                for (int i = 0; i < SelSqlds1.Tables[0].Rows.Count; i++)
                                {
                                    F_SZXY_CUSTID = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["F_SZXY_CUSTID"]);
                                    FNAME = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["FNAME"]);
                                    string XH = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["XH"]);

                                    if (!MacInfo.IsNullOrEmptyOrWhiteSpace() && !XH.IsNullOrEmptyOrWhiteSpace())
                                    {
                                        if (j == 0)
                                        {
                                            j++;
                                            PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, XH, "BillNo", ref ckb);

                                        }
                                        STR.Append($"'{XH}',");
                                    }
                                }
                                if (STR.ToString() != "" && !STR.ToString().IsNullOrEmptyOrWhiteSpace())
                                {
                                    string BarNoStr = STR.ToString();
                                    BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);

                                    if (PrintModelDS != null)
                                    {
                                        XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "BillNo");
                                    }

                                }
                            }
                        }
                        Logger.Debug($"客户+产品型号  箱号匹配到：{SelSqlds.Tables[0].Rows.Count}条数据。  BillNo匹配到：{SelSqlds1.Tables[0].Rows.Count}条数据", F_SZXY_CustIdH);

                    }

                    //清空已勾选复选框
                    //if (this.Model.DataObject["SZXY_BZDHEntry"] is DynamicObjectCollection entry)
                    //{
                    //    foreach (DynamicObject item in from ck in entry
                    //                                   where Convert.ToString(ck["F_SZXY_CheckBox"]).EqualsIgnoreCase("true")
                    //                                   select ck)
                    //    {
                    //        item["F_SZXY_CheckBox"] = "false";
                    //    }
                    //    View.UpdateView("F_SZXY_BZDH");
                    //    this.Model.DataObject["FFormId"] = View.BusinessInfo.GetForm().Id;
                    //    IOperationResult a = Utils.Save(View, new DynamicObject[] {  this.Model.DataObject  }, OperateOption.Create(), Context);
                    //}
                }

            }
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            int m = e.Row;

            //在XY包装单扫描分切编号时，匹配 与日计划日期 物料厚度宽度 单据日期//


            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_Barcode"))
            {

                DynamicObject billobj = this.Model.DataObject;
                string orgname = string.Empty;
                string FqNo = Convert.ToString(this.Model.GetValue("F_SZXY_Barcode", m));//获取输入的分切编号


                string NoKey = "F_SZXY_Barcode";
                string Entry = "SZXY_BZDEntry";

                //恢复 检查是否存在此条码
                XYComposite.CheckNoIsCur(this.View, this.Model.DataObject, FqNo, Entry, NoKey, e.Row, "F_SZXY_BZD", "BZ", Context);

                info.FQBH = FqNo;
                if (billobj["F_SZXY_OrgId1"] is DynamicObject orgobj && !FqNo.IsNullOrEmptyOrWhiteSpace())
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);

                    string ISCurrSql = $"select T1.F_SZXY_ISUNBOX from SZXY_t_BZDHEntry T1 Left join SZXY_t_BZD t2 on T1.Fid=T2.Fid where T2.F_SZXY_ORGID1={orgid} and T1.F_SZXY_BARCODE='{FqNo}'   ";
                    DataSet ISCurrDS = Utils.CBData(ISCurrSql, Context);

                    if (ISCurrDS.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow roe in ISCurrDS.Tables[0].Rows)
                        {

                            if (Convert.ToString(roe["F_SZXY_ISUNBOX"]) != "1")
                            {
                                this.View.ShowMessage("此条码已被使用！"); return;
                            }
                        }

                    }

                    DateTime FDate = Convert.ToDateTime(this.Model.GetValue("FDate"));
                    orgname = orgobj["Name"].ToString();
                    string sql = $"/*dialect*/select top 1 T2.F_SZXY_Material '物料',T2.F_SZXY_PLy '厚度',t2.F_SZXY_Width '宽度',t1.FDate '日期',T3.F_SZXY_YXJ '优先级'" +
                                $", T3.F_SZXY_Team '班组',T3.F_SZXY_Class '班次',T3.F_SZXY_PRInt '打印型号',T3.F_SZXY_Machinestate '包装状态',T3.F_SZXY_CJ '车间' " +
                                $", T2.F_SZXY_Len '长度',T2.F_SZXY_Area '面积',T3.F_SZXY_operator '操作员',T3.F_SZXY_SOSEQ '销售订单行号',T3.F_SZXY_SOENTRYID '销售订单行内码' " +
                                $", T3.F_SZXY_PTNo '生产订单号',T3.F_SZXY_MoLineNo1 '生产订单行号',T3.F_SZXY_CustMaterial '客户物料代码' " +
                                $", T3.F_SZXY_Text1 '客户订单号',T3.F_SZXY_Cases '总箱数',T3.F_SZXY_HCASES '第几箱',T3.F_SZXY_Volume '卷数',T3.F_SZXY_SMARK '特殊标志' " +
                                $" , T3.F_SZXY_CWType '卷芯类型',T3.F_SZXY_CustId '客户代码',T3.F_SZXY_ProductionArea '批量'" +
                                $" ,T3.FEntryID '包装日计划FEntryID', T2.FEntryID '分切单FEntryID', T3.Fid '日计划Fid'" +
                                $" ,  T3.F_SZXY_SDate '到货日期',T6.FPLANFINISHDATE '完工日期' " +
                                $" ,T2.F_SZXY_Blevel '等级',T3.F_SZXY_LEVEL 'RJH等级', T2.F_SZXY_CustBacth '客户批次号',T2.F_SZXY_SpecialMark '特殊标志' " +
                                $"  from SZXY_t_XYFQEntry T2 " +
                                $" left  join SZXY_t_XYFQ T1 on T2.Fid = T1.FID " +
                                $" left  join SZXY_t_BZRSCJHEntry T3 on  T2.F_SZXY_Material = T3.F_SZXY_Material" + // 
                                $"  left  join SZXY_t_BZRSCJH T4 on T3.Fid = T4.FID " +
                                $" left join T_PRD_MO T5 on T5.FBILLNO=T3.F_SZXY_PTNo " +
                                $" left join T_PRD_MOENTRY T6 on T6.FSeq=T3.F_SZXY_MoLineNo1 " +
                                $"  where " +
                                $"  T2.F_SZXY_PLy = T3.F_SZXY_PLy " +
                                $" and T2.F_SZXY_Width = T3.F_SZXY_Width " +
                                $"  and T4.FDOCUMENTSTATUS in  ('C') " +//datetime 120
                                $"  and (CONVERT(date,'{FDate}', 23)  between CONVERT(date, T3.F_SZXY_SDate, 23) and CONVERT(date, T3.F_SZXY_EndDate, 23)) " +
                                $" and T4.F_SZXY_OrgId = {orgid} " +
                                $" and T2.F_SZXY_BarCodeE='{FqNo}'" +
                                //$" and T3.F_SZXY_MACHINESTATE!='Y' " +
                                $" order by T3.F_SZXY_YXJ ";

                    Logger.Debug("包装单匹配SQL", sql);
                    DataSet DpDs = Utils.CBData(sql, Context);


                    if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                    {
                        IViewService Services = ServiceHelper.GetService<IViewService>();
                        for (int i = 0; i < DpDs.Tables[0].Rows.Count; i++)
                        {
                            DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_BZDEntry"] as DynamicObjectCollection;
                            //string BZState = Convert.ToString(DpDs.Tables[0].Rows[i]["包装状态"]);
                            //if (BZState.EqualsIgnoreCase("Y"))
                            if (true)
                            {
                                //this.View.ShowWarnningMessage("匹配的日计划包装状态为已装完!");
                                int index = m + i;
                                DynamicObjectCollection BZDEntry =billobj["SZXY_BZDEntry"] as DynamicObjectCollection ;
                                int currIndex = this.Model.GetEntryCurrentRowIndex("");

                                if (Convert.ToString(DpDs.Tables[0].Rows[i]["等级"]) != Convert.ToString(DpDs.Tables[0].Rows[i]["RJH等级"]))
                                {
                                    this.View.ShowMessage("分切外观等级与日计划等级不一样,不允许装一箱!"); return;

                                }
                                else
                                {
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[i]["等级"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToInt64(DpDs.Tables[0].Rows[i]["等级"]) > 0)
                                    {
                                        DynamicObject F_SZXY_Level = this.Model.GetValue("F_SZXY_Level", index) as DynamicObject;
                                        Utils.SetFZZLDataValue(Services, (DynamicObject)entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Level"), ref F_SZXY_Level, Context, Convert.ToString(DpDs.Tables[0].Rows[i]["等级"]));
                                        this.View.UpdateView("F_SZXY_Level", index);
                                    }

                                    string RMat = "";
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[i]["物料"]).IsNullOrEmptyOrWhiteSpace())
                                    {
                                        RMat = Utils.GetRootMatId(Convert.ToString(DpDs.Tables[0].Rows[i]["物料"]), orgid.ToString(), Context);
                                        FormMetadata meta = MetaDataServiceHelper.Load(Context, "BD_Material") as FormMetadata;
                                        DynamicObject F_SZXY_Material = this.Model.GetValue("F_SZXY_Material", index) as DynamicObject;
                                        Utils.SetBaseDataValue(Services, entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material"), Convert.ToInt64(RMat), ref F_SZXY_Material, Context);
                                        this.View.UpdateView("F_SZXY_Material", index);
                                    }

                                    this.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["长度"]), index);

                                    if (BZDEntry != null && BZDEntry.Count > 0&& index!=0)
                                    {
                                       // string LevelStr =Convert.ToString( entry1[index - 1]["F_SZXY_Level_Id"]);//当前行前一行的等级

                                        foreach (DynamicObject row in BZDEntry.Where(op=>!Convert.ToString( op["F_SZXY_Barcode"]).IsNullOrEmptyOrWhiteSpace()))
                                        {
                                            if (Convert.ToInt32(row["Seq"]) != index + 1 && !Convert.ToString(row["F_SZXY_Barcode"]).IsNullOrEmptyOrWhiteSpace())
                                            {
                                                string LevelStr = Convert.ToString( row["F_SZXY_Level_Id"]);
                                                if (LevelStr != Convert.ToString(DpDs.Tables[0].Rows[i]["等级"]))
                                                {
                                                    //一箱内不允许有不同长度，不同物料，不同的等级
                                                    this.View.SetEntityFocusRow("F_SZXY_BZD", index);
                                                    this.View.ShowMessage("一箱内不允许有不同的等级!");
                                 
                                                    return;
                                                }

                                                string MatIdStr = Convert.ToString(row["F_SZXY_Material_Id"]);
                                                if (MatIdStr != RMat)
                                                {
                                                    this.View.SetEntityFocusRow("F_SZXY_BZD", index);
                                                    this.View.ShowMessage("一箱内不允许有不同的物料!"); 
                                                    return;
                                                }

                                              
                                                string LenStr = Convert.ToString(row["F_SZXY"]);
                                                if ( Convert.ToDecimal( LenStr) !=Convert.ToDecimal( Convert.ToString(DpDs.Tables[0].Rows[i]["长度"])))
                                                {
                                                    this.View.SetEntityFocusRow("F_SZXY_BZD", index);
                                                    this.View.ShowMessage("一箱内不允许有不同的长度!"); 
                                                    return;
                                                }

                                            }
                                        }
                                     
                                    }
                                }

  

                                #region   给单据头赋值

                                this.Model.CreateNewEntryRow("F_SZXY_BZD");
  

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    //this.Model.SetValue("F_SZXY_Recorder", Convert.ToInt64(DpDs.Tables[0].Rows[0]["操作员"]));

                                    DynamicObject F_SZXY_Recorder = View.Model.GetValue("F_SZXY_Recorder") as DynamicObject;


                                    Utils.SetBaseDataValue(Services, billobj, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Recorder"), Convert.ToInt64(Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"])), ref F_SZXY_Recorder, Context);
                                    View.UpdateView("F_SZXY_Recorder");

                                }

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));

                                this.Model.SetValue("F_SZXY_RJHFID", Convert.ToString(DpDs.Tables[0].Rows[0]["日计划Fid"]));
                                string F_SZXY_Volume = Convert.ToString(DpDs.Tables[0].Rows[i]["卷数"]);
                                #endregion

                                #region 单据体赋值

                                this.Model.SetValue("F_SZXY_PudNo", Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单号"]), index);
                                this.Model.SetValue("F_SZXY_PudLineNo", Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单行号"]), index);
                                //this.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["长度"]), index);

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["销售订单行内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SOENTRYID", Convert.ToInt64(DpDs.Tables[0].Rows[i]["销售订单行内码"]), index);

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["销售订单行号"]).IsNullOrEmptyOrWhiteSpace())

                                {
                                    this.Model.SetValue("F_SZXY_SOSEQ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["销售订单行号"].ToString()), index);
                                }

                              

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["卷芯类型"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    // this.Model.SetValue("F_SZXY_Mandrel", Convert.ToString(DpDs.Tables[0].Rows[i]["卷芯类型"]), index);

                                    string RMat = Utils.GetRootMatId(Convert.ToString(DpDs.Tables[0].Rows[i]["卷芯类型"]), orgid.ToString(), Context);

                                    DynamicObject F_SZXY_Mandrel = this.Model.GetValue("F_SZXY_Mandrel", index) as DynamicObject;
                                    Utils.SetBaseDataValue(Services, entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Mandrel"), Convert.ToInt64(RMat), ref F_SZXY_Mandrel, Context);
                                }

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["客户代码"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    string CustId = Utils.GetRootCustId(Convert.ToString(DpDs.Tables[0].Rows[i]["客户代码"]), orgid.ToString(), Context);
                                    DynamicObject CustDo = View.Model.GetValue("F_SZXY_Custid", index) as DynamicObject;


                                    Utils.SetBaseDataValue(Services, entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Custid"), Convert.ToInt64(CustId), ref CustDo, Context);
                                    View.UpdateView("F_SZXY_Custid", index);

                                }


                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup1", Convert.ToInt32(DpDs.Tables[0].Rows[i]["班组"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes1", Convert.ToInt32(DpDs.Tables[0].Rows[i]["班次"]), index);


                              

                                //特殊标志
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_SpecialMark1", Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]), index);
                                    this.View.UpdateView("F_SZXY_SpecialMark1", index);
                                }



                                this.Model.SetValue("F_SZXY_PLy", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["厚度"]), index);
                                this.Model.SetValue("F_SZXY_Width", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["宽度"]), index);
                                this.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["长度"]), index);
                                this.Model.SetValue("F_SZXY_Area1", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["面积"]), index);


                                this.Model.SetValue("F_SZXY_PrintModel", Convert.ToString(DpDs.Tables[0].Rows[i]["打印型号"]), index);

                                this.Model.SetValue("F_SZXY_ToDate1", Convert.ToDateTime(DpDs.Tables[0].Rows[i]["到货日期"]), index);
                                this.Model.SetValue("F_SZXY_OverDate", Convert.ToDateTime(DpDs.Tables[0].Rows[i]["完工日期"]), index);
                                string operatorcode = "";

                                //操作员
                                //  string Recorder = Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]);
                                DynamicObject OperDo = View.Model.GetValue("F_SZXY_Recorder") as DynamicObject;
                                if (OperDo != null)
                                {
                                    DynamicObject operatorNew = this.Model.GetValue("F_SZXY_operator", index) as DynamicObject;
                                    Utils.SetBaseDataValue(Services, entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_operator"), Convert.ToInt64(OperDo["Id"]), ref operatorNew, Context);

                                    this.View.UpdateView("F_SZXY_operator", index);
                                }
                                //if (!Recorder.IsNullOrEmptyOrWhiteSpace())
                                //{
                                //    DynamicObject operatorDO = this.Model.GetValue("F_SZXY_operator", index) as DynamicObject;
                                //    Utils.SetFZZLDataValue(Services, (DynamicObject)entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_operator"), ref operatorDO, Context, Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]));
                                //    this.View.UpdateView("F_SZXY_operator", index);
                                //}
                                else
                                {
                                    this.View.ShowMessage("没有操作员！");
                                }

                                // 操作员代码=操作员编码后四位+'.'+性能等级编码后两位
                                if (Convert.ToInt64(DpDs.Tables[0].Rows[i]["操作员"]) > 0 && Convert.ToInt64(DpDs.Tables[0].Rows[i]["等级"]) > 0)
                                {
                                    if (this.Model.GetValue("F_SZXY_Recorder") is DynamicObject opobj && this.Model.GetValue("F_SZXY_Level", index) is DynamicObject levobj)
                                    {
                                        //this.Model.SetValue("F_SZXY_operator", Convert.ToInt64(opobj["Id"]), index);

                                        string opnumber = opobj["FStaffNumber"].ToString();//员工编号
                                        string levnumber = levobj["Number"].ToString();
                                        if (opnumber.Length >= 4 && levnumber.Length >= 0)
                                        {
                                            opnumber = opnumber.Substring(opnumber.Length - 4);

                                            operatorcode = $"{opnumber}.{levnumber}";
                                            this.Model.SetValue("F_SZXY_operator1", operatorcode, index);
                                        }
                                        else
                                        {
                                            this.Model.SetValue("F_SZXY_operator1", $"{opnumber}.{levnumber}", index);
                                        }
                                    }

                                    else
                                    {
                                        this.View.ShowMessage("生成操作员代码失败，请检查操作员和等级！");
                                    }
                                    this.View.UpdateView("F_SZXY_operator1", index);
                                }

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["客户物料代码"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_CustMaterid", Convert.ToString(DpDs.Tables[0].Rows[i]["客户物料代码"]), index);



                                }
                                this.Model.SetValue("F_SZXY_CustBacth", Convert.ToString(DpDs.Tables[0].Rows[i]["客户批次号"]), index);
                                this.Model.SetValue("F_SZXY_CustNo", Convert.ToString(DpDs.Tables[0].Rows[i]["客户订单号"]), index);

                                this.Model.SetValue("F_SZXY_BacthQty", Convert.ToString(DpDs.Tables[0].Rows[i]["批量"]), index);
                                this.Model.SetValue("F_SZXY_Date", Convert.ToDateTime(this.Model.GetValue("FDate")), index);
                                this.Model.SetValue("F_SZXY_TotalCTNS", Convert.ToString(DpDs.Tables[0].Rows[i]["总箱数"]), index);
                                this.Model.SetValue("F_SZXY_HCTNS", Convert.ToString(DpDs.Tables[0].Rows[i]["第几箱"]), index);
                                //取条码号的前10位
                                if (FqNo.Length > 10)
                                {
                                    string F_SZXY_LotNo1 = FqNo.Substring(0, 9); // or  str=str.Remove(i,str.Length-i); 
                                                                                 //this.Model.SetValue("F_SZXY_CTN", FqNo);
                                    if (!F_SZXY_LotNo1.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_LotNo", F_SZXY_LotNo1, index);
                                }

                                this.Model.SetValue("F_SZXY_FQDEntryID", Convert.ToString(DpDs.Tables[0].Rows[i]["分切单FEntryID"]), index);

                                this.Model.SetValue("F_SZXY_RJHEntryID", Convert.ToString(DpDs.Tables[0].Rows[i]["包装日计划FEntryID"]), index);
                                info.BZRJHRowId = Convert.ToString(DpDs.Tables[0].Rows[i]["包装日计划FEntryID"]);


                                this.View.UpdateView("F_SZXY_BZD");
                                this.View.SetEntityFocusRow("F_SZXY_BZD", index + 1);

                                this.View.GetControl<EntryGrid>("F_SZXY_BZD").SetEnterMoveNextColumnCell(true);
                                #endregion




                            }


                        }
                    }
                    else
                    {
                        this.View.UpdateView("F_SZXY_BZD");
                        this.View.SetEntityFocusRow("F_SZXY_BZD", 0);
                        this.View.ShowMessage("没有匹配到数据！包装单匹配日计划规则：包装日计划产品型号，宽度，厚度，物料与分切单一样。"); return;
                    }
                }

            }
            // List <string> 

            //生成最新箱号
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_RJHEntryID"))
            {
                int index = e.Row;

                if (this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj && !Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID", index)).IsNullOrEmptyOrWhiteSpace() && !info.FQBH.IsNullOrEmptyOrWhiteSpace())
                {

                    //获取日计划母卷数  
                    string orgname = OrgObj["Name"].ToString();
                    string orgid = OrgObj["Id"].ToString();
                    if (Convert.ToInt64(orgid) <= 0)
                    {
                        this.View.ShowMessage("组织为空，操作中止！"); return;
                    }
                    DynamicObject BillHead = this.Model.DataObject;
                    DynamicObjectCollection Entrys = BillHead["SZXY_BZDEntry"] as DynamicObjectCollection;
                    string F_SZXY_RJHEntryID = Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID", e.Row));

                    if (F_SZXY_RJHEntryID.IsNullOrEmptyOrWhiteSpace())
                    {

                        this.View.ShowMessage("没有匹配到对应的日计划！"); return;
                    }

                    string sql = $"/*DIALECT*/select F_SZXY_Volume  from  SZXY_t_BZRSCJHEntry  where FEntryID={F_SZXY_RJHEntryID}";
                    DataSet dsv = Utils.CBData(sql, Context);
                    int Volume = 1;

                    //默认1
                    if (dsv != null && dsv.Tables.Count > 0 && dsv.Tables[0].Rows.Count > 0)
                    {
                        Volume = Convert.ToInt32(dsv.Tables[0].Rows[0]["F_SZXY_Volume"]);
                    }


                    int count = 0;
                    foreach (DynamicObject row in Entrys.Where(em => !Convert.ToString(em["F_SZXY_RJHEntryID"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToString(em["F_SZXY_RJHEntryID"]) == F_SZXY_RJHEntryID))
                    {
                        count++;
                    }
                    if (count == Volume && count != 0)
                    {
                        // 生成编码调用打印
                        #region 生成编码

                        DateTime DateNo = Convert.ToDateTime(this.Model.GetValue("FDate"));
                        if (DateNo != DateTime.MinValue && this.Model.GetValue("F_SZXY_TeamGroup1", index) is DynamicObject TeamObejct
                            && !orgname.IsNullOrEmptyOrWhiteSpace())
                        {
                            string Formid = this.View.BusinessInfo.GetForm().Id;
                            //string SMarkName = SMarkobj["FDataValue"].ToString();
                            string BZName = TeamObejct["Name"].ToString();


                            string BZNo = GenNo(Context, Formid, OrgObj, DateNo, BZName, info.FQBH);


                            if (!BZNo.IsNullOrEmptyOrWhiteSpace())
                            {

                                this.Model.SetValue("F_SZXY_CTN", BZNo);
                                //this.View.InvokeFieldUpdateService("F_SZXY_CTN", 0);

                                //将数据写入打印缓存


                                int Rowcount = this.Model.GetEntryRowCount("F_SZXY_BZD");
                                int CurrentRowIndex = this.Model.GetEntryCurrentRowIndex("F_SZXY_BZD");
                                DynamicObjectCollection entry = this.Model.DataObject["SZXY_BZDEntry"] as DynamicObjectCollection;
                                TransferData(Context, View, entry, BZNo);

                                //this.Model.DeleteEntryData("F_SZXY_BZD");
                                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);



                                //this.View.InvokeFormOperation(FormOperationEnum.Refresh);

                                //this.View.Refresh();
                                //6.14
                                //this.Model.BatchCreateNewEntryRow("F_SZXY_BZD", CurrentRowIndex - 1);

                               //this.Model.BatchCreateNewEntryRow("F_SZXY_BZD", 2);
                                this.View.UpdateView("F_SZXY_BZD");
                                this.View.UpdateView("F_SZXY_BZDH");

                                this.View.GetControl<Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel.EntryGrid>("F_SZXY_BZD").SetEnterMoveNextColumnCell(true);

                                //调用打印
                                #region


                                string PId = "";
                                DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_PrintTemp") as DynamicObject;
                                if (PrintTemp != null)
                                {
                                    PId = Convert.ToString(PrintTemp["Id"]);
                                }

                                string PJSQL = " ";
                                if (!PId.IsNullOrEmptyOrWhiteSpace())
                                {
                                    PJSQL = $" and T1.Fid={PId} ";
                                }
                                string MacInfo = Utils.GetMacAddress();



                                Logger.Debug("当前MAC地址", MacInfo);
                                DataSet PrintModelDS = null;
                                string cust = "";
                                string Material = "";
                                int ckb = 0;
                                int flag = 0;
                                if (this.Model.DataObject["SZXY_BZDHEntry"] is DynamicObjectCollection BZDHEntry)
                                {
                                    foreach (var Row in BZDHEntry.Where(we => Convert.ToString(we["F_SZXY_CTNNO"]) == BZNo))
                                    {

                                        if (Row["F_SZXY_MATERIAL"] is DynamicObject MATERIALObj && Row["F_SZXY_Custid"] is DynamicObject CustIdObj)
                                        {
                                            if (flag == 0)
                                            {
                                                cust = Convert.ToString(CustIdObj["Id"]);
                                                Material = Convert.ToString(MATERIALObj["Name"]);

                                                //订单超额请选择模板

                                                string selISCeSql = $"select F_SZXY_Cases '总箱数',F_SZXY_HCASES '第几箱' from SZXY_t_BZRSCJHEntry where FEntryId={e.NewValue.ToString()}";
                                                DataSet selISCeSqlds = DBServiceHelper.ExecuteDataSet(Context, selISCeSql);

                                                decimal HowQty = 0;
                                                decimal CountQty = 0;
                                                if (selISCeSqlds != null && selISCeSqlds.Tables.Count > 0 && selISCeSqlds.Tables[0].Rows.Count > 0)
                                                {
                                                    string HowQtyStr = selISCeSqlds.Tables[0].Rows[0]["第几箱"].ToString();
                                                    string CountQtyStr = selISCeSqlds.Tables[0].Rows[0]["总箱数"].ToString();
                                                }

                                                if (HowQty > CountQty)
                                                {
                                                    this.View.ShowWarnningMessage("订单超额请选择模板!");

                                                    if (!PJSQL.IsNullOrEmptyOrWhiteSpace())
                                                    {
                                                        PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, cust, Material, BZNo, "XH", ref ckb);
                                                    }
                                                }
                                                else
                                                {
                                                    PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, cust, Material, BZNo, "XH", ref ckb);
                                                }


                                            }

                                            flag++;
                                        }
                                    }
                                    if (PrintModelDS == null)
                                    {
                                        this.View.UpdateView("F_SZXY_BZD");
                                        this.View.SetEntityFocusRow("F_SZXY_BZD", 0);

                                    }
                                    else
                                    {
                                        XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{BZNo}'", "XH");
                                    }

                                }


                                #endregion



                                this.View.SetEntityFocusRow("F_SZXY_BZD", 0);
                                this.View.GetControl("F_SZXY_Barcode").SetFocus();
                                this.View.UpdateView("F_SZXY_BZD");
                            }


                            #endregion
                        }
                        else
                        {
                            View.ShowMessage("生成箱号失败，请检查数据完整性！"); return;
                        }

                    }
                }
            }

            //反写第几箱
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_CTN"))
            {
                if (e.NewValue is string F_SZXY_CTN && this.Model.GetValue("F_SZXY_RJHFID") is string RJHFid)
                {
                    string BZRJHRowId = info.BZRJHRowId;
                    //获取ViewService
                    IViewService viewService = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
                    //获取元数据服务
                    IMetaDataService metadataService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();

                    //获取单据元数据
                    FormMetadata BilMetada = metadataService.Load(Context, "SZXY_BZRSCJH") as FormMetadata;

                    DynamicObject Rjhobj = viewService.LoadSingle(this.Context, Convert.ToInt64(RJHFid), BilMetada.BusinessInfo.GetDynamicObjectType());

                    if (Rjhobj["SZXY_BZRSCJHEntry"] is DynamicObjectCollection RJHEntry && !BZRJHRowId.IsNullOrEmptyOrWhiteSpace())
                    {

                        //List row= RJHEntry.Where(q=>Convert.ToString(q["id"]).EqualsIgnoreCase(BZRJHRowId)).ToList();
                        var res = from row in RJHEntry
                                  where Convert.ToString(row["id"]).EqualsIgnoreCase(BZRJHRowId)
                                  select row;

                        int F_SZXY_HCases = 0;
                        int seq = 0;
                        int F_SZXY_Cases = 0;
                        //获取第几箱  F_SZXY_Cases

                        if (res != null)
                        {
                            foreach (DynamicObject item in res)
                            {
                                F_SZXY_HCases = Convert.ToInt32(item["F_SZXY_HCases"]);//获取第几箱
                                seq = Convert.ToInt32(item["Seq"]);
                                F_SZXY_Cases = Convert.ToInt32(item["F_SZXY_Cases"]);//总箱数
                            }

                            if (F_SZXY_HCases + 1 == F_SZXY_Cases)
                            {
                                RJHEntry[seq - 1]["F_SZXY_Machinestate"] = 'Y';
                            }
                            else RJHEntry[seq - 1]["F_SZXY_Machinestate"] = 'Z';

                            RJHEntry[seq - 1]["F_SZXY_HCases"] = Convert.ToInt32(RJHEntry[seq - 1]["F_SZXY_HCases"]) + 1;
                            Rjhobj["FFormID"] = "SZXY_BZRSCJH";
                            var saveResult = BusinessDataServiceHelper.Save(Context, BilMetada.BusinessInfo, Rjhobj);
                        }


                    }
                }
            }
        }


        /// <summary>
        /// 将单据体数据插入打印缓存
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="Context"></param>
        /// <param name="View"></param>
        private void TransferData(Context Context, IBillView View, DynamicObjectCollection entry, string BZNo)
        {

            IViewService viewService = ServiceHelper.GetService<IViewService>();
            int entry2Count = View.Model.GetEntryRowCount("F_SZXY_BZDH");

            List<int> ListSeq = new List<int>() { };
            List<DynamicObject> ListRow = new List<DynamicObject>() { };


            StringBuilder STR = new StringBuilder();

            foreach (DynamicObject Row in entry.Where(em => !Convert.ToString(em["F_SZXY_RJHEntryID"]).IsNullOrEmptyOrWhiteSpace()))
            {
                ListRow.Add(Row);
                ListSeq.Add(Convert.ToInt32(Row["Seq"]));


                Entity entity = View.BillBusinessInfo.GetEntity("F_SZXY_BZDH");
                DynamicObjectCollection entityRows = View.Model.DataObject["SZXY_BZDHEntry"] as DynamicObjectCollection;

                DynamicObject newRow = new DynamicObject(entity.DynamicObjectType);




                newRow["Seq"] = entry2Count + 1;
                newRow["F_SZXY_Date"] = Convert.ToDateTime(Row["F_SZXY_Date"]);

                DynamicObject F_SZXY_Material = Row["F_SZXY_Material"] as DynamicObject;
                DynamicObject F_SZXY_Material1 = newRow["F_SZXY_Material"] as DynamicObject;

                if ((DynamicObject)Row["F_SZXY_Material"] != null)
                {
                    long MaterialId = Convert.ToInt64(F_SZXY_Material["Id"]);
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material1"), MaterialId, ref F_SZXY_Material1, Context);
                }

                newRow["F_SZXY_PLy"] = Convert.ToDecimal(Row["F_SZXY_PLy"]);
                newRow["F_SZXY_Width"] = Convert.ToDecimal(Row["F_SZXY_Width"]);
                newRow["F_SZXY"] = Convert.ToDecimal(Row["F_SZXY"]);
                newRow["F_SZXY_Area1"] = Convert.ToDecimal(Row["F_SZXY_Area1"]);

                DynamicObject F_SZXY_TeamGroup1 = Row["F_SZXY_TeamGroup1"] as DynamicObject;
                DynamicObject F_SZXY_TeamGroup11 = newRow["F_SZXY_TeamGroup1"] as DynamicObject;
                if ((DynamicObject)Row["F_SZXY_TeamGroup1"] != null)
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_TeamGroup11"), Convert.ToInt64(F_SZXY_TeamGroup1["Id"]), ref F_SZXY_TeamGroup11, Context);


                DynamicObject F_SZXY_Classes1 = Row["F_SZXY_Classes1"] as DynamicObject;
                DynamicObject F_SZXY_Classes11 = newRow["F_SZXY_Classes1"] as DynamicObject;
                if ((DynamicObject)Row["F_SZXY_Classes1"] != null)
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Classes11"), Convert.ToInt64(F_SZXY_Classes1["Id"]), ref F_SZXY_Classes11, Context);

                newRow["F_SZXY_CTNNO"] = BZNo;// Convert.ToString(View.Model.DataObject["F_SZXY_CTN"]);  
                newRow["F_SZXY_Barcode"] = Convert.ToString(Row["F_SZXY_Barcode"]);

                STR.Append($"'{ Convert.ToString(Row["F_SZXY_Barcode"])}',");

                //DynamicObject F_SZXY_operator = Row["F_SZXY_operator"] as DynamicObject;

                //DynamicObject F_SZXY_operator1 = newRow["F_SZXY_operator"] as DynamicObject;

                //if (F_SZXY_operator != null)
                //{ 
                //    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_operator2"), Convert.ToInt64(F_SZXY_operator["Id"]), ref F_SZXY_operator1, Context);
                //}

                //操作员

                DynamicObject OperDo = View.Model.GetValue("F_SZXY_Recorder") as DynamicObject;
                if (OperDo != null)
                {
                    DynamicObject operatorNew = newRow["F_SZXY_operator"] as DynamicObject;
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_operator2"), Convert.ToInt64(OperDo["Id"]), ref operatorNew, Context);

                    this.View.UpdateView("F_SZXY_operator", this.Model.GetEntryCurrentRowIndex("F_SZXY_BZDH"));
                }

                DynamicObject F_SZXY_level = Row["F_SZXY_Level"] as DynamicObject;
                DynamicObject F_SZXY_level1 = newRow["F_SZXY_Level"] as DynamicObject;

                if (F_SZXY_level != null)
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Level1"), Convert.ToInt64(F_SZXY_level["Id"]), ref F_SZXY_level1, Context);



                DynamicObject SpecialMark = Row["F_SZXY_SpecialMark1"] as DynamicObject;
                DynamicObject SpecialMark1 = newRow["F_SZXY_SpecialMark1"] as DynamicObject;
                if (SpecialMark != null)
                {

                    Utils.SetFZZLDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_SpecialMark11"), ref SpecialMark1, Context, Convert.ToString(SpecialMark["Id"]));
                }

                newRow["F_SZXY_RJHEntryIDH"] = Convert.ToString(Row["F_SZXY_RJHEntryID"]);

                info.BZRJHRowId = Convert.ToString(Row["F_SZXY_RJHEntryID"]);
                newRow["F_SZXY_FQDEntryIDH"] = Convert.ToString(Row["F_SZXY_FQDEntryID"]);

                newRow["F_SZXY_operator1"] = Convert.ToString(Row["F_SZXY_operator1"]);

                newRow["F_SZXY_PudNo"] = Convert.ToString(Row["F_SZXY_PudNo"]);
                newRow["F_SZXY_PudLineNo"] = Convert.ToString(Row["F_SZXY_PudLineNo"]);
                // newRow["F_SZXY_CustMaterid_Id"] = Convert.ToString(Row["F_SZXY_CustMaterid_Id"]);

                newRow["F_SZXY_SOENTRYID1"] = Convert.ToString(Row["F_SZXY_SOENTRYID"]);
                newRow["F_SZXY_SOSEQ1"] = Convert.ToString(Row["F_SZXY_SOSEQ"]);

                newRow["F_SZXY_remark1"] = Convert.ToString(Row["F_SZXY_remark1"]);
                newRow["F_SZXY_CustNo"] = Convert.ToString(Row["F_SZXY_CustNo"]);
                newRow["F_SZXY_PrintModel"] = Convert.ToString(Row["F_SZXY_PrintModel"]);
                newRow["F_SZXY_LotNo"] = Convert.ToString(Row["F_SZXY_LotNo"]);
                newRow["F_SZXY_CustBacth"] = Convert.ToString(Row["F_SZXY_CustBacth"]);


                //  newRow["F_SZXY_Custid"] = (DynamicObject)Row["F_SZXY_Custid"];
                DynamicObject CustidOld = Row["F_SZXY_Custid"] as DynamicObject;
                DynamicObject CustidNew = newRow["F_SZXY_Custid"] as DynamicObject;
                if (CustidOld != null)
                {
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Custid1"), Convert.ToInt64(CustidOld["Id"]), ref CustidNew, Context);

                }


                if (Row["F_SZXY_Mandrel"] is DynamicObject MandrelOld)
                {
                    DynamicObject MandrelNew = newRow["F_SZXY_Mandrel"] as DynamicObject;
                    Utils.SetBaseDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Mandrel1"), Convert.ToInt64(MandrelOld["Id"]), ref MandrelNew, Context);

                    //newRow["F_SZXY_Mandrel_Id"] = Convert.ToString( Mandrel["Id"]); F_SZXY_Mandrel1
                    //newRow["F_SZXY_Mandrel"] = Mandrel;
                }




                //是否拆箱
                newRow["F_SZXY_ISunbox"] = 0;


                newRow["F_SZXY_JQTY"] = 1;


                newRow["F_SZXY_CirculationState"] = Convert.ToString(Row["F_SZXY_CirculationState"]);
                newRow["F_SZXY_ToDate1"] = Convert.ToDateTime(Row["F_SZXY_ToDate1"]);
                newRow["F_SZXY_BacthQty"] = Convert.ToDecimal(Row["F_SZXY_BacthQty"]);
                newRow["F_SZXY_TotalCTNS"] = Convert.ToDecimal(Row["F_SZXY_TotalCTNS"]);
                newRow["F_SZXY_INArea"] = Convert.ToDecimal(Row["F_SZXY_INArea"]);
                newRow["F_SZXY_HCTNS"] = Convert.ToInt32(Row["F_SZXY_HCTNS"]);

                if (Row["F_SZXY_CustMaterid"] is DynamicObject F_SZXY_CustMaterid)
                {
                    //newRow["F_SZXY_CustMaterid_Id"] = Convert.ToString(Row["F_SZXY_CustMaterid_Id"]);
                    //newRow["F_SZXY_CustMaterid"] = F_SZXY_CustMaterid;
                    DynamicObject OldeCustDO = Row["F_SZXY_CustMaterid"] as DynamicObject;
                    DynamicObject CustMateridOld = newRow["F_SZXY_CustMaterid"] as DynamicObject;
                    Utils.SetFZZLDataValue(viewService, newRow, (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_CustMaterid1"), ref CustMateridOld, Context, Convert.ToString(OldeCustDO["Id"]));

                }

                //newRow["F_SZXY_FQDEntryID"] = Convert.ToString(Row["F_SZXY_FQDEntryID"]);
                newRow["F_SZXY_OverDate"] = Convert.ToDateTime(Row["F_SZXY_OverDate"]);
                entityRows.Add(newRow);
                entry2Count++;


                #region 设置为空
                Row["F_SZXY_Date"] = null;
                Row["F_SZXY_Material"] = null;
                Row["F_SZXY_Material_Id"] = 0;
                Row["F_SZXY_PLy"] = 0;
                Row["F_SZXY_Width"] = 0;
                Row["F_SZXY"] = 0;
                Row["F_SZXY_TeamGroup1"] = null;
                Row["F_SZXY_TeamGroup1_Id"] = 0;
                Row["F_SZXY_Classes1"] = null;
                Row["F_SZXY_Classes1_Id"] = 0;
                Row["F_SZXY_CTNNO"] = "";
                Row["F_SZXY_Barcode"] = "";
                Row["F_SZXY_operator"] = null;
                Row["F_SZXY_operator_Id"] = 0;
                Row["F_SZXY_operator1"] = "";
                Row["F_SZXY_PudNo"] = "";
                Row["F_SZXY_remark1"] = "";
                Row["F_SZXY_CustNo"] = "";
                Row["F_SZXY_PrintModel"] = "";
                Row["F_SZXY_LotNo"] = "";
                Row["F_SZXY_CustBacth"] = "";
                Row["F_SZXY_Custid"] = null;
                Row["F_SZXY_Custid_Id"] = 0;
                Row["F_SZXY_ISunbox"] = 0;
                Row["F_SZXY_SpecialMark1"] = null;
                Row["F_SZXY_SpecialMark1_Id"] = "";
                Row["F_SZXY_CirculationState"] = 0;
                Row["F_SZXY_ToDate1"] = null;
                Row["F_SZXY_BacthQty"] = 0;
                Row["F_SZXY_TotalCTNS"] = 0;
                Row["F_SZXY_OverDate"] = null;
                Row["F_SZXY_HCTNS"] = "0";
                Row["F_SZXY_RJHEntryID"] = "";
                Row["F_SZXY_Area1"] = "0";
                Row["F_SZXY_FQDEntryID"] = "";
                Row["F_SZXY_INArea"] = "0";
                Row["F_SZXY_CustMaterid"] = null;
                Row["F_SZXY_CustMaterid_Id"] = "";
                Row["F_SZXY_Mandrel"] = null;
                Row["F_SZXY_Mandrel_Id"] = "0";
                Row["F_SZXY_FQDEntryID"] = "";
                Row["F_SZXY_PUDLINENO"] = "0";
                Row["F_SZXY_Level"] = null;
                Row["F_SZXY_Level_Id"] = "0";
                Row["F_SZXY_SOSEQ"] = "0";
                Row["F_SZXY_SOENTRYID"] = "0";

                #endregion

            }


            if (this.Model.DataObject["SZXY_BZDEntry"] is DynamicObjectCollection Entry)
            {

                //List<DynamicObject> ListRow1 = new List<DynamicObject>() { };

                //int Seq = 0;
                //foreach (var item in Entry)
                //{
                //    if (Convert.ToString(item["F_SZXY_Barcode"]).IsNullOrEmptyOrWhiteSpace() || item["F_SZXY_Barcode"] == null)
                //    {
                //        ListRow1.Add(item);
                //    }

                //}

                //ListRow1.Reverse();
                //if (ListRow1 != null && ListRow1.Count > 0)
                //{
                //    foreach (var item in ListRow1)
                //    {
                //        Entry.Remove(item);
                //    }

                //    this.View.UpdateView("F_SZXY_BZD");

                //}


                //// 删除完毕，重新整理行号
                //Entity entity = this.View.BillBusinessInfo.GetEntity("F_SZXY_BZD");
                //int count = this.Model.GetEntryRowCount("F_SZXY_BZD");
                //for (int i = 1; i <= count; i++)
                //{
                //    Entry[i - 1]["Seq"] = i;
                //}
                //this.View.UpdateView("F_SZXY_BZD");
            }

            //改写分切外观单
            if (STR.ToString() != "")
            {
                string BarNoStr = STR.ToString();
                BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);
                string UpdateSql = $"/*dialect*/update   SZXY_t_FQJYDEntry set F_SZXY_Delete=1 where F_SZXY_BARCODE in ({BarNoStr})";
                DBServiceHelper.Execute(Context, UpdateSql);
            }


            this.View.SetEntityFocusRow("F_SZXY_BZD", 0);
            this.View.GetControl("F_SZXY_Barcode").SetFocus();
            this.View.UpdateView("F_SZXY_BZD");
            this.View.UpdateView("F_SZXY_BZDH");
            //this.View.Refresh();
        }


        /// <summary>
        /// 生成包装编码
        /// </summary>
        /// <param name="context"></param>
        /// <param name="FormId"></param>
        /// <param name="orgobj"></param>
        /// <param name="OrgName"></param>
        /// <param name="GXCode"></param>
        /// <param name="Date"></param>
        /// <param name="TeamName"></param>
        /// <param name="分切编号"></param>
        /// <returns></returns>
        public static string GenNo(Context context, string FormId, DynamicObject orgobj, DateTime Date, string TeamName, string FQBH)
        {
            string value = string.Empty;

            Logger.Debug("genno", $"Date:{Date},orgobj{orgobj},FormId{FormId},TeamName{TeamName},FQBH{FQBH}");
            if (Date != DateTime.MinValue && orgobj != null && !FormId.IsNullOrEmptyOrWhiteSpace() && !TeamName.IsNullOrEmptyOrWhiteSpace() && !FQBH.IsNullOrEmptyOrWhiteSpace())
            {
                string orgId = orgobj["Id"].ToString();
                string orgname = orgobj["Name"].ToString();//AT-
                FQBH = FQBH.Replace("LG-", string.Empty);
                FQBH = FQBH.Replace("AT-", string.Empty);
                string[] strArr = FQBH.Split('-');
                string A = Convert.ToString(strArr[0]);
                string GXNO = "";

                GXNO = A.Substring(A.Length - 1);
                string No1 = "";
                if (orgname.Contains("深圳"))
                {
                    No1 = "S";
                }
                else if (orgname.Contains("合肥"))
                {
                    No1 = "H";
                }
                else if (orgname.Contains("常州"))
                {
                    No1 = "C";
                }
                else if (orgname.Contains("江苏"))
                {
                    No1 = "J";
                }

                string GxCode = "";
                if (GXNO.EqualsIgnoreCase("1"))
                {
                    GxCode = "CW";

                }
                else if (GXNO.EqualsIgnoreCase("2"))
                {
                    GxCode = "CD";

                }
                else if (GXNO.EqualsIgnoreCase("3"))
                {
                    GxCode = "CL";
                }
                string DateNo = Date.ToString("yyMMdd");
                string PudDate = Date.ToString("yyyyMMdd");



                string LSH = Utils.GetLSH(context, "BZ", Convert.ToInt64(orgId), PudDate);
                if (!LSH.IsNullOrEmptyOrWhiteSpace())
                {
                    if (LSH != "")
                    {
                        Logger.Debug("LSH=========================", LSH.ToString());
                        //string StrCurrLSH = String.Format("{0:D4}", CurrLSh.ToString());
                        // value = $"{No1}{GxCode}{DateNo}{TeamName}{GXNO}-{LSH}";
                        value = $"{GxCode}{DateNo}{TeamName}{GXNO}-{LSH}";
                    }
                }



            }
            else
            {
                throw new Exception("生成编码失败，请检查录入数据的完整性!");
            }
            return value;
        }



        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);

            if (this.Model.DataObject != null && (e.Operation.FormOperation.Id.EqualsIgnoreCase("save") || e.Operation.FormOperation.Id.EqualsIgnoreCase("submit")))
            {



                if (this.Model.DataObject["SZXY_BZDEntry"] is DynamicObjectCollection Entry)
                {

                    List<DynamicObject> ListRow1 = new List<DynamicObject>() { };

                    int Seq = 0;
                    foreach (var item in Entry)
                    {
                        if (Convert.ToString(item["F_SZXY_Barcode"]).IsNullOrEmptyOrWhiteSpace() || item["F_SZXY_Barcode"] == null)
                        {
                            ListRow1.Add(item);
                        }

                    }

                    ListRow1.Reverse();
                    if (ListRow1 != null && ListRow1.Count > 0)
                    {
                        foreach (var item in ListRow1)
                        {
                            Entry.Remove(item);
                        }

                        this.View.UpdateView("F_SZXY_BZD");

                    }


                    // 删除完毕，重新整理行号
                    Entity entity = this.View.BillBusinessInfo.GetEntity("F_SZXY_BZD");
                    int count = this.Model.GetEntryRowCount("F_SZXY_BZD");
                    for (int i = 1; i <= count; i++)
                    {
                        //DynamicObject row = ListRow1[i - 1];
                        //entity.SeqDynamicProperty.SetValue(row, i);
                        Entry[i - 1]["Seq"] = i;
                    }
                    this.View.UpdateView("F_SZXY_BZD");
                }
            }
        }


        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            this.Model.BatchCreateNewEntryRow("F_SZXY_BZD", 2);
            this.View.UpdateView("F_SZXY_BZD");
        }


        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

            this.Model.BatchCreateNewEntryRow("F_SZXY_BZD", 2);
            this.View.UpdateView("F_SZXY_BZD");
            // this.View.GetControl("F_SZXY_Barcode").SetEnterNavLock(true);
            this.View.GetControl<Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel.EntryGrid>("F_SZXY_BZD").SetEnterMoveNextColumnCell(true);
            this.View.SetEntityFocusRow("F_SZXY_BZD", 0);
            this.View.GetControl("F_SZXY_Barcode").SetFocus();


        }
        public override void EntryCellFocued(EntryCellFocuedEventArgs e)
        {
            base.EntryCellFocued(e);
            if (e.EntryKey.Equals("F_SZXY_BZD"))
            {
                this.View.SetEntityFocusRow("F_SZXY_BZD", 0);
                this.View.GetControl("F_SZXY_Barcode").SetFocus();
            }


        }

        public override void AfterCreateNewEntryRow(CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);

            if (e.Entity.Key.EqualsIgnoreCase("F_SZXY_BZDH"))
            {
                // this.Model.BatchCreateNewEntryRow("F_SZXY_BZD", 2);
                EntryEntity EN = this.View.BusinessInfo.GetEntryEntity("F_SZXY_BZD");
                this.View.UpdateView("F_SZXY_BZDH");
                this.View.SetEntityFocusRow("F_SZXY_BZD", 0);
                this.View.GetControl("F_SZXY_Barcode").SetFocus();
            }
        }



        /// <summary>
        /// /删除行改写是否拆箱 0：不拆箱 1：拆箱可重新扫描
        /// </summary>
        /// <param name="e"></param>
        //public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        //    {
        //        base.BeforeDeleteRow(e);
        //        bool flag = e.EntityKey.EqualsIgnoreCase("F_SZXY_BZDH");
        //        if (flag)
        //        {
        //            string F_SZXY_CTNNO1 = Convert.ToString(this.Model.GetValue("F_SZXY_CTNNO1", e.Row));

        //            if (this.Model.DataObject["SZXY_BZDHEntry"] is DynamicObjectCollection entry && !F_SZXY_CTNNO1.IsNullOrEmptyOrWhiteSpace())
        //            {
        //                foreach (var item in entry)
        //                {
        //                    item["F_SZXY_ISunbox"] = 1;
        //                }
        //                this.View.UpdateView("F_SZXY_ISunbox");
        //                //View.SetEntityFocusRow("F_SZXY_XYFHEntity", e.Row);
        //                //View.GetControl("F_SZXY_PlasticNo").SetFocus();
        //            }
        //        }
        //    }


        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);


            //重检拆箱
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbUnPack"))
            {
                EntryEntity Entry = this.View.BusinessInfo.GetEntryEntity("F_SZXY_BZD");

                if (this.Model.DataObject["SZXY_BZDEntry"] is DynamicObjectCollection entry)
                {
                    StringBuilder STR = new StringBuilder();

                    foreach (DynamicObject row in entry.Where(op => !Convert.ToString(op["F_SZXY_CTNNO"]).IsNullOrEmptyOrWhiteSpace()))
                    {
                        string F_SZXY_CTNNO = Convert.ToString(row["F_SZXY_CTNNO"]);
                        STR.Append($"'{ F_SZXY_CTNNO}',");

                        if (this.Model.DataObject["SZXY_BZDHEntry"] is DynamicObjectCollection entryH)
                        {
                            foreach (DynamicObject rowH in entry.Where(op => !Convert.ToString(op["F_SZXY_CTNNO"]).IsNullOrEmptyOrWhiteSpace()))
                            {
                                if (rowH["F_SZXY_CTNNO"].ToString() == F_SZXY_CTNNO)
                                {
                                    rowH["F_SZXY_ISunbox"] = 1;
                                }
                            }

                            this.View.UpdateView("F_SZXY_BZDH");
                            this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                        }



                        if (STR.ToString() != "")
                        {
                            string CXXHCode = STR.ToString();//拆箱箱号
                            CXXHCode = CXXHCode.Substring(0, CXXHCode.Length - 1);

                            string UpSql = $"/*dialect*/update SZXY_t_BZDHEntry set F_SZXY_ISUNBOX = 1  where F_SZXY_CTNNO in ({CXXHCode})";

                            int res = DBServiceHelper.Execute(Context, UpSql);
                            if (res > 1)
                            {
                                this.View.ShowMessage("拆箱成功！");
                            }
                        }
                    }

                }
            }

            //剩余装箱
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbPack"))
            {

                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                DateTime DateNo = Convert.ToDateTime(this.Model.GetValue("FDate"));
                string FqNo = Convert.ToString(this.Model.GetValue("F_SZXY_Barcode", 0));//获取输入的分切编号

                int index = this.Model.GetEntryCurrentRowIndex("F_SZXY_BZD");
                if (DateNo != DateTime.MinValue && this.Model.GetValue("F_SZXY_TeamGroup1", 0) is DynamicObject TeamObejct && this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj)
                {
                    string orgid = OrgObj["id"].ToString();
                    string Formid = this.View.BusinessInfo.GetForm().Id;

                    string BZName = TeamObejct["Name"].ToString();


                    string BZNo = GenNo(Context, Formid, OrgObj, DateNo, BZName, FqNo);


                    if (!BZNo.IsNullOrEmptyOrWhiteSpace())
                    {

                        this.Model.SetValue("F_SZXY_CTN", BZNo);
                        DynamicObjectCollection entry = this.Model.DataObject["SZXY_BZDEntry"] as DynamicObjectCollection;
                        TransferData(Context, View, entry, BZNo);

                        this.Model.DeleteEntryData("F_SZXY_BZD");
                        this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                        Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);



                        //this.View.InvokeFormOperation(FormOperationEnum.Save);

                        //this.View.Refresh();

                        this.Model.BatchCreateNewEntryRow("F_SZXY_BZD", 2);
                        this.View.UpdateView("F_SZXY_BZD");
                        this.View.UpdateView("F_SZXY_BZDH");

                        this.View.GetControl<Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel.EntryGrid>("F_SZXY_BZD").SetEnterMoveNextColumnCell(true);

                        //调用打印
                        #region


                        string PId = "";
                        DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_PrintTemp") as DynamicObject;
                        if (PrintTemp != null)
                        {
                            PId = Convert.ToString(PrintTemp["Id"]);
                        }

                        string PJSQL = " ";
                        if (!PId.IsNullOrEmptyOrWhiteSpace())
                        {
                            PJSQL = $" and T1.Fid={PId} ";
                        }
                        string MacInfo = Utils.GetMacAddress();



                        Logger.Debug("当前MAC地址", MacInfo);
                        DataSet PrintModelDS = null;
                        string cust = "";
                        string Material = "";
                        int ckb = 0;
                        int flag = 0;
                        if (this.Model.DataObject["SZXY_BZDHEntry"] is DynamicObjectCollection BZDHEntry)
                        {
                            foreach (var Row in BZDHEntry.Where(we => Convert.ToString(we["F_SZXY_CTNNO"]) == BZNo))
                            {

                                if (Row["F_SZXY_MATERIAL"] is DynamicObject MATERIALObj && Row["F_SZXY_Custid"] is DynamicObject CustIdObj)
                                {
                                    if (flag == 0)
                                    {
                                        cust = Convert.ToString(CustIdObj["Id"]);
                                        Material = Convert.ToString(MATERIALObj["Name"]);

                                        PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, cust, Material, BZNo, "XH", ref ckb);
                                    }

                                    flag++;
                                }
                            }

                            if (PrintModelDS == null)
                            {
                                this.View.UpdateView("F_SZXY_BZD");
                                this.View.SetEntityFocusRow("F_SZXY_BZD", 0);

                            }
                            else
                            {
                                XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{BZNo}'", "XH");
                            }

                        }
                    }
                }

                #endregion

            }
        }

        public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            bool flag = e.EntityKey.EqualsIgnoreCase("F_SZXY_BZDH");
            if (flag)
            {
 
                if (this.Model.DataObject["SZXY_BZDHEntry"] is DynamicObjectCollection entrys )
                {
                   string Barcode= Convert.ToString(entrys[e.Row]["F_SZXY_Barcode"]);
                    if (!Barcode.IsNullOrEmptyOrWhiteSpace())
                    {
                        string UpdateSql = $"/*dialect*/update  SZXY_t_FQJYDEntry set F_SZXY_Delete=0 where F_SZXY_BARCODE in ('{Barcode}')";
                        DBServiceHelper.Execute(Context, UpdateSql);
                    }
                }
            }
        }


        public static DataSet GetNullCustPrintModel(Context Context, IBillView View, long orgid)
        {
            DataSet SelNullModelDS = null;

            string SelNullModel = "/*dialect*/select T1.FID,T1.F_SZXY_REPORT,T1.F_SZXY_PRINTMAC,T1.F_SZXY_PRINTQTY,T1.F_SZXY_LPRINT,T1.F_SZXY_CONNSTRING,T1.F_SZXY_QUERYSQL," +
                                      "T1.F_SZXY_ListSQL,T1.F_SZXY_CustID ,T1.F_SZXY_Model '产品型号',T1.F_SZXY_CHECKBOX 'CKB' from SZXY_t_BillTemplatePrint T1" +
                                      " left join   T_BD_MATERIAL T2  on T2.FMATERIALID=T1.F_SZXY_Model " +
                                      " left join   T_BD_MATERIAL_L T3 on t2.FMATERIALID=T3.FMATERIALID   where  T1.F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and T1.FUSEORGID='" + orgid + "'" +
                                      " and T1.F_SZXY_TYPESELECT='1'   and T1.FDOCUMENTSTATUS='C'   and F_SZXY_CustID='0' and F_SZXY_Model='0'  order by  T1.F_SZXY_Sel";
            SelNullModelDS = DBServiceHelper.ExecuteDataSet(Context, SelNullModel);

            return SelNullModelDS;

        }

        public static void Print(DataSet ds, int v, Context Context, IBillView View, string XH, bool IFCD = false, string INNOType = "")
        {
            string MacInfo = Utils.GetMacAddress();
            Logger.Debug("当前MAC地址", MacInfo);
            #region

            Logger.Debug("打印---", "------------BEGIN------------------");
            List<dynamic> listData = new List<dynamic>();
            listData.Clear();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {

                foreach (DataRow Row in ds.Tables[0].Rows)
                {
                    string FListSQL = Convert.ToString(Row["F_SZXY_ListSQL"]);
                    if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                    if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                    string QSQL = "";

                    if (IFCD && INNOType == "BillNo")
                    {
                        //重打BillNo
                        QSQL = $"/*dialect*/{Convert.ToString(Row[6])}F_SZXY_CTNNO1='{XH}' {FListSQL}";

                        Logger.Debug("重打+订单号拼接sql:", QSQL);
                    }
                    if (IFCD && INNOType != "BillNo")
                    {
                        QSQL = $"/*dialect*/{Convert.ToString(Row[6])}F_SZXY_CTNNO='{XH}' {FListSQL}";
                        Logger.Debug("重打+箱号拼接sql:", QSQL);
                    }

                    var ReportModel = new
                    {
                        FID = Convert.ToString(Row[0]),
                        report = Convert.ToString(Row[1]),
                        PrintAddress = Convert.ToString(Row[2]),
                        PrintQty = Convert.ToString(Row[3]),
                        //Label = Convert.ToString(Row[4]),
                        ConnString = Convert.ToString(Row[5]),

                        QuerySQL = QSQL
                    };


                    if (QSQL != "")
                    {
                        DataSet SelNullDS = DBServiceHelper.ExecuteDataSet(Context, QSQL);

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
                        if (!linkUrl.IsNullOrEmptyOrWhiteSpace())
                        {
                            View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Preview " + strJson);

                            //打印记录表
                            Utils.GenPrintReCord(View, Context, View.BillBusinessInfo.GetForm().Id.ToString(), XH);
                        }
                        // if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

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

            #endregion
        }

 
    }
}
