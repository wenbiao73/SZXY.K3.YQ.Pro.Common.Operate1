
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

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("成品重检信息单据操作。")]
    public class Recheck : AbstractBillPlugIn
    {

  

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
 
            int m = this.Model.GetEntryRowCount("F_SZXY_XYCPJYEntity");
            int mm = 2;
            this.Model.BatchCreateNewEntryRow("F_SZXY_XYCPJYEntity", mm);
            this.View.GetControl<EntryGrid>("F_SZXY_XYCPJYEntity").SetEnterMoveNextColumnCell(true);
            this.View.GetControl("F_SZXY_HCNO").SetFocus();
            this.View.UpdateView("F_SZXY_XYCPJYEntity");

        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            int m = e.Row; string SelSQL = "";
            DynamicObject billobj = this.Model.DataObject;
            string InNO = Convert.ToString(this.Model.GetValue("F_SZXY_HCNO"));//获取输入的重检箱号
 
            string FQNo = Convert.ToString(this.Model.GetValue("F_SZXY_BarCode",m));//获取输入分切编号


            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_HCNO"))
            {

                if (InNO.IsNullOrEmptyOrWhiteSpace()) return;

                if (!InNO.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {

                    string orgid = Convert.ToString(orgobj["Id"]);
                    if (InNO.Length <= 2)
                    {
                        this.View.ShowWarnningMessage("请检查输入的编号是否有误！"); return;
                    }

                    SelSQL = "/*dialect*/" +
                        "select T1.F_SZXY_NO '分切编号'," +
                        "T1.F_SZXY_Team '班组'," +
                        "T1.F_SZXY_Class '班次'," +
                        "T1.F_SZXY_station '工位'," +
                        "T1.F_SZXY_SpecialMark '特殊标志'," +
                        "T1.F_SZXY_XNDJ '性能等级'," +
                        "T1.F_SZXY_Material '产品型号'," +
                        "T1.F_SZXY_PLy '厚度'," +
                        "T1.F_SZXY_Width '宽度'," +
                        "T1.F_SZXY_Len '长度'," +
                        "T1.F_SZXY_Area '面积'," +
                        "T2.F_SZXY_Machine '分切机'," +
                        "T2.F_SZXY_CASTMAC '流延机'," +
                        "T2.F_SZXY_LayerNo '产品编号' " +
                        "from SZXY_t_XYDCJXXBEntry T1 " +
                        "left join SZXY_t_XYDCJXXB T3 on T1.fid=T3.FID " +
                        "left join SZXY_t_XYFQEntry T2 on T1.F_SZXY_NO=T2.F_SZXY_BARCODEE  " +
                        "left join SZXY_t_XYFQ T4 on T2.fid=T4.FID " +
                        $"where T1.F_SZXY_BOXNOE='{InNO}' " +
                        $"and T3.F_SZXY_ORGID= {orgid}";
                                   
                        DataSet fillData = DBServiceHelper.ExecuteDataSet(this.Context, SelSQL);
                        SetBillValue(Context, fillData, this.View, e.Row, orgobj, "XH");
                    }

 
                }

           // 外观检验存在修改等级时，通过新单据体扫码分切编号，输入新等级，点击【确认修改等级】，系统实现自动刷新XY成品重检单上等级字段
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_BarCode"))
            {

                if (FQNo.IsNullOrEmptyOrWhiteSpace()) { return; }

                if (!FQNo.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {

                    // 检查是否存在此条码
                    string NoKey = "F_SZXY_BarCode";
                    string Entry = "SZXY_XYCPJYEntry";
                    string entrykey = "F_SZXY_XYCPJYEntity";
                    XYComposite.CheckNoIsCur(this.View, this.Model.DataObject, FQNo, Entry, NoKey, e.Row, entrykey, "RCK", Context);
                    string orgid = Convert.ToString(orgobj["Id"]);
                    if (FQNo.Length <= 2)
                    {
                        this.View.ShowWarnningMessage("请检查输入的编号是否有误！"); return;
                    }

                    SelSQL = "/*dialect*/" +
                        "select T1.F_SZXY_BARCODEE '分切编号', " +
                        "T1.F_SZXY_TEAMGROUP1 '班组'," +
                        "T1.F_SZXY_CLASSES1 '班次'," +
                        "T1.F_SZXY_STATION '工位'," +
                        "T1.F_SZXY_SPECIALMARK '特殊标志'," +
                        "T1.F_SZXY_BLEVEL '性能等级'," +
                        "T1.F_SZXY_MATERIAL '产品型号'," +
                        "T1.F_SZXY_PLy '厚度'," +
                        "T1.F_SZXY_Width '宽度'," +
                        "T1.F_SZXY_Len '长度'," +
                        "T1.F_SZXY_Area '面积'," +
                        "T1.F_SZXY_Machine '分切机'," +
                        "T1.F_SZXY_CASTMAC '流延机'," +
                        "T1.F_SZXY_LayerNo '产品编号' " +
                        //"from SZXY_t_XYDCJXXBEntry T1 " +
                        //"left join SZXY_t_XYDCJXXB T3 on T1.fid=T3.FID " +
                        " from SZXY_t_XYFQEntry T1  " +//on T1.F_SZXY_NO=T2.F_SZXY_BARCODEE  " +
                        "left join SZXY_t_XYFQ T4 on T1.fid=T4.FID " +
                        $"where T1.F_SZXY_BARCODEE='{FQNo}' " +
                        $"and T4.F_SZXY_OrgId1= {orgid}";

                    DataSet fillData = DBServiceHelper.ExecuteDataSet(this.Context, SelSQL);
                    SetBillValue(Context, fillData, this.View, e.Row, orgobj, "XH");
                }


            }
        }

        private void SetBillValue(Context context, DataSet fillData, IBillView view, int erow,  DynamicObject orgobj, string NoType)
        {

            DateTime dt = DateTime.Now;
            if (fillData != null && fillData.Tables.Count > 0 && fillData.Tables[0].Rows.Count > 0)
            {
                string value = "";
                DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYCPJYEntry"] as DynamicObjectCollection;


                for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                {
              
                    int index = erow + i;

                    //if (!Convert.ToString(fillData.Tables[0].Rows[i]["性能等级"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(fillData.Tables[0].Rows[i]["性能等级"]) > 0)
                    //{
                        this.Model.CreateNewEntryRow("F_SZXY_XYCPJYEntity");

                        this.View.GetControl<EntryGrid>("F_SZXY_XYCPJYEntity").SetEnterMoveNextColumnCell(true);
                    
                        IViewService viewService = ServiceHelper.GetService<IViewService>();
                        //物料编码
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["产品型号"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace())
                        {

                        string RMat = Utils.GetRootMatId(value, orgobj["Id"].ToString(), Context);
                        DynamicObject F_SZXY_Material = this.Model.GetValue("F_SZXY_Material", index) as DynamicObject;
                        if (RMat!="")
                        {
                            Utils.SetBaseDataValue(viewService, entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material"), Convert.ToInt64(RMat), ref F_SZXY_Material, Context);

                        }
                    }
 
                        if (this.Model.GetValue("F_SZXY_QAINSPECTOH") is DynamicObject opDo)
                        {
                            value = Convert.ToString(opDo["Id"]);

                            if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_QAInspecto", value, index);
                       
                        }
                    this.Model.SetValue("F_SZXY_Date", Convert.ToDateTime(this.Model.GetValue("FDate")), index);

                        entry1[index]["F_SZXY_BarCode"] = Convert.ToString(fillData.Tables[0].Rows[i]["分切编号"]);

                        if (!Convert.ToString(fillData.Tables[0].Rows[i]["分切机"]).IsNullOrEmptyOrWhiteSpace()) 
                                this.Model.SetValue("F_SZXY_MNo", Convert.ToString(fillData.Tables[0].Rows[i]["分切机"]), index);

                        if (!Convert.ToString(fillData.Tables[0].Rows[i]["流延机"]).IsNullOrEmptyOrWhiteSpace())
                            this.Model.SetValue("F_SZXY_LYMac", Convert.ToString(fillData.Tables[0].Rows[i]["流延机"]), index);

                        if (!Convert.ToString(fillData.Tables[0].Rows[i]["产品编号"]).IsNullOrEmptyOrWhiteSpace())
                            this.Model.SetValue("F_SZXY_HNO", Convert.ToString(fillData.Tables[0].Rows[i]["产品编号"]), index);
 
                        //班组
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["班组"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Team", value, index);

                        value = Convert.ToString(fillData.Tables[0].Rows[i]["班次"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Class", value, index);

                        //工位
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["工位"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Station", value, index);

                        value = Convert.ToString(fillData.Tables[0].Rows[i]["特殊标志"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", value, index);

                        //性能等级
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["性能等级"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_XNDJ1", value, index);


                        //厚度
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["厚度"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Ply", value, index);

                        //宽度
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["宽度"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Width", value, index);

                        //长度
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["长度"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_LENGTH", Convert.ToDecimal( value), index);
                   
                        //面积
                        value = Convert.ToString(fillData.Tables[0].Rows[i]["面积"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Area", value, index);

                   
  
                    //else
                    //{
                    //    this.View.ShowWarnningMessage("此编号没有性能等级！"); return;
                    //}
                    this.View.UpdateView("F_SZXY_XYCPJYEntity");
                    this.View.SetEntityFocusRow("F_SZXY_XYCPJYEntity", index + 1);

                }


                OperateOption saveOption = OperateOption.Create();
                view.Model.DataObject["FFormID"] = View.BillBusinessInfo.GetForm().Id;
                var saveResult = BusinessDataServiceHelper.Save(this.View.Context, View.BusinessInfo, view.Model.DataObject, saveOption, "Save");


            }
            else
            {
                this.View.ShowWarnningMessage("没有匹配到数据！");return;
            }

        }


        public static void Print(DataSet ds, int v, Context Context, IBillView View, string XH)
        {
            #region
            if (v == 0)
            {
                List<dynamic> listData = new List<dynamic>();
                listData.Clear();
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    string FListSQL = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_ListSQL"]);
                    foreach (DataRow Row in ds.Tables[0].Rows)
                    {

                        // Kingdee.BOS.Log.Logger.Debug("QuerySQL", $"/*Dialect*/{Convert.ToString(Row[6])} {View.Model.GetPKValue()}");
                        if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                        if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                        var ReportModel = new
                        {
                            FID = Convert.ToString(Row[0]),
                            report = Convert.ToString(Row[1]),
                            PrintAddress = Convert.ToString(Row[2]),
                            PrintQty = Convert.ToString(Row[3]),
                            //Label = Convert.ToString(Row[4]),
                            ConnString = Convert.ToString(Row[5]),
                            QuerySQL = $"{Convert.ToString(Row[6])}F_SZXY_CBNO1='{XH}' {FListSQL}"

                        };
                        listData.Add(ReportModel);

                    }
                }
                string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(listData);
                //调用打印
                string SQL = "/*dialect*/select F_SZXY_EXTERNALCONADS from SZXY_t_ClientExtern ";
                DataSet ids = DBServiceHelper.ExecuteDataSet(Context, SQL);
                if (ids != null && ids.Tables.Count > 0 && ids.Tables[0].Rows.Count > 0)
                {
                    string linkUrl = Convert.ToString(ids.Tables[0].Rows[0][0]).Replace("\\", "\\\\");// @"C:\Users\Administrator\Desktop\Grid++Report 6old\Grid++Report 6\Samples\CSharp\8.PrintInForm\bin\Debug\PrintReport.exe";

                    if (!strJson.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

                    else View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                }
                View.SendDynamicFormAction(View);

            }
            else
            {
                List<dynamic> listData = new List<dynamic>();
                listData.Clear();
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    string FListSQL = Convert.ToString(ds.Tables[0].Rows[0]["F_SZXY_ListSQL"]);
                    foreach (DataRow Row in ds.Tables[0].Rows)
                    {

                        //Kingdee.BOS.Log.Logger.Debug("QuerySQL", $"/*Dialect*/{Convert.ToString(Row[6])} {View.Model.GetPKValue()}");
                        if (Convert.ToString(Row[1]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印模板，请检查!");
                        if (Convert.ToString(Row[2]).IsNullOrEmptyOrWhiteSpace()) View.ShowMessage("当前用户没有设置grf打印地址，请检查!");
                        var ReportModel = new
                        {
                            FID = Convert.ToString(Row[0]),
                            report = Convert.ToString(Row[1]),
                            PrintAddress = Convert.ToString(Row[2]),
                            PrintQty = Convert.ToString(Row[3]),
                            //Label = Convert.ToString(Row[4]),
                            ConnString = Convert.ToString(Row[5]),
                            QuerySQL = $"{Convert.ToString(Row[6])}F_SZXY_CTNNO='{XH}' {FListSQL}"

                        };
                        listData.Add(ReportModel);

                    }
                }
                string strJson = Newtonsoft.Json.JsonConvert.SerializeObject(listData);
                //调用打印
                string SQL = "/*dialect*/select F_SZXY_EXTERNALCONADS from SZXY_t_ClientExtern ";
                DataSet ids = DBServiceHelper.ExecuteDataSet(Context, SQL);
                if (ids != null && ids.Tables.Count > 0 && ids.Tables[0].Rows.Count > 0)
                {
                    string linkUrl = Convert.ToString(ids.Tables[0].Rows[0][0]).Replace("\\", "\\\\");// @"C:\Users\Administrator\Desktop\Grid++Report 6old\Grid++Report 6\Samples\CSharp\8.PrintInForm\bin\Debug\PrintReport.exe";
                    if (!linkUrl.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Preview " + strJson);
                    if (!strJson.IsNullOrEmptyOrWhiteSpace()) View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

                    else View.ShowMessage("当前用户没有设置Grid++Report打印外接程序地址，请检查!");
                }
                View.SendDynamicFormAction(View);
            }
            #endregion
        }
    }
}
