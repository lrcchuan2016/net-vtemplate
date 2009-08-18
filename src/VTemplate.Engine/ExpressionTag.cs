﻿/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  ExpressionTag
 *
 * ***********************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VTemplate.Engine
{
    /// <summary>
    /// 表达式标签.如: &lt;vt:expression var="totalAge" args="user1.age" args="user2.age" expression="{0}+{1}" /&gt;
    /// </summary>
    [Serializable]
    public class ExpressionTag : Tag
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerTemplate"></param>
        internal ExpressionTag(Template ownerTemplate)
            : base(ownerTemplate)
        {
            //注册添加属性时触发事件.用于设置自身的某些属性值
            this.ExpArgs = new ElementCollection<VariableExpression>();
        }

        #region 重写Tag的方法
        /// <summary>
        /// 返回标签的名称
        /// </summary>
        public override string TagName
        {
            get { return "expression"; }
        }
        /// <summary>
        /// 返回标签的结束标签名称.
        /// </summary>
        public override string EndTagName
        {
            get
            {
                return "expression";
            }
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
        /// 参与表达式运算的变量参数列表
        /// </summary>
        public virtual ElementCollection<VariableExpression> ExpArgs { get; protected set; }

        /// <summary>
        /// 表达式.
        /// </summary>
        /// <remarks>表达式中可用"{0}","{1}"..之类的标记符表示变量参数的值</remarks>
        public string Expression { get; protected set; }

        /// <summary>
        /// 存储表达式结果的变量
        /// </summary>
        public Variable Variable { get; protected set; }
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
                case "args":
                    this.ExpArgs.Add(ParserHelper.CreateVariableExpression(this.OwnerTemplate, item.Value));
                    break;
                case "expression":
                    this.Expression = item.Value;
                    break;
                case "var":
                    this.Variable = Utility.GetVariableOrAddNew(this.OwnerTemplate, item.Value);
                    break;
            }
        }
        #endregion

        #region 呈现本元素的数据
        /// <summary>
        /// 呈现本元素的数据
        /// </summary>
        /// <param name="writer"></param>
        public override void Render(System.IO.TextWriter writer)
        {
            //计算表达式的值
            object value = null;
            List<object> expParams = new List<object>();
            foreach (VariableExpression exp in this.ExpArgs)
            {
                expParams.Add(exp.GetValue());
            }
            try
            {
                value = Evaluator.ExpressionEvaluator.Eval(string.Format(this.Expression, expParams.ToArray()));
            }
            catch
            {
                value = null;
            }
            this.Variable.Value = value;
            base.Render(writer);
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
            if (string.IsNullOrEmpty(this.Expression)) throw new ParserException(string.Format("{0}标签中缺少expression属性", this.TagName));

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
            ExpressionTag tag = new ExpressionTag(ownerTemplate);
            tag.Id = this.Id;
            tag.Name = this.Name;
            tag.Attributes = this.Attributes;
            tag.Expression = this.Expression;
            tag.Variable = this.Variable == null ? null : Utility.GetVariableOrAddNew(ownerTemplate, this.Variable.Name);
            foreach (VariableExpression exp in this.ExpArgs)
            {
                tag.ExpArgs.Add((VariableExpression)(exp.Clone(ownerTemplate)));
            }
            foreach (Element element in this.InnerElements)
            {
                tag.AppendChild(element.Clone(ownerTemplate));
            }
            return tag;
        }
        #endregion
    }
}