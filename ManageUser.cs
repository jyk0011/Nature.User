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
 * function: 登录用户类库，用户登录、登出、是否登录，和保存用户状态。
 * history:  created by 金洋 
 *           2011.4.18 增加两个标识，UserLoginSign、UserIDKey。从web.config里面取值。
 *           UserLoginSign：区分不同的项目，不同的项目可以有自己的登录状态。
 *           如果不设置，或者设置相同值，则可以实现多点登录
 *           
 *           UserIDKey：把用户ID保存在cookies里，加密的密钥。
 * **********************************************
 */


using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Nature.Common;
using Nature.Data;
using Nature.Data.Part;
using Nature.DebugWatch;

namespace Nature.User
{
    /// <summary>
    /// 管理登录、登出、是否登录，和保存用户状态
    /// </summary>
    public class ManageUser
    {
        #region cookies、session标识
        /// <summary>
        /// 不同的项目使用不同的标识，以便于区分cookies，避免冲突（使用端口区分网站的时候）
        /// </summary>
        private readonly string _userLoginSign;
        #endregion

        #region 加密的密钥
        /// <summary>
        /// 加密cookie信息的密钥。
        /// </summary>
        private readonly string _userIDKey;
        #endregion

        #region 属性

        #region 访问数据库的实例，四个

        /// <summary>
        /// 访问数据库的实例的集合，四个
        /// </summary>
        /// user:jyk
        /// time:2012/9/13 10:52
        public DalCollection Dal { set; get; }

        #endregion

        #region 错误信息
        private string _errorMessage = "";
        /// <summary>
        /// 出错的时候，记录出错的描述
        /// </summary>
        public string ErroeMessage
        {
            get { return _errorMessage; }
        }
        #endregion

        #region 定义实体类 BaseUserInfo UserInfo，存放用户的一些常用信息

        /// <summary>
        /// 存放用户的一些常用信息
        /// </summary>
        public UserOnlineInfo UserInfo { set; get; }

        #endregion

        #endregion

        #region 初始化
        /// <summary>
        /// 初始化，设置session的标识，以区分不同的项目
        /// 设置密钥。
        /// </summary>
        public ManageUser()
        {
            //设置标识
            _userLoginSign = ConfigurationManager.AppSettings["UserLoginSign"];
            //获取密钥
            _userIDKey = ConfigurationManager.AppSettings["UserLoginKey"];

            if (_userLoginSign == null)
                _userLoginSign = "";

            if (_userIDKey == null)
                _userIDKey = "oik9t4gr";

        }
        #endregion
        
        #region 创建用户实例
        /// <summary>
        /// 创建用户实例
        /// </summary>
        /// <param name="userID">用户ID</param>
        /// <returns></returns>
        /// <param name="debugInfoList">子步骤的列表</param>
        public UserOnlineInfo CreateUser(string userID, IList<NatureDebugInfo> debugInfoList)
        {
            if (debugInfoList == null)
                debugInfoList = new List<NatureDebugInfo>();

            if (userID == null)
                return null;

            var debugInfo = new NatureDebugInfo { Title = "[Nature.User.ManageUser.CreateUser]获取用户信息，" };

            #region 查看缓存
            if (HttpContext.Current.Session[_userLoginSign + "sysUserInfo"] != null)
            {
                debugInfo.Title += "有缓存，从session里获取" + userID;
                //Session 没有失效，读取session里的信息
                UserInfo = (UserOnlineInfo)HttpContext.Current.Session[_userLoginSign + "sysUserInfo"];

                NatureDebugInfo debugInfo2;

                //判断session里的用户ID是否过时。
                if (UserInfo.BaseUser.UserID != userID )
                {
                    debugInfo2 = new NatureDebugInfo { Title = "用户ID过时，重新生成实例UserOnlineInfo：" + userID };

                    debugInfo.Remark += "用户ID过时，重新生成实例UserOnlineInfo：" + userID + "。<br/>";
                    UserInfo = new UserOnlineInfo
                    {
                        BaseUser = CreateUserInfoFormDataBase(userID, debugInfo2.DetailList),
                        UserPermission = CreateUserPermissionFormDataBase(userID, debugInfo2.DetailList)
                    };
                    debugInfo2.Stop();
                    debugInfo.DetailList.Add(debugInfo2);
                }

                //判断角色的版本是否过时。
                string tmpVersion = Dal.DalRole.ExecuteString("select Version from Role_RoleVersion");
                if (tmpVersion != UserInfo.UserPermission.RoleVersion)
                {
                    debugInfo2 = new NatureDebugInfo { Title = "用户的角色有变化，重新加载角色：" + userID };

                    debugInfo.Remark += "用户的角色有变化，重新加载角色：" + userID + "。<br/>";
                    //角色有变化，重新加载
                    UserInfo.UserPermission = CreateUserPermissionFormDataBase(userID, debugInfo2.DetailList);

                    debugInfo2.Stop();
                    debugInfo.DetailList.Add(debugInfo2);
                }
            }
            else
            {
                debugInfo.Title += "没有缓存，根据用户ID重新生成实例。" + userID;
           
                //Session 丢失，根据用户ID重新生成
                UserInfo = new UserOnlineInfo
                {
                    BaseUser = CreateUserInfoFormDataBase(userID, debugInfo.DetailList),
                    UserPermission = CreateUserPermissionFormDataBase(userID, debugInfo.DetailList)
                };
                  
            }
            UserInfo.UserPermission.BaseUser  = UserInfo.BaseUser;
            #endregion

            ManagerParameter parm = Dal.DalUser.ManagerParameter;
            
            #region 修改Person_User_Info，记录登录次数、登录IP、登录时间、最后访问时间

            string userIp = System.Web.HttpContext.Current.Request.UserHostAddress;
            string dtLoginTime = DateTime.Now.Date.ToString("yyyy-MM-dd");
            const string sql = "update Person_User_Info set [LogonCount] = [LogonCount] + 1,[LogonIP]=@LogonIP ,[LogonTime]=@LogonTime,[LogonLastTime]=@LogonLastTime where UserID=@UserID ";
            parm.ClearParameter();
            parm.AddNewInParameter("LogonIP", userIp, 16);
            parm.AddNewInParameter("LogonTime", dtLoginTime, 40);
            parm.AddNewInParameter("LogonLastTime", dtLoginTime, 40);
            parm.AddNewInParameter("UserID", userID);

            Dal.DalUser.ExecuteNonQuery(sql);
            if (Dal.DalUser.ErrorMessage.Length > 0)
            {
                _errorMessage = "访问数据库出现异常，请与管理员联系！";
                throw new Exception("访问数据库出现异常！\n" + Dal.DalUser.ErrorMessage);
            }
            #endregion

            #region 添加登录的历史记录
            parm.ClearParameter();
            parm.AddNewInParameter("UserID", userID, 16);
            //parm.AddNewInParameter("UserCode", userCode, 16);
            parm.AddNewInParameter("UserName", "", 16);
            parm.AddNewInParameter("LogonIP", userIp, 16);
            parm.AddNewInParameter("LogonTime", dtLoginTime, 16);
            //                             Person_User_LogonLog
            Dal.DalUser.ModifyData.InsertData("Person_User_LogonLog");
            #endregion

            HttpContext.Current.Session[_userLoginSign + "sysUserInfo"] = UserInfo;

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);

            //保存用户ID
            //sso端的标识包
            string source = string.Format("{0}_{1}_{2}", userID, userIp, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            //加密
            string miwen = DesUrl.Encrypt(source, _userIDKey);
            HttpContext.Current.Response.Cookies["saleUserID___"].Value = miwen;

            return UserInfo ;
        }
        #endregion

        #region 修改密码
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pswOld">原来的密码</param>
        /// <param name="pswNew1">新密码</param>
        /// <param name="pswNew2">验证密码</param>
        /// <returns></returns>
        public string UpdatePassword(string userId, string pswOld, string pswNew1, string pswNew2)
        {
            //验证原来的密码是否正确
            if (string.IsNullOrEmpty(userId))
            {
                return "\"msg\":\"没有用户名\"";
            }

            //验证密码
            pswOld = pswOld.Replace("'", "''");
            pswOld = Functions.ToMD5(pswOld);

            pswNew1 = pswNew1.Replace("'", "''");
            pswNew1 = Functions.ToMD5(pswNew1);

            pswNew2 = pswNew2.Replace("'", "''");
            pswNew2 = Functions.ToMD5(pswNew2);

            const string sql = "SELECT TOP 1 UserID from Person_User_Info where UserID={0} and LoginPsw ='{1}'";

            bool isTrue = Dal.DalUser.ExecuteExists(string.Format(sql, userId, pswOld));
            if (Dal.DalUser.ErrorMessage.Length > 2)
            {
                //debugInfo.Remark = "到数据库验证登录账户和密码，出现异常！";
                return "\"msg\":\"" + Dal.DalUser.ErrorMessage + "\"";
            }

            if (!isTrue)
            {
                //密码不正确              
                return "密码不正确";
            }
            else
            {
                //密码正确
                //验证两次密码是否一致
                if (pswNew1 != pswNew2)
                {
                    //
                    return "两次密码是否一致";
                }

                //修改密码
                const string sql2 = "update Person_User_Info set  LoginPsw ='{0}' where UserID={1}";

                Dal.DalUser.ExecuteNonQuery(string.Format(sql2, pswNew1, userId));

                if (Dal.DalUser.ErrorMessage.Length > 2)
                {
                    //debugInfo.Remark = "到数据库验证登录账户和密码，出现异常！";
                    return "\"msg\":\"" + Dal.DalUser.ErrorMessage + "\"";
                }

            }
           
            return "";

        }
        #endregion

        #region 临时修改登录人的所在部门，实现“变身”的部分功能

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userInfo">当前登录人的信息</param>
        /// <param name="newDepartmentId">要变身的部门</param>
        /// <param name="newKind">要变身的部门的类型</param>
        /// <param name="debugInfoList">日志信息</param>
        /// <returns></returns>
        public string ChangeDepartmentId(BaseUserInfo userInfo, string newDepartmentId,PersonKind newKind, IList<NatureDebugInfo> debugInfoList)
        {
            var debugInfo = new NatureDebugInfo { Title = "临时变换登录人的所在部门" };

            //获取要变换的部门名称
            string tableName = "";
            string colName = "";

            switch (newKind)
            {
                case PersonKind.LeLian:
                    tableName = "le_LeLian_Department";
                    colName = "DeptName";
                    break;
                case PersonKind.Supplier:
                    tableName = "le_Supplier";
                    colName = "SupplierName";
                    break;
                case PersonKind.Cafeteria:
                    tableName = "le_Cafeteria";
                    colName = "CafeteriaName";
                    break;

            }
            string sql = "select " + colName + " from " + tableName + " where DepartmentID = " + newDepartmentId;
            userInfo.DepartmentName = Dal.DalCustomer.ExecuteString(sql);

            if (Dal.DalUser.ErrorMessage.Length > 0)
            {
                debugInfo.Remark = "访问数据库出现异常！<br/>" + Dal.DalUser.ErrorMessage;
                debugInfo.Stop();
                debugInfoList.Add(debugInfo);

                throw new Exception("访问数据库出现异常！\n" + Dal.DalUser.ErrorMessage);
            }

            userInfo.DepartmentID[0] = newDepartmentId;

            debugInfo.Remark = "新名称：" + userInfo.DepartmentName;

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);

            return "";
        }
        #endregion


        #region 获取用户ID，并且判断是否登录
        /// <summary>
        /// 获取用户ID，并且判断是否登录
        /// </summary>
        /// <returns></returns>
        public string GetUserId()
        {
            string userId = "";

            var a = HttpContext.Current.Request.Cookies["saleUserID___"];
            if (a == null)
            {
                //没有登录
            }
            else
            {
                string tmp = a.Value;
                tmp = DesUrl.Decrypt(tmp, _userIDKey);
                userId = tmp.Split('_')[0];

            }


            return userId;
        }
        #endregion

        #region 内部函数

        #region 从数据库里面提取用户的一些信息，填充实体类
        /// <summary>
        /// 从数据库里面提取用户的一些信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="debugInfoList">子步骤的列表</param>
        /// <returns></returns>
        private BaseUserInfo CreateUserInfoFormDataBase(string userId, IList<NatureDebugInfo> debugInfoList)
        {
            var debugInfo = new NatureDebugInfo { Title = "实例化 BaseUserInfo" + userId };

            #region 实例化
            var userInfo = new BaseUserInfo {UserID = userId};
            debugInfo.Stop();
            debugInfoList.Add(debugInfo);
            #endregion

            debugInfo = new NatureDebugInfo { Title = "获取人员ID、登录账号" };

            #region 获取           人员ID、登录账号、所在部门、 员工类型
            string sql = "select UserID,UserCode,DepartmentID,UserKind from Person_User_Info where UserID = " + userId;
            string[] tmp = Dal.DalUser.ExecuteStringsBySingleRow(sql);
            if (tmp == null)
            {
                debugInfo.Remark = "没有找到";
                debugInfo.Stop();
                debugInfoList.Add(debugInfo);

                return null;
            }

            userInfo.PersonID = userId;
            userInfo.UserCode = tmp[1];
           
            userInfo.DepartmentID = new[] { tmp[2] };
            userInfo.OldDepartmentId = tmp[2];
            userInfo.PersonKind = (PersonKind)Int32.Parse(tmp[3]);
             
            //获取所在部门的名称
            string tableName = "";
            string colName = "";

            switch (userInfo.PersonKind)
            {
                case PersonKind.LeLian:
                    tableName = "le_LeLian_Department";
                    colName = "DeptName";
                    break;
                case PersonKind.Supplier:
                    tableName = "le_Supplier";
                    colName = "SupplierName";
                    break;
                case PersonKind.Cafeteria:
                    tableName = "le_Cafeteria";
                    colName = "CafeteriaName";
                    break;

            }
            sql = "select " + colName + " from " + tableName + " where DepartmentID = " + userInfo.DepartmentID[0];
            userInfo.DepartmentName = Dal.DalCustomer.ExecuteString(sql);

            if (Dal.DalUser.ErrorMessage.Length > 0)
            {
                debugInfo.Remark = "访问数据库出现异常！<br/>" + Dal.DalUser.ErrorMessage;
                debugInfo.Stop();
                debugInfoList.Add(debugInfo);

                throw new Exception("访问数据库出现异常！\n" + Dal.DalUser.ErrorMessage);
            }

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);
            #endregion

            debugInfo = new NatureDebugInfo { Title = "获取人员姓名" };

            #region 获取人员姓名

            switch (userInfo.PersonKind)
            {
                case PersonKind.LeLian:
                    tableName = "le_LeLian_Employee";
                    colName = "EmployeeName";
                    break;
                case PersonKind.Supplier:
                    tableName = "le_Supplier_Employee";
                    colName = "EmployeeName";
                    break;
                case PersonKind.Cafeteria:
                    tableName = "le_Cafeteria_Employee";
                    colName = "EmployeeName";
                    break;

            }
            sql = "select " + colName + " from " + tableName + " where [UserId] = " + userInfo.UserID;
            userInfo.PersonName = Dal.DalCustomer.ExecuteString(sql);
            if (Dal.DalUser.ErrorMessage.Length > 0)
            {
                debugInfo.Remark = "访问数据库出现异常！<br/>" + Dal.DalUser.ErrorMessage;
                debugInfo.Stop();
                debugInfoList.Add(debugInfo);

                throw new Exception("访问数据库出现异常！\n" + Dal.DalUser.ErrorMessage);
            }
            debugInfo.Stop();
            debugInfoList.Add(debugInfo);
            #endregion

            return userInfo;

        }
        #endregion

        #region 从数据库里面提取用户的一些信息，填充实体类
        /// <summary>
        /// 从数据库里面提取用户的一些信息
        /// </summary>
        /// <param name="userID">用户ID</param>
        /// <param name="debugInfoList">子步骤的列表</param>
        /// <returns></returns>
        private BaseUserPermission CreateUserPermissionFormDataBase(string userID, IList<NatureDebugInfo> debugInfoList)
        {
            var debugInfo = new NatureDebugInfo { Title = "实例化 BaseUserPermission" + userID };

            var userPermission = new BaseUserPermission ();
            
            debugInfo.Stop();
            debugInfoList.Add(debugInfo);

            debugInfo = new NatureDebugInfo { Title = "获取用户拥有的角色ID d" };

            #region 获取用户拥有的角色ID
            string sql = "";

            //获取用户拥有的角色ID
            sql = "select RoleID from Role_RoleUser where UserID = " + userID;
            userPermission.RoleID = Dal.DalMetadata.ExecuteStringsByColumns(sql);
            if (userPermission.RoleID == null)
            {
                debugInfo.Remark = "没有找到用户拥有的角色";
                debugInfo.Stop();
                debugInfoList.Add(debugInfo);

                return null;
            }

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);
            #endregion

            debugInfo = new NatureDebugInfo { Title = "设置字符串形式的角色ID c" };

            #region 设置字符串形式的角色ID
            string tmpRoleID = "";
            foreach (string a in userPermission.RoleID)
            {
                tmpRoleID += a + ",";
            }
            tmpRoleID = tmpRoleID.TrimEnd(',');
            userPermission.RoleIDs = tmpRoleID;

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);
            #endregion

            debugInfo = new NatureDebugInfo { Title = "设置用户可以访问的功能节点 f" };

            #region 设置用户可以访问的功能节点
            if (userPermission.RoleID.Length == 0)
            {
                //没有分配角色
                debugInfo.Remark = "没有分配角色";
            }
            else
            {
                //获取可以访问的节点ID
                LoadUserModuleID(userPermission, debugInfo.DetailList);
            }
            #endregion

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);


            return userPermission;

        }
        #endregion

        #endregion

        #region 加载用户可以访问的功能节点
        /// <summary>
        /// 加载用户可以访问的功能节点
        /// </summary>
        /// <param name="userInfo">用户的登录信息的实例</param>
        /// <param name="debugInfoList">子步骤的列表</param>
        /// <returns></returns>
        private string LoadUserModuleID(BaseUserPermission userInfo, IList<NatureDebugInfo> debugInfoList)
        {
            var debugInfo = new NatureDebugInfo { Title = "从数据库里获取用户可以访问的功能节点" };

            string tmpModuleIDs = "";
            string sql  ;

            if (userInfo.RoleIDs.Length > 0)
            {
                //设置了角色
                sql = "SELECT ModuleID FROM Role_RoleModule WHERE RoleID IN ({0})";
                string[] arrModuleIDs = Dal.DalRole.ExecuteStringsByColumns(string.Format(sql, userInfo.RoleIDs));

                foreach(string a in arrModuleIDs )
                {
                    tmpModuleIDs += a + ",";
                }
            }

            tmpModuleIDs = tmpModuleIDs.TrimEnd(',');
            userInfo.ModuleIDs = tmpModuleIDs;

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);

            debugInfo = new NatureDebugInfo { Title = "从数据库里获取角色的版本" };

            //获取角色的版本
            sql = "select Version from Role_RoleVersion ";
            userInfo.RoleVersion = Dal.DalRole.ExecuteString(sql);

            debugInfo.Stop();
            debugInfoList.Add(debugInfo);

            return tmpModuleIDs;
        }
        #endregion


    }
}
