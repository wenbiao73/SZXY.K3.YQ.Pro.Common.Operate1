using System;
using System.Data;
using Kingdee.BOS.Util;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Resource;
using Kingdee.BOS.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using System.Collections.Generic;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System.IO;
using Kingdee.BOS.Orm.DataEntity;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    public  class BarcodeScanerCreate : AbstractDynamicWebFormBuilderPlugIn
    {
        public override void CreateControl(CreateControlEventArgs e)
        {
            base.CreateControl(e);
            if (e.ControlAppearance.Key.EqualsIgnoreCase("FSPECSCANTEXT"))
            {
                var editor = e.Control["item"] as JSONObject;
                if (editor != null)
                    editor["xtype"] = "kdscantext";
            }
        }
    }
    /// <summary>
    /// 测试条形码扫描枪插件
    /// </summary>
    public class BarcodeScanerPlugIn : AbstractDynamicFormPlugIn
    {
        Dictionary<string, bool> verifyCodes = new Dictionary<string, bool>();

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (verifyCodes.Count == 0)
            {
                verifyCodes["1901232892136"] = true;
                verifyCodes["227250948129"] = true;
                verifyCodes["227250948128"] = true;
            }
            this.View.GetControl("FBARCODE").SetEnterNavLock(true);
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            if (e.Key == "FVERIFY")
            {
            }
            if (e.Key == "FENTER")
            {
                this.View.GetControl("FBARCODE").SetEnterNavLock(true);
            }
            if (e.Key == "FTAB")
            {
                this.View.GetControl("FBARCODE").SetTabNavLock(true);
            }
            if (e.Key == "FNULL")
            {
                this.View.GetControl("FBARCODE").SetEnterNavLock(false);
                this.View.GetControl("FBARCODE").SetTabNavLock(false);
            }
        }
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            if (!(e.Key.Equals("FBARCODE") || e.Key.Equals("FSPECSCANTEXT")))
            {
                return;
            }
            var code = e.Value != null ? e.Value.ToString() : "[null]";
            e.Cancel = true;

            if (string.IsNullOrWhiteSpace(code))
                return;

            // 添加新行
            var msg = "Error"; var verify = false;
            if (!verifyCodes.TryGetValue(code, out verify))
            {
                verify = false;
            }
            if (verify)
            {
                msg = "OK";
            }
            var isAppend = (bool)this.Model.GetValue("F_KD_Append");
            if (isAppend)
            {
                AppendRow(code, msg);
            }
            else
            {
                NewRowUpdateView(code, msg);
            }
        }

        private void AppendRow(string code, string msg)
        {
            var ekey = "FENTITY";
            this.Model.CreateNewEntryRow(ekey);
            int rowIndex = this.Model.GetEntryCurrentRowIndex(ekey);

            this.Model.SetValue("F_KD_BARCODE", code, rowIndex);
            this.Model.SetValue("F_KD_REMARK", msg, rowIndex);

            var info = this.View.OpenParameter.FormMetaData.BusinessInfo;
            var entity = info.GetEntity(ekey);
            var rowData = this.Model.GetEntityDataObject(entity, rowIndex);
            var field = info.GetField("F_KD_REMARK");
            this.View.StyleManager.SetEnabled(field.Key, rowData, "BillStatusByEntry", false);

            var redColor = "#FF0000";
            var whiteColor = "#FFFFFF";
            var blackColor = "#000000";
            var greenColor = "#00FFCC";
            var backColor = greenColor;
            var foreColor = whiteColor;
            var grid = this.View.GetControl<EntryGrid>(ekey);
            if (msg != "OK")
            {
                backColor = redColor;
            }
            else
            {
                foreColor = blackColor;
            }
            grid.SetForecolor("F_KD_REMARK", foreColor, rowIndex);
            grid.SetBackcolor("F_KD_REMARK", backColor, rowIndex);
        }

        private void NewRowUpdateView(string code, string msg)
        {
            var ekey = "FENTITY";
            var info = this.View.OpenParameter.FormMetaData.BusinessInfo;
            var ent = info.GetEntity(ekey);
            var entModel = this.Model.GetEntityDataObject(ent);
            var barCodeField = info.GetField("F_KD_BARCODE");
            var remarkField = info.GetField("F_KD_REMARK");
            var rowData = new DynamicObject(ent.DynamicObjectType);
            barCodeField.DynamicProperty.SetValue(rowData, code);
            remarkField.DynamicProperty.SetValue(rowData, msg);
            entModel.Add(rowData);
            this.View.UpdateView(ekey);
            var lastIdx = this.Model.GetRowIndex(ent, rowData);
            this.View.GetControl<EntryGrid>(ekey).SetFocusRowIndex(lastIdx);
            this.View.GetControl<EntryGrid>(ekey).SelectRows(new int[] { lastIdx });
        }
    }
}