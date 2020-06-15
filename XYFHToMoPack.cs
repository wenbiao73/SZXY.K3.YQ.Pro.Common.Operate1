using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
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
    [Description("复合到生产领料单单据转换")]
    public class XYFHToMoPack : AbstractConvertPlugIn
    {
 
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            IViewService Services = ServiceHelper.GetService<IViewService>();
            //获取整个下推的数据
            var dataObjs = e.Result.FindByEntityKey("FBillHead");

            if (dataObjs != null)
            {

                foreach (var extendedDataEntity in dataObjs)
                {
                    DynamicObjectCollection EntryCollect = null;//
                    //获取目标单的明细信息
                    Entity entity = e.TargetBusinessInfo.GetEntity("FEntity");
                    DynamicObject BillObject = extendedDataEntity.DataEntity;
                    EntryCollect = extendedDataEntity.DataEntity["Entity"] as DynamicObjectCollection;//生产领料单

                    if (EntryCollect != null && EntryCollect.Count > 0)
                    {
                        foreach (DynamicObject Row in EntryCollect)
                        {
                            string MoId = Convert.ToString(Row["MoId"]);//生产订单内码
                            int MoSeq = Convert.ToInt32(Row["MoEntrySeq"]);//生产订单行号
                            string FMATERIALID = Convert.ToString(Row["MaterialId_Id"]);//原料
                            //加载生产订单
                            if (MoId != "0")
                            {
                                DynamicObject MoObejct = Utils.LoadFIDBillObject(this.Context, "PRD_MO", MoId);
                                long MoEntryId = 0;//生产订单分录内码
                                if (MoObejct["TreeEntity"] is DynamicObjectCollection MoEntry)
                                {
                                    MoEntryId = Convert.ToInt64(MoEntry[MoSeq - 1]["Id"]);
                                }
                                if (MoObejct != null && !Convert.ToString(Row["MoEntrySeq"]).IsNullOrEmptyOrWhiteSpace() && !FMATERIALID.IsNullOrEmptyOrWhiteSpace())
                                {
                                    string MoBillNo = Convert.ToString(MoObejct["BillNo"]);//生产订单编号
                                    string SelUseSql = "/*dialect*/select T1.FBILLNO,T1.FID,T2.FENTRYID,T2.FSEQ from T_PRD_PPBOM T1 left join T_PRD_PPBOMENTRY T2 on t1.FID=T2.FID " +
                                                       $"where T1.FMOBILLNO = '{MoBillNo}' and T1.FMOENTRYSEQ = '{MoSeq}' and T2.FMATERIALID = '{FMATERIALID}'";
                                    DataSet Ds = Utils.CBData(SelUseSql, Context);
                                    if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
                                    {
                                        string UseFBILLNO = Convert.ToString(Ds.Tables[0].Rows[0]["FBILLNO"]);//用料清单编号
                                        long UseFID = Convert.ToInt64(Ds.Tables[0].Rows[0]["FID"]);//用料清单ID
                                        long UseEntryId = Convert.ToInt64(Ds.Tables[0].Rows[0]["FENTRYID"]);//用料清单行Id 
                                        string UseSeq = Convert.ToString(Ds.Tables[0].Rows[0]["FSEQ"]);//用料清单Seq
                                        Row["MoEntryId"] = MoEntryId;
                                        Row["PPBomEntryId"] = UseEntryId;
                                        //用料清单编号
                                        Row["MoId"] = MoId;
                                        Row["MoEntrySeq"] = MoSeq;
                                        Row["PPBomBillNo"] = UseFBILLNO;

                                    }
                                }
                            }


                        }
                    }

                }
            }
        }
        /// <summary>
        /// 给基础资料字段赋值 
        /// </summary>
        private void SetBaseDataValue(IViewService service, DynamicObject data, BaseDataField bdfield, long value, ref DynamicObject dyValue)
        {
            if (value == 0)
            {
                //bdfield.RefIDDynamicProperty.SetValue(data, 0);
                //bdfield.DynamicProperty.SetValue(data, null);
                dyValue = null;
            }
            else
            {
                if (dyValue == null)
                {
                    dyValue = service.LoadSingle(this.Context, value, bdfield.RefFormDynamicObjectType);
                }
                if (dyValue != null)
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, value);
                    bdfield.DynamicProperty.SetValue(data, dyValue);
                }
                else
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, 0);
                    bdfield.DynamicProperty.SetValue(data, null);
                }
            }
        }
        // <summary>
        /// 给维度字段赋值 
        /// </summary>
        private void SetFlexDataValue(IViewService service, DynamicObject data, RelatedFlexGroupField bdfield, long value, ref DynamicObject dyValue)
        {
            if (value == 0)
            {
                //bdfield.RefIDDynamicProperty.SetValue(data, 0);
                //bdfield.DynamicProperty.SetValue(data, null);
                dyValue = null;
            }
            else
            {
                if (dyValue == null)
                {
                    dyValue = service.LoadSingle(this.Context, value, bdfield.RefFormDynamicObjectType);
                }
                if (dyValue != null)
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, value);
                    bdfield.DynamicProperty.SetValue(data, dyValue);
                }
                else
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, 0);
                    bdfield.DynamicProperty.SetValue(data, null);
                }
            }
        }

    }
}
