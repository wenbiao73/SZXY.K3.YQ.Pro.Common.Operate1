using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [HotUpdate]
    [Description("内外控标准操作")]
    public class NKWK : AbstractDynamicFormPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase("F_BGJ_Save"))
            {
                string formID = this.View.BusinessInfo.GetForm().Id;
                if (formID== "SZXY_NKWLXNJYBZ")
                {
                    #region
                    this.Model.ClearNoDataRow();
                   
                    DynamicObject billObj = this.Model.DataObject;
                    if (billObj[GetFieldsKey(formID)["Entry"]] is DynamicObjectCollection entityRows)
                    {//
                       
                      
                        List<string> ListSql = new List<string>();
                        string Sqldel = "";
                        Sqldel = $"Delete From {GetFieldsKey(formID)["Table"] } where F_SZXY_OrgId={Convert.ToString(billObj["F_SZXY_OrgIdH_Id"])}";
                        if (Sqldel != "") DBServiceHelper.Execute(Context, Sqldel);
                        foreach (DynamicObject Row in entityRows.Where(p=>!Convert.ToString(p["F_SZXY_Material"]).IsNullOrEmptyOrWhiteSpace()))
                        {
                            long FEntryID = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(Context, "SZXY_t_NKWLXNJYBEntry", 1).FirstOrDefault();
                            string sql = $"/*Dialect*/INSERT INTO {GetFieldsKey(formID)["Table"] }" +
                            $"(FEntryID,FSeq, F_SZXY_OrgId," +
                            $"F_SZXY_Material," +
                            $"F_SZXY_PlyAvg," +
                            $"F_SZXY_PlyAvgMin," +
                            $"F_SZXY_PlyAvgMax," +
                            $"F_SZXY_Ply," +
                            $"F_SZXY_PlyMax," +
                            $"F_SZXY_PlyMin," +
                            $"F_SZXY_TQLAVG," +
                            $"F_SZXY_TQLAVGMax," +
                            $"F_SZXY_TQLAVGMin," +
                            $"F_SZXY_TQL," +
                            $"F_SZXY_TQLMax," +
                            $"F_SZXY_TQLMin," +
                            $"F_SZXY_MMDAvg," +
                            $"F_SZXY_MMDAvgMax," +
                            $"F_SZXY_MMDAvgMin," +
                            $"F_SZXY_MMDDDD," +
                            $"F_SZXY_MMDDDDMAX," +
                            $"F_SZXY_MMDDDDMin," +
                            $"F_SZXY_KXLAAVG," +
                            $"F_SZXY_KXLAAVGMax," +
                            $"F_SZXY_KXLAAVGMin," +
                            $"F_SZXY_KXL," +
                            $"F_SZXY_KXLMAx," +
                            $"F_SZXY_KXLMin," +
                            $"F_SZXY_LSQDMDAVG," +
                            $"F_SZXY_LSQDMDAVGMax," +
                            $"F_SZXY_LSQDMDAVGMin," +
                            $"F_SZXY_LSQDMD," +
                            $"F_SZXY_LSQDMDMax," +
                            $"F_SZXY_LSQDMDMin," +
                            $"F_SZXY_LSQDTDAVG," +
                            $"F_SZXY_LSQDTDAVGMAx," +
                            $"F_SZXY_LSQDTDAVGMin," +
                            $"F_SZXY_LSQDTD," +
                            $"F_SZXY_LSQDTDMax," +
                            $"F_SZXY_LSQDTDMin," +
                            $"F_SZXY_DLSCLMDAVG," +
                            $"F_SZXY_DLSCLMDAVGMax," +
                            $"F_SZXY_DLSCLMDAVGMin," +
                            $"F_SZXY_DLSCLMD," +
                            $"F_SZXY_DLSCLMDMAx," +
                            $"F_SZXY_DLSCLMDMin," +
                            $"F_SZXY_DLSCLTDAVG," +
                            $"F_SZXY_DLSCLTDAVGMAx," +
                            $"F_SZXY_DLSCLTDAVGMin," +
                            $"F_SZXY_DLSCLTD," +
                            $"F_SZXY_DLSCLTDMAx," +
                            $"F_SZXY_DLSCLTDMin," +
                            $"F_SZXY_CCQDAVG," +
                            $"F_SZXY_CCQDAVGMax," +
                            $"F_SZXY_CCQDAVGMin," +
                            $"F_SZXY_CCQD," +
                            $"F_SZXY_CCQDMax," +
                            $"F_SZXY_CCQDMin," +
                            $"F_SZXY_BLLAVG," +
                            $"F_SZXY_BLLAVGMAx," +
                            $"F_SZXY_BLLAVGMin," +
                            $"F_SZXY_BLL," +
                            $"F_SZXY_BLLMAx," +
                            $"F_SZXY_BLLMin," +
                            $"F_SZXY_90MDAVG," +
                            $"F_SZXY_90MDAVGMax," +
                            $"F_SZXY_90MDAVGMin," +
                            $"F_SZXY_90MD," +
                            $"F_SZXY_90MDMax," +
                            $"F_SZXY_90MDMin," +
                            $"F_SZXY_Decimal," +
                            $"F_SZXY_Decimal1," +
                            $"F_SZXY_Decimal2," +
                            $"F_SZXY_Decimal3," +
                            $"F_SZXY_Decimal4," +
                            $"F_SZXY_Decimal5," +
                            $"F_SZXY_Decimal6," +
                            $"F_SZXY_Decimal7," +
                            $"F_SZXY_Decimal8," +
                            $"F_SZXY_Decimal9," +
                            $"F_SZXY_Decimal10," +
                            $"F_SZXY_Decimal11," +
                            $"F_SZXY_Decimal12," +
                            $"F_SZXY_Decimal13," +
                            $"F_SZXY_Decimal14," +
                            $"F_SZXY_Decimal15," +
                            $"F_SZXY_Decimal16," +
                            $"F_SZXY_Decimal17," +
                            $"F_SZXY_Decimal18," +
                            $"F_SZXY_Decimal19," +
                            $"F_SZXY_Decimal20," +
                            $"F_SZXY_Decimal21," +
                            $"F_SZXY_Decimal22," +
                            $"F_SZXY_Decimal23," +
                            $"F_SZXY_Decimal24," +
                            $"F_SZXY_Decimal25," +
                            $"F_SZXY_Decimal26," +
                            $"F_SZXY_Decimal27," +
                            $"F_SZXY_Decimal28," +
                            $"F_SZXY_Decimal29," +
                            $"F_SZXY_Decimal30," +
                            $"F_SZXY_Decimal31," +
                            $"F_SZXY_Decimal32," +
                            $"F_SZXY_Decimal33," +
                            $"F_SZXY_Decimal34," +
                            $"F_SZXY_Decimal35," +
                            $"F_SZXY_Decimal36," +
                            $"F_SZXY_Decimal37," +
                            $"F_SZXY_Decimal38," +
                            $"F_SZXY_Decimal39," +
                            $"F_SZXY_Decimal40," +
                            $"F_SZXY_Decimal41," +
                            $"F_SZXY_Decimal42," +
                            $"F_SZXY_Decimal43," +
                            $"F_SZXY_Decimal44," +
                            $"F_SZXY_Decimal45," +
                            $"F_SZXY_Decimal46," +
                            $"F_SZXY_Decimal47," +
                            $"F_SZXY_Decimal48," +
                            $"F_SZXY_Decimal49," +
                            $"F_SZXY_Decimal50," +
                            $"F_SZXY_Decimal51," +
                            $"F_SZXY_Decimal52," +
                            $"F_SZXY_Decimal53," +
                            $"F_SZXY_Decimal54," +
                            $"F_SZXY_Decimal55," +
                            $"F_SZXY_Decimal56," +
                            $"F_SZXY_Decimal57," +
                            $"F_SZXY_Decimal58," +
                            $"F_SZXY_Decimal59," +
                            $"F_SZXY_Decimal60," +
                            $"F_SZXY_Decimal61," +
                            $"F_SZXY_Decimal62," +
                            $"F_SZXY_DECIMAL63," +
                            $"F_SZXY_DECIMAL64," +
                            $"F_SZXY_DECIMAL65," +
                            $"F_SZXY_DECIMAL66," +
                            $"F_SZXY_DECIMAL67," +
                            $"F_SZXY_DECIMAL68," +
                            $"F_SZXY_DECIMAL69," +
                            $"F_SZXY_DECIMAL70," +
                            $"F_SZXY_DECIMAL71," +
                            $"F_SZXY_DECIMAL72," +
                            $"F_SZXY_DECIMAL73," +
                            $"F_SZXY_DECIMAL74," +
                            $"F_SZXY_DECIMAL75," +
                            $"F_SZXY_DECIMAL76," +
                            $"F_SZXY_DECIMAL77," +
                            $"F_SZXY_DECIMAL78," +
                            $"F_SZXY_DECIMAL79," +
                            $"F_SZXY_DECIMAL80," +
                            $"F_SZXY_DECIMAL81," +
                            $"F_SZXY_DECIMAL82," +
                            $"F_SZXY_DECIMAL83," +
                            $"F_SZXY_DECIMAL84," +
                            $"F_SZXY_DECIMAL85," +
                            $"F_SZXY_DECIMAL86)" +


                           $"  VALUES " +
                            $"({FEntryID},{Convert.ToInt32(Row["Seq"])},{Convert.ToString(Row["F_SZXY_OrgId_Id"])}," +
                            $"{Convert.ToString(Row["F_SZXY_Material_Id"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyAvg"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyAvgMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyAvgMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Ply"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLAVGMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQL"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDAvg"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDAvgMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDAvgMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDDDD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDDDDMAX"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDDDDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLAAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLAAVGMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLAAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXL"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDAVGMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDAVGMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDAVGMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDAVGMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDAVGMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLAVGMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLL"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDAVG"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDAVGMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDAVGMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDMin"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal1"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal2"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal3"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal4"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal5"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal6"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal7"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal8"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal9"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal10"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal11"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal12"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal13"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal14"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal15"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal16"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal17"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal18"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal19"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal20"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal21"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal22"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal23"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal24"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal25"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal26"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal27"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal28"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal29"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal30"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal31"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal32"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal33"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal34"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal35"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal36"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal37"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal38"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal39"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal40"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal41"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal42"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal43"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal44"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal45"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal46"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal47"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal48"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal49"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal50"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal51"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal52"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal53"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal54"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal55"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal56"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal57"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal58"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal59"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal60"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal61"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal62"])},"+

                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL63"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL64"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL65"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL66"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL67"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL68"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL69"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL70"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL71"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL72"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL73"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL74"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL75"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL76"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL77"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL78"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL79"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL80"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL81"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL82"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL83"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL84"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL85"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL86"])})";

                            if (sql != "") DBServiceHelper.Execute(Context, sql);
                        }
                      
                       // if (ListSql != null && ListSql.Count > 0) DBServiceHelper.ExecuteBatch(Context, ListSql);
                    }
                    this.View.UpdateView("F_SZXY_NKWLXNJYBEntity");
# endregion
                }
                else if(formID == "SZXY_WKJYBZ")
                {
                    #region
                    this.Model.ClearNoDataRow();
              
                    DynamicObject billObj = this.Model.DataObject;
                    if (billObj[GetFieldsKey(formID)["Entry"]] is DynamicObjectCollection entityRows)
                    {//
                        string Sqldel = ""; Sqldel = $"Delete From {GetFieldsKey(formID)["Table"] } where F_SZXY_OrgId={Convert.ToString(billObj["F_SZXY_OrgIdH_Id"])}";
                        if (Sqldel != "") DBServiceHelper.Execute(Context, Sqldel);
                        List<string> ListSql = new List<string>();
                        foreach (DynamicObject Row in entityRows.Where(p => !Convert.ToString(p["F_SZXY_Material"]).IsNullOrEmptyOrWhiteSpace()))
                        {
                      
                            long FEntryID = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(Context, "SZXY_t_WKJYBZEntry", 1).FirstOrDefault();
                            string sql = $"/*Dialect*/INSERT INTO {GetFieldsKey(formID)["Table"] }" +
                            $"(FEntryID,FSeq,F_SZXY_CUST, F_SZXY_OrgId," +
                            $"F_SZXY_Material," +
                            $"F_SZXY_Ply," +
                            $"F_SZXY_PlyMax," +
                            $"F_SZXY_PlyMin," +
                            $"F_SZXY_TQL," +
                            $"F_SZXY_TQLMax," +
                            $"F_SZXY_TQLMin," +
                
                            $"F_SZXY_MMDDDD," +
                            $"F_SZXY_MMDDDDMAX," +
                            $"F_SZXY_MMDDDDMin," +
                     
                            $"F_SZXY_KXL," +
                            $"F_SZXY_KXLMAx," +
                            $"F_SZXY_KXLMin," +
               
                            $"F_SZXY_LSQDMD," +
                            $"F_SZXY_LSQDMDMax," +
                            $"F_SZXY_LSQDMDMin," +
                   
                            $"F_SZXY_LSQDTD," +
                            $"F_SZXY_LSQDTDMax," +
                            $"F_SZXY_LSQDTDMin," +
              
                            $"F_SZXY_DLSCLMD," +
                            $"F_SZXY_DLSCLMDMAx," +
                            $"F_SZXY_DLSCLMDMin," +
                
                            $"F_SZXY_DLSCLTD," +
                            $"F_SZXY_DLSCLTDMAx," +
                            $"F_SZXY_DLSCLTDMin," +
           
                            $"F_SZXY_CCQD," +
                            $"F_SZXY_CCQDMax," +
                            $"F_SZXY_CCQDMin," +
       
                            $"F_SZXY_BLL," +
                            $"F_SZXY_BLLMAx," +
                            $"F_SZXY_BLLMin," +
                   
                            $"F_SZXY_90MD," +
                            $"F_SZXY_90MDMax," +
                            $"F_SZXY_90MDMin," +
                   
                            $"F_SZXY_Decimal3," +
                            $"F_SZXY_Decimal4," +
                            $"F_SZXY_Decimal5," +
                            $"F_SZXY_Decimal6," +
                            $"F_SZXY_Decimal7," +
                            $"F_SZXY_Decimal8," +
                     
                            $"F_SZXY_Decimal12," +
                            $"F_SZXY_Decimal13," +
                            $"F_SZXY_Decimal14," +
                          
                            $"F_SZXY_Decimal18," +
                            $"F_SZXY_Decimal19," +
                            $"F_SZXY_Decimal20," +
                  
                            $"F_SZXY_Decimal24," +
                            $"F_SZXY_Decimal25," +
                            $"F_SZXY_Decimal26," +
                 
                            $"F_SZXY_Decimal30," +
                            $"F_SZXY_Decimal31," +
                            $"F_SZXY_Decimal32," +
                     
                            $"F_SZXY_Decimal36," +
                            $"F_SZXY_Decimal37," +
                            $"F_SZXY_Decimal38," +
                            
                            $"F_SZXY_Decimal42," +
                            $"F_SZXY_Decimal43," +
                            $"F_SZXY_Decimal44," +
                             
                            $"F_SZXY_Decimal48," +
                            $"F_SZXY_Decimal49," +
                            $"F_SZXY_Decimal50," +
                          
                            $"F_SZXY_Decimal54," +
                            $"F_SZXY_Decimal55," +
                            $"F_SZXY_Decimal56," +
                            $"F_SZXY_Decimal57," +
                            $"F_SZXY_Decimal58," +
                            $"F_SZXY_Decimal59," +
                            $"F_SZXY_Decimal60," +
                            $"F_SZXY_Decimal61," +
                            $"F_SZXY_Decimal62," +

                            //
                            $"F_SZXY_Decimal," +
                            $"F_SZXY_Decimal1," +
                            $"F_SZXY_Decimal2," +
                            $"F_SZXY_Decimal9," +
                            $"F_SZXY_Decimal10," +
                            $"F_SZXY_Decimal11," +
                            $"F_SZXY_Decimal15," +
                            $"F_SZXY_Decimal16," +
                            $"F_SZXY_Decimal17," +
                            $"F_SZXY_Decimal21," +
                            $"F_SZXY_Decimal22," +
                            $"F_SZXY_Decimal23)" +

                           $"  VALUES " +
                            $"({FEntryID},{Convert.ToInt32(Row["Seq"])},{ Convert.ToString(Row["F_SZXY_CUST_Id"])},{Convert.ToString(Row["F_SZXY_OrgId_Id"])}," +
                            $"{Convert.ToString(Row["F_SZXY_Material_Id"])}," +
                      
                            $"{Convert.ToDecimal(Row["F_SZXY_Ply"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_PlyMin"])}," +
                   
                            $"{Convert.ToDecimal(Row["F_SZXY_TQL"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_TQLMin"])}," +
                 
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDDDD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDDDDMAX"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_MMDDDDMin"])}," +
               
                            $"{Convert.ToDecimal(Row["F_SZXY_KXL"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_KXLMin"])}," +
  
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDMDMin"])}," +
                
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_LSQDTDMin"])}," +
               
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLMDMin"])}," +
               
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DLSCLTDMin"])}," +
              
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_CCQDMin"])}," +
                  
                            $"{Convert.ToDecimal(Row["F_SZXY_BLL"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLMAx"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_BLLMin"])}," +
      
                            $"{Convert.ToDecimal(Row["F_SZXY_90MD"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDMax"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_90MDMin"])}," +
                        
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal3"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal4"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal5"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal6"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal7"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal8"])}," +
                        
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal12"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal13"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal14"])}," +
                
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal18"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal19"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal20"])}," +
                           
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal24"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal25"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal26"])}," +
                           
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal30"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal31"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal32"])}," +
                
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal36"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal37"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal38"])}," +
                            
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal42"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal43"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal44"])}," +
                            
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal48"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal49"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal50"])}," +
                            
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal54"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal55"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal56"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal57"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal58"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal59"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal60"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal61"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal62"])},"+

                            //
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal1"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal2"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal9"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal10"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal11"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal15"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal16"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal17"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal21"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal22"])}," +
                             $"{Convert.ToDecimal(Row["F_SZXY_Decimal23"])})";

                            if (sql != "") DBServiceHelper.Execute(Context, sql);
                        }
                   
                   
                    }
                    this.View.UpdateView("F_SZXY_WKJYBZEntity");
                    #endregion
                }

                else if (formID == "SZXY_LYMM")
                {
                    #region
                    this.Model.ClearNoDataRow();

                    DynamicObject billObj = this.Model.DataObject;
                    if (billObj["SZXY_LYMEntry"] is DynamicObjectCollection entityRows)
                    {//
                        string Sqldel = ""; Sqldel = $"Delete From SZXY_t_LYMEntry";
                        if (Sqldel != "") DBServiceHelper.Execute(Context, Sqldel);
                        List<string> ListSql = new List<string>();
                        foreach (DynamicObject Row in entityRows.Where(p => !Convert.ToString(p["F_SZXY_Decimal"]).IsNullOrEmptyOrWhiteSpace()))
                        {

                            long FEntryID = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(Context, "SZXY_t_LYMEntry", 1).FirstOrDefault();
                            string sql = $"/*Dialect*/INSERT INTO SZXY_t_LYMEntry" +
                            $"(" +
                            $"FEntryID," +
                            $"F_SZXY_Decimal," +
                            $"F_SZXY_Decimal1," +
                            $"F_SZXY_Decimal2," +
                            $"F_SZXY_Decimal3," +
                            $"F_SZXY_Decimal4," +
                            $"F_SZXY_Decimal5," +
                            $"F_SZXY_Decimal6," +
                            $"F_SZXY_Decimal7," +
                            $"F_SZXY_Decimal8," +
                            $"F_SZXY_Decimal9," +
                            $"F_SZXY_Decimal10," +
                            $"F_SZXY_Decimal11," +
                            $"F_SZXY_Decimal12," +
                            $"F_SZXY_Decimal13," +
                            $"F_SZXY_Decimal14," +
                            $"F_SZXY_Decimal15," +
                            $"F_SZXY_Decimal16," +
                            $"F_SZXY_Decimal17," +
                            $"F_SZXY_Decimal18," +
                            $"F_SZXY_Decimal19," +
                            $"F_SZXY_Decimal20," +
                            $"F_SZXY_Decimal21," +
                            $"F_SZXY_Decimal22," +
                            $"F_SZXY_Decimal23," +
                            $"F_SZXY_Decimal24," +
                            $"F_SZXY_Decimal25," +
                            $"F_SZXY_Decimal26," +
                            $"F_SZXY_Decimal27," +
                            $"F_SZXY_Decimal28," +
                            $"F_SZXY_Decimal29," +
                            $"F_SZXY_Decimal30," +
                            $"F_SZXY_Decimal31," +
                            $"F_SZXY_Decimal32," +
                            $"F_SZXY_Decimal33," +
                            $"F_SZXY_Decimal34," +
                            $"F_SZXY_Decimal35," +
                            $"F_SZXY_Decimal36," +
                            $"F_SZXY_Decimal37," +
                            $"F_SZXY_Decimal38," +
                            $"F_SZXY_Decimal39," +
                            $"F_SZXY_Decimal40," +
                            $"F_SZXY_Decimal41," +
                            $"F_SZXY_Decimal42," +
                            $"F_SZXY_Decimal43," +
                            $"F_SZXY_Decimal44," +
                            $"F_SZXY_Decimal45," +
                            $"F_SZXY_Decimal46," +
                            $"F_SZXY_Decimal47," +
                            $"F_SZXY_Decimal48," +
                            $"F_SZXY_Decimal49," +
                            $"F_SZXY_Decimal50," +
                            $"F_SZXY_Decimal51," +
                            $"F_SZXY_Decimal52," +
                            $"F_SZXY_Decimal53," +
                            $"F_SZXY_Decimal54," +
                            $"F_SZXY_Decimal55," +
                            $"F_SZXY_Decimal56," +
                            $"F_SZXY_Decimal57," +
                            $"F_SZXY_Decimal58," +
                            $"F_SZXY_Decimal59," +
                            $"F_SZXY_Decimal60," +
                            $"F_SZXY_Decimal61," +
                            $"F_SZXY_Decimal62," +
                            $"F_SZXY_DECIMAL63," +
                            $"F_SZXY_DECIMAL64," +
                            $"F_SZXY_DECIMAL65," +
                            $"F_SZXY_DECIMAL66," +
                            $"F_SZXY_DECIMAL67," +
                            $"F_SZXY_DECIMAL68," +
                            $"F_SZXY_DECIMAL69," +
                            $"F_SZXY_DECIMAL70," +
                            $"F_SZXY_DECIMAL71," +
                            $"F_SZXY_DECIMAL72," +
                            $"F_SZXY_DECIMAL73 )" +

                           $"  VALUES " +
                            $"({FEntryID}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal1"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal2"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal3"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal4"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal5"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal6"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal7"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal8"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal9"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal10"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal11"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal12"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal13"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal14"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal15"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal16"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal17"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal18"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal19"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal20"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal21"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal22"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal23"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal24"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal25"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal26"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal27"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal28"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal29"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal30"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal31"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal32"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal33"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal34"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal35"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal36"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal37"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal38"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal39"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal40"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal41"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal42"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal43"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal44"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal45"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal46"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal47"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal48"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal49"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal50"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal51"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal52"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal53"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal54"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal55"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal56"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal57"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal58"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal59"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal60"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal61"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_Decimal62"])}," +

                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL63"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL64"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL65"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL66"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL67"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL68"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL69"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL70"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL71"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL72"])}," +
                            $"{Convert.ToDecimal(Row["F_SZXY_DECIMAL73"])})";
                   
                            if (sql != "") DBServiceHelper.Execute(Context, sql);
                        }


                    }
                    this.View.UpdateView("F_SZXY_WKJYBZEntity");
                    #endregion
                }



                this.View.ShowMessage("保存成功");
            }
          

        }
        /// <summary>
        /// 加载数据到动态表单
        /// </summary>SZXY_WKJYBZEntry
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DynamicObject billObj = this.Model.DataObject;
            string formID = this.View.BusinessInfo.GetForm().Id;
            if (formID == "SZXY_NKWLXNJYBZ")
            {
                #region
                string SelectBindSql = $"/*dialect*/select * from SZXY_t_NKWLXNJYBEntry where F_SZXY_OrgId={Convert.ToString(billObj["F_SZXY_OrgIdH_Id"])}";
                DataSet GetData = DBServiceHelper.ExecuteDataSet(this.Context, SelectBindSql);

                if (GetData != null && GetData.Tables.Count > 0 && GetData.Tables[0].Rows.Count > 0)
                {
                    this.Model.ClearNoDataRow();
                    this.Model.DeleteEntryData("F_SZXY_NKWLXNJYBEntity");
                  
                    for (int i = 0; i < GetData.Tables[0].Rows.Count; i++)
                    {
                        this.Model.CreateNewEntryRow("F_SZXY_NKWLXNJYBEntity");
                        if (!Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_OrgId"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_OrgId", Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_OrgId"]), i);
                       if(!Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_Material"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Material", Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_Material"]), i);
                        this.Model.SetValue("F_SZXY_PlyAvg", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyAvg"]), i);
                        this.Model.SetValue("F_SZXY_PlyAvgMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyAvgMin"]), i);
                        this.Model.SetValue("F_SZXY_PlyAvgMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyAvgMax"]), i);
                        this.Model.SetValue("F_SZXY_Ply", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Ply"]), i);
                        this.Model.SetValue("F_SZXY_PlyMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyMax"]), i);
                        this.Model.SetValue("F_SZXY_PlyMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyMin"]), i);
                        this.Model.SetValue("F_SZXY_TQLAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLAVG"]), i);
                        this.Model.SetValue("F_SZXY_TQLAVGMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLAVGMax"]), i);
                        this.Model.SetValue("F_SZXY_TQLAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_TQL", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQL"]), i);
                        this.Model.SetValue("F_SZXY_TQLMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLMax"]), i);
                        this.Model.SetValue("F_SZXY_TQLMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLMin"]), i);
                        this.Model.SetValue("F_SZXY_MMDAvg", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDAvg"]), i);
                        this.Model.SetValue("F_SZXY_MMDAvgMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDAvgMax"]), i);
                        this.Model.SetValue("F_SZXY_MMDAvgMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDAvgMin"]), i);
                        this.Model.SetValue("F_SZXY_MMDDDD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDDDD"]), i);
                        this.Model.SetValue("F_SZXY_MMDDDDMAX", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDDDDMAX"]), i);
                        this.Model.SetValue("F_SZXY_MMDDDDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDDDDMin"]), i);
                        this.Model.SetValue("F_SZXY_KXLAAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLAAVG"]), i);
                        this.Model.SetValue("F_SZXY_KXLAAVGMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLAAVGMax"]), i);
                        this.Model.SetValue("F_SZXY_KXLAAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLAAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_KXL", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXL"]), i);
                        this.Model.SetValue("F_SZXY_KXLMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLMAx"]), i);
                        this.Model.SetValue("F_SZXY_KXLMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLMin"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDAVG"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDAVGMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDAVGMax"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMD"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDMax"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDMin"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDAVG"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDAVGMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDAVGMAx"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTD"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDMax"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDMin"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDAVG"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDAVGMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDAVGMax"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMD"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDMAx"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDMin"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDAVG"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDAVGMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDAVGMAx"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTD"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDMAx"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDMin"]), i);
                        this.Model.SetValue("F_SZXY_CCQDAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDAVG"]), i);
                        this.Model.SetValue("F_SZXY_CCQDAVGMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDAVGMax"]), i);
                        this.Model.SetValue("F_SZXY_CCQDAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_CCQD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQD"]), i);
                        this.Model.SetValue("F_SZXY_CCQDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDMax"]), i);
                        this.Model.SetValue("F_SZXY_CCQDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDMin"]), i);
                        this.Model.SetValue("F_SZXY_BLLAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLAVG"]), i);
                        this.Model.SetValue("F_SZXY_BLLAVGMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLAVGMAx"]), i);
                        this.Model.SetValue("F_SZXY_BLLAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_BLL", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLL"]), i);
                        this.Model.SetValue("F_SZXY_BLLMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLMAx"]), i);
                        this.Model.SetValue("F_SZXY_BLLMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLMin"]), i);
                        this.Model.SetValue("F_SZXY_90MDAVG", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDAVG"]), i);
                        this.Model.SetValue("F_SZXY_90MDAVGMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDAVGMax"]), i);
                        this.Model.SetValue("F_SZXY_90MDAVGMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDAVGMin"]), i);
                        this.Model.SetValue("F_SZXY_90MD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MD"]), i);
                        this.Model.SetValue("F_SZXY_90MDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDMax"]), i);
                        this.Model.SetValue("F_SZXY_90MDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDMin"]), i);
                        this.Model.SetValue("F_SZXY_Decimal", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal"]), i);
                        this.Model.SetValue("F_SZXY_Decimal1", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal1"]), i);
                        this.Model.SetValue("F_SZXY_Decimal2", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal2"]), i);
                        this.Model.SetValue("F_SZXY_Decimal3", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal3"]), i);
                        this.Model.SetValue("F_SZXY_Decimal4", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal4"]), i);
                        this.Model.SetValue("F_SZXY_Decimal5", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal5"]), i);
                        this.Model.SetValue("F_SZXY_Decimal6", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal6"]), i);
                        this.Model.SetValue("F_SZXY_Decimal7", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal7"]), i);
                        this.Model.SetValue("F_SZXY_Decimal8", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal8"]), i);
                        this.Model.SetValue("F_SZXY_Decimal9", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal9"]), i);
                        this.Model.SetValue("F_SZXY_Decimal10", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal10"]), i);
                        this.Model.SetValue("F_SZXY_Decimal11", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal11"]), i);
                        this.Model.SetValue("F_SZXY_Decimal12", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal12"]), i);
                        this.Model.SetValue("F_SZXY_Decimal13", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal13"]), i);
                        this.Model.SetValue("F_SZXY_Decimal14", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal14"]), i);
                        this.Model.SetValue("F_SZXY_Decimal15", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal15"]), i);
                        this.Model.SetValue("F_SZXY_Decimal16", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal16"]), i);
                        this.Model.SetValue("F_SZXY_Decimal17", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal17"]), i);
                        this.Model.SetValue("F_SZXY_Decimal18", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal18"]), i);
                        this.Model.SetValue("F_SZXY_Decimal19", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal19"]), i);
                        this.Model.SetValue("F_SZXY_Decimal20", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal20"]), i);
                        this.Model.SetValue("F_SZXY_Decimal21", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal21"]), i);
                        this.Model.SetValue("F_SZXY_Decimal22", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal22"]), i);
                        this.Model.SetValue("F_SZXY_Decimal23", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal23"]), i);
                        this.Model.SetValue("F_SZXY_Decimal24", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal24"]), i);
                        this.Model.SetValue("F_SZXY_Decimal25", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal25"]), i);
                        this.Model.SetValue("F_SZXY_Decimal26", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal26"]), i);
                        this.Model.SetValue("F_SZXY_Decimal27", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal27"]), i);
                        this.Model.SetValue("F_SZXY_Decimal28", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal28"]), i);
                        this.Model.SetValue("F_SZXY_Decimal29", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal29"]), i);
                        this.Model.SetValue("F_SZXY_Decimal30", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal30"]), i);
                        this.Model.SetValue("F_SZXY_Decimal31", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal31"]), i);
                        this.Model.SetValue("F_SZXY_Decimal32", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal32"]), i);
                        this.Model.SetValue("F_SZXY_Decimal33", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal33"]), i);
                        this.Model.SetValue("F_SZXY_Decimal34", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal34"]), i);
                        this.Model.SetValue("F_SZXY_Decimal35", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal35"]), i);
                        this.Model.SetValue("F_SZXY_Decimal36", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal36"]), i);
                        this.Model.SetValue("F_SZXY_Decimal37", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal37"]), i);
                        this.Model.SetValue("F_SZXY_Decimal38", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal38"]), i);
                        this.Model.SetValue("F_SZXY_Decimal39", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal39"]), i);
                        this.Model.SetValue("F_SZXY_Decimal40", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal40"]), i);
                        this.Model.SetValue("F_SZXY_Decimal41", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal41"]), i);
                        this.Model.SetValue("F_SZXY_Decimal42", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal42"]), i);
                        this.Model.SetValue("F_SZXY_Decimal43", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal43"]), i);
                        this.Model.SetValue("F_SZXY_Decimal44", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal44"]), i);
                        this.Model.SetValue("F_SZXY_Decimal45", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal45"]), i);
                        this.Model.SetValue("F_SZXY_Decimal46", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal46"]), i);
                        this.Model.SetValue("F_SZXY_Decimal47", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal47"]), i);
                        this.Model.SetValue("F_SZXY_Decimal48", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal48"]), i);
                        this.Model.SetValue("F_SZXY_Decimal49", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal49"]), i);
                        this.Model.SetValue("F_SZXY_Decimal50", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal50"]), i);
                        this.Model.SetValue("F_SZXY_Decimal51", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal51"]), i);
                        this.Model.SetValue("F_SZXY_Decimal52", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal52"]), i);
                        this.Model.SetValue("F_SZXY_Decimal53", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal53"]), i);
                        this.Model.SetValue("F_SZXY_Decimal54", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal54"]), i);
                        this.Model.SetValue("F_SZXY_Decimal55", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal55"]), i);
                        this.Model.SetValue("F_SZXY_Decimal56", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal56"]), i);
                        this.Model.SetValue("F_SZXY_Decimal57", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal57"]), i);
                        this.Model.SetValue("F_SZXY_Decimal58", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal58"]), i);
                        this.Model.SetValue("F_SZXY_Decimal59", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal59"]), i);
                        this.Model.SetValue("F_SZXY_Decimal60", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal60"]), i);
                        this.Model.SetValue("F_SZXY_Decimal61", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal61"]), i);
                        this.Model.SetValue("F_SZXY_Decimal62", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal62"]), i);

                        //
                        this.Model.SetValue("F_SZXY_Decimal63", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal63"]), i);
                        this.Model.SetValue("F_SZXY_Decimal64", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal64"]), i);
                        this.Model.SetValue("F_SZXY_Decimal65", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal65"]), i);
                        this.Model.SetValue("F_SZXY_Decimal66", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal66"]), i);
                        this.Model.SetValue("F_SZXY_Decimal67", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal67"]), i);
                        this.Model.SetValue("F_SZXY_Decimal68", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal68"]), i);
                        this.Model.SetValue("F_SZXY_Decimal69", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal69"]), i);
                        this.Model.SetValue("F_SZXY_Decimal70", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal70"]), i);
                        this.Model.SetValue("F_SZXY_Decimal71", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal71"]), i);
                        this.Model.SetValue("F_SZXY_Decimal72", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal72"]), i);
                        this.Model.SetValue("F_SZXY_Decimal73", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal73"]), i);
                        this.Model.SetValue("F_SZXY_Decimal74", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal74"]), i);
                        this.Model.SetValue("F_SZXY_Decimal75", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal75"]), i);
                        this.Model.SetValue("F_SZXY_Decimal76", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal76"]), i);
                        this.Model.SetValue("F_SZXY_Decimal77", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal77"]), i);
                        this.Model.SetValue("F_SZXY_Decimal78", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal78"]), i);
                        this.Model.SetValue("F_SZXY_Decimal79", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal79"]), i);
                        this.Model.SetValue("F_SZXY_Decimal80", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal80"]), i);
                        this.Model.SetValue("F_SZXY_Decimal81", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal81"]), i);
                        this.Model.SetValue("F_SZXY_Decimal82", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal82"]), i);
                        this.Model.SetValue("F_SZXY_Decimal83", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal83"]), i);
                        this.Model.SetValue("F_SZXY_Decimal84", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal84"]), i);
                        this.Model.SetValue("F_SZXY_Decimal85", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal85"]), i);
                        this.Model.SetValue("F_SZXY_Decimal86", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal86"]), i);

                    }
                    this.View.UpdateView("F_SZXY_NKWLXNJYBEntity");
                }
                #endregion
            }
            else if (formID == "SZXY_WKJYBZ")
            {
                #region
                string SelectBindSql = $"/*dialect*/select * from SZXY_t_WKJYBZEntry where F_SZXY_OrgId={Convert.ToString(billObj["F_SZXY_OrgIdH_Id"])}";
                DataSet GetData = DBServiceHelper.ExecuteDataSet(this.Context, SelectBindSql);

                if (GetData != null && GetData.Tables.Count > 0 && GetData.Tables[0].Rows.Count > 0)
                {
                    this.Model.ClearNoDataRow();
                    this.Model.DeleteEntryData("F_SZXY_WKJYBZEntity");
               
                    for (int i = 0; i < GetData.Tables[0].Rows.Count; i++)
                    {
                        this.Model.CreateNewEntryRow("F_SZXY_WKJYBZEntity");
                       if(!Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_CUST"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_CUST", Convert.ToInt32(GetData.Tables[0].Rows[i]["F_SZXY_CUST"]), i);
                        if (!Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_OrgId"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_OrgId", Convert.ToInt32(GetData.Tables[0].Rows[i]["F_SZXY_OrgId"]), i);
                        if (!Convert.ToString(GetData.Tables[0].Rows[i]["F_SZXY_Material"]).IsNullOrEmptyOrWhiteSpace()) this.Model.SetValue("F_SZXY_Material", Convert.ToInt32(GetData.Tables[0].Rows[i]["F_SZXY_Material"]), i);
                   
                        this.Model.SetValue("F_SZXY_Ply", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Ply"]), i);
                        this.Model.SetValue("F_SZXY_PlyMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyMax"]), i);
                        this.Model.SetValue("F_SZXY_PlyMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_PlyMin"]), i);
      
                        this.Model.SetValue("F_SZXY_TQL", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQL"]), i);
                        this.Model.SetValue("F_SZXY_TQLMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLMax"]), i);
                        this.Model.SetValue("F_SZXY_TQLMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_TQLMin"]), i);
                         
                        this.Model.SetValue("F_SZXY_MMDDDD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDDDD"]), i);
                        this.Model.SetValue("F_SZXY_MMDDDDMAX", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDDDDMAX"]), i);
                        this.Model.SetValue("F_SZXY_MMDDDDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_MMDDDDMin"]), i);
                      
                        this.Model.SetValue("F_SZXY_KXL", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXL"]), i);
                        this.Model.SetValue("F_SZXY_KXLMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLMAx"]), i);
                        this.Model.SetValue("F_SZXY_KXLMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_KXLMin"]), i);
                       
                        this.Model.SetValue("F_SZXY_LSQDMD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMD"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDMax"]), i);
                        this.Model.SetValue("F_SZXY_LSQDMDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDMDMin"]), i);
                      
                        this.Model.SetValue("F_SZXY_LSQDTD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTD"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDMax"]), i);
                        this.Model.SetValue("F_SZXY_LSQDTDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_LSQDTDMin"]), i);
                   
                        this.Model.SetValue("F_SZXY_DLSCLMD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMD"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDMAx"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLMDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLMDMin"]), i);
                      
                        this.Model.SetValue("F_SZXY_DLSCLTD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTD"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDMAx"]), i);
                        this.Model.SetValue("F_SZXY_DLSCLTDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_DLSCLTDMin"]), i);
                        
                        this.Model.SetValue("F_SZXY_CCQD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQD"]), i);
                        this.Model.SetValue("F_SZXY_CCQDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDMax"]), i);
                        this.Model.SetValue("F_SZXY_CCQDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_CCQDMin"]), i);
                     
                        this.Model.SetValue("F_SZXY_BLL", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLL"]), i);
                        this.Model.SetValue("F_SZXY_BLLMAx", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLMAx"]), i);
                        this.Model.SetValue("F_SZXY_BLLMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_BLLMin"]), i);
                     
                        this.Model.SetValue("F_SZXY_90MD", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MD"]), i);
                        this.Model.SetValue("F_SZXY_90MDMax", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDMax"]), i);
                        this.Model.SetValue("F_SZXY_90MDMin", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_90MDMin"]), i);
                      
                        this.Model.SetValue("F_SZXY_Decimal3", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal3"]), i);
                        this.Model.SetValue("F_SZXY_Decimal4", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal4"]), i);
                        this.Model.SetValue("F_SZXY_Decimal5", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal5"]), i);
                        this.Model.SetValue("F_SZXY_Decimal6", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal6"]), i);
                        this.Model.SetValue("F_SZXY_Decimal7", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal7"]), i);
                        this.Model.SetValue("F_SZXY_Decimal8", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal8"]), i);
                       
                        this.Model.SetValue("F_SZXY_Decimal12", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal12"]), i);
                        this.Model.SetValue("F_SZXY_Decimal13", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal13"]), i);
                        this.Model.SetValue("F_SZXY_Decimal14", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal14"]), i);
                        
                        this.Model.SetValue("F_SZXY_Decimal18", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal18"]), i);
                        this.Model.SetValue("F_SZXY_Decimal19", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal19"]), i);
                        this.Model.SetValue("F_SZXY_Decimal20", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal20"]), i);
                     
                        this.Model.SetValue("F_SZXY_Decimal24", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal24"]), i);
                        this.Model.SetValue("F_SZXY_Decimal25", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal25"]), i);
                        this.Model.SetValue("F_SZXY_Decimal26", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal26"]), i);
                        
                        this.Model.SetValue("F_SZXY_Decimal30", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal30"]), i);
                        this.Model.SetValue("F_SZXY_Decimal31", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal31"]), i);
                        this.Model.SetValue("F_SZXY_Decimal32", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal32"]), i);
                         
                        this.Model.SetValue("F_SZXY_Decimal36", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal36"]), i);
                        this.Model.SetValue("F_SZXY_Decimal37", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal37"]), i);
                        this.Model.SetValue("F_SZXY_Decimal38", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal38"]), i);
                        
                        this.Model.SetValue("F_SZXY_Decimal42", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal42"]), i);
                        this.Model.SetValue("F_SZXY_Decimal43", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal43"]), i);
                        this.Model.SetValue("F_SZXY_Decimal44", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal44"]), i);
                        
                        this.Model.SetValue("F_SZXY_Decimal48", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal48"]), i);
                        this.Model.SetValue("F_SZXY_Decimal49", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal49"]), i);
                        this.Model.SetValue("F_SZXY_Decimal50", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal50"]), i);
                      
                        this.Model.SetValue("F_SZXY_Decimal54", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal54"]), i);
                        this.Model.SetValue("F_SZXY_Decimal55", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal55"]), i);
                        this.Model.SetValue("F_SZXY_Decimal56", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal56"]), i);
                        this.Model.SetValue("F_SZXY_Decimal57", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal57"]), i);
                        this.Model.SetValue("F_SZXY_Decimal58", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal58"]), i);
                        this.Model.SetValue("F_SZXY_Decimal59", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal59"]), i);
                        this.Model.SetValue("F_SZXY_Decimal60", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal60"]), i);
                        this.Model.SetValue("F_SZXY_Decimal61", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal61"]), i);
                        this.Model.SetValue("F_SZXY_Decimal62", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal62"]), i);

                        //
                        this.Model.SetValue("F_SZXY_Decimal", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal1"]), i);
                        this.Model.SetValue("F_SZXY_Decimal1", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal1"]), i);
                        this.Model.SetValue("F_SZXY_Decimal2", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal2"]), i);
                        this.Model.SetValue("F_SZXY_Decimal9", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal9"]), i);
                        this.Model.SetValue("F_SZXY_Decimal10", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal10"]), i);
                        this.Model.SetValue("F_SZXY_Decimal11", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal11"]), i);
                        this.Model.SetValue("F_SZXY_Decimal15", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal15"]), i);
                        this.Model.SetValue("F_SZXY_Decimal16", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal16"]), i);
                        this.Model.SetValue("F_SZXY_Decimal17", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal17"]), i);
                        this.Model.SetValue("F_SZXY_Decimal21", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal21"]), i);
                        this.Model.SetValue("F_SZXY_Decimal22", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal22"]), i);
                        this.Model.SetValue("F_SZXY_Decimal23", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal23"]), i);

                    }
                    this.View.UpdateView("F_SZXY_WKJYBZEntity");
                }
                #endregion
            }
            else if (formID == "SZXY_LYMM")
            {
                #region
                string SelectBindSql = $"/*dialect*/select * from SZXY_t_LYMEntry ";
                DataSet GetData = DBServiceHelper.ExecuteDataSet(this.Context, SelectBindSql);

                if (GetData != null && GetData.Tables.Count > 0 && GetData.Tables[0].Rows.Count > 0)
                {
                    this.Model.ClearNoDataRow();
                    this.Model.DeleteEntryData("F_SZXY_LYMEntity");

                    for (int i = 0; i < GetData.Tables[0].Rows.Count; i++)
                    {
                        this.Model.CreateNewEntryRow("F_SZXY_LYMEntity");
 
                        this.Model.SetValue("F_SZXY_Decimal", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal"]), i);
                        this.Model.SetValue("F_SZXY_Decimal1", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal1"]), i);
                        this.Model.SetValue("F_SZXY_Decimal2", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal2"]), i);
                        this.Model.SetValue("F_SZXY_Decimal3", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal3"]), i);
                        this.Model.SetValue("F_SZXY_Decimal4", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal4"]), i);
                        this.Model.SetValue("F_SZXY_Decimal5", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal5"]), i);
                        this.Model.SetValue("F_SZXY_Decimal6", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal6"]), i);
                        this.Model.SetValue("F_SZXY_Decimal7", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal7"]), i);
                        this.Model.SetValue("F_SZXY_Decimal8", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal8"]), i);
                        this.Model.SetValue("F_SZXY_Decimal9", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal9"]), i);
                        this.Model.SetValue("F_SZXY_Decimal10", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal10"]), i);
                        this.Model.SetValue("F_SZXY_Decimal11", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal11"]), i);
                        this.Model.SetValue("F_SZXY_Decimal12", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal12"]), i);
                        this.Model.SetValue("F_SZXY_Decimal13", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal13"]), i);
                        this.Model.SetValue("F_SZXY_Decimal14", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal14"]), i);
                        this.Model.SetValue("F_SZXY_Decimal15", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal15"]), i);
                        this.Model.SetValue("F_SZXY_Decimal16", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal16"]), i);
                        this.Model.SetValue("F_SZXY_Decimal17", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal17"]), i);
                        this.Model.SetValue("F_SZXY_Decimal18", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal18"]), i);
                        this.Model.SetValue("F_SZXY_Decimal19", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal19"]), i);
                        this.Model.SetValue("F_SZXY_Decimal20", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal20"]), i);
                        this.Model.SetValue("F_SZXY_Decimal21", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal21"]), i);
                        this.Model.SetValue("F_SZXY_Decimal22", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal22"]), i);
                        this.Model.SetValue("F_SZXY_Decimal23", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal23"]), i);
                        this.Model.SetValue("F_SZXY_Decimal24", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal24"]), i);
                        this.Model.SetValue("F_SZXY_Decimal25", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal25"]), i);
                        this.Model.SetValue("F_SZXY_Decimal26", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal26"]), i);
                        this.Model.SetValue("F_SZXY_Decimal27", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal27"]), i);
                        this.Model.SetValue("F_SZXY_Decimal28", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal28"]), i);
                        this.Model.SetValue("F_SZXY_Decimal29", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal29"]), i);
                        this.Model.SetValue("F_SZXY_Decimal30", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal30"]), i);
                        this.Model.SetValue("F_SZXY_Decimal31", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal31"]), i);
                        this.Model.SetValue("F_SZXY_Decimal32", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal32"]), i);
                        this.Model.SetValue("F_SZXY_Decimal33", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal33"]), i);
                        this.Model.SetValue("F_SZXY_Decimal34", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal34"]), i);
                        this.Model.SetValue("F_SZXY_Decimal35", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal35"]), i);
                        this.Model.SetValue("F_SZXY_Decimal36", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal36"]), i);
                        this.Model.SetValue("F_SZXY_Decimal37", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal37"]), i);
                        this.Model.SetValue("F_SZXY_Decimal38", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal38"]), i);
                        this.Model.SetValue("F_SZXY_Decimal39", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal39"]), i);
                        this.Model.SetValue("F_SZXY_Decimal40", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal40"]), i);
                        this.Model.SetValue("F_SZXY_Decimal41", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal41"]), i);
                        this.Model.SetValue("F_SZXY_Decimal42", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal42"]), i);
                        this.Model.SetValue("F_SZXY_Decimal43", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal43"]), i);
                        this.Model.SetValue("F_SZXY_Decimal44", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal44"]), i);
                        this.Model.SetValue("F_SZXY_Decimal45", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal45"]), i);
                        this.Model.SetValue("F_SZXY_Decimal46", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal46"]), i);
                        this.Model.SetValue("F_SZXY_Decimal47", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal47"]), i);
                        this.Model.SetValue("F_SZXY_Decimal48", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal48"]), i);
                        this.Model.SetValue("F_SZXY_Decimal49", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal49"]), i);
                        this.Model.SetValue("F_SZXY_Decimal50", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal50"]), i);
                        this.Model.SetValue("F_SZXY_Decimal51", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal51"]), i);
                        this.Model.SetValue("F_SZXY_Decimal52", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal52"]), i);
                        this.Model.SetValue("F_SZXY_Decimal53", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal53"]), i);
                        this.Model.SetValue("F_SZXY_Decimal54", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal54"]), i);
                        this.Model.SetValue("F_SZXY_Decimal55", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal55"]), i);
                        this.Model.SetValue("F_SZXY_Decimal56", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal56"]), i);
                        this.Model.SetValue("F_SZXY_Decimal57", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal57"]), i);
                        this.Model.SetValue("F_SZXY_Decimal58", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal58"]), i);
                        this.Model.SetValue("F_SZXY_Decimal59", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal59"]), i);
                        this.Model.SetValue("F_SZXY_Decimal60", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal60"]), i);
                        this.Model.SetValue("F_SZXY_Decimal61", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal61"]), i);
                        this.Model.SetValue("F_SZXY_Decimal62", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal62"]), i);

                        //
                        this.Model.SetValue("F_SZXY_Decimal63", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal63"]), i);
                        this.Model.SetValue("F_SZXY_Decimal64", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal64"]), i);
                        this.Model.SetValue("F_SZXY_Decimal65", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal65"]), i);
                        this.Model.SetValue("F_SZXY_Decimal66", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal66"]), i);
                        this.Model.SetValue("F_SZXY_Decimal67", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal67"]), i);
                        this.Model.SetValue("F_SZXY_Decimal68", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal68"]), i);
                        this.Model.SetValue("F_SZXY_Decimal69", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal69"]), i);
                        this.Model.SetValue("F_SZXY_Decimal70", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal70"]), i);
                        this.Model.SetValue("F_SZXY_Decimal71", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal71"]), i);
                        this.Model.SetValue("F_SZXY_Decimal72", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal72"]), i);
                        this.Model.SetValue("F_SZXY_Decimal73", Convert.ToDecimal(GetData.Tables[0].Rows[i]["F_SZXY_Decimal73"]), i);
                   

                    }
                    this.View.UpdateView("F_SZXY_LYMEntity");
                }
                #endregion
            }


        }
       
  
        /// <summary>
        /// 获取当前单据,字段的唯一标识
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetFieldsKey(string formID)
        {
            Dictionary<string, string> FieldsKey = new Dictionary<string, string>();
            try
            {
                switch (formID.ToUpper())
                {
                    //[CONFIG]
                    case "SZXY_NKWLXNJYBZ":// 
                        FieldsKey.Add("Entry", "SZXY_NKWLXNJYBEntry");
                        FieldsKey.Add("Table", "SZXY_t_NKWLXNJYBEntry");
                        FieldsKey.Add("组织", "F_SZXY_ORGID");
                        FieldsKey.Add("产品型号", "F_SZXY_Material");
                        FieldsKey.Add("厚度（平均值）", "F_SZXY_PlyAvg");
                        FieldsKey.Add("厚度（平均值）最小值", "F_SZXY_PlyAvgMin");
                        FieldsKey.Add("厚度（平均值）最大值", "F_SZXY_PlyAvgMax");
                        FieldsKey.Add("厚度", "F_SZXY_Ply");
                        FieldsKey.Add("厚度最大值", "F_SZXY_PlyMax");
                        FieldsKey.Add("厚度最小值", "F_SZXY_PlyMin");
                        FieldsKey.Add("透气率（平均值）", "F_SZXY_TQLAVG");
                        FieldsKey.Add("透气率（平均值）最大值", "F_SZXY_TQLAVGMax");
                        FieldsKey.Add("透气率（平均值）最小值", "F_SZXY_TQLAVGMin");
                        FieldsKey.Add("透气率", "F_SZXY_TQL");
                        FieldsKey.Add("透气率最大值", "F_SZXY_TQLMax");
                        FieldsKey.Add("透气率最小值", "F_SZXY_TQLMin");
                        FieldsKey.Add("面密度（平均值）", "F_SZXY_MMDAvg");
                        FieldsKey.Add("面密度（平均值）最大值", "F_SZXY_MMDAvgMax");
                        FieldsKey.Add("面密度（平均值）最小值", "F_SZXY_MMDAvgMin");
                        FieldsKey.Add("面密度（对单点）", "F_SZXY_MMDDDD");
                        FieldsKey.Add("面密度（对单点）最大值", "F_SZXY_MMDDDDMAX");
                        FieldsKey.Add("面密度（对单点）最小值", "F_SZXY_MMDDDDMin");
                        FieldsKey.Add("孔隙率（平均值）", "F_SZXY_KXLAAVG");
                        FieldsKey.Add("孔隙率（平均值）最大值", "F_SZXY_KXLAAVGMax");
                        FieldsKey.Add("孔隙率（平均值）最小值", "F_SZXY_KXLAAVGMin");
                        FieldsKey.Add("孔隙率", "F_SZXY_KXL");
                        FieldsKey.Add("孔隙率最大值", "F_SZXY_KXLMAx");
                        FieldsKey.Add("孔隙率最小值", "F_SZXY_KXLMin");
                        FieldsKey.Add("拉伸强度MD（平均值）", "F_SZXY_LSQDMDAVG");
                        FieldsKey.Add("拉伸强度MD（平均值）最大值", "F_SZXY_LSQDMDAVGMax");
                        FieldsKey.Add("拉伸强度MD（平均值）最小值", "F_SZXY_LSQDMDAVGMin");
                        FieldsKey.Add("拉伸强度MD", "F_SZXY_LSQDMD");
                        FieldsKey.Add("拉伸强度MD最大值", "F_SZXY_LSQDMDMax");
                        FieldsKey.Add("拉伸强度MD最小值", "F_SZXY_LSQDMDMin");
                        FieldsKey.Add("拉伸强度TD（平均值）", "F_SZXY_LSQDTDAVG");
                        FieldsKey.Add("拉伸强度TD（平均值）最大值", "F_SZXY_LSQDTDAVGMAx");
                        FieldsKey.Add("拉伸强度TD（平均值）最小值", "F_SZXY_LSQDTDAVGMin");
                        FieldsKey.Add("拉伸强度TD", "F_SZXY_LSQDTD");
                        FieldsKey.Add("拉伸强度TD最大值", "F_SZXY_LSQDTDMax");
                        FieldsKey.Add("拉伸强度TD最小值", "F_SZXY_LSQDTDMin");
                        FieldsKey.Add("断裂伸长率MD（平均值）", "F_SZXY_DLSCLMDAVG");
                        FieldsKey.Add("断裂伸长率MD（平均值）最大值", "F_SZXY_DLSCLMDAVGMax");
                        FieldsKey.Add("断裂伸长率MD（平均值）最小值", "F_SZXY_DLSCLMDAVGMin");
                        FieldsKey.Add("断裂伸长率MD", "F_SZXY_DLSCLMD");
                        FieldsKey.Add("断裂伸长率MD最大值", "F_SZXY_DLSCLMDMAx");
                        FieldsKey.Add("断裂伸长率MD最小值", "F_SZXY_DLSCLMDMin");
                        FieldsKey.Add("断裂伸长率TD（平均值）", "F_SZXY_DLSCLTDAVG");
                        FieldsKey.Add("断裂伸长率TD（平均值）最大值", "F_SZXY_DLSCLTDAVGMAx");
                        FieldsKey.Add("断裂伸长率TD（平均值）最小值", "F_SZXY_DLSCLTDAVGMin");
                        FieldsKey.Add("断裂伸长率TD", "F_SZXY_DLSCLTD");
                        FieldsKey.Add("断裂伸长率TD最大值", "F_SZXY_DLSCLTDMAx");
                        FieldsKey.Add("断裂伸长率TD最小值", "F_SZXY_DLSCLTDMin");
                        FieldsKey.Add("穿刺强度（平均值）", "F_SZXY_CCQDAVG");
                        FieldsKey.Add("穿刺强度（平均值）最大值", "F_SZXY_CCQDAVGMax");
                        FieldsKey.Add("穿刺强度（平均值）最小值", "F_SZXY_CCQDAVGMin");
                        FieldsKey.Add("穿刺强度", "F_SZXY_CCQD");
                        FieldsKey.Add("穿刺强度最大值", "F_SZXY_CCQDMax");
                        FieldsKey.Add("穿刺强度最小值", "F_SZXY_CCQDMin");
                        FieldsKey.Add("剥离力（平均值）", "F_SZXY_BLLAVG");
                        FieldsKey.Add("剥离力（平均值）最大值", "F_SZXY_BLLAVGMAx");
                        FieldsKey.Add("剥离力（平均值）最小值", "F_SZXY_BLLAVGMin");
                        FieldsKey.Add("剥离力", "F_SZXY_BLL");
                        FieldsKey.Add("剥离力最大值", "F_SZXY_BLLMAx");
                        FieldsKey.Add("剥离力最小值", "F_SZXY_BLLMin");
                        FieldsKey.Add("90℃MD（平均值）", "F_SZXY_90MDAVG");
                        FieldsKey.Add("90℃MD（平均值）最大值", "F_SZXY_90MDAVGMax");
                        FieldsKey.Add("90℃MD（平均值）最小值", "F_SZXY_90MDAVGMin");
                        FieldsKey.Add("90℃MD", "F_SZXY_90MD");
                        FieldsKey.Add("90℃MD最大值", "F_SZXY_90MDMax");
                        FieldsKey.Add("90℃MD最小值", "F_SZXY_90MDMin");
                        FieldsKey.Add("90℃TD（平均值）", "F_SZXY_DECIMAL");
                        FieldsKey.Add("90℃TD（平均值）最大值", "F_SZXY_DECIMAL1 ");
                        FieldsKey.Add("90℃TD（平均值）最小值", "F_SZXY_DECIMAL2 ");
                        FieldsKey.Add("90℃TD", "F_SZXY_DECIMAL3 ");
                        FieldsKey.Add("90℃TD最大值", "F_SZXY_DECIMAL4 ");
                        FieldsKey.Add("90℃TD最小值", "F_SZXY_DECIMAL5 ");
                        FieldsKey.Add("105℃判定结果", "F_SZXY_DECIMAL6 ");
                        FieldsKey.Add("105℃判定结果最大值", "F_SZXY_DECIMAL7 ");
                        FieldsKey.Add("105℃判定结果最小值", "F_SZXY_DECIMAL8 ");
                        FieldsKey.Add("105℃MD（平均值）", "F_SZXY_DECIMAL9 ");
                        FieldsKey.Add("105℃MD（平均值）最大值", "F_SZXY_DECIMAL10");
                        FieldsKey.Add("105℃MD（平均值）最小值", "F_SZXY_DECIMAL11");
                        FieldsKey.Add("105℃MD", "F_SZXY_DECIMAL12");
                        FieldsKey.Add("105℃MD最大值", "F_SZXY_DECIMAL13");
                        FieldsKey.Add("105℃MD最小值", "F_SZXY_DECIMAL14");
                        FieldsKey.Add("105℃TD（平均值）", "F_SZXY_DECIMAL15");
                        FieldsKey.Add("105℃TD（平均值）最大值", "F_SZXY_DECIMAL16");
                        FieldsKey.Add("105℃TD（平均值）最小值", "F_SZXY_DECIMAL17");
                        FieldsKey.Add("105℃TD", "F_SZXY_DECIMAL18");
                        FieldsKey.Add("105℃TD最大值", "F_SZXY_DECIMAL19");
                        FieldsKey.Add("105℃TD最小值", "F_SZXY_DECIMAL20");
                        FieldsKey.Add("120℃MD（平均值）", "F_SZXY_DECIMAL21");
                        FieldsKey.Add("120℃MD（平均值）最大值", "F_SZXY_DECIMAL22");
                        FieldsKey.Add("120℃MD（平均值）最小值", "F_SZXY_DECIMAL23");
                        FieldsKey.Add("120℃MD", "F_SZXY_DECIMAL24");
                        FieldsKey.Add("120℃MD最大值", "F_SZXY_DECIMAL25");
                        FieldsKey.Add("120℃MD最小值", "F_SZXY_DECIMAL26");
                        FieldsKey.Add("120℃TD（平均值）", "F_SZXY_DECIMAL27");
                        FieldsKey.Add("120℃TD（平均值）最大值", "F_SZXY_DECIMAL28");
                        FieldsKey.Add("120℃TD（平均值）最小值", "F_SZXY_DECIMAL29");
                        FieldsKey.Add("120℃TD", "F_SZXY_DECIMAL30");
                        FieldsKey.Add("120℃TD最大值", "F_SZXY_DECIMAL31");
                        FieldsKey.Add("120℃TD最小值", "F_SZXY_DECIMAL32");
                        FieldsKey.Add("130℃MD（平均值）", "F_SZXY_DECIMAL33");
                        FieldsKey.Add("130℃MD（平均值）最大值", "F_SZXY_DECIMAL34");
                        FieldsKey.Add("130℃MD（平均值）最小值", "F_SZXY_DECIMAL35");
                        FieldsKey.Add("130℃MD", "F_SZXY_DECIMAL36");
                        FieldsKey.Add("130℃MD最大值", "F_SZXY_DECIMAL37");
                        FieldsKey.Add("130℃MD最小值", "F_SZXY_DECIMAL38");
                        FieldsKey.Add("130℃TD（平均值）", "F_SZXY_DECIMAL39");
                        FieldsKey.Add("130℃TD（平均值）最大值", "F_SZXY_DECIMAL40");
                        FieldsKey.Add("130℃TD（平均值）最小值", "F_SZXY_DECIMAL41");
                        FieldsKey.Add("130℃TD", "F_SZXY_DECIMAL42");
                        FieldsKey.Add("130℃TD最大值", "F_SZXY_DECIMAL43");
                        FieldsKey.Add("130℃TD最小值", "F_SZXY_DECIMAL44");
                        FieldsKey.Add("150℃MD（平均值）", "F_SZXY_DECIMAL45");
                        FieldsKey.Add("150℃MD（平均值）最大值", "F_SZXY_DECIMAL46");
                        FieldsKey.Add("150℃MD（平均值）最小值", "F_SZXY_DECIMAL47");
                        FieldsKey.Add("150℃MD", "F_SZXY_DECIMAL48");
                        FieldsKey.Add("150℃MD最大值", "F_SZXY_DECIMAL49");
                        FieldsKey.Add("150℃MD最小值", "F_SZXY_DECIMAL50");
                        FieldsKey.Add("150℃TD（平均值）", "F_SZXY_DECIMAL51");
                        FieldsKey.Add("150℃TD（平均值）最大值", "F_SZXY_DECIMAL52");
                        FieldsKey.Add("150℃TD（平均值）最小值", "F_SZXY_DECIMAL53");
                        FieldsKey.Add("150℃TD", "F_SZXY_DECIMAL54");
                        FieldsKey.Add("150℃TD最大值", "F_SZXY_DECIMAL55");
                        FieldsKey.Add("150℃TD最小值", "F_SZXY_DECIMAL56");
                        FieldsKey.Add("水分含量", "F_SZXY_DECIMAL57");
                        FieldsKey.Add("水分含量最大值", "F_SZXY_DECIMAL58");
                        FieldsKey.Add("水分含量最小值", "F_SZXY_DECIMAL59");
                        FieldsKey.Add("PCL", "F_SZXY_DECIMAL60");
                        FieldsKey.Add("PCL最大值", "F_SZXY_DECIMAL61");
                        FieldsKey.Add("PCL最小值", "F_SZXY_DECIMAL62");
                        break;
                    case "SZXY_WKJYBZ":
                        FieldsKey.Add("Entry", "SZXY_WKJYBZEntry");
                        FieldsKey.Add("Table", "SZXY_t_WKJYBZEntry");
                        FieldsKey.Add("组织", "F_SZXY_ORGID");
                        FieldsKey.Add("客户", "F_SZXY_Cust");
                        FieldsKey.Add("产品型号", "F_SZXY_Material");
                        FieldsKey.Add("厚度", "F_SZXY_Ply");
                        FieldsKey.Add("厚度最大值", "F_SZXY_PlyMax");
                        FieldsKey.Add("厚度最小值", "F_SZXY_PlyMin");
                        FieldsKey.Add("透气率", "F_SZXY_TQL");
                        FieldsKey.Add("透气率最大值", "F_SZXY_TQLMax");
                        FieldsKey.Add("透气率最小值", "F_SZXY_TQLMin");
                        FieldsKey.Add("面密度（对单点）", "F_SZXY_MMDDDD");
                        FieldsKey.Add("面密度（对单点）最大值", "F_SZXY_MMDDDDMAX");
                        FieldsKey.Add("面密度（对单点）最小值", "F_SZXY_MMDDDDMin");
                        FieldsKey.Add("孔隙率", "F_SZXY_KXL");
                        FieldsKey.Add("孔隙率最大值", "F_SZXY_KXLMAx");
                        FieldsKey.Add("孔隙率最小值", "F_SZXY_KXLMin");
                        FieldsKey.Add("拉伸强度MD", "F_SZXY_LSQDMD");
                        FieldsKey.Add("拉伸强度MD最大值", "F_SZXY_LSQDMDMax");
                        FieldsKey.Add("拉伸强度MD最小值", "F_SZXY_LSQDMDMin");
                        FieldsKey.Add("拉伸强度TD", "F_SZXY_LSQDTD");
                        FieldsKey.Add("拉伸强度TD最大值", "F_SZXY_LSQDTDMax");
                        FieldsKey.Add("拉伸强度TD最小值", "F_SZXY_LSQDTDMin");
                        FieldsKey.Add("断裂伸长率MD", "F_SZXY_DLSCLMD");
                        FieldsKey.Add("断裂伸长率MD最大值", "F_SZXY_DLSCLMDMAx");
                        FieldsKey.Add("断裂伸长率MD最小值", "F_SZXY_DLSCLMDMin");
                        FieldsKey.Add("断裂伸长率TD", "F_SZXY_DLSCLTD");
                        FieldsKey.Add("断裂伸长率TD最大值", "F_SZXY_DLSCLTDMAx");
                        FieldsKey.Add("断裂伸长率TD最小值", "F_SZXY_DLSCLTDMin");
                        FieldsKey.Add("穿刺强度", "F_SZXY_CCQD");
                        FieldsKey.Add("穿刺强度最大值", "F_SZXY_CCQDMax");
                        FieldsKey.Add("穿刺强度最小值", "F_SZXY_CCQDMin");
                        FieldsKey.Add("剥离力", "F_SZXY_BLL");
                        FieldsKey.Add("剥离力最大值", "F_SZXY_BLLMAx");
                        FieldsKey.Add("剥离力最小值", "F_SZXY_BLLMin");
                        FieldsKey.Add("90℃MD", "F_SZXY_90MD");
                        FieldsKey.Add("90℃MD最大值", "F_SZXY_90MDMax");
                        FieldsKey.Add("90℃MD最小值", "F_SZXY_90MDMin");
                        FieldsKey.Add("90℃TD", "F_SZXY_DECIMAL3 ");
                        FieldsKey.Add("90℃TD最大值", "F_SZXY_DECIMAL4 ");
                        FieldsKey.Add("90℃TD最小值", "F_SZXY_DECIMAL5 ");
                        FieldsKey.Add("105℃判定结果", "F_SZXY_DECIMAL6 ");
                        FieldsKey.Add("105℃判定结果最大值", "F_SZXY_DECIMAL7 ");
                        FieldsKey.Add("105℃判定结果最小值", "F_SZXY_DECIMAL8 ");
                        FieldsKey.Add("105℃MD", "F_SZXY_DECIMAL12");
                        FieldsKey.Add("105℃MD最大值", "F_SZXY_DECIMAL13");
                        FieldsKey.Add("105℃MD最小值", "F_SZXY_DECIMAL14");
                        FieldsKey.Add("105℃TD", "F_SZXY_DECIMAL18");
                        FieldsKey.Add("105℃TD最大值", "F_SZXY_DECIMAL19");
                        FieldsKey.Add("105℃TD最小值", "F_SZXY_DECIMAL20");
                        FieldsKey.Add("120℃MD", "F_SZXY_DECIMAL24");
                        FieldsKey.Add("120℃MD最大值", "F_SZXY_DECIMAL25");
                        FieldsKey.Add("120℃MD最小值", "F_SZXY_DECIMAL26");
                        FieldsKey.Add("120℃TD", "F_SZXY_DECIMAL30");
                        FieldsKey.Add("120℃TD最大值", "F_SZXY_DECIMAL31");
                        FieldsKey.Add("120℃TD最小值", "F_SZXY_DECIMAL32");
                        FieldsKey.Add("130℃MD", "F_SZXY_DECIMAL36");
                        FieldsKey.Add("130℃MD最大值", "F_SZXY_DECIMAL37");
                        FieldsKey.Add("130℃MD最小值", "F_SZXY_DECIMAL38");
                        FieldsKey.Add("130℃TD", "F_SZXY_DECIMAL42");
                        FieldsKey.Add("130℃TD最大值", "F_SZXY_DECIMAL43");
                        FieldsKey.Add("130℃TD最小值", "F_SZXY_DECIMAL44");
                        FieldsKey.Add("150℃MD", "F_SZXY_DECIMAL48");
                        FieldsKey.Add("150℃MD最大值", "F_SZXY_DECIMAL49");
                        FieldsKey.Add("150℃MD最小值", "F_SZXY_DECIMAL50");
                        FieldsKey.Add("150℃TD", "F_SZXY_DECIMAL54");
                        FieldsKey.Add("150℃TD最大值", "F_SZXY_DECIMAL55");
                        FieldsKey.Add("150℃TD最小值", "F_SZXY_DECIMAL56");
                        FieldsKey.Add("水分含量", "F_SZXY_DECIMAL57");
                        FieldsKey.Add("水分含量最大值", "F_SZXY_DECIMAL58");
                        FieldsKey.Add("水分含量最小值", "F_SZXY_DECIMAL59");
                        FieldsKey.Add("PCL", "F_SZXY_DECIMAL60");
                        FieldsKey.Add("PCL最大值", "F_SZXY_DECIMAL61");
                        FieldsKey.Add("PCL最小值", "F_SZXY_DECIMAL62");
                        break;
                }
            }
            catch (Exception) { }

            return FieldsKey;

        }
    }
}
