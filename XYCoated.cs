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
    [Description("XY涂覆单据操作。")]
    public class XYCoated : AbstractBillPlugIn
    {

        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);

            if (this.Model.DataObject != null && (e.Operation.FormOperation.Id.EqualsIgnoreCase("save") || e.Operation.FormOperation.Id.EqualsIgnoreCase("submit")))
            {
 
                if (this.Model.DataObject["SZXY_XYTFHEntry"] is DynamicObjectCollection Entry)
                {

                    List<DynamicObject> ListRow1 = new List<DynamicObject>() { };

                    int Seq = 0;
                    foreach (var item in Entry)
                    {
                        if (Convert.ToString(item["F_SZXY_CoatCode"]).IsNullOrEmptyOrWhiteSpace() || item["F_SZXY_CoatCode"] == null)
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

                        this.View.UpdateView("F_SZXY_XYTFEntity");

                    }


             
                }
            }
        }


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;
            //在XY涂覆单输入机台、时间，系统根据机台、时间匹配日计划记录，自动将相同的日计划记录携带到XY涂覆单上
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && billobj["F_SZXY_Mac"] is DynamicObject MacObj && !formID.IsNullOrEmptyOrWhiteSpace())
            {

                string MacId = MacObj["ID"].ToString();
                string MacNumber = MacObj["Number"].ToString();
                DynamicObjectCollection entry = billobj["SZXY_XYTFHEntry"] as DynamicObjectCollection;//单据体

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
                        string Sql = $"/*dialect*/select T2.F_SZXY_PTNo '生成订单号',T2.F_SZXY_MoLineNo '生成订单行号',T2.F_SZXY_CJ '车间'" +
                                       $" ,T2.F_SZXY_recipe '配方',T2.F_SZXY_Team '班组',T2.F_SZXY_Class '班次',T2.F_SZXY_MOID '生产订单内码'  " +
                                       $" ,T2.F_SZXY_PLY '厚度',T2.F_SZXY_WIDTH '宽度',T2.F_SZXY_LEN '长度',T2.F_SZXY_MATERIAL '产品型号',  " +
                                       $"  T2.F_SZXY_AREA '面积',T2.F_SZXY_OPERATOR '操作员',T2.F_SZXY_SMark '特殊标志'," +
                                       $" T2.F_SZXY_SDate 'Stime', T2.F_SZXY_EndDate 'Etime',T2.FEntryID,T2.Fid " +
                                       $" from 	SZXY_t_TFHTRSCJH T1 " +
                                       $" Left join SZXY_t_TFHTRSCJHEntry T2 on T1.Fid=T2.Fid " +
                               
                                       $" where T2.F_SZXY_Machine='{MacId}' " +
                                       $" and T1.F_SZXY_OrgId={orgid} " +
                                       $" and CONVERT(datetime ,'{Stime}', 120) between CONVERT(datetime,T2.F_SZXY_SDATE, 120)  and  CONVERT(datetime ,T2.F_SZXY_ENDDATE, 120) " +
                                       $" and T1.FDOCUMENTSTATUS in  ('C')";

                        Logger.Debug("匹配日计划的SQL", Sql);
                        DataSet DpDs = DBServiceHelper.ExecuteDataSet(Context, Sql);
                        IViewService Services = ServiceHelper.GetService<IViewService>();
                        if (DpDs != null && DpDs.Tables.Count > 0 && DpDs.Tables[0].Rows.Count > 0)
                        {

                            int n = this.Model.GetEntryRowCount("F_SZXY_XYTFEntity");
                            for (int i = 0; i < DpDs.Tables[0].Rows.Count; i++)
                            {

                                int index = n + i;
                                //给单据头赋值
                                if (i==0)
                                {
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[i]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(DpDs.Tables[0].Rows[i]["生产订单内码"]));
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[i]["操作员"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_operator1", Convert.ToInt64(DpDs.Tables[0].Rows[i]["操作员"]));
                                    if (!Convert.ToString(DpDs.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(DpDs.Tables[0].Rows[i]["车间"]));
                                }
                              
                                //单据体


                                #region
                                this.Model.CreateNewEntryRow("F_SZXY_XYTFEntity");
                                this.Model.SetValue("F_SZXY_RJHEntryID", Convert.ToString(DpDs.Tables[0].Rows[i]["FEntryID"]), index);
                                this.Model.SetValue("F_SZXY_PTNO", DpDs.Tables[0].Rows[i]["生成订单号"].ToString(), index);
                                this.Model.SetValue("F_SZXY_PuOLineNo", DpDs.Tables[0].Rows[i]["生成订单行号"].ToString(), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", DpDs.Tables[0].Rows[i]["特殊标志"].ToString(), index);
                                this.Model.SetValue("F_SZXY_TFMac", MacId, index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt32(DpDs.Tables[0].Rows[i]["班组"]), index);
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt32(DpDs.Tables[0].Rows[i]["班次"]), index);

                                DateTime dt = DateTime.Now;
                                this.Model.SetValue("F_SZXY_SDate", dt, index);

                                if (index != 0) this.Model.SetValue("F_SZXY_EDate", dt, index - 1);
                                DynamicObjectCollection entry1 = billobj["SZXY_XYTFHEntry"] as DynamicObjectCollection;
                                DynamicObject Material1 = this.Model.GetValue("F_SZXY_productModel", index) as DynamicObject;
                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                                {
                                    string RMat = Utils.GetRootMatId(Convert.ToString(DpDs.Tables[0].Rows[i]["产品型号"]), orgid.ToString(), Context);
                                    Utils.SetBaseDataValue(Services, (DynamicObject)entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_productModel"), Convert.ToInt64(RMat), ref Material1, Context);

                                }

                                if (!Convert.ToString(DpDs.Tables[0].Rows[i]["配方"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_formula", DpDs.Tables[0].Rows[i]["配方"].ToString(), index);
                                this.Model.SetValue("F_SZXY_PLYL", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["厚度"]), index);
                                this.Model.SetValue("F_SZXY_WidthL", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["宽度"]), index);
                                this.Model.SetValue("F_SZXY_LENL", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["长度"]), index);
                                this.Model.SetValue("F_SZXY_AreaL", Convert.ToDecimal(DpDs.Tables[0].Rows[i]["面积"]), index);

                                if (this.Model.GetValue("F_SZXY_productModel", index) is DynamicObject Material)
                                {
                                    string PudType="";
                                    if (Material["F_SZXY_Assistant"] is DynamicObject PudTypeObj)
                                    {
                                        PudType = Convert.ToString(PudTypeObj["Number"]);
                                    }
                                    #region 生成涂覆编号
                                    string OrgName = orgobj["Name"].ToString();
                                    string No1 = ""; string PF = "", BZ = "";string GX = "";
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
                                    //L:陶瓷涂覆
                                    //Y: 油涂
                                    // P:喷涂
                                    if (new string[] { "GFTCTF", "SFTCTF" }.Contains(PudType))
                                    {
                                        GX = "L";
                                    }
                                    else if (new string[] { "GFTJTF", "SFTJTF" }.Contains(PudType))
                                    {
                                        GX = "P";
                                    }
                                    else if (new string[] { "GFYXTF", "SFYXTF" }.Contains(PudType))
                                    {
                                        GX = "Y";
                                    }
 
                                    if (this.Model.GetValue("F_SZXY_formula", index) is DynamicObject PFObj) PF = PFObj["FDataValue"].ToString();
 
                                    DateTime DateNo1 = Convert.ToDateTime(this.Model.GetValue("FDate"));
                                    string MacName = MacObj["Name"].ToString();
                                    MacName = System.Text.RegularExpressions.Regex.Replace(MacName, @"[^0-9]+", "");
                                    string DateNo = DateNo1.ToString("yyMMdd");
                                    if (this.Model.GetValue("F_SZXY_TeamGroup", index) is DynamicObject Teamobj) BZ = Teamobj["Name"].ToString();
                                    //编号
                                    if (GX=="")
                                    {
                                        throw new Exception("生成编码失败，请检查物料的产品类型！");
                                    }
                                    if (MacObj == null || DateNo1 == DateTime.MinValue || PF == "" || BZ == "") throw new Exception("生成编码失败，请检查数据完整性！");
                                    string LSH = Utils.GetLSH(Context, "TF", orgid, DateNo1.ToString("yyyyMMdd"),MacObj);
                                    string TFNo = $"{No1}L{GX}{PF}{MacName}{DateNo}{BZ}{LSH}";
                                    this.Model.SetValue("F_SZXY_CoatCode", TFNo, index);
                                    #endregion

                                }
 
                                #endregion
                            }
                            int co = this.Model.GetEntryRowCount("F_SZXY_XYTFEntity");
                            //billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            //Utils.Save(View, new DynamicObject[] { billobj }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                        
                            this.View.UpdateView();
                        }
                        else
                        {
                            this.View.ShowWarnningMessage("没有匹配到日计划");
                            return;
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
                    Utils.TYPrint(this.View, Context, orgid);


                    if (this.Model.DataObject["SZXY_XYTFHEntry"] is DynamicObjectCollection entry2)
                    {
                        foreach (DynamicObject item in from ck in entry2
                                                       where Convert.ToString(ck["F_SZXY_print"]).EqualsIgnoreCase("true")
                                                       select ck)
                        {
                            item["F_SZXY_print"] = "false";
                        }
                          View.UpdateView("F_SZXY_XYTFEntity");
                        //billobj["FFormId"] = View.BusinessInfo.GetForm().Id;
                        //IOperationResult a = Utils.Save(View, new DynamicObject[] { billobj }, OperateOption.Create(), Context);
                    }
                }
            }
        }
   

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            //在涂覆单明细信息扫描分层编号 / 分切编号 / 湿法编号 / 涂覆编号时，
            //能自动将XY分层单 / 分切单 / 湿法单 / 涂覆单上的分层 / 分切 / 湿法 / 涂覆记录携带到XY涂覆单上，
            //点击功能菜单【生成涂覆编号】按钮，按规则生成产品编码（涂覆编号）

            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_OFNo"))
            {
                int m = e.Row;
                string YMNo = Convert.ToString(this.Model.GetValue("F_SZXY_OFNo", m));
                string YMNo1 = "";
                if (!YMNo.IsNullOrEmptyOrWhiteSpace())
                {
                    YMNo1 = YMNo.Replace("LG-", string.Empty);
                    YMNo1 = YMNo1.Replace("AT-", string.Empty);
                };
                DynamicObject orgobj = this.Model.GetValue("F_SZXY_OrgId") as DynamicObject;
                string RJHEntryID = Convert.ToString(this.Model.GetValue("F_SZXY_RJHEntryID",m));
                if (YMNo.IsNullOrEmptyOrWhiteSpace())
                {
                    return;
                };
                int start = 1, length = 1;
                string GXCode = YMNo1.Substring(start, length);
             
                string GXName = "";
                switch (GXCode.ToUpper())
                {
                    case "H":
                        GXName = "分层编号";
                        break;
                    case "S":
                        GXName = "分切编号";
                        break;
                    case "W":
                        GXName = "湿法编号";
                        break;
                    case "L":
                        GXName = "涂覆编号";
                        break;
                }
                if (GXName=="")
                {
                      GXCode = YMNo1.Substring(0, length);
                    switch (GXCode.ToUpper())
                    {
                        case "H":
                            GXName = "分层编号";
                            break;
                        case "S":
                            GXName = "分切编号";
                            break;
                        case "W":
                            GXName = "湿法编号";
                            break;
                        case "L":
                            GXName = "涂覆编号";
                            break;
                    }
                }

                if (YMNo != "" && orgobj != null)
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    string sql = string.Empty;
                    if (GXName.EqualsIgnoreCase("湿法编号"))
                    {
                        sql = $"/*dialect*/select T1.F_SZXY_Mac '原膜机台', " +
                                  " T2.fid '原膜外观', " +
                                  " T2.F_SZXY_station '工位号'," +
                                  "  T2.F_SZXY_PLy '原膜厚度'," +
                                  " T2.F_SZXY_Width '原膜宽度'," +
                                  "  T2.F_SZXY_Len '原膜长度'," +
                                  "  T2.F_SZXY_Area '原膜面积'," +
                                  "  T2.F_SZXY_ZHDJ '原膜等级' ," +
                                  "  T2.F_SZXY_MATERIALID '原膜料号'," +
                                  "T2.F_SZXY_AIRP '透气率'," +
                                  " T2.F_SZXY_AREALDENSITY '面密度'," +
                                  " '' '外观不良'," +
                                  //"  T3.F_SZXY_PTNo '生成订单号',T3.F_SZXY_MoLineNo '生成订单行号' " +
                                  //"  , T3.F_SZXY_recipe '配方',T3.F_SZXY_Team '班组',T3.F_SZXY_Class '班次' " +
                                  //"  ,T3.F_SZXY_PLY '厚度',T3.F_SZXY_WIDTH '宽度',T3.F_SZXY_LEN '长度',T3.F_SZXY_MATERIAL '产品型号',  " +
                                  //"  T3.F_SZXY_AREA '面积',T3.F_SZXY_OPERATOR '操作员',T3.F_SZXY_SMark '特殊标志'," +
                                  " T2.FEntryID,T2.Fid " +//T3.FEntryID 'RJHFEntryID' " +
                                  "  from SZXY_t_SFD T1" +
                                  " left join  SZXY_t_SFDEntry T2 on T1.FID = T2.FID " +
                                  //$" left join  SZXY_t_TFHTRSCJHEntry T3 on T3.FEntryID = {RJHEntryID} " +
                                  $" where T2.F_SZXY_SFNO = '{YMNo}' " +
                                 // $"  and T1.F_SZXY_OrgId ={orgid}" +
                                  "  ";
                    }
                    else if (GXName.EqualsIgnoreCase("分层编号"))
                    {
                        sql = $"/*dialect*/select T1.F_SZXY_Mac '原膜机台', " +
                                   " T2.F_SZXY_DOA '原膜外观', " +
                                   " T2.fid '工位号'," +
                                   "  T2.F_SZXY_OUTPLY '原膜厚度'," +
                                   " T2.F_SZXY_OUTWIDTH '原膜宽度'," +
                                   "  T2.F_SZXY_OUTLEN '原膜长度'," +
                                   "  T2.F_SZXY_OUTAREA '原膜面积'," +
                                   "  T2.F_SZXY_ZHDJ '原膜等级' ," +
                                   "  T2.F_SZXY_Material '原膜料号'," +
                                   "T2.F_SZXY_PERMEABILITY '透气率'," +
                                     " '' '面密度'," +
                                     " '' '外观不良'," +
                                   " T2.FEntryID,T2.Fid " +//T3.FEntryID 'RJHFEntryID' " +
                                   "  from SZXY_t_XYFC  T1" +
                                   " left join  SZXY_t_XYFCEntry T2 on T1.FID = T2.FID " +
                                   //$" left join  SZXY_t_TFHTRSCJHEntry T3 on T3.FEntryID = {RJHEntryID} " +
                                   $" where T2.F_SZXY_LayerNo = '{YMNo}' " +
                                   //$"  and T1.F_SZXY_OrgId ={orgid}" +
                                   "  ";
                    }
                    else if (GXName.EqualsIgnoreCase("分切编号"))
                    {
                        sql = $"/*dialect*/select T1.F_SZXY_Mac '原膜机台', " +
                                   " T2.F_SZXY_QState '原膜外观', " +
                                   " T2.F_SZXY_station '工位号'," +
                                   "  T2.F_SZXY_PLY '原膜厚度'," +
                                   " T2.F_SZXY_WIDTH '原膜宽度'," +
                                   "  T2.F_SZXY_LEN '原膜长度'," +
                                   "  T2.F_SZXY_AREA '原膜面积'," +
                                   "  T2.F_SZXY_BLEVEL '原膜等级' ," +
                                   "  T2.F_SZXY_Material '原膜料号'," +
                                   " '' '透气率'," +
                                   " '' '面密度'," +
                                     "T2.F_SZXY_BLYY '外观不良'," +
                                   " T2.FEntryID,T2.Fid " +//T3.FEntryID 'RJHFEntryID' " +
                                   "  from SZXY_t_XYFQ  T1" +
                                   " left join  SZXY_t_XYFQEntry T2 on T1.FID = T2.FID " +
                                   //$" left join  SZXY_t_TFHTRSCJHEntry T3 on T3.FEntryID = {RJHEntryID} " +
                                   $" where T2.F_SZXY_BARCODEE = '{YMNo}' " +
                                   $"  and T1.F_SZXY_OrgId1 ={orgid}" +
                                   " ";
                    }
                    else if (GXName.EqualsIgnoreCase("涂覆编号"))
                    {
                        sql = $"/*dialect*/select T1.F_SZXY_Mac '原膜机台', " +
                                   " T2.F_SZXY_AppearaDys '原膜外观', " +
                                   " T2.F_SZXY_Tag '工位号'," +
                                   "  T2.F_SZXY_PLy '原膜厚度'," +
                                   " T2.F_SZXY_Width '原膜宽度'," +
                                   "  T2.F_SZXY_Len '原膜长度'," +
                                   "  T2.F_SZXY_Area '原膜面积'," +
                                   "  T2.F_SZXY_ZHDJ '原膜等级' ," +
                                   "  T2.F_SZXY_Material '原膜料号'," +
                                    " T2.F_SZXY_AP '透气率'," +
                                     " T2.F_SZXY_AREALDENSITY '面密度'," +
                                     "T2.F_SZXY_BLYY '外观不良'," +
                                   //"  T3.F_SZXY_PTNo '生成订单号',T3.F_SZXY_MoLineNo '生成订单行号' " +
                                   //"  , T3.F_SZXY_recipe '配方',T3.F_SZXY_Team '班组',T3.F_SZXY_Class '班次' " +
                                   //"  ,T3.F_SZXY_PLY '厚度',T3.F_SZXY_WIDTH '宽度',T3.F_SZXY_LEN '长度',T3.F_SZXY_MATERIAL '产品型号',  " +
                                   //"  T3.F_SZXY_AREA '面积',T3.F_SZXY_OPERATOR '操作员',T3.F_SZXY_SMark '特殊标志'," +
                                   " T2.FEntryID,T2.Fid " +//T3.FEntryID 'RJHFEntryID' " +
                                   "  from SZXY_t_XYTF  T1" +
                                   " left join  SZXY_t_XYTFEntry T2 on T1.FID = T2.FID " +
                                   //$" left join  SZXY_t_TFHTRSCJHEntry T3 on T3.FEntryID = {RJHEntryID} " +
                                   $" where T2.F_SZXY_CoatCode = '{YMNo}' ";
                                   //$"  and T1.F_SZXY_OrgId ={orgid}";
                            
                    }
                     
                 
                    Logger.Debug("SElsql:", sql);
                    DataSet ds = Utils.CBData(sql, Context);
                    if (ds!=null&&ds.Tables.Count>0&&ds.Tables[0].Rows.Count>0)
                    {
                        SetValue(ds, m, YMNo, orgobj, GXName);
                    }
                    else
                    {
                        this.View.ShowWarnningMessage("没有匹配到数据");return;
                    }
                   
                }
            }


            //录入浆料编号平分面积
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_JLBH"))
            {
                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                string JLNO=  Convert.ToString(e.NewValue);
                if (JLNO != "" &&!e.NewValue.IsNullOrEmptyOrWhiteSpace())
                {
                    //携带 产出重量 
                    string WEIGHT = "0";
                
                    string AreaCount = "0";
                    string Sql = $"select F_SZXY_WEIGHT,F_SZXY_FORMULAH1  from SZXY_t_JLD where F_SZXY_JLNO='{JLNO}'";
                    DataSet SelweightDs = DBServiceHelper.ExecuteDataSet(Context, Sql);
                    if (SelweightDs!=null&& SelweightDs.Tables.Count>0 && SelweightDs.Tables[0].Rows.Count>0)
                    {
                          WEIGHT = Convert.ToString( SelweightDs.Tables[0].Rows[0][0]);
                          this.Model.SetValue("F_SZXY_JLZL", Convert.ToDecimal(WEIGHT));
                        if (!Convert.ToString(SelweightDs.Tables[0].Rows[0][1]).IsNullOrEmptyOrWhiteSpace())
                        {
                            this.Model.SetValue("F_SZXY_JLPF", Convert.ToString(SelweightDs.Tables[0].Rows[0][1]));
                        }
                 
                        //平分到整个涂覆包含浆料编号的单据体行，平分重量=浆料编号对应产出重量*（行面积/所有行汇总面积）
                        string SelAreaCount = "select Sum(T1.F_SZXY_AREAL)from SZXY_t_XYTFEntry T1 left join  SZXY_t_XYTF T2 on T1.Fid=T2.Fid" +
                                              $" where T2.FDOCUMENTSTATUS!='C' and T1.F_SZXY_JLNO='{JLNO}' ";
                        DataSet SelAreaDs = DBServiceHelper.ExecuteDataSet(Context, SelAreaCount);
                        if (SelAreaDs != null && SelAreaDs.Tables.Count > 0 && SelAreaDs.Tables[0].Rows.Count > 0)
                        {
                            AreaCount = Convert.ToString(SelAreaDs.Tables[0].Rows[0][0]);
                        }
                
 
                        if (Convert.ToDecimal( WEIGHT)>0 && Convert.ToDecimal(AreaCount) > 0)
                        {
                            string UpdateJLZL = $"/*dialect*/update T1 set T1.F_SZXY_JLWEIGHT = {Convert.ToDecimal(WEIGHT)}*(T1.F_SZXY_AREAL/{Convert.ToDecimal(AreaCount)}) " +
                                  $" from SZXY_t_XYTFEntry T1, SZXY_t_XYTF T2 " +
                                  $" where  T1.Fid=T2.Fid" +
                                  $" and T2.FDOCUMENTSTATUS!='C' and T1.F_SZXY_JLNO='{JLNO}' ";
                            DBServiceHelper.Execute(Context, UpdateJLZL);
                            this.View.UpdateView();
                            this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                            Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                        }
                  
                    }
                    else
                    {
                        this.View.ShowWarnningMessage("此编码没有匹配到信息！");return;
                    }

                }
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
           // this.Model.BatchCreateNewEntryRow("F_SZXY_XYTFEntity", 2);
            this.View.UpdateView("F_SZXY_XYTFEntity");
            this.View.GetControl<EntryGrid>("F_SZXY_XYTFEntity").SetEnterMoveNextColumnCell(true);

        }
        public override void AfterCreateNewEntryRow(CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            this.View.GetControl<EntryGrid>("F_SZXY_XYTFEntity").SetEnterMoveNextColumnCell(true);
        }


        private void SetValue(DataSet Ds, int m, string lyNo, DynamicObject orgobj,string GXName)
        {
 
            IViewService Services = ServiceHelper.GetService<IViewService>();

            string SelCurrCode = "";
            //判断当前编号是否被使用  单据体投入面积汇总 和 分层产出面积对比 ，还有剩余就平分到分切投入面积，没有就设置为0 
            switch (GXName)
            {
                case "分层编号": //分层编号
                    SelCurrCode = $"/*diacect*/select T1.F_SZXY_OFNO,sum(T1.F_SZXY_AREA) '已投入面积',ISNULL(T2.F_SZXY_OUTAREA,0)  '原膜产出面积' "+
                                    $" from SZXY_t_XYTFEntry T1 "+
                                    $" left  join SZXY_t_XYFCEntry T2 on T1.F_SZXY_OFNO = T2.F_SZXY_LAYERNO "+
                                    $" where T1.F_SZXY_OFNO = '{lyNo}' "+
                                    $" group by T1.F_SZXY_OFNO,T2.F_SZXY_OUTAREA ";
                    break;

                case "湿法编号": //湿法编号
                    SelCurrCode = $"/*diacect*/select T1.F_SZXY_OFNO,sum(T1.F_SZXY_AREA) '已投入面积',ISNULL(T2.F_SZXY_AREA,0) '原膜产出面积'  " +
                                    $"from SZXY_t_XYTFEntry T1  " +
                                    $" left join SZXY_t_SFDEntry T2 on T1.F_SZXY_OFNO = T2.F_SZXY_SFNO  " +
                                    $" where T1.F_SZXY_OFNO = '{lyNo}'  " +
                                    $" group by T1.F_SZXY_OFNO,T2.F_SZXY_AREA ";
                    break;

                case "涂覆编号"://涂覆编号
                    SelCurrCode = $"/*diacect*/select T1.F_SZXY_OFNO,sum(T1.F_SZXY_AREA) '已投入面积',ISNULL(T2.F_SZXY_AREAL,0) '原膜产出面积' " +
                                  $"   from SZXY_t_XYTFEntry T1 " +
                                  $"   left join SZXY_t_XYTFEntry T2 on T1.F_SZXY_OFNO = T2.F_SZXY_COATCODE " +
                                  $"   where T1.F_SZXY_OFNO = '{lyNo}' " +
                                  $"   group by T1.F_SZXY_OFNO,T2.F_SZXY_AREA,T2.F_SZXY_AREAL ";
                    break;

                case "分切编号":
                    SelCurrCode = "/*dialect*/select T1.F_SZXY_OFNO,sum(T1.F_SZXY_AREA) '已投入面积',ISNULL(T2.F_SZXY_AREA,0) '原膜产出面积' " +
                               $"  from SZXY_t_XYTFEntry T1 " +
                               $"  left  join SZXY_t_XYFQEntry T2 on T1.F_SZXY_OFNO = T2.F_SZXY_BARCODEE " +
                              $"   where T1.F_SZXY_OFNO = '{lyNo}' " +
                               $"  group by T1.F_SZXY_OFNO,T2.F_SZXY_AREA";
                    break;

            }


            Logger.Debug("检查编号在分切是否存在，查出面积SQL：", SelCurrCode);
            DataSet SelCurrCodeDS = DBServiceHelper.ExecuteDataSet(Context, SelCurrCode);

            decimal DFPArea = 0;//未分配的面积

            bool UseCode = false;//默认未使用
            if (SelCurrCodeDS != null && SelCurrCodeDS.Tables.Count > 0 && SelCurrCodeDS.Tables[0].Rows.Count > 0)
            {
                //存在当前编号

                UseCode = true;
                decimal SumInArea = Convert.ToDecimal(SelCurrCodeDS.Tables[0].Rows[0]["已投入面积"]);
                decimal FCOUTAREA = Convert.ToDecimal(SelCurrCodeDS.Tables[0].Rows[0][2]);//分层产出面积
                if (SumInArea <= FCOUTAREA)
                {
                    DFPArea = FCOUTAREA - SumInArea;
                }
                Logger.Debug("存在当前编号", $"编号投入面积汇总{SumInArea}，原膜的产出面积{FCOUTAREA}，未分配的面积{DFPArea}");
            }
            if (UseCode)
            {
                 View.ShowWarnningMessage("此条码已被使用过！");
            }

            if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
            {
                for (int i = 0; i < Ds.Tables[0].Rows.Count; i++)
                {
                    int index = m + i;
         
                    DynamicObject MacObj = this.Model.DataObject["F_SZXY_Mac"] as DynamicObject;
                    this.Model.SetValue("F_SZXY_TFMac", MacObj, index);
                    this.Model.SetValue("F_SZXY_FCDEntryID", Convert.ToString(Ds.Tables[0].Rows[i]["FEntryID"]), m + i);
                    this.Model.SetValue("F_SZXY_PLy", Convert.ToString(Ds.Tables[0].Rows[i]["原膜厚度"]), m + i);
                    if (!Convert.ToString(Ds.Tables[0].Rows[i]["原膜料号"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        //this.Model.SetValue("F_SZXY_Material", Convert.ToString(Ds.Tables[0].Rows[i]["原膜料号"]), m + i);

                        string RMat = Utils.GetRootMatId(Convert.ToString(Ds.Tables[0].Rows[i]["原膜料号"]), orgobj["Id"].ToString(), Context);
                        DynamicObjectCollection entry1 = this.Model.DataObject["SZXY_XYTFHEntry"] as DynamicObjectCollection;
                        DynamicObject Material1 = this.Model.GetValue("F_SZXY_Material", index) as DynamicObject;
                        Utils.SetBaseDataValue(Services, (DynamicObject)entry1[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_Material"), Convert.ToInt64(RMat), ref Material1, Context);
                    } 
                    this.Model.SetValue("F_SZXY_LEN", Convert.ToString(Ds.Tables[0].Rows[i]["原膜长度"]), m + i);
                    this.Model.SetValue("F_SZXY_WIDTH", Convert.ToString(Ds.Tables[0].Rows[i]["原膜宽度"]), m + i);

                    if (!UseCode)
                    {
                        this.Model.SetValue("F_SZXY_Area", Convert.ToString(Ds.Tables[0].Rows[i]["原膜面积"]), m + i);
                    }
                    else
                    {
                        this.Model.SetValue("F_SZXY_Area", DFPArea, m + i);
                    }
               

                    this.Model.SetValue("F_SZXY_Tag", Convert.ToString(Ds.Tables[0].Rows[i]["工位号"]), m + i);

                    if (!Convert.ToString(Ds.Tables[0].Rows[i]["原膜等级"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_OFLevel", Convert.ToString(Ds.Tables[0].Rows[i]["原膜等级"]), m + i);
                    if (!Convert.ToString(Ds.Tables[0].Rows[i]["原膜机台"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_OFMac", Convert.ToString(Ds.Tables[0].Rows[i]["原膜机台"]), m + i);

                    if (!Convert.ToString(Ds.Tables[0].Rows[i]["透气率"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_AP", Convert.ToDecimal(Ds.Tables[0].Rows[i]["透气率"]), m + i);
                    if (!Convert.ToString(Ds.Tables[0].Rows[i]["面密度"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_arealdensity", Convert.ToDecimal(Ds.Tables[0].Rows[i]["面密度"]), m + i);
                    if (!Convert.ToString(Ds.Tables[0].Rows[i]["外观不良"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_BLYY", Convert.ToString(Ds.Tables[0].Rows[i]["外观不良"]), m + i);

                    #region
                    //this.Model.SetValue("F_SZXY_RJHEntryID", Convert.ToDecimal(Ds.Tables[0].Rows[i]["RJHFEntryID"]), index);
                    //this.Model.SetValue("F_SZXY_PTNO", Ds.Tables[0].Rows[i]["生成订单号"].ToString(), index);
                    //this.Model.SetValue("F_SZXY_PuOLineNo", Ds.Tables[0].Rows[i]["生成订单行号"].ToString(), index);
                    // if (!Convert.ToString(Ds.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMark", Ds.Tables[0].Rows[i]["特殊标志"].ToString(), index);

                    // if (!Convert.ToString(Ds.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt32(Ds.Tables[0].Rows[i]["班组"]), index);
                    //if (!Convert.ToString(Ds.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt32(Ds.Tables[0].Rows[i]["班次"]), index);
                    //if (!Convert.ToString(Ds.Tables[0].Rows[i]["产品型号"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_productModel", Convert.ToInt32(Ds.Tables[0].Rows[i]["产品型号"]), index);
                    //if (!Convert.ToString(Ds.Tables[0].Rows[i]["配方"]).IsNullOrEmptyOrWhiteSpace())
                    //{
                    //    IViewService viewService = ServiceHelper.GetService<IViewService>();
                    //    DynamicObject F_SZXY_formulaObj = this.Model.GetValue("F_SZXY_formula", index) as DynamicObject;
                    //    Utils.SetFZZLDataValue(viewService, ((DynamicObjectCollection)this.Model.DataObject["SZXY_XYTFHEntry"])[index], (BaseDataField)this.View.BusinessInfo.GetField("F_SZXY_formula"), ref F_SZXY_formulaObj, Context, Convert.ToString(Ds.Tables[0].Rows[i]["配方"]));
                    //    //this.Model.SetValue("F_SZXY_formula", Convert.ToString(Ds.Tables[0].Rows[i]["配方"]), index);
                    //}
                    //this.Model.SetValue("F_SZXY_PLYL", Convert.ToDecimal(Ds.Tables[0].Rows[i]["厚度"]), index);
                    //this.Model.SetValue("F_SZXY_WidthL", Convert.ToDecimal(Ds.Tables[0].Rows[i]["宽度"]), index);
                    //this.Model.SetValue("F_SZXY_LENL", Convert.ToDecimal(Ds.Tables[0].Rows[i]["长度"]), index);
                    //this.Model.SetValue("F_SZXY_AreaL", Convert.ToDecimal(Ds.Tables[0].Rows[i]["面积"]), index);
                    #endregion
                    this.View.UpdateView("F_SZXY_XYTFEntity");
                    this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                    Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                 
                    this.View.SetEntityFocusRow("F_SZXY_XYTFEntity", m + 1);
                   // this.View.GetControl("F_SZXY_OFNo").SetFocus();

                }
            }
        }


    }
}
