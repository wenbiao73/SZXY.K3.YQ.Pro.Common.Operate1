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
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [Description("流延机台日计划到流延单据转换")]
    public class XYCastConvert : AbstractConvertPlugIn
    {
        /// <summary>
        /// 成品明细对应的子键明细的数据实现
        /// panqing  2016.08.15
        /// </summary>
        /// <param name="e"></param>
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
                    DynamicObjectCollection EntryCollect = null;//流延单
                    //获取目标单的明细信息（流延单）
                    Entity entity = e.TargetBusinessInfo.GetEntity("FBillEntry");
                    DynamicObject BillObject = extendedDataEntity.DataEntity;
                    EntryCollect = extendedDataEntity.DataEntity["SZXY_XYLYEntry"] as DynamicObjectCollection;
                    //string BillNo = Convert.ToString(BillObject["BillNo"]);
                   // DynamicObject orgObj = BillObject["F_SZXY_OrgId"] as DynamicObject;
                    //移除目标单成品明细信息空行(选单时目标单会有空行)
                    if (EntryCollect != null && EntryCollect.Count > 0)
                    {
                        foreach(DynamicObject Row in EntryCollect)
                        {
                            //DynamicObject newRow = new DynamicObject(entity.DynamicObjectType);
                            string Date = Convert.ToString(Row["F_SZXY_Date"]);
                            DynamicObject formulaObejct = Row["F_SZXY_Formula"] as DynamicObject;
                            DynamicObject machineObejct = Row["F_SZXY_Machine"] as DynamicObject;
                            string Seq = Convert.ToString(Row["F_SZXY_Seq"]);
                            string rollNO = Convert.ToString(Row["F_SZXY_RollNo"]);
                        
                            if (!Date.IsNullOrEmptyOrWhiteSpace() && formulaObejct != null&& machineObejct != null && !Seq.IsNullOrEmptyOrWhiteSpace() && !rollNO.IsNullOrEmptyOrWhiteSpace())
                            {
                                string formulaNumber = Convert.ToString(formulaObejct["FNumber"]);
                                string machineNumber = Convert.ToString(machineObejct["FNumber"]);
                               // string org=Convert.ToString(orgObj["Number"]);
                                Row["F_SZXY_PlasticNo"] = "C" + formulaNumber+ Convert.ToDateTime(Row["F_SZXY_Date"]).ToString("yy-MM-dd") + machineNumber.Trim().Substring(1, machineNumber.Trim().Length -1)+ Seq + rollNO;
                                Row["F_SZXY_InPutQty"] = ((Convert.ToDecimal(Row["F_SZXY_PLy"]) * (Convert.ToDecimal(Row["F_SZXY_Width"]) * Convert.ToDecimal(Row["F_SZXY_Len"]) / Convert.ToDecimal(1000)) * Convert.ToDecimal(0.91)) / Convert.ToDecimal(1000)) / Convert.ToDecimal(0.83);
                                Row["F_SZXY_Output"] = (Convert.ToDecimal(Row["F_SZXY_PLy"]) * (Convert.ToDecimal(Row["F_SZXY_Width"]) * Convert.ToDecimal(Row["F_SZXY_Len"]) / Convert.ToDecimal(1000)) * Convert.ToDecimal(0.91)) / Convert.ToDecimal(1000);
                            }
                            //EntryCollect.Add(newRow);F_SZXY_Output
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
