using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{
    public class FormInstance
    {
        public string InstanceId { get; set; }			// 表单实例Id
        public string InstanceName { get; set; }		// 表单实例标题
        public string BusinessId { get; set; }			// 启动实例时的businessId
        public long EntryId { get; set; }			    // 流水号
        public InfoPlusUser Creator { get; set; }		// 创建人/所有者[1]，创建此表单实例的用户
        public InfoPlusUser NextUser { get; set; }	    // 第一步的可办理用户

        public InfoPlusUser Entruster { get; set; }	    // 委托人


        public long CreateTime { get; set; }			// 创建时间
        public long LastUpdate { get; set; }			// 最后更新时间
        // 实例状态：-1->未知,0->已创建，1->进行中，2->挂起，3->已中止，4->已完成
        public int State { get; set; }
        public int Priority { get; set; }               // 优先级
        public string Tags { get; set; }                // 标签
        
        public long Version { get; set; }               // 工作流版本
        public int rating { get; set; }                 // 评价
        public string review { get; set; }              // 评价内容
        public bool release { get; set; }               // 是否发布版
        public string token { get; set; }               // 匿名访问的Token
        // public bool archived { get; set; }               // 是否已删除
        // public long deadline { get; set; }              // 承诺办理时间
        // public long timeout { get; set; }               // 流程超时时间


    }
}
