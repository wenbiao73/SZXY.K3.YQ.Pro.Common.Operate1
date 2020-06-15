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
using System.Threading;


namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("分切单据操作。")]
    public class XYStraddleCut : AbstractBillPlugIn
    {

        public static string LSH { get; private set; }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (e.Operation.Id.EqualsIgnoreCase("Audit"))
            {
                this.View.UpdateView("F_SZXY_XYFQEntity");
            }
            if (e.Operation.Id.EqualsIgnoreCase("UnAudit"))
            {
                this.View.UpdateView("F_SZXY_XYFQEntity");
            }
        }



        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
 
            //打印按钮事件  
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                #region
                if (this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    //是否指定标签模板
                    string PJSQL = " ";

                    DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_LabelPrint") as DynamicObject;

                    if (PrintTemp != null)
                    {
                        string PId = Convert.ToString(PrintTemp["Id"]);

                        if (!PId.IsNullOrEmptyOrWhiteSpace())
                        {
                            PJSQL = $" and T1.Fid={PId} ";
                        }
                    }


                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);
                    string F_SZXY_CustId = "";
                    string material = "";
                    string CustName = "";
          
                    DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYFQEntry"] as DynamicObjectCollection;
                    if (entry1 != null)
                    {
                        StringBuilder STR = new StringBuilder();
                        int i = 0;
                        DataSet PrintModelDS = null;
                        int ckb = 0;
                        foreach (var item in entry1.Where(m => !Convert.ToString(m["F_SZXY_BarCodeE"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToString(m["F_SZXY_CheckBox"]).EqualsIgnoreCase("true")))
                        {
                            i++;
                            string  BarNo = Convert.ToString(item["F_SZXY_BarCodeE"]);
                        
                            if (item["F_SZXY_Custid"] is DynamicObject cust)
                            {
                                Logger.Debug("界面上客户", CustName);
                                F_SZXY_CustId = Convert.ToString(cust["Id"]);
                                CustName= Convert.ToString(cust["Name"]);
                            }
                            if (item["F_SZXY_Material"] is DynamicObject Mat)
                            {
                                material = Convert.ToString(Mat["Name"]);
                            }
                           
                                if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                                {
                                if (i==1)
                                {
                                    Logger.Debug("调用匹配模板前客户为", CustName);
                                    PrintModelDS = getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CustId, material, BarNo, "BarNo",ref ckb);
                                }
                                  STR.Append($"'{ BarNo}',");
                              }
                        }
                        if (STR.ToString()!="" && !STR.ToString().IsNullOrEmptyOrWhiteSpace())
                        {
                            string BarNoStr = STR.ToString();
                            BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);
                            if (PrintModelDS!=null)
                            {
                            
                                Print(PrintModelDS,ckb,Context,this.View, BarNoStr,"BarNo");
                            }
                        
                        }

                    }

                    #endregion




                    //打印后清除已勾选的复选框
                    if (this.Model.DataObject["SZXY_XYFQEntry"] is DynamicObjectCollection entry)
                    {
                        foreach (DynamicObject item in from ck in entry
                                                       where Convert.ToString(ck["F_SZXY_CheckBox"]).EqualsIgnoreCase("true")
                                                       select ck)
                        {
                            item["F_SZXY_CheckBox"] = "false";
                        }
                        base.View.UpdateView("F_SZXY_XYFQEntity");
                        //this.Model.DataObject["FFormId"] = base.View.BusinessInfo.GetForm().Id;
                        //IOperationResult a = Utils.Save(base.View, new DynamicObject[] { this.Model.DataObject }, OperateOption.Create(), base.Context);
                    }
                }
            }


            //补打标签
             if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNoBD"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                string F_SZXY_ForLabel = Convert.ToString(this.Model.GetValue("F_SZXY_ForLabel"));//输入的补打标签
                DynamicObject OrgObj = this.Model.GetValue("F_SZXY_OrgId1") as DynamicObject;
                if (OrgObj != null && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace())
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);
                    //如果输入的是分切条码号
                    string SelSql = $" /*dialect*/select T1.F_SZXY_CUSTID,T4.FNAME ,F_SZXY_CHECKBOX '是否打印'   " +
                                    $" from  SZXY_t_XYFQEntry T1  " +
                                    $"left join SZXY_t_XYFQ T2  on t1.fid=T2.fid   " +
                                    $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                                    $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                    //$"  left join T_BD_CUSTOMER T5 on T5.FCUSTID=T1.F_SZXY_CUSTID " +
                                    //$"   left join T_BD_CUSTOMER_L t6 on t6.FCUSTID=T5.FCUSTID " +
                                    $"where  T1.F_SZXY_BARCODEE='{F_SZXY_ForLabel}' ";// and T2.F_SZXY_ORGID1='{orgid}' ";
                    DataSet SelSqlds = DBServiceHelper.ExecuteDataSet(this.Context, SelSql);


                    //如果输入的是生产订单号加行号
                    string SelSql1 = $"/*dialect*/select " +
                        $"T1.F_SZXY_BARCODEE ,T1.F_SZXY_CUSTID,T4.FNAME    " +
                        $" from  SZXY_t_XYFQEntry T1 " +
                        $"left join SZXY_t_XYFQ T2  on t1.fid=T2.fid " +
                        $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                        $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
        
                        $" where  T1.F_SZXY_PONO+CAST(T1.F_SZXY_POLINENO as varchar(50))='{F_SZXY_ForLabel}'" +
                       // $" and T2.F_SZXY_ORGID1='{orgid}'  " +
                        $" group by T1.F_SZXY_BARCODEE ,F_SZXY_CUSTID,T4.FNAME ";
                    DataSet SelSqlds1 = DBServiceHelper.ExecuteDataSet(this.Context, SelSql1);


                    string InType = "";

                    if (SelSqlds.Tables[0].Rows.Count > 0)
                    {
                        InType = "BarNo";
                        Logger.Debug("输入的是条码号", SelSql);
                    }
                    if (SelSqlds1.Tables[0].Rows.Count > 0)
                    {
                        InType = "BillNo";
                        Logger.Debug("输入的是生产订单号", SelSql1);
                    }
                    if (InType == "")
                    {
                        this.View.ShowWarnningMessage("没有匹配到信息,请输入条码号或者生产订单号+行号!"); return;
                    }

                    //是否指定标签模板
                    string PJSQL = " ";

                    DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_LabelPrint") as DynamicObject;

                    if (PrintTemp != null)
                    {
                        string PId = Convert.ToString(PrintTemp["Id"]);

                        if (!PId.IsNullOrEmptyOrWhiteSpace())
                        {
                            PJSQL = $" and T1.Fid={PId} ";
                        }
                    }
                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);

                    #region 

                    //条码号
                    orgid = Convert.ToInt64(OrgObj["Id"]);

                    StringBuilder STR = new StringBuilder();
                    DataSet PrintModelDS = null;
                    int ckb = 0;
                    if (InType == "BarNo" && SelSqlds.Tables[0].Rows.Count > 0 && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace() && orgid != 0)
                    {

                        for (int i = 0; i < SelSqlds.Tables[0].Rows.Count; i++)
                        {
                            string F_SZXY_CUSTID = Convert.ToString(SelSqlds.Tables[0].Rows[i]["F_SZXY_CUSTID"]);
                            string FNAME = Convert.ToString(SelSqlds.Tables[0].Rows[i]["FNAME"]);

                            if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                            {
                                if (i == 0)
                                {
                                    PrintModelDS = getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, F_SZXY_ForLabel, "BarNo", ref ckb);
                                }
                                STR.Append($"'{F_SZXY_ForLabel}',");
                            }
                        }

                        if (STR.ToString() != "" && !STR.ToString().IsNullOrEmptyOrWhiteSpace())
                        {
                            string BarNoStr = STR.ToString();
                            BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);
                            if (PrintModelDS != null)
                            {
                                Print(PrintModelDS, ckb, Context, this.View, BarNoStr, "BarNo");
                            }

                        }
                    }

                    //生产订单号+行号
                    if (InType == "BillNo" && SelSqlds1.Tables[0].Rows.Count > 0 && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace() && orgid != 0)
                    {
                        //string MacInfo = Utils.GetMacAddress();
                        if (SelSqlds1 != null && SelSqlds1.Tables.Count > 0 && SelSqlds1.Tables[0].Rows.Count > 0)
                        {
                            int j = 0;
                            for (int i = 0; i < SelSqlds1.Tables[0].Rows.Count; i++)
                            {
                             
                                string F_SZXY_CUSTID = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["F_SZXY_CUSTID"]);
                                string FNAME = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["FNAME"]);
                                string F_SZXY_BARCODEE = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["F_SZXY_BARCODEE"]);
                                
                                if (!MacInfo.IsNullOrEmptyOrWhiteSpace()&& !F_SZXY_BARCODEE.IsNullOrEmptyOrWhiteSpace()&& F_SZXY_BARCODEE!="")
                                {
                                    if (j == 0)
                                    {
                                        j++;
                                        PrintModelDS = getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, F_SZXY_BARCODEE, "BillNo", ref ckb);
                                    }
                                    STR.Append($"'{F_SZXY_BARCODEE}',");
                                }
                            }
                            if (STR.ToString() != "")
                            {
                                string BarNoStr = STR.ToString();
                                BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);

                                if (PrintModelDS != null)
                                {
                                    Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "BillNo");
                                }
                            }
                        }
                        Logger.Debug($"箱号匹配到：{SelSqlds.Tables[0].Rows.Count}条数据", $" 生产订单号+行号匹配到：{SelSqlds1.Tables[0].Rows.Count}条数据");

                        #endregion

                    }
                }
             }


             //重建打印
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbReCheck"))
                {

                    if (this.Model.GetValue("F_SZXY_Mac") is DynamicObject MacDo && this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject OrgObj)
                    {
                        int EntryQtyi = Convert.ToInt32(this.Model.DataObject["F_SZXY_EnterNum"]);

                        if (EntryQtyi <= 0)
                        {
                            this.View.ShowWarnningMessage("录入数量不允许小于等于0！"); return;
                        }
                        DateTime Stime = Convert.ToDateTime(this.Model.DataObject["F_SZXY_DatetimeL"]);
                        if (Stime == DateTime.MinValue)
                        {
                            Stime = DateTime.Now;
                           // this.View.Model.SetValue("F_SZXY_DatetimeL", Stime);
                        }
                        string MacName = Convert.ToString(MacDo["Name"]);
                        if (!MacName.EqualsIgnoreCase("重检"))
                        {
                            this.View.ShowWarnningMessage("仅用于机台名为重检的机台！"); return;
                        }
                        long OrgID = Convert.ToInt64(OrgObj["Id"]);

                        if (MacName.EqualsIgnoreCase("重检") && EntryQtyi > 0)
                        {
                            string SelSql = $"/*dialect*/select T1.FDOCUMENTSTATUS, T2.F_SZXY_Team '班组', T2.F_SZXY_Class '班次', T2.F_SZXY_CJ '车间'," +
                                               "  T4.FNAME '物料名称', T3.F_SZXY_LossWidth '耗损宽度', T2.F_SZXY_Material '产品型号', " +
                                               "  T2.F_SZXY_Text1 '打印型号', T2.F_SZXY_PLY '厚度', T2.F_SZXY_WIDTH '宽度', T2.F_SZXY_LEN '长度', " +
                                               "  T2.F_SZXY_AREA '面积', T2.F_SZXY_Machine '机台', T2.F_SZXY_SMark '特殊标志', T2.F_SZXY_OPERATOR '操作员', " +
                                               "  T2.F_SZXY_MOID '生产订单内码', T2.F_SZXY_PTNo '生产订单号',T2.F_SZXY_CustId '客户'," +
                                               "  T2.F_SZXY_MOLineNo '生产订单行号' from SZXY_t_FQJTRSCJH T1 " +
                                               "  Left  join SZXY_t_FQJTRSCJHEntry T2 on T1.Fid = T2.FID " +
                                               "  join T_BD_MATERIAL T3   on T3.FMATERIALID = T2.F_SZXY_Material " +
                                               "  join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                               "  left join T_ENG_EQUIPMENT T5 on T2.F_SZXY_Machine=T5.FID  " +
                                               "  left join T_ENG_EQUIPMENT_L T6 on T5.FID=T6.FID  " +
                                                $"  where T6.FNAME = '{MacName}'  and T1.F_SZXY_OrgId = {OrgID}  " +
                                                $"  and CONVERT(datetime ,'{Stime}', 120)   between CONVERT(datetime,T2.F_SZXY_SDATE, 120)  and CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                                $" and T1.FDOCUMENTSTATUS in ('C')";

                            DataSet RJHDS = DBServiceHelper.ExecuteDataSet(Context, SelSql);


                            Logger.Info("重检匹配SQL：", SelSql);

                            if (RJHDS.Tables.Count > 0 || RJHDS.Tables[0].Rows.Count > 0)
                            {
                                SetBillValueByReCheck(this.View, Context, RJHDS, EntryQtyi);

                                
                                //#region
                                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                                this.View.UpdateView();


                            #region
                            //string F_SZXY_CustId = "";
                            //string CustName = "";
                            //string material = "";
                            //string MacInfo = Utils.GetMacAddress();

                            //DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYFQEntry"] as DynamicObjectCollection;
                            //if (entry1 != null)
                            //{
                            //    StringBuilder STR = new StringBuilder();
                            //    int i = 0;
                            //    DataSet PrintModelDS = null;
                            //    int ckb = 0;
                            //    foreach (var item in entry1.Where(m => !Convert.ToString(m["F_SZXY_BarCodeE"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToString(m["F_SZXY_CheckBox"]).EqualsIgnoreCase("true")))
                            //    {
                                   
                            //        string BarNo = Convert.ToString(item["F_SZXY_BarCodeE"]);

                            //        if (item["F_SZXY_Custid"] is DynamicObject cust)
                            //        {
                            //            F_SZXY_CustId = Convert.ToString(cust[0]);
                            //            CustName = Convert.ToString(cust["Name"]);
                            //        }
                            //        if (item["F_SZXY_Material"] is DynamicObject Mat)
                            //        {
                            //            material = Convert.ToString(Mat["Name"]);
                            //        }
                                
                            //        if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                            //        {
                            //            if (i == 0)
                            //            {
                            //                i++;
                            //                PrintModelDS = getPrintModel(this.View, Context, "", OrgID.ToString(), MacInfo, F_SZXY_CustId, CustName, BarNo, "BarNo", ref ckb);
                            //            }
                            //            STR.Append($"'{ BarNo}',");
                            //        }
                            //    }
                            //    if (STR.ToString() != "")
                            //    {
                            //        string BarNoStr = STR.ToString();
                            //        BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);
                            //        if (PrintModelDS != null)
                            //        {
                            //            Print(PrintModelDS, ckb, Context, this.View, BarNoStr, "BarNo");
                            //        }
                            //    }

                            //}
                            #endregion
                        }
                        else
                            {
                                this.View.ShowWarnningMessage("请先输入机台和组织！"); return;
                            }
                        }

                    }
                }
          
        }
        


        public override void DataChanged(DataChangedEventArgs e)
        {

            base.DataChanged(e);

        

            //  扫描编码 拿分层明细里的产品型号名称，去匹配分切日计划的产品型号名称，生成条码调用打印    AT-  

            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_PudNoH") && e.OldValue.IsNullOrEmptyOrWhiteSpace() && !e.NewValue.IsNullOrEmptyOrWhiteSpace())
            {
                DynamicObject billobj = this.Model.DataObject;

                string formID = this.View.BillBusinessInfo.GetForm().Id;
                DynamicObject MacObj = billobj["F_SZXY_Mac"] as DynamicObject;
                string PudNo = Convert.ToString(this.Model.GetValue("F_SZXY_PudNoH"));
 

                if (!PudNo.IsNullOrEmptyOrWhiteSpace())
                {
                    string PudNo1 = "";
                    if (!PudNo.IsNullOrEmptyOrWhiteSpace())
                    {
                        PudNo1 = PudNo.Replace("LG-", string.Empty);
                        PudNo1 = PudNo1.Replace("AT-", string.Empty);
                    }

                    int start = 1, length = 1;
                    string GXCode = PudNo1.Substring(start, length);
                    string GXName = "";
                    string PJSQl = "";
                    switch (GXCode.ToUpper())
                    {
                        case "H":
                            GXName = "分层编号";
                            PJSQl = "select T4.FNAME '物料名称',T2.F_SZXY_Material,T2.F_SZXY_InWidth '投入宽度'," +
                                          "T2.F_SZXY_InLen '投入长度',T2.F_SZXY_InArea1 '投入面积',T2.F_SZXY_Material '物料'," +
                                          "T2.F_SZXY_ZHDJ '基膜等级',T2.F_SZXY_CastMac '流延机',T2.F_SZXY_Formula '拉伸配方' " +
                                          "from SZXY_t_XYFC T1 left " +
                                          "join SZXY_t_XYFCEntry T2 on T1.FID = T2.FID " +
                                          "join T_BD_MATERIAL T3  on T3.FMATERIALID = T2.F_SZXY_Material " +
                                          "join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                          $"where T2.F_SZXY_LayerNo = '{PudNo}' ";
                            break;
                        case "W":
                            GXName = "湿法编号";
                            PJSQl = "   select T4.FNAME '物料名称', T2.F_SZXY_Width '投入宽度'," +
                                      "T2.F_SZXY_Len '投入长度',T2.F_SZXY_Area '投入面积',T2.F_SZXY_MaterialId '物料'," +
                                      "T2.F_SZXY_ZHDJ '基膜等级','' '流延机',T2.F_SZXY_Formula '拉伸配方' " +
                                      "from SZXY_t_SFD T1 left join SZXY_t_SFDEntry T2 on T1.FID = T2.FID " +
                                      "join T_BD_MATERIAL T3  on T3.FMATERIALID=T2.F_SZXY_MaterialId  " +
                                      "join T_BD_MATERIAL_L T4 on t3.FMATERIALID=T4.FMATERIALID  " +
                                      $"where T2.F_SZXY_SFNO = '{PudNo}'  ";
                            break;
                        case "L":
                            GXName = "涂覆编号";
                            PJSQl = "    select T4.FNAME '物料名称', T2.F_SZXY_WidthL '投入宽度', " +
                                   "  T2.F_SZXY_LenL '投入长度',T2.F_SZXY_AREAL '投入面积',T2.F_SZXY_productModel '物料'," +
                                   "  T2.F_SZXY_ZHDJ '基膜等级',T2.F_SZXY_OFMac'流延机',T2.F_SZXY_Formula '拉伸配方'  " +
                                   "  from SZXY_t_XYTF T1 left join SZXY_t_XYTFEntry T2 on T1.FID = T2.FID  " +
                                   "  join T_BD_MATERIAL T3  on T3.FMATERIALID=T2.F_SZXY_productModel   " +
                                   "  join T_BD_MATERIAL_L T4 on t3.FMATERIALID=T4.FMATERIALID  " +
                                   $" where T2.F_SZXY_CoatCode = '{PudNo}'  ";
                            break;
                    }

                    if (GXName=="")
                    {
                        GXCode = PudNo1.Substring(0, length);
                        switch (GXCode.ToUpper())
                        {
                            case "H":
                                GXName = "分层编号";
                                PJSQl = "select T4.FNAME '物料名称',T2.F_SZXY_Material,T2.F_SZXY_InWidth '投入宽度'," +
                                              "T2.F_SZXY_InLen '投入长度',T2.F_SZXY_InArea1 '投入面积',T2.F_SZXY_Material '物料'," +
                                              "T2.F_SZXY_ZHDJ '基膜等级',T2.F_SZXY_CastMac '流延机',T2.F_SZXY_Formula '拉伸配方' " +
                                              "from SZXY_t_XYFC T1 left " +
                                              "join SZXY_t_XYFCEntry T2 on T1.FID = T2.FID " +
                                              "join T_BD_MATERIAL T3  on T3.FMATERIALID = T2.F_SZXY_Material " +
                                              "join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                              $"where T2.F_SZXY_LayerNo = '{PudNo}' ";
                                break;
                            case "W":
                                GXName = "湿法编号";
                                PJSQl = "   select T4.FNAME '物料名称', T2.F_SZXY_Width '投入宽度'," +
                                          "T2.F_SZXY_Len '投入长度',T2.F_SZXY_Area '投入面积',T2.F_SZXY_MaterialId '物料'," +
                                          "T2.F_SZXY_ZHDJ '基膜等级','' '流延机',T2.F_SZXY_Formula '拉伸配方' " +
                                          "from SZXY_t_SFD T1 left join SZXY_t_SFDEntry T2 on T1.FID = T2.FID " +
                                          "join T_BD_MATERIAL T3  on T3.FMATERIALID=T2.F_SZXY_MaterialId  " +
                                          "join T_BD_MATERIAL_L T4 on t3.FMATERIALID=T4.FMATERIALID  " +
                                          $"where T2.F_SZXY_SFNO = '{PudNo}'  ";
                                break;
                            case "L":
                                GXName = "涂覆编号";
                                PJSQl = "    select T4.FNAME '物料名称', T2.F_SZXY_WidthL '投入宽度', " +
                                       "  T2.F_SZXY_LenL '投入长度',T2.F_SZXY_AREAL '投入面积',T2.F_SZXY_productModel '物料'," +
                                       "  T2.F_SZXY_ZHDJ '基膜等级',T2.F_SZXY_OFMac'流延机',T2.F_SZXY_Formula '拉伸配方'  " +
                                       "  from SZXY_t_XYTF T1 left join SZXY_t_XYTFEntry T2 on T1.FID = T2.FID  " +
                                       "  join T_BD_MATERIAL T3  on T3.FMATERIALID=T2.F_SZXY_productModel   " +
                                       "  join T_BD_MATERIAL_L T4 on t3.FMATERIALID=T4.FMATERIALID  " +
                                       $" where T2.F_SZXY_CoatCode = '{PudNo}'  ";
                                break;
                        }
                    }
                    if (GXName.IsNullOrEmptyOrWhiteSpace())
                    {
                        this.View.ShowWarnningMessage("请检查输入的编号！"); return;
                    }

                    if (PudNo != "" && this.Model.GetValue("F_SZXY_OrgId1") is DynamicObject orgobj && GXName != "")
                    {
                        long orgid = Convert.ToInt64(orgobj["Id"]);
                        if (MacObj == null)
                        {
                            this.View.ShowWarnningMessage("机台为必录项！"); return;
                        }
                        string MacId = MacObj["ID"].ToString();
                        DateTime Stime = Convert.ToDateTime(billobj["F_SZXY_DatetimeL"]);
                        if (Stime == DateTime.MinValue)
                        {
                            Stime = DateTime.Now;
                            //this.View.Model.SetValue("F_SZXY_DatetimeL", Stime);
                        }


                        string RjhSql = "/*dialect*/select FLOOR(((TA.产出宽度-(TB.耗损宽度)*2))/TB.宽度) '记录条数', " +
                                          "  * from(" +
              //分层编号
             "  select T4.FNAME '物料名称', T2.F_SZXY_InWidth '投入宽度',T6.F_SZXY_StretchMac '大膜机台', T2.F_SZXY_OUTWIDTH '产出宽度',T2.F_SZXY_OUTLEN '产出长度'," +
                                     "       T2.F_SZXY_InLen '投入长度', T2.F_SZXY_InArea1 '投入面积', T2.F_SZXY_Material '物料', " +
                                    "        T2.F_SZXY_ZHDJ '基膜等级', T2.F_SZXY_CastMac '流延机', T2.F_SZXY_Formula '拉伸配方' , T2.F_SZXY_LayerNo 'GXCode' " +
                                     "       from SZXY_t_XYFC T1 " +
                                     "       left    join SZXY_t_XYFCEntry T2 on T1.FID = T2.FID " +
                                    "         join T_BD_MATERIAL T3 " +
                                   "       on T3.FMATERIALID = T2.F_SZXY_Material " +
                                     "       join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                     "  Left join  SZXY_t_XYLSEntry T5 on  T2.F_SZXY_StretchNo =T5.F_SZXY_STRETCHNO " +
                                     "  Left join SZXY_t_XYLS T6 on T5.Fid=T6.Fid" +
                                     $" where T2.F_SZXY_LayerNo = '{PudNo}' "+
                      
                        "   union " +
                //湿法编号 
                "  select T4.FNAME '物料名称', T2.F_SZXY_Width '投入宽度',T1.F_SZXY_Mac '大膜机台',T2.F_SZXY_WIDTH '产出宽度', T2.F_SZXY_LEN '产出长度'," +
                                                 "     T2.F_SZXY_Len '投入长度', T2.F_SZXY_Area '投入面积', T2.F_SZXY_MaterialId '物料', " +
                                                  "    T2.F_SZXY_ZHDJ '基膜等级', '' '流延机', T2.F_SZXY_Formula '拉伸配方',T2.F_SZXY_SFNO 'GXCode'    " +
                                                 "     from SZXY_t_SFD T1 left  " +
                                                 "     join SZXY_t_SFDEntry T2 on T1.FID = T2.FID  " +
                                                "      join T_BD_MATERIAL T3  on T3.FMATERIALID = T2.F_SZXY_MaterialId  " +
                                                  "    join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                                                 $" where T2.F_SZXY_SFNO = '{PudNo}'  "+
            
                        "  union " +
                  //涂覆编号
                  "  select T4.FNAME '物料名称', T2.F_SZXY_WidthL '投入宽度', T2.F_SZXY_TFMac '大膜机台', T2.F_SZXY_WIDTHL '产出宽度',T2.F_SZXY_LENL '产出长度'," +
                                                  "   T2.F_SZXY_LenL '投入长度', T2.F_SZXY_AREAL '投入面积', T2.F_SZXY_productModel '物料', " +
                                                  "    T2.F_SZXY_ZHDJ '基膜等级', T2.F_SZXY_OFMac'流延机', T2.F_SZXY_Formula '拉伸配方', T2.F_SZXY_CoatCode 'GXCode' " +
                                                 "     from SZXY_t_XYTF T1 left " +
                                                 "     join SZXY_t_XYTFEntry T2 on T1.FID = T2.FID " +
                                                "      join T_BD_MATERIAL T3  on T3.FMATERIALID = T2.F_SZXY_productModel " +
                                                 "     join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                                 $"  where T2.F_SZXY_CoatCode = '{PudNo}'  "+
             
                                    "    )TA " +
                                 "  join " +
                                 "  (select T1.FDOCUMENTSTATUS, T2.F_SZXY_Team '班组', T2.F_SZXY_Class '班次', T2.F_SZXY_CJ '车间'," +
                                 "   T4.FNAME '日计划物料名称', T3.F_SZXY_LossWidth '耗损宽度', T2.F_SZXY_Material '产品型号', " +
                                 "     T2.F_SZXY_Text1 '打印型号', T2.F_SZXY_PLY '厚度', T2.F_SZXY_WIDTH '宽度', T2.F_SZXY_LEN '长度', " +
                                 "    T2.F_SZXY_AREA '面积', T2.F_SZXY_Machine '机台', T2.F_SZXY_SMark '特殊标志', T2.F_SZXY_OPERATOR '操作员', " +
                                 "     T2.F_SZXY_MOID '生产订单内码', T2.F_SZXY_PTNo '生产订单号',T2.F_SZXY_CustId '客户'," +
                                 "    T2.F_SZXY_MOLineNo '生产订单行号' from SZXY_t_FQJTRSCJH T1 " +
                                 "    Left  join SZXY_t_FQJTRSCJHEntry T2 on T1.Fid = T2.FID join T_BD_MATERIAL T3 " +
                                "    on T3.FMATERIALID = T2.F_SZXY_Material " +
                                "     join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                $"  where T2.F_SZXY_Machine = '{MacId}'  and T1.F_SZXY_OrgId = {orgid}  " +
                                $"  and CONVERT(datetime ,'{Stime}', 120) " +
                                $"  between CONVERT(datetime,T2.F_SZXY_SDATE, 120)  and CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                "     and T1.FDOCUMENTSTATUS in ('C') ) TB   " +
                                $"    on TA.GXCode = '{PudNo}' ";

                        Logger.Debug("分切匹配SQL：", RjhSql);
                        DataSet RJHDS = DBServiceHelper.ExecuteDataSet(Context, RjhSql);
 
                        if (RJHDS==null||RJHDS.Tables.Count == 0 || RJHDS.Tables[0].Rows.Count == 0)
                        {
                            this.View.ShowWarnningMessage("没有找到该编码对应的信息，或者未满足以下条件，请检查" +
                                "日计划和此编码的产品型号名称相等，日期时间"); return;
                        }
                        else
                        {


                            if (!GXName.IsNullOrEmptyOrWhiteSpace())
                            {
                            

                                SetBillValue(this.View, Context, RJHDS, PudNo, GXName);
                                // this.View.UpdateView();

                                #region
                                //string F_SZXY_CustId = "";
                                //string CustName = "";
                                //string material = "";
                                //string MacInfo = Utils.GetMacAddress();

                                //DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYFQEntry"] as DynamicObjectCollection;

                                //if (entry1 != null)
                                //{
                                //    StringBuilder STR = new StringBuilder();
                                //    int i = 0;
                                //    DataSet PrintModelDS = null;
                                //    int ckb = 0;
                                //    foreach (var item in entry1.Where(m => !Convert.ToString(m["F_SZXY_BarCodeE"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToString(m["F_SZXY_CheckBox"]).EqualsIgnoreCase("true")))
                                //    {
                                //        i++;
                                //        string BarNo = Convert.ToString(item["F_SZXY_BarCodeE"]);

                                //        if (item["F_SZXY_Custid"] is DynamicObject cust)
                                //        {
                                //            F_SZXY_CustId = Convert.ToString(cust[0]);
                                //            CustName = Convert.ToString(cust["Name"]);
                                //        }
                                //        if (item["F_SZXY_Material"] is DynamicObject Mat)
                                //        {
                                //            material = Convert.ToString(Mat["Name"]);
                                //        }

                                //        if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                                //        {
                                //            if (i == 1)
                                //            {
                                //                PrintModelDS = getPrintModel(this.View, Context, "", orgid.ToString(), MacInfo, F_SZXY_CustId, CustName, BarNo, "BarNo", ref ckb);
                                //            }
                                //            STR.Append($"'{ BarNo}',");
                                //        }
                                //    }

                                //    if (STR.ToString() != ""&& !STR.ToString().IsNullOrEmptyOrWhiteSpace())
                                //    {
                                //        string BarNoStr = STR.ToString();
                                //        BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);
                                //        if (PrintModelDS != null)
                                //        {
                                //            Print(PrintModelDS, ckb, Context, this.View, BarNoStr, "BarNo");
                                //        }
                                //    }

                                //}
                                #endregion

                            }

                            View.UpdateView("F_SZXY_XYFQEntity");
                        }

                    }

                }
            }



            //报废原因
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_discardR"))
            {
                int m = e.Row;
                if (!e.NewValue.IsNullOrEmptyOrWhiteSpace()&&e.OldValue.IsNullOrEmptyOrWhiteSpace())
                {
                  decimal Area=Convert.ToDecimal(  this.Model.GetValue("F_SZXY_Area", m));
                  this.Model.SetValue("F_SZXY_discardA", Area, m);

                    this.Model.SetValue("F_SZXY_Area", Area - Area, m);
      
                    View.UpdateView("F_SZXY_Area",m);
                    View.UpdateView("F_SZXY_discardA", m);
            
                }
                else if(!e.OldValue.IsNullOrEmptyOrWhiteSpace()&&e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {
                    decimal F_SZXY_discardA = Convert.ToDecimal(this.Model.GetValue("F_SZXY_discardA", m));
                    this.Model.SetValue("F_SZXY_Area", F_SZXY_discardA, m);
                    this.Model.SetValue("F_SZXY_discardA", F_SZXY_discardA- F_SZXY_discardA, m);

                
                    View.UpdateView("F_SZXY_Area", m);
                    View.UpdateView("F_SZXY_discardA", m);
                }
               
            }

        }




        /// <summary>
        /// 单据赋值
        /// </summary>
        /// <param name="View"></param>
        /// <param name="Context"></param>
        public static void SetBillValue(IBillView View, Context Context, DataSet RJHDS, string barNo, string GXName)
        {
            string SelCurrCode = "";
            bool UseCode = false;
            //判断当前编号是否被使用  分切单据体投入面积汇总 和 分层产出面积对比 ，还有剩余就平分到分切投入面积，没有就设置为0 
            switch (GXName)
            {
                case "分层编号": //分层编号
                    SelCurrCode = $"/*diacect*/select T1.F_SZXY_LAYERNO,sum(T1.F_SZXY_INAREA1) '已投入面积',ISNULL(T2.F_SZXY_OUTAREA,0)" +
                      $" from SZXY_t_XYFQEntry  T1 " +
                      $" left join SZXY_t_XYFCEntry T2 on T1.F_SZXY_LAYERNO=T2.F_SZXY_LAYERNO " +
                      $" where T1.F_SZXY_LAYERNO ='{barNo}' " +
                      $" group by T1.F_SZXY_LAYERNO,T2.F_SZXY_OUTAREA ";
                    break;
                case "湿法编号": //湿法编号
                    SelCurrCode = $"/*diacect*/select T1.F_SZXY_LAYERNO,sum(T1.F_SZXY_INAREA1) '已投入面积',ISNULL(T2.F_SZXY_AREA,0)" +
                      $" from SZXY_t_XYFQEntry  T1 " +
                      $" left join SZXY_t_SFDEntry T2 on T1.F_SZXY_LAYERNO=T2.F_SZXY_SFNO" +
                      $" where T1.F_SZXY_LAYERNO ='{barNo}' " +
                      $" group by T1.F_SZXY_LAYERNO,T2.F_SZXY_AREA ";
                    break;
                case "涂覆编号"://涂覆编号
                    SelCurrCode = $"/*diacect*/select T1.F_SZXY_LAYERNO,sum(T1.F_SZXY_INAREA1) '已投入面积',ISNULL(T2.F_SZXY_AREAL,0)" +
                      $" from SZXY_t_XYFQEntry  T1 " +
                      $" left join SZXY_t_XYTFEntry T2 on T1.F_SZXY_LAYERNO=T2.F_SZXY_COATCODE " +
                      $" where T1.F_SZXY_LAYERNO ='{barNo}' " +
                      $" group by T1.F_SZXY_LAYERNO,T2.F_SZXY_AREAL ";
                    break;
            }
            

            Logger.Debug("检查编号在分切是否存在，查出面积SQL：", SelCurrCode);
            DataSet SelCurrCodeDS = DBServiceHelper.ExecuteDataSet(Context, SelCurrCode);


            decimal DFPArea = 0;//未分配的面积
          
            if (SelCurrCodeDS != null && SelCurrCodeDS.Tables.Count >0 &&SelCurrCodeDS.Tables[0].Rows.Count > 0)
            {
              //存在当前编号

              UseCode = true;
              decimal SumInArea=  Convert.ToDecimal(SelCurrCodeDS.Tables[0].Rows[0]["已投入面积"]);
              decimal FCOUTAREA = Convert.ToDecimal(SelCurrCodeDS.Tables[0].Rows[0][2]);//分层产出面积
                if (SumInArea<= FCOUTAREA)
                {
                    DFPArea = FCOUTAREA - SumInArea;
                }
                Logger.Debug("存在当前编号", $"编号投入面积汇总{SumInArea}，分层的产出面积{FCOUTAREA}，未分配的面积{DFPArea}");
            }
            //if (UseCode)
            //{
            //    View.GetControl("F_SZXY_PudNoH").SetFocus();
            //    View.ShowWarnningMessage("此条码已被使用过！");
            //    View.GetControl("F_SZXY_PudNoH").SetFocus();
            //}

            if (RJHDS != null && RJHDS.Tables.Count > 0 && RJHDS.Tables[0].Rows.Count > 0)
            {
                DynamicObject orgobj = View.Model.GetValue("F_SZXY_OrgId1") as DynamicObject;
                long OrgID = Convert.ToInt64(orgobj["Id"]);

                int index;
                int m = View.Model.GetEntryRowCount("F_SZXY_XYFQEntity");


                //判断产品型号名称是否一致
                if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["物料名称"]).IsNullOrEmptyOrWhiteSpace()&& !Convert.ToString(RJHDS.Tables[0].Rows[0]["日计划物料名称"]).IsNullOrEmptyOrWhiteSpace())
                {
                    string CodeMatName = Convert.ToString(RJHDS.Tables[0].Rows[0]["物料名称"]);
                    string RJHMatName = Convert.ToString(RJHDS.Tables[0].Rows[0]["日计划物料名称"]);
                    if (!CodeMatName.EqualsIgnoreCase(RJHMatName))
                    {
                        View.ShowMessage("产品编码产品型号名称和日计划产品型号名称不一致！");
                    }
                }
               


                int RecordQty = Convert.ToInt32(RJHDS.Tables[0].Rows[0]["记录条数"]);
                if (RecordQty == 0)
                {
                    View.ShowMessage("生成记录条数为0，请检查该编码所在订单的宽度，产品型号的耗损宽度，日计划的宽度！");
                    return;
                }

                View.Model.SetValue("F_SZXY_EnterNum", RecordQty);
                View.Model.BatchCreateNewEntryRow("F_SZXY_XYFQEntity", RecordQty);

                DateTime Gentime = DateTime.Now;
                for (int i = 0; i < RecordQty; i++)
                {
                    index = m + i;
                    string RMat = "";
                    if (i == 0)
                    {
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_MOID", Convert.ToInt32(RJHDS.Tables[0].Rows[0]["生产订单内码"]));

                        View.Model.SetValue("F_SZXY_MoNO", RJHDS.Tables[0].Rows[0]["生产订单号"].ToString());
                        View.Model.SetValue("F_SZXY_POLineNOH", RJHDS.Tables[0].Rows[0]["生产订单行号"].ToString());
                        View.Model.SetValue("F_SZXY_LenH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["长度"]));
                        View.Model.SetValue("F_SZXY_PlyH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["厚度"].ToString()));
                        View.Model.SetValue("F_SZXY_WidthH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["宽度"].ToString()));
                        View.Model.SetValue("F_SZXY_AreaH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["面积"].ToString()));

                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班组"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班次"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["操作员"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_OperatorH", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["操作员"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                        {
                              RMat = Utils.GetRootMatId(Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]), OrgID.ToString(), Context);
                            View.Model.SetValue("F_SZXY_MaterialId", RMat);
                        }
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(RJHDS.Tables[0].Rows[i]["车间"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_SMark", Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]), index);
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]).IsNullOrEmptyOrWhiteSpace())

                        {
                          string CustId =  Utils.GetRootCustId(Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]), OrgID.ToString(), Context);
                              View.Model.SetValue("F_SZXY_CustIdH", CustId);
                        }

                        View.Model.SetValue("F_SZXY_Model", Convert.ToString(RJHDS.Tables[0].Rows[0]["打印型号"]));
                   
                    }

                    //日计划信息 单据体赋值  大膜机台 F_SZXY_SCSJ
                    View.Model.SetValue("F_SZXY_SCSJ", Gentime, index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["大膜机台"]).IsNullOrEmptyOrWhiteSpace())

                    { 
                        View.Model.SetValue("F_SZXY_BWMac", Convert.ToString(RJHDS.Tables[0].Rows[0]["大膜机台"]), index); 
                    }

                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["机台"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        View.Model.SetValue("F_SZXY_Machine", Convert.ToString(RJHDS.Tables[0].Rows[0]["机台"]), index);
                    }

                    View.Model.SetValue("F_SZXY_PONO", RJHDS.Tables[0].Rows[0]["生产订单号"].ToString(), index);
                    View.Model.SetValue("F_SZXY_POLineNO", RJHDS.Tables[0].Rows[0]["生产订单行号"].ToString(), index);

                    View.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["长度"]), index);
                    View.Model.SetValue("F_SZXY_Ply", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["厚度"].ToString()), index);
                    View.Model.SetValue("F_SZXY_Width", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["宽度"].ToString()), index);
                    View.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["面积"].ToString()), index);

                    View.Model.SetValue("F_SZXY_Date", Convert.ToDateTime(View.Model.GetValue("FDate")), index);

                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        string CustId = Utils.GetRootCustId(Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]), OrgID.ToString(), Context);
                        View.Model.SetValue("F_SZXY_CustId", CustId, index);
                    }// View.Model.SetValue("F_SZXY_CustId", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["客户"]), index);

                    View.Model.SetValue("F_SZXY_PrintModel", Convert.ToString(RJHDS.Tables[0].Rows[0]["打印型号"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["操作员"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_operator", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["操作员"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_SpecialMark", Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_TeamGroup1", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班组"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_Classes1", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班次"]), index);
                  
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        RMat = Utils.GetRootMatId(Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]), OrgID.ToString(), Context);
                        View.Model.SetValue("F_SZXY_Material", RMat, index);
                    }

               
                    string F_SZXY_station = Convert.ToString(i + 1);

                    if (Convert.ToInt32(F_SZXY_station) < 10)
                    {
                        F_SZXY_station = F_SZXY_station.ToString().PadLeft(2, '0');
                    }
                    View.Model.SetValue("F_SZXY_station", F_SZXY_station, index);


                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["拉伸配方"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_Formula", Convert.ToString(RJHDS.Tables[0].Rows[0]["拉伸配方"]), index);

                    if (!RJHDS.Tables[0].Rows[0]["产出宽度"].IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_InWidth", Convert.ToString(RJHDS.Tables[0].Rows[0]["产出宽度"]), index);
                    if (!RJHDS.Tables[0].Rows[0]["产出长度"].IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_InLen", Convert.ToString(RJHDS.Tables[0].Rows[0]["产出长度"]), index);
                   


                    decimal inwidth = 0;
                    decimal inlen = 0;
                    decimal inarea = 0;
                    if (!RJHDS.Tables[0].Rows[0]["产出宽度"].IsNullOrEmptyOrWhiteSpace() && !RJHDS.Tables[0].Rows[0]["产出长度"].IsNullOrEmptyOrWhiteSpace())
                    {
                        inwidth = Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["产出宽度"]);
                        inlen = Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["产出长度"]);
                    }

                    if (!UseCode)
                    {
                        if (!RJHDS.Tables[0].Rows[0]["投入面积"].IsNullOrEmptyOrWhiteSpace())
                        {
                            string inareaAll = Convert.ToString(RJHDS.Tables[0].Rows[0]["投入面积"]);
                            if (i == 0)
                            {
                                View.Model.SetValue("F_SZXY_InArea1", inareaAll, index);
                            }
                            else
                            {
                                View.Model.SetValue("F_SZXY_InArea1", 0, index);
                            }

                        }
                    }
                    else
                    {
                        if (DFPArea > 0)//未分配面积>0
                        {
                            if (i == 0)
                            {
                                View.Model.SetValue("F_SZXY_InArea1", DFPArea, index);
                            }
                            else
                            {
                                View.Model.SetValue("F_SZXY_InArea1", 0, index);
                            }
                        }
                        else
                        {
                            View.Model.SetValue("F_SZXY_InArea1", 0, index);
                        }
                    }

                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["流延机"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        View.Model.SetValue("F_SZXY_CastMac", Convert.ToString(RJHDS.Tables[0].Rows[0]["流延机"]), index);
                    }

                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["基膜等级"]).IsNullOrEmptyOrWhiteSpace())
                    { 
                        View.Model.SetValue("F_SZXY_YMDJ", Convert.ToString(RJHDS.Tables[0].Rows[0]["基膜等级"]), index); 
                    }

                    View.Model.SetValue("F_SZXY_LayerNo", Convert.ToString(View.Model.GetValue("F_SZXY_PudNoH")), index);

                    if (!RJHDS.Tables[0].Rows[0]["物料"].IsNullOrEmptyOrWhiteSpace())
                    {
                        RMat = Utils.GetRootMatId(Convert.ToString(RJHDS.Tables[0].Rows[0]["物料"]), OrgID.ToString(), Context);
                        View.Model.SetValue("F_SZXY_FCMATERIALID", RMat, index);

                    }

                    View.Model.SetValue("F_SZXY_barcodeold", barNo, index);

                    //设置客户批次号 取产品编号内的机台+日期  产品类型
                    string SelSql = "";
                    string GX = "";

                    string PudType = "";
                    if (View.Model.GetValue("F_SZXY_Material", index) is DynamicObject Material1)
                    {
                        if (Material1["F_SZXY_Assistant"] is DynamicObject PudTypeObj)
                        {
                            PudType = Convert.ToString(PudTypeObj["Number"]);
                        }
                    }

                    if (PudType.EqualsIgnoreCase("GF"))
                    {
                        GX = "2";
                        SelSql = " select T2.F_SZXY_STRETCHMAC '机台', T2.FDate '时间'  " +
                                    "  from SZXY_t_XYLSEntry T1 left  " +
                                    "  join SZXY_t_XYLS T2 on T1.FID = T2.FID  " +
                                    $"  where T1.F_SZXY_StretchNo+T1.F_SZXY_ROLLNO ='{barNo}'  ";


                    }
                    else if (PudType.EqualsIgnoreCase("SF"))
                    {
                        GX = "1";
                        SelSql = "select T1.F_SZXY_Mac '机台', T1.FDATE '时间' " +
                                   " from SZXY_t_SFD T1 left " +
                                    "join SZXY_t_SFDEntry T2 on T1.FID = T2.FID " +
                                   $" where T2.F_SZXY_SFNO ='{barNo}' ";



                    }
                    else if (PudType.Contains("TF"))
                    {
                        GX = "3";
                        SelSql = "select T1.F_SZXY_Mac '机台', T1.FDATE '时间' " +
                                  " from SZXY_t_XYTF T1 left " +
                                   "join SZXY_t_XYTFEntry T2 on T1.FID = T2.FID " +
                                   $" where T2.F_SZXY_CoatCode ='{barNo}' ";


                    }
                    else if (PudType == "")
                    {
                        View.ShowWarnningMessage("请检查产品型号的产品类型！");
                        return;
                    }




                    DynamicObject CustDo= View.Model.GetValue("F_SZXY_Custid", index) as DynamicObject;

                    string FDescription = "";

                    if (CustDo!=null)
                    {
                        if (!Convert.ToString(CustDo["Description"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            FDescription = Convert.ToString(CustDo["Description"]);
                        }
                    }
       

                    //备注存在批次号 设置批次号
                    if (FDescription.EqualsIgnoreCase("批次号"))
                    {

                        string ZbarNo = "";
                        string DMJD = "";
                        if (!barNo.IsNullOrEmptyOrWhiteSpace()&& barNo.Length>4)
                        {
                            ZbarNo = barNo.Replace("LG-", string.Empty);
                            ZbarNo = ZbarNo.Replace("AT-", string.Empty);

                            int start = 0, length = 1;
                             DMJD = ZbarNo.Substring(start, length);//大膜基地
                        }

                

                        #region
                 


                        string OrgName = orgobj["Name"].ToString();

                        string FQJD = ""; //分切基地
                        if (OrgName.Contains("深圳"))
                        {
                            FQJD = "S";
                        }
                        else if (OrgName.Contains("合肥"))
                        {
                            FQJD = "H";
                        }
                        else if (OrgName.Contains("常州"))
                        {
                            FQJD = "C";
                        }
                        else if (OrgName.Contains("江苏"))
                        {
                            FQJD = "J";
                        }
                      

                        DataSet SelDS = null;
                        if (SelSql != "")
                        {
                            SelDS = Utils.CBData(SelSql, Context);
                        }

                        if (RJHDS != null && RJHDS.Tables.Count > 0 && RJHDS.Tables[0].Rows.Count > 0)
                        {
                            string LsMacId = Convert.ToString(SelDS.Tables[0].Rows[0]["机台"]);
                            string date = Convert.ToDateTime(SelDS.Tables[0].Rows[0]["时间"]).ToString("yyMMdd");

                            if (LsMacId != "" && Convert.ToDateTime(SelDS.Tables[0].Rows[0]["时间"]) != DateTime.MinValue)
                            {
                                FormMetadata meta = MetaDataServiceHelper.Load(Context, "ENG_Equipment") as FormMetadata;
                                DynamicObject LSMacobj = BusinessDataServiceHelper.LoadSingle(Context, Convert.ToInt32(LsMacId), meta.BusinessInfo.GetDynamicObjectType());

                                string MacName = "";
                                if (LSMacobj != null)
                                {
                                    MacName = LSMacobj["Name"].ToString();
                                }

                                if (!MacName.IsNullOrEmptyOrWhiteSpace()&& !DMJD.IsNullOrEmptyOrWhiteSpace() && !FQJD.IsNullOrEmptyOrWhiteSpace() )
                                {
                                    MacName = System.Text.RegularExpressions.Regex.Replace(MacName, @"[^0-9]+", "");

                                    //大膜基地，分切基地，大膜日期，大膜机台
                                    //客户引用备注 ：批次号
                                    string Bacthno = $"{DMJD}{FQJD}{date}{MacName}";

                                    View.Model.SetValue("F_SZXY_CustBacth", Bacthno, index);
                                }
                            }
                        }

                        #endregion
                    }


                    //生成分切编号 
                    DynamicObject Teamobj = View.Model.GetValue("F_SZXY_TeamGroup1", index) as DynamicObject;
                    //FormMetadata meta = MetaDataServiceHelper.Load(Context, "BD_Material") as FormMetadata;
                    if (View.Model.GetValue("F_SZXY_Material") is DynamicObject Materialobj  && Teamobj != null)
                    {
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
                        else if (OrgName.Contains("江苏"))
                        {
                            No1 = "J";
                        }
                        DynamicObject Material = View.Model.GetValue("F_SZXY_Material", index) as DynamicObject;
                        string formID = View.BillBusinessInfo.GetForm().Id;
                        DynamicObject MacObj = View.Model.DataObject["F_SZXY_Mac"] as DynamicObject;
                        string PudDate = Convert.ToDateTime(View.Model.GetValue("FDate")).ToString("yyyyMMdd");

                        string data = Convert.ToDateTime(View.Model.GetValue("FDate")).ToString("yyMMdd");
                        string Team = Convert.ToString(Teamobj["Name"]);
                        string Materialno = Convert.ToString(Material["Number"]);
                        if (No1 == "" || orgobj == null || MacObj == null || formID == "" || Materialno == "" || Team.IsNullOrEmptyOrWhiteSpace() || GX == "") 
                            throw new Exception("生成编码失败，请检查录入数据的完整性!");

                        //if (i==0)
                        //{
                        //    SqlParam sqlParam1 = new SqlParam("@GX", KDDbType.String, "FQ");
                        //    SqlParam sqlParam2 = new SqlParam("@OrgId", KDDbType.String, "FQ");
                        //    SqlParam sqlParam3 = new SqlParam("@Date", KDDbType.String, "FQ");
                        //    sqlParam1.Direction = ParameterDirection.Output;
                        //    List<SqlParam> listParam = new List<SqlParam>();
                        //    listParam.Add(sqlParam1);
                        //    LSHDS= DBUtils.ExecuteDataSet(Context, CommandType.StoredProcedure, "/*dialect*/proc_JHSHD", listParam);
                        //}

                        if (i == 0)
                        {
                            LSH = Utils.GetLSH(Context, "FQ", OrgID, PudDate, RecordQty); //取出max值
                            Logger.Debug("取出max值", $"{LSH}");
                        }
                  

                        if (!LSH.IsNullOrEmptyOrWhiteSpace())
                        {
                            long LSMax = Convert.ToInt64(LSH);
                            long initialLS = LSMax - Convert.ToInt64(RecordQty);
                            long CurrLSh = 0;
                            CurrLSh = initialLS + i + 1;


                            if (LSH != "")
                            {

                                string StrCurrLSH = String.Format("{0:D4}", CurrLSh);


                                string F_SZXY_BarCode = $"S{data}{Team}{GX}-{StrCurrLSH}";
                                View.Model.SetValue("F_SZXY_BarCodeE", F_SZXY_BarCode, index);
                                View.Model.SetValue("F_SZXY_BarCode", F_SZXY_BarCode);
                                Logger.Debug("LSH=========================", $"{StrCurrLSH}");
                            }
                        }


                    }
                }


                //View.UpdateView("F_SZXY_PudNoH");
                // 调用保存
                View.Model.DataObject["FFormId"] = View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { View.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                View.UpdateView("F_SZXY_XYFQEntity");

            }
            View.Model.SetValue("F_SZXY_PudNoH", string.Empty, 0);
            View.Model.DataObject["F_SZXY_PudNoH"] = "";
            //View.GetControl("F_SZXY_PudNoH").SetFocus();
            if (UseCode)
            {
                View.ShowMessage("此条码已被使用过！");
                View.GetControl("F_SZXY_PudNoH").SetFocus();
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.Model.DataObject["F_SZXY_PudNoH"] = "";
            this.View.UpdateView("F_SZXY_PudNoH");
            View.GetControl("F_SZXY_PudNoH").SetFocus();
        }


       
        /// <summary>
        /// 重检按钮单据赋值
        /// </summary>
        /// <param name="view"></param>
        /// <param name="context"></param>
        /// <param name="RJHDS"></param>
        /// <param name="EntryQty"></param>
        private void SetBillValueByReCheck(IBillView view, Context context, DataSet RJHDS,int EntryQty)
        {
            if (RJHDS != null && RJHDS.Tables.Count > 0 && RJHDS.Tables[0].Rows.Count > 0)
            {
                DynamicObject orgobj = View.Model.GetValue("F_SZXY_OrgId1") as DynamicObject;
                long OrgID = Convert.ToInt64(orgobj["Id"]);
                int index;

                int m = View.Model.GetEntryRowCount("F_SZXY_XYFQEntity");
                View.Model.BatchCreateNewEntryRow("F_SZXY_XYFQEntity", EntryQty);

                DateTime Gentime = DateTime.Now;
                for (int i = 0; i < EntryQty; i++)
                {
                    index = m + i;
                    string RMat = "";
                    if (i == 0)
                    {
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_MOID", Convert.ToInt32(RJHDS.Tables[0].Rows[0]["生产订单内码"]));
                        View.Model.SetValue("F_SZXY_MoNO", RJHDS.Tables[0].Rows[0]["生产订单号"].ToString());
                        View.Model.SetValue("F_SZXY_POLineNOH", RJHDS.Tables[0].Rows[0]["生产订单行号"].ToString());
                        View.Model.SetValue("F_SZXY_LenH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["长度"]));
                        View.Model.SetValue("F_SZXY_PlyH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["厚度"].ToString()));
                        View.Model.SetValue("F_SZXY_WidthH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["宽度"].ToString()));
                        View.Model.SetValue("F_SZXY_AreaH", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["面积"].ToString()));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班组"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班次"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["操作员"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_OperatorH", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["操作员"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            RMat = Utils.GetRootMatId(Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]), OrgID.ToString(), Context);
                            View.Model.SetValue("F_SZXY_MaterialId", RMat);
                        } 
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(RJHDS.Tables[0].Rows[i]["车间"]));
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_SMark", Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]), index);
                        if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            string CustId = Utils.GetRootCustId(Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]), OrgID.ToString(), Context);
                            View.Model.SetValue("F_SZXY_CustIdH", CustId);
                        } 
                        View.Model.SetValue("F_SZXY_Model", Convert.ToString(RJHDS.Tables[0].Rows[0]["打印型号"]));

                    }

                    //日计划信息 单据体赋值  大膜机台 F_SZXY_SCSJ
                    View.Model.SetValue("F_SZXY_SCSJ", Gentime, index);
 
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["机台"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_Machine", Convert.ToString(RJHDS.Tables[0].Rows[0]["机台"]), index);
                    View.Model.SetValue("F_SZXY_PONO", RJHDS.Tables[0].Rows[0]["生产订单号"].ToString(), index);
                    View.Model.SetValue("F_SZXY_POLineNO", RJHDS.Tables[0].Rows[0]["生产订单行号"].ToString(), index);
                    View.Model.SetValue("F_SZXY_Len", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["长度"]), index);
                    View.Model.SetValue("F_SZXY_Ply", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["厚度"].ToString()), index);
                    View.Model.SetValue("F_SZXY_Width", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["宽度"].ToString()), index);
                    View.Model.SetValue("F_SZXY_Area", Convert.ToDecimal(RJHDS.Tables[0].Rows[0]["面积"].ToString()), index);

                    View.Model.SetValue("F_SZXY_Date", Convert.ToDateTime(View.Model.GetValue("FDate")), index);

                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        string CustId = Utils.GetRootCustId(Convert.ToString(RJHDS.Tables[0].Rows[0]["客户"]), OrgID.ToString(), Context);
                        View.Model.SetValue("F_SZXY_CustId", CustId, index);
                    } 

                    View.Model.SetValue("F_SZXY_PrintModel", Convert.ToString(RJHDS.Tables[0].Rows[0]["打印型号"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["操作员"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_operator", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["操作员"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_SpecialMark", Convert.ToString(RJHDS.Tables[0].Rows[0]["特殊标志"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班组"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_TeamGroup1", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班组"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["班次"]).IsNullOrEmptyOrWhiteSpace()) View.Model.SetValue("F_SZXY_Classes1", Convert.ToInt64(RJHDS.Tables[0].Rows[0]["班次"]), index);
                    if (!Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        string RMat1 = Utils.GetRootMatId(Convert.ToString(RJHDS.Tables[0].Rows[0]["产品型号"]), OrgID.ToString(), Context);
                        View.Model.SetValue("F_SZXY_Material", RMat1, index);
                    } 
                    string F_SZXY_station = Convert.ToString(i + 1);

                    if (Convert.ToInt32(F_SZXY_station) < 10)
                    {
                        F_SZXY_station = F_SZXY_station.ToString().PadLeft(2, '0');
                    }
                    View.Model.SetValue("F_SZXY_station", F_SZXY_station, index);
 
                    //设置客户批次号 取产品编号内的机台+日期  产品类型

                    //生成分切编号 
                    DynamicObject Teamobj = View.Model.GetValue("F_SZXY_TeamGroup1", index) as DynamicObject;
                    //FormMetadata meta = MetaDataServiceHelper.Load(Context, "BD_Material") as FormMetadata;
                    if (View.Model.GetValue("F_SZXY_Material") is DynamicObject Materialobj  && Teamobj != null)
                    {
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
                        else if (OrgName.Contains("江苏"))
                        {
                            No1 = "J";

                        }
                        DynamicObject Material = View.Model.GetValue("F_SZXY_Material", index) as DynamicObject;
                        string formID = View.BillBusinessInfo.GetForm().Id;
                        DynamicObject MacObj = View.Model.DataObject["F_SZXY_Mac"] as DynamicObject;
                        string PudDate = Convert.ToDateTime(View.Model.GetValue("FDate")).ToString("yyyyMMdd");
                        
                        string data = Convert.ToDateTime(View.Model.GetValue("FDate")).ToString("yyMMdd");
                        string Team = Convert.ToString(Teamobj["Name"]);
                        if (Material!=null)
                        {

                        string Materialno = Convert.ToString(Material["Number"]);
                        if (No1 == "" || orgobj == null || MacObj == null || formID == "" || Materialno == ""   || Team.IsNullOrEmptyOrWhiteSpace() ) throw new Exception("生成编码失败，请检查录入数据的完整性!");
  
                        string GX = "";

                        string PudType = "";
                        if (View.Model.GetValue("F_SZXY_Material", index) is DynamicObject Material1)
                        {
                            if (Material1["F_SZXY_Assistant"] is DynamicObject PudTypeObj)
                            {
                                PudType = Convert.ToString(PudTypeObj["Number"]);
                            }
                        }
                        // string PudType = Convert.ToString(RJHDS.Tables[0].Rows[0]["产品类型"]);
                        if (PudType.EqualsIgnoreCase("GF"))
                        {
                            GX = "2";
                        
                        }
                        else if (PudType.EqualsIgnoreCase("SF"))
                        {
                            GX = "1";
                         }
                        else if (PudType.Contains("TF"))
                        {
                            GX = "3";
                        }
                        else if (PudType == "")
                        {
                            View.ShowMessage("请检查产品型号的产品类型！");
                            return;
                        }
                            if (i == 0)
                            {
                                LSH = Utils.GetLSH(Context, "FQ", OrgID, PudDate, EntryQty); //取出max值
                                Logger.Debug("取出max值", $"{LSH}");
                            }

                            if (!LSH.IsNullOrEmptyOrWhiteSpace())
                            {
                                long LSMax = Convert.ToInt64(LSH);
                                long initialLS = LSMax - Convert.ToInt64(EntryQty);
                                long CurrLSh = 0;
                                CurrLSh = initialLS + i + 1;
                                if (LSH != "")
                                {
                                    string StrCurrLSH = String.Format("{0:D4}", CurrLSh);
                                   // string F_SZXY_BarCode = $"{No1}S{data}{Team}{GX}-{StrCurrLSH}";

                                    string F_SZXY_BarCode = $"S{data}{Team}{GX}-{StrCurrLSH}";
                                    View.Model.SetValue("F_SZXY_BarCodeE", F_SZXY_BarCode, index);
                                    View.Model.SetValue("F_SZXY_BarCode", F_SZXY_BarCode);
                                    Logger.Debug("LSH=========================", $"{StrCurrLSH}");
                                }
                            }

                        }
                    }

                }
            }

        }

        public static void FQTYPrint(IBillView View, Context Context, long orgid, string MacInfo, string F_SZXY_CustIdH, string PJSQL, DynamicObject billobj)
        {
          
            //指定MAC打印
            string SQL12 = "/*dialect*/select T1.FID,T1.F_SZXY_REPORT,T1.F_SZXY_PRINTMAC,T1.F_SZXY_PRINTQTY,T1.F_SZXY_LPRINT,T1.F_SZXY_CONNSTRING," +
                    "T1.F_SZXY_QUERYSQL,T1.F_SZXY_ListSQL,T1.F_SZXY_CustID ,T1.F_SZXY_Model '产品型号' ,F_SZXY_CHECKBOX 'CKB'  " +
                    " from SZXY_t_BillTemplatePrint T1" +
                    " left join   T_BD_MATERIAL T2  on T2.FMATERIALID=T1.F_SZXY_Model " +
                    " left join   T_BD_MATERIAL_L T3 on t2.FMATERIALID=T3.FMATERIALID  " +
                    " where  T1.F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' " +
                    "and T1.FUSEORGID='" + orgid + "' and T1.F_SZXY_TYPESELECT='1'   and T1.FDOCUMENTSTATUS='C'  " +
                    "and T1.F_SZXY_Remark='"+MacInfo+"' " + F_SZXY_CustIdH + "  " + PJSQL + "   order by T1.F_SZXY_Sel";
            DataSet ds12 = DBServiceHelper.ExecuteDataSet(Context, SQL12);
            if (ds12 != null && ds12.Tables.Count > 0 && ds12.Tables[0].Rows.Count > 0)
            {
                int V = 0;
                if (!Convert.ToString(ds12.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                {
                    V = Convert.ToInt32(ds12.Tables[0].Rows[0]["CKB"]);
                }
                XYCast.Print(ds12, V, Context, View, MacInfo,Convert.ToString(billobj[0]));
            }
            else
            {
                //无MAC打印
                string SQL = "/*dialect*/select T1.FID,T1.F_SZXY_REPORT,T1.F_SZXY_PRINTMAC,T1.F_SZXY_PRINTQTY,T1.F_SZXY_LPRINT,T1.F_SZXY_CONNSTRING," +
                    "T1.F_SZXY_QUERYSQL,T1.F_SZXY_ListSQL,T1.F_SZXY_CustID ,T1.F_SZXY_Model '产品型号' ,F_SZXY_CHECKBOX 'CKB'  " +
                    " from SZXY_t_BillTemplatePrint T1" +
                    " left join   T_BD_MATERIAL T2  on T2.FMATERIALID=T1.F_SZXY_Model " +
                    " left join   T_BD_MATERIAL_L T3 on t2.FMATERIALID=T3.FMATERIALID   where  T1.F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' " +
                    "and T1.FUSEORGID='" + orgid + "' and T1.F_SZXY_TYPESELECT='1'   and T1.FDOCUMENTSTATUS='C'  and T1.F_SZXY_Remark='' " + F_SZXY_CustIdH + "  " + PJSQL + "   order by T1.F_SZXY_Sel";
                DataSet ds = DBServiceHelper.ExecuteDataSet(Context, SQL);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    int V = 0;
                    if (!Convert.ToString(ds.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        V = Convert.ToInt32(ds.Tables[0].Rows[0]["CKB"]);
                    }
                    XYCast.Print(ds, V, Context, View, MacInfo, Convert.ToString(billobj[0]));
                }
                else

                {
                    Logger.Debug("没有找到匹配模板，打通用模板", "");
                    //不预览直接打印
                    DataSet SelNullModelDS = XYPack.GetNullCustPrintModel(Context, View, orgid);

                    if (SelNullModelDS != null && SelNullModelDS.Tables.Count > 0 && SelNullModelDS.Tables[0].Rows.Count > 0)
                    {
                        int V = 0;
                        if (!Convert.ToString(SelNullModelDS.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            V = Convert.ToInt32(SelNullModelDS.Tables[0].Rows[0]["CKB"]);
                        }
                        XYCast.Print(SelNullModelDS, V, Context, View, MacInfo, Convert.ToString(billobj[0]));
                    }
                    else
                    {
                        View.ShowMessage("没有找到匹配的模板！");
                        return;
                    }
                }
            }
        }



      
        /// <summary>
        /// 匹配套打模板
        /// </summary>
        /// <param name="View"></param>
        /// <param name="Context"></param>
        /// <param name="ZDPrintModel">指定标签模板</param>
        /// <param name="orgid"></param>
        /// <param name="MacInfo"></param>
        /// <param name="F_SZXY_CUSTID">客户</param>
        /// <param name="material">物料名</param>
        /// <param name="F_SZXY_ForLabel">条码或者生产订单号+行号的值</param>
        /// <param name="InType">条码或者生产订单号+行号的类型</param>
        /// <param name="V">预览或打印</param>
        /// <returns></returns>
        public static DataSet  getPrintModel(IBillView View, Context Context, string ZDPrintModel, string orgid, string MacInfo, string F_SZXY_CUSTID, string material, string F_SZXY_ForLabel, string InType,ref int V)
        {
            DataSet RESDS = null;
            Logger.Debug("调用匹配模板前客户为", F_SZXY_CUSTID);


            if (!F_SZXY_CUSTID.IsNullOrEmptyOrWhiteSpace())
            {
                string SelCust = $"/*dialect*/select T6.FNAME '客户名' from T_BD_CUSTOMER T5    left join T_BD_CUSTOMER_L t6 on t6.FCUSTID=T5.FCUSTID where t6.FCUSTID='{F_SZXY_CUSTID}'";
                DataSet SelCustDS = DBServiceHelper.ExecuteDataSet(Context, SelCust);
                if (SelCustDS != null && SelCustDS.Tables.Count > 0 && SelCustDS.Tables[0].Rows.Count > 0)
                {
                    Logger.Debug($"客户:{Convert.ToString(SelCustDS.Tables[0].Rows[0][0])}", $"产品：{material}");
                }
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

            if (ZDPrintModel!=""&& !ZDPrintModel.IsNullOrEmptyOrWhiteSpace())
            {
                DS = DBServiceHelper.ExecuteDataSet(Context, SQL12);
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
            }
            else 
            {
                
                if (!material.IsNullOrEmptyOrWhiteSpace())
                {

                    if (F_SZXY_CUSTID.IsNullOrEmptyOrWhiteSpace())
                    {
                        F_SZXY_CUSTID = "0";
                    }
                    //如果不为空 物料+客户
                    WhereSql = $" {SQL12}  and T1.F_SZXY_CustID={F_SZXY_CUSTID} and  T3.FNAME='{material}' and F_SZXY_Remark='{MacInfo}'";
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
                        //匹配  物料+客户
                        WhereSql = $" {SQL12}  and T1.F_SZXY_CustID={F_SZXY_CUSTID} and  T3.FNAME  is null  and F_SZXY_Remark='{MacInfo}'  ";
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
                            WhereSql = $" {SQL12}  and T1.F_SZXY_CustID=0 and  T3.FNAME is null  and F_SZXY_Remark='{MacInfo}'  ";
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
                                WhereSql = $" {SQL12}  and T1.F_SZXY_CustID={F_SZXY_CUSTID} and  T3.FNAME='{material}' and F_SZXY_Remark='' ";
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
                                    WhereSql = $" {SQL12}  and T1.F_SZXY_CustID={F_SZXY_CUSTID} and  T3.FNAME  is null and F_SZXY_Remark='' ";
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
                                        WhereSql = $" {SQL12}  and T1.F_SZXY_CustID=0 and  T3.FNAME  is null and F_SZXY_Remark='' ";
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
                        }


                    }
                }
            }
          
            Logger.Debug("匹配单据套打模板编码Sql", WhereSql);
            if (RESDS!=null&& RESDS.Tables.Count>0&& RESDS.Tables[0].Rows.Count>0)
            {
                string BQ =Convert.ToString( RESDS.Tables[0].Rows[0]["标签"]);
                Logger.Debug("匹配单据套打模板编码", BQ);
            }
            return RESDS;
            #endregion
        }





        public static void Print(DataSet DS, int v, Context Context, IBillView View, string XH, string INNOType = "")
        {
            string formid= View.BillBusinessInfo.GetForm().Id.ToString();
            Logger.Debug("打印---", "------------BEGIN------------------");
            
            Logger.Debug("---", $"------------打印条码或生产订单号为：{XH}------------------");
            List<dynamic> listData = new List<dynamic>();

            listData.Clear();

            if (DS != null && DS.Tables.Count > 0&& DS.Tables[0].Rows.Count>0)
              {
          
                foreach (DataRow Row in DS.Tables[0].Rows)
                {
                    string FListSQL = Convert.ToString(Row["F_SZXY_ListSQL"]);
                    if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                    if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                    string QSQL = "";
                    if (INNOType == "BillNo")
                    {

                        QSQL = $"{Convert.ToString(Row[6])} DYBQDD in ({XH}) {FListSQL}";

                        Logger.Debug("订单号拼接sql:", QSQL);
                    }
                    else
                    {
                  
                        QSQL = $"{Convert.ToString(Row[6])} DYBQ in ({XH})  {FListSQL}";
                        Logger.Debug("条码号拼接sql:", QSQL);
                    }

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
                if (listData.Count>0&& listData!=null)
                {
                      strJson = Newtonsoft.Json.JsonConvert.SerializeObject(listData);
                }

                if (strJson!="")
                {
                    //调用打印
                    string SQL = "/*dialect*/select F_SZXY_EXTERNALCONADS from SZXY_t_ClientExtern ";
                    DataSet ids = DBServiceHelper.ExecuteDataSet(Context, SQL);

                    if (ids != null && ids.Tables.Count > 0 && ids.Tables[0].Rows.Count > 0)
                    {
                        string linkUrl = Convert.ToString(ids.Tables[0].Rows[0][0]).Replace("\\", "\\\\");// @"C:\Users\Administrator\Desktop\Grid++Report 6old\Grid++Report 6\Samples\CSharp\8.PrintInForm\bin\Debug\PrintReport.exe";
                    if (v == 0)
                    {
                        if (!strJson.IsNullOrEmptyOrWhiteSpace())
                        {
                            View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

                            //打印记录表
                            Utils.GenPrintReCord(View, Context, formid, XH);
                           
                        }
                        else
                        {
                            View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                        }
                    }
                    else
                    {
                        if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Preview " + strJson);
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

        }
 
    }
}
