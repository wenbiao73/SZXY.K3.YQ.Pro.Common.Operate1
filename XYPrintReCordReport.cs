 
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using Kingdee.K3.FIN.App.Core;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.ServiceHelper.ManagementCenter;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("打印记录表")]
    public class XYPrintReCordReport : SysReportBaseService
    {
        string temTableName = "";
        public override void Initialize()
        {
            base.Initialize();
            // 简单账表类型：普通、树形、分页
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            // 标记报表类型
            //this.ReportProperty.ReportType = ReportType.REPORTTYPE_MOVE;
            //this.ReportProperty.IsGroupSummary = true;
            // 报表名称
            this.ReportProperty.ReportName = new LocaleValue("打印记录表", base.Context.UserLocale.LCID);
            // 
            this.IsCreateTempTableByPlugin = true;
            // 
            this.ReportProperty.IsUIDesignerColumns = false;
            // 
            this.ReportProperty.IsGroupSummary = true;
            // 
            this.ReportProperty.SimpleAllCols = false;
            // 单据主键：两行FID相同，则为同一单的两条分录，单据编号可以不重复显示
            this.ReportProperty.PrimaryKeyFieldName = "FIDENTITYID";
            // 
            this.ReportProperty.IsDefaultOnlyDspSumAndDetailData = true;

            // 报表主键字段名：默认为FIDENTITYID，可以修改
            this.ReportProperty.IdentityFieldName = "FIDENTITYID";
            //this.ReportProperty.BillKeyFieldName = "FBILLID";
            //this.ReportProperty.FormIdFieldName = "FFORMID";
        }
        public override string GetTableName()
        {
            var result = base.GetTableName();
            return result;
        }

        /// <summary>
        /// 向报表临时表，插入报表数据
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);

            // 拼接过滤条件 ： filter
            // 略
            temTableName = AppServiceContext.DBService.CreateTemporaryTableName(this.Context);
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;

            string FilterString = filter.FilterParameter.FilterString;
            string SupperId = "";

            string OrgId = "";

            string sWhere = "";
            if (dyFilter["F_SZXY_OrgId"] is DynamicObject OrgDo)
            {
                OrgId = Convert.ToString(OrgDo["Id"]);
                sWhere += $" and t0.F_SZXY_ORGID={OrgId} ";
            }
            if (dyFilter["F_SZXY_GX"] is string GX)
            {
                 
                sWhere += $" and t0.F_SZXY_GX={GX} ";
            }

            if (dyFilter["F_SZXY_SDate"] is DateTime Sdate && dyFilter["F_SZXY_EDate"] is DateTime Edate)
            {
                if (Sdate != DateTime.MinValue && Sdate != null && Edate != DateTime.MinValue && Edate != null)
                {
                    string sdate = dyFilter["F_SZXY_SDate"].ToString();
                    string edate = dyFilter["F_SZXY_EDate"].ToString();
                    sWhere += $" and   CONVERT(date,T0.F_SZXY_Date, 23) between CONVERT(date, '{sdate}', 23) and CONVERT(date,  '{edate}', 23) ";

                }
            }


            // 默认排序字段：需要从filter中取用户设置的排序字段
            string seqFld = string.Format(base.KSQL_SEQ, " T0.FEntryId ");


            string sql2 = string.Format(@" /*dialect*/SELECT CASE F_SZXY_GX
		                                WHEN 'SZXY_XYFQ'
			                                THEN '分切'
		                                WHEN 'SZXY_XYBZ'
			                                THEN '包装'
		                                END '工序'
	                                ,F_SZXY_ReCordCode '打印记录号'
	                                ,T1.FNAME '打印人N'
	                                ,T0.F_SZXY_Recorder '打印人'
	                                ,F_SZXY_Date '日期'
	                                ,F_SZXY_PLy '厚度'
	                                ,F_SZXY_Width '宽度'
	                                ,F_SZXY_Len '长度'
	                                ,F_SZXY_Area '面积'
	                                ,F_SZXY_MoNO '生产订单号'
	                                ,F_SZXY_MoLineNo '生产订单行号'
	                                ,F_SZXY_CustID '客户'
	                                ,T2.FNAME '客户N'
	                                ,{0}
                                INTO {1}
                                FROM SZXY_t_PrintRecordEntry T0
                                LEFT JOIN T_HR_EMPINFO_L T1 ON T1.FID = T0.F_SZXY_Recorder
	                                AND T1.FLOCALEID = '2052'
                                LEFT JOIN T_BD_CUSTOMER_L T2 ON t2.FCUSTID = T0.F_SZXY_CustID
                                 ",
                                seqFld, tableName, sWhere);

            DBUtils.ExecuteDynamicObject(this.Context, sql2);

        }
        protected override string GetIdentityFieldIndexSQL(string tableName)
        {
            string result = base.GetIdentityFieldIndexSQL(tableName);
            return result;
        }
        protected override void ExecuteBatch(List<string> listSql)
        {
            base.ExecuteBatch(listSql);
        }
        /// <summary>
        /// 构建出报表列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <remarks>
        /// // 如下代码，演示如何设置同一分组的分组头字段合并
        /// // 需配合Initialize事件，设置分组依据字段(PrimaryKeyFieldName)
        /// ReportHeader header = new ReportHeader();
        /// header.Mergeable = true;
        /// int width = 80;
        /// ListHeader headChild1 = header.AddChild("FBILLNO", new LocaleValue("应付单号"));
        /// headChild1.Width = width;
        /// headChild1.Mergeable = true;
        ///             
        /// ListHeader headChild2 = header.AddChild("FPURMAN", new LocaleValue("采购员"));
        /// headChild2.Width = width;
        /// headChild2.Mergeable = true;
        /// </remarks>
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;

            ReportHeader header = new ReportHeader();

            // 序号
            var FID = header.AddChild("FIDENTITYID", new LocaleValue("序号"));
            FID.ColIndex = 0;

            // 单据编码
            var billNo = header.AddChild("工序", new LocaleValue("工序"));
            billNo.ColIndex = 1;

            // 打印记录号
            var FSTOCKORGID = header.AddChild("打印记录号", new LocaleValue("打印记录号"));
            FSTOCKORGID.ColIndex = 2;

            // 打印人
            var FMaterialCode = header.AddChild("打印人N", new LocaleValue("打印人"));
            FMaterialCode.ColIndex = 3;

            // 日期
            var FMaterialName = header.AddChild("日期", new LocaleValue("日期"), SqlStorageType.SqlDatetime);
            FMaterialName.ColIndex = 4;

            // 厚度
            var FSpecification = header.AddChild("厚度", new LocaleValue("厚度"), SqlStorageType.SqlDecimal);
            FSpecification.ColIndex = 5;

            // 宽度
            var FSTOCKID = header.AddChild("宽度", new LocaleValue("宽度"), SqlStorageType.SqlDecimal);
            FSTOCKID.ColIndex = 6;

            //长度
            var FSTOCKNAME = header.AddChild("长度", new LocaleValue("长度"), SqlStorageType.SqlDecimal);
            FSTOCKNAME.ColIndex = 7;

            // 面积
            var FLOT = header.AddChild("面积", new LocaleValue("面积"),SqlStorageType.SqlDecimal);
            FLOT.ColIndex = 8;


            // 生产订单号
            var FOWNERID = header.AddChild("生产订单号", new LocaleValue("生产订单号"));
            FOWNERID.ColIndex = 9;

            // 生产订单行号
            var FMINDATE = header.AddChild("生产订单行号", new LocaleValue("生产订单行号"), SqlStorageType.SqlDecimal);
            FMINDATE.ColIndex = 10;
 
            // 客户
            var FSTOCKQTY = header.AddChild("客户", new LocaleValue("客户"));

            FSTOCKQTY.ColIndex = 13;

            

            return header;
        }
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            var result = base.GetReportTitles(filter);
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (dyFilter != null)
            {
                if (result == null)
                {
                    result = new ReportTitles();
                }
                //result.AddTitle("F_BGJ_Date", Convert.ToString(dyFilter["F_BGJ_Date"]));
            }
            return result;
        }

        protected override string AnalyzeDspCloumn(IRptParams filter, string tablename)
        {
            string result = base.AnalyzeDspCloumn(filter, tablename);
            return result;
        }
        protected override void AfterCreateTempTable(string tablename)
        {
            base.AfterCreateTempTable(tablename);
        }
        /// <summary>
        /// 设置报表合计列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {

            List<SummaryField> result = new List<SummaryField>();
            //result.Add(new SummaryField("含税金额", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //result.Add(new SummaryField("扣款金额", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //result.Add(new SummaryField("应付金额", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //result.Add(new SummaryField("剩余付款金额", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));

            return result;
        }
        protected override string GetSummaryColumsSQL(List<SummaryField> summaryFields)
        {
            var result = base.GetSummaryColumsSQL(summaryFields);
            return result;
        }
        protected override System.Data.DataTable GetListData(string sSQL)
        {
            var result = base.GetListData(sSQL);
            return result;
        }
        protected override System.Data.DataTable GetReportData(IRptParams filter)
        {
            var result = base.GetReportData(filter);
            return result;
        }
        protected override System.Data.DataTable GetReportData(string tablename, IRptParams filter)
        {
            var result = base.GetReportData(tablename, filter);
            return result;
        }
        public override int GetRowsCount(IRptParams filter)
        {
            var result = base.GetRowsCount(filter);
            return result;
        }
        protected override string BuilderFromWhereSQL(IRptParams filter)
        {
            string result = base.BuilderFromWhereSQL(filter);
            return result;
        }
        protected override string BuilderSelectFieldSQL(IRptParams filter)
        {
            string result = base.BuilderSelectFieldSQL(filter);
            return result;
        }
        protected override string BuilderTempTableOrderBySQL(IRptParams filter)
        {
            string result = base.BuilderTempTableOrderBySQL(filter);
            return result;
        }
        public override void CloseReport()
        {
            base.CloseReport();
            if (temTableName.IsNullOrEmptyOrWhiteSpace())
            {
                return;
            }
            IDBService dBService = Kingdee.BOS.App.ServiceHelper.GetService<Kingdee.BOS.Contracts.IDBService>();
            dBService.DeleteTemporaryTableName(this.Context, new string[] { temTableName });
        }
        protected override string CreateGroupSummaryData(IRptParams filter, string tablename)
        {
            string result = base.CreateGroupSummaryData(filter, tablename);
            return result;
        }
        protected override void CreateTempTable(string sSQL)
        {
            base.CreateTempTable(sSQL);
        }
        public override void DropTempTable()
        {
            base.DropTempTable();
        }
        public override System.Data.DataTable GetList(IRptParams filter)
        {
            var result = base.GetList(filter);
            return result;
        }
        public override List<long> GetOrgIdList(IRptParams filter)
        {
            var result = base.GetOrgIdList(filter);
            return result;
        }
        public override List<Kingdee.BOS.Core.Metadata.TreeNode> GetTreeNodes(IRptParams filter)
        {
            var result = base.GetTreeNodes(filter);
            return result;
        }
    }
}
