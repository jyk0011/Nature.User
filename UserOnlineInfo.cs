using System;
using System.Collections.Generic;
using System.Text;
using Nature.Data;

namespace Nature.User
{
    /// <summary>
    /// 在线用户的相关信息
    /// </summary>
    /// user:jyk
    /// time:2013/2/2 15:58
    public class UserOnlineInfo
    {
        #region 属性

        #region dal
        private DataAccessLibrary _dal;
        /// <summary>
        /// 数据访问类库的实例
        /// </summary>
        public DataAccessLibrary DAL
        {
            get { return _dal; }
            set { _dal = value; }
        }
        #endregion

        /// <summary>
        /// 用户的基本信息
        /// </summary>
        /// user:jyk
        /// time:2013/2/2 15:56
        public BaseUserInfo BaseUser { get;set; }

        /// <summary>
        /// 用户的相关权限
        /// </summary>
        /// user:jyk
        /// time:2013/2/2 15:56
        public BaseUserPermission UserPermission { get; set; }

        #endregion

    }
}
