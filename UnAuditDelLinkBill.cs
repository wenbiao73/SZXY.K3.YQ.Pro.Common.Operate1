
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.App;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.BusinessEntity.BusinessFlow;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.App.Core.BusinessFlow;
using Kingdee.BOS.Core.BusinessFlow.ServiceArgs;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    /// <summary>
    /// 反审核操作，自动删除下游单据；
    /// </summary>
    /// <remarks>
    /// 案例说明：
    /// 需要在反审核时，自动删除下游单据；
    /// 难点在于如何寻找到全部下游单据，然后逐一删除
    /// 
    /// 需要停用反审核操作的操作校验器：下推过不允许反审核
    /// 如果删除失败，反审核也失败
    /// </remarks>
    [Description("反审核时，自动删除下游单据")]
    public class UnAuditDelLinkBill : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 在读取待操作的数据前触发：要求加载各实体内码字段
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            foreach (var entity in this.BusinessInfo.Entrys)
            {
                if (entity is SubEntryEntity)
                {
                    // 子单据体不能作为关联主实体，忽略掉
                    continue;
                }
                // 添加单据体主键字段
                if (entity is EntryEntity
                    && entity.DynamicProperty != null)
                {
                    e.FieldKeys.Add(string.Format("{0}_{1}", entity.Key, entity.EntryPkFieldName));
                }
            }
        }

        /// <summary>
        /// 操作已经完成，事务未提交时触发：同步删除下游单据数据
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            // 本单主键为字符串类型，略过
            if (this.BusinessInfo.GetForm().PkFieldType == EnumPkFieldType.STRING)
            {
                return;
            }

            // 把单据数据包，展开为按实体Key为标识存储的数据字典
            ExtendedDataEntitySet dataEntitySet = new ExtendedDataEntitySet();
            dataEntitySet.Parse(e.DataEntitys, this.BusinessInfo);

            // 对各单据体进行循环，分别扫描下游单据，逐单删除
            foreach (var entity in this.BusinessInfo.Entrys)
            {
                // 判断此实体，是否需要扫描下游单据
                if (this.IgnoreEntity(entity)) continue;

                // 取实体数据集合
                ExtendedDataEntity[] rows = dataEntitySet.FindByEntityKey(entity.Key);
                if (rows == null)
                {
                    // 容错
                    continue;
                }

                // 取实体的业务流程数据
                HashSet<long> entityIds = new HashSet<long>();
                foreach (var row in rows)
                {
                    long entityId = Convert.ToInt64(row.DataEntity[0]);
                    entityIds.Add(entityId);
                }
                BusinessFlowInstanceCollection bfInstances = this.LoadBFInstance(entity.Key, entityIds);
                if (bfInstances == null || bfInstances.Count == 0)
                {
                    // 无关联的业务流程实例，略过
                    continue;
                }

                // 从业务流程实例中，分析出本单据体的下游单据内码：按目标单据体分好组
                Dictionary<string, HashSet<long>> dctTargetEntityIds = this.GetTargetEntityIds(
                    entity, entityIds, bfInstances);

                // 对各种下游单据进行循环，逐个删除
                foreach (var targetBill in dctTargetEntityIds)
                {
                    IOperationResult deleteResult = this.DeleteTargetBill(targetBill.Key, targetBill.Value);
                    if (CheckOpResult(deleteResult) == false)
                    {
                        // 删除失败，无需继续，退出
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// 判断此实体是否需要忽略，无需扫描下游单据
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool IgnoreEntity(Entity entity)
        {
            if (entity is SubEntryEntity
                || entity is SubHeadEntity)
            {
                // 子单据头、单据体不能作为关联主实体，忽略掉
                return true;
            }

            if (entity.DynamicObjectType == null)
            {
                // 容错
                return true;
            }

            return false;
        }

        /// <summary>
        /// 加载单据体相关的业务流程实例
        /// </summary>
        /// <param name="entityKey">单据体Key</param>
        /// <param name="entityIds">单据体内码</param>
        /// <returns>业务流程实例集合</returns>
        private BusinessFlowInstanceCollection LoadBFInstance(string entityKey, HashSet<long> entityIds)
        {
            LoadInstancesByEntityIdArgs args = new LoadInstancesByEntityIdArgs(
                    this.BusinessInfo.GetForm().Id,
                    entityKey,
                    entityIds.ToArray());
            IBusinessFlowDataService bfDataServer = ServiceHelper.GetService<IBusinessFlowDataService>();
            var bfInstances = bfDataServer.LoadInstancesByEntityId(this.Context, args);
            return bfInstances;
        }

        /// <summary>
        /// 分析业务流程实例，输出全部下游单据
        /// </summary>
        /// <param name="entity">上游单据体</param>
        /// <param name="entityIds">上游单据体内码</param>
        /// <param name="bfInstances">相关的业务流程实例</param>
        /// <returns>Dictioanry(下游单据表格编码, 下游单据体内码集合)</returns>
        private Dictionary<string, HashSet<long>> GetTargetEntityIds(
            Entity entity, HashSet<long> entityIds,
            BusinessFlowInstanceCollection bfInstances)
        {
            Dictionary<string, HashSet<long>> dctTargetEntityIds = new Dictionary<string, HashSet<long>>();

            IBusinessFlowService bfService = ServiceHelper.GetService<IBusinessFlowService>();
            TableDefine srcTableDefine = bfService.LoadTableDefine(
                this.Context, this.BusinessInfo.GetForm().Id, entity.Key);

            // 逐个实例查找本单的下游单据体内码
            foreach (var instance in bfInstances)
            {
                // 首先找到业务流程实例中，本单所在的节点
                List<RouteTreeNode> srcNodes = instance.SerarchTargetFormNodes(srcTableDefine.TableNumber);
                foreach (RouteTreeNode srcNode in srcNodes)
                {
                    if (entityIds.Contains(srcNode.Id.EId))
                    {
                        // 找到了本单所在的节点，按类别输出其下游节点：
                        foreach (RouteTreeNode targetNode in srcNode.ChildNodes)
                        {
                            if (dctTargetEntityIds.Keys.Contains(targetNode.Id.Tbl) == false)
                            {
                                dctTargetEntityIds.Add(targetNode.Id.Tbl, new HashSet<long>());
                            }
                            if (dctTargetEntityIds[targetNode.Id.Tbl].Contains(targetNode.Id.EId) == false)
                            {
                                dctTargetEntityIds[targetNode.Id.Tbl].Add(targetNode.Id.EId);
                            }
                        }
                    }
                }
            }
            return dctTargetEntityIds;
        }

        /// <summary>
        /// 尝试删除下游单据，返回删除结果
        /// </summary>
        /// <param name="tableNumber">下游单据表格编码</param>
        /// <param name="entityIds">下游单据体内码</param>
        /// <returns></returns>
        private IOperationResult DeleteTargetBill(
            string tableNumber, HashSet<long> entityIds)
        {
            IBusinessFlowService bfService = ServiceHelper.GetService<IBusinessFlowService>();
            TableDefine tableDefine = bfService.LoadTableDefine(this.Context, tableNumber);

            // 读取下游单据的元数据
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();
            FormMetadata meta = metaService.Load(
                this.Context, tableDefine.FormId) as FormMetadata;

            // 根据下游单据体的内码，读取单据内码
            HashSet<long> billIds = this.LoadTargetBillIds(meta.BusinessInfo, tableDefine.EntityKey, entityIds);

            object[] pkValues = (from p in billIds select (object)p).ToArray();
            // 调用删除服务，删除单据
            IDeleteService deleteService = ServiceHelper.GetService<IDeleteService>();
            IOperationResult deleteResult = deleteService.Delete(this.Context, meta.BusinessInfo,
                pkValues, this.Option);

            return deleteResult;
        }

        /// <summary>
        /// 根据单据体内码，加载单据内码
        /// </summary>
        /// <param name="targetBusinessInfo"></param>
        /// <param name="entityKey"></param>
        /// <param name="entityIds"></param>
        /// <returns></returns>
        private HashSet<long> LoadTargetBillIds(
            BusinessInfo targetBusinessInfo,
            string entityKey,
            HashSet<long> entityIds)
        {
            // 根据单据体内码，读取取下游单据的单据内码
            HashSet<long> billIds = new HashSet<long>();
            Entity entity = targetBusinessInfo.GetEntity(entityKey);
            if (entity is HeadEntity)
            {
                foreach (var billId in entityIds)
                {
                    billIds.Add(billId);
                }
            }
            else
            {
                string entityPKFieldNameAs = string.Concat(entity.Key, "_", entity.EntryPkFieldName);
                QueryBuilderParemeter queryParem = new QueryBuilderParemeter()
                {
                    FormId = targetBusinessInfo.GetForm().Id,
                    BusinessInfo = targetBusinessInfo,
                };
                queryParem.SelectItems.Add(new SelectorItemInfo(targetBusinessInfo.GetForm().PkFieldName));
                queryParem.SelectItems.Add(new SelectorItemInfo(entityPKFieldNameAs));
                queryParem.ExtJoinTables.Add(
                    new ExtJoinTableDescription()
                    {
                        TableName = "table(fn_StrSplit(@EntryPKValue,',',1))",
                        TableNameAs = "sp",
                        FieldName = "FID",
                        ScourceKey = entityPKFieldNameAs,
                    });
                queryParem.SqlParams.Add(new SqlParam("@EntryPKValue", KDDbType.udt_inttable, entityIds.ToArray()));

                IQueryService queryService = ServiceHelper.GetService<IQueryService>();
                DynamicObjectCollection rows = queryService.GetDynamicObjectCollection(this.Context, queryParem);
                foreach (var row in rows)
                {
                    long billId = Convert.ToInt64(row[0]);
                    if (billIds.Contains(billId) == false)
                    {
                        billIds.Add(billId);
                    }
                }
            }
            return billIds;
        }

        /// <summary>
        /// 判断操作结果是否成功，如果不成功，则直接抛错中断进程
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private bool CheckOpResult(IOperationResult opResult)
        {
            bool isSuccess = false;
            if (opResult.IsSuccess == true)
            {
                // 操作成功
                isSuccess = true;
            }
            else
            {
                // 操作失败，拼接失败原因，然后抛出中断
                opResult.MergeValidateErrors();
                if (opResult.OperateResult == null)
                {
                    throw new KDBusinessException("DeleteTargetBill-001", "未知原因导致自动删除下游单据失败！");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("自动删除下游单据失败，失败原因：");
                    foreach (var operateResult in opResult.OperateResult)
                    {
                        sb.AppendLine(operateResult.Message);
                    }
                    throw new KDBusinessException("DeleteTargetBill-002", sb.ToString());
                }
            }
            return isSuccess;
        }
    }
}
