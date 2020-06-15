 

using Kingdee.BOS;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("物流快递单据打印操作。")]
    public class WLKDPrint : AbstractBillPlugIn
    {



        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);


            DynamicObject billobj = this.Model.DataObject;
            string formID = this.View.BillBusinessInfo.GetForm().Id;

            //打印按钮事件
            if (e.BarItemKey.EqualsIgnoreCase("SZXY_tbGridPrintNo"))
            {
                if (billobj["F_SZXY_OrgId"] is DynamicObject org)
                {
                    this.Model.DataObject["FFormId"] = this.View.BusinessInfo.GetForm().Id;
                    Utils.Save(View, new DynamicObject[] { this.Model.DataObject }, Kingdee.BOS.Orm.OperateOption.Create(), Context);

                    Utils.TYPrint(this.View, Context, Convert.ToInt64(org["Id"]), billobj[0].ToString());

                }

            }
        }

    }
}
