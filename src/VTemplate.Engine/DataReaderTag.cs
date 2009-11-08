﻿/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * DateTime		:  2009-8-28 10:53:23
 * Description	:  DataReaderTag
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Data.Common;
using System.Data;

namespace VTemplate.Engine
{
    /// <summary>
    /// DataReader标签.如:&lt;vt:datareader var="members" connection="sitedb"  commandtext="select * from [member]"&gt;...&lt;/vt:foreach&gt;
    /// </summary>
    public class DataReaderTag : Tag
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerTemplate"></param>
        internal DataReaderTag(Template ownerTemplate)
            : base(ownerTemplate)
        {
            this.Parameters = new ElementCollection<VariableExpression>();
        }

        #region 重写Tag的方法
        /// <summary>
        /// 返回标签的名称
        /// </summary>
        public override string TagName
        {
            get { return "datareader"; }
        }

        /// <summary>
        /// 返回此标签是否是单一标签.即是不需要配对的结束标签
        /// </summary>
        internal override bool IsSingleTag
        {
            get { return false; }
        }
        #endregion

        #region 属性定义
        /// <summary>
        /// 数据源名称.此名称必须已在项目配置文件(如:web.config)里的connectionStrings节点里定义.
        /// </summary>
        /// <remarks></remarks>
        public IExpression Connection { get; protected set; }

        /// <summary>
        /// 数据查询命令语句.
        /// </summary>
        public IExpression CommandText { get; protected set; }

        /// <summary>
        /// 要获取的行号.从0开始计算
        /// </summary>
        public IExpression RowIndex { get; protected set; }

        /// <summary>
        /// 存储表达式结果的变量
        /// </summary>
        public VariableIdentity Variable { get; protected set; }

        /// <summary>
        /// 查询命令中使用的变量参数列表,各参数在查询命令语句中用"@p0","@p1"之类的代替
        /// </summary>
        public virtual ElementCollection<VariableExpression> Parameters { get; protected set; }
        #endregion

        #region 添加标签属性时的触发函数.用于设置自身的某些属性值
        /// <summary>
        /// 添加标签属性时的触发函数.用于设置自身的某些属性值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="item"></param>
        protected override void OnAddingAttribute(string name, Attribute item)
        {
            switch (name)
            {
                case "var":
                    this.Variable = ParserHelper.CreateVariableIdentity(this.OwnerTemplate, item.Value);
                    break;
                case "connection":
                    this.Connection = ParserHelper.CreateExpression(this.OwnerTemplate, item.Value);
                    break;
                case "commandtext":
                    this.CommandText = ParserHelper.CreateExpression(this.OwnerTemplate, item.Value);
                    break;
                case "rowindex":
                    this.RowIndex = ParserHelper.CreateExpression(this.OwnerTemplate, item.Value);
                    break;
                case "parameters":
                    this.Parameters.Add(ParserHelper.CreateVariableExpression(this.OwnerTemplate, item.Value));
                    break;
            }
        }
        #endregion

        #region 开始解析标签数据
        /// <summary>
        /// 开始解析标签数据
        /// </summary>
        /// <param name="ownerTemplate">宿主模版</param>
        /// <param name="container">标签的容器</param>
        /// <param name="tagStack">标签堆栈</param>
        /// <param name="text"></param>
        /// <param name="match"></param>
        /// <param name="isClosedTag">是否闭合标签</param>
        /// <returns>如果需要继续处理EndTag则返回true.否则请返回false</returns>
        internal override bool ProcessBeginTag(Template ownerTemplate, Tag container, Stack<Tag> tagStack, string text, ref Match match, bool isClosedTag)
        {
            if (this.Variable == null) throw new ParserException(string.Format("{0}标签中缺少var属性", this.TagName));
            if (this.Connection == null) throw new ParserException(string.Format("{0}标签中缺少connection属性", this.TagName));
            if (this.CommandText == null) throw new ParserException(string.Format("{0}标签中缺少commandtext属性", this.TagName));

            return base.ProcessBeginTag(ownerTemplate, container, tagStack, text, ref match, isClosedTag);
        }
        #endregion

        #region 克隆当前元素到新的宿主模版
        /// <summary>
        /// 克隆当前元素到新的宿主模版
        /// </summary>
        /// <param name="ownerTemplate"></param>
        /// <returns></returns>
        internal override Element Clone(Template ownerTemplate)
        {
            DataReaderTag tag = new DataReaderTag(ownerTemplate);
            this.CopyTo(tag);
            tag.Connection = this.Connection == null ? null : this.Connection.Clone(ownerTemplate);
            tag.CommandText = this.CommandText == null ? null : this.CommandText.Clone(ownerTemplate);
            tag.RowIndex = this.RowIndex == null ? null : this.RowIndex.Clone(ownerTemplate);
            tag.Variable = this.Variable == null ? null : this.Variable.Clone(ownerTemplate);
            foreach (VariableExpression exp in this.Parameters)
            {
                tag.Parameters.Add((VariableExpression)(exp.Clone(ownerTemplate)));
            }
            return tag;
        }
        #endregion

        #region 呈现本元素的数据
        /// <summary>
        /// 呈现本元素的数据
        /// </summary>
        /// <param name="writer"></param>
        public override void Render(System.IO.TextWriter writer)
        {
            this.Variable.Value = GetDataSource();
            base.Render(writer);
        }
        #endregion

        #region 获取数据源
        /// <summary>
        /// 获取数据源
        /// </summary>
        /// <returns></returns>
        protected virtual object GetDataSource()
        {
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings[this.Connection.GetValue().ToString()];
            if (setting == null) return null;

            DbProviderFactory dbFactory = Utility.CreateDbProviderFactory(setting.ProviderName);
            if (dbFactory == null) return null;

            object result = null;
            using (DbConnection dbConnection = dbFactory.CreateConnection())
            {
                dbConnection.ConnectionString = setting.ConnectionString;
                using (DbCommand dbCommand = dbConnection.CreateCommand())
                {
                    dbCommand.CommandText = this.CommandText.GetValue().ToString();

                    if (this.Parameters.Count > 0)
                    {
                        List<object> expParams = new List<object>();
                        for (int i = 0; i < this.Parameters.Count; i++)
                        {
                            VariableExpression exp = this.Parameters[i];
                            DbParameter dbParameter = dbFactory.CreateParameter();
                            object value = exp.GetValue();
                            dbParameter.ParameterName = "@p" + i.ToString();
                            dbParameter.DbType = Utility.GetObjectDbType(value);
                            dbParameter.Value = value;
                            dbCommand.Parameters.Add(dbParameter);
                        }
                    }

                    using (DbDataAdapter dbAdapter = dbFactory.CreateDataAdapter())
                    {
                        dbAdapter.SelectCommand = dbCommand;
                        DataTable table = new DataTable();
                        dbAdapter.Fill(table);

                        if (this.RowIndex != null)
                        {
                            //只获取其中的某行数据
                            int row = Utility.ConverToInt32(this.RowIndex.GetValue().ToString());
                            if (table.Rows.Count > row)
                            {
                                result = table.Rows[row];
                            }
                        }
                        else
                        {
                            result = table;
                        }
                    }
                }
            }
            return result;
        }
        #endregion
    }
}
