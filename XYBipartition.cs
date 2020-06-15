using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
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
    [Description("对分单据操作。")]
    public class XYBipartition : AbstractBillPlugIn
    {
        public string ASEQ { get; private set; }
        public string BFTYPE { get; private set; }

        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            //，扫描拉伸编号，系统将拉伸编号记录信息携带到XY对分单单据头上，点击【生成对分编号】按钮，按规则生成产品编码（对分编号）。
            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGenerateNo") && billobj["F_SZXY_ProductNo"] != null)
            {

                DynamicObjectCollection entry = billobj["SZXY_DFDEntry"] as DynamicObjectCollection;//单据体
     
                if (entry != null)
                {
                    int curindex = 0;
                    foreach (DynamicObject item in from op in entry
                                                   where !Convert.ToString(op["F_SZXY_bipartitionNO"]).IsNullOrEmptyOrWhiteSpace()
                                                   select op)
                    {
                        curindex++;
                    }
                    bool flag3 = curindex >= 2;
                    if (flag3)
                    {
                        throw new Exception("不允许多次对分！");
                    }
                }


                DynamicObject MacObj = billobj["F_SZXY_plasticMac1"] as DynamicObject;
                string MacId = string.Empty;
                if (MacObj!=null)  MacId = MacObj["ID"].ToString();
              
                string LsNo = Convert.ToString(billobj["F_SZXY_ProductNo"]);
                //
                string F_SZXY_SeqH = Convert.ToString(billobj["F_SZXY_SeqH"]);//层序
                if (!LsNo.IsNullOrEmptyOrWhiteSpace() && billobj["F_SZXY_OrgId"] is DynamicObject orgobj && F_SZXY_SeqH!="")
                {
                    long orgid = Convert.ToInt64(orgobj["Id"]);
                    string orgName = Convert.ToString(orgobj["Name"]);
                    string selMaxSeq = "/*dialect*/select  max(FSeq) MaxSeq,T4.FDATAVALUE,T1.F_SZXY_CirculationState1   from SZXY_t_XYLSEntry T2 " +
                                       " left join SZXY_t_XYLS T1  on T1.Fid = T2.FID " +
                                       "  left join t_bas_assistantdataentry_l T4 on  T4.Fentryid=T1.F_SZXY_BFTYPE " +
                                       $" where T2.F_SZXY_StretchNo = '{LsNo}' " +
                                       $" and (T2.F_SZXY_RollNo like '%-{F_SZXY_SeqH}' or   T2.F_SZXY_RollNo = '{F_SZXY_SeqH}'  ) " +
                                       $" and T1.F_SZXY_OrgId = {orgid}" +
                                       $" group by  T1.F_SZXY_CirculationState1,T4.FDATAVALUE ";
                    DataSet MaxDs = Utils.CBData(selMaxSeq, Context);
                    string seq = "";
                    if (MaxDs!=null&& MaxDs.Tables.Count>0&& MaxDs.Tables[0].Rows.Count>0)
                    {
                        seq = MaxDs.Tables[0].Rows[0]["MaxSeq"].ToString();
                        ASEQ = seq;
                        BFTYPE = MaxDs.Tables[0].Rows[0]["FDATAVALUE"].ToString();
                        string  F_SZXY_CirculationState1 = MaxDs.Tables[0].Rows[0]["F_SZXY_CirculationState1"].ToString();
                        if (F_SZXY_CirculationState1=="1")
                        {
                            throw new Exception("此编码已经过对分");
                        }
                    }
                    else
                    {
                        throw new Exception("没有匹配到数据，请检查录入字段");
                    }
                    //对分层次 江苏要头尾  深圳不算 扣除上面表调层
                    string ISZF = "";
                    if (orgName.Contains("深圳"))
                    {
                        ISZF = " and F_SZXY_IsZF != N'作废' ";
                    }
                    if (seq != "")
                    {
                      
                        string sql = $"/*dialect*/select  T1.F_SZXY_PTNoH '生产任务单号',T1.F_SZXY_MOID '生产订单内码'," +
                                     $"SUM(T2.F_SZXY_AREAE)  '汇总面积' ,Count(FSeq) '对分层数'," +
                                     $"  T1.F_SZXY_POLineNum '生产订单行号'," +
                                     $"  T1.F_SZXY_MaterialID  '产品型号',T1.F_SZXY_CJ '车间'," +
                                     $" T1.F_SZXY_TeamGroup '班组',T1.F_SZXY_Classes '班次'," +
                                     "  T1.F_SZXY_SpecialMarkH '特殊标志',T1.F_SZXY_Formula '配方' ," +
                                     "  T1.F_SZXY_Layer '层数', T1.F_SZXY_AreaH '面积' , " +
                                     "  T2.F_SZXY_Width '宽度', T2.F_SZXY_Len  '长度'," +
                                     " T1.F_SZXY_plasticMac '流延机' " +
                                     $" from SZXY_t_XYLS T1  left join SZXY_t_XYLSEntry T2 on T1.Fid=T2.Fid  " +
                                     $" where T2.F_SZXY_StretchNO='{LsNo}'" +
                                     $" and  T1.F_SZXY_OrgId={orgid}" +
                                     $" and T2.FSeq<={seq} " +
                                     $" {ISZF} " +
                                     $" group by T1.F_SZXY_PTNoH,T1.F_SZXY_POLineNum,F_SZXY_MaterialID,"+
                                     "F_SZXY_SpecialMarkH,F_SZXY_Formula,F_SZXY_Layer,F_SZXY_Width,T1.F_SZXY_CJ," +
                                     "F_SZXY_Len,F_SZXY_plasticMac, T1.F_SZXY_AreaH,F_SZXY_TeamGroup,F_SZXY_Classes,T1.F_SZXY_MOID " +

                                     " union all "+

                                     $"select  T1.F_SZXY_PTNoH '生产任务单号', T1.F_SZXY_MOID '生产订单内码'," +
                                     $"SUM(T2.F_SZXY_AREAE)  '汇总面积' ,Count(FSeq) '对分层数'," +
                                     $"  T1.F_SZXY_POLineNum '生产订单行号'," +
                                     $"  T1.F_SZXY_MaterialID  '产品型号',T1.F_SZXY_CJ '车间'," +
                                     $" T1.F_SZXY_TeamGroup '班组',T1.F_SZXY_Classes '班次'," +
                                     "  T1.F_SZXY_SpecialMarkH '特殊标志',T1.F_SZXY_Formula '配方' ," +
                                     "  T1.F_SZXY_Layer '层数', T1.F_SZXY_AreaH '面积' , " +
                                     "  T2.F_SZXY_Width '宽度', T2.F_SZXY_Len  '长度'," +
                                     " T1.F_SZXY_plasticMac '流延机' " +
                                     $" from SZXY_t_XYLS T1  left join SZXY_t_XYLSEntry T2 on T1.Fid=T2.Fid  " +
                                     $" where T2.F_SZXY_StretchNO='{LsNo}'" +
                                     $" and  T1.F_SZXY_OrgId={orgid}" +
                                     $" and T2.FSeq >{seq} " +
                                     $"  {ISZF} "  +
                                     $" group by T1.F_SZXY_PTNoH,T1.F_SZXY_POLineNum,F_SZXY_MaterialID," +
                                     "F_SZXY_SpecialMarkH,F_SZXY_Formula,F_SZXY_Layer,F_SZXY_Width,T1.F_SZXY_CJ," +
                                     "F_SZXY_Len,F_SZXY_plasticMac, T1.F_SZXY_AreaH,F_SZXY_TeamGroup,F_SZXY_Classes,T1.F_SZXY_MOID ";

                        DataSet dsB = Utils.CBData(sql, Context);
                      
                        SetValue(dsB, LsNo);
                    }
           
                }


           
            }
            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {

                this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject OrgObj)
                {
                    long orgid = Convert.ToInt64(OrgObj["Id"]);

                    Utils.TYPrint(this.View, Context, orgid);
 
                    DynamicObjectCollection entry2 = this.Model.DataObject["SZXY_DFDEntry"] as DynamicObjectCollection;
                    bool flag15 = entry2 != null;
                    if (flag15)
                    {
                        foreach (DynamicObject item2 in from ck in entry2
                                                        where Convert.ToString(ck["F_SZXY_PRINT"]).EqualsIgnoreCase("true")
                                                        select ck)
                        {
                            item2["F_SZXY_PRINT"] = "false";
                        }
                        base.View.UpdateView("F_SZXY_DFDEntity");
                        //billobj["FFormId"] = base.View.BusinessInfo.GetForm().Id;
                        //IOperationResult a = Utils.Save(base.View, new DynamicObject[]
                        //{
                        //  billobj
                        //}, OperateOption.Create(), base.Context);
                    }
                }
            }
        }

        private void SetValue(DataSet LsDSA, string LsNo)
        {
            string F_SZXY_SeqH = Convert.ToString(this.Model.GetValue("F_SZXY_SeqH"));//层序
            string cx1 = "";
            string cx2 = "";
            if (this.Model.GetValue("F_SZXY_OrgId") is DynamicObject orgobj)
            {
                cx1 = $"1-{F_SZXY_SeqH}";
                cx2 = $"{Convert.ToInt32(F_SZXY_SeqH) + 1}-{  Convert.ToString(LsDSA.Tables[0].Rows[0]["层数"])}";

                bool IsRenZ = false;
                int RZQTY = 0;
            if (LsDSA != null && LsDSA.Tables.Count > 0 && LsDSA.Tables[0].Rows.Count > 0)
            {
                int m = this.Model.GetEntryRowCount("F_SZXY_DFDEntity");
                for (int i = 0; i < LsDSA.Tables[0].Rows.Count; i++)
                {
                    int index = m + i;
                    if (i == 0)
                    {
                        //给单据头赋值
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["生产订单内码"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_MOID", Convert.ToInt64(LsDSA.Tables[0].Rows[i]["生产订单内码"]));
                        this.Model.SetValue("F_SZXY_PTNoH", LsDSA.Tables[0].Rows[i]["生产任务单号"].ToString());
                        this.Model.SetValue("F_SZXY_POLineNum", LsDSA.Tables[0].Rows[i]["生产订单行号"].ToString());
                        if (!Convert.ToString( LsDSA.Tables[0].Rows[i]["特殊标志"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_SpecialMarkH", Convert.ToString(LsDSA.Tables[0].Rows[i]["特殊标志"]));
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["班组"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_TeamGroup", Convert.ToInt64(LsDSA.Tables[0].Rows[i]["班组"]));
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["班次"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Classes", Convert.ToInt64(LsDSA.Tables[0].Rows[i]["班次"]));
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["配方"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_FormulaH", Convert.ToString(LsDSA.Tables[0].Rows[i]["配方"]));
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["产品型号"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            string RMat = Utils.GetRootMatId(Convert.ToString(LsDSA.Tables[0].Rows[i]["产品型号"]), orgobj["Id"].ToString(), Context);
                            this.Model.SetValue("F_SZXY_MaterialID1", RMat);
                                if (this.Model.GetValue("F_SZXY_MaterialID1") is DynamicObject MatDo)
                                {
                                    if (!Convert.ToString(MatDo["F_SZXY_renzenQTY"]).IsNullOrEmptyOrWhiteSpace())
                                    {
                                             IsRenZ = true;
                                             RZQTY = Convert.ToInt32(MatDo["F_SZXY_renzenQTY"]);
                                    }
                                }
                        }
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["流延机"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_plasticMac", Convert.ToString(LsDSA.Tables[0].Rows[i]["流延机"]));
                        this.Model.SetValue("F_SZXY_AreaH", Convert.ToDecimal(LsDSA.Tables[0].Rows[i]["面积"]));
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["车间"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CJ", Convert.ToInt64(LsDSA.Tables[0].Rows[i]["车间"]));

                        DynamicObject FCreatorIdobj = this.Model.GetValue("FCreatorId") as DynamicObject;
                        if (!Convert.ToString(FCreatorIdobj["Id"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Recorder_Id", Convert.ToString(FCreatorIdobj["Id"]));
                        if (!Convert.ToString(LsDSA.Tables[0].Rows[i]["层数"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Layer", Convert.ToString(LsDSA.Tables[0].Rows[i]["层数"]));
                        DynamicObject billobj = this.Model.DataObject; 
                    }
                    //单据体赋值
                    this.Model.CreateNewEntryRow("F_SZXY_DFDEntity");
                    this.Model.SetValue("F_SZXY_StretchNO", LsNo, index); 
                    this.Model.SetValue("F_SZXY_bipartitionWidth", Convert.ToDecimal(LsDSA.Tables[0].Rows[i]["宽度"]), index);
                    this.Model.SetValue("F_SZXY_bipartitionLen", Convert.ToDecimal(LsDSA.Tables[0].Rows[i]["长度"]), index);
                    this.Model.SetValue("F_SZXY_bipartitionArea", Convert.ToDecimal(LsDSA.Tables[0].Rows[i]["汇总面积"]), index);
                    this.Model.SetValue("F_SZXY_bipartition", Convert.ToString(LsDSA.Tables[0].Rows[i]["对分层数"]), index);
                    this.Model.SetValue("F_SZXY_StretchC", cx1, index);
                    string DFNo = "";//拉伸编号+拉伸层次
                    if (cx1!=""&&  i==0)
                    {

                        this.Model.SetValue("F_SZXY_StretchC", cx1, index);
                        //this.Model.SetValue("F_SZXY_bipartition", Convert.ToInt32(this.Model.GetValue("F_SZXY_SeqH")));
                        DFNo = $"{LsNo}{cx1}";
                         
                        this.Model.SetValue("F_SZXY_bipartitionNO", DFNo, index);
                    }
                    if (cx2 != "" &&  i == 1)
                    {
                        this.Model.SetValue("F_SZXY_StretchC", cx2, index);
                        //this.Model.SetValue("F_SZXY_bipartition", Convert.ToInt32(this.Model.GetValue("F_SZXY_SeqH")));
                        DFNo = $"{LsNo}{cx2}";
                            this.Model.SetValue("F_SZXY_bipartitionNO", DFNo, index);

                            if (RZQTY > 0 && IsRenZ&&BFTYPE.EqualsIgnoreCase("单层"))
                            {
                                string b1 = "";
                                string b2 = "";
                                //string LsNo = Convert.ToString(billobj["F_SZXY_ProductNo"]);
                                string Before= Convert.ToString( Convert.ToInt32( ASEQ.ToString())+1);
                                string selsql = $"select F_SZXY_RollNo from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO='{LsNo}' and FSeq={Before} ";
                                DataSet Ds1 = DBServiceHelper.ExecuteDataSet(Context, selsql);
                                if (Ds1!=null&&Ds1.Tables.Count>0&& Ds1.Tables[0].Rows.Count>0)
                                {
                                   b1=  Convert.ToString( Ds1.Tables[0].Rows[0]["F_SZXY_RollNo"]);
                                }
                                string selsql1 = $"select top 1 F_SZXY_RollNo from SZXY_t_XYLSEntry where F_SZXY_STRETCHNO='{LsNo}' order by FSeq Desc";
                                DataSet Ds11 = DBServiceHelper.ExecuteDataSet(Context, selsql1);
                                if (Ds11 != null && Ds11.Tables.Count > 0 && Ds11.Tables[0].Rows.Count > 0)
                                {
                                    b2 = Convert.ToString(Ds11.Tables[0].Rows[0]["F_SZXY_RollNo"]);
                                   // b2 = String.Format("{0:D2}", Convert.ToInt32( b2));
                                }
                                cx2= $"{b1}-{b2}";
                                DFNo = $"{LsNo}{cx2}";
                                this.Model.SetValue("F_SZXY_StretchC", cx2, index);
                            }
                            this.Model.SetValue("F_SZXY_bipartitionNO", DFNo, index);
                        }
                   
                }
                //调用保存
                //
                string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYLS set  F_SZXY_CirculationState1={0}   where F_SZXY_RecNo='{1}' ", 1, LsNo);
                int res = DBServiceHelper.Execute(Context, UpdateStateSql);


                View.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                Utils.Save(View, new DynamicObject[] { View.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);
                this.View.UpdateView();
            }
            else this.View.ShowMessage("此编号没有匹配到数据！");
            //if (LsDSB != null && LsDSB.Tables.Count > 0 && LsDSB.Tables[0].Rows.Count > 0)
            //{
            //    int m = this.Model.GetEntryRowCount("F_SZXY_DFDEntity");
            //    for (int i = 0; i < LsDSB.Tables[0].Rows.Count; i++)
            //    {
            //        int index = m + i;
            //        if (i == 0)
            //        //单据体赋值
            //        this.Model.CreateNewEntryRow("F_SZXY_DFDEntity");
            //        this.Model.SetValue("F_SZXY_StretchNO", LsNo, index);
            //        this.Model.SetValue("F_SZXY_bipartitionWidth", Convert.ToDecimal(LsDSB.Tables[0].Rows[i]["宽度"]), index);
            //        this.Model.SetValue("F_SZXY_bipartitionLen", Convert.ToDecimal(LsDSB.Tables[0].Rows[i]["长度"]), index);
            //        this.Model.SetValue("F_SZXY_bipartitionArea", Convert.ToDecimal(LsDSB.Tables[0].Rows[i]["汇总面积"]), index);
            //       // this.Model.SetValue("F_SZXY_bipartition", Convert.ToString(LsDSB.Tables[0].Rows[i]["对分层数"]), index);
            //        this.Model.SetValue("F_SZXY_StretchC", cx2, index);
            //        string DFNo = "";//拉伸编号+拉伸层次
            //        if (cx2!= "")
            //        {
            //            string[] strArr = cx2.Split('-');
            //            int A = Convert.ToInt32(strArr[0]);
            //            int B = Convert.ToInt32(strArr[1]);
            //            this.Model.SetValue("F_SZXY_bipartition", Convert.ToInt32(this.Model.GetValue("F_SZXY_SeqH")) - B);
            //            DFNo = $"{LsNo}{cx2}";
            //        }

            //        this.Model.SetValue("F_SZXY_bipartitionNO", DFNo, index);

            //    }

            //}
        }
        }
    }
}
