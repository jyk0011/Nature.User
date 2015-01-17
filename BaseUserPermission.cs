/**
 * 自然框架之登录用户类库
 * http://www.natureFW.com/
 *
 * @author
 * 金洋（金色海洋jyk）
 * 
 * @copyright
 * Copyright (C) 2005-2013 金洋.
 *
 * Licensed under a GNU Lesser General Public License.
 * http://creativecommons.org/licenses/LGPL/2.1/
 *
 * 自然框架之登录用户类库 is free software. You are allowed to download, modify and distribute 
 * the source code in accordance with LGPL 2.1 license, however if you want to use 
 * 自然框架之登录用户类库 on your site or include it in your commercial software, you must  be registered.
 * http://www.natureFW.com/registered
 */

/* ***********************************************
 * author :  金洋（金色海洋jyk）
 * email  :  jyk0011@live.cn  
 * function: 登录用户类库，定义登录人员的一些常用信息。
 * history:  created by 金洋 
 * **********************************************
 */

using System.Collections.Generic;
using System.Text;
using System.Web;
using Nature.Data;
using Nature.DebugWatch;
using Nature.MetaData.Entity.WebPage;
using Nature.MetaData.ManagerMeta;

namespace Nature.User
{
    /// <summary>
    /// 记录用户的登录信息，用户ID、人员ID，角色，FunctionIDs等。
    /// </summary>
    public class BaseUserPermission
    {
        #region 属性

        internal BaseUserInfo BaseUser { get; set; }

 
        #region 用户的角色ID集合，数组的形式
        /// <summary>
        /// 用户的角色ID集合，数组的形式
        /// </summary>
        public string[] RoleID { get; set; }

        #endregion

        #region 用户的角色ID ，字符串1,2,3的形式
        /// <summary>
        /// 用户的角色ID ，字符串1,2,3的形式，半角逗号分隔
        /// </summary>
        public string RoleIDs { get; set; }

        #endregion

        #region 角色的版本
        /// <summary>
        /// 角色的版本，当角色有变化的时候，依据这个标识来重新加载角色相关的信息
        /// </summary>
        public string RoleVersion { get; set; }

        #endregion

        #region 当前用户可以访问的节点ID集合，字符串的形式。
        private string _moduleIDs = "";
        /// <summary>
        /// 当前用户可以访问的节点ID集合，字符串的形式，半角逗号分隔。
        /// </summary>
        public string ModuleIDs
        {
            set { _moduleIDs = value; }
            get { return _moduleIDs; }
        }
        #endregion

        #endregion

        #region 函数
        #region 当前用户可以使用指定模块的功能按钮
        /// <summary>
        /// 当前用户可以使用指定模块的功能按钮
        /// </summary>
        /// <param name="moduleID">功能节点ID</param>
        /// <param name="dal">元数据的实例</param>
        /// <returns>返回用户可以访问的功能按钮ID的集合，半角逗号区分</returns>
        public string GetUserButtonID(int moduleID, DataAccessLibrary dal)
        {
            //判断是否已经分配了角色
            if (RoleIDs.Length == 0)
            {
                //没有分配角色
                return "0";
            }

            //获取用户的角色可以使用的功能节点ID
            //string sql = "select ButtonIDs from Role_RoleButtonPV where RoleID in (" + RoleIDs + ") and ModuleID = " + functionID;
            const string sql = "select ButtonIDs,PVIDs from Role_RoleButtonPV where RoleID in ({0}) and ModuleID = {1}";

            string[] buttonIDs = dal.ExecuteStringsByColumns(string.Format(sql, RoleIDs, moduleID));
            var sb = new StringBuilder(buttonIDs.Length * 12);
            foreach (string a in buttonIDs)
            {
                sb.Append(a);
                sb.Append(",");
            }

            if (sb.Length >1)
                sb = sb.Remove(sb.Length -1,0);

            return sb.ToString().Trim(',');
        }
        #endregion

        #region 当前用户的列表的过滤方案，需要传递功能节点

        /// <summary>
        /// 传入节点ID，返回当前登录人访问该节点需要设置的查询条件
        /// </summary>
        /// <param name="pvid">页面视图ID</param>
        /// <param name="dal">数据访问函数库的实例</param>
        /// <returns>返回查询条件</returns>
        public string GetResourceListCastSQL(int pvid, DataAccessLibrary dal)
        {
            //获取过滤条件
            //string sql = "select [SQL] from V_Nature_User_GetListCase where UserID = " + UserID + " and FunctionID= " + functionID;
            const string sql = "select [SQL] from V_Role_List_UserPVFilter where UserID = {0} and PVID= {1} ";
            string listCase = dal.ExecuteString(string.Format(sql, BaseUser.UserID, pvid));

            if (listCase == null)
            {
                //没有设置列表过滤方案
                return "";
            }

            if (listCase.Length > 0)
            {
                var sb = new StringBuilder(listCase.Length*2);
                sb.Append(listCase.ToLower());
                //sb = sb.Replace("{personid}", PersonID);
                sb = sb.Replace("{userid}", BaseUser.UserID);

                if (BaseUser.DepartmentID.Length == 0)
                    //没有部门
                    sb = sb.Replace("{deptid}", "0");
                else if (BaseUser.DepartmentID.Length == 1)
                    //只有一个部门
                    sb = sb.Replace("{deptid}", BaseUser.DepartmentID[0]);
                else
                    //有多个部门，暂时没有处理
                    sb = sb.Replace("{deptid}", BaseUser.DepartmentID[0]);

                return sb.ToString();
            }
            return "";

        }

        #endregion

        #region 获取当前用户对于指定的节点可以访问的字段
        /// <summary>
        /// 获取当前用户对于指定的节点可以访问的字段
        /// 返回当前用户指定的节点可以访问的字段ID的集合，“1,2,3”的形式
        /// </summary>
        /// <param name="pageViewID">页面视图的ID</param>
        /// <param name="dalMetadata">访问元数据的实例</param>
        /// <returns>当前用户，指定节点，可以访问的字段ID的集合，“1,2,3”的形式</returns>
        public string GetUserColumnIDs(int pageViewID, DataAccessLibrary dalMetadata)
        {
            if (RoleIDs.Length == 0)
            { 
                //没有设置角色
                return "";
            }

            const string sql = "select ColumnIDs from Role_RoleColumn where RoleID in ({0}) and PVID={1}  ";
            string tmp = dalMetadata.ExecuteString(string.Format(sql, RoleIDs, pageViewID)) ?? "";

            return tmp;

        }
        #endregion

        #region 验证当前用户是否可以访问指定的功能模块
        /// <summary>
        /// 验证当前用户是否可以访问指定的功能模块
        /// </summary>
        /// <param name="moduleID">要验证的模块ID</param>
        public void CheckModuleID(string moduleID)
        { 
            //超级管理员不做验证
            if (BaseUser.UserID == "1")
                return;

            //判断当前用户是否有权限访问该网页，
            string tmpModuleIDs = "," + ModuleIDs + ",";

            moduleID = "," + moduleID + ",";

            if (!tmpModuleIDs.Contains(moduleID))
            {
                //没有权限
                HttpContext.Current.Response.Redirect("~/noPermission.aspx");
                HttpContext.Current.Response.End();
            }
           
        }
        #endregion

        #region 验证当前用户是否可以使用指定的功能模块
        /// <summary>
        /// 验证当前用户是否可以访问指定的功能模块
        /// </summary>
        /// <param name="moduleID">要验证的模块ID</param>
        public bool CanUseModuleID(string moduleID)
        {
            //超级管理员不做验证
            if (BaseUser.UserID == "1")
                return true ;

            //判断当前用户是否有权限访问该网页，
            string tmpModuleIDs = "," + ModuleIDs + ",";

            moduleID = "," + moduleID + ",";

            if (!tmpModuleIDs.Contains(moduleID))
            {
                //没有权限
                return false;
            }
            return true;

        }
        #endregion

        #region 验证当前用户是否可以使用指定的按钮打开的页面
        /// <summary>
        /// 验证当前用户是否可以使用指定的按钮打开的页面
        /// </summary>
        /// <param name="moduleID">要验证的节点</param>
        /// <param name="buttonID">要验证的按钮ID</param>
        /// <param name="dal">元数据的实例</param>
        public void CheckButtonID(int moduleID, string buttonID, DataAccessLibrary dal)
        {
            //超级管理员不做验证
            if (BaseUser.UserID == "1")
                return;

            //判断当前用户是否可以使用指定的按钮打开的页面，
            //获取当前用户可以使用的按钮
            string buttonIDs = GetUserButtonID(moduleID, dal);

            buttonIDs = "," + buttonIDs + ",";
            buttonID = "," + buttonID + ",";
            if (!buttonIDs.Contains(buttonID ))
            {
                //没有权限
                HttpContext.Current.Response.Redirect("~/noPermission.aspx");
                HttpContext.Current.Response.End();
            }
           
        }
        #endregion

        #region 验证当前用户是否可以修改指定的记录
        /// <summary>
        /// 验证当前用户是否可以修改指定的记录
        /// </summary>
        /// <param name="moduleID">模块ID</param>
        /// <param name="pageViewID">视图ID</param>
        /// <param name="dataID">要判断的记录ID</param>
        /// <param name="dalMetadata">访问元数据的实例</param>
        /// <param name="debugInfoList">子步骤的列表</param>
        /// <returns></returns>
        public string CheckCanUpdate(int moduleID, int pageViewID, string dataID, DataAccessLibrary dalMetadata, IList<NatureDebugInfo> debugInfoList)
        {
            if (dataID == "-2")
                //添加记录，不验证
                return "";

            //获取数据列表、主键
            //获取过滤条件
            //拼接SQL语句验证

            //string sql = "select TableNameList,PKColumn from  where FunctionID=" + functionID;

            //获取数据列表的名称
            //string[] info = dal_Metadata.ExecuteStringsBySingleRow(sql);

            var dal = new DalCollection {DalMetadata = dalMetadata};
            var pageView = new ManagerPageViewMeta { DalCollection = dal, PageViewID = pageViewID,ModuleID = moduleID  };
            PageViewMeta funInfo = pageView.GetPageViewMeta(debugInfoList);

            if (funInfo == null)
            {
                return "在元数据里没有找到数据列表和主键名！";
            }

            //获取过滤条件
            string query = GetResourceListCastSQL(moduleID, dalMetadata); 

            if (query.Length > 0)
            {
                //拼接SQL语句验证
                query = query.Replace("{userid}", BaseUser.UserID);
                query = query.Replace("{personid}", BaseUser.PersonID);

                //string sql = "select top 1 1 from " + funInfo.PagerInfo.TableNameList + " where " + query + " and " + funInfo.PagerInfo.PKColumn + " = '" + dataID + "'";
                string sql = "select top 1 1 from {0} where {1} and {2} = '{3}'";
                sql = string.Format(sql, funInfo.PageTurnMeta.TableNameList, query, funInfo.PageTurnMeta.PKColumn, dataID);
                if (!dalMetadata.ExecuteExists(sql))
                {
                    return "您没有访问该记录的权限！";
                }
            }

            return "";
        }
        #endregion

        #endregion
    }

}
