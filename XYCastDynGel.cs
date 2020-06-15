using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
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
    [Description("流延的晶点动态表单")]
    public class XYCastDynGel : AbstractDynamicFormPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string value = Convert.ToString(this.View.OpenParameter.GetCustomParameter("value"));
            if (!value.IsNullOrEmptyOrWhiteSpace())
            {
                string[] strValue = value.Split('|');
                int m = strValue.Count();
                if(m>0)
                {
                    for(int i=0;i<m;i++)
                    {
                        string[] xyValue= strValue[i].Split(',');
                        if(xyValue!=null && xyValue.Count()>0)
                        {
                            this.Model.SetValue("F_SZXY_XCoordinate", xyValue[0],i);
                            this.Model.SetValue("F_SZXY_YCoordinate", xyValue[1],i);
                        }                        
                    }
                }
               
            }
            this.View.UpdateView("F_SZXY_Entity");
        }
        /// <summary>
        /// 返回数值
        /// </summary>
        /// <param name="e"></param>
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);         
            if (e.Key.EqualsIgnoreCase("F_SZXY_OK"))
            {
                string value = "";
                int m = this.Model.GetEntryCurrentRowIndex("F_SZXY_Entity")+1;
                if(m>0)
                {
                    for(int i=1;i<=m;i++)
                    {
                        string xvalue = Convert.ToString(this.Model.GetValue("F_SZXY_XCoordinate",i-1));//X坐标
                        string yvalue = Convert.ToString(this.Model.GetValue("F_SZXY_YCoordinate",i-1));//Y坐标
                        if (!xvalue.IsNullOrEmptyOrWhiteSpace() && !yvalue.IsNullOrEmptyOrWhiteSpace()) value += "|" + xvalue + "," + yvalue;
                    }
                }
                
                //此处写【确定】按钮所执行的方法
                AScanReturnInfo returnInfo = new AScanReturnInfo();

                if (!value.IsNullOrEmptyOrWhiteSpace()) returnInfo.Value = value.Substring(1, value.Length-1);
                else returnInfo.Value = "";
                // 把数据对象，返回给父界面
                //this.View.ReturnToParentWindow(new FormResult(returnInfo));
                this.View.ReturnToParentWindow(new FormResult(value.Substring(1, value.Length - 1)));
                this.View.Close();

            }
            else
            {
                //此处写【确定】按钮所执行的方法
                AScanReturnInfo returnInfo = new AScanReturnInfo();
                returnInfo.Value = "";
                // 把数据对象，返回给父界面
                //this.View.ReturnToParentWindow(new FormResult(returnInfo));
                this.View.ReturnToParentWindow(new FormResult(""));
                this.View.Close();
            }
            
        
        }
    }
}
