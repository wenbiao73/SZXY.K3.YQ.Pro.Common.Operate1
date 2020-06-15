 
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
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
    [Description("复合单到生产入库单的单据转换")]
    public class XYFHToInStockConvert : AbstractConvertPlugIn
    {

        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);

            //获取整个下推的数据
            var dataObjs = e.Result.FindByEntityKey("FBillHead");

            if (dataObjs != null)
            {
                foreach (var extendedDataEntity in dataObjs)
                {
                    DynamicObjectCollection EntryCollect = null;

                    DynamicObject BillObject = extendedDataEntity.DataEntity;

                    //生产入库单明细
                    EntryCollect = extendedDataEntity.DataEntity["Entity"] as DynamicObjectCollection;

                    Entity entity2 = e.TargetBusinessInfo.GetEntity("FEntity_Link");

                    if (EntryCollect != null && EntryCollect.Count > 0)
                    {

                        foreach (DynamicObject row in EntryCollect)
                        {
                            string fmoent = "";
                            string sql = "select FENTRYID   FROM T_PRD_MOENTRY  where FID= "
                            + row["MoId"]
                            + "and  FSEQ= "
                            + row["MoEntrySeq"];//sql拼接
                            DataSet sqltable = DBServiceHelper.ExecuteDataSet(this.Context, sql);//放入DataSet
                            if (sqltable != null && sqltable.Tables.Count > 0 && sqltable.Tables[0].Rows.Count > 0)
                            {
                                fmoent = sqltable.Tables[0].Rows[0][0].ToString();
                            }

                            //源单类型
                            row["SrcBillType"] = "PRD_MO";
                            //源单编号
                            row["SrcBillNo"] = row["MoBillNo"];
                            //源单内码
                            row["SrcInterId"] = row["MoId"];
                            //源单分录内码
                            row["SrcEntryId"] = fmoent;
                            //源单行号
                            row["SrcEntrySeq"] = row["MoEntrySeq"];

                            DynamicObjectCollection linkCollect = row["FEntity_Link"] as DynamicObjectCollection;

                            linkCollect[0]["RuleId"] = "PRD_MO2INSTOCK";//PRD_INSTOCK

                            linkCollect[0]["STableName"] = "T_PRD_MOENTRY";

                            linkCollect[0]["SBillId"] = row["MoId"];

                            //生产订单分录内码
                            linkCollect[0]["SId"] = fmoent;

                        }

                    }

                }
            }

        }

    }
}
