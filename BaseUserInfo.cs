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


namespace Nature.User
{
    /// <summary>
    /// 员工类型
    /// </summary>
    public enum PersonKind
    {
        /// <summary>
        /// 乐莲员工
        /// </summary>
        LeLian = 1,

        /// <summary>
        /// 供应商员工
        /// </summary>
        Supplier = 2,

        /// <summary>
        /// 餐厅员工
        /// </summary>
        Cafeteria = 3

    }

    /// <summary>
    /// 记录用户的登录信息，用户ID、人员ID， 所在部门等
    /// </summary>
    public class BaseUserInfo
    {
        #region 属性
         
        #region 用户账号的ID
        private string _userID = "";
        /// <summary>
        /// 用户账号的ID，一个人可以有多个账号。每个账号都可以有不同的角色、权限
        /// </summary>
        public string UserID
        {
            set{ _userID = value;}
            get{ return _userID; }
        }
        #endregion

        #region 用户账号
        private string _userCode = "";
        /// <summary>
        /// 用户账号，一个人可以有多个账号。每个账号都可以有不同的角色、权限
        /// </summary>
        public string UserCode
        {
            set { _userCode = value; }
            get { return _userCode; }
        }
        #endregion

        #region 人员ID
        private string _personID = "";
        /// <summary>
        /// 人员ID
        /// </summary>
        public string PersonID
        {
            set{  _personID = value;}
            get{ return _personID; }
        }
        #endregion

        #region 姓名
        private string _personName = "";
        /// <summary>
        /// 姓名
        /// </summary>
        public string PersonName
        {
            set { _personName = value; }
            get { return _personName; }
        }
        #endregion

        #region 员工类型
        /// <summary>
        /// 员工类型
        /// </summary>
        public PersonKind PersonKind{get; set; }
        #endregion

        #region 员工工号
        /// <summary>
        /// 员工类型
        /// </summary>
        public string PersonNumber { get; set; }
        #endregion

        #region 部门ID集合
        /// <summary>
        /// 部门ID集合，数组形式。一个人可以在多个部门
        /// </summary>
        public string[] DepartmentID { get; set; }

        #endregion

        #region 登录时的所在部门
        /// <summary>
        /// 部门ID集合，数组形式。一个人可以在多个部门
        /// </summary>
        public string OldDepartmentId { get; set; }

        #endregion

        #region 人员所在部门名称
        private string _departmentName = "";
        /// <summary>
        /// 人员所在部门的名称
        /// </summary>
        public string DepartmentName
        {
            set { _departmentName = value; }
            get { return _departmentName; }
        }
        #endregion


       
        #endregion

       
    }

}
