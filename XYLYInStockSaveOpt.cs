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
    [Description("生产入库单保存反写编号到流延单")]
    public class XYLYInStockSaveOpt: AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        List<string> IdList = new List<string>();

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

            foreach (DynamicObject dy in selectedRows)
            {
                string Id = Convert.ToString(dy["Id"]);

                DynamicObject checkObejct = Utils.LoadFIDBillObject(this.Context, "PRD_INSTOCK", Id);

                string RKTZD = Convert.ToString(checkObejct["F_SZXY_RKTZD"]);

                if (!RKTZD.IsNullOrEmptyOrWhiteSpace()) IdList.Add(RKTZD);

            }
        }
 
        /// </summary>
        /// <param name="e"></param>
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);

            if (this.OperationResult.IsSuccess && IdList != null && IdList.Count() > 0)
            {

                foreach (string RKTZD in IdList)
                {

                    string SQL = "/*dialect*/update  SZXY_t_BZRKTZD set F_SZXY_RKDNO=''  where FID='" + RKTZD + "'";

                    DBServiceHelper.Execute(this.Context, SQL);
                }
            }

        }
    }
}
