using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    /// status	int	其中，status可以为：
    /// a)	挂起(2)
    /// b)	恢复(1)
    /// c)	完成(5)（自动完成当前所有的半自动步骤）
    /// d)	中止(3)
    /// e)	NULL(0)，不改变状态，仅修改表单数据/变量。
    public enum InstanceStates
    {
        Offline = 2,
        Resume = 1,
        // Complete = 5,
        Terminate = 3,
        Null = 0
    }

    /// <summary>
    /// 要更改的内容：
    /// 状态，数据，超时
    /// </summary>
    public enum AlterScopes
    { 
        Status = 0,
        Data = 1,
        Expire = 2
    }


    /// <summary>
    /// Used to parse data when AlterScopes is "Status"
    /// by marstone, 2012/05/10
    /// </summary>
    public class AlterState
    {
        public InstanceStates Status { get; set; }
        public string Reason { get; set; }
    }

}
