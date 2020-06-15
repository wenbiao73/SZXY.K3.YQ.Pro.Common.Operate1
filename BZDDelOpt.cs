 
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
    [Description("包装单删除改写分切外观检验")]
    public class BZDDelOpt : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);


            e.FieldKeys.Add("F_SZXY_RJHEntryIDH"); 
            e.FieldKeys.Add("F_SZXY_OrgId1"); 
            e.FieldKeys.Add("F_SZXY_Barcode1");
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
                    decimal Area = 0;
                    string RJHFid = string.Empty;
                    string RJHRowId = string.Empty;
                    //RJHFid = Convert.ToString(dy["F_SZXY_FIDH"]);

                    if (dy["SZXY_BZDHEntry"] is DynamicObjectCollection Entry)
                    {
                        foreach (DynamicObject item in Entry)
                        {
                            string F_SZXY_Barcode = Convert.ToString(item["F_SZXY_Barcode"]);

                            if (!F_SZXY_Barcode.IsNullOrEmptyOrWhiteSpace())
                            {

                                string UpdateSql = $"/*dialect*/update  SZXY_t_FQJYDEntry set F_SZXY_Delete=0 where F_SZXY_BARCODE in ('{F_SZXY_Barcode}')";
                                DBServiceHelper.Execute(Context, UpdateSql);
                            }
                        }
                    }

                }
            }

        }


    }
}
