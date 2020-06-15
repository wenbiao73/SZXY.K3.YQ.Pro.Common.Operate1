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
    [Description("生产领料单自动扫码填充批号。")]
    public class XYPickMtrl:AbstractBillPlugIn
    {
        private int CSRowCount;

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
             CSRowCount = this.Model.GetEntryRowCount("FEntity");
            //int mm = 2;
            //this.Model.BatchCreateNewEntryRow("FEntity", mm);
            this.View.GetControl<EntryGrid>("FEntity").SetEnterMoveNextColumnCell(true);
            this.View.UpdateView("FEntity");

        }

        public override void AfterCreateNewEntryRow(CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            
            
            this.View.GetControl<EntryGrid>("FEntity").SetEnterMoveNextColumnCell(true);
        }
      

        public override void AfterCopyRow(AfterCopyRowEventArgs e)
        {
            base.AfterCopyRow(e);
            this.Model.SetValue("FActualQty", 0, e.NewRow);

            this.Model.SetValue("F_SZXY_LYENTRYID", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_SFENTRYID", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_FHFNM", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_LSFNM", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_FCFNM", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_FQFNM", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_TFHNM", 0, e.NewRow);
            this.Model.SetValue("F_SZXY_JLFNM", 0, e.NewRow);
 
            this.View.GetControl<EntryGrid>("FEntity").SetEnterMoveNextColumnCell(true);
           
            this.View.SetEntityFocusRow("FEntity", e.NewRow);
            this.View.GetControl("F_SZXY_GXCode").SetFocus();
            this.View.UpdateView("FEntity");
        }

        
      

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            DynamicObject billobj = this.Model.DataObject;

         
            int m = e.Row;

            int BactQty = 1;
            if (e.Field.Key.EqualsIgnoreCase("F_SZXY_GXCode"))
            {
                string GXCode = Convert.ToString(this.Model.GetValue("F_SZXY_GXCode", m));//获取输入的编号

                if (this.Model.GetValue("FPrdOrgId") is DynamicObject orgdo && !GXCode.IsNullOrEmptyOrWhiteSpace())
                {
                    string orgId = Convert.ToString(orgdo["Id"]);
                    string BARCODESQL = "/*dialect*/select distinct 'BZ' FLINK  from  SZXY_t_BZDHEntry  where F_SZXY_CTNNO='" + GXCode + "'";

                    BARCODESQL += " union ";

                    BARCODESQL += "/*dialect*/select distinct  'DF' FLINK  from SZXY_t_DFDEntry where F_SZXY_BIPARTITIONNO='" + GXCode + "'";

                    BARCODESQL += " union ";

                    BARCODESQL += "/*dialect*/select distinct  'SF' FLINK from SZXY_t_SFDEntry  where F_SZXY_SFNO='" + GXCode + "'";

                    BARCODESQL += " union ";

                    BARCODESQL += "/*dialect*/select distinct  'TF' FLINK  from SZXY_t_XYTFEntry where F_SZXY_COATCODE='" + GXCode + "'";

                    string FBARCODE = GXCode;

                    DataSet BARCODEData = DBServiceHelper.ExecuteDataSet(this.Context, BARCODESQL);

                    if (BARCODEData != null && BARCODEData.Tables.Count > 0 && BARCODEData.Tables[0].Rows.Count > 0)
                    {
                        BactQty++;
                        string FLINK = Convert.ToString(BARCODEData.Tables[0].Rows[0]["FLINK"]);



                        if (!FLINK.IsNullOrEmptyOrWhiteSpace())
                        {
                            string SQL = "";

                            DataSet fillData = null;

                            //包装

                            if (FLINK.EqualsIgnoreCase("BZ"))
                            {
                                //
                                SQL = "/*dialect*/select t1.F_SZXY_MATERIAL,t1.F_SZXY_PLY,t1.F_SZXY_WIDTH '宽度',t1.F_SZXY '长度',t1.F_SZXY_MANDREL,t1.F_SZXY_CUSTID," +
                                    "t1.F_SZXY_PUDNO,t1.F_SZXY_CUSTNO,t1.F_SZXY_CUSTBACTH,t1.F_SZXY_PUDLINENO,t18.FID,t2.FBASEUNITID,t1.F_SZXY_SOSEQ1" +
                                    ",t1.F_SZXY_SOENTRYID1,t8.FENTRYID,t3.F_SZXY_CJ,sum(t1.F_SZXY_AREA1) '面积' ,sum(t1.F_SZXY_JQTY)  BOXCount  " +
                                    "from  SZXY_t_BZDHEntry t1 join SZXY_t_BZD t3 on t1.FID=t3.FID  " +
                                    "left join t_BD_MaterialBase t2 on t1.F_SZXY_MATERIAL=t2.FMATERIALID  " +
                                    "left join T_PRD_MO t18 on t1.F_SZXY_PUDNO=t18.FBILLNO " +
                                    "left join T_PRD_MOENTRY t8 on t18.FID=t8.FID and t8.FSEQ=t1.F_SZXY_PUDLINENO  " +
                                    "where t1.F_SZXY_CTNNO='" + FBARCODE + "' " +
                                    "group by t1.F_SZXY_MATERIAL,t1.F_SZXY_PLY,t1.F_SZXY_WIDTH,t1.F_SZXY,t1.F_SZXY_MANDREL,t1.F_SZXY_CUSTID,t1.F_SZXY_PUDNO,t1.F_SZXY_CUSTNO,t1.F_SZXY_CUSTBACTH,t1.F_SZXY_PUDLINENO,t18.FID,t2.FBASEUNITID,t1.F_SZXY_SOSEQ1,t1.F_SZXY_SOENTRYID1,t8.FENTRYID,t3.F_SZXY_CJ ";
                                fillData = DBServiceHelper.ExecuteDataSet(this.Context, SQL);

                            }
                            //对分
                            else if (FLINK.EqualsIgnoreCase("DF"))
                            {

                                SQL = "/*dialect*/select t1.F_SZXY_BIPARTITIONWIDTH '宽度',t1.F_SZXY_BIPARTITIONLEN '长度',t1.F_SZXY_BIPARTITIONAREA '面积'," +
                                    "t2.F_SZXY_PTNOH,t2.F_SZXY_MATERIALID1,t2.F_SZXY_MOID,t2.F_SZXY_POLINENUM ,t3.FBASEUNITID,t3.FHEIGHT," +
                                    "t2.F_SZXY_CJ " +
                                    "from SZXY_t_DFDEntry t1 join SZXY_t_DFD t2  on t1.FID=t2.FID" +
                                    " left join t_BD_MaterialBase t3 on t2.F_SZXY_MATERIALID1=t3.FMATERIALID" +
                                    "  where t1.F_SZXY_BIPARTITIONNO='" + FBARCODE + "'" +
                                 
                                    " union " +
                                    " select " +
                                    " t1.F_SZXY_WIDTHH as '宽度',t1.F_SZXY_LENH as '长度',t1.F_SZXY_AREAH as '面积',F_SZXY_PTNOH,F_SZXY_MATERIALID as F_SZXY_MATERIALID1,F_SZXY_MOID,F_SZXY_POLINENUM,t3.FBASEUNITID,t3.FHEIGHT,t1.F_SZXY_CJ" +
                                    " from SZXY_t_XYLS t1" +
                                    " left join t_BD_MaterialBase t3 on t1.F_SZXY_MATERIALID = t3.FMATERIALID" +
                                    $" where t1.F_SZXY_RECNO = '{FBARCODE}'";

                                fillData = DBServiceHelper.ExecuteDataSet(this.Context, SQL);

                            }
                            //湿法
                            else if (FLINK.EqualsIgnoreCase("SF"))
                            {
                                SQL = "/*dialect*/select  t1.F_SZXY_MATERIALID,t1.F_SZXY_PLY,t1.F_SZXY_WIDTH '宽度',t1.F_SZXY_LEN '长度',t1.F_SZXY_AREA '面积'," +
                                    "t1.F_SZXY_PUONO,t2.F_SZXY_MOID,t1.F_SZXY_PUOLINENO,t3.FBASEUNITID,t2.F_SZXY_CJ " +
                                    "from SZXY_t_SFDEntry t1 join SZXY_t_SFD t2 on t1.FID=t2.FID" +
                                    "  left join t_BD_MaterialBase t3 on t1.F_SZXY_MATERIALID=t3.FMATERIALID" +
                                    "  where t1.F_SZXY_SFNO='" + FBARCODE + "'";
                                fillData = DBServiceHelper.ExecuteDataSet(this.Context, SQL);
                            }
                            //涂覆
                            else if (FLINK.EqualsIgnoreCase("TF"))
                            {
                                SQL = "/*dialect*/select t1.F_SZXY_PRODUCTMODEL,t1.F_SZXY_PLYL,t1.F_SZXY_WIDTHL '宽度',t1.F_SZXY_LENL '长度',t1.F_SZXY_AREAL '面积'," +
                                    "t1.F_SZXY_PTNO,t1.F_SZXY_PUOLINENO,t2.F_SZXY_MOID,t3.FBASEUNITID,t2.F_SZXY_CJ" +
                                    "  from SZXY_t_XYTFEntry t1 join SZXY_t_XYTF t2 on t1.FID=t2.FID " +
                                    "left join t_BD_MaterialBase t3 on t1.F_SZXY_PRODUCTMODEL=t3.FMATERIALID " +
                                    "where t1.F_SZXY_COATCODE='" + FBARCODE + "'";
                                fillData = DBServiceHelper.ExecuteDataSet(this.Context, SQL);
                            }

                            string Stockid = "";
                            string StockLocid = "";

                            string selStockSql = $"/*dialect*/select FSTOCKID,FSTOCKLOCID from T_STK_INVENTORY m "+
                                         " join T_BD_LOTMASTER n on  m.FLOT = n.FLOTID " +
                                         $" where n.FNUMBER = '{GXCode}' and m.FBASEQTY>0  ";
                            DataSet selStockDS= DBServiceHelper.ExecuteDataSet(this.Context, selStockSql);
                            if (selStockDS != null && selStockDS.Tables.Count > 0 && selStockDS.Tables[0].Rows.Count > 0)
                            {
                                Stockid= Convert.ToString(selStockDS.Tables[0].Rows[0][0]);
                                StockLocid = Convert.ToString(selStockDS.Tables[0].Rows[0][1]);
                            }

                            if (fillData != null && fillData.Tables.Count > 0 && fillData.Tables[0].Rows.Count > 0)
                            {
                                string value = "";

                                int mm = 0;

                                if (FLINK.EqualsIgnoreCase("BZ"))
                                {

                                    for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                                    {
                                        mm = m + i;

    
                                        this.Model.SetValue("FLot", GXCode, mm);
                                        this.View.UpdateView("FLot", mm);
 
                                        if (!Stockid.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("FStockId", Stockid, mm);

                                        int currindex= this.Model.GetEntryCurrentRowIndex("FEntity");

                                        DynamicObjectCollection entry= this.Model.DataObject["Entity"] as DynamicObjectCollection;

                                        if (!StockLocid.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            IViewService viewService = ServiceHelper.GetService<IViewService>();
                                            DynamicObject FStockLocIddo = this.Model.GetValue("FStockLocId") as DynamicObject;
                                      
                                           
                                            Utils.SetFlexDataValue(viewService, entry[mm], (RelatedFlexGroupField)this.View.BusinessInfo.GetField("FStockLocId"), Convert.ToInt64(StockLocid), ref FStockLocIddo, Context);
                                            this.View.UpdateView("FStockLocId", mm);
                                         
                                        }
                                    

                                        //宽度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i][2]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("宽度", value, mm);
                                        this.View.UpdateView("F_SZXY_KD", mm);

                                        //长度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["长度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CD", value, mm);
                                        this.View.UpdateView("F_SZXY_CD", mm);

                                        //面积
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["面积"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            this.Model.SetValue("FAppQty", value, mm);
                                            this.Model.SetValue("FActualQty", value, mm);
                                            this.View.UpdateView("FAppQty", mm);
                                            this.View.UpdateView("FActualQty", mm);
                                        }
                               

                                    }


                                }

                                else if (FLINK.EqualsIgnoreCase("DF"))
                                {

                                    for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                                    {
                                        mm = m + i;


                                        this.Model.SetValue("FLot", GXCode, mm);
                                        this.View.UpdateView("FLot", mm);

                                        if (!Stockid.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("FStockId", Stockid, mm);

                                        int currindex = this.Model.GetEntryCurrentRowIndex("FEntity");

                                        DynamicObjectCollection entry = this.Model.DataObject["Entity"] as DynamicObjectCollection;

                                        if (!StockLocid.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            IViewService viewService = ServiceHelper.GetService<IViewService>();
                                            DynamicObject FStockLocIddo = this.Model.GetValue("FStockLocId") as DynamicObject;


                                            Utils.SetFlexDataValue(viewService, entry[mm], (RelatedFlexGroupField)this.View.BusinessInfo.GetField("FStockLocId"), Convert.ToInt64(StockLocid), ref FStockLocIddo, Context);
                                            this.View.UpdateView("FStockLocId", mm);

                                        }


                                        //宽度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["宽度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_KD", value, mm);
                                        this.View.UpdateView("F_SZXY_KD", mm);

                                        //长度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["长度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CD", value, mm);
                                        this.View.UpdateView("F_SZXY_CD", mm);

                                        //面积
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["面积"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            this.Model.SetValue("FAppQty", value, mm);
                                            this.Model.SetValue("FActualQty", value, mm);
                                            this.View.UpdateView("FAppQty", mm);
                                            this.View.UpdateView("FActualQty", mm);
                                        }


                                    }
 

                                }

                                else if (FLINK.EqualsIgnoreCase("SF"))
                                {
                                    for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                                    {
                                        mm = m + i;


                                        this.Model.SetValue("FLot", GXCode, mm);
                                        this.View.UpdateView("FLot", mm);

                                        if (!Stockid.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("FStockId", Stockid, mm);

                                        int currindex = this.Model.GetEntryCurrentRowIndex("FEntity");

                                        DynamicObjectCollection entry = this.Model.DataObject["Entity"] as DynamicObjectCollection;

                                        if (!StockLocid.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            IViewService viewService = ServiceHelper.GetService<IViewService>();
                                            DynamicObject FStockLocIddo = this.Model.GetValue("FStockLocId") as DynamicObject;


                                            Utils.SetFlexDataValue(viewService, entry[mm], (RelatedFlexGroupField)this.View.BusinessInfo.GetField("FStockLocId"), Convert.ToInt64(StockLocid), ref FStockLocIddo, Context);
                                            this.View.UpdateView("FStockLocId", mm);

                                        }


                                        //宽度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["宽度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_KD", value, mm);
                                        this.View.UpdateView("F_SZXY_KD", mm);

                                        //长度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["长度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CD", value, mm);
                                        this.View.UpdateView("F_SZXY_CD", mm);

                                        //面积
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["面积"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            this.Model.SetValue("FAppQty", value, mm);
                                            this.Model.SetValue("FActualQty", value, mm);
                                            this.View.UpdateView("FAppQty", mm);
                                            this.View.UpdateView("FActualQty", mm);
                                        }


                                    }

                                }

                                else if (FLINK.EqualsIgnoreCase("TF"))
                                {

                                    for (int i = 0; i < fillData.Tables[0].Rows.Count; i++)
                                    {
                                        mm = m + i;


                                        this.Model.SetValue("FLot", GXCode, mm);
                                        this.View.UpdateView("FLot", mm);

                                        if (!Stockid.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("FStockId", Stockid, mm);

                                        int currindex = this.Model.GetEntryCurrentRowIndex("FEntity");

                                        DynamicObjectCollection entry = this.Model.DataObject["Entity"] as DynamicObjectCollection;

                                        if (!StockLocid.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            IViewService viewService = ServiceHelper.GetService<IViewService>();
                                            DynamicObject FStockLocIddo = this.Model.GetValue("FStockLocId") as DynamicObject;


                                            Utils.SetFlexDataValue(viewService, entry[mm], (RelatedFlexGroupField)this.View.BusinessInfo.GetField("FStockLocId"), Convert.ToInt64(StockLocid), ref FStockLocIddo, Context);
                                            this.View.UpdateView("FStockLocId", mm);

                                        }


                                        //宽度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["宽度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_KD", value, mm);
                                        this.View.UpdateView("F_SZXY_KD", mm);

                                        //长度
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["长度"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CD", value, mm);
                                        this.View.UpdateView("F_SZXY_CD", mm);

                                        //面积
                                        value = Convert.ToString(fillData.Tables[0].Rows[i]["面积"]);

                                        if (!value.IsNullOrEmptyOrWhiteSpace())
                                        {
                                            this.Model.SetValue("FAppQty", value, mm);
                                            this.Model.SetValue("FActualQty", value, mm);
                                            this.View.UpdateView("FAppQty", mm);
                                            this.View.UpdateView("FActualQty", mm);
                                        }


                                    }

                                }
                            }



                        }

                    }

                    int Entrycount = this.Model.GetEntryRowCount("FEntity"); ;
                    //if (m== Entrycount - 2)
                    //{
                    //    this.Model.BatchCreateNewEntryRow("FEntity", 1);
                    //    Entrycount = this.Model.GetEntryRowCount("FEntity"); ;
                    //    this.View.Model.CopyEntryRow("FEntity", m, Entrycount - 1, true);
                    //    this.View.UpdateView("FEntity");
                    //    this.View.GetControl<EntryGrid>("FEntity").SetEnterMoveNextColumnCell(true);
                    //}

                    if (m == Entrycount -1)
                    {
              
                        Entrycount = this.Model.GetEntryRowCount("FEntity"); ;
                        this.View.Model.CopyEntryRow("FEntity", m, m+1, true);
                       
                        this.View.GetControl<EntryGrid>("FEntity").SetEnterMoveNextColumnCell(true);
                    }




                    this.View.SetEntityFocusRow("FEntity", e.Row + 1);
                }
            }
             
           



        }
    }
}
