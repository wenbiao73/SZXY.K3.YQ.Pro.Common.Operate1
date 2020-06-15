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
    [Description("客户回签单单据操作。")]
    public class CustBack : AbstractBillPlugIn
    {


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            //客户回签签单打印  传Fid ,不需要自动打印，加一个按钮打印
            //补打标签
      
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNoBD"))
            {

                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                string F_SZXY_ForLabel = Convert.ToString(this.Model.GetValue("F_SZXY_ForLabel"));//输入的补打标签

                DynamicObject OrgObj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;

                if (OrgObj != null && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace())
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    //如果输入的是分切条码号
                    string SelSql = $" /*dialect*/select T1.F_SZXY_CBNO1,T1.F_SZXY_CUST,T4.FNAME   " +
                                    $" from  SZXY_t_KHHQDEntry T1  " +
                                    $"left join SZXY_t_KHHQD T2  on t1.fid=T2.fid   " +
                                    $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                                    $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID " +
                                    $"where  T1.F_SZXY_CBNO1='{F_SZXY_ForLabel}' "; 
                    DataSet SelSqlds = DBServiceHelper.ExecuteDataSet(this.Context, SelSql);


                    //如果输入的是生产订单号加行号
                    string SelSql1 = $"/*dialect*/select " +
                        $"T1.F_SZXY_CBNO1 ,T1.F_SZXY_CUST,T4.FNAME    " +
                        $" from  SZXY_t_KHHQDEntry T1 " +
                        $"left join SZXY_t_KHHQD T2  on t1.fid=T2.fid " +
                        $"left join T_BD_MATERIAL T3   on T3.FMATERIALID = T1.F_SZXY_MATERIAL  " +
                        $"left join T_BD_MATERIAL_L T4 on t3.FMATERIALID = T4.FMATERIALID  " +
                        $" where  T1.F_SZXY_MONOE+CAST(T1.F_SZXY_MOLINENOE as varchar(50))='{F_SZXY_ForLabel}' " +
                   
                        $" group by T1.F_SZXY_CBNO1 ,F_SZXY_CUST,T4.FNAME ";

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
                            string F_SZXY_CUSTID = Convert.ToString(SelSqlds.Tables[0].Rows[i]["F_SZXY_CUST"]);
                            string FNAME = Convert.ToString(SelSqlds.Tables[0].Rows[i]["FNAME"]);

                            if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                            {
                                if (i == 0)
                                {
                                    PrintModelDS =XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, F_SZXY_ForLabel, "BarNo", ref ckb);
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
                                XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, BarNoStr, "BarNo");
                            }

                        }
                    }

                    //生产订单号+行号
                    if (InType == "BillNo" && SelSqlds1.Tables[0].Rows.Count > 0 && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace() && orgid > 0)
                    {
                 
                        if (SelSqlds1 != null && SelSqlds1.Tables.Count > 0 && SelSqlds1.Tables[0].Rows.Count > 0)
                        {
                            int j = 0;
                            for (int i = 0; i < SelSqlds1.Tables[0].Rows.Count; i++)
                            {

                                string F_SZXY_CUSTID = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["F_SZXY_CUST"]);
                                string FNAME = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["FNAME"]);
                                string F_SZXY_BARCODEE = Convert.ToString(SelSqlds1.Tables[0].Rows[i]["F_SZXY_CBNO1"]);

                                if (!MacInfo.IsNullOrEmptyOrWhiteSpace() && !F_SZXY_BARCODEE.IsNullOrEmptyOrWhiteSpace() && F_SZXY_BARCODEE != "")
                                {
                                    if (j == 0)
                                    {
                                        j++;
                                        PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_CUSTID, FNAME, F_SZXY_BARCODEE, "BillNo", ref ckb);
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
                                    XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "BillNo");
                                }
                            }

                        }
                        Logger.Debug($"箱号匹配到：{SelSqlds.Tables[0].Rows.Count}条数据", $" 生产订单号+行号匹配到：{SelSqlds1.Tables[0].Rows.Count}条数据");

                        #endregion

                    }
                }
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

       

            int m = this.Model.GetEntryRowCount("F_SZXY_KHHQDEntity");
            int mm = 2;
            this.Model.BatchCreateNewEntryRow("F_SZXY_KHHQDEntity", mm);
            this.View.GetControl<EntryGrid>("F_SZXY_KHHQDEntity").SetEnterMoveNextColumnCell(true);
            this.View.UpdateView("F_SZXY_KHHQDEntity");

        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            int m = e.Row; string WhereSql = "";
            DynamicObject billobj = this.Model.DataObject;
            string InNO = Convert.ToString(this.Model.GetValue("F_SZXY_BoxNo", m));//获取输入的编号
            string MONO = Convert.ToString(this.Model.GetValue("F_SZXY_MONO", m));//获取输入的任务单行号
            string XQty = Convert.ToString(this.Model.GetValue("F_SZXY_BoxQty"));//获取输入的箱数

            string flag = "";
            //客户订单号获取客户PO号、日期获取包装日期、业务员、客户
            //扫包装箱号
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_BoxNo"))
            {
                flag = "XH";
                if (!MONO.IsNullOrEmptyOrWhiteSpace())  return;

                if (!XQty.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(XQty) > 0 && !InNO.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {
                    string orgname = string.Empty;

                    if (InNO.Length <= 2)
                    {
                        this.View.ShowWarnningMessage("请检查输入的编号是否有误！"); return;
                    }

                    //string no1 = InNO.Remove(1, 1);

                    //int Currindex = no1.IndexOf('S');
                    //if (!InNO.IsNullOrEmptyOrWhiteSpace())
                    //{
                    //    if (Currindex == 0)
                    //    {
                        
                            WhereSql = $"  T1.F_SZXY_CTNNO='{InNO}'  ";
                    //    }
                     
                    //}
                    SetBillValue(Context, orgobj, WhereSql, this.View, e.Row);
                    this.Model.BatchCreateNewEntryRow("F_SZXY_KHHQDEntity", 1);
                    this.View.GetControl<EntryGrid>("F_SZXY_KHHQDEntity").SetEnterMoveNextColumnCell(true);
                    this.View.SetEntityFocusRow("F_SZXY_KHHQDEntity", e.Row + 1);
                    this.View.UpdateView("F_SZXY_KHHQDEntity");

 
                }
                else
                {
                    this.View.ShowWarnningMessage("请检查输入的箱数!");
                    return;
                }

            }


            //扫生产订单号
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_MoNO"))
            {

                flag = "DDH";
                if (!MONO.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {
                    WhereSql = $"  T1.F_SZXY_PUDNO+CAST(T1.F_SZXY_PUDLINENO as nvarchar(50))='{MONO}' ";
 
                    if (!XQty.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(XQty) > 0)
                    {
                        
                        SetBillValue(Context, orgobj, WhereSql, this.View, e.Row);

                        //调用打印

                        #region

                        //if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgDO)
                        //{
                        //    //是否指定标签模板
                        //    string PJSQL = "";

                        //    long orgid = Convert.ToInt64(OrgDO["Id"]);
 
                        //    int ckb = 0;
                        //    string CustId = "";
                        //    string Material = "";
                        //    string MacInfo = Utils.GetMacAddress();
                        //    Logger.Debug("当前MAC地址", MacInfo);

                        //    if (this.Model.DataObject["SZXY_KHHQDEntry"] is DynamicObjectCollection Entry)
                        //    {
                        //        foreach (var Row in Entry.Where(we => !Convert.ToString(we["F_SZXY_CBNO1"]).IsNullOrEmptyOrWhiteSpace()))
                        //        {
                        //            if (Row["F_SZXY_Material"] is DynamicObject MATERIALObj)
                        //            {
                        //                Material = MATERIALObj["Name"].ToString();

                        //            }


                        //            if (Row["F_SZXY_CUST"] is DynamicObject CustIdObj)
                        //            {
                        //                CustId = CustIdObj["Id"].ToString();
                        //            }

                        //            DataSet PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, CustId, Material, KBNO, "BarNo", ref ckb);

                        //            if (PrintModelDS != null)
                        //            {
                        //                XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{KBNO}'", "BarNo");
                        //            }
                        //        }
                        //    }

       
                         #endregion


                     }
                       else
                    {
                        this.View.ShowWarnningMessage("请检查输入的箱数!");
                        return;
                    }

                }
            }


             
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_CBNO"))
            {

               if(true) //if (flag == "XH")
                {
                    string KBNO = Convert.ToString(this.Model.GetValue("F_SZXY_CBNO"));
                        if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgDO)
                        {

                            long orgid = Convert.ToInt64(OrgDO["Id"]);
                            if (KBNO.IsNullOrEmptyOrWhiteSpace())
                            {
                                return;
                            }
                            #region

                            int ckb = 0;
                            string CustId = "";
                            string Material = "";
                            string MacInfo = Utils.GetMacAddress();
                            Logger.Debug("当前MAC地址", MacInfo);

                            if (this.Model.DataObject["SZXY_KHHQDEntry"] is DynamicObjectCollection Entry)
                            {
                                foreach (var Row in Entry.Where(we => Convert.ToString(we["F_SZXY_CBNO1"]) == KBNO))
                                {
                                    if (Row["F_SZXY_Material"] is DynamicObject MATERIALObj)
                                    {
                                        Material = MATERIALObj["Name"].ToString();

                                    }


                                    if (Row["F_SZXY_CUST"] is DynamicObject CustIdObj)
                                    {
                                        CustId = CustIdObj["Id"].ToString();
                                    }
                                    break;
                                }
                            }
                     

                        //是否指定标签模板
                        string PJSQL = " ";


                       DataSet  PrintModelDS = XYStraddleCut.getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, CustId, Material, KBNO, "BarNo", ref ckb);

                        if (PrintModelDS!=null)
                        {
                            XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{KBNO}'", "BarNo");
                        }

                            #endregion
                        }
                    }
                }
      
            
        }

        private void SetBillValue(Context context, DynamicObject orgobj, string WhereSql, IBillView view, int erow)
        {
            string XQty = Convert.ToString(this.Model.GetValue("F_SZXY_BoxQty"));//获取输入的箱数
            if (!WhereSql.IsNullOrEmptyOrWhiteSpace())
            {

                long orgid = Convert.ToInt64(orgobj["Id"]);

                string SQL = "/*dialect*/select t1.F_SZXY_MATERIAL,t1.F_SZXY_PLY,t1.F_SZXY_WIDTH,t1.F_SZXY,t1.F_SZXY_MANDREL,T5.F_SZXY_Text '客户订单号', "+
                            "t1.F_SZXY_CUSTID,t1.F_SZXY_PUDNO,t1.F_SZXY_CUSTNO,t1.F_SZXY_CUSTBACTH,t1.F_SZXY_PUDLINENO,T1.F_SZXY_CTNNO,t3.F_SZXY_MOID, " +
                            "t1.F_SZXY_SOSEQ1,t1.F_SZXY_SOENTRYID1,sum(t1.F_SZXY_AREA1) Area , " +
                            "sum(t1.F_SZXY_JQTY)  BOXCount ,T5.F_SZXY_SALER '销售员',T1.F_SZXY_DATE '包装日期',T5.F_SZXY_XSCUST '客户' " +
                            "from SZXY_t_BZDHEntry t1 " +
                            "join SZXY_t_BZD t3 on t1.FID = t3.FID " +
                            "left join T_PRD_MO T4 on T4.FBILLNO = t1.F_SZXY_PUDNO " +
                            "left join T_PRD_MOENTRY T5 on t5.FSEQ = t1.F_SZXY_PUDLINENO " +
                            $" where {WhereSql} " +
                            " group by t1.F_SZXY_MATERIAL,t1.F_SZXY_PLY,t1.F_SZXY_WIDTH,F_SZXY_CTNNO,t1.F_SZXY, " +
                            " t1.F_SZXY_MANDREL,t1.F_SZXY_CUSTID,t1.F_SZXY_PUDNO,t1.F_SZXY_CUSTNO, " +
                            " t1.F_SZXY_CUSTBACTH,t1.F_SZXY_PUDLINENO,t3.F_SZXY_MOID,t1.F_SZXY_SOSEQ1,t1.F_SZXY_SOENTRYID1 " +
                            ",T5.F_SZXY_SALER  ,T5.F_SZXY_SALER ,T5.F_SZXY_TEXT,T1.F_SZXY_DATE,T5.F_SZXY_XSCUST";

                //客户订单号获取客户PO号、日期获取包装日期、业务员、客户
                Logger.Debug("客户回签单", SQL);
                DataSet fillData = DBServiceHelper.ExecuteDataSet(this.Context, SQL);
                DateTime dt = DateTime.Now;
                if (fillData != null && fillData.Tables.Count > 0 && fillData.Tables[0].Rows.Count > 0)
                {
                    string value = "";

                    int mm = 0;


                    for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                    {
                        

                        mm = erow+i;

                        this.Model.BatchCreateNewEntryRow("F_SZXY_KHHQDEntity",1);

                        this.View.UpdateView("F_SZXY_KHHQDEntity");

                        this.View.GetControl<EntryGrid>("F_SZXY_KHHQDEntity").SetEnterMoveNextColumnCell(true);

                        this.Model.SetValue("F_SZXY_CBNO1", Convert.ToString(this.Model.GetValue("F_SZXY_CBNO")), mm);

                        this.Model.SetValue("F_SZXY_BoxNo", Convert.ToString(fillData.Tables[0].Rows[i]["F_SZXY_CTNNO"]), mm);



                         IViewService viewService = ServiceHelper.GetService<IViewService>();
                        //物料编码
                        value = Convert.ToString(fillData.Tables[0].Rows[i][0]);

                        if (!value.IsNullOrEmptyOrWhiteSpace())
                        {
                            string RMat = Utils.GetRootMatId(value, orgid.ToString(), Context);
                            DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_KHHQDEntry"] as DynamicObjectCollection;
                            DynamicObject F_SZXY_Material = this.Model.GetValue("F_SZXY_Material", mm) as DynamicObject;
                            Utils.SetBaseDataValue(viewService, entry1[mm], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_MATERIAL"), Convert.ToInt64(RMat), ref F_SZXY_Material, Context);
                        }
                        this.View.UpdateView("F_SZXY_Material", mm);

                        //客户代码
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["客户"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace())
                        {
                            //this.Model.SetValue("F_SZXY_Cust", value, mm);

                            string CustId = Utils.GetRootCustId(value, orgid.ToString(), Context);
                            this.Model.SetValue("F_SZXY_Cust", CustId, mm);
                            this.View.UpdateView("F_SZXY_Cust", mm);
                        }

                        this.Model.SetValue("F_SZXY_Date", Convert.ToString(fillData.Tables[0].Rows[i]["包装日期"]), mm);

                        value = Convert.ToString(fillData.Tables[0].Rows[i]["销售员"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_salesman", value, mm);
                        this.View.UpdateView("F_SZXY_salesman", mm);

                        //客户订单号
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["客户订单号"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CustOrderNo1", value, mm);
                 

                        //卷数
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["BOXCount"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_volume", value, mm);
       

                        //客户批号
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["F_SZXY_CUSTBACTH"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CustBacthNo", value, mm);
     

                        //生产订单编号
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["F_SZXY_PUDNO"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MONOE", value, mm);
       
                        //生产订单行号
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["F_SZXY_PUDLINENO"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOLineNOE", value, mm);
        

                        //厚度
                        value = Convert.ToString(fillData.Tables[0].Rows[i][1]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Ply", value, mm);
         
                        //宽度
                        value = Convert.ToString(fillData.Tables[0].Rows[i][2]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Width", value, mm);
                
                        //长度
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["F_SZXY"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Len", value, mm);
            
                        //面积
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["Area"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Area", value, mm);
 

 
                            if (mm>0&&(mm % Convert.ToInt32(XQty)) == 0)
                            {
                                string KBNO = GenNo(Context, this.View.BusinessInfo.GetForm().Id, orgobj, Convert.ToDateTime(this.Model.GetValue("FDate")));
                                if (!KBNO.IsNullOrEmptyOrWhiteSpace())
                                {
                                    this.Model.SetValue("F_SZXY_CBNO", KBNO);
                                    this.Model.SetValue("F_SZXY_CBNO1", KBNO, mm);//单据体卡板号
                                }
                            }
 
            
                        if (erow == 0 && mm == 0)
                        {
                            string KBNO = GenNo(Context, this.View.BusinessInfo.GetForm().Id, orgobj, Convert.ToDateTime(this.Model.GetValue("FDate")));
                            if (!KBNO.IsNullOrEmptyOrWhiteSpace())
                            {
                                this.Model.SetValue("F_SZXY_CBNO", KBNO);
                                this.Model.SetValue("F_SZXY_CBNO1", KBNO, erow);

                            }
                        }
                        this.View.SetEntityFocusRow("F_SZXY_KHHQDEntity", mm + 1);
                    }

         
                

 
                    OperateOption saveOption = OperateOption.Create();
                    view.Model.DataObject["FFormID"] = View.BillBusinessInfo.GetForm().Id;
                    var saveResult = BusinessDataServiceHelper.Save(this.View.Context, View.BusinessInfo, view.Model.DataObject, saveOption, "Save");
                   this.View.UpdateView("F_SZXY_KHHQDEntity");

                }
                else
                {
                    this.View.ShowWarnningMessage("没有匹配到数据！");return;
                }

            }
        }

        public static string GenNo(Context context, string FormId, DynamicObject orgobj, DateTime Date)
        {
            string value = string.Empty;

            Logger.Debug("genno", $"Date:{Date},orgobj{orgobj},FormId{FormId}");
            if (Date != DateTime.MinValue && orgobj != null && !FormId.IsNullOrEmptyOrWhiteSpace())
            {
                string orgId = orgobj["Id"].ToString();
                string orgname = orgobj["Name"].ToString();//AT-

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

                string DateNo = Date.ToString("yyMMdd");
                // 获取主键值
                //string tname = string.Empty;
                //switch (FormId)
                //{
                //    case "SZXY_BZD":
                //        tname = "SZXY_BGJ_XYBZList";
                //        break;
                //}
                //string resname = tname + orgId + "6";
                //long ids = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(context, resname, 1).FirstOrDefault();
                string LSH = Utils.GetLSH(context, "HQ",Convert.ToInt64( orgId), Date.ToString("yyyyMMdd"));
                //string LSh = String.Format("{0:D3}", ids);
                value = $"XY{No1}{DateNo}-{LSH}";

            }
            else throw new Exception("生成编码失败，请检查录入数据的完整性!");
            return value;
        }

    
    }
}
