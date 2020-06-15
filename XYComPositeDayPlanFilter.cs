using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.SqlBuilder;
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
    [Description("机台日计划过滤动态表单")]
    public class XYComPositeDayPlanFilter : AbstractDynamicFormPlugIn
    {
        private Dictionary<string, DynamicObject> FilterInfoDir;
        string index = "";
        public override void OnInitialize(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.InitializeEventArgs e)
        {
            base.OnInitialize(e);
            //初始化时从参数获取自定义参数
            FilterInfoDir = (Dictionary<string, DynamicObject>)this.View.OpenParameter.GetCustomParameter("FilterInfo");
            index = Convert.ToString(this.View.OpenParameter.GetCustomParameter("index"));
        }
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            DynamicObject BillHead = this.Model.DataObject;
            string MaterialId = "", MacNumber = ""; DynamicObject Material = null, Mac = null;
            if (!(FilterInfoDir.IsNullOrEmptyOrWhiteSpace()))
            {
                Material = FilterInfoDir["物料"] as DynamicObject;
                MaterialId = Material["Id"].ToString();
                Mac = FilterInfoDir["机台"] as DynamicObject;
                MacNumber = Mac["FNumber"].ToString(); //MacId = Mac[""].ToString();
                this.Model.SetValue("F_SZXY_Mac", Mac);
                this.Model.SetValue("F_SZXY_Mateial", Material);
            }

            //DynamicObject[] dynamicObject1s = BusinessDataServiceHelper.Load(this.Context, new[] { row.PrimaryKeyValue }, metadata.BusinessInfo.GetDynamicObjectType());
            //构建快捷过滤条件
            if (MaterialId != "" && MacNumber != "")
            {

                //string sql = $"/*dialect*/select T2.FBillNo,T2.FDocumentStatus ,T2.FCreateDate,T1.F_SZXY_TEAMGROUP," +
                //             $"T1.F_SZXY_CLASSES," +
                //             $"T1.F_SZXY_PONO," +
                //             $"T1.F_SZXY_MoLineNo," +
                //             $"T1.F_SZXY_Material," +
                //             $"T1.F_SZXY_recipe," +
                //             $"T1.F_SZXY_SMark," +
                //             $"T1.F_SZXY_LEN,T1.F_SZXY_WIDTH,T1.F_SZXY_PLY," +
                //             $"T1.F_SZXY_OPERATOR,T1.F_SZXY_WIRED,T1.F_SZXY_MVolume" +
                //             $" from SZXY_t_FHJTRSCJHEntry T1 left join SZXY_t_FHJTRSCJH T2 on T1.Fid = T2.Fid  where T1.F_SZXY_Machine='{MacId}' and T1.F_SZXY_Material='{MaterialId}'";
                //DataSet Ds = Utils.CBData(sql, Context);
                //SetValue(Ds);

            }

            //Model.ClearNoDataRow();
            //this.View.UpdateView("F_SZXY_Entity");


        }

        private void SetValue(DataSet Ds)
        {
            if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
            {
                Entity entity = this.View.BusinessInfo.GetEntity("F_SZXY_Entity");
                DynamicObjectCollection Entrys = this.Model.DataObject["SZXY_Entry"] as DynamicObjectCollection;
                Entrys.Clear();
                for (int i = 0; i < Ds.Tables[0].Rows.Count; i++)
                {
                    //DynamicObject NewRow = new DynamicObject(entity.DynamicObjectType);
                    this.Model.CreateNewEntryRow("F_SZXY_Entity");


                    this.Model.SetValue("F_SZXY_BillNo", Convert.ToString(Ds.Tables[0].Rows[i]["FBILLNO"]), i);
                    this.Model.SetValue("F_SZXY_DocumentStatus", Convert.ToString(Ds.Tables[0].Rows[i]["FDocumentStatus"]), i);
                    this.Model.SetValue("F_SZXY_CreateDate", Convert.ToString(Ds.Tables[0].Rows[i]["FCreateDate"]), i);
                    this.Model.SetValue("F_SZXY_PONO", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_PONO"]), i);
                    this.Model.SetValue("F_SZXY_MoLineNo", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_MoLineNo"]), i);
                    this.Model.SetValue("F_SZXY_LEN", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_LEN"]), i);
                    this.Model.SetValue("F_SZXY_WIDTH", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_WIDTH"]), i);
                    this.Model.SetValue("F_SZXY_PLY", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_PLY"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_TEAMGROUP"] != null) this.Model.SetValue("F_SZXY_TEAMGROUP", Convert.ToInt32(Ds.Tables[0].Rows[i]["F_SZXY_TEAMGROUP"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_CLASSES"] != null) this.Model.SetValue("F_SZXY_CLASSES", Convert.ToInt32(Ds.Tables[0].Rows[i]["F_SZXY_CLASSES"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_Material"] != null) this.Model.SetValue("F_SZXY_Material", Convert.ToInt32(Ds.Tables[0].Rows[i]["F_SZXY_Material"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_OPERATOR"] != null) this.Model.SetValue("F_SZXY_OPERATOR", Convert.ToInt32(Ds.Tables[0].Rows[i]["F_SZXY_OPERATOR"]), i);


                    if (Ds.Tables[0].Rows[i]["F_SZXY_recipe"] != null) this.Model.SetValue("F_SZXY_recipe", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_recipe"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_SMark"] != null) this.Model.SetValue("F_SZXY_SMark", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_SMark"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_WIRED"] != null) this.Model.SetValue("F_SZXY_WIRED", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_WIRED"]), i);
                    if (Ds.Tables[0].Rows[i]["F_SZXY_MVolume"] != null) this.Model.SetValue("F_SZXY_MVolume", Convert.ToString(Ds.Tables[0].Rows[i]["F_SZXY_MVolume"]), i);

                    // Entrys.Add(NewRow);

                }
            }
        }

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.EqualsIgnoreCase("F_SZXY_Ok"))
            {
                DynamicObject Mateialobj = this.Model.GetValue("F_SZXY_Mateial") as DynamicObject;
                DynamicObject Maclobj = this.Model.GetValue("F_SZXY_Mac") as DynamicObject;
                int Mateial = 0;
                string Mac = string.Empty;
                if (Mateialobj != null && Maclobj != null)
                {

                    Mateial = Convert.ToInt32(Mateialobj["Id"]);
                    Mac = Convert.ToString(Maclobj["Id"]);
                }
                ScanReturnInfo returnInfo = new ScanReturnInfo();
                returnInfo.Mateial = Mateial;
                returnInfo.Mac = Mac;
                returnInfo.Row = Convert.ToInt32(index);
                int m = Convert.ToInt32(index);
                this.View.ReturnToParentWindow(new FormResult(returnInfo));
                this.View.Close();
                //(this.View.ParentFormView as IDynamicFormViewService).ButtonClick("F_SZXY_Button", "");
            }
            else
            {
                //ScanReturnInfo returnInfo = new ScanReturnInfo();
                //returnInfo.Mateial = 0;
                //returnInfo.Mac = "";
                //this.View.ReturnToParentWindow(new FormResult(new FormResult(returnInfo)));
                this.View.Close();
            }
        }
 
    }
}