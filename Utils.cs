using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Core;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DependencyRules;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldConverter;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    public static class Utils
    {
        /// <summary>
        /// 创建一个视图，后续将利用此视图的各种方法，设置应收单字段值
        /// </summary>
        /// <remarks>
        /// 理论上，也可以直接修改应收单的数据包达成修改数据的目的
        /// 但是，利用单据视图更具有优势：
        /// 1. 视图会自动触发插件，这样逻辑更加完整；
        /// 2. 视图会自动利用单据元数据，填写字段默认值，不用担心字段值不符合逻辑；
        /// 3. 字段改动，会触发实体服务规则；
        /// 
        /// 而手工修改数据包的方式，所有的字段值均需要自行填写，非常麻烦
        /// </remarks>
        /// <param name="context">上下文</param>
        /// <param name="id">ID，如果不存在=null或者""</param>
        public static IBillView CreateView(Context context, string id, string FormID)
        {
            //创建用于引入数据的单据view
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var view = (IDynamicFormViewService)Activator.CreateInstance(type);
            try
            {
                //读取应收单的元数据
                FormMetadata meta = MetaDataServiceHelper.Load(context, FormID) as FormMetadata;//"BD_Customer"
                Form form = meta.BusinessInfo.GetForm();
                //开始初始化receivableView：
                //创建视图加载参数对象，指定各种参数，如FormId, 视图(LayoutId)等
                BillOpenParameter openParam = CreateOpenParameter(meta, context);

                //动态领域模型服务提供类，通过此类，构建MVC实例
                var provider = form.GetFormServiceProvider();
                view.Initialize(openParam, provider);

                //载入数据
                ((IBillViewService)view).LoadData();

            }
            catch (Exception e)
            {
                Kingdee.BOS.Log.Logger.Info("新增保存界面", "CreateView" + e.Message);
                throw new Exception(e.Message);
            }
            return view as IBillView;

        }
        /// <summary>
        /// 创建视图加载参数对象，指定各种初始化视图时，需要指定的属性
        /// </summary>
        /// <param name="meta">元数据</param>
        /// <param name="context">上下文</param>
        /// <returns>视图加载参数对象</returns>
        public static BillOpenParameter CreateOpenParameter(FormMetadata meta, Context context)
        {
            Form form = meta.BusinessInfo.GetForm();
            //指定FormId, LayoutId
            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            try
            {
                //数据库上下文
                openParam.Context = context;
                //本单据模型使用的MVC框架
                openParam.ServiceName = form.FormServiceName;
                //随机产生一个不重复的PageId，作为视图的标识
                openParam.PageId = Guid.NewGuid().ToString();
                //元数据
                openParam.FormMetaData = meta;
                //界面状态：新增 (修改、查看)
                openParam.Status = OperationStatus.ADDNEW;
                //单据主键：本案例演示新建物料，不需要设置主键
                openParam.PkValue = null;
                //界面创建目的：普通无特殊目的 （为工作流、为下推、为复制等）
                openParam.CreateFrom = CreateFrom.Default;
                //基础资料分组维度：基础资料允许添加多个分组字段，每个分组字段会有一个分组维度
                //具体分组维度Id，请参阅 form.FormGroups 属性
                openParam.GroupId = "";
                //基础资料分组：如果需要为新建的基础资料指定所在分组，请设置此属性
                openParam.ParentId = 0;
                //单据类型
                openParam.DefaultBillTypeId = "";
                //业务流程
                openParam.DefaultBusinessFlowId = "";
                //主业务组织改变时，不用弹出提示界面
                openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
                //插件
                List<AbstractDynamicFormPlugIn> plugs = form.CreateFormPlugIns();
                openParam.SetCustomParameter(FormConst.PlugIns, plugs);
                PreOpenFormEventArgs args = new PreOpenFormEventArgs(context, openParam);
                foreach (var plug in plugs)
                {
                    //触发插件PreOpenForm事件，供插件确认是否允许打开界面
                    plug.PreOpenForm(args);
                }
                if (args.Cancel == true)
                {
                    //插件不允许打开界面
                    //本案例不理会插件的诉求，继续....
                }
                //返回

            }
            catch (Exception e)
            {
                Kingdee.BOS.Log.Logger.Info("新增保存参数", "BillOpenParameter" + e.Message);
                throw new Exception(e.Message);
            }
            return openParam;
        }
        /// <summary>
        /// 创建一个视图，后续将利用此视图的各种方法，设置应收单字段值
        /// </summary>
        /// <remarks>
        /// 理论上，也可以直接修改应收单的数据包达成修改数据的目的
        /// 但是，利用单据视图更具有优势：
        /// 1. 视图会自动触发插件，这样逻辑更加完整；
        /// 2. 视图会自动利用单据元数据，填写字段默认值，不用担心字段值不符合逻辑；
        /// 3. 字段改动，会触发实体服务规则；
        /// 
        /// 而手工修改数据包的方式，所有的字段值均需要自行填写，非常麻烦
        /// </remarks>
        /// <param name="context">上下文</param>
        /// <param name="id">ID，如果不存在=null或者""</param>PkValue
        public static IBillView CreateEditView(Context context, string id, string FormID, object PkValue)
        {
            //创建用于引入数据的单据view
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var view = (IDynamicFormViewService)Activator.CreateInstance(type);
            //读取应收单的元数据
            try
            {
                FormMetadata meta = MetaDataServiceHelper.Load(context, FormID) as FormMetadata;//"BD_Customer"
                Form form = meta.BusinessInfo.GetForm();
                //开始初始化receivableView：
                //创建视图加载参数对象，指定各种参数，如FormId, 视图(LayoutId)等
                BillOpenParameter openParam = CreateEditOpenParameter(meta, context, PkValue);
                //动态领域模型服务提供类，通过此类，构建MVC实例
                var provider = form.GetFormServiceProvider();
                view.Initialize(openParam, provider);
                //载入数据
                ((IBillViewService)view).LoadData();

            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return view as IBillView;
        }
        /// <summary>
        /// 创建视图加载参数对象，指定各种初始化视图时，需要指定的属性
        /// </summary>
        /// <param name="meta">元数据</param>
        /// <param name="context">上下文</param>
        /// <returns>视图加载参数对象</returns>
        public static BillOpenParameter CreateEditOpenParameter(FormMetadata meta, Context context, object PkValue)
        {
            Form form = meta.BusinessInfo.GetForm();
            //指定FormId, LayoutId
            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            try
            {
                Kingdee.BOS.Log.Logger.Info("修改", "CreateEditOpenParameter" + "A");
                //数据库上下文
                openParam.Context = context;
                //本单据模型使用的MVC框架
                openParam.ServiceName = form.FormServiceName;
                //随机产生一个不重复的PageId，作为视图的标识
                openParam.PageId = Guid.NewGuid().ToString();
                //元数据
                openParam.FormMetaData = meta;
                Kingdee.BOS.Log.Logger.Info("修改", "CreateEditOpenParameter" + "B");
                //界面状态：新增 (修改、查看)
                openParam.Status = OperationStatus.VIEW;
                //单据主键：本案例演示新建物料，不需要设置主键
                openParam.PkValue = PkValue;
                //界面创建目的：普通无特殊目的 （为工作流、为下推、为复制等）
                openParam.CreateFrom = CreateFrom.Default;
                //基础资料分组维度：基础资料允许添加多个分组字段，每个分组字段会有一个分组维度
                //具体分组维度Id，请参阅 form.FormGroups 属性
                openParam.GroupId = "";
                //基础资料分组：如果需要为新建的基础资料指定所在分组，请设置此属性
                openParam.ParentId = 0;
                //单据类型
                openParam.DefaultBillTypeId = "";
                //业务流程
                openParam.DefaultBusinessFlowId = "";
                //主业务组织改变时，不用弹出提示界面
                openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
                //插件
                List<AbstractDynamicFormPlugIn> plugs = form.CreateFormPlugIns();
                openParam.SetCustomParameter(FormConst.PlugIns, plugs);
                PreOpenFormEventArgs args = new PreOpenFormEventArgs(context, openParam);
                foreach (var plug in plugs)
                {
                    //触发插件PreOpenForm事件，供插件确认是否允许打开界面
                    plug.PreOpenForm(args);
                }
                if (args.Cancel == true)
                {
                    //插件不允许打开界面
                    //本案例不理会插件的诉求，继续....
                }

            }
            catch (Exception e)
            {
                Kingdee.BOS.Log.Logger.Info("修改保存界面", "CreateEditOpenParameter" + e.Message);
                throw new Exception(e.Message);
            }
            //返回
            return openParam;
        }
        /// <summary>
        /// 暂存
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="operateOption">操作选项</param>
        /// <param name="context">上下文</param>
        public static IOperationResult Draft(IBillView view, DynamicObject[] ListObject, OperateOption operateOption, Context context)
        {
            //调用保存操作
            // 设置FormId
            //Form form = view.BillBusinessInfo.GetForm();
            //if (form.FormIdDynamicProperty != null)
            //{
            //    form.FormIdDynamicProperty.SetValue(view.Model.DataObject, form.Id);
            //}
            try
            {
                //IOperationResult operationResult = BusinessDataServiceHelper.Save(context, view.BillBusinessInfo, view.Model.DataObject, operateOption, "Save");
                IOperationResult operationResult = BusinessDataServiceHelper.Draft(context, view.BillBusinessInfo, ListObject, operateOption, "Draft");
                Kingdee.BOS.Log.Logger.Info("开始", "SaveA");
                //显示处理结果
                if (operationResult == null)
                {
                    throw new Exception("保存失败！未知原因。");
                }
                else if (operationResult.IsSuccess == true)
                {
                    //保存成功，直接返回
                    Kingdee.BOS.Log.Logger.Info("开始", "SaveC" + "保存成功！");
                    return operationResult;
                }
                else
                {
                    //保存失败，显示错误信息
                    if (operationResult.IsShowMessage)
                    {
                        operationResult.MergeValidateErrors();
                        string errorMsg = "";
                        foreach (OperateResult orResult in operationResult.OperateResult)
                        {
                            errorMsg += orResult.Message + "\r\n";
                        }
                        Kingdee.BOS.Log.Logger.Info("开始", "SaveC" + errorMsg);
                        throw new Exception(errorMsg);
                    }
                    else
                    {
                        throw new Exception();

                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="operateOption">操作选项</param>
        /// <param name="context">上下文</param>
        public static IOperationResult Save(IBillView view, DynamicObject[] ListObject, OperateOption operateOption, Context context)
        {
            //调用保存操作
            // 设置FormId
            //Form form = view.BillBusinessInfo.GetForm();
            //if (form.FormIdDynamicProperty != null)
            //{
            //    form.FormIdDynamicProperty.SetValue(view.Model.DataObject, form.Id);
            //}
           // billobj["FFormId"] = this.View.BusinessInfo.GetForm().Id;
            try
            {
                //IOperationResult operationResult = BusinessDataServiceHelper.Save(context, view.BillBusinessInfo, view.Model.DataObject, operateOption, "Save");
                IOperationResult operationResult = BusinessDataServiceHelper.Save(context, view.BillBusinessInfo, ListObject, operateOption, "Save");
                Kingdee.BOS.Log.Logger.Info("开始", "SaveA");
                //显示处理结果
                if (operationResult == null)
                {
                    throw new Exception("保存失败！未知原因。");
                }
                else if (operationResult.IsSuccess == true)
                {
                    //保存成功，直接返回
                    Kingdee.BOS.Log.Logger.Info("开始", "SaveC" + "保存成功！");
                    return operationResult;
                }
                else
                {
                    //保存失败，显示错误信息
                    if (operationResult.IsShowMessage)
                    {
                        operationResult.MergeValidateErrors();
                        string errorMsg = "";
                        foreach (OperateResult orResult in operationResult.OperateResult)
                        {
                            errorMsg += orResult.Message + "\r\n";
                        }
                        Kingdee.BOS.Log.Logger.Info("开始", "SaveC" + errorMsg);
                        throw new Exception(errorMsg);
                    }
                    else
                    {
                        throw new Exception();

                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="businessInfo">配置信息</param>
        /// <param name="id">ID</param>
        /// <param name="operateOption">操作选项</param>
        /// <param name="context">上下文</param>
        public static IOperationResult Submit(BusinessInfo businessInfo, object[] FID, OperateOption operateOption, Context context)
        {
            try
            {
                //调用提交操作
                IOperationResult operationResult = BusinessDataServiceHelper.Submit(context, businessInfo, FID, "Submit", operateOption);
                //显示处理结果
                if (operationResult == null)
                {
                    throw new Exception("提交失败！未知原因。");
                }
                else if (operationResult.IsSuccess == true)
                {
                    //提交成功，直接返回
                    return operationResult;
                }
                else
                {
                    //提交失败，显示错误信息
                    if (operationResult.IsShowMessage)
                    {
                        operationResult.MergeValidateErrors();
                        string errorMsg = "";
                        foreach (OperateResult orResult in operationResult.OperateResult)
                        {
                            errorMsg += orResult.Message + "\r\n";
                        }
                        Kingdee.BOS.Log.Logger.Info("提交", "SubmitA" + errorMsg);
                        throw new Exception(errorMsg);
                    }
                    else
                    {
                        throw new Exception("提交失败！未知原因。");
                    }
                }
            }
            catch (Exception e)
            {
                Kingdee.BOS.Log.Logger.Info("提交", "Submit" + e.Message);
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 审核
        /// </summary>
        /// <param name="businessInfo">配置信息</param>
        /// <param name="id">ID</param>
        /// <param name="operateOption">操作选项</param>
        /// <param name="context">上下文</param>
        public static IOperationResult Audit(BusinessInfo businessInfo, object[] FID, OperateOption operateOption, Context context)
        {
            try
            {
                //调用审核操作
                FormOperation formOperation = businessInfo.GetForm().GetOperation(OperationNumberConst.OperationNumber_Audit);
                SetStatus setStatus = new SetStatus(context);
                setStatus.Initialize(businessInfo, formOperation.Operation, operateOption);
                IOperationResult operationResult = setStatus.Excute(FID);

                //显示处理结果
                if (operationResult == null)
                {
                    throw new Exception("审核失败！未知原因。");
                }
                else if (operationResult.IsSuccess == true)
                {
                    //审核成功，直接返回
                    return operationResult;
                }
                else
                {
                    //审核失败，显示错误信息
                    if (operationResult.IsShowMessage)
                    {
                        operationResult.MergeValidateErrors();
                        string errorMsg = "";
                        foreach (OperateResult orResult in operationResult.OperateResult)
                        {
                            errorMsg += orResult.Message + "\r\n";
                        }
                        Kingdee.BOS.Log.Logger.Info("审核", "OKFillA" + errorMsg);
                        throw new Exception(errorMsg);
                    }
                    else
                    {
                        throw new Exception("审核失败！位置原因。");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        /// <summary>
        /// 删除
        /// panqing 2017.2.22
        /// </summary>
        /// <param name="businessInfo">配置信息</param>
        /// <param name="id">ObjectID</param>
        /// <param name="operateOption">操作选项</param>
        /// <param name="context">上下文</param>
        public static void Delete(IBillView view, Object[] ObjectID, OperateOption operateOption, Context context)
        {
            //调用删除操作
            IOperationResult operationResult = BusinessDataServiceHelper.Delete(context, view.BillBusinessInfo, ObjectID, operateOption, "Delete");
            Kingdee.BOS.Log.Logger.Info("删除", "DeleteA");
            //显示处理结果
            if (operationResult == null)
            {
                Kingdee.BOS.Log.Logger.Info("删除", "DeleteA" + "保存失败！未知原因。");
                throw new Exception("保存失败！未知原因。");
            }
            else if (operationResult.IsSuccess == true)
            {
                Kingdee.BOS.Log.Logger.Info("删除", "DeleteA" + "保存成功！");
                //保存成功，直接返回
                return;
            }
            else
            {
                //保存失败，显示错误信息
                if (operationResult.IsShowMessage)
                {
                    operationResult.MergeValidateErrors();
                    string errorMsg = "";
                    foreach (OperateResult orResult in operationResult.OperateResult)
                    {
                        errorMsg += orResult.Message + "\r\n";
                    }
                    Kingdee.BOS.Log.Logger.Info("删除", "DeleteB" + errorMsg);
                    throw new Exception(errorMsg);
                }
                else
                {
                    throw new Exception();

                }
            }
        }
        /// 通用取数据集函数
        /// panqing 2016.7.15
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Field"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public static DataSet CBData(string sql, Context context)
        {
            DataSet CBData = null;
            StringBuilder CBSql = new StringBuilder();
            CBSql.Append(sql);
            CBData = DBServiceHelper.ExecuteDataSet(context, CBSql.ToString());
            return CBData;
        }
        /// <summary>
        /// 数据库删除,通常不采用这种方式
        /// panqing 2017.2.22
        /// </summary>
        /// <param name="businessInfo">配置信息</param>
        /// <param name="id">ObjectID</param>
        /// <param name="operateOption">操作选项</param>
        /// <param name="context">上下文</param>
        public static string SDelete(string TableName, string Condition, Context context)
        {
            //调用删除操作
            int ReSet = 0;
            StringBuilder CBSql = new StringBuilder();
            CBSql.Append("delete ");
            CBSql.Append(TableName);
            CBSql.Append(" ");
            CBSql.Append(Condition);
            ReSet = DBServiceHelper.Execute(context, CBSql.ToString());
            return Convert.ToString(ReSet);
        }
        public static List<string> ResolveXML(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            List<string> User = new List<string>();
            User.Clear();
            User.Add(Convert.ToString(xmlDoc.FirstChild.Attributes["F_BGJ_LoadAdress"].Value));
            User.Add(Convert.ToString(xmlDoc.FirstChild.Attributes["F_BGJ_AccountID"].Value));
            User.Add(Convert.ToString(xmlDoc.FirstChild.Attributes["F_BGJ_Users"].Value));
            User.Add(Convert.ToString(xmlDoc.FirstChild.Attributes["F_BGJ_Password"].Value));
            return User;
        }
        /// <summary>
        /// 给基础资料字段赋值 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="Row对象"></param>
        /// <param name="bdfield"></param>
        /// <param name="ID"></param>
        /// <param name="dyValue"></param>
        /// <param name="context"></param>
        public static void SetBaseDataValue(IViewService service, DynamicObject data, BaseDataField bdfield, long value, ref DynamicObject dyValue, Context context)
        {
            if (value == 0)
            {
                //bdfield.RefIDDynamicProperty.SetValue(data, 0);
                //bdfield.DynamicProperty.SetValue(data, null);
                dyValue = null;
            }
            else
            {
                if (dyValue == null)
                {
                    dyValue = service.LoadSingle(context, value, bdfield.RefFormDynamicObjectType);
                }
                if (dyValue != null)
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, value);
                    bdfield.DynamicProperty.SetValue(data, dyValue);
                }
                else
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, 0);
                    bdfield.DynamicProperty.SetValue(data, null);
                }
            }

          
        }

        public static void SetFZZLDataValue(IViewService service, DynamicObject data, BaseDataField bdfield, ref DynamicObject dyValue, Context context, string FzZlId)
        {
            if (FzZlId == "")
            {
                //bdfield.RefIDDynamicProperty.SetValue(data, 0);
                //bdfield.DynamicProperty.SetValue(data, null);
                dyValue = null;
            }
            else
            {
                if (dyValue == null)
                {
                    dyValue = service.LoadSingle(context, FzZlId, bdfield.RefFormDynamicObjectType);
                }
                if (dyValue != null)
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, FzZlId);
                    bdfield.DynamicProperty.SetValue(data, dyValue);
                }
                else
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, 0);
                    bdfield.DynamicProperty.SetValue(data, null);
                }
            }

         
        }
        // <summary>
        /// 给维度字段赋值 
        /// </summary>
        public static void SetFlexDataValue(IViewService service, DynamicObject data, RelatedFlexGroupField bdfield, long value, ref DynamicObject dyValue, Context context)
        {
            if (value == 0)
            {
                //bdfield.RefIDDynamicProperty.SetValue(data, 0);
                //bdfield.DynamicProperty.SetValue(data, null);
                dyValue = null;
            }
            else
            {
                if (dyValue == null)
                {
                    dyValue = service.LoadSingle(context, value, bdfield.RefFormDynamicObjectType);
                }
                if (dyValue != null)
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, value);
                    bdfield.DynamicProperty.SetValue(data, dyValue);
                }
                else
                {
                    bdfield.RefIDDynamicProperty.SetValue(data, 0);
                    bdfield.DynamicProperty.SetValue(data, null);
                }
            }
        }

        /// <summary>
        /// 显示表单
        /// </summary>
        /// <param name="view"></param>
        /// <param name="panelKey"></param>
        /// <returns></returns>
        public static void ShowForm(this IDynamicFormView view, string formId, string panelKey = null, string pageId = null, Action<FormResult> callback = null, Action<DynamicFormShowParameter> showParaCallback = null)
        {
            DynamicFormShowParameter showPara = new DynamicFormShowParameter();
            showPara.PageId = string.IsNullOrWhiteSpace(pageId) ? Guid.NewGuid().ToString() : pageId;
            showPara.ParentPageId = view.PageId;
            if (string.IsNullOrWhiteSpace(panelKey))
            {
                showPara.OpenStyle.ShowType = ShowType.Default;
            }
            else
            {
                showPara.OpenStyle.ShowType = ShowType.InContainer;
                showPara.OpenStyle.TagetKey = panelKey;
            }
            showPara.FormId = formId;
            showPara.OpenStyle.CacheId = pageId;
            if (showParaCallback != null)
            {
                showParaCallback(showPara);
            }

            view.ShowForm(showPara, callback);
        }

        /// <summary>
        /// 显示列表
        /// </summary>
        /// <param name="view"></param>
        /// <param name="formId"></param>
        /// <param name="listType"></param>
        /// <param name="bMultiSel"></param>
        /// <param name="callback"></param>
        public static void ShowList(this IDynamicFormView view, string formId, BOSEnums.Enu_ListType listType, bool bMultiSel = true, string filter = "", Action<ListShowParameter> showPara = null, Action<FormResult> callback = null)
        {
            ListShowParameter listShowPara = new ListShowParameter();
            listShowPara.FormId = formId;
            listShowPara.PageId = Guid.NewGuid().ToString();
            listShowPara.ParentPageId = view.PageId;
            listShowPara.MultiSelect = bMultiSel;
            listShowPara.ListType = (int)listType;
            if (listType == BOSEnums.Enu_ListType.SelBill)
            {
                listShowPara.IsLookUp = true;
            }
            listShowPara.ListFilterParameter.Filter = listShowPara.ListFilterParameter.Filter.JoinFilterString(filter);
            listShowPara.IsShowUsed = true;
            listShowPara.IsShowApproved = false;
            if (showPara != null) showPara(listShowPara);

            view.ShowForm(listShowPara, callback);
        }

        /// <summary>
        /// 设置某个实体整体可用性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="bEnabled"></param>
        public static void SetEntityEnabled(this IDynamicFormView view, string entityKey, bool bEnabled)
        {
            EntityAppearance entityAp = view.LayoutInfo.GetEntityAppearance(entityKey);
            if (entityAp == null) return;
            foreach (var ap in entityAp.Layoutinfo.Controls)
            {
                view.StyleManager.SetEnabled(ap, null, bEnabled);
            }
        }

        /// <summary>
        /// 设置行可用性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="row"></param>
        /// <param name="bEnabled"></param>
        /// <param name="exceptFieldkeys"></param>
        public static void SetEntityRowEnabled(this IDynamicFormView view, string entityKey, int row, bool bEnabled, IEnumerable<string> exceptFieldkeys = null)
        {
            Entity entity = view.BillBusinessInfo.GetEntity(entityKey);
            DynamicObject rowObj = view.Model.GetEntityDataObject(entity, row);

            SetEntityRowEnabled(view, entityKey, rowObj, bEnabled, exceptFieldkeys);
        }

        /// <summary>
        /// 设置行可用性
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="rowObject"></param>
        /// <param name="bEnabled"></param>
        /// <param name="exceptFieldkeys"></param>
        public static void SetEntityRowEnabled(this IDynamicFormView view, string entityKey, DynamicObject rowObject, bool bEnabled, IEnumerable<string> exceptFieldkeys = null)
        {
            if (exceptFieldkeys == null) exceptFieldkeys = new string[] { };

            foreach (Field field in (from o in view.BillBusinessInfo.GetEntryEntity(entityKey).Fields
                                     where !exceptFieldkeys.Contains(o.Key)
                                     select o).ToList<Field>())
            {
                view.StyleManager.SetEnabled(field, rowObject, null, bEnabled);
            }
        }

        /// <summary>
        /// 设置工具条整体可用状态
        /// </summary>
        /// <param name="view"></param>
        /// <param name="bEnabled">可用性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarEnabled(this IDynamicFormView view, bool bEnabled, string barOwnerKey = "")
        {
            if (string.IsNullOrWhiteSpace(barOwnerKey))
            {
                FormAppearance formAppearance = view.LayoutInfo.GetFormAppearance();
                if (((formAppearance.Menu != null) && !string.IsNullOrWhiteSpace(formAppearance.Menu.Id)) && (formAppearance.ShowMenu == 1))
                {
                    foreach (var item in formAppearance.Menu.GetAllBarItems())
                    {
                        view.SetBarItemEnabled(item.Key, bEnabled, barOwnerKey);
                    }
                }
            }
            else
            {
                EntryEntityAppearance appearance3 = view.LayoutInfo.GetEntryEntityAppearance(barOwnerKey);
                if ((appearance3 != null) && (appearance3.Menu != null))
                {
                    foreach (var item in appearance3.Menu.GetAllBarItems())
                    {
                        view.SetBarItemEnabled(item.Key, bEnabled, barOwnerKey);
                    }
                }

                ToolBarCtrlAppearance appearance4 = view.LayoutInfo.GetToolbarCtrlAppearances().FirstOrDefault(o => o.Key == barOwnerKey);
                if ((appearance4 != null) && (appearance4.Menu != null))
                {
                    foreach (var item in appearance4.Menu.GetAllBarItems())
                    {
                        view.SetBarItemEnabled(item.Key, bEnabled, barOwnerKey);
                    }
                }
            }

        }

        /// <summary>
        /// 设置按钮可用状态
        /// </summary>
        /// <param name="view"></param>
        /// <param name="barItemKey">按钮标识</param>
        /// <param name="bEnabled">可用性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarItemEnabled(this IDynamicFormView view, string barItemKey, bool bEnabled, string barOwnerKey = "")
        {
            Appearance ap = null;
            if (!string.IsNullOrWhiteSpace(barOwnerKey))
                ap = view.LayoutInfo.GetAppearance(barOwnerKey);

            BarItemControl barItem = null;
            if (ap == null)
            {
                barItem = view.GetMainBarItem(barItemKey);

                if (barItem != null)
                {
                    barItem.Enabled = bEnabled;
                }
            }

            foreach (var entityAp in view.LayoutInfo.GetEntityAppearances())
            {
                if (entityAp is HeadEntityAppearance || entityAp is SubHeadEntityAppearance) continue;

                if (barOwnerKey.IsNullOrEmptyOrWhiteSpace() || entityAp.Key.EqualsIgnoreCase(barOwnerKey))
                {
                    barItem = view.GetBarItem(entityAp.Key, barItemKey);

                    if (barItem != null)
                    {
                        barItem.Enabled = bEnabled;
                    }
                }
            }

        }

        /// <summary>
        /// 设置按钮可见状态
        /// </summary>
        /// <param name="view"></param>
        /// <param name="barItemKey">按钮标识</param>
        /// <param name="bVisible">可见性</param>
        /// <param name="barOwnerKey">工具条拥有者标识，单据主工具条不用传值，表格工具条请传表格标识，其它独立工具条请传工具条标识</param>
        public static void SetBarItemVisible(this IDynamicFormView view, string barItemKey, bool bVisible, string barOwnerKey = "")
        {
            Appearance ap = null;
            if (!string.IsNullOrWhiteSpace(barOwnerKey))
                ap = view.LayoutInfo.GetAppearance(barOwnerKey);

            BarItemControl barItem = null;
            if (ap == null)
            {
                barItem = view.GetMainBarItem(barItemKey);
            }
            else
            {
                barItem = view.GetBarItem(ap.Key, barItemKey);
            }
            if (barItem != null)
            {
                barItem.Visible = bVisible;
            }
        }

        /// <summary>
        /// 更新可视元素宽度
        /// </summary>
        /// <param name="formState"></param>
        /// <param name="key"></param>
        /// <param name="width"></param>
        public static void UpdateColumnWidth(this IDynamicFormView view, ControlAppearance gridAp, string colKey, int width)
        {
            IDynamicFormState formState = view.GetService<IDynamicFormState>();
            //SetFieldPropValue(formState, ctlAp.Key, "width", width, -1);
            SetColumnPropValue(formState, gridAp, colKey, "width", width);
        }

        public static void UpdateColumnHeader(this IDynamicFormView view, ControlAppearance gridAp, string colKey, string header)
        {
            IDynamicFormState formState = view.GetService<IDynamicFormState>();
            SetColumnPropValue(formState, gridAp, colKey, "header", header);
        }

        private static void SetFieldPropValue(IDynamicFormState formState, string key, string propName, object value, int rowIndex)
        {
            JSONObject obj2 = formState.GetControlProperty(key, -1, propName) as JSONObject;
            if (obj2 == null)
            {
                obj2 = new JSONObject();
            }
            obj2[rowIndex.ToString()] = value;
            formState.SetControlProperty(key, rowIndex, propName, obj2);
        }

        private static void SetColumnPropValue(IDynamicFormState formState, ControlAppearance ctlAp, string colKey, string propName, object value)
        {
            JSONObject obj2 = new JSONObject();
            obj2["key"] = colKey;
            obj2[propName] = value;
            formState.InvokeControlMethod(ctlAp, "UpdateFieldStates", new object[] { obj2 });
        }

        /// <summary>
        /// 移动表格分录
        /// </summary>
        /// <param name="view"></param>
        /// <param name="entityKey"></param>
        /// <param name="iSrcRowIndex"></param>
        /// <param name="iDstRowIndex"></param>
        /// <param name="callback"></param>
        public static void MoveEntryRow(this IDynamicFormView view, string entityKey, int iSrcRowIndex, int iDstRowIndex, Action<int, int> callback = null)
        {
            EntryEntity entryEntity = view.BillBusinessInfo.GetEntryEntity(entityKey);
            DynamicObjectCollection dataEntities = view.Model.GetEntityDataObject(entryEntity);
            if (iSrcRowIndex < 0 || iSrcRowIndex >= dataEntities.Count) return;
            if (iDstRowIndex < 0 || iDstRowIndex >= dataEntities.Count) return;
            var srcRow = dataEntities[iSrcRowIndex];
            var dstRow = dataEntities[iDstRowIndex];
            if (iSrcRowIndex > iDstRowIndex)
            {
                dataEntities.RemoveAt(iSrcRowIndex);
                dataEntities.Insert(iDstRowIndex, srcRow);
            }
            else
            {
                dataEntities.RemoveAt(iDstRowIndex);
                dataEntities.Insert(iSrcRowIndex, dstRow);
            }

            EntryGrid grid = view.GetControl<EntryGrid>(entityKey);
            grid.ExchangeRowIndex(iSrcRowIndex, iDstRowIndex);
            grid.SetFocusRowIndex(iDstRowIndex);

            if (callback != null)
            {
                callback(iSrcRowIndex, iDstRowIndex);
            }
        }

        #region 实现块粘贴的填充功能
        /// <summary>
        /// 处理Excel块粘贴功能
        /// </summary>
        /// <param name="view"></param>
        /// <param name="e"></param>
        /// <param name="bAllowAutoNewRows">允许自动新增行</param>
        /// <param name="bCanPaste">是否允许填充某字段</param>
        public static void PasteBlockData(this IDynamicFormView view, EntityBlockPastingEventArgs e, bool bAllowAutoNewRows = false, Func<FieldAppearance, int, bool> bCanPaste = null)
        {
            if (e.BlockValue.IsNullOrEmptyOrWhiteSpace()) return;
            FieldAppearance startFieldAp = view.LayoutInfo.GetFieldAppearance(e.StartKey);
            if (startFieldAp == null || (startFieldAp.Field.Entity is EntryEntity) == false) return;
            EntryEntity entryEntity = (EntryEntity)startFieldAp.Field.Entity;
            int iTotalRows = view.Model.GetEntryRowCount(entryEntity.Key);

            var copyOperation = view.BillBusinessInfo.GetForm().FormOperations
                        .FirstOrDefault(o => o.OperationId == 31 && string.Equals(o.Parmeter.OperationObjectKey, entryEntity.Key, StringComparison.InvariantCultureIgnoreCase));
            bool isCopyLinkEntry = false;
            //如果表格未配置复制行操作，则不允许自动新增行
            if (copyOperation == null)
            {
                bAllowAutoNewRows = false;
            }
            else
            {
                isCopyLinkEntry = GetIsCopyLinkEntryParam(copyOperation.Parmeter);
            }

            string[] strBlockDataRows = e.BlockValue.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int iRow = e.StartRow;
            foreach (var rowData in strBlockDataRows)
            {
                if (iRow >= iTotalRows)
                {
                    if (bAllowAutoNewRows)
                    {
                        view.Model.CopyEntryRow(entryEntity.Key, iRow - 1, iRow, isCopyLinkEntry);
                    }
                    else
                    {
                        break;
                    }
                }
                string[] strItemValues = rowData.Split(new char[] { '\t' });

                FieldAppearance fieldAp = startFieldAp;
                foreach (var value in strItemValues)
                {
                    if (fieldAp == null) continue;
                    object objValue = value;

                    if (typeof(ValueType).IsAssignableFrom(fieldAp.Field.GetPropertyType()))
                    {
                        if (value.IsNullOrEmptyOrWhiteSpace())
                        {
                            objValue = 0;
                        }
                        else
                        {
                            ValueTypeConverter converter = new ValueTypeConverter();
                            if (value != null && converter.CanConvertTo(value.GetType()))
                            {
                                objValue = converter.ConvertTo(value, fieldAp.Field.GetPropertyType());
                            }
                        }
                    }
                    if (bCanPaste == null || bCanPaste(fieldAp, iRow))
                    {
                        (view as IDynamicFormViewService).UpdateValue(fieldAp.Key, iRow, objValue);
                    }
                    fieldAp = GetNextEditFieldAp(view, fieldAp, iRow);
                }

                iRow++;
            }
        }

        private static FieldAppearance GetNextEditFieldAp(IDynamicFormView view, FieldAppearance fieldAp, int iRow)
        {
            FieldAppearance nextFieldAp = null;
            if (fieldAp != null)
            {
                EntryEntityAppearance entryEntityAp = view.LayoutInfo.GetEntryEntityAppearance(fieldAp.EntityKey);
                if (entryEntityAp != null)
                {
                    DynamicObject rowData = view.Model.GetEntityDataObject(entryEntityAp.Entity, iRow);
                    int iStartFindPos = entryEntityAp.Layoutinfo.Appearances.IndexOf(fieldAp);
                    if (iStartFindPos >= 0)
                    {
                        for (int i = iStartFindPos + 1; i < entryEntityAp.Layoutinfo.Appearances.Count; i++)
                        {
                            nextFieldAp = entryEntityAp.Layoutinfo.Appearances[i] as FieldAppearance;
                            if (nextFieldAp == null) continue;
                            //跳过不可见或不可编辑的字段
                            if (nextFieldAp.IsLocked(view.OpenParameter.Status) == true
                                || nextFieldAp.IsVisible(view.OpenParameter.Status) == false) continue;

                            //单元格锁定也不填充
                            if (rowData != null && view.StyleManager.GetEnabled(fieldAp, rowData) == false) continue;

                            break;
                        }
                    }
                }
            }

            return nextFieldAp;
        }

        private static bool GetIsCopyLinkEntryParam(OperationParameter operationParameter)
        {
            bool flag = false;
            string expressValue = operationParameter.ExpressValue;
            if (!string.IsNullOrEmpty(expressValue))
            {
                string[] strArray = expressValue.Split(new char[] { ':' });
                if (strArray.Length == 2)
                {
                    flag = Convert.ToInt32(strArray[1]) == 1;
                }
            }
            return flag;
        }


        #endregion
        public static DataTable SortDataTable(DataTable dt, string strExpr, string strSort)
        {
            dt.DefaultView.RowFilter = strExpr;
            dt.DefaultView.Sort = strSort;
            return dt;
        }
        /// <summary>
        /// 根据单据编号，加载单据数据包
        /// </summary>
        /// <param name="">单据FormId</param>
        /// <param name="billno">单据编号</param>
        /// <returns></returns>
        public static DynamicObject LoadOrgBillObject(Context context, string formId, string billno, string OrgId)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(context, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
            queryParam.FormId = formId;
            queryParam.BusinessInfo = meta.BusinessInfo;

            queryParam.FilterClauseWihtKey = string.Format(" {0} = '{1}' and {2} = {3}",
                meta.BusinessInfo.GetBillNoField().Key,
                billno, meta.BusinessInfo.MainOrgField.FieldName, OrgId);

            var objs = BusinessDataServiceHelper.Load(context,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);

            return objs[0];
        }
        /// <summary>
        /// 根据单据编号，加载单据数据包
        /// </summary>
        /// <param name="">单据FormId</param>
        /// <param name="billno">单据编号</param>
        /// <returns></returns>
        public static DynamicObject LoadBillObject(Context context, string formId, string billno)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(context, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
            queryParam.FormId = formId;
            queryParam.BusinessInfo = meta.BusinessInfo;

            queryParam.FilterClauseWihtKey = string.Format(" {0} = '{1}'",
                meta.BusinessInfo.GetBillNoField().Key,
                billno);

            var objs = BusinessDataServiceHelper.Load(context,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);

            return objs[0];
        }
        public static DynamicObject LoadFIDBillObject(Context context, string formId, string FID)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(context, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
            queryParam.FormId = formId;
            queryParam.BusinessInfo = meta.BusinessInfo;

            queryParam.FilterClauseWihtKey = string.Format(" FID = {0}", FID);

            var objs = BusinessDataServiceHelper.Load(context,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);

            return objs[0];
        }
        /// <summary>
        /// 根据基础资料的编码，加载基础资料数据包
        /// </summary>
        /// <param name="fieldKey">基础资料字段Key</param>
        /// <param name="number">基础资料编码</param>
        /// <returns></returns>
        public static DynamicObject LoadBDFieldObject(Context context, string formId, string fieldKey, string number)
        {
            //BaseDataField bdField = this.View.BillBusinessInfo.GetField(fieldKey) as BaseDataField;       
            FormMetadata meta = MetaDataServiceHelper.Load(context, formId) as FormMetadata;
            BaseDataField bdField = meta.BusinessInfo.GetField(fieldKey) as BaseDataField;
            // 构建查询参数，设置过滤条件
            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
            queryParam.FormId = bdField.LookUpObject.FormId;

            queryParam.FilterClauseWihtKey = string.Format(" {0} = '{1}' ",
                bdField.NumberProperty.Key,
                number);

            var bdObjs = BusinessDataServiceHelper.Load(context,
                bdField.RefFormDynamicObjectType,
                queryParam);

            return bdObjs[0];
        }
        /// <summary>
        /// 根据基础资料的编码，加载基础资料数据包
        /// </summary>
        /// <param name="">基础资料FormId</param>
        /// <param name="number">基础资料编码</param>
        /// <returns></returns>
        public static DynamicObject LoadBDFullObject(Context context, string formId, string number)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(context, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
            queryParam.FormId = formId;
            queryParam.BusinessInfo = meta.BusinessInfo;

            queryParam.FilterClauseWihtKey = string.Format(" {0} = '{1}' ",
                meta.BusinessInfo.GetForm().NumberFieldKey,
                number);

            var bdObjs = BusinessDataServiceHelper.Load(context,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);

            return bdObjs[0];
        }
        /// <summary>
        /// 根据基础资料的编码，加载基础资料数据包
        /// </summary>
        /// <param name="">基础资料FormId</param>
        /// <param name="number">基础资料编码</param>
        /// <returns></returns>
        public static DynamicObject LoadOrgBDFullObject(Context context, string formId, string number, string OrgId)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(context, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            QueryBuilderParemeter queryParam = new QueryBuilderParemeter();
            queryParam.FormId = formId;
            queryParam.BusinessInfo = meta.BusinessInfo;

            queryParam.FilterClauseWihtKey = string.Format(" {0} = '{1}' and {2}={3} ",
                meta.BusinessInfo.GetForm().NumberFieldKey,
                number, meta.BusinessInfo.GetForm().UseOrgFieldKey, OrgId);

            DynamicObject[] bdObjs = BusinessDataServiceHelper.Load(context,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);
            DynamicObject Objs = null;
            if (bdObjs != null) Objs = bdObjs[0];
            return Objs;
        }
        public static string GetNumCaption(BusinessInfo info, string key, string value)
        {
            string Caption = "";
            Field field = info.GetField(key);
            //转为ComboField
            ComboField comboField = field as ComboField;
            //获取下拉列表字段绑定的枚举
            var enumObj = (EnumObject)comboField.EnumObject;
            //根据枚举值获取枚举项，然后拿枚举项的枚举名称
            Caption = enumObj.Items.FirstOrDefault(p => p.Value.Equals(value)).Caption.ToString();
            return Caption;
        }
        /// <summary>
        /// 判断是否数字
        /// </summary>
        /// <param name="strNumber"></param>
        /// <returns></returns>
        public static bool IsNumber(String strNumber)
        {
            Regex objNotNumberPattern = new Regex("[^0-9.-]");
            Regex objTwoDotPattern = new Regex("[0-9]*[.][0-9]*[.][0-9]*");
            Regex objTwoMinusPattern = new Regex("[0-9]*[-][0-9]*[-][0-9]*");
            String strValidRealPattern = "^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$";
            String strValidIntegerPattern = "^([-]|[0-9])[0-9]*$";
            Regex objNumberPattern = new Regex("(" + strValidRealPattern + ")|(" + strValidIntegerPattern + ")");
            return !objNotNumberPattern.IsMatch(strNumber) &&
                   !objTwoDotPattern.IsMatch(strNumber) &&
                   !objTwoMinusPattern.IsMatch(strNumber) &&
                   objNumberPattern.IsMatch(strNumber);
        }
        //将datatable转为xml
        public static string DataTableToXml(DataTable vTable)
        {
            if (null == vTable) return string.Empty;
            StringWriter writer = new StringWriter();
            vTable.WriteXml(writer);
            string xmlstr = writer.ToString();
            return xmlstr;
        }

        public static bool IsLegalTime(DateTime dt, string time_intervals)
        {
            //当前时间
            int time_now = dt.Hour * 10000 + dt.Minute * 100 + dt.Second;
            //查看各个时间区间
            string[] time_interval = time_intervals.Split(';');
            foreach (string time in time_interval)
            {
                //空数据直接跳过
                if (string.IsNullOrWhiteSpace(time))
                {
                    continue;
                }
                //一段时间格式：六个数字-六个数字
                if (!Regex.IsMatch(time, "^[0-9]{6}-[0-9]{6}$"))
                {
                    Console.WriteLine("{0}： 错误的时间数据", time);
                }
                string timea = time.Substring(0, 6);
                string timeb = time.Substring(7, 6);
                int time_a, time_b;
                //尝试转化为整数
                if (!int.TryParse(timea, out time_a))
                {
                    Console.WriteLine("{0}： 转化为整数失败", timea);
                }
                if (!int.TryParse(timeb, out time_b))
                {
                    Console.WriteLine("{0}： 转化为整数失败", timeb);
                }
                //如果当前时间不小于初始时间，不大于结束时间，返回true
                if (time_a <= time_now && time_now <= time_b)
                {
                    return true;
                }
            }
            //不在任何一个区间范围内，返回false
            return false;
        }
 
        /// <summary>
        /// 通过辅助资料ID获取编码
        /// </summary>
        /// <param name="FZZL"></param>
        /// <returns></returns>
        public static string GetFZZL(string FZZL, Context context)
        {
            string number = String.Empty;
            string sql = "/*dialect*/ SELECT S3.FNUMBER " +
                       " FROM T_BAS_ASSISTANTDATA S1 " +
                        " INNER JOIN T_BAS_ASSISTANTDATA_L S2 ON S2.FID = S1.FID AND S2.FLOCALEID = 2052" +
                       " INNER JOIN T_BAS_ASSISTANTDATAENTRY S3 ON S3.FID = S1.FID " +
                       "  INNER JOIN T_BAS_ASSISTANTDATAENTRY_L S4 ON S4.FENTRYID = S3.FENTRYID AND S4.FLOCALEID = 2052 " +
                      $"  WHERE S3.FENTRYID = '{FZZL}' " +
                      "  ORDER BY S3.FNUMBER";
            DataSet ds = DBServiceHelper.ExecuteDataSet(context, sql);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                number = ds.Tables[0].Rows[0]["FNUMBER"].ToString();
            }
            return number;
        }

        /// <summary>
        /// 生成流水号
        /// </summary>
        /// <param name="orgobj"></param>
        /// <param name="JTobj"></param>
        /// <param name="FormId"></param>
        /// <returns></returns>
        //public static string GenLSH(Context con, DynamicObject orgobj, DynamicObject Macobj, string FormId)
        //{
        //    string LSh = string.Empty;
        //    if (con != null && orgobj != null && Macobj != null && FormId != "")
        //    {
        //        //取机台名
        //        string mac = Convert.ToString(Macobj["Name"]);
        //        string result = System.Text.RegularExpressions.Regex.Replace(mac, @"[^0-9]+", "");
        //        if (result.IsNullOrEmptyOrWhiteSpace())
        //        {
        //            throw new Exception("此机台命名规范有误");
        //        }
        //        result = Convert.ToString(Convert.ToInt32(result));
        //        string org = orgobj["Id"].ToString();
        //        string tname = string.Empty;
        //        switch (FormId)
        //        {
        //            case "SZXY_XYLY":
        //                tname = "SZXY_BGJ_XYLYList";
        //                break;
        //            case "SZXY_XYFH":
        //                tname = "SZXY_BGJ_XYFHList";
        //                break;
        //            case "SZXY_XYLS":
        //                tname = "SZXY_BGJ_XYLSList";
        //                break;
        //            case "SZXY_XYFC":
        //                tname = "SZXY_BGJ_XYFCList";
        //                break;
        //            case "SZXY_XYFQ":
        //                tname = "SZXY_BGJ_XYFQList";
        //                break;
        //            case "SZXY_XYTF":
        //                tname = "SZXY_BGJ_XYTFList";
        //                break;
        //            case "SZXY_JLD":
        //                tname = "SZXY_BGJ_XYJLList";
        //                break;
        //            case "SZXY_SFD":
        //                tname = "SZXY_BGJ_XYSFList";
        //                break;
        //        }
        //        // 获取主键值
        //        string resname = tname + org + result;
        //        long ids = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(con, resname, 1).FirstOrDefault();
        //        LSh = String.Format("{0:D3}", ids);
        //        if(FormId.Equals("SZXY_XYFQ")) LSh = String.Format("{0:D4}", ids);

        //    }
        //    return LSh;
        //}
        /// <summary>
        /// 生成工序编码
        /// </summary>
        /// <param name="特殊标记"></param>
        /// <param name="组织"></param>
        /// <param name="FormId"></param>
        /// <param name="配方"></param>
        /// <param name="机台"></param>
        /// <param name="生产日期"></param>
        /// <param name="班组"></param>
        /// <param name="流水号"></param>
        /// <param name="辊号"></param>
        /// <param name="层数"></param>
        /// <returns></returns>
        public static string GenNo(DynamicObject SMarkobj, DynamicObject orgobj, string FormId, DynamicObject PFobj, DynamicObject JTobj, DateTime Date, DynamicObject Bzobj, string No, string RollNo, DynamicObject Material=null, string Cc = "")
        {
            string value = string.Empty;
            if (Date != DateTime.MinValue && SMarkobj != null && orgobj != null && FormId != "" && PFobj != null && JTobj != null && Convert.ToString(Date) != "" && Bzobj != null && No != "" && RollNo != "")
            {
                string formulaName = Convert.ToString(PFobj["FDataValue"]);
                string MacName = Convert.ToString(JTobj["Name"]);
                MacName = System.Text.RegularExpressions.Regex.Replace(MacName, @"[^0-9]+", "");
          
                if (MacName.IsNullOrEmptyOrWhiteSpace())
                {
                    MacName = "0";
                }
                if (formulaName.IsNullOrEmptyOrWhiteSpace())
                {
                    formulaName = "0";
                }
                string SpecialMark = Convert.ToString(SMarkobj["FNumber"]);

                string Team = Convert.ToString(Bzobj["Name"]);

                string SMark = string.Empty;
                if (SpecialMark.EqualsIgnoreCase("02"))
                {
                    SMark = "LG-";
                }
                else if (SpecialMark.EqualsIgnoreCase("03"))
                {
                    SMark = "AT-";
                }
                string org = string.Empty;
                if (Convert.ToString(orgobj["Name"]).Contains("深圳"))
                {
                    org = "S";
                }
                else if (Convert.ToString(orgobj["Name"]).Contains("江苏"))
                {
                    org = "J";
                }
                string GX = string.Empty;
                switch (FormId)
                {
                    case "SZXY_XYLY":
                        GX = "C";
                        break;
                    case "SZXY_XYFH":
                        GX = "M";
                        break;
                    case "SZXY_XYLS":
                        GX = "H";
                        break;
                    case "SZXY_XYFC":
                        GX = "H";
                        break;
                }

                string DateNo = Date.ToString("yyMMdd");
                value = $"{SMark}{org}{GX}{formulaName}{MacName}{DateNo}{Team}{No}{RollNo.Trim()}";
                if (Cc != "")
                {
                    value = $"{SMark}{org}{GX}{formulaName}{MacName}{DateNo}{Team}{No}{RollNo.Trim()}{Cc.Trim()}";
                }

            }

            return value;
        }

      

        /// <summary>
        /// 生成辊号
        /// </summary>
        /// <param name="Date"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GenGH(DateTime Date, Context context, string FormId,string OrgId)
        {
            long tick = DateTime.Now.Ticks;
            Random ran = new Random((int)(tick & 0xffffffffL) | (int)(tick >> 32));
            string str = ran.Next().ToString();
            string Rollstr = str.Remove(0, str.Length - 3);
            string TSql = string.Empty;
              
            switch (FormId)
            {
                case "SZXY_XYLY":

                    TSql = $"/*dialect*/select  1 from  SZXY_t_XYLYEntry T2 left join SZXY_t_XYLY T1 on T2.Fid=T1.Fid where T2.F_SZXY_RollNo={Rollstr}" +
                        $" and  CONVERT(varchar(100)  ,T1.FDATE, 12) =CONVERT(varchar(100)  ,'{Date}', 12) ";
                    break;
                case "SZXY_XYFH":
                    TSql = "SZXY_BGJ_XYFHList";
                    break;
                case "SZXY_XYLS":
                    TSql = "SZXY_BGJ_XYLSList";
                    break;
 
            }

            DataSet ds = Utils.CBData(TSql, context);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                Rollstr= CheckGHISCurrent(Rollstr, FormId);
            }
            return Rollstr;
        }

        private static string CheckGHISCurrent(string rollstr, string formId)
        {
            return "";
        }



        /// <summary>
        /// 生成字母数字随机
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string GenRandrom(int num)
        {

            string chars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

            Random randrom = new Random((int)DateTime.Now.Ticks);

            string str = "";
            for (int i = 0; i < num; i++)
            {
                str += chars[randrom.Next(chars.Length)];
            }

            return str;

        }


        /// <summary>
        /// 检查时间是否在一个区间
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool CheckDateTimeISHas(string start, string end, DateTime dt)
        {

            if (start == end)
            {
                return true;
            }
            else
            {
                int iStart = int.Parse(start.Replace("", ""));
                int iEnd = int.Parse(end.Replace(":", ""));
                int iSet = int.Parse(dt.ToString("HHmm"));

                //开始时间大于结束时间，说明跨天了，这时给定的日期只需要大于开始，或小于结束即可
                if (iStart > iEnd)
                {
                    return (iSet >= iStart || iSet <= iEnd) ? true : false;
                }
                else
                {
                    return (iSet >= iStart && iSet <= iEnd) ? true : false;
                }
            }
        }
        

        /// <summary>
        /// 自定义Distinct扩展方法
        /// </summary>
        /// <typeparam name="T">要去重的对象类</typeparam>
        /// <typeparam name="C">自定义去重的字段类型</typeparam>
        /// <param name="source">要去重的对象</param>
        /// <param name="getfield">获取自定义去重字段的委托</param>
        /// <returns></returns>
        public static IEnumerable<T> MyDistinct<T, C>(this IEnumerable<T> source, Func<T, C> getfield)
        {
            return source.Distinct(new Compare<T, C>(getfield));
        }


        public class Compare<T, C> : IEqualityComparer<T>
        {
            private Func<T, C> _getField;
            public Compare(Func<T, C> getfield)
            {
                this._getField = getfield;
            }
            public bool Equals(T x, T y)
            {
                return EqualityComparer<C>.Default.Equals(_getField(x), _getField(y));
            }
            public int GetHashCode(T obj)
            {
                return EqualityComparer<C>.Default.GetHashCode(this._getField(obj));
            }
        }
 
        public static void ShowPrintView(IBillView view,string FID="", string Label="", string report="", string PrintAddress="", string PrintQty = "", string ConnString = "", string QuerySQL = "", string FormId = "", string BarItemKey = "") 
        {
            DynamicFormShowParameter showParam = new DynamicFormShowParameter();
            showParam.FormId = "SZXY_PrintGridSelectHide";//dayin
            showParam.OpenStyle.ShowType = ShowType.Default;
            showParam.HOffset = 0;
            showParam.VOffset = 0;
            showParam.Height = 0;
            showParam.Width = 0;
            //showParam.ShowMaxButton = false;
            //showParam.HiddenCloseButton = true;
            showParam.CustomParams.Add("FID", FID);
            showParam.CustomParams.Add("Label", Label);
            showParam.CustomParams.Add("report", report);
            showParam.CustomParams.Add("PrintAddress", PrintAddress);
            showParam.CustomParams.Add("PrintQty", PrintQty);
            showParam.CustomParams.Add("ConnString", ConnString);
            showParam.CustomParams.Add("QuerySQL", QuerySQL);
            showParam.CustomParams.Add("BillIdentifi",  FormId);
            showParam.CustomParams.Add("MnueIdentifi",  BarItemKey);
            view.ShowForm(showParam);
        }



        static string encryptKey = "KDSZ";//字符串加密密钥(注意：密钥只能是4位)
        public static string Encrypt(string str)
        {//加密字符串

            try
            {
                byte[] key = Encoding.Unicode.GetBytes(encryptKey);//密钥
                byte[] data = Encoding.Unicode.GetBytes(str);//待加密字符串

                DESCryptoServiceProvider descsp = new DESCryptoServiceProvider();//加密、解密对象
                MemoryStream MStream = new MemoryStream();//内存流对象

                //用内存流实例化加密流对象
                CryptoStream CStream = new CryptoStream(MStream, descsp.CreateEncryptor(key, key), CryptoStreamMode.Write);
                CStream.Write(data, 0, data.Length);//向加密流中写入数据
                CStream.FlushFinalBlock();//将数据压入基础流
                byte[] temp = MStream.ToArray();//从内存流中获取字节序列
                CStream.Close();//关闭加密流
                MStream.Close();//关闭内存流

                return Convert.ToBase64String(temp);//返回加密后的字符串
            }
            catch
            {
                return str;
            }
        }


        /// <summary>
        /// 字符串截取
        /// </summary>
        /// <param name="TxtStr"></param>
        /// <param name="FirstStr"></param>
        /// <param name="SecondStr"></param>
        /// <returns></returns>
        public static  string GetStr(string TxtStr, string FirstStr, string SecondStr)
        {
            if (FirstStr.IndexOf(SecondStr, 0) != -1)
                return "";
            int FirstSite = TxtStr.IndexOf(FirstStr, 0);
            int SecondSite = TxtStr.IndexOf(SecondStr, FirstSite + 1);
            if (FirstSite == -1 || SecondSite == -1)
                return "";
            return TxtStr.Substring(FirstSite + FirstStr.Length, SecondSite - FirstSite - FirstStr.Length);
        }

        /// <summary>
        /// 自动下推并保存
        /// </summary>
        /// <param name="sourceFormId">源单FormId</param>
        /// <param name="targetFormId">目标单FormId</param>
        /// <param name="sourceBillIds">源单内码</param>
        private static void DoPush(Context ctx, string sourceFormId, string targetFormId, List<long> sourceBillIds)
        {
            // 获取源单与目标单的转换规则
            IConvertService convertService = ServiceHelper.GetService<IConvertService>();
            var rules = convertService.GetConvertRules(ctx, sourceFormId, targetFormId);
            if (rules == null || rules.Count == 0)
            {
                throw new KDBusinessException("", string.Format("未找到{0}到{1}之间，启用的转换规则，无法自动下推！", sourceFormId, targetFormId));
            }
            // 取勾选了默认选项的规则
            var rule = rules.FirstOrDefault(t => t.IsDefault);
            // 如果无默认规则，则取第一个
            if (rule == null)
            {
                rule = rules[0];
            }
            // 开始构建下推参数：
            // 待下推的源单数据行
            List<ListSelectedRow> srcSelectedRows = new List<ListSelectedRow>();
            foreach (var billId in sourceBillIds)
            {// 把待下推的源单内码，逐个创建ListSelectedRow对象，添加到集合中
                srcSelectedRows.Add(new ListSelectedRow(billId.ToString(), string.Empty, 0, sourceFormId));
                // 特别说明：上述代码，是整单下推；
                // 如果需要指定待下推的单据体行，请参照下句代码，在ListSelectedRow中，指定EntryEntityKey以及EntryId
                //srcSelectedRows.Add(new ListSelectedRow(billId.ToString(), entityId, 0, sourceFormId) { EntryEntityKey = "FEntity" });
            }

            // 指定目标单单据类型:情况比较复杂，没有合适的案例做参照，示例代码暂略，直接留空，会下推到默认的单据类型
            string targetBillTypeId = string.Empty;
            // 指定目标单据主业务组织：情况更加复杂，需要涉及到业务委托关系，缺少合适案例，示例代码暂略
            // 建议在转换规则中，配置好主业务组织字段的映射关系：运行时，由系统根据映射关系，自动从上游单据取主业务组织，避免由插件指定
            long targetOrgId = 0;
            // 自定义参数字典：把一些自定义参数，传递到转换插件中；转换插件再根据这些参数，进行特定处理
            Dictionary<string, object> custParams = new Dictionary<string, object>();
            // 组装下推参数对象
            PushArgs pushArgs = new PushArgs(rule, srcSelectedRows.ToArray())
            {
                TargetBillTypeId = targetBillTypeId,
                TargetOrgId = targetOrgId,
                CustomParams = custParams
            };
            // 调用下推服务，生成下游单据数据包
            ConvertOperationResult operationResult = convertService.Push(ctx, pushArgs, OperateOption.Create());
            // 开始处理下推结果:
            // 获取下推生成的下游单据数据包
            DynamicObject[] targetBillObjs = (from p in operationResult.TargetDataEntities select p.DataEntity).ToArray();
            if (targetBillObjs.Length == 0)
            {
                // 未下推成功目标单，抛出错误，中断审核
                throw new KDBusinessException("", string.Format("由{0}自动下推{1}，没有成功生成数据包，自动下推失败！", sourceFormId, targetFormId));
            }
            // 对下游单据数据包，进行适当的修订，以避免关键字段为空，自动保存失败
            // 示例代码略
            // 读取目标单据元数据
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();
            var targetBillMeta = metaService.Load(ctx, targetFormId) as FormMetadata;
            // 构建保存操作参数：设置操作选项值，忽略交互提示
            OperateOption saveOption = OperateOption.Create();
            // 忽略全部需要交互性质的提示，直接保存；
            saveOption.SetIgnoreWarning(true);              // 忽略交互提示
            //saveOption.SetInteractionFlag(this.Option.GetInteractionFlag());        // 如果有交互，传入用户选择的交互结果
            //// using Kingdee.BOS.Core.Interaction;
            //saveOption.SetIgnoreInteractionFlag(this.Option.GetIgnoreInteractionFlag());

            //// 如下代码，强制要求忽略交互提示(演示案例不需要，注释掉)
            //saveOption.SetIgnoreWarning(true);
            //// using Kingdee.BOS.Core.Interaction;
            //saveOption.SetIgnoreInteractionFlag(true);IDeleteService

            //IDeleteService deleteService = ServiceHelper.GetService<IDeleteService>();
            //deleteService.Delete();

            // 调用保存服务，自动保存
            ISaveService saveService = ServiceHelper.GetService<ISaveService>();
            var saveResult = saveService.Save(ctx, targetBillMeta.BusinessInfo, targetBillObjs, saveOption, "Save");
            // 判断自动保存结果：只有操作成功，才会继续
            if (CheckOpResult(saveResult, saveOption))
            {
                return;
            }
        }

        /// <summary>
        /// 判断操作结果是否成功，如果不成功，则直接抛错中断进程
        /// </summary>
        /// <param name="opResult">操作结果</param>
        /// <param name="opOption">操作参数</param>
        /// <returns></returns>
        private static bool CheckOpResult(IOperationResult opResult, OperateOption opOption)
        {
            bool isSuccess = false;
            if (opResult.IsSuccess == true)
            {
                // 操作成功
                isSuccess = true;
            }
            else
            {
                if (opResult.InteractionContext != null
                    && opResult.InteractionContext.Option.GetInteractionFlag().Count > 0)
                {// 有交互性提示
                    //// 传出交互提示完整信息对象
                    //OperationResult.InteractionContext = opResult.InteractionContext;
                    //// 传出本次交互的标识，
                    //// 用户在确认继续后，会重新进入操作；
                    //// 将以此标识取本交互是否已经确认过，避免重复交互
                    //this.OperationResult.Sponsor = opResult.Sponsor;
                    //// 抛出交互错误，把交互信息传递给前端
                    //new KDInteractionException(opOption, opResult.Sponsor);
                }
                else
                {
                    // 操作失败，拼接失败原因，然后抛出中断
                    opResult.MergeValidateErrors();
                    if (opResult.OperateResult == null)
                    {// 未知原因导致提交失败
                        throw new KDBusinessException("", "未知原因导致自动提交、审核失败！");
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("自动操作失败：");
                        foreach (var operateResult in opResult.OperateResult)
                        {
                            sb.AppendLine(operateResult.Message);
                        }
                        throw new KDBusinessException("", sb.ToString());
                    }
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        /// <param name=""></param>
        public static void LoadBillOBJByID(Context Context) 
        {
            //  获取ViewService
            IViewService viewService = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
            //获取元数据服务
            IMetaDataService metadataService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();

            //获取单据元数据
            FormMetadata BilMetada = metadataService.Load(Context, "UG_SKU_STOCKLOC") as FormMetadata;// 改  UG_SKU_STOCKLOC BGJ_SKU_STOCKLOC_1

            QueryBuilderParemeter queryParameter = new QueryBuilderParemeter();
            queryParameter.BusinessInfo = BilMetada.BusinessInfo;
            //queryParameter.FilterClauseWihtKey = $"FMATERIALID = {CurrentList[0]} and FUSEORGID={CurrentList[1]}";
            //DynamicObject[] objs = viewService.Load.Context, BilMetada.BusinessInfo.GetDynamicObjectType(), queryParameter);
            //string formid = objs[0][0].ToString();
            //DynamicObjectCollection wlcwEntrty = objs[0]["UG_SKU_STOCKLOC_RELENTRY"] as DynamicObjectCollection;  //
            //    int MaxSeq = wlcwEntrty.Count();

            //    DynamicObject NewRow = new DynamicObject(wlcwEntrty[0].DynamicObjectType);
            //    //NewRow["Seq"] = MaxSeq + 1;
            //    //NewRow["FSTOCKID_Id"] = CurrentList[2].ToString();
            //    //NewRow["FSTOCKLOCID_Id"] = STOCKLOC;
            //    //NewRow["F_ORDER"] = "1";
            //    wlcwEntrty.Add(NewRow);
            //    var saveResult = BusinessDataServiceHelper.Save(Context, BilMetada.BusinessInfo, objs);
        }



        /// <summary>
        /// 流水号生成无机台
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="GX"></param>
        /// <param name="OrgID"></param>
        /// <param name="PudDate"></param>
        /// <returns></returns>
        public static string GetLSH(Context Context, string GX, long OrgID, string PudDate,int Qty= 1)
        {
         
            string LSH = "";

            string TBName = $"{GX}{OrgID}{PudDate}";
            Logger.Debug("序列表------------", TBName);
            string SelIsCurrSql = $"/*dialect*/SELECT * FROM sys.sequences WHERE name = N'{TBName}'";
            DataSet SelSeqDS = DBServiceHelper.ExecuteDataSet(Context, SelIsCurrSql);

            if (SelSeqDS.Tables.Count > 0 && SelSeqDS.Tables[0].Rows.Count > 0)
            {
                string SeqSql = "";
                for (int i = 0; i < Qty; i++)
                {
                      SeqSql += $"/*dialect*/select NEXT VALUE FOR {TBName}; ";
                }
               
                DataSet SelMaxSeqDS = DBServiceHelper.ExecuteDataSet(Context, SeqSql);
                if (SelMaxSeqDS != null && SelMaxSeqDS.Tables.Count > 0 && SelMaxSeqDS.Tables[0].Rows.Count > 0)
                {
                    LSH = Convert.ToString(SelMaxSeqDS.Tables[Qty-1].Rows[0][0]);
                }
            }
            else
            {
                string CreateSql = $"/*dialect*/create sequence  {TBName} INCREMENT BY 1  START WITH 1  NO MAXVALUE NO CYCLE";
                DBServiceHelper.Execute(Context, CreateSql);

                string SeqSql = "";
                for (int i = 0; i < Qty; i++)
                {
                    SeqSql += $"/*dialect*/select NEXT VALUE FOR {TBName}; ";
                }

                DataSet SelMaxSeqDS = DBServiceHelper.ExecuteDataSet(Context, SeqSql);
                if (SelMaxSeqDS != null && SelMaxSeqDS.Tables.Count > 0 && SelMaxSeqDS.Tables[0].Rows.Count > 0)
                {
                    LSH = Convert.ToString(SelMaxSeqDS.Tables[Qty - 1].Rows[0][0]);
                }
            }

            LSH = String.Format("{0:D3}", int.Parse(LSH));
            if (GX.EqualsIgnoreCase("FQ"))
            { 
                LSH = String.Format("{0:D4}", int.Parse(LSH)); 
            }
            Logger.Debug($"{GX}LSH=========================", LSH.ToString());
            return LSH;
        }



        public static string GetLSH(Context Context, string GX, long OrgID, string PudDate, DynamicObject Macobj, int Qty = 1)
        { 
            //  取机台名
            string mac = Convert.ToString(Macobj["Name"]);
            string MacNAme = System.Text.RegularExpressions.Regex.Replace(mac, @"[^0-9]+", "");
            if (MacNAme.IsNullOrEmptyOrWhiteSpace())
            {
                throw new Exception("此机台命名规范有误");
            }
            string LSH = "";

            string TBName = $"{GX}{MacNAme}{OrgID}{PudDate}";
       
            string SelIsCurrSql = $"/*dialect*/SELECT * FROM sys.sequences WHERE name = N'{TBName}'";
            DataSet SelSeqDS = DBServiceHelper.ExecuteDataSet(Context, SelIsCurrSql);

            if (SelSeqDS.Tables.Count > 0 && SelSeqDS.Tables[0].Rows.Count > 0)
            {
                string SeqSql = "";
                for (int i = 0; i < Qty; i++)
                {
                    SeqSql += $"/*dialect*/select NEXT VALUE FOR {TBName}; ";
                }

                DataSet SelMaxSeqDS = DBServiceHelper.ExecuteDataSet(Context, SeqSql);
                if (SelMaxSeqDS != null && SelMaxSeqDS.Tables.Count > 0 && SelMaxSeqDS.Tables[0].Rows.Count > 0)
                {
                    LSH = Convert.ToString(SelMaxSeqDS.Tables[Qty - 1].Rows[0][0]);
                }
            }
            else
            {
                string CreateSql = $"/*dialect*/create sequence  {TBName} INCREMENT BY 1  START WITH 1  NO MAXVALUE NO CYCLE";
                DBServiceHelper.Execute(Context, CreateSql);

                string SeqSql = "";
                for (int i = 0; i < Qty; i++)
                {
                    SeqSql += $"/*dialect*/select NEXT VALUE FOR {TBName}; ";
                }

                DataSet SelMaxSeqDS = DBServiceHelper.ExecuteDataSet(Context, SeqSql);
                if (SelMaxSeqDS != null && SelMaxSeqDS.Tables.Count > 0 && SelMaxSeqDS.Tables[0].Rows.Count > 0)
                {
                    LSH = Convert.ToString(SelMaxSeqDS.Tables[Qty - 1].Rows[0][0]);
                }
            }
            LSH = String.Format("{0:D3}", int.Parse(LSH));

            if (GX.EqualsIgnoreCase("FQ")) LSH = String.Format("{0:D4}", Convert.ToInt32((LSH)));
            Logger.Debug($"{GX}LSH=========================", LSH.ToString());
            return LSH;
        }



        /// 通用物料内码加固函数
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Field"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public static string GetRootMatId(string currentmatId, string orgId, Context context)
        {
            string matId = "";

            string SQL = "/*dialect*/select t2.FMATERIALID  from T_BD_MATERIAL t1 join T_BD_MATERIAL t2 on t1.FMASTERID=t2.FMASTERID where t1.FMATERIALID=" + currentmatId + " and t2.FUSEORGID=" + orgId + "";

            DataSet matData = DBServiceHelper.ExecuteDataSet(context, SQL);

            if (matData != null && matData.Tables.Count > 0 && matData.Tables[0].Rows.Count > 0) matId = Convert.ToString(matData.Tables[0].Rows[0][0]);

            return matId;
        }


        /// <summary>
        /// 获取mac地址
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddress()
        {
            try
            {
                //获取网卡硬件地址 
                string mac = "";
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        mac = mo["MacAddress"].ToString();
                        break;
                    }
                }
                moc = null;
                mc = null;
                return mac;
            }
            catch
            {
                return "unknow";
            }
            finally
            {
            }
           
        }
        /// <summary>
        /// 通用打印
        /// </summary>
        public static void TYPrint(IBillView View, Context Context, long orgid, string Fid = "")
        {
            #region
            string MacInfo = Utils.GetMacAddress();
            Logger.Debug("当前MAC地址", MacInfo);
            if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
            {
                //指定MAC打印
                string SQL12 = "/*dialect*/select FID,F_SZXY_REPORT,F_SZXY_PRINTMAC,F_SZXY_PRINTQTY,F_SZXY_LPRINT,F_SZXY_CONNSTRING,F_SZXY_QUERYSQL,F_SZXY_ListSQL,F_SZXY_CustID ,F_SZXY_Model '产品型号',F_SZXY_Remark 'MAC',F_SZXY_CHECKBOX 'CKB'   from SZXY_t_BillTemplatePrint " +
                             "where  F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and FUSEORGID='" + orgid + "' and F_SZXY_TYPESELECT='1'  and FDOCUMENTSTATUS='C' and F_SZXY_Remark='" + MacInfo + "' ";
                DataSet ds12 = DBServiceHelper.ExecuteDataSet(Context, SQL12);
                if (ds12 != null && ds12.Tables.Count > 0 && ds12.Tables[0].Rows.Count > 0)
                {
                    int V = 0;//0:打印，1：预览打印
                    if (!Convert.ToString(ds12.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        V = Convert.ToInt32(ds12.Tables[0].Rows[0]["CKB"]);
                    }
                    XYCast.Print(ds12, V, Context, View, MacInfo, Convert.ToString(View.Model.DataObject[0]));
                }
                else
                {
                    //不指定MAC打印
                    string SQL = "/*dialect*/select FID,F_SZXY_REPORT,F_SZXY_PRINTMAC,F_SZXY_PRINTQTY,F_SZXY_LPRINT,F_SZXY_CONNSTRING,F_SZXY_QUERYSQL,F_SZXY_ListSQL,F_SZXY_CustID ,F_SZXY_Model '产品型号',F_SZXY_CHECKBOX 'CKB'  " +
                        "from SZXY_t_BillTemplatePrint where  F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and FUSEORGID='" + orgid + "' and F_SZXY_TYPESELECT='1' and FDOCUMENTSTATUS='C' and F_SZXY_Remark='' ";
                    DataSet ds = DBServiceHelper.ExecuteDataSet(Context, SQL);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        int V = 0;
                        if (!Convert.ToString(ds.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            V = Convert.ToInt32(ds.Tables[0].Rows[0]["CKB"]);
                        }
                        XYCast.Print(ds, V, Context, View, MacInfo, Convert.ToString(View.Model.DataObject[0]));
                    }
                    else
                    {
                        Logger.Debug("根据客户物料没有找到匹配模板，打通用模板", "");

                        DataSet SelNullModelDS = XYPack.GetNullCustPrintModel(Context, View, orgid);
                        if (SelNullModelDS != null && SelNullModelDS.Tables.Count > 0 && SelNullModelDS.Tables[0].Rows.Count > 0)
                        {
                            int V = 0;
                            if (!Convert.ToString(SelNullModelDS.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                            {
                                V = Convert.ToInt32(SelNullModelDS.Tables[0].Rows[0]["CKB"]);
                            }
                            XYCast.Print(SelNullModelDS, V, Context, View, MacInfo, Convert.ToString(View.Model.DataObject[0]));
                        }
                        else
                        {
                            View.ShowMessage("没有找到匹配的模板！");
                        }
                    }
                }
            }
            #endregion
        }



        /// <summary>
        /// 包装打印带客户
        /// </summary>
        /// <param name="View"></param>
        /// <param name="Context"></param>
        /// <param name="orgid"></param>
        /// <param name="客户拼接sql"></param>
        /// <param name="是否指定标签"></param>
        /// <param name="输入的重打标签类型1，箱号2，生产订单号+行号"></param>
        /// <param name="补打标签str"></param>
        public static void TYPrint(IBillView View, Context Context, long orgid,string CustSQL,string ZDModel,string InputType,string F_SZXY_ForLabel)
        {
            #region
            string MacInfo = Utils.GetMacAddress();
            Logger.Debug("当前MAC地址", MacInfo);
            if (!MacInfo.IsNullOrEmptyOrWhiteSpace())
            {

                string SQL12 = "/*dialect*/select FID,F_SZXY_REPORT,F_SZXY_PRINTMAC,F_SZXY_PRINTQTY,F_SZXY_LPRINT,F_SZXY_CONNSTRING,F_SZXY_QUERYSQL,F_SZXY_ListSQL,F_SZXY_CustID ,F_SZXY_Model '产品型号',F_SZXY_Remark 'MAC',F_SZXY_CHECKBOX 'CKB'   from SZXY_t_BillTemplatePrint " +
                             "where  F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and FUSEORGID='" + orgid + "' and F_SZXY_TYPESELECT='1'  and FDOCUMENTSTATUS='C' and F_SZXY_Remark='" + MacInfo + "' " + CustSQL + " " + ZDModel + "";
                DataSet ds12 = DBServiceHelper.ExecuteDataSet(Context, SQL12);

                if (ds12 != null && ds12.Tables.Count > 0 && ds12.Tables[0].Rows.Count > 0)
                {
                    int V = 0;
                    if (!Convert.ToString(ds12.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                    {
                        V = Convert.ToInt32(ds12.Tables[0].Rows[0]["CKB"]);
                    }
                    XYPack.Print(ds12, V, Context, View, F_SZXY_ForLabel, true, InputType);
 
                 
                }
                else
                {
                    string SQL = "/*dialect*/select FID,F_SZXY_REPORT,F_SZXY_PRINTMAC,F_SZXY_PRINTQTY,F_SZXY_LPRINT,F_SZXY_CONNSTRING,F_SZXY_QUERYSQL,F_SZXY_ListSQL,F_SZXY_CustID ,F_SZXY_Model '产品型号',F_SZXY_CHECKBOX 'CKB'  " +
                        "from SZXY_t_BillTemplatePrint where  F_SZXY_BILLIDENTIFI='" + View.BusinessInfo.GetForm().Id + "' and FUSEORGID='" + orgid + "' and F_SZXY_TYPESELECT='1' and FDOCUMENTSTATUS='C' and F_SZXY_Remark='' " + CustSQL + " " + ZDModel + "";
                    DataSet ds = DBServiceHelper.ExecuteDataSet(Context, SQL);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        int V = 0;
                        if (!Convert.ToString(ds.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                        {
                            V = Convert.ToInt32(ds.Tables[0].Rows[0]["CKB"]);
                        }
                        XYPack.Print(ds, V, Context, View, F_SZXY_ForLabel, true, InputType);
                    }
                    else
                    {
                        Logger.Debug("根据客户物料没有找到匹配模板，打通用模板", "");

                        DataSet SelNullModelDS = XYPack.GetNullCustPrintModel(Context, View, orgid);
                        if (SelNullModelDS != null && SelNullModelDS.Tables.Count > 0 && SelNullModelDS.Tables[0].Rows.Count > 0)
                        {
                            int V = 0;
                            if (!Convert.ToString(SelNullModelDS.Tables[0].Rows[0]["CKB"]).IsNullOrEmptyOrWhiteSpace())
                            {
                                V = Convert.ToInt32(SelNullModelDS.Tables[0].Rows[0]["CKB"]);
                            }
                            XYPack.Print(SelNullModelDS, V, Context, View, F_SZXY_ForLabel, true, InputType);
                        }
                        else
                        {
                            View.ShowMessage("没有找到匹配的模板！");
                        }
                    }
                }
            }
            #endregion
        }



        /// <summary>
        /// 检查原料是否在用料清单存在
        /// by星源
        /// </summary>
        /// <param name="view"></param>
        /// <param name="context"></param>
        /// <param name="yLKey"></param>
        /// <param name="moNOKey"></param>
        /// <param name="moLineNOKey"></param>
        /// <param name="row"></param>
        /// <param name="formid"></param>
        public static void CheckRawMaterialIsTrue(IBillView view, Context context, string yLKey, string moNOKey, string moLineNOKey, int row,string formid="")
        {
            if (view.Model.GetValue(yLKey, row) is DynamicObject YLDO)
            {
                string YL = YLDO["Id"].ToString();
                string F_SZXY_PTNoH = "";
                string F_SZXY_POLineNo = "";

                if (formid=="SF")
                {
                      F_SZXY_PTNoH = view.Model.GetValue(moNOKey,0).ToString();
                      F_SZXY_POLineNo = view.Model.GetValue(moLineNOKey,0).ToString();
                }
                else
                {
                      F_SZXY_PTNoH = view.Model.GetValue(moNOKey).ToString();
                      F_SZXY_POLineNo = view.Model.GetValue(moLineNOKey).ToString();
                }
                if (YL != "" && F_SZXY_PTNoH != "" && F_SZXY_POLineNo != "")
                {
                    string Sel = $"/*dialect*/select T1.FBILLNO,T1.FID,T2.FENTRYID,T2.FSEQ from T_PRD_PPBOM T1 " +
                        $" left join T_PRD_PPBOMENTRY T2 on t1.FID=T2.FID " +
                        $" where T1.FMOBILLNO = '{F_SZXY_PTNoH}' and T1.FMOENTRYSEQ = '{F_SZXY_POLineNo}' and T2.FMATERIALID = '{YL}' ";
                    DataSet ds = DBServiceHelper.ExecuteDataSet(context, Sel);
                    if (ds == null || ds.Tables.Count <= 0 || ds.Tables[0].Rows.Count <= 0)
                    {
                        view.ShowErrMessage("此原料在对应生产用料清单上未找到");
                        return;
                    }
                }

            }
        }



        /// </summary>
        /// 通用客户内码加固函数
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Field"></param>
        /// <param name="Condition"></param>
        /// <returns></returns>
        public static string GetRootCustId(string currentmatId, string orgId, Context context)
        {
            string matId = "";

            string SQL = "/*dialect*/select t2.FCUSTID  from T_BD_CUSTOMER t1 join T_BD_CUSTOMER t2 on t1.FMASTERID=t2.FMASTERID where t1.FCUSTID=" + currentmatId + " and t2.FUSEORGID=" + orgId + "";

            DataSet matData = DBServiceHelper.ExecuteDataSet(context, SQL);

            if (matData != null && matData.Tables.Count > 0 && matData.Tables[0].Rows.Count > 0) matId = Convert.ToString(matData.Tables[0].Rows[0][0]);

            return matId;
        }


        /// <summary>
        /// 填充打印记录表
        /// by.星源
        /// </summary>
        /// <param name="view"></param>
        /// <param name="context"></param>
        /// <param name="formid"></param>
        /// <param name="xH"></param>
        public static void GenPrintReCord(IBillView view, Context context, string formid, string xH)
        {

            string InsertPrintRecordSql = $"/*dialect*/select  distinct '{formid}' GX , n.F_SZXY_ORGID1,m.F_SZXY_CTNNO,m.F_SZXY_OPERATOR,n.FDate,m.F_SZXY_PLY,m.F_SZXY_WIDTH,m.F_SZXY,m.F_SZXY_AREA1, " +
                                              $" m.F_SZXY_PUDNO,m.F_SZXY_PUDLINENO,m.F_SZXY_CUSTID " +
                                              $" from SZXY_t_BZDHEntry m  join SZXY_t_BZD  n on m.fid = n.fid " +
                                              $" where m.F_SZXY_CTNNO in ({xH}) " +
                                              $"  group by n.F_SZXY_ORGID1,m.F_SZXY_CTNNO,m.F_SZXY_CUSTID,m.F_SZXY_OPERATOR,n.FDate,m.F_SZXY_PLY,m.F_SZXY_WIDTH,m.F_SZXY,m.F_SZXY_AREA1, " +
                                              $"  m.F_SZXY_PUDNO,m.F_SZXY_PUDLINENO " +

                                              $" union " +

                                              $" select distinct '{formid}'  GX , n.F_SZXY_ORGID1,m.F_SZXY_CTNNO,m.F_SZXY_OPERATOR,n.FDate,m.F_SZXY_PLY,m.F_SZXY_WIDTH,m.F_SZXY,m.F_SZXY_AREA1, " +
                                              $" m.F_SZXY_PUDNO,m.F_SZXY_PUDLINENO,m.F_SZXY_CUSTID " +
                                              $" from SZXY_t_BZDHEntry m  join SZXY_t_BZD  n on m.fid = n.fid " +
                                              $" where m.F_SZXY_PUDNO + CAST(m.F_SZXY_PUDLINENO as varchar(50)) in ({xH}) " +
                                              $" group by n.F_SZXY_ORGID1,m.F_SZXY_CTNNO,m.F_SZXY_CUSTID,m.F_SZXY_OPERATOR,n.FDate,m.F_SZXY_PLY,m.F_SZXY_WIDTH,m.F_SZXY,m.F_SZXY_AREA1, " +
                                              $" m.F_SZXY_PUDNO,m.F_SZXY_PUDLINENO " +

                                              $" union " +

                                              $" select distinct'{formid}'  GX , n.F_SZXY_ORGID1,m.F_SZXY_BARCODEE,m.F_SZXY_OPERATOR,n.FDate,m.F_SZXY_PLY,m.F_SZXY_WIDTH,m.F_SZXY_LEN,m.F_SZXY_AREA, " +
                                              $" m.F_SZXY_PONO,m.F_SZXY_POLINENO,m.F_SZXY_CUSTID " +
                                              $" from  SZXY_t_XYFQEntry m " +
                                              $" left join SZXY_t_XYFQ n on m.fid = n.fid " +
                                              $" where m.F_SZXY_BARCODEE in ({xH}) " +

                                              $" union " +

                                              $" select distinct '{formid}'  GX , n.F_SZXY_ORGID1,m.F_SZXY_BARCODEE,m.F_SZXY_OPERATOR,n.FDate,m.F_SZXY_PLY,m.F_SZXY_WIDTH,m.F_SZXY_LEN,m.F_SZXY_AREA, " +
                                              $" m.F_SZXY_PONO,m.F_SZXY_POLINENO,m.F_SZXY_CUSTID " +
                                              $" from  SZXY_t_XYFQEntry m " +
                                              $" left join SZXY_t_XYFQ n on m.fid = n.fid " +
                                              $" where m.F_SZXY_PONO + CAST(m.F_SZXY_POLINENO as varchar(50)) in ({xH}) ";
            DataSet InsertPrintRecordDS = DBServiceHelper.ExecuteDataSet(context, InsertPrintRecordSql);
            if (InsertPrintRecordDS != null && InsertPrintRecordDS.Tables.Count > 0 && InsertPrintRecordDS.Tables[0].Rows.Count > 0)
            {
                List<string> insertList = new List<string>();
                string Insert = "";
                foreach (DataRow Row in InsertPrintRecordDS.Tables[0].Rows)
                {
                    long FEntryID = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(context, "SZXY_t_PrintRecordEntry", 1).FirstOrDefault();
                    Insert = $"/*dialect*/insert into SZXY_t_PrintRecordEntry " +
                              $"(FEntryID, " +
                              $"F_SZXY_GX,F_SZXY_OrgId,F_SZXY_ReCordCode,F_SZXY_Recorder,F_SZXY_Date," +
                              $"F_SZXY_PLy,F_SZXY_Width,F_SZXY_Len,F_SZXY_Area," +
                              $"F_SZXY_MoNO,F_SZXY_MoLineNo,F_SZXY_CustID " +
                              $")  " +
                              $"values " +
                              $"( " +
                              $" {FEntryID}," +
                              $"'{Convert.ToString(Row[0]) }'," +
                              $"'{Convert.ToString(Row[1])}'," +
                              $"'{Convert.ToString(Row[2])}'," +
                              $"'{Convert.ToString(Row[3])}'," +
                              $" GETDATE()," +
                              $"'{Convert.ToString(Row[5])}'," +
                              $"'{Convert.ToString(Row[6])}'," +
                              $"'{Convert.ToString(Row[7])}'," +
                              $"'{Convert.ToString(Row[8])}'," +
                              $"'{Convert.ToString(Row[9])}'," +
                              $"'{Convert.ToString(Row[10])}'," +
                              $"'{Convert.ToString(Row[11])}'" +
                              $")";

                    insertList.Add(Insert);
                }
                DBServiceHelper.ExecuteBatch(context, insertList);
            }
        }

    }
}
