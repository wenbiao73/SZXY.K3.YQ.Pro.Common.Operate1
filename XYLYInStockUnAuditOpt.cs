using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("流延反审核检测入库单号是否为空，不为空不能删")]
    public class XYLYInStockUnAuditOpt : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            //e.FieldKeys.Add("F_BGJ_IOS_INSTOCK");

            //e.FieldKeys.Add("FBillTypeID");

            //e.FieldKeys.Add("FISTAXINCOST");

            //e.FieldKeys.Add("FMAINBOOKSTDCURRID");

        }

        /// <summary>
        /// 开始获取选中单据信息
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);

            selectedRows = e.SelectedRows.Select(s => s.DataEntity);

        }

        /// <summary>
        /// 按钮事件完毕开始检查费用应付单自动生成成本调整单
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            if (this.OperationResult.IsSuccess && selectedRows != null && selectedRows.Count() != 0)
            {
            
                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);

                    DynamicObject checkObejct = Utils.LoadFIDBillObject(this.Context, "SZXY_BZRKTZD", Id);

                    string RKDNO = Convert.ToString(checkObejct["F_SZXY_RKDNO"]);

                    string BillNo = Convert.ToString(checkObejct["FBillNo"]);

                    if(!RKDNO.IsNullOrEmptyOrWhiteSpace()) throw new Exception("入库通知单："+ BillNo+"有下推生产入库单"+ RKDNO+",不允许反审核!");

                }
            }

        }
    }
}
