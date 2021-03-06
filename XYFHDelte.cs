﻿ 

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
    [Description("复合单删除重置流转状态")]
    public class XYFHDelte : AbstractOperationServicePlugIn
    {

        IEnumerable<DynamicObject> selectedRows = null;

        /// <summary>
        /// 加载指定字段到实体里
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("F_SZXY_Machine");// 
            e.FieldKeys.Add("F_SZXY_FIDH");//   RJHFID
            e.FieldKeys.Add("F_SZXY_FEntryIDH");//  RJHROWID
            e.FieldKeys.Add("F_SZXY_MATERIALID");// 
            e.FieldKeys.Add("F_SZXY_ligature");// 
            e.FieldKeys.Add("F_SZXY_MotherVolume");//  
            e.FieldKeys.Add("F_SZXY_OrgId");//  
            e.FieldKeys.Add("F_SZXY_InArea");
            e.FieldKeys.Add("F_SZXY_PlasticNo");//流延编号
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
                string SoureFormId = "SZXY_FHJTRSCJH";
                //获取单据元数据
                FormMetadata BilMetada = metadataService.Load(Context, SoureFormId) as FormMetadata;

                foreach (DynamicObject dy in selectedRows)
                {
                    string Id = Convert.ToString(dy["Id"]);
                    string RJHFid = string.Empty;
                    string RJHRowId = string.Empty;
      
                    if (dy["SZXY_XYFHEntry"] is DynamicObjectCollection Entrys)
                    {
                        foreach (var item in Entrys.Where(op => !Convert.ToString(op["F_SZXY_PlasticNo"]).IsNullOrEmptyOrWhiteSpace()))
                        {
                            string LyNo = Convert.ToString(item["F_SZXY_PlasticNo"]);
                            //改写流转状态
                            string UpdateStateSql = string.Format("/*dialect*/update SZXY_t_XYLYEntry set  F_SZXY_CirculationState={0}   where F_SZXY_PlasticNo='{1}' ", 0, LyNo);
                            int res = DBServiceHelper.Execute(Context, UpdateStateSql);
                        }
                    }
                    
                }
            }

        }


    }
}
