using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("机台日计划判断机台和时间是否已存在")]
    public class DayPlanDateControl : AbstractBillPlugIn
    {
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            if (e.Operation.FormOperation.Id.EqualsIgnoreCase("Save") || e.Operation.FormOperation.Id.EqualsIgnoreCase("submit"))
            {
                this.View.UpdateView();
                string formid = this.View.BusinessInfo.GetForm().Id;
                DynamicObject billob = this.Model.DataObject;


                if (billob != null && !formid.EqualsIgnoreCase("SZXY_BZRSCJH")&& !formid.EqualsIgnoreCase("SZXY_FCJTRSCJH"))
                {
                    Dictionary<string, List<string>> dir = new Dictionary<string, List<string>>() { };
                    dir.Clear();
                    string BillNo = Convert.ToString(billob["FBillNo"]);
                    if (billob[GetFieldsKey(formid)["FEntry"]] is DynamicObjectCollection Entrys)
                    {

                        DateTime EtimePre;
                        int index = 0;
                        foreach (DynamicObject row in Entrys)
                        {
                            string seq = Convert.ToString(row["Seq"]);
                            DateTime Stime = Convert.ToDateTime(row[GetFieldsKey(formid)["Time"]]);
                            DateTime Etime = Convert.ToDateTime(row[GetFieldsKey(formid)["ETime"]]);
                          if(this.Model.GetValue(GetFieldsKey(formid)["ETime"], index - 1) != null) 
                            {
                                EtimePre = Convert.ToDateTime(this.Model.GetValue(GetFieldsKey(formid)["ETime"], index - 1));
                                if (Stime== EtimePre)
                                {
                                    throw new Exception($"开始时间不能等于上一行结束时间！！");
                                }
                            }
                            if (row[GetFieldsKey(formid)["Mac"]] is DynamicObject Macobj)
                            {
                                //if (Stime== Etime)
                                //{
                                //    throw new Exception($"第{seq}行，开始时间不能等于结束时间！！");
                                //}
                                int S = DateTime.Compare(Stime, Etime);
                                if (S > 0)
                                {
                                    throw new Exception($"第{seq}行，开始时间不能大于结束时间！！");
                                }
                                else if (S == 0)
                                {
                                    throw new Exception($"第{seq}行，开始时间不能等于结束时间！！");
                                }
                                int E = DateTime.Compare(Etime, Stime);
                                if (E < 0)
                                {
                                    throw new Exception($"第{seq}行，结束时间不能大于开始时间！！");
                                }
                                List<string> TimeList = new List<string>();
                                TimeList.Clear();
                                string MacId = Macobj["Id"].ToString();
                                if (Stime != DateTime.MinValue && Etime != DateTime.MinValue)
                                {
                                    TimeList.Add(DateTimeToStamp(Stime));
                                    TimeList.Add(DateTimeToStamp(Etime));
                                    TimeList.Add(MacId);
                                    if (TimeList != null)
                                    {
                                        dir.Add(row["Seq"].ToString(), TimeList);
                                    }
                                }
                            }
                            else throw new Exception("机台不允许为空！！");
                            index++;
                        }

                        foreach (DynamicObject row in Entrys)
                        {
                            string seq = Convert.ToString(row["Seq"]);
                            DateTime Stime = Convert.ToDateTime(row[GetFieldsKey(formid)["Time"]]);

                            DateTime Etime = Convert.ToDateTime(row[GetFieldsKey(formid)["ETime"]]);
                             string FEntryId = Convert.ToString(row["Id"]);
                            DynamicObject Macobj = row[GetFieldsKey(formid)["Mac"]] as DynamicObject;

                            if (Macobj != null)
                            {
                                string MacId = Macobj["Id"].ToString();

                                if (dir != null && dir.Count > 0)
                                {
                                    foreach (var mac in dir)
                                    {
                                        string rewSeq = mac.Key.ToString();
                                        if (rewSeq != seq)
                                        {
                                            long instime = Convert.ToInt64(Convert.ToString(DateTimeToStamp(Stime)));//输入的开始时间
                                            long inetime = Convert.ToInt64(Convert.ToString(DateTimeToStamp(Etime))); //输入的结束时间

                                            List<string> TimeList1 = mac.Value;
                                            long fromdate = Convert.ToInt64(TimeList1[0]);
                                            long todate = Convert.ToInt64(TimeList1[1]);
                                            string fromMac = Convert.ToString(TimeList1[2]);
                                            if (fromMac == MacId)
                                            {
                                                if (fromdate < instime && instime < todate)
                                                {
                                                    throw new Exception("第" + rewSeq + "行，同一机台的开始时间结束时间在此单据中存在冲突!");
                                                }
                                            }
                                        }
                                    }

                                }
                                string Pjsql = "";
                                if (!FEntryId.IsNullOrEmptyOrWhiteSpace())
                                {
                                      Pjsql = $" and T2.FEntryId !={ FEntryId} ";
                                }
                                DynamicObject orgobj = billob[GetFieldsKey(formid)["Org"]] as DynamicObject;
                                if (orgobj == null) throw new Exception("业务组织不允许为空");
                                long orgid = Convert.ToInt64(orgobj["Id"]);
                                string THead = GetFieldsKey(formid)["TBHead"];
                                string TEntry = GetFieldsKey(formid)["TBEntry"];
                                string sql = $"/*dialect*/select  T1.FBILLNO ," +
                                             $"CONVERT(varchar(100), T2.F_SZXY_SDATE, 20) 'sdate'," +
                                             $"CONVERT(varchar(100), T2.F_SZXY_ENDDATE, 20)   'edate'" +
                                             $"from  {TEntry} t2 left  join {THead} T1 on T2.Fid = T1.Fid " +
                                             $" where T2.{GetFieldsKey(formid)["Mac"]}= '{MacId}' " +
                                             $"and T1.F_SZXY_OrgId={orgid} {Pjsql}  " +
                                                   //  $" and ((CONVERT(varchar(100)  ,'{Stime}', 20) between CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["Time"]}, 20) and CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["ETime"]}, 20) " +
                                                   //$"  or    CONVERT(varchar(100)  ,'{Etime}', 20) between  CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["Time"]}, 20) and  CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["ETime"]}, 20) )) ";
                                           $" and(CONVERT(varchar(100), '{Stime}', 20)BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 20) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) " +
                                           $" OR CONVERT(varchar(100), '{Etime}', 20) BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 20) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) " +
                                           $" OR CONVERT(varchar(100), T2.F_SZXY_SDate, 20) BETWEEN CONVERT(varchar(100), '{Stime}', 20) AND CONVERT(varchar(100), '{Etime}', 20) " +
                                           $" OR CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) BETWEEN CONVERT(varchar(100), '{Stime}', 20) AND CONVERT(varchar(100), '{Etime}', 20)) ";
                                DataSet Ds = Utils.CBData(sql, Context);

                                if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
                                {
                                    Logger.Debug("存在此机台的时间段", sql);
                                    throw new Exception("第" + seq + "行，历史订单号：" + Convert.ToString(Ds.Tables[0].Rows[0][0]) + "存在此机台的时间段");
                                }

                            }
                        }

                    }
                }




                else if (billob != null && formid.EqualsIgnoreCase("SZXY_BZRSCJH"))
                {
                    Dictionary<string, List<string>> dir = new Dictionary<string, List<string>>() { };
                    dir.Clear();
                    string BillNo = Convert.ToString(billob["FBillNo"]);
                    if (billob[GetFieldsKey(formid)["FEntry"]] is DynamicObjectCollection Entrys)
                    {
                        int index = 0;
                        foreach (DynamicObject row in Entrys)/*.Where(m => !Convert.ToString(m["F_SZXY_Material"]).IsNullOrEmptyOrWhiteSpace()))*/
                        {
                            string seq = Convert.ToString(row["Seq"]);
                            DateTime Stime = Convert.ToDateTime(row[GetFieldsKey(formid)["Time"]]);
                            DateTime Etime = Convert.ToDateTime(row[GetFieldsKey(formid)["ETime"]]);
                            DynamicObject F_SZXY_Material =(row["F_SZXY_Material"]) as DynamicObject;
                            decimal WIDTH = Convert.ToDecimal(row["F_SZXY_WIDTH"]);
                            string YXJ = Convert.ToString(row["F_SZXY_YXJ"]);
                            
                            string Mat = "";
                            if (F_SZXY_Material!=null)
                            {
                                Mat = F_SZXY_Material["Id"].ToString();
                            }
                            else
                            {
                                this.View.ShowWarnningMessage($"第{seq}行，产平型号不能为空！");
                            }
                            //int S = DateTime.Compare(Stime, Etime);
                            //if (S > 0)
                            //{
                            //    throw new Exception($"第{seq}行，开始时间不能大于结束时间！！");
                            //}
                            //else if (S == 0)
                            //{
                            //    throw new Exception($"第{seq}行，开始时间不能等于结束时间！！");
                            //}
                            //int E = DateTime.Compare(Etime, Stime);
                            //if (E < 0)
                            //{
                            //    throw new Exception($"第{seq}行，结束时间不能大于开始时间！！");
                            //}
                            List<string> TimeList = new List<string>();
                            TimeList.Clear();

                            if (Stime != DateTime.MinValue && Etime != DateTime.MinValue)
                            {
                                TimeList.Add(DateTimeToStamp(Stime));
                                //TimeList.Add(Stime.ToString("yyyyMMdd"));
                                TimeList.Add(Mat);
                                TimeList.Add(Convert.ToString( WIDTH));
                                TimeList.Add(YXJ);

                                if (TimeList != null)
                                {
                                   dir.Add(row["Seq"].ToString(), TimeList);
                                }
                            }
                            index++;
                        }

                        foreach (DynamicObject row in Entrys)
                        {
                            string seq = Convert.ToString(row["Seq"]);
                            DateTime Stime = Convert.ToDateTime(row[GetFieldsKey(formid)["Time"]]);
                            DateTime Etime = Convert.ToDateTime(row[GetFieldsKey(formid)["ETime"]]);
                            DynamicObject RF_SZXY_Material = (row["F_SZXY_Material"]) as DynamicObject;
                            string RWIDTH = Convert.ToString(row["F_SZXY_WIDTH"]);
                            string RYXJ = Convert.ToString(row["F_SZXY_YXJ"]);

                            string Mat = "";
                            if (RF_SZXY_Material != null)
                            {
                                Mat = RF_SZXY_Material["Id"].ToString();
                            }

                            string FEntryId = Convert.ToString(row["Id"]);
                            //DynamicObject Macobj = row[GetFieldsKey(formid)["Mac"]] as DynamicObject;

                            if (dir != null && dir.Count > 0)
                            {
                                foreach (var item in dir)
                                {
                                    string rewSeq = item.Key.ToString();
                                    if (rewSeq != seq)
                                    {
                                        long instime = Convert.ToInt64(Convert.ToString(DateTimeToStamp(Stime)));//输入的开始时间
                                        //long inetime = Convert.ToInt64(Convert.ToString(DateTimeToStamp(Etime))); //输入的结束时间
 
                                        List<string> TimeList1 = item.Value;
                                        long fromdate = Convert.ToInt64(TimeList1[0]);
                                        //long todate = Convert.ToInt64(TimeList1[1]);
                                   
                                        string Material= Convert.ToString(TimeList1[1]);
                                        string WIDTH = Convert.ToString(TimeList1[2]);
                                        string YXJ = Convert.ToString(TimeList1[3]);
                                        if (instime== fromdate && Mat==Material &&WIDTH==RWIDTH &&RYXJ==YXJ)
                                        {
                                            throw new Exception("第" + rewSeq + "行，同一物料+宽度 +优先级+开始时间不允许重复!");
                                        }
                                        //if (fromdate < instime && instime < todate)
                                        //{
                                        //    throw new Exception("第" + rewSeq + "行，开始结束日期在此单据存在冲突!");
                                        //}

                                    }
                                }

                            

                            DynamicObject orgobj = billob[GetFieldsKey(formid)["Org"]] as DynamicObject;
                                if (orgobj == null) throw new Exception("业务组织不允许为空");
                                string Pjsql = "";
                                //if (!FEntryId.IsNullOrEmptyOrWhiteSpace())
                                //{
                                //    Pjsql = $" and T2.FEntryId !={ FEntryId} ";
                                //}
                                string Fid = this.Model.DataObject[0].ToString();
                                if (!Fid.IsNullOrEmptyOrWhiteSpace())
                                {
                                    Pjsql = $" and T2.Fid !={ Fid} ";
                                }
                                long orgid = Convert.ToInt64(orgobj["Id"]);
                                string THead = GetFieldsKey(formid)["TBHead"];
                                string TEntry = GetFieldsKey(formid)["TBEntry"];
                                if (Mat.IsNullOrEmptyOrWhiteSpace())
                                {
                                    return;
                                }
                            //string sql = $"/*dialect*/select  T1.FBILLNO ,T2.Fid " +
                            //             //$"CONVERT(varchar(100), T2.F_SZXY_SDATE, 20) 'sdate'," +
                            //             //$"CONVERT(varchar(100), T2.F_SZXY_ENDDATE, 20)   'edate'" +
                            //            $" from  {TEntry} t2" +
                            //            $" left  join {THead} T1 on T2.Fid = T1.Fid " +
                            //           $" where  T1.F_SZXY_OrgId={orgid}  {Pjsql} " +
                            //           $" and(CONVERT(varchar(100), '{Stime}', 20)BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 20) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) " +
                            //           $" OR CONVERT(varchar(100), '{Etime}', 20) BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 120) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) " +
                            //           $" OR CONVERT(varchar(100), T2.F_SZXY_SDate, 20) BETWEEN CONVERT(varchar(100), '{Stime}', 120) AND CONVERT(varchar(100), '{Etime}', 20) " +
                            //           $" OR CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) BETWEEN CONVERT(varchar(100), '{Stime}', 120) AND CONVERT(varchar(100), '{Etime}', 20)) " +
                            //           $" Group by T1.FBILLNO,T2.Fid ";

                            string sql = $"/*dialect*/select  T1.FBILLNO ,T2.Fid " +
                                        $" from  {TEntry} t2" +
                                        $" left  join {THead} T1 on T2.Fid = T1.Fid " +
                                       $" where  T1.F_SZXY_OrgId={orgid}  {Pjsql} " +
                                       $" and T2.F_SZXY_SDate='{Stime}' " +
                                       //   $" and CONVERT(varchar(100), '{Stime}', 120) BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 20) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20)) " +
                                       $" and  T2.F_SZXY_YXJ={RYXJ} " +
                                       $" and  T2.F_SZXY_MATERIAL={Mat} " +
                                       $" and  T2.F_SZXY_WIDTH={RWIDTH} " +

                                       $" Group by T1.FBILLNO,T2.Fid ";
                            DataSet Ds = Utils.CBData(sql, Context);

                                if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
                                {
                                    Logger.Debug("包装日计划SQL", sql);
                                    throw new Exception("历史订单号：" + Convert.ToString(Ds.Tables[0].Rows[0][0]) + "同一物料+宽度 +优先级+开始时间不允许重复!");
                                }

                            }
                         }

                    }
                }



                //分层日计划 同一机台同一时间段 不允许有相同的物料
                if (formid.EqualsIgnoreCase("SZXY_FCJTRSCJH"))
                {
                    formid = "SZXY_FCJTRSCJH";
                    Dictionary<string, List<string>> dir = new Dictionary<string, List<string>>() { };
                    dir.Clear();
                    string BillNo = Convert.ToString(billob["FBillNo"]);
                    if (billob[GetFieldsKey(formid)["FEntry"]] is DynamicObjectCollection Entrys)
                    {

                        DateTime EtimePre;
                        int index = 0;
                        foreach (DynamicObject row in Entrys)
                        {
                            string seq = Convert.ToString(row["Seq"]);
                            DateTime Stime = Convert.ToDateTime(row[GetFieldsKey(formid)["Time"]]);
                            DateTime Etime = Convert.ToDateTime(row[GetFieldsKey(formid)["ETime"]]);
                            if (this.Model.GetValue(GetFieldsKey(formid)["ETime"], index - 1) != null)
                            {
                                EtimePre = Convert.ToDateTime(this.Model.GetValue(GetFieldsKey(formid)["ETime"], index - 1));
                                if (Stime == EtimePre)
                                {
                                    throw new Exception($"开始时间不能等于上一行结束时间！！");
                                }
                            }
                            if (row[GetFieldsKey(formid)["Mac"]] is DynamicObject Macobj && row["F_SZXY_Material"] is DynamicObject Material)
                            {

                                int S = DateTime.Compare(Stime, Etime);
                                if (S > 0)
                                {
                                    throw new Exception($"第{seq}行，开始时间不能大于结束时间！！");
                                }
                                else if (S == 0)
                                {
                                    throw new Exception($"第{seq}行，开始时间不能等于结束时间！！");
                                }
                                int E = DateTime.Compare(Etime, Stime);
                                if (E < 0)
                                {
                                    throw new Exception($"第{seq}行，结束时间不能大于开始时间！！");
                                }
                                List<string> TimeList = new List<string>();
                                TimeList.Clear();
                                string MacId = Macobj["Id"].ToString();


                                if (Stime != DateTime.MinValue && Etime != DateTime.MinValue)
                                {
                                    TimeList.Add(DateTimeToStamp(Stime));
                                    TimeList.Add(DateTimeToStamp(Etime));
                                    TimeList.Add(MacId);
                                    TimeList.Add(Material["ID"].ToString());
                                    if (TimeList != null)
                                    {
                                        dir.Add(row["Seq"].ToString(), TimeList);
                                    }
                                }
                            }
                            else throw new Exception("机台不允许为空！！");
                            index++;
                        }



                        foreach (DynamicObject row in Entrys)
                        {
                            string seq = Convert.ToString(row["Seq"]);
                            DateTime Stime = Convert.ToDateTime(row[GetFieldsKey(formid)["Time"]]);

                            DateTime Etime = Convert.ToDateTime(row[GetFieldsKey(formid)["ETime"]]);
                            string FEntryId = Convert.ToString(row["Id"]);

                            if (row[GetFieldsKey(formid)["Mac"]] is DynamicObject Macobj && row["F_SZXY_Material"] is DynamicObject Material)
                            {
                                string MacId = Macobj["Id"].ToString();
                                string MaterialId = Material["Id"].ToString();
                                if (dir != null && dir.Count > 0)
                                {
                                    foreach (var mac in dir)
                                    {
                                        string rewSeq = mac.Key.ToString();
                                        if (rewSeq != seq)
                                        {
                                            long instime = Convert.ToInt64(Convert.ToString(DateTimeToStamp(Stime)));//输入的开始时间
                                            long inetime = Convert.ToInt64(Convert.ToString(DateTimeToStamp(Etime))); //输入的结束时间

                                            List<string> TimeList1 = mac.Value;
                                            long fromdate = Convert.ToInt64(TimeList1[0]);
                                            long todate = Convert.ToInt64(TimeList1[1]);
                                            string fromMac = Convert.ToString(TimeList1[2]);
                                            string FromMaterial = Convert.ToString(TimeList1[3]);
                                            if (fromMac == MacId&& MaterialId== FromMaterial)
                                            {
                                                if (fromdate < instime && instime < todate)
                                                {
                                                    throw new Exception("第" + rewSeq + "行，同一机台同一产品型号的开始时间结束时间在此单据中存在冲突!");
                                                }
                                            }
                                        }
                                    }

                                }
                                string Pjsql = "";
                                if (!FEntryId.IsNullOrEmptyOrWhiteSpace())
                                {
                                    Pjsql = $" and T2.FEntryId !={ FEntryId} ";
                                }
                                DynamicObject orgobj = billob[GetFieldsKey(formid)["Org"]] as DynamicObject;
                                if (orgobj == null) throw new Exception("业务组织不允许为空");
                                long orgid = Convert.ToInt64(orgobj["Id"]);
                                string THead = GetFieldsKey(formid)["TBHead"];
                                string TEntry = GetFieldsKey(formid)["TBEntry"];
                                string sql = $"/*dialect*/select  T1.FBILLNO ," +
                                             $"CONVERT(varchar(100), T2.F_SZXY_SDATE, 20) 'sdate'," +
                                             $"CONVERT(varchar(100), T2.F_SZXY_ENDDATE, 20)   'edate'" +
                                             $" from  {TEntry} t2 " +
                                             $" left  join {THead} T1 on T2.Fid = T1.Fid " +
                                             $" where T2.{GetFieldsKey(formid)["Mac"]}= '{MacId}'" +
                                             $" and t2.F_SZXY_MATERIAL='{MaterialId}' " +
                                             $"and T1.F_SZXY_OrgId={orgid} {Pjsql}  " +
                                           //  $" and ((CONVERT(varchar(100)  ,'{Stime}', 20) between CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["Time"]}, 20) and CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["ETime"]}, 20) " +
                                           //$"  or    CONVERT(varchar(100)  ,'{Etime}', 20) between  CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["Time"]}, 20) and  CONVERT(varchar(100)  ,T2.{GetFieldsKey(formid)["ETime"]}, 20) )) ";
                                           $" and(CONVERT(varchar(100), '{Stime}', 20)BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 20) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) " +
                                           $" OR CONVERT(varchar(100), '{Etime}', 20) BETWEEN CONVERT(varchar(100), T2.F_SZXY_SDate, 20) AND CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) " +
                                           $" OR CONVERT(varchar(100), T2.F_SZXY_SDate, 20) BETWEEN CONVERT(varchar(100), '{Stime}', 20) AND CONVERT(varchar(100), '{Etime}', 20) " +
                                           $" OR CONVERT(varchar(100), T2.F_SZXY_EndDate, 20) BETWEEN CONVERT(varchar(100), '{Stime}', 20) AND CONVERT(varchar(100), '{Etime}', 20)) ";
                                DataSet Ds = Utils.CBData(sql, Context);

                                if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
                                {
                                    Logger.Debug("存在此机台的时间段", sql);
                                    throw new Exception("第" + seq + "行，历史订单号：" + Convert.ToString(Ds.Tables[0].Rows[0][0]) + "存在此机台的时间段");
                                }

                            }
                        }

                    }
                }
            } }

        private Dictionary<string, string> GetFieldsKey(string formID)
        {
            Dictionary<string, string> FieldsKey = new Dictionary<string, string>();
            try
            {
                switch (formID.ToUpper())
                {
                    //[CONFIG] F_SZXY_OrgId1
                    case "SZXY_LYJTRSCJH"://流延机台日计划
                        FieldsKey.Add("FEntry", "SZXY_LYJTRSCJHEntry");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_LYJTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_LYJTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;
                    case "SZXY_FHJTRSCJH"://复合机台日计划 
                        FieldsKey.Add("FEntry", "SZXY_FHJTRSCJHEntry");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_FHJTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_FHJTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;
                    case "SZXY_LSJTRSCJH"://拉伸机台日计划
                        FieldsKey.Add("FEntry", "SZXY_LSJTRSCJHEntry");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_LSJTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_LSJTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;
                    case "SZXY_FCJTRSCJH"://分层机台日生产计划 
                        FieldsKey.Add("FEntry", "SZXY_FCJTRSCJHEntry");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_FCJTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_FCJTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;
                    case "SZXY_FQJTRSCJH"://分切机台日生产计划
                        FieldsKey.Add("FEntry", "SZXY_FQJTRSCJH");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_FQJTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_FQJTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;
                    case "SZXY_BZRSCJH"://bz 日计划
                        FieldsKey.Add("FEntry", "SZXY_BZRSCJHEntry");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_BZRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_BZRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;

                    case "SZXY_SFJTRSCJH"://SF机台日生产计划
                        FieldsKey.Add("FEntry", "SZXY_SFJTRSCJH");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_SFJTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_SFJTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;
                    case "SZXY_TFHTRSCJH"://TF机台日生产计划
                        FieldsKey.Add("FEntry", "SZXY_TFHTRSCJH");
                        FieldsKey.Add("Mac", "F_SZXY_Machine");
                        FieldsKey.Add("Time", "F_SZXY_SDate");
                        FieldsKey.Add("ETime", "F_SZXY_EndDate");
                        FieldsKey.Add("TBHead", "SZXY_t_TFHTRSCJH");
                        FieldsKey.Add("TBEntry", "SZXY_t_TFHTRSCJHEntry");
                        FieldsKey.Add("Org", "F_SZXY_OrgId");
                        break;


                }
            }
            catch (Exception) { }

            return FieldsKey;
        }
        // 时间转时间戳
        public string DateTimeToStamp(DateTime dtime)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
            long timeStamp = (long)(dtime - startTime).TotalMilliseconds; // 相差毫秒数
            return timeStamp.ToString();
        }

    

     
        public override void AfterCreateNewEntryRow(CreateNewEntryEventArgs e)
        {
            base.AfterCreateNewEntryRow(e);
            if (this.View.BusinessInfo.GetForm().Id.EqualsIgnoreCase("SZXY_BZRSCJH"))
            {
                //e.Row
                if (this.Model.DataObject["SZXY_BZRSCJHEntry"] is DynamicObjectCollection Entry)
                {
                    if (Entry.Count>0&& Entry[e.Row]!=null)
                    {
                        if (Entry[e.Row]["F_SZXY_SDate"] != null && Entry[e.Row]["F_SZXY_EndDate"] != null)
                        {
                            DateTime SDate = Convert.ToDateTime(Entry[e.Row]["F_SZXY_SDate"]);
                            DateTime EDate = SDate.AddHours(Convert.ToDouble(12));

                            Entry[e.Row]["F_SZXY_EndDate"] = EDate;
                        }
                    }
                
             
                } 
            }
            
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (this.View.BusinessInfo.GetForm().Id.EqualsIgnoreCase("SZXY_BZRSCJH"))
            {
                int m = e.Row;
                //总箱数 F_SZXY_Cases 公式：计划生产面积/面积/卷数，向上取整
                if (e.Field.Key.EqualsIgnoreCase("F_SZXY_PlanProductionArea") || e.Field.Key.EqualsIgnoreCase("F_SZXY_Area") || e.Field.Key.EqualsIgnoreCase("F_SZXY_Volume"))
                {
                    DynamicObject Billobj = this.Model.DataObject;
                    string PlanProductionArea = Convert.ToString(this.Model.GetValue("F_SZXY_PlanProductionArea", m));
                    string Area = Convert.ToString(this.Model.GetValue("F_SZXY_Area", m));
                    string Volume = Convert.ToString(this.Model.GetValue("F_SZXY_Volume", m));
                    if (!PlanProductionArea.IsNullOrEmptyOrWhiteSpace() && !Area.IsNullOrEmptyOrWhiteSpace() && !Volume.IsNullOrEmptyOrWhiteSpace())
                    {
                        if (Convert.ToDecimal(PlanProductionArea) > 0 && Convert.ToDecimal(Area) > 0 && Convert.ToDecimal(Volume) > 0)
                        {

                            decimal Cases = Convert.ToDecimal(PlanProductionArea) / Convert.ToDecimal(Area) / Convert.ToDecimal(Volume);

                            Cases = Math.Ceiling(Convert.ToDecimal(PlanProductionArea) / Convert.ToDecimal(Area) / Convert.ToDecimal(Volume));
                            this.Model.SetValue("F_SZXY_Cases", Cases, m);
                        }
                    }
                    this.View.UpdateView("F_SZXY_Cases");

                }

            }
          
          
        }

      


    }
}
