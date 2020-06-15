
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
    [Description("待重检信息单据操作。")]
    public class WaitRecheck : AbstractBillPlugIn
    {
        public object inNo { get; private set; }
        public string KBNO { get; private set; }



        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {

                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);


                string F_SZXY_ForLabel = Convert.ToString(this.Model.GetValue("F_SZXY_ForLabel"));//补打标签


                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj && !F_SZXY_ForLabel.IsNullOrEmptyOrWhiteSpace())
                {

                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    //如果输入的是箱号
                    string SelSql = $"/*dialect*/select  T1.F_SZXY_BOXNOE from SZXY_t_XYDCJXXBEntry T1 left join SZXY_t_XYDCJXXB  T2  on t1.fid=T2.fid " +

                                     $"where  T1.F_SZXY_BOXNOE='{F_SZXY_ForLabel}'" +
                                     $" group by  T1.F_SZXY_BOXNOE";

                    DataSet SelSqlds = DBServiceHelper.ExecuteDataSet(this.Context, SelSql);

                    if (SelSqlds.Tables[0].Rows.Count > 0 && SelSqlds != null && SelSqlds.Tables.Count > 0)
                    {
                    }
                    else
                    {
                        this.View.ShowWarnningMessage("没有找到此箱号的信息");
                        return;
                    }

                    string MacInfo = Utils.GetMacAddress();
                    Logger.Debug("当前MAC地址", MacInfo);


                    DataSet PrintModelDS = null;
                    int ckb = 0;
                    orgid = Convert.ToInt64(OrgObj["Id"]);


                    //是否指定标签模板
                    string PJSQL = " ";

                    DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_Tagtype") as DynamicObject;

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
                        PrintModelDS = getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_ForLabel, ref ckb);

                        if (PrintModelDS != null)
                        {
                            XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "XH");
                        }
                        else
                        {
                            View.ShowWarnningMessage("没有匹配到模板！");
                            return;
                        }

                        Logger.Debug($" 箱号匹配到：{SelSqlds.Tables[0].Rows.Count}条数据。", "");

                    }

                }

            }
        }


        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            this.Model.ClearNoDataRow();
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.Model.ClearNoDataRow();
            this.Model.BatchCreateNewEntryRow("F_SZXY_XYDCJXXBEntity", 2);
            this.View.UpdateView("F_SZXY_XYDCJXXBEntity");
            this.View.GetControl<EntryGrid>("F_SZXY_XYDCJXXBEntity").SetEnterMoveNextColumnCell(true);
        }



        List<string> XHList = new List<string>();

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string Flag = "";

            int m = e.Row; string SelSQL = "";
            DynamicObject billobj = this.Model.DataObject;
            string InNO = Convert.ToString(this.Model.GetValue("F_SZXY_NO", m));//获取输入的编号
            DynamicObject BQTypeDO = this.Model.GetValue("F_SZXY_Tagtype") as DynamicObject;//获取输入 标签类型 
            string FQJYNO = Convert.ToString(this.Model.GetValue("F_SZXY_FQJYNO"));//获取输入的编号
            string XQty = Convert.ToString(this.Model.GetValue("F_SZXY_volume"));//获取输入每箱装的卷卷数

            //选择标签类型 并输入卷数（每箱装的卷卷数），扫描分切编号，自动将分切编号相关信息携带到XY待重检信息表中，装满一箱时自动生成箱条码，并调用标签进行打印；
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_NO"))
            {
                Flag = "BarNo";
                if (InNO.IsNullOrEmptyOrWhiteSpace())
                {
                    return;
                }

                if (!XQty.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(XQty) > 0 && !InNO.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {



                    string orgid = Convert.ToString(orgobj["Id"]);
                    if (InNO.Length <= 2)
                    {
                        this.View.ShowWarnningMessage("请检查输入的编号是否有误！"); return;
                    }

                    //string no1 = InNO.Remove(1, 1);
                    //inNo = InNO;
                    //int Currindex = no1.IndexOf('C');
                    //if (Currindex == 0)


                    SelSQL = "/*dialect*/" +
                                "select " +
                                "T1.F_SZXY_TEAMGROUP1 '班组',T1.F_SZXY_CLASSES1 '班次'," +
                                "T1.F_SZXY_STATION '工位',T1.F_SZXY_CHECKOUT '检验标志'," +
                                "T1.F_SZXY_SpecialMark '特殊标志',T1.F_SZXY_BLEVEL '性能等级'," +
                                "T1.F_SZXY_MATERIAL '产品型号',T1.F_SZXY_AREA '面积'," +
                                "T1.F_SZXY_PLy '厚度',T1.F_SZXY_Width '宽度'," +
                                "T1.F_SZXY_Len '长度',T2.FDATE '生产日期' " +
                                "from SZXY_t_XYFQEntry T1  " +
                                "left join SZXY_t_XYFQ  T2 on T1.fid=T2.fid " +
                                $"where T1.F_SZXY_BARCODEE='{InNO}' " +
                                $"and T2.F_SZXY_ORGID1={orgid}";
                    DataSet fillData = DBServiceHelper.ExecuteDataSet(this.Context, SelSQL);
                    SetBillValue(Context, fillData, this.View, e.Row, Convert.ToInt32(XQty), orgobj, "FQ");
                    //this.View.UpdateView("F_SZXY_XYDCJXXBEntity");


                    //打印
                    //if (XHList.Count>0 && XHList!=null)
                    //{

                    //    string MacInfo = Utils.GetMacAddress();
                    //    Logger.Debug("当前MAC地址", MacInfo);


                    //    DataSet PrintModelDS = null;
                    //    int ckb = 0;


                    //    //是否指定标签模板
                    //    string PJSQL = " ";

                    //    DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_Tagtype") as DynamicObject;

                    //    if (PrintTemp != null)
                    //    {
                    //        string PId = Convert.ToString(PrintTemp["Id"]);

                    //        if (!PId.IsNullOrEmptyOrWhiteSpace())
                    //        {
                    //            PJSQL = $" and T1.Fid={PId} ";
                    //        }
                    //    }
                    //    foreach (string F_SZXY_ForLabel in XHList)
                    //    {

                    //        if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                    //        {
                    //            PrintModelDS = getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_ForLabel, ref ckb);

                    //            if (PrintModelDS != null)
                    //            {
                    //                XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "XH");
                    //            }
                    //            else
                    //            {
                    //                View.ShowWarnningMessage("没有匹配到模板！");
                    //                return;
                    //            }
                    //        }


                    //    }
                    //}
                }
                else
                {
                    this.View.ShowWarnningMessage("请检查输入的卷数!");
                    this.Model.SetValue("F_SZXY_NO", "", e.Row);
                    this.View.UpdateView("F_SZXY_NO");
                    return;
                }

            }

            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_FQJYNO"))
            {
                Flag = "BillNo";
                if (!FQJYNO.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj)
                {

                    string orgid = Convert.ToString(orgobj["Id"]);
                    if (FQJYNO.Length <= 2)
                    {
                        this.View.ShowWarnningMessage("请检查输入的编号是否有误！"); return;
                    }

                    //分切外观检验单
                    SelSQL = "/*dialect*/" +
                             "select  " +
                             "T2.F_SZXY_STATION '工位',T2.F_SZXY_Barcode '分切编号'," +
                             "T2.F_SZXY_XNDJ '性能等级',T3.F_SZXY_TEAMGROUP1 '班组',T3.F_SZXY_CLASSES1 '班次'," +
                             "T2.F_SZXY_MATERIAL '产品型号',T2.F_SZXY_Area1 '面积',T3.F_SZXY_SpecialMark '特殊标志'," +
                             "T2.F_SZXY_PLy '厚度',T2.F_SZXY_Width '宽度'," +
                             "T2.F_SZXY '长度',T1.FDATE '生产日期' " +
                             "from  SZXY_t_FQJYD T1   " +
                             "left join SZXY_t_FQJYDEntry  T2 on T1.fid=T2.fid " +
                             "left join SZXY_t_XYFQEntry T3 on T2.F_SZXY_BARCODE=T3.F_SZXY_BarCodeE " +

                             $"where T1.FBILLNO='{FQJYNO}' " +
                             $"and T1.F_SZXY_ORGID={orgid}";
                    DataSet fillData = DBServiceHelper.ExecuteDataSet(this.Context, SelSQL);
                    SetBillValue(Context, fillData, this.View, e.Row, Convert.ToInt32(XQty), orgobj, "FQJY");
                    this.View.UpdateView("F_SZXY_XYDCJXXBEntity");


                    ////打印
                    //if (XHList.Count > 0 && XHList != null)
                    //{

                    //    string MacInfo = Utils.GetMacAddress();
                    //    Logger.Debug("当前MAC地址", MacInfo);


                    //    DataSet PrintModelDS = null;
                    //    int ckb = 0;


                    //    //是否指定标签模板
                    //    string PJSQL = " ";

                    //    DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_Tagtype") as DynamicObject;

                    //    if (PrintTemp != null)
                    //    {
                    //        string PId = Convert.ToString(PrintTemp["Id"]);

                    //        if (!PId.IsNullOrEmptyOrWhiteSpace())
                    //        {
                    //            PJSQL = $" and T1.Fid={PId} ";
                    //        }
                    //    }

                    //    StringBuilder STR = new StringBuilder();
                    //    int a = 0;
                    //    foreach (string F_SZXY_ForLabel in XHList)
                    //    {
                    //        if (a==0)
                    //        {
                    //            PrintModelDS = getPrintModel(this.View, Context, PJSQL, orgid.ToString(), MacInfo, F_SZXY_ForLabel, ref ckb);
                    //            a++;
                    //        }

                    //        STR.Append($"'{F_SZXY_ForLabel}',");

                    //    }

                    //    if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                    //    {

                    //        if (STR.ToString() != "" && !STR.ToString().IsNullOrEmptyOrWhiteSpace())
                    //        {
                    //            string BarNoStr = STR.ToString();
                    //            BarNoStr = BarNoStr.Substring(0, BarNoStr.Length - 1);
                    //            if (PrintModelDS != null)
                    //            {
                    //                XYStraddleCut.Print(PrintModelDS, ckb, Context, this.View, BarNoStr, "XH");
                    //            }
                    //            else
                    //            {
                    //                View.ShowWarnningMessage("没有匹配到模板！");
                    //                return;
                    //            }

                    //        }


                    //    }
                    //}

                }
            }


            // 
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_BOXNO"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                if (e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {
                    return;
                }
                else
                {

                }
                string NewXH = Convert.ToString(this.Model.GetValue("F_SZXY_BOXNO"));//
                string F_SZXY_ForLabel = e.OldValue.ToString();
                //if (!NewXH.IsNullOrEmptyOrWhiteSpace())
                //{
                //    XHList.Add(NewXH);
                //}

                //打印

                string MacInfo = Utils.GetMacAddress();
                Logger.Debug("当前MAC地址", MacInfo);


                DataSet PrintModelDS = null;
                int ckb = 0;


                //是否指定标签模板
                string PJSQL = " ";

                DynamicObject PrintTemp = this.Model.GetValue("F_SZXY_Tagtype") as DynamicObject;

                if (PrintTemp != null)
                {
                    string PId = Convert.ToString(PrintTemp["Id"]);

                    if (!PId.IsNullOrEmptyOrWhiteSpace())
                    {
                        PJSQL = $" and T1.Fid={PId} ";
                    }
                }


                PrintModelDS = getPrintModel(this.View, Context, PJSQL, this.Model.DataObject["F_SZXY_OrgId_Id"].ToString(), MacInfo, F_SZXY_ForLabel, ref ckb);



                if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
                {



                    if (PrintModelDS != null)
                    {
                        Print(PrintModelDS, ckb, Context, this.View, $"'{F_SZXY_ForLabel}'", "XH");
                    }
                    else
                    {
                        View.ShowWarnningMessage("没有匹配到模板！");
                        return;
                    }



                }

            }
        }
        private void SetBillValue(Context context, DataSet fillData, IBillView view, int erow, int XQty, DynamicObject orgobj, string NoType)
        {
            DateTime dt = DateTime.Now;
            if (fillData != null && fillData.Tables.Count > 0 && fillData.Tables[0].Rows.Count > 0)
            {
                string value = "";

                DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYDCJXXBEntry"] as DynamicObjectCollection;
                for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                {

                    int mm = erow + i;
                    if (Convert.ToString(fillData.Tables[0].Rows[i]["性能等级"]).IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(fillData.Tables[0].Rows[i]["性能等级"]) <= 0)
                    {
                        this.View.ShowWarnningMessage("此编号没有性能等级！");
                        return;
                    }


                    this.Model.CreateNewEntryRow("F_SZXY_XYDCJXXBEntity");

                    this.View.GetControl<EntryGrid>("F_SZXY_XYDCJXXBEntity").SetEnterMoveNextColumnCell(true);
                    //this.View.UpdateView("F_SZXY_XYDCJXXBEntity");
                    IViewService viewService = ServiceHelper.GetService<IViewService>();
                    //物料编码
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["产品型号"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace())
                    {
                        string RMat = Utils.GetRootMatId(value, orgobj["Id"].ToString(), Context);

                        DynamicObject F_SZXY_Material = this.Model.GetValue("F_SZXY_Material", mm) as DynamicObject;
                        Utils.SetBaseDataValue(viewService, entry1[mm], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material"), Convert.ToInt64(RMat), ref F_SZXY_Material, Context);
                    }



                    if (this.Model.GetValue("F_SZXY_operator") is DynamicObject opDo)
                    {
                        value = Convert.ToString(opDo["Id"]);

                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_operator1", value, mm);
                        this.View.UpdateView("F_SZXY_operator1", mm);
                    }

                    this.Model.SetValue("F_SZXY_IsCheck", "true", mm);
                    if (NoType == "FQJY")
                    {
                        //this.Model.SetValue("F_SZXY_NO", Convert.ToString(fillData.Tables[0].Rows[i]["分切编号"]), mm);
                        //DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYDCJXXBEntry"] as DynamicObjectCollection;

                        entry1[mm]["F_SZXY_NO"] = Convert.ToString(fillData.Tables[0].Rows[i]["分切编号"]);
                    }


                    //班组
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["班组"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Team", value, mm);

                    value = Convert.ToString(fillData.Tables[0].Rows[i]["班次"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Class", value, mm);

                    //工位
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["工位"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_station", value, mm);

                    value = Convert.ToString(fillData.Tables[0].Rows[i]["特殊标志"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", value, mm);

                    //性能等级
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["性能等级"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_XNDJ", value, mm);


                    //厚度
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["厚度"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Ply", value, mm);

                    //宽度
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["宽度"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Width", value, mm);

                    //长度
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["长度"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Len", value, mm);

                    //面积
                    value = Convert.ToString(fillData.Tables[0].Rows[i]["面积"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Area", value, mm);

                    value = Convert.ToString(fillData.Tables[0].Rows[i]["生产日期"]);

                    if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Date1", value, mm);
                    DynamicObject Teamobj = this.Model.GetValue("F_SZXY_Team", mm) as DynamicObject;



                    //string GX = "";

                    string PudType = "";
                    if (View.Model.GetValue("F_SZXY_Material", mm) is DynamicObject Material1)
                    {
                        if (Material1["F_SZXY_Assistant"] is DynamicObject PudTypeObj)
                        {
                            PudType = Convert.ToString(PudTypeObj["Number"]);
                        }
                    }
                    string GX = "";
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
                        View.ShowWarnningMessage("请检查产品型号的产品类型！");
                        return;
                    }


                    if (NoType == "FQ")
                    {
                        if (this.Model.GetValue("F_SZXY_BoxNo") is string NewBox)
                        {
                            this.Model.SetValue("F_SZXY_BoxNoE", NewBox, mm);
                        }
                        if (mm > 0 && (mm % Convert.ToInt32(XQty)) == 0)
                        {
                            string BoxNo = GenNo(Context, this.View.BusinessInfo.GetForm().Id, orgobj, Convert.ToDateTime(this.Model.GetValue("FDate")), Teamobj, GX);
                            if (!BoxNo.IsNullOrEmptyOrWhiteSpace())
                            {
                                this.Model.SetValue("F_SZXY_BoxNo", BoxNo);
                                this.Model.SetValue("F_SZXY_BoxNoE", BoxNo, mm);//
                            }
                        }

                    }
                    //初始化生成箱号

                    if (NoType == "FQJY")
                    {
                        if (mm == 0)
                        {
                            KBNO = GenNo(Context, this.View.BusinessInfo.GetForm().Id, orgobj, Convert.ToDateTime(this.Model.GetValue("FDate")), Teamobj, GX);
                            if (!KBNO.IsNullOrEmptyOrWhiteSpace())
                            {
                                this.Model.SetValue("F_SZXY_BoxNo", KBNO.ToString());//
                                this.Model.SetValue("F_SZXY_BoxNoE", KBNO.ToString(), mm);//
                            }
                        }
                        else
                        {
                            this.Model.SetValue("F_SZXY_BoxNoE", KBNO, mm);//
                        }

                    }


                    if (erow == 0 && mm == 0 && NoType == "FQ")
                    {
                        string KBNO = GenNo(Context, this.View.BusinessInfo.GetForm().Id, orgobj, Convert.ToDateTime(this.Model.GetValue("FDate")), Teamobj, GX);

                        if (!KBNO.IsNullOrEmptyOrWhiteSpace())
                        {
                            this.Model.SetValue("F_SZXY_BoxNo", KBNO);
                            this.Model.SetValue("F_SZXY_BoxNoE", KBNO, erow);

                        }
                    }



                    this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                    Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                    this.View.UpdateView("F_SZXY_XYDCJXXBEntity");
                    this.View.SetEntityFocusRow("F_SZXY_XYDCJXXBEntity", mm + 1);

                }

            }
            else
            {
                this.View.ShowWarnningMessage("没有匹配到数据！"); return;
            }

        }

        public static string GenNo(Context context, string FormId, DynamicObject orgobj, DateTime Date, DynamicObject TeamObejct, string GX)
        {
            string value = string.Empty;

            Logger.Debug("genno", $"Date:{Date},orgobj{orgobj},FormId{FormId}");
            if (Date != DateTime.MinValue && orgobj != null && !FormId.IsNullOrEmptyOrWhiteSpace() && TeamObejct != null && GX != "")
            {
                string BZName = TeamObejct["Name"].ToString();
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

                string LSh = Utils.GetLSH(context, "DCJ", Convert.ToInt64(orgId), Date.ToString("yyyyMMdd"));

                value = $"{No1}DJ{DateNo}{BZName}{GX}-{LSh}";



            }
            else throw new Exception("生成编码失败，请检查录入数据的完整性!");
            return value;
        }



        private DataSet getPrintModel(IBillView view, Context context, string pJSQL, string orgid, string macInfo, string f_SZXY_ForLabel, ref int V)
        {

            DataSet RESDS = null;
            string SQL12 = "/*dialect*/select T1.FID,T1.F_SZXY_REPORT,T1.F_SZXY_PRINTMAC,T1.F_SZXY_PRINTQTY,T1.F_SZXY_LPRINT,T1.F_SZXY_CONNSTRING,T1.F_SZXY_QUERYSQL," +
            "T1.F_SZXY_ListSQL,T1.F_SZXY_CustID ,T1.F_SZXY_Model '产品型号', T3.FNAME, T1.F_SZXY_CHECKBOX 'CKB',T1.F_SZXY_Remark,T1.FNUMBER '标签' from SZXY_t_BillTemplatePrint T1" +
            " left join   T_BD_MATERIAL T2  on T2.FMATERIALID=T1.F_SZXY_Model " +
            " left join   T_BD_MATERIAL_L T3 on t2.FMATERIALID=T3.FMATERIALID   where" +
            "  T1.F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and T1.FUSEORGID='" + orgid + "'" +
            " and T1.F_SZXY_TYPESELECT='1'   and T1.FDOCUMENTSTATUS='C'  " + pJSQL + " ";
            DataSet DS = null;
            if (!macInfo.IsNullOrEmptyOrWhiteSpace())
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
                else
                {
                    string WhereSql = $"{SQL12} and F_SZXY_Remark='{macInfo}' ";
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
                        WhereSql = $"{SQL12} and F_SZXY_Remark='' ";
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
                    }
                }

            }

            return RESDS;
        }

        public static void Print(DataSet DS, int v, Context Context, IBillView View, string XH, string INNOType = "")
        {
            string formid = View.BillBusinessInfo.GetForm().Id.ToString();
            Logger.Debug("打印---", "------------BEGIN------------------");

            Logger.Debug("---", $"------------打印条码或生产订单号为：{XH}------------------");
            List<dynamic> listData = new List<dynamic>();

            listData.Clear();

            if (DS != null && DS.Tables.Count > 0 && DS.Tables[0].Rows.Count > 0)
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
                        if (!strJson.IsNullOrEmptyOrWhiteSpace())
                        {
                            View.GetControl("F_SZXY_Link").InvokeControlMethod("SetClickFromServerOfParameter", linkUrl, "Print " + strJson);

                            //打印记录表
                            // Utils.GenPrintReCord(View, Context, formid, XH);

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
