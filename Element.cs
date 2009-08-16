﻿/* ***********************************************
 * Author		:  kingthy
 * Email		:  kingthy@gmail.com
 * Description	:  Element
 *
 * ***********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace VTemplate.Engine
{
    /// <summary>
    /// 元素
    /// </summary>
    [Serializable]
    public abstract class Element
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerTemplate">宿主模版</param>
        protected Element(Template ownerTemplate)
        {
            this.OwnerTemplate = ownerTemplate;
        }

        /// <summary>
        /// 此元素的宿主模版
        /// </summary>
        public Template OwnerTemplate { get; protected set; }
        /// <summary>
        /// 呈现本元素的数据
        /// </summary>
        /// <param name="writer"></param>
        public abstract void Render(TextWriter writer);
        /// <summary>
        /// 克隆元素
        /// </summary>
        /// <param name="ownerTemplate"></param>
        /// <returns></returns>
        internal abstract Element Clone(Template ownerTemplate);
    }
}