 

using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
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
    [Description("涂覆检验单审核反写涂覆单实际厚度")]
    public class TFJYDAuditOpt : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("F_SZXY_PLyAvg1");// 
            e.FieldKeys.Add("F_SZXY_TFNo1");//  
 
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
        ///累加日计划面积
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            IViewService Services = ServiceHelper.GetService<IViewService>();
            if (selectedRows != null && selectedRows.Count() != 0)
            {
                IMetaDataService metadataService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
 

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
 
                    if (dy["SZXY_TFJCDEntry2"] is DynamicObjectCollection Entry)
                    {
                        foreach (var item in Entry)
                        {
                            decimal  PlyAvg = Convert.ToDecimal(item["F_SZXY_PLyAvg"]);
                           
                            string    TFCode = Convert.ToString(item["F_SZXY_TFNo1"]);
                            if (!TFCode.IsNullOrEmptyOrWhiteSpace() && Convert.ToInt32(PlyAvg) > 0)
                            {

                                string sql1 = $"/*dialect*/update SZXY_t_XYTFEntry  set F_SZXY_SJPLY={PlyAvg} " +
                                             $" where F_SZXY_COATCODE='{TFCode}' ";

                                DBServiceHelper.Execute(Context, sql1);
                            }
                        }
                    }

                }
            }

        }


    }
}
