using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class InfoPlusTimer
    {
        
        /// <summary>
        /// 时间间隔，有3种情况：
        ///  0：不作处理，忽略此事
        /// -1：终止当前流程
        /// 正整数：延时的秒数
        /// </summary>
        public int Interval { get; set; }


        /// <summary>
        /// 自动执行的action的code，注意：
        /// * 仅当STEP_EXPIRING有效
        /// * interval为-1时将忽略action，直接终止
        /// * 只能使用系统用户办理（执行），此步骤必须配置为"系统用户可办"
        /// * 执行时几时发生的，如执行不成功依然可以设置interval忽略或者延时
        /// </summary>
        public string Action { get; set; }
    }
}
